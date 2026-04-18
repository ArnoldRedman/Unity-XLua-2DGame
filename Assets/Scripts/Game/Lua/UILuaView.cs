#if XLUA
using UnityEngine;
using XLua;

namespace SkierFramework
{
    /// <summary>
    /// 通用 Lua 驱动的 UIView
    /// 所有新 UI 预制体统一挂这一个脚本，通过 luaModulePath 指定对应的 Lua 控制器
    /// 这样新增 UI 不需要写任何 C# 代码，只需热更 Lua 文件 + 预制体
    /// </summary>
    public class UILuaView : UIView
    {
        [Tooltip("Lua 控制器模块路径，如 UI.Bag.UIBagView")]
        public string luaModulePath;

        private LuaTable _luaView;

        /// <summary>
        /// 获取 Lua 控制器实例，供 UILuaItem 等外部访问
        /// </summary>
        public LuaTable LuaView => _luaView;

        public override void OnInit(UIControlData uIControlData, UIViewController controller)
        {
            base.OnInit(uIControlData, controller);

            if (string.IsNullOrEmpty(luaModulePath))
            {
                Debug.LogError($"[UILuaView] luaModulePath is empty on {gameObject.name}");
                return;
            }

            _luaView = LuaUIBridge.Create(luaModulePath, this);
        }

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            LuaUIBridge.Call(_luaView, "OnOpen", userData);
        }

        public override void OnAddListener()
        {
            base.OnAddListener();
            LuaUIBridge.Call(_luaView, "OnAddListener");
        }

        public override void OnRemoveListener()
        {
            base.OnRemoveListener();
            LuaUIBridge.Call(_luaView, "OnRemoveListener");
        }

        public override void OnResume()
        {
            base.OnResume();
            LuaUIBridge.Call(_luaView, "OnResume");
        }

        public override void OnPause()
        {
            base.OnPause();
            LuaUIBridge.Call(_luaView, "OnPause");
        }

        public override void OnClose()
        {
            LuaUIBridge.Call(_luaView, "OnClose");
            base.OnClose();
        }

        private void Update()
        {
            // 每帧转发给 Lua（用于逐字显示等需要 Update 的功能）
            LuaUIBridge.Call(_luaView, "OnUpdate");
        }

        public override void OnRelease()
        {
            LuaUIBridge.Destroy(ref _luaView);
            base.OnRelease();
        }

        /// <summary>
        /// 供 Lua 调用：刷新 UIScrollView 列表
        /// Lua 端调用: self.csView:UpdateScrollView(scrollView, luaDataArray, itemPrefab)
        /// </summary>
        public void UpdateScrollView(UIScrollView scrollView, LuaTable luaDataArray, GameObject itemPrefab)
        {
            if (scrollView == null || itemPrefab == null) return;
            var list = LuaItemData.FromLuaArray(luaDataArray);
            scrollView.UpdateList(list, itemPrefab, typeof(UILuaItem));
        }
    }
}
#endif
