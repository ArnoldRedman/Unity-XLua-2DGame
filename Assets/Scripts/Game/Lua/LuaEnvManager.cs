#if XLUA
using System;
using System.IO;
using UnityEngine;
using XLua;

namespace SkierFramework
{
    /// <summary>
    /// Lua环境管理器：创建/销毁LuaEnv，注册自定义Loader，每帧Tick
    /// </summary>
    public class LuaEnvManager : SingletonMono<LuaEnvManager>
    {
        private LuaEnv _luaEnv;
        private LuaTable _globalTable;

        public LuaEnv LuaEnv => _luaEnv;
        public LuaTable Global => _globalTable;

        /// <summary>
        /// 初始化Lua环境
        /// </summary>
        public void Initialize()
        {
            if (_luaEnv != null) return;

            _luaEnv = new LuaEnv();
            _globalTable = _luaEnv.Global;

            // 注册自定义Loader：编辑器下从文件系统读取，打包后从Addressables读取
            _luaEnv.AddLoader(CustomLoader);

            Debug.Log("[LuaEnvManager] Lua environment initialized.");
        }

        /// <summary>
        /// 执行Lua入口脚本
        /// </summary>
        public void StartMain()
        {
            DoFile("Main");
        }

        /// <summary>
        /// 执行一段Lua代码
        /// </summary>
        public object[] DoString(string luaCode, string chunkName = "chunk")
        {
            if (_luaEnv == null)
            {
                Debug.LogError("[LuaEnvManager] LuaEnv not initialized.");
                return null;
            }
            return _luaEnv.DoString(luaCode, chunkName);
        }

        /// <summary>
        /// 通过require执行一个Lua模块
        /// </summary>
        public object[] DoFile(string moduleName)
        {
            return DoString($"return require('{moduleName}')", moduleName);
        }

        /// <summary>
        /// 创建一个新的LuaTable用作模块实例
        /// </summary>
        public LuaTable NewTable()
        {
            LuaTable table = _luaEnv.NewTable();

            // 设置元表，让实例可以访问全局表
            using (LuaTable meta = _luaEnv.NewTable())
            {
                meta.Set("__index", _globalTable);
                table.SetMetaTable(meta);
            }

            return table;
        }

        private void Update()
        {
            if (_luaEnv != null)
            {
                _luaEnv.Tick();
            }
        }

        /// <summary>
        /// 自定义Lua脚本加载器
        /// 查找顺序：热更目录 → Assets/Lua 目录
        /// </summary>
        private byte[] CustomLoader(ref string modulePath)
        {
            // 将模块路径中的 . 替换为 /
            string filePath = modulePath.Replace('.', '/');

            // 1. 优先从热更目录加载（PersistentDataPath）
            string hotfixPath = Path.Combine(Application.persistentDataPath, "Lua", filePath + ".lua");
            if (File.Exists(hotfixPath))
            {
                return File.ReadAllBytes(hotfixPath);
            }

#if UNITY_EDITOR
            // 2. 编辑器下直接从Assets/Lua目录读取
            string editorPath = Path.Combine(Application.dataPath, "Lua", filePath + ".lua");
            if (File.Exists(editorPath))
            {
                return File.ReadAllBytes(editorPath);
            }
#endif

            // 3. 从Addressables加载（同步方式，用于require）
            // 打包后Lua文件应通过Addressables以TextAsset形式加载
            string addressablePath = $"Assets/Lua/{filePath}.lua";
            try
            {
                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<TextAsset>(addressablePath);
                var textAsset = handle.WaitForCompletion();
                if (textAsset != null)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(textAsset.text);
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                    return bytes;
                }
            }
            catch
            {
                // Addressables加载失败，忽略
            }

            return null;
        }

        public void Dispose()
        {
            if (_luaEnv != null)
            {
                _luaEnv.Dispose();
                _luaEnv = null;
                _globalTable = null;
            }
        }

        public override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }
    }
}
#endif
