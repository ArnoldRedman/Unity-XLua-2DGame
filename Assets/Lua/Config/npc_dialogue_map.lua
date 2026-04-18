---============================================================
--- npc_dialogue_map.lua - NPC 动态对话映射
---
--- 根据任务状态返回 NPC 当前应使用的对话 ID
--- NpcDialogueMap[npcId] = function() return dialogueId end
---============================================================

_G.NpcDialogueMap = {}

-- NPC 1: 村长
NpcDialogueMap[1] = function()
    if QuestSystem:Is(1001, "completed") then
        return 20   -- 任务完成后的对话
    elseif QuestSystem:Is(1001, "active") then
        return 6    -- 任务进行中的对话
    else
        return 1    -- 初始对话（给任务）
    end
end

-- NPC 2: 商人
NpcDialogueMap[2] = function()
    return 10  -- 商人始终同一段对话
end
