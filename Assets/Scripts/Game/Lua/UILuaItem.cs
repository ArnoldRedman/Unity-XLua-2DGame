#if XLUA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace SkierFramework
{
    /// <summary>
    /// Lua 列表项数据包装器
    /// 将 Lua table 包装成 C# 对象，用于 UIScrollView 的 IList
    /// </summary>
    public class LuaItemData
    {
        public LuaTable data;

        public LuaItemData(LuaTable data)
        {
            this.data = data;
        }

        /// <summary>
        /// 从 Lua table 数组构建 C# List，供 UIScrollView.UpdateList 使用
        /// </summary>
        public static List<LuaItemData> FromLuaArray(LuaTable luaArray)
        {
            var list = new List<LuaItemData>();
            if (luaArray == null) return list;
            int len = luaArray.Length;
            for (int i = 1; i <= len; i++)
            {
                LuaTable item = luaArray.Get<int, LuaTable>(i);
                if (item != null)
                    list.Add(new LuaItemData(item));
            }
            return list;
        }
    }

    /// <summary>
    /// 通用 Lua 驱动的列表项
    /// 所有列表项预制体统一挂这个脚本，通过 luaModulePath 指定对应的 Lua 控制器
    /// </summary>
    public class UILuaItem : UILoopItem
    {
        [Tooltip("Lua 列表项控制器模块路径，如 UI.Bag.UIBagItem")]
        public string luaModulePath;

        private LuaTable _luaItem;
        private LuaTable _moduleTable;

        public override void OnInit()
        {
            base.OnInit();
            InitLua();
        }

        private void InitLua()
        {
            if (_luaItem != null) return;
            if (string.IsNullOrEmpty(luaModulePath)) return;

            var luaEnv = LuaEnvManager.Instance?.LuaEnv;
            if (luaEnv == null) return;

            // require 模块
            object[] results = luaEnv.DoString($"return require('{luaModulePath}')", luaModulePath);
            if (results == null || results.Length == 0 || !(results[0] is LuaTable modTable))
            {
                Debug.LogError($"[UILuaItem] Failed to require: {luaModulePath}");
                return;
            }
            _moduleTable = modTable;

            // 创建实例
            _luaItem = luaEnv.NewTable();
            using (LuaTable meta = luaEnv.NewTable())
            {
                meta.Set("__index", _moduleTable);
                _luaItem.SetMetaTable(meta);
            }

            // 注入 C# 引用
            _luaItem.Set("csItem", this);
            _luaItem.Set("gameObject", gameObject);
            _luaItem.Set("transform", transform);

            // 绑定 UIControlData 控件到 Lua
            var controlData = GetComponent<UIControlData>();
            if (controlData != null)
            {
                controlData.BindDataToLua(this, _luaItem);
            }

            // 调用 Lua 端 OnInit
            LuaUIBridge.Call(_luaItem, "OnInit");
        }

        public override void OnAddListener()
        {
            base.OnAddListener();
            LuaUIBridge.Call(_luaItem, "OnAddListener");
        }

        protected override void OnUpdateData(IList dataList, int index, object userData)
        {
            if (_luaItem == null) return;
            if (index < 0 || index >= dataList.Count) return;

            var itemData = dataList[index] as LuaItemData;
            if (itemData != null)
            {
                // 传 Lua table 数据给 Lua 控制器
                LuaUIBridge.Call(_luaItem, "OnUpdateData", itemData.data, index);
            }
        }

        public override void CheckSelect(int index)
        {
            if (_luaItem != null)
            {
                LuaUIBridge.Call(_luaItem, "CheckSelect", index);
            }
        }

        private void OnDestroy()
        {
            if (_luaItem != null)
            {
                LuaUIBridge.Call(_luaItem, "OnRelease");
                _luaItem.Dispose();
                _luaItem = null;
            }
        }
    }
}
#endif
