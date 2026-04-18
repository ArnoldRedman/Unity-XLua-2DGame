#if !XLUA
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if XLUA
using XLua;
#endif

namespace SkierFramework
{
    /// <summary>
    /// 背包界面 C# 薄壳
    /// 生命周期全部转发给 Lua 控制器 (UI.Bag.UIBagView)
    /// </summary>
    public class UIBagView : UIView
    {
        [ControlBinding] private Button BtnClose;
        [ControlBinding] private Button BtnSort;
        [ControlBinding] private UIScrollView ScrollView;
        [ControlBinding] private GameObject ItemPrefab;
        [ControlBinding] private TextMeshProUGUI TxtGold;
        [ControlBinding] private TextMeshProUGUI TxtCapacity;

#if XLUA
        private LuaTable _luaView;
#endif

        public override void OnInit(UIControlData uIControlData, UIViewController controller)
        {
            base.OnInit(uIControlData, controller);

#if XLUA
            _luaView = LuaUIBridge.Create("UI.Bag.UIBagView", this);
#endif
        }

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);

#if XLUA
            LuaUIBridge.Call(_luaView, "OnOpen", userData);
#endif
        }

        public override void OnAddListener()
        {
            base.OnAddListener();
            BtnClose.AddClick(OnClickClose);
            BtnSort.AddClick(OnClickSort);
        }

        public override void OnRemoveListener()
        {
            base.OnRemoveListener();
            BtnClose.onClick.RemoveAllListeners();
            BtnSort.onClick.RemoveAllListeners();
        }

        public override void OnClose()
        {
#if XLUA
            LuaUIBridge.Call(_luaView, "OnClose");
#endif
            base.OnClose();
        }

        public override void OnRelease()
        {
#if XLUA
            LuaUIBridge.Destroy(ref _luaView);
#endif
            base.OnRelease();
        }

        // ---------- Lua 调用的 C# 方法 ----------

        /// <summary>
        /// 由 Lua 调用，刷新背包列表
        /// Lua 传过来的是一个 LuaTable 数组，转换后交给 UIScrollView
        /// </summary>
#if XLUA
        public void RefreshList(LuaTable luaItemList)
        {
            if (luaItemList == null) return;

            var list = new List<BagItemData>();
            int len = luaItemList.Length;
            for (int i = 1; i <= len; i++)
            {
                LuaTable item = luaItemList.Get<int, LuaTable>(i);
                if (item != null)
                {
                    list.Add(new BagItemData
                    {
                        slotIndex = item.Get<string, int>("slotIndex"),
                        itemId = item.Get<string, int>("itemId"),
                        count = item.Get<string, int>("count"),
                        name = item.Get<string, string>("name"),
                        icon = item.Get<string, string>("icon"),
                        quality = item.Get<string, int>("quality"),
                        type = item.Get<string, int>("type"),
                    });
                }
            }

            ScrollView.UpdateList<UIBagItem>(list, ItemPrefab);

            // 刷新容量显示
            if (TxtCapacity != null)
            {
                TxtCapacity.text = $"{list.Count}/{30}";
            }
        }
#endif

        // ---------- 按钮回调 ----------

        private void OnClickClose()
        {
#if XLUA
            LuaUIBridge.Call(_luaView, "OnClickClose");
#else
            UIManager.Instance.Close(Controller.uiType);
#endif
        }

        private void OnClickSort()
        {
#if XLUA
            LuaUIBridge.Call(_luaView, "OnClickSort");
#endif
        }
    }

    /// <summary>
    /// 背包物品数据，用于 UIScrollView 列表传递
    /// </summary>
    public class BagItemData
    {
        public int slotIndex;
        public int itemId;
        public int count;
        public string name;
        public string icon;
        public int quality;
        public int type;
    }
}
#endif
