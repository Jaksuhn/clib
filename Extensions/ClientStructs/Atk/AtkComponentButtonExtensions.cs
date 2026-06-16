using FFXIVClientStructs.FFXIV.Component.GUI;

namespace clib.Extensions;

public static unsafe class AtkComponentButtonExtensions {
    extension(scoped ref AtkComponentButton target) {
        public void Click() {
            fixed (AtkComponentButton* ptr = &target) {
                if (ptr == null) return;

                var ownerNode = ptr->OwnerNode;
                var evt = ownerNode->AtkResNode.AtkEventManager.Event;
                ownerNode->OwnerAddon->ReceiveEvent(evt->State.EventType, (int)evt->Param, evt);
            }
        }
    }
}
