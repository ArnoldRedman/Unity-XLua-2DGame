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
require("System.QuestSystem")

-- 加载 NPC 对话映射
require("Config.npc_dialogue_map")

print("[Lua] All modules loaded, Lua ready.")

-- 注册对话事件：接受任务时发放道具
EventBus:On("AcceptQuest", function(args)
    if args and args.questId == 1001 then
        QuestSystem:Accept(1001)
        BagSystem:AddItem(1002, 3)   -- 3个中型生命药水
        BagSystem:AddItem(2001, 1)   -- 铁剑
        BagSystem:AddGold(200)       -- 200金币
        print("[Main] Quest 1001 accepted! Received supplies.")
    end
end)
