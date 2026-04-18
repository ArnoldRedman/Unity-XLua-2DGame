---============================================================
--- UIDialogueView.lua - 对话界面 Lua 控制器
---
--- 预制体挂 UILuaView，luaModulePath = "UI.Dialogue.UIDialogueView"
--- UIControlData 绑定：
---   TxtSpeaker   : TextMeshProUGUI  - 说话者名字
---   TxtContent   : TextMeshProUGUI  - 对话内容
---   BtnNext      : Button           - 继续/关闭按钮
---   OptionRoot   : GameObject       - 选项容器（默认隐藏）
---   BtnOption1   : Button           - 选项 1
---   BtnOption2   : Button           - 选项 2
---   BtnOption3   : Button           - 选项 3
---   TxtOption1   : TextMeshProUGUI  - 选项 1 文字
---   TxtOption2   : TextMeshProUGUI  - 选项 2 文字
---   TxtOption3   : TextMeshProUGUI  - 选项 3 文字
---============================================================

local UIDialogueView = {}

-- 逐字显示相关
UIDialogueView._fullText = ""
UIDialogueView._displayedLen = 0
UIDialogueView._isTyping = false
UIDialogueView._typeSpeed = 0.03  -- 每个字的间隔（秒）
UIDialogueView._typeTimer = 0
UIDialogueView._hasOptions = false

function UIDialogueView:OnAddListener()
    if self.BtnNext then
        self.BtnNext.onClick:AddListener(function() self:OnClickNext() end)
    end
    if self.BtnOption1 then
        self.BtnOption1.onClick:AddListener(function() self:OnSelectOption(1) end)
    end
    if self.BtnOption2 then
        self.BtnOption2.onClick:AddListener(function() self:OnSelectOption(2) end)
    end
    if self.BtnOption3 then
        self.BtnOption3.onClick:AddListener(function() self:OnSelectOption(3) end)
    end

    EventBus:On("DialogueShow", self.OnDialogueShow, self)
end

function UIDialogueView:OnRemoveListener()
    if self.BtnNext then self.BtnNext.onClick:RemoveAllListeners() end
    if self.BtnOption1 then self.BtnOption1.onClick:RemoveAllListeners() end
    if self.BtnOption2 then self.BtnOption2.onClick:RemoveAllListeners() end
    if self.BtnOption3 then self.BtnOption3.onClick:RemoveAllListeners() end
    EventBus:OffAll(self)
end

function UIDialogueView:OnOpen(userData)
    -- 隐藏选项
    self:HideOptions()
end

--- 收到 DialogueShow 事件
function UIDialogueView:OnDialogueShow(data)
    if not data then return end

    -- 设置说话者
    if self.TxtSpeaker then
        self.TxtSpeaker.text = data.speaker or ""
    end

    -- 开始逐字显示
    self._fullText = data.content or ""
    self._displayedLen = 0
    self._isTyping = true
    self._typeTimer = 0
    self._hasOptions = (data.options ~= nil and #data.options > 0)

    if self.TxtContent then
        self.TxtContent.text = ""
    end

    -- 显示继续按钮，隐藏选项
    if self.BtnNext then
        self.BtnNext.gameObject:SetActive(not self._hasOptions)
    end
    self:HideOptions()

    -- 预存选项数据
    self._options = data.options

    -- 启动逐字显示协程（用 Update 驱动）
    self:StartTypewriter()
end

function UIDialogueView:StartTypewriter()
    -- 使用 C# MonoBehaviour 的 Update 来驱动逐字显示
    -- 通过 csView 上挂的 MonoBehaviour 来实现
    if self._typeCoroutine then
        -- 如果有正在进行的，先标记结束
    end

    -- 简化方案：直接用 Lua 协程模拟
    -- 实际会在 OnUpdate 中调用 UpdateTypewriter
    self._isTyping = true
end

--- 由外部每帧调用（需要在 UILuaView 中添加 Update 转发）
function UIDialogueView:OnUpdate()
    if not self._isTyping then return end

    self._typeTimer = self._typeTimer + CS.UnityEngine.Time.deltaTime

    while self._typeTimer >= self._typeSpeed and self._displayedLen < #self._fullText do
        self._typeTimer = self._typeTimer - self._typeSpeed
        self._displayedLen = self._displayedLen + 1

        -- UTF-8 中文处理：找到下一个完整字符的边界
        local byte = string.byte(self._fullText, self._displayedLen)
        if byte and byte >= 0xC0 then
            -- 多字节 UTF-8 字符
            if byte >= 0xF0 then
                self._displayedLen = self._displayedLen + 3
            elseif byte >= 0xE0 then
                self._displayedLen = self._displayedLen + 2
            elseif byte >= 0xC0 then
                self._displayedLen = self._displayedLen + 1
            end
        end
    end

    if self._displayedLen >= #self._fullText then
        self._displayedLen = #self._fullText
        self._isTyping = false
        self:OnTypewriterComplete()
    end

    if self.TxtContent then
        self.TxtContent.text = string.sub(self._fullText, 1, self._displayedLen)
    end
end

--- 逐字显示完毕
function UIDialogueView:OnTypewriterComplete()
    if self._hasOptions and self._options then
        -- 显示选项
        self:ShowOptions(self._options)
        if self.BtnNext then
            self.BtnNext.gameObject:SetActive(false)
        end
    else
        -- 显示继续按钮
        if self.BtnNext then
            self.BtnNext.gameObject:SetActive(true)
        end
    end
end

function UIDialogueView:ShowOptions(options)
    if self.OptionRoot then
        self.OptionRoot:SetActive(true)
    end

    local btns = { self.BtnOption1, self.BtnOption2, self.BtnOption3 }
    local txts = { self.TxtOption1, self.TxtOption2, self.TxtOption3 }

    for i = 1, 3 do
        if btns[i] then
            if options[i] then
                btns[i].gameObject:SetActive(true)
                if txts[i] then
                    txts[i].text = options[i].text
                end
            else
                btns[i].gameObject:SetActive(false)
            end
        end
    end
end

function UIDialogueView:HideOptions()
    if self.OptionRoot then
        self.OptionRoot:SetActive(false)
    end
end

function UIDialogueView:OnClickNext()
    if self._isTyping then
        -- 正在逐字显示 → 直接显示全部
        self._displayedLen = #self._fullText
        self._isTyping = false
        if self.TxtContent then
            self.TxtContent.text = self._fullText
        end
        self:OnTypewriterComplete()
    else
        -- 已经显示完毕 → 推进到下一句
        DialogueSystem:Next()
    end
end

function UIDialogueView:OnSelectOption(index)
    DialogueSystem:SelectOption(index)
end

function UIDialogueView:OnClose()
    self._isTyping = false
end

function UIDialogueView:OnRelease()
end

return UIDialogueView
