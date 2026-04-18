---============================================================
--- QuestSystem.lua - 简易任务状态系统
---
--- 管理任务进度，NPC 根据任务状态切换对话
---============================================================

local QuestSystem = {}

-- 任务状态: "none" / "active" / "completed"
QuestSystem._quests = {}

--- 获取任务状态
function QuestSystem:GetState(questId)
    return self._quests[questId] or "none"
end

--- 设置任务状态
function QuestSystem:SetState(questId, state)
    self._quests[questId] = state
    print("[QuestSystem] Quest " .. questId .. " -> " .. state)
    EventBus:Emit("QuestStateChanged", questId, state)
end

--- 接受任务
function QuestSystem:Accept(questId)
    self:SetState(questId, "active")
end

--- 完成任务
function QuestSystem:Complete(questId)
    self:SetState(questId, "completed")
end

--- 判断任务是否处于某状态
function QuestSystem:Is(questId, state)
    return self:GetState(questId) == state
end

_G.QuestSystem = QuestSystem
return QuestSystem
