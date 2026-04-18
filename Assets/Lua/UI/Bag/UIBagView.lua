---============================================================
--- UIBagView.lua - 背包界面 Lua 控制器
---
--- 由通用 UILuaView 通过 LuaUIBridge 转发生命周期
--- self.csView 指向 C# 的 UILuaView 实例
--- self 上还有 UIControlData 绑定过来的控件引用
---============================================================

local UIBagView = {}

function UIBagView:OnAddListener()
    if self.BtnClose then
        self.BtnClose.onClick:AddListener(function() self:OnClickClose() end)
    end
    if self.BtnSort then
        self.BtnSort.onClick:AddListener(function() self:OnClickSort() end)
    end

    -- 监听背包变化事件
    EventBus:On("BagChanged", self.RefreshBag, self)
    EventBus:On("GoldChanged", self.RefreshGold, self)
    EventBus:On("BagItemClicked", self.OnClickItem, self)
end

function UIBagView:OnRemoveListener()
    if self.BtnClose then self.BtnClose.onClick:RemoveAllListeners() end
    if self.BtnSort then self.BtnSort.onClick:RemoveAllListeners() end
    EventBus:OffAll(self)
end

function UIBagView:OnOpen(userData)
    print("[UIBagView] OnOpen")
    self:RefreshBag()
    self:RefreshGold()
end

function UIBagView:RefreshBag()
    local itemList = BagSystem:GetItemList()
    -- 通过通用 UILuaView 的 UpdateScrollView 刷新列表
    -- 直接传 Lua table 数组，C# 会包装成 LuaItemData 列表
    if self.csView and self.ScrollView and self.ItemPrefab then
        self.csView:UpdateScrollView(self.ScrollView, itemList, self.ItemPrefab)
    end

    if self.TxtCapacity then
        self.TxtCapacity.text = #itemList .. "/30"
    end
    print("[UIBagView] Bag refreshed, item count: " .. #itemList)
end

function UIBagView:RefreshGold()
    local gold = BagSystem:GetGold()
    if self.TxtGold then
        self.TxtGold.text = tostring(gold)
    end
end

function UIBagView:OnClickClose()
    local CS = CS
    CS.SkierFramework.UIManager.Instance:Close(CS.SkierFramework.UIType.UIBagView)
end

function UIBagView:OnClickSort()
    BagSystem:SortBag()
end

function UIBagView:OnClickItem(slotIndex, itemId)
    local cfg = ItemConfig.Get(itemId)
    if not cfg then return end

    if cfg.type == 1 then
        local ok, reason = BagSystem:UseItem(slotIndex)
        if not ok then
            print("[UIBagView] Cannot use item: " .. (reason or "unknown"))
        end
    elseif cfg.type == 2 then
        print("[UIBagView] Equip item: " .. cfg.name .. " (EquipSystem TODO)")
    end
end

function UIBagView:OnClose()
    print("[UIBagView] OnClose")
end

function UIBagView:OnRelease()
    print("[UIBagView] OnRelease")
end

return UIBagView
