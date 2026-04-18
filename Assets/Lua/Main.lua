---============================================================
--- Main.lua - Lua入口脚本
--- 由 LuaEnvManager.StartMain() 调用
---============================================================

print("[Lua] Main.lua loaded")

-- 加载基础模块
require("Common.Class")
require("Common.EventBus")

-- 加载配置
require("Config.item_config")
require("Config.dialogue_config")

-- 加载业务系统
require("System.BagSystem")
require("System.DialogueSystem")

print("[Lua] All modules loaded, Lua ready.")
