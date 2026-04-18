---============================================================
--- dialogue_config.lua - 对话配置表
---
--- 每条对话数据:
---   id          : 对话 ID
---   speaker     : 说话者名字（nil 表示使用 NPC 名字）
---   content     : 对话内容
---   nextId      : 下一条对话 ID（nil 表示结束）
---   options     : 分支选项列表（可选）
---     { text = "选项文字", nextId = 跳转ID }
---   triggerEvent: 触发的事件名（可选）
---   triggerArgs : 事件参数（可选）
---============================================================

local DialogueConfig = {}

local data = {
    -- ===== NPC 老村长的对话 =====
    [1] = {
        id = 1,
        speaker = nil, -- 使用 NPC 名字
        content = "你好，年轻的冒险者！欢迎来到这个地牢。",
        nextId = 2,
    },
    [2] = {
        id = 2,
        speaker = nil,
        content = "这里曾经是一个繁荣的地下城市，但现在到处都是怪物...",
        nextId = 3,
    },
    [3] = {
        id = 3,
        speaker = nil,
        content = "你愿意帮助我们清除这些怪物吗？",
        options = {
            { text = "当然，交给我！", nextId = 4 },
            { text = "让我再想想...", nextId = 5 },
        },
    },
    [4] = {
        id = 4,
        speaker = nil,
        content = "太好了！先去消灭地牢里的 3 只史莱姆吧。这是一些补给品，拿好了。",
        nextId = nil,
        triggerEvent = "AcceptQuest",
        triggerArgs = { questId = 1001 },
    },
    [5] = {
        id = 5,
        speaker = nil,
        content = "没关系，准备好了再来找我。",
        nextId = nil,
    },

    -- ===== 商人对话 =====
    [10] = {
        id = 10,
        speaker = "商人",
        content = "嘿，冒险者！要看看我的货物吗？",
        options = {
            { text = "打开商店", nextId = 11 },
            { text = "不了，谢谢", nextId = 12 },
        },
    },
    [11] = {
        id = 11,
        speaker = "商人",
        content = "欢迎光临！慢慢挑选。",
        nextId = nil,
        triggerEvent = "OpenShop",
    },
    [12] = {
        id = 12,
        speaker = "商人",
        content = "下次再来啊！",
        nextId = nil,
    },

    -- ===== 任务完成后的对话 =====
    [20] = {
        id = 20,
        speaker = nil,
        content = "做得好！你果然是个勇敢的冒险者。这是你应得的奖赏！",
        nextId = nil,
        triggerEvent = "SubmitQuest",
        triggerArgs = { questId = 1001 },
    },
}

function DialogueConfig.Get(id)
    return data[id]
end

_G.DialogueConfig = DialogueConfig
return DialogueConfig
