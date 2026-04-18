---============================================================
--- BagSystem.lua - 背包系统
---
--- 职责：所有背包相关的业务规则
--- 原则：UI 不直接改 Model，必须经过 System
---============================================================

require("Model.BagModel")

local BagSystem = {}

--------------------------------------------------------------
-- 添加物品
--------------------------------------------------------------

--- 向背包添加物品
--- @param itemId number 物品ID
--- @param count number 数量
--- @return boolean 是否全部添加成功
--- @return number 剩余未能添加的数量
function BagSystem:AddItem(itemId, count)
    if not itemId or count <= 0 then
        return false, count
    end

    local cfg = ItemConfig.Get(itemId)
    if not cfg then
        print("[BagSystem] Item config not found: " .. tostring(itemId))
        return false, count
    end

    local remaining = count

    -- 可堆叠物品：先填充已有堆
    if cfg.maxStack > 1 then
        local existSlots = BagModel:FindItemSlots(itemId)
        for _, slotIndex in ipairs(existSlots) do
            if remaining <= 0 then break end
            local slot = BagModel:GetSlot(slotIndex)
            local canAdd = cfg.maxStack - slot.count
            if canAdd > 0 then
                local add = math.min(canAdd, remaining)
                BagModel:SetSlot(slotIndex, itemId, slot.count + add)
                remaining = remaining - add
            end
        end
    end

    -- 剩余的放入空格子
    while remaining > 0 do
        local emptySlot = BagModel:FindEmptySlot()
        if not emptySlot then
            print("[BagSystem] Bag is full! Remaining: " .. remaining)
            break
        end
        local add = math.min(cfg.maxStack, remaining)
        BagModel:SetSlot(emptySlot, itemId, add)
        remaining = remaining - add
    end

    if remaining < count then
        -- 有东西被添加了，发事件
        EventBus:Emit("BagChanged")
        EventBus:Emit("ItemAdded", itemId, count - remaining)
    end

    return remaining == 0, remaining
end

--------------------------------------------------------------
-- 移除物品
--------------------------------------------------------------

--- 从背包移除物品
--- @param itemId number 物品ID
--- @param count number 数量
--- @return boolean 是否成功移除了指定数量
function BagSystem:RemoveItem(itemId, count)
    if not itemId or count <= 0 then
        return false
    end

    local total = BagModel:GetItemCount(itemId)
    if total < count then
        print("[BagSystem] Not enough items: have " .. total .. ", need " .. count)
        return false
    end

    local remaining = count
    local slots = BagModel:FindItemSlots(itemId)

    -- 从后往前移除，优先清空数量少的格子
    for i = #slots, 1, -1 do
        if remaining <= 0 then break end
        local slotIndex = slots[i]
        local slot = BagModel:GetSlot(slotIndex)
        if slot.count <= remaining then
            remaining = remaining - slot.count
            BagModel:ClearSlot(slotIndex)
        else
            BagModel:SetSlot(slotIndex, itemId, slot.count - remaining)
            remaining = 0
        end
    end

    EventBus:Emit("BagChanged")
    EventBus:Emit("ItemRemoved", itemId, count)
    return true
end

--- 移除指定格子中的物品
--- @param slotIndex number 格子索引
--- @param count number 数量（nil则移除该格全部）
--- @return boolean 是否成功
function BagSystem:RemoveItemBySlot(slotIndex, count)
    local slot = BagModel:GetSlot(slotIndex)
    if not slot then return false end

    count = count or slot.count
    if slot.count <= count then
        BagModel:ClearSlot(slotIndex)
    else
        BagModel:SetSlot(slotIndex, slot.itemId, slot.count - count)
    end

    EventBus:Emit("BagChanged")
    return true
end

--------------------------------------------------------------
-- 使用物品
--------------------------------------------------------------

--- 使用物品
--- @param slotIndex number 格子索引
--- @return boolean 是否使用成功
--- @return string|nil 失败原因
function BagSystem:UseItem(slotIndex)
    local slot = BagModel:GetSlot(slotIndex)
    if not slot then
        return false, "empty slot"
    end

    local cfg = ItemConfig.Get(slot.itemId)
    if not cfg then
        return false, "config missing"
    end

    if cfg.useEffect == 0 then
        return false, "item not usable"
    end

    -- 执行使用效果
    if cfg.useEffect == 1 then
        -- 回血
        EventBus:Emit("UseItemEffect", "hp", cfg.useValue)
        print("[BagSystem] Use item: " .. cfg.name .. ", restore HP " .. cfg.useValue)
    elseif cfg.useEffect == 2 then
        -- 回蓝
        EventBus:Emit("UseItemEffect", "mp", cfg.useValue)
        print("[BagSystem] Use item: " .. cfg.name .. ", restore MP " .. cfg.useValue)
    end

    -- 消耗1个
    self:RemoveItemBySlot(slotIndex, 1)

    EventBus:Emit("ItemUsed", slot.itemId)
    return true
end

--------------------------------------------------------------
-- 查询
--------------------------------------------------------------

--- 背包是否已满
function BagSystem:IsFull()
    return BagModel:GetUsedSlotCount() >= BagModel.capacity
end

--- 检查是否拥有指定数量的物品
function BagSystem:HasItem(itemId, count)
    count = count or 1
    return BagModel:GetItemCount(itemId) >= count
end

--- 获取背包快照供UI显示
function BagSystem:GetItemList()
    return BagModel:GetSnapshot()
end

--- 获取金币数
function BagSystem:GetGold()
    return BagModel.gold
end

--------------------------------------------------------------
-- 金币
--------------------------------------------------------------

--- 增加金币
function BagSystem:AddGold(amount)
    if amount <= 0 then return end
    BagModel.gold = BagModel.gold + amount
    EventBus:Emit("GoldChanged", BagModel.gold)
end

--- 消耗金币
function BagSystem:SpendGold(amount)
    if amount <= 0 then return false end
    if BagModel.gold < amount then
        return false
    end
    BagModel.gold = BagModel.gold - amount
    EventBus:Emit("GoldChanged", BagModel.gold)
    return true
end

--------------------------------------------------------------
-- 出售物品
--------------------------------------------------------------

--- 出售背包中指定格子的物品
--- @param slotIndex number 格子索引
--- @param count number 出售数量（nil则全部出售）
function BagSystem:SellItem(slotIndex, count)
    local slot = BagModel:GetSlot(slotIndex)
    if not slot then return false end

    local cfg = ItemConfig.Get(slot.itemId)
    if not cfg or cfg.sellPrice <= 0 then
        return false
    end

    count = count or slot.count
    count = math.min(count, slot.count)

    local gold = cfg.sellPrice * count
    self:RemoveItemBySlot(slotIndex, count)
    self:AddGold(gold)

    print("[BagSystem] Sold " .. count .. "x " .. cfg.name .. " for " .. gold .. " gold")
    return true
end

--------------------------------------------------------------
-- 整理
--------------------------------------------------------------

--- 整理背包：同类物品合并堆叠，空格子移到后面
function BagSystem:SortBag()
    -- 收集所有物品
    local items = {} -- { [itemId] = totalCount }
    for i = 1, BagModel.capacity do
        local slot = BagModel:GetSlot(i)
        if slot then
            items[slot.itemId] = (items[slot.itemId] or 0) + slot.count
            BagModel:ClearSlot(i)
        end
    end

    -- 按ID排序后重新放入
    local sortedIds = {}
    for itemId, _ in pairs(items) do
        table.insert(sortedIds, itemId)
    end
    table.sort(sortedIds)

    local slotIndex = 1
    for _, itemId in ipairs(sortedIds) do
        local cfg = ItemConfig.Get(itemId)
        local remaining = items[itemId]
        local maxStack = cfg and cfg.maxStack or 1
        while remaining > 0 and slotIndex <= BagModel.capacity do
            local add = math.min(maxStack, remaining)
            BagModel:SetSlot(slotIndex, itemId, add)
            remaining = remaining - add
            slotIndex = slotIndex + 1
        end
    end

    EventBus:Emit("BagChanged")
    print("[BagSystem] Bag sorted")
end

--------------------------------------------------------------
-- 存档
--------------------------------------------------------------

function BagSystem:ExportData()
    return BagModel:ExportData()
end

function BagSystem:ImportData(data)
    BagModel:ImportData(data)
    EventBus:Emit("BagChanged")
end

--------------------------------------------------------------
-- 初始化
--------------------------------------------------------------

--- 初始化背包（加入一些测试道具）
function BagSystem:Init()
    BagModel:Reset()
    -- 初始只给少量基础道具
    self:AddItem(1001, 3)   -- 3个小型生命药水
    self:AddGold(100)       -- 100金币
    print("[BagSystem] Initialized with starter items")
end

_G.BagSystem = BagSystem

-- 自动初始化
BagSystem:Init()

return BagSystem
