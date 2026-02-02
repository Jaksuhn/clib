using clib.Services;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace clib.TaskSystem;

[Flags]
public enum MovementOptions {
    None = 0,
    Mount = 1 << 0,
    Fly = 1 << 1,
    Dismount = 1 << 2,
}

public enum PathingStrategy {
    Auto = 0,
    Navmesh = 1,
    Direct = 2,
}

public static class MovementOptionsExtensions {
    extension(MovementOptions) {
        public static MovementOptions GetCurrent() {
            if (Svc.Objects.LocalPlayer.InFlight)
                return MovementOptions.Mount | MovementOptions.Fly | MovementOptions.Dismount;
            if (Svc.Objects.LocalPlayer.Mounted)
                return MovementOptions.Mount | MovementOptions.Dismount;
            return MovementOptions.None;
        }
    }
}

public readonly record struct MovementConfig(float? Tolerance, MovementOptions Movement, PathingStrategy Pathing) {
    public static MovementConfig Default => new(null, MovementOptions.None, PathingStrategy.Auto);
    public static MovementConfig Everything => new(null, MovementOptions.Mount | MovementOptions.Fly | MovementOptions.Dismount, PathingStrategy.Auto);
    public static MovementConfig GroundMove => new(null, MovementOptions.Mount | MovementOptions.Dismount, PathingStrategy.Auto);
    public static MovementConfig InteractRange => new(3, MovementOptions.None, PathingStrategy.Auto);

    public MovementConfig WithTolerance(float? tolerance) => this with { Tolerance = tolerance };
    public MovementConfig WithOptions(MovementOptions movement) => this with { Movement = movement };
    public MovementConfig WithStrategy(PathingStrategy pathing) => this with { Pathing = pathing };
}

[Flags]
public enum UiSkipOptions {
    None = 0,
    Talk = 1 << 0,
    YesNo = 1 << 1,
    Request = 1 << 2,
}

public abstract class TaskBase : AutoTask {
    private readonly OverrideMovement movement = new();
    private static IPlayerCharacter? Player => Svc.Objects.LocalPlayer;

    protected TaskBase() {
        RegisterCleanup(movement);
    }

    private async Task NavmeshReady() {
        using var scope = BeginScope("WaitingForNavmesh");
        Status = "Waiting for Navmesh";
        await WaitUntil(() => Svc.Navmesh.IsReady || Svc.Navmesh.BuildProgress >= 0, "WaitForBuildStart");
        if (Svc.Navmesh.BuildProgress >= 0) {
            await WaitWhile(() => Svc.Navmesh.BuildProgress >= 0, "BuildMesh");
        }
        ErrorIf(!Svc.Navmesh.IsReady, "Failed to build navmesh for the zone");
    }

    protected async Task MoveTo(FlagMapMarker flag, MovementConfig config, Func<bool>? stopCondition = null, Func<Task>? onStopReached = null) {
        using var scope = BeginScope("MoveToFlag");
        await TeleportTo(flag.TerritoryId, flag);
        await NavmeshReady();
        await MoveTo(flag.ToVector3(), config, stopCondition, onStopReached);
    }

    protected async Task MoveTo(Vector3 dest, MovementConfig config, Func<bool>? stopCondition = null, Func<Task>? onStopReached = null) {
        using var scope = BeginScope("MoveTo");
        await WaitUntil(() => Player.Available, "WaitingForPlayer");
        var tolerance = config.Tolerance ?? Svc.Navmesh.GetTolerance();
        if (Player.WithinRange(dest, tolerance))
            return;

        if (Coords.IsTeleportingFaster(dest)) {
            await TeleportTo(Svc.ClientState.TerritoryType, dest, allowSameZoneTeleport: true);
        }

        if (config.Movement.HasFlag(MovementOptions.Mount) || config.Movement.HasFlag(MovementOptions.Fly))
            await Mount();

        if (config.Pathing == PathingStrategy.Direct)
            await MoveToDirectly(dest, tolerance);
        else {
            await NavmeshReady();
            await WaitUntil(() => !Svc.Navmesh.PathfindingInProgress, "Waiting for in-progress calls to finish");
            ErrorIf(!Svc.Navmesh.PathfindAndMoveTo(dest, config.Movement.HasFlag(MovementOptions.Fly) && Control.CanFly), "Failed to start pathfinding to destination");
            Status = $"Moving to {dest}";
            using var stop = new OnDispose(Svc.Navmesh.Stop);

            if (stopCondition is null)
                await WaitWhile(() => !Player.WithinRange(dest, tolerance), "Navigate");
            else {
                await WaitWhile(() => !(Player.WithinRange(dest, tolerance) || stopCondition()), "Navigate");
                if (stopCondition() && onStopReached is not null) {
                    Svc.Navmesh.Stop(); // must be stopped because onStopReached's MoveTo (if present) calls !PathfindingInProgress
                    await onStopReached();
                }
            }
        }

        if (config.Movement.HasFlag(MovementOptions.Dismount) && Player.WithinRange(dest, tolerance)) // only dismount if we're close
            await Dismount();
    }

    protected async Task MoveToDirectly(Vector3 dest, Func<bool> stopCondition) {
        using var scope = BeginScope("MoveDirectly");
        if (stopCondition())
            return;

        Status = $"Moving to {dest}";
        movement.DesiredPosition = dest;
        movement.Enabled = true;
        using var stop = new OnDispose(() => movement.Enabled = false);
        await WaitUntil(stopCondition, "WaitForCondition");
    }

    protected async Task MoveToDirectly(Vector3 dest, float tolerance) {
        using var scope = BeginScope("MoveDirectlyWithTolerance");
        await MoveToDirectly(dest, () => Player.WithinRange(dest, tolerance));
    }

    protected async Task TeleportTo(uint territoryId, FlagMapMarker flag, bool allowSameZoneTeleport = false)
        => await TeleportTo(territoryId, new Vector3(flag.XFloat, 0, flag.YFloat), allowSameZoneTeleport);

    protected async Task TeleportTo(uint territoryId, Vector3 destination, bool allowSameZoneTeleport = false) {
        using var scope = BeginScope("Teleport");
        if (!allowSameZoneTeleport && Svc.ClientState.TerritoryType == territoryId)
            return; // already in correct zone

        var closestAetheryteId = Coords.FindClosestAetheryte(territoryId, destination) ?? 0;
        var teleportAetheryteId = Coords.FindPrimaryAetheryte(closestAetheryteId);
        ErrorIf(teleportAetheryteId == 0, $"Failed to find aetheryte in [{territoryId}] {Svc.Data.GetRef<Sheets.TerritoryType>(territoryId).Value.PlaceName.Value.Name}");
        if (Svc.Data.GetRef<Sheets.Aetheryte>(teleportAetheryteId) is { Value.Territory.RowId: var destinationId, Value.PlaceName.Value.Name: var destinationName } && Svc.ClientState.TerritoryType != destinationId) {
            Status = $"Teleporting to {destinationName}";
            await CastAction(() => ActionManager.Teleport(teleportAetheryteId), ActionType.GeneralAction, 5);
            await WaitUntilTerritory(destinationId);
        }

        if (Svc.ClientState.TerritoryType == territoryId) {
            Status = "Teleporting to aetheryte";
            await CastAction(() => ActionManager.Teleport(teleportAetheryteId), ActionType.GeneralAction, 5);
            if (teleportAetheryteId == closestAetheryteId) return;

            var (aetheryteId, aetherytePos) = Coords.FindAetheryte(teleportAetheryteId);
            if (!Player.WithinRange(aetherytePos, 15))
                await MoveTo(aetherytePos, MovementConfig.GroundMove.WithTolerance(10), skipTeleportCheck: true);
            ErrorIf(!TargetSystem.InteractWith(aetheryteId), "Failed to interact with aetheryte");
            await WaitUntilSkipping(() => AtkUnitBase.IsAddonReady("SelectString"), "WaitSelectAethernet", UiSkipOptions.Talk);
            PacketDispatcher.TeleportToAethernet(teleportAetheryteId, closestAetheryteId);
            await WaitUntil(() => Player.IsBusy, "TeleportStart");
            await WaitUntil(() => Svc.ClientState.TerritoryType == territoryId && GameMain.IsTerritoryLoaded && Player.Interactable, "TeleportFinish");
            return;
        }

        if (teleportAetheryteId != closestAetheryteId) {
            Status = $"Interacting with aethernet to get to [{territoryId}]";
            var (aetheryteId, aetherytePos) = Coords.FindAetheryte(teleportAetheryteId);
            await MoveTo(aetherytePos, MovementConfig.Default.WithTolerance(10));
            ErrorIf(!TargetSystem.InteractWith(aetheryteId), "Failed to interact with aetheryte");
            await WaitUntilSkipping(() => AtkUnitBase.IsAddonReady("SelectString"), "WaitSelectAethernet", UiSkipOptions.Talk);
            PacketDispatcher.TeleportToAethernet(teleportAetheryteId, closestAetheryteId);
            await WaitUntil(() => Player.IsBusy, "TeleportStart"); // TODO: something better
            await WaitUntil(() => Svc.ClientState.TerritoryType == territoryId && GameMain.IsTerritoryLoaded && Player.Interactable, "TeleportFinish");
        }

        if (territoryId == 886) {
            // firmament special case
            Status = $"Interacting with aetheryte to get to the Firmament";
            var (aetheryteId, aetherytePos) = Coords.FindAetheryte(teleportAetheryteId);
            await MoveTo(aetherytePos, MovementConfig.Default.WithTolerance(10));
            ErrorIf(!TargetSystem.InteractWith(aetheryteId), "Failed to interact with aetheryte");
            await WaitUntilSkipping(() => AtkUnitBase.IsAddonReady("SelectString"), "WaitSelectFirmament", UiSkipOptions.Talk);
            PacketDispatcher.TeleportToFirmament(teleportAetheryteId);
            await WaitUntilTerritory(territoryId);
        }

        // I think this check gives more problems than it solves
        WarningIf(Svc.ClientState.TerritoryType != territoryId, $"Failed to teleport to expected zone (exp: {territoryId}, act: {Svc.ClientState.TerritoryType})");
    }

    protected async Task CastAction(ActionType actionType, uint actionId, int maxRetries = 5) {
        using var scope = BeginScope(nameof(CastAction));
        using var hooksScope = new OnDispose(Svc.CastHooks.Disable);
        Svc.CastHooks.Enable();

        var attempt = 0;
        while (maxRetries == 0 || attempt < maxRetries) {
            attempt++;
            Status = $"{nameof(CastAction)} (attempt {attempt})";
            // TODO: maybe if we're in combat, call vbm to deal with it?
            await WaitWhile(() => Player.IsBusy || ActionManager.IsActionInUse(actionType, actionId), $"WaitForNotBusy");

            Svc.CastHooks.StartWatching(actionType, actionId);

            Verbose($"Starting cast of {actionType} {actionId}");
            if (!ActionManager.UseAction(actionType, actionId)) {
                Svc.CastHooks.ClearWatching();
                continue;
            }

            while (true) {
                if (Svc.CastHooks.LastCastCompleted) {
                    Verbose($"{actionType}:#{actionId} completed");
                    Svc.CastHooks.ClearWatching();
                    return;
                }

                if (Svc.CastHooks.LastCastCancelled) {
                    Verbose($"{actionType}:#{actionId} cancelled");
                    break;
                }

                await NextFrame();
            }
            Svc.CastHooks.ClearWatching();
        }

        Error($"{nameof(CastAction)} failed after {maxRetries} attempts");
    }

    /// <remarks>Only for actions triggered outside of ActionManager.</remarks>
    protected async Task CastAction(Func<bool> action, ActionType actionType, uint actionId, int maxRetries = 5) {
        using var scope = BeginScope(nameof(CastAction));
        using var hooksScope = new OnDispose(Svc.CastHooks.Disable);
        Svc.CastHooks.Enable();

        var attempt = 0;
        while (maxRetries == 0 || attempt < maxRetries) {
            attempt++;
            Status = $"{nameof(CastAction)} (attempt {attempt})";
            await WaitWhile(() => Player.IsBusy, $"WaitForNotBusy");

            Svc.CastHooks.StartWatching(actionType, actionId);

            Verbose($"Starting cast of {actionType} {actionId}");
            if (!action()) {
                Svc.CastHooks.ClearWatching();
                continue;
            }

            while (true) {
                if (Svc.CastHooks.LastCastCompleted) {
                    Verbose($"{actionType}:#{actionId} completed");
                    Svc.CastHooks.ClearWatching();
                    return;
                }

                if (Svc.CastHooks.LastCastCancelled) {
                    Verbose($"{actionType}:#{actionId} cancelled");
                    break;
                }

                await NextFrame();
            }
            Svc.CastHooks.ClearWatching();
        }

        Error($"{nameof(CastAction)} failed after {maxRetries} attempts");
    }

    protected async Task Mount() {
        using var scope = BeginScope("Mount");
        if (Player is null || Player.Mounted) return;
        Status = "Mounting";
        while (!Player.Mounted) { // TODO: this is terrible. Need a better CastAction
            await CastAction(ActionType.GeneralAction, 24);
            await WaitUntil(() => Player.Mounted, () => Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat], "WaitForMount");
        }
        ErrorIf(!Player.Mounted, "Failed to mount after retries");
    }

    protected async Task Dismount() {
        using var scope = BeginScope("Dismount");
        if (Player is null || !Player.Mounted) return;

        if (Svc.Navmesh.NearestPointReachable(Player.Position) is { } nearestPoint)
            await MoveTo(nearestPoint, MovementConfig.Everything);
        else
            Warning($"No nearest landable point found from {Player.Position}. Dismounting may fail");

        // we are assuming from here on out that you cannot possibly be above ground that is unlandable
        if (Player.InFlight && !Player.IsAirDismountable) {
            ActionManager.UseAction(ActionType.GeneralAction, 23); // TODO: find a force ground function
            await WaitWhile(() => Player.InFlight, "WaitForGround");
        }
        if (Player.InFlight && Player.IsAirDismountable) {
            GameMain.ExecuteLocationCommand(LocationCommandFlag.Dismount, Player.Position, (int)Player.PackedRotation);
            await WaitWhile(() => Player.Mounted, "WaitForDismount");
        }
        if (Player.Mounted && !Player.InFlight) {
            GameMain.ExecuteCommand(CommandFlag.Dismount, 1);
            await WaitWhile(() => Player.Mounted, "WaitForDismount");
        }
        ErrorIf(Player.Mounted, "Failed to dismount");
    }

    protected async Task WaitUntilSkipping(Func<bool> condition, string scopeName, UiSkipOptions skip) {
        using var scope = BeginScope(scopeName);
        while (!condition()) {
            if (skip.HasFlag(UiSkipOptions.Talk) && AtkUnitBase.IsAddonReady("Talk")) {
                Log("progressing talk...");
                AddonTalk.Progress();
            }
            if (skip.HasFlag(UiSkipOptions.YesNo) && AtkUnitBase.IsAddonReady("SelectYesno")) {
                Log("progressing yes/no...");
                AddonSelectYesno.Yes();
            }
            if (skip.HasFlag(UiSkipOptions.Request) && AtkUnitBase.IsAddonReady("Request")) {
                Log("progressing request...");
                AgentNpcTrade.TurnInRequests();
            }
            Log("waiting...");
            await NextFrame();
        }
    }

    protected async Task WaitUntilTerritory(uint territoryId) {
        using var scope = BeginScope("WaitUntilTerritory");
        await WaitUntil(() => Svc.ClientState.TerritoryType == territoryId && GameMain.IsTerritoryLoaded && Player.Interactable, "WaitingForTerritory");
    }

    protected async Task InteractWith(IGameObject obj, Func<bool>? waitUntil = null, int? selectStringIndex = null, UiSkipOptions skip = UiSkipOptions.None) {
        using var scope = BeginScope("InteractWith");

        if (!obj.IsInInteractRange()) {
            Log("Not in interact range, moving closer");
            await MoveToDirectly(obj.Position, obj.IsInInteractRange);
        }

        Status = $"Interacting with {obj.GameObjectId}";
        await WaitWhile(() => Player.IsJumping, "WaitForAbleToInteract");
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++) {
            if (TargetSystem.InteractWith(obj.GameObjectId)) {
                if (selectStringIndex is { } index) {
                    await WaitUntil(() => AtkUnitBase.IsAddonReady("SelectString"), "WaitingForSelectString");
                    AddonSelectString.Select(index);
                }
                if (waitUntil is { } condition) {
                    await WaitUntilSkipping(condition, "WaitingForNpcInteractionToFinish", skip);
                    return;
                }
                else return;
            }
            await NextFrame();
        }
        ErrorIf(true, $"Failed to interact with object after {maxAttempts} tries");
    }
}
