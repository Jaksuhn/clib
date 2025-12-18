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
        await WaitWhile(() => Svc.Navmesh.BuildProgress >= 0, "BuildMesh");
        ErrorIf(!Svc.Navmesh.IsReady, "Failed to build navmesh for the zone");
    }

    protected async Task MoveTo(FlagMapMarker flag, MovementConfig config, Func<bool>? stopCondition = null, Func<Task>? onStopReached = null) {
        using var scope = BeginScope("MoveToFlag");
        await TeleportTo(flag.TerritoryId, flag.ToVector3());
        await MoveTo(flag.ToVector3(), config, stopCondition, onStopReached);
    }

    protected async Task MoveTo(Vector3 dest, MovementConfig config, Func<bool>? stopCondition = null, Func<Task>? onStopReached = null) {
        using var scope = BeginScope("MoveTo");
        await WaitUntil(() => Player.Available, "WaitingForPlayer");
        var tolerance = config.Tolerance ?? Svc.Navmesh.GetTolerance();
        if (Player.WithinRange(dest, tolerance))
            return;

        if (Coords.IsTeleportingFaster(dest)) {
            Log("Teleporting faster");
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
                if (stopCondition() && onStopReached is not null)
                    await onStopReached();
            }
        }

        if (config.Movement.HasFlag(MovementOptions.Dismount))
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

    protected async Task TeleportTo(uint territoryId, Vector3 destination, bool allowSameZoneTeleport = false) {
        using var scope = BeginScope("Teleport");
        if (!allowSameZoneTeleport && Svc.ClientState.TerritoryType == territoryId)
            return; // already in correct zone

        var closestAetheryteId = Coords.FindClosestAetheryte(territoryId, destination) ?? 0;
        var teleportAetheryteId = Coords.FindPrimaryAetheryte(closestAetheryteId);
        ErrorIf(teleportAetheryteId == 0, $"Failed to find aetheryte in [{territoryId}] {Svc.Data.GetRef<Sheets.TerritoryType>(territoryId).Value.PlaceName.Value.Name}");
        if (Svc.Data.GetRef<Sheets.Aetheryte>(teleportAetheryteId) is { Value.Territory.RowId: var destinationId, Value.PlaceName.Value.Name: var destinationName } && Svc.ClientState.TerritoryType != destinationId) {
            Status = $"Teleporting to {destinationName}";
            ErrorIf(!Coords.ExecuteTeleport(teleportAetheryteId), $"Failed to teleport to {teleportAetheryteId}");
            await WaitUntilTerritory(destinationId);
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

    protected async Task Mount() {
        using var scope = BeginScope("Mount");
        if (Player.Mounted) return;
        Status = "Mounting";
        await WaitUntil(() => ActionManager.UseAction(ActionType.GeneralAction, 24), "MountCast");
        await WaitUntil(() => Player.Mounted, "Mounting");
        ErrorIf(!Player.Mounted, "Failed to mount");
    }

    protected async Task Dismount() {
        using var scope = BeginScope("Dismount");
        if (!Player.Mounted) return;

        if (Player.InFlight) {
            //GameMain.ExecuteLocationCommand(LocationCommand.Dismount, );
            ActionManager.UseAction(ActionType.GeneralAction, 23); // TODO use ELC
            await WaitWhile(() => Player.InFlight, "WaitingToLand");
        }
        if (Player.Mounted && !Player.InFlight) {
            ActionManager.UseAction(ActionType.GeneralAction, 23);
            await WaitWhile(() => Player.Mounted, "WaitingToDismount");
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
