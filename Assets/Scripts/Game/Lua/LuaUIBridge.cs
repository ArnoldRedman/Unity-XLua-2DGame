#if XLUA
using System;
using UnityEngine;
using XLua;

namespace SkierFramework
{
    /// <summary>
    /// Lua UI桥接层：将C# UIView的生命周期转发给Lua控制器
    /// 
    /// 用法：
    ///   在C# UIView.OnInit中调用 LuaUIBridge.Create("UI.Bag.UIBagView", this)
    ///   在各生命周期中调用 LuaUIBridge.Call(luaView, "OnOpen", userData) 等
    /// </summary>
    public static class LuaUIBridge
    {
        /// <summary>
        /// 创建Lua UI控制器实例
        /// </summary>
        /// <param name="luaModulePath">Lua模块路径，如 "UI.Bag.UIBagView"</param>
        /// <param name="csView">C# UIView实例，会注入到Lua的self.csView中</param>
        /// <returns>Lua控制器实例（LuaTable）</returns>
        public static LuaTable Create(string luaModulePath, UIView csView)
        {
            var luaEnv = LuaEnvManager.Instance.LuaEnv;
            if (luaEnv == null)
            {
                Debug.LogError("[LuaUIBridge] LuaEnv is null, cannot create Lua UI controller.");
                return null;
            }

            // require模块，获得模块table（类定义）
            object[] results = luaEnv.DoString($"return require('{luaModulePath}')", luaModulePath);
            if (results == null || results.Length == 0 || !(results[0] is LuaTable moduleTable))
            {
                Debug.LogError($"[LuaUIBridge] Failed to require lua module: {luaModulePath}");
                return null;
            }

            // 创建实例table，设置元表指向模块table（实现方法查找）
            LuaTable instance = luaEnv.NewTable();
            using (LuaTable meta = luaEnv.NewTable())
            {
                meta.Set("__index", moduleTable);
                instance.SetMetaTable(meta);
            }

            // 注入C#引用
            instance.Set("csView", csView);
            instance.Set("gameObject", csView.gameObject);
            instance.Set("transform", csView.transform);

            // 绑定UIControlData中的控件到Lua
            var controlData = csView.GetComponent<UIControlData>();
            if (controlData != null)
            {
                controlData.BindDataToLua(csView.GetComponent<LuaViewRunner>() ?? csView as IBindableUI, instance);
            }

            return instance;
        }

        /// <summary>
        /// 调用Lua控制器上的方法（无额外参数）
        /// </summary>
        public static void Call(LuaTable luaView, string methodName)
        {
            if (luaView == null) return;

            LuaFunction func = luaView.Get<LuaFunction>(methodName);
            if (func != null)
            {
                func.Call(luaView);
                func.Dispose();
            }
        }

        /// <summary>
        /// 调用Lua控制器上的方法（1个参数）
        /// </summary>
        public static void Call(LuaTable luaView, string methodName, object arg1)
        {
            if (luaView == null) return;

            LuaFunction func = luaView.Get<LuaFunction>(methodName);
            if (func != null)
            {
                func.Call(luaView, arg1);
                func.Dispose();
            }
        }

        /// <summary>
        /// 调用Lua控制器上的方法（2个参数）
        /// </summary>
        public static void Call(LuaTable luaView, string methodName, object arg1, object arg2)
        {
            if (luaView == null) return;

            LuaFunction func = luaView.Get<LuaFunction>(methodName);
            if (func != null)
            {
                func.Call(luaView, arg1, arg2);
                func.Dispose();
            }
        }

        /// <summary>
        /// 销毁Lua UI控制器
        /// </summary>
        public static void Destroy(ref LuaTable luaView)
        {
            if (luaView != null)
            {
                Call(luaView, "OnRelease");
                luaView.Dispose();
                luaView = null;
            }
        }

        /// <summary>
        /// 供 Lua 调用：异步加载 Sprite 并设置到 Image
        /// Lua 用法: CS.SkierFramework.LuaUIBridge.LoadIcon(image, "icon_potion_hp_s")
        /// </summary>
        public static void LoadIcon(UnityEngine.UI.Image image, string address)
        {
            if (image == null || string.IsNullOrEmpty(address)) return;
            try
            {
                ResourceManager.Instance.LoadAssetAsync<Sprite>(address, (sprite) =>
                {
                    if (image != null && sprite != null)
                        image.sprite = sprite;
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LuaUIBridge] LoadIcon failed for '{address}': {e.Message}");
            }
        }
    }
}
#endif
