---============================================================
--- DialogueSystem.lua - 对话系统
---
--- 管理对话状态机：开始对话 → 逐句推进 → 分支选项 → 结束
--- 通过 EventBus 与 UI 通信
---============================================================

local DialogueSystem = {}

-- 当前对话状态
DialogueSystem.currentId = nil        -- 当前对话节点 ID
DialogueSystem.currentNpcName = nil   -- 当前 NPC 名字
DialogueSystem.isActive = false       -- 是否正在对话中

--- 开始一段对话
--- @param dialogueId number 起始对话 ID
--- @param npcName string NPC 名字
function DialogueSystem:StartDialogue(dialogueId, npcName)
    local cfg = DialogueConfig.Get(dialogueId)
    if not cfg then
        print("[DialogueSystem] Dialogue not found: " .. tostring(dialogueId))
        return
    end

    self.currentId = dialogueId
    self.currentNpcName = npcName or "???"
    self.isActive = true

    -- 打开对话 UI
    local CS = CS
    CS.SkierFramework.UIManager.Instance:Open(CS.SkierFramework.UIType.UIDialogueView)

    -- 显示第一句
    self:ShowCurrent()
end

--- 显示当前对话节点
function DialogueSystem:ShowCurrent()
    local cfg = DialogueConfig.Get(self.currentId)
    if not cfg then
        self:EndDialogue()
        return
    end

    local speaker = cfg.speaker or self.currentNpcName

    -- 通知 UI 显示内容
    EventBus:Emit("DialogueShow", {
        speaker = speaker,
        content = cfg.content,
        options = cfg.options, -- nil 或 table
    })
end

--- 推进到下一句（玩家点击继续）
function DialogueSystem:Next()
    if not self.isActive then return end

    local cfg = DialogueConfig.Get(self.currentId)
    if not cfg then
        self:EndDialogue()
        return
    end

    -- 如果有选项，不能直接 Next（需要通过 SelectOption）
    if cfg.options and #cfg.options > 0 then
        return
    end

    -- 触发事件（如果有）
    if cfg.triggerEvent then
        EventBus:Emit(cfg.triggerEvent, cfg.triggerArgs)
    end

    -- 跳到下一句
    if cfg.nextId then
        self.currentId = cfg.nextId
        self:ShowCurrent()
    else
        self:EndDialogue()
    end
end

--- 选择分支选项
--- @param optionIndex number 选项索引（从 1 开始）
function DialogueSystem:SelectOption(optionIndex)
    if not self.isActive then return end

    local cfg = DialogueConfig.Get(self.currentId)
    if not cfg or not cfg.options then return end

    local option = cfg.options[optionIndex]
    if not option then return end

    -- 触发当前节点的事件
    if cfg.triggerEvent then
        EventBus:Emit(cfg.triggerEvent, cfg.triggerArgs)
    end

    -- 跳转到选项指定的下一句
    if option.nextId then
        self.currentId = option.nextId
        self:ShowCurrent()
    else
        self:EndDialogue()
    end
end

--- 结束对话
function DialogueSystem:EndDialogue()
    self.isActive = false
    self.currentId = nil
    self.currentNpcName = nil

    -- 关闭对话 UI
    local CS = CS
    CS.SkierFramework.UIManager.Instance:Close(CS.SkierFramework.UIType.UIDialogueView)

    -- 通知 NPC 对话结束
    EventBus:Emit("DialogueEnd")
end

_G.DialogueSystem = DialogueSystem
return DialogueSystem
