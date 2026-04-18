---============================================================
--- UIBagItem.lua - 背包物品格子 Lua 控制器
---
--- 由通用 UILuaItem 转发生命周期
--- self.csItem 指向 C# 的 UILuaItem 实例
--- OnUpdateData 接收的 data 是一个 Lua table（来自 BagSystem:GetItemList）
---============================================================

local UIBagItem = {}

function UIBagItem:OnInit()
    print("[UIBagItem] OnInit")
end

function UIBagItem:OnAddListener()
    if self.BtnItem then
        self.BtnItem.onClick:AddListener(function() self:OnClick() end)
    end
end

function UIBagItem:OnUpdateData(data, index)
    if not data then return end

    self.slotIndex = data.slotIndex
    self.itemId = data.itemId

    if self.TxtName then
        self.TxtName.text = data.name or ""
    end
    if self.TxtCount then
        local countStr = ""
        if data.count and data.count > 1 then
            countStr = tostring(data.count)
        end
        self.TxtCount.text = countStr
    end
    -- 品质颜色
    if self.ImgQuality and data.quality then
        self.ImgQuality.color = self:GetQualityColor(data.quality)
    end
end

function UIBagItem:OnClick()
    if self.slotIndex and self.itemId then
        EventBus:Emit("BagItemClicked", self.slotIndex, self.itemId)
    end
end

function UIBagItem:GetQualityColor(quality)
    local CS = CS
    local Color = CS.UnityEngine.Color
    if quality == 1 then return Color.white
    elseif quality == 2 then return Color.green
    elseif quality == 3 then return CS.UnityEngine.Color(0.3, 0.5, 1, 1)
    elseif quality == 4 then return CS.UnityEngine.Color(0.7, 0.3, 0.9, 1)
    elseif quality == 5 then return CS.UnityEngine.Color(1, 0.6, 0, 1)
    else return Color.gray end
end

function UIBagItem:OnRelease()
    if self.BtnItem then
        self.BtnItem.onClick:RemoveAllListeners()
    end
end

return UIBagItem
