using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace clib.Extensions;

public static unsafe class AgentShopExtensions {
    extension(AgentShop) {
        public static bool IsShopOpen(uint shopId = 0) {
            var agent = AgentShop.Instance();
            if (agent == null || !agent->IsAgentActive() || agent->EventReceiver == null || !agent->IsAddonReady())
                return false;
            if (shopId == 0)
                return true; // some shop is open...
            if (!EventFramework.Instance()->EventHandlerModule.EventHandlerMap.TryGetValuePointer(shopId, out var eh) || eh == null || eh->Value == null)
                return false;
            var proxy = (ShopEventHandler.AgentProxy*)agent->EventReceiver;
            return proxy->Handler == eh->Value;
        }

        public static bool CloseShop() {
            var agent = AgentShop.Instance();
            if (agent == null || agent->EventReceiver == null)
                return false;
            AtkValue res = default, arg = default;
            var proxy = (ShopEventHandler.AgentProxy*)agent->EventReceiver;
            proxy->Handler->CancelInteraction();
            arg.SetInt(-1);
            agent->ReceiveEvent(&res, &arg, 1, 0);
            return true;
        }

        public static bool BuyItemFromShop(uint shopId, uint itemId, int count) {
            if (!EventFramework.Instance()->EventHandlerModule.EventHandlerMap.TryGetValuePointer(shopId, out var eh) || eh == null || eh->Value == null) {
                Svc.Log.Error($"Event handler for shop {shopId:X} not found");
                return false;
            }

            if (eh->Value->Info.EventId.ContentId != EventHandlerContent.Shop) {
                Svc.Log.Error($"{shopId:X} is not a shop");
                return false;
            }

            var shop = (ShopEventHandler*)eh->Value;
            for (var i = 0; i < shop->VisibleItemsCount; ++i) {
                var index = shop->VisibleItems[i];
                if (shop->Items[index].ItemId == itemId) {
                    Svc.Log.Debug($"Buying {count}x {itemId} from {shopId:X}");
                    shop->BuyItemIndex = index;
                    shop->ExecuteBuy(count);
                    return true;
                }
            }

            Svc.Log.Error($"Did not find item {itemId} in shop {shopId:X}");
            return false;
        }

        public static bool ShopTransactionInProgress(uint shopId) {
            if (!EventFramework.Instance()->EventHandlerModule.EventHandlerMap.TryGetValuePointer(shopId, out var eh) || eh == null || eh->Value == null) {
                Svc.Log.Error($"Event handler for shop {shopId:X} not found");
                return false;
            }

            if (eh->Value->Info.EventId.ContentId != EventHandlerContent.Shop) {
                Svc.Log.Error($"{shopId:X} is not a shop");
                return false;
            }

            var shop = (ShopEventHandler*)eh->Value;
            return shop->WaitingForTransactionToFinish;
        }
    }
}
