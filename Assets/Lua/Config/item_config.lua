---============================================================
--- item_config.lua - 物品配置表
---
--- 字段说明:
---   id          : 物品ID
---   name        : 物品名称
---   type        : 物品类型 (1=消耗品, 2=装备, 3=材料, 4=任务道具)
---   quality     : 品质 (1=白, 2=绿, 3=蓝, 4=紫, 5=橙)
---   icon        : 图标资源名
---   maxStack    : 最大堆叠数 (装备类=1)
---   desc        : 描述
---   useEffect   : 使用效果类型 (0=不可使用, 1=回血, 2=回蓝)
---   useValue    : 使用效果数值
---   sellPrice   : 出售价格
---============================================================

local ItemConfig = {
    [1001] = {
        id = 1001,
        name = "小型生命药水",
        type = 1,
        quality = 1,
        icon = "icon_potion_hp_s",
        maxStack = 99,
        desc = "恢复100点生命值",
        useEffect = 1,
        useValue = 100,
        sellPrice = 10,
    },
    [1002] = {
        id = 1002,
        name = "中型生命药水",
        type = 1,
        quality = 2,
        icon = "icon_potion_hp_m",
        maxStack = 99,
        desc = "恢复300点生命值",
        useEffect = 1,
        useValue = 300,
        sellPrice = 30,
    },
    [1003] = {
        id = 1003,
        name = "小型魔力药水",
        type = 1,
        quality = 1,
        icon = "icon_potion_mp_s",
        maxStack = 99,
        desc = "恢复50点魔力值",
        useEffect = 2,
        useValue = 50,
        sellPrice = 15,
    },
    [2001] = {
        id = 2001,
        name = "铁剑",
        type = 2,
        quality = 2,
        icon = "icon_weapon_sword_iron",
        maxStack = 1,
        desc = "一把普通的铁剑，攻击力+15",
        useEffect = 0,
        useValue = 0,
        sellPrice = 100,
    },
    [2002] = {
        id = 2002,
        name = "精钢剑",
        type = 2,
        quality = 3,
        icon = "icon_weapon_sword_steel",
        maxStack = 1,
        desc = "精工打造的钢剑，攻击力+30",
        useEffect = 0,
        useValue = 0,
        sellPrice = 300,
    },
    [2003] = {
        id = 2003,
        name = "皮甲",
        type = 2,
        quality = 1,
        icon = "icon_armor_leather",
        maxStack = 1,
        desc = "轻便的皮甲，防御力+10",
        useEffect = 0,
        useValue = 0,
        sellPrice = 80,
    },
    [3001] = {
        id = 3001,
        name = "铁矿石",
        type = 3,
        quality = 1,
        icon = "icon_mat_iron_ore",
        maxStack = 999,
        desc = "常见的铁矿石，可用于锻造",
        useEffect = 0,
        useValue = 0,
        sellPrice = 5,
    },
    [3002] = {
        id = 3002,
        name = "兽皮",
        type = 3,
        quality = 1,
        icon = "icon_mat_leather",
        maxStack = 999,
        desc = "野兽的毛皮，可用于制作护甲",
        useEffect = 0,
        useValue = 0,
        sellPrice = 8,
    },
    [4001] = {
        id = 4001,
        name = "村长的信件",
        type = 4,
        quality = 3,
        icon = "icon_quest_letter",
        maxStack = 1,
        desc = "村长写给城里铁匠的一封信",
        useEffect = 0,
        useValue = 0,
        sellPrice = 0,
    },
    [4002] = {
        id = 4002,
        name = "狼牙",
        type = 4,
        quality = 2,
        icon = "icon_quest_wolf_fang",
        maxStack = 99,
        desc = "从野狼身上获取的牙齿，某些人会需要",
        useEffect = 0,
        useValue = 0,
        sellPrice = 0,
    },
}

--- 获取物品配置
function ItemConfig.Get(itemId)
    return ItemConfig[itemId]
end

_G.ItemConfig = ItemConfig

return ItemConfig
