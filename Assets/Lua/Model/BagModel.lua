---============================================================
--- BagModel.lua - 背包数据模型
---
--- 职责：只存数据，不包含业务逻辑
--- 所有修改都必须通过 BagSystem 进行
---============================================================

require("Config.item_config")

local BagModel = {}

--- 背包格子列表，每个元素: { itemId = number, count = number }
--- nil 或空表示空格子
BagModel.slots = {}

--- 背包容量
BagModel.capacity = 30

--- 金币
BagModel.gold = 0

--------------------------------------------------------------
-- 内部数据操作（仅供 BagSystem 调用）
--------------------------------------------------------------

--- 获取指定位置的格子数据
function BagModel:GetSlot(slotIndex)
    return self.slots[slotIndex]
end

--- 设置指定位置的格子数据
function BagModel:SetSlot(slotIndex, itemId, count)
    if count <= 0 then
        self.slots[slotIndex] = nil
    else
        self.slots[slotIndex] = { itemId = itemId, count = count }
    end
end

--- 清空指定格子
function BagModel:ClearSlot(slotIndex)
    self.slots[slotIndex] = nil
end

--- 找到第一个空格子的索引，没有则返回nil
function BagModel:FindEmptySlot()
    for i = 1, self.capacity do
        if not self.slots[i] then
            return i
        end
    end
    return nil
end

--- 找到包含指定物品且未满的格子索引列表
function BagModel:FindItemSlots(itemId)
    local result = {}
    for i = 1, self.capacity do
        local slot = self.slots[i]
        if slot and slot.itemId == itemId then
            table.insert(result, i)
        end
    end
    return result
end

--- 统计某个物品的总数量
function BagModel:GetItemCount(itemId)
    local total = 0
    for i = 1, self.capacity do
        local slot = self.slots[i]
        if slot and slot.itemId == itemId then
            total = total + slot.count
        end
    end
    return total
end

--- 获取当前已使用的格子数
function BagModel:GetUsedSlotCount()
    local count = 0
    for i = 1, self.capacity do
        if self.slots[i] then
            count = count + 1
        end
    end
    return count
end

--- 获取背包快照（用于UI显示）
function BagModel:GetSnapshot()
    local list = {}
    for i = 1, self.capacity do
        local slot = self.slots[i]
        if slot then
            local cfg = ItemConfig.Get(slot.itemId)
            table.insert(list, {
                slotIndex = i,
                itemId = slot.itemId,
                count = slot.count,
                name = cfg and cfg.name or "???",
                icon = cfg and cfg.icon or "",
                quality = cfg and cfg.quality or 1,
                type = cfg and cfg.type or 0,
            })
        end
    end
    return list
end

--- 重置背包
function BagModel:Reset()
    self.slots = {}
    self.gold = 0
end

--- 导出存档数据
function BagModel:ExportData()
    return {
        slots = self.slots,
        capacity = self.capacity,
        gold = self.gold,
    }
end

--- 导入存档数据
function BagModel:ImportData(data)
    if not data then return end
    self.slots = data.slots or {}
    self.capacity = data.capacity or 30
    self.gold = data.gold or 0
end

_G.BagModel = BagModel

return BagModel
