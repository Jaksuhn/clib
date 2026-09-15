using FFXIVClientStructs.FFXIV.Component.GUI;

namespace clib.Extensions;

public static unsafe class AtkComponentCheckBoxExtensions {
    extension(scoped ref AtkComponentCheckBox target) {
        public void Click() {
            fixed (AtkComponentCheckBox* box = &target) {
                if (box == null || !box->IsEnabled || box->OwnerNode == null)
                    return;

                var evt = box->OwnerNode->AtkResNode.AtkEventManager.Event;
                while (evt != null && evt->State.EventType != AtkEventType.ButtonClick)
                    evt = evt->NextEvent;
                if (evt == null)
                    return;
                var data = stackalloc AtkEventData[1];
                box->SetChecked(!box->IsChecked);
                box->OwnerNode->AtkResNode.OwnerAddon->ReceiveEvent(evt->State.EventType, (int)evt->Param, evt, data);
            }
        }
    }
}
