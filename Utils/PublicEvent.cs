using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.Interop;

namespace clib.Utils;

public enum FateType {
    Normal,
    DynamicEvent, // forays
    MechaEvent, // cosmic exploration
}

/// <summary>
/// Wrapper for all public event types (FATEs, Dynamic Events, Mecha Events)
/// </summary>
public unsafe class PublicEvent(nint address, FateType fateType, uint id) {
    public IntPtr Address { get; } = address;
    public FateType FateType { get; } = fateType;
    public uint Id { get; } = id;

    public static implicit operator PublicEvent(FateContext* fate) => new((nint)fate, FateType.Normal, fate->FateId);
    public static implicit operator PublicEvent(Pointer<FateContext> fate) => new((nint)fate.Value, FateType.Normal, fate.Value->FateId);
    public static implicit operator PublicEvent(DynamicEvent* dynamicEvent) => new((nint)dynamicEvent, FateType.DynamicEvent, dynamicEvent->DynamicEventId);
    public static implicit operator PublicEvent(DynamicEvent dynamicEvent) => new((nint)(&dynamicEvent), FateType.DynamicEvent, dynamicEvent.DynamicEventId);
    public static implicit operator PublicEvent(WKSMechaEvent* mechaEvent) => new((nint)mechaEvent, FateType.MechaEvent, mechaEvent->WKSMechaEventDataRowId);
    public static implicit operator PublicEvent(WKSMechaEvent mechaEvent) => new((nint)(&mechaEvent), FateType.MechaEvent, mechaEvent.WKSMechaEventDataRowId);

    public static PublicEvent? CurrentFate => (TerritoryIntendedUse)Svc.Data.GetRef<Sheets.TerritoryType>(Svc.ClientState.TerritoryType).Value.TerritoryIntendedUse.RowId switch {
        TerritoryIntendedUse.Overworld => FateManager.Instance()->CurrentFate != null ? FateManager.Instance()->CurrentFate : null,
        TerritoryIntendedUse.Bozja or TerritoryIntendedUse.OccultCrescent => DynamicEventContainer.GetInstance()->GetCurrentEvent() != null ? DynamicEventContainer.GetInstance()->GetCurrentEvent() : null,
        TerritoryIntendedUse.CosmicExploration => WKSManager.Instance()->MechaEventModule->CurrentEvent != null ? WKSManager.Instance()->MechaEventModule->CurrentEvent : null,
        _ => throw new NotImplementedException(),
    };

    public static unsafe IEnumerable<PublicEvent> Fates => (TerritoryIntendedUse)Svc.Data.GetRef<Sheets.TerritoryType>(Svc.ClientState.TerritoryType).Value.TerritoryIntendedUse.RowId switch {
        TerritoryIntendedUse.Overworld => FateManager.Instance()->Fates.Select(evt => (PublicEvent)evt),
        TerritoryIntendedUse.Bozja or TerritoryIntendedUse.OccultCrescent => DynamicEventContainer.GetInstance()->Events.ToArray().Select(evt => (PublicEvent)evt),
        TerritoryIntendedUse.CosmicExploration => WKSManager.Instance()->MechaEventModule->Events.ToArray().Select(evt => (PublicEvent)evt),
        _ => [],
    };

    private FateContext* GetFate() {
        var fate = (FateContext*)Address;
        if (fate != null && fate->FateId == Id)
            return fate;
        return FateManager.Instance()->GetFateById((ushort)Id);
    }

    private DynamicEvent GetDynamicEvent() {
        var dynamicEvent = (DynamicEvent*)Address;
        if (dynamicEvent != null && dynamicEvent->DynamicEventId == Id)
            return *dynamicEvent;

        foreach (var evt in DynamicEventContainer.GetInstance()->Events)
            if (evt.DynamicEventId == Id)
                return evt;
        throw new InvalidOperationException($"DynamicEvent with ID {Id} not found");
    }

    private WKSMechaEvent GetMechaEvent() {
        var mechaEvent = (WKSMechaEvent*)Address;
        if (mechaEvent != null && mechaEvent->WKSMechaEventDataRowId == Id)
            return *mechaEvent;

        foreach (var evt in WKSManager.Instance()->MechaEventModule->Events)
            if (evt.WKSMechaEventDataRowId == Id)
                return evt;
        throw new InvalidOperationException($"WKSMechaEvent with ID {Id} not found");
    }

    private T GetValue<T>(Func<nint, T> getFate, Func<DynamicEvent, T> getDynamicEvent, Func<WKSMechaEvent, T> getMechaEvent, T defaultValue = default!) => FateType switch {
        FateType.Normal => getFate((nint)GetFate()),
        FateType.DynamicEvent => getDynamicEvent(GetDynamicEvent()),
        FateType.MechaEvent => getMechaEvent(GetMechaEvent()),
        _ => defaultValue,
    };

    public Vector3 Position => GetValue(
        fate => fate.As<FateContext>()->Location,
        dynamicEvent => dynamicEvent.MapMarker.Position,
        mechaEvent => mechaEvent.MapMarkers[0].MapMarkerData.Position,
        Vector3.Zero
    );

    public float Radius => GetValue(
        fate => fate.As<FateContext>()->Radius,
        dynamicEvent => dynamicEvent.MapMarker.Radius,
        mechaEvent => mechaEvent.MapMarkers[0].MapMarkerData.Radius,
        0f
    );

    public int Progress => GetValue(
        fate => fate.As<FateContext>()->Progress,
        dynamicEvent => dynamicEvent.Progress,
        mechaEvent => mechaEvent.EventProgress,
        0
    );

    public int Duration => GetValue(
        fate => fate.As<FateContext>()->Duration,
        dynamicEvent => (int)dynamicEvent.SecondsDuration,
        mechaEvent => mechaEvent.EventEndTimestamp - mechaEvent.EventStartTimestamp,
        0
    );

    public float TimeRemaining => GetValue(
        fate => fate.As<FateContext>()->StartTimeEpoch + fate.As<FateContext>()->Duration - DateTimeOffset.Now.ToUnixTimeSeconds(),
        dynamicEvent => dynamicEvent.SecondsLeft,
        mechaEvent => mechaEvent.EventStartTimestamp + (mechaEvent.EventEndTimestamp - mechaEvent.EventStartTimestamp) - DateTimeOffset.Now.ToUnixTimeSeconds(),
        -1f
    );

    public int StartTimeEpoch => GetValue(
        fate => fate.As<FateContext>()->StartTimeEpoch,
        dynamicEvent => dynamicEvent.StartTimestamp,
        mechaEvent => mechaEvent.EventStartTimestamp,
        0
    );

    public int EndTimeEpoch => GetValue(
        fate => fate.As<FateContext>()->StartTimeEpoch + fate.As<FateContext>()->Duration,
        dynamicEvent => (int)(dynamicEvent.StartTimestamp + dynamicEvent.SecondsDuration),
        mechaEvent => mechaEvent.EventEndTimestamp,
        0
    );

    public bool HasBonus => GetValue(
        fate => fate.As<FateContext>()->IsBonus,
        _ => false,
        _ => false,
        false
    );

    public byte Level => GetValue(
        fate => fate.As<FateContext>()->Level,
        dynamicEvent => (byte)dynamicEvent.MapMarker.RecommendedLevel,
        mechaEvent => (byte)mechaEvent.MapMarkers[0].MapMarkerData.RecommendedLevel,
        (byte)0
    );

    public string Name => FateType switch {
        FateType.Normal => Svc.Data.GetRef<Sheets.Fate>(Id).Value.Name.ToString() ?? $"FATE {Id}",
        FateType.DynamicEvent => Svc.Data.GetRef<Sheets.DynamicEvent>(Id).Value.Name.ToString() ?? $"DynamicEvent {Id}",
        FateType.MechaEvent => Svc.Data.GetRef<Sheets.WKSMechaEventData>(Id).Value.Unknown0.ToString() ?? $"MechaEvent {Id}",
        _ => $"Unknown Type: {Id}",
    };

    public uint MotivationNpcId => GetValue(
        fate => fate.As<FateContext>()->MotivationNpc,
        _ => 0u,
        _ => 0u,
        0u
    );

    public IGameObject? MotivationNpc => GetValue(
        fate => Svc.Objects.FirstOrDefault(o => o.EntityId == fate.As<FateContext>()->MotivationNpc),
        _ => null,
        _ => null,
        null
    );

    public FateState State => GetValue(
        fate => fate.As<FateContext>()->State,
        dynamicEvent => ToFateState(dynamicEvent.State),
        _ => FateState.Running, // ???
        (FateState)0
    );

    private FateState ToFateState(DynamicEventState state) => state switch {
        DynamicEventState.Register => FateState.Preparing,
        DynamicEventState.Warmup => FateState.Preparing,
        DynamicEventState.Battle => FateState.Running,
        _ => FateState.Ending,
    };
}

