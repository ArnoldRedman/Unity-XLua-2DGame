# Unity + XLua 2D RPG 实战指南

这份文档不是泛泛介绍 XLua，而是基于你当前这个项目来讲，目标是让你知道商业 Unity 项目里 Lua 一般怎么落地，以及你要做一个支持热更的 2D RPG 小游戏时，应该按什么顺序做。

当前仓库现状：

- XLua 已经接入，代码和原生插件都在工程里。
- UI 框架已经完整可用，核心入口是 [Assets/Scripts/Framework/UI/UIManager.cs](Assets/Scripts/Framework/UI/UIManager.cs)。
- UI 控件绑定已经给 Lua 预留了能力，关键文件是 [Assets/Scripts/Framework/UI/UIControlBinding/Scripts/UIControlData.cs](Assets/Scripts/Framework/UI/UIControlBinding/Scripts/UIControlData.cs)。
- 真正的 Lua 视图入口还没完成，[Assets/Scripts/Framework/UI/UIControlBinding/Scripts/LuaViewRunner.cs](Assets/Scripts/Framework/UI/UIControlBinding/Scripts/LuaViewRunner.cs) 里的 `BindLua` 还是空实现。

这意味着：你现在不是从零开始，而是已经具备了“UI 框架完成，Lua 底座半完成”的状态。最合理的做法不是一上来全项目 Lua 化，而是做一套 C# 底座 + Lua 业务层 的混合架构。

## 1. 商业项目里 Lua 一般怎么分工

商业 Unity 项目里，Lua 很少拿来替代整个引擎层。常见分工是：

- C# 负责稳定底层：资源加载、场景管理、UI 框架、战斗底层、网络、存档、SDK、性能敏感逻辑。
- Lua 负责高频业务：UI 控制、活动玩法、任务流程、剧情对话、配置驱动逻辑、引导、数值规则。
- 配置表负责静态数据：物品表、装备表、任务表、对话表、怪物表。

原因很直接：

- C# 适合做长期稳定、重性能、强类型的底层。
- Lua 适合快速迭代、热更新、频繁改规则的业务层。

如果你要做一个 2D RPG 小游戏，推荐你这样分：

- C#：框架层、UI 容器层、资源层、存档层、地图加载层、角色表现层。
- Lua：背包系统、装备系统、任务系统、对话系统、主流程状态机、UI 页面逻辑。

这就是最常见、也最稳的做法。

## 2. 你这个项目最适合的架构

我建议你直接采用四层结构：

1. Framework 层，现有 C# 框架继续保留。
2. Game Core 层，用 C# 提供游戏运行时能力。
3. Lua Logic 层，用 Lua 写业务模块和 UI 控制器。
4. Config Data 层，用表驱动所有 RPG 数据。

建议目录：

```text
Assets/
├── Scripts/
│   ├── Framework/                 # 现有框架，尽量少动
│   ├── Game/
│   │   ├── Entry/
│   │   │   ├── GameLauncher.cs
│   │   │   └── LuaGameEntry.cs
│   │   ├── Lua/
│   │   │   ├── LuaEnvManager.cs
│   │   │   ├── LuaLoader.cs
│   │   │   └── LuaUpdateDriver.cs
│   │   ├── Config/
│   │   │   ├── ConfigManager.cs
│   │   │   └── SaveManager.cs
│   │   ├── UI/
│   │   │   ├── LuaUIView.cs
│   │   │   └── LuaUIBridge.cs
│   │   ├── World/
│   │   │   ├── PlayerActor.cs
│   │   │   ├── NPCActor.cs
│   │   │   └── MapController.cs
│   │   └── Service/
│   │       ├── ItemService.cs
│   │       ├── QuestService.cs
│   │       └── DialogueService.cs
│   └── Application/
│       └── UIViews/               # 现有示例 UI，可逐步 Lua 化
└── Lua/
    ├── Main.lua
    ├── Boot/
    │   ├── GameBoot.lua
    │   └── ModuleLoader.lua
    ├── Common/
    │   ├── Class.lua
    │   ├── EventBus.lua
    │   └── TableUtil.lua
    ├── Config/
    │   ├── item_config.lua
    │   ├── equip_config.lua
    │   ├── quest_config.lua
    │   └── dialogue_config.lua
    ├── Model/
    │   ├── PlayerModel.lua
    │   ├── BagModel.lua
    │   ├── EquipModel.lua
    │   ├── QuestModel.lua
    │   └── DialogueModel.lua
    ├── System/
    │   ├── BagSystem.lua
    │   ├── EquipSystem.lua
    │   ├── QuestSystem.lua
    │   └── DialogueSystem.lua
    ├── UI/
    │   ├── Login/
    │   │   └── UILoginView.lua
    │   ├── Bag/
    │   │   ├── UIBagView.lua
    │   │   └── UIBagItem.lua
    │   ├── Equip/
    │   │   └── UIEquipView.lua
    │   ├── Quest/
    │   │   └── UIQuestView.lua
    │   └── Dialogue/
    │       └── UIDialogueView.lua
    └── Flow/
        ├── GameFlow.lua
        └── SceneFlow.lua
```

这个目录的核心思想只有一句话：

Lua 负责“规则和流程”，C# 负责“宿主和能力”。

## 3. 第一阶段不要全 Lua UI，先用“薄壳 UIView + Lua 控制器”

你现在项目里最稳的接法，不是马上把所有 UI 全改成 LuaViewRunner，而是先走一层薄壳。

推荐做法：

- 每个 UI 预制体和 UIConfig 仍然按现在的 C# 方式接进 UIManager。
- 每个 UIView 只保留生命周期转发，不写业务。
- 真正的按钮逻辑、数据刷新、打开关闭行为放到对应的 Lua 文件里。

也就是：

- C# UIView = 容器
- Lua ViewController = 业务逻辑

比如背包界面：

```csharp
public class UIBagView : UIView
{
    [ControlBinding] private Button ButtonClose;
    [ControlBinding] private UIScrollView UIScrollView;
    [ControlBinding] private GameObject ItemPrefab;

    private object _luaView;

    public override void OnInit(UIControlData uIControlData, UIViewController controller)
    {
        base.OnInit(uIControlData, controller);
        _luaView = LuaUIBridge.Create("UI.Bag.UIBagView", this);
    }

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        LuaUIBridge.Call(_luaView, "OnOpen", userData);
    }

    public override void OnClose()
    {
        LuaUIBridge.Call(_luaView, "OnClose");
        base.OnClose();
    }

    public override void OnRelease()
    {
        LuaUIBridge.Call(_luaView, "OnRelease");
        _luaView = null;
        base.OnRelease();
    }
}
```

Lua 端：

```lua
local UIBagView = {}

function UIBagView:OnOpen(userData)
    self:RefreshBag()
end

function UIBagView:RefreshBag()
    local bagItems = BagSystem:GetItemList()
    self.csView.UIScrollView:UpdateList(bagItems, self.csView.ItemPrefab, typeof(CS.SkierFramework.UIBagItem))
end

function UIBagView:OnClose()
end

function UIBagView:OnRelease()
end

return UIBagView
```

这个方案的优点：

- 复用你现成的 UIManager、UIControlData、UIScrollView。
- UI 生命周期完全跟当前框架兼容。
- LuaViewRunner 没完成也不耽误开发。
- 之后你要进一步改成纯 Lua UI，也只是替换接入层，不用推倒业务。

这是最务实的路线。

## 4. 第二阶段再补 LuaViewRunner，做“纯 Lua UI”入口

项目里现在已经有两个关键点：

- [Assets/Scripts/Framework/UI/UIControlBinding/Scripts/UIControlData.cs](Assets/Scripts/Framework/UI/UIControlBinding/Scripts/UIControlData.cs) 已经支持把控件直接绑定到 LuaTable。
- [Assets/Scripts/Framework/UI/UIControlBinding/Scripts/LuaViewRunner.cs](Assets/Scripts/Framework/UI/UIControlBinding/Scripts/LuaViewRunner.cs) 还缺真正的绑定逻辑。

所以第二阶段的目标很明确：

1. 创建一个全局 LuaEnvManager。
2. 让 LuaViewRunner.BindLua 能加载指定 Lua 模块。
3. 用 UIControlData.BindDataToLua 把 Button、Text、SubView 等控件灌进 LuaTable。
4. 把 OnOpen、OnClose、OnResume 这些生命周期转发给 Lua。

建议 BindLua 的职责：

- 根据 `viewClassName` 执行 `require(viewClassName)`。
- 创建 view 实例 table。
- 注入 `gameObject`、`transform`、`csViewRunner`、`uiController`。
- 调用 `UIControlData.BindDataToLua(this, luaTable)`。
- 缓存 `OnInit`、`OnOpen`、`OnClose` 等 LuaFunction。

注意一个关键结论：

你不需要一开始就做“完全纯 Lua 项目”。先用薄壳方案把业务跑起来，再补 LuaViewRunner，会稳很多。

## 5. LuaEnv 应该怎么管理

商业项目里，LuaEnv 通常只有一个主环境，最多再加少量隔离环境。你这个项目建议只有一个。

建议结构：

- `LuaEnvManager`：负责创建和销毁 LuaEnv。
- `LuaLoader`：负责把 Lua 脚本从本地、Addressables 或热更目录加载出来。
- `LuaUpdateDriver`：每帧调用 `luaEnv.Tick()`。

基本原则：

- 游戏运行期间只保留一个 LuaEnv。
- 场景切换不要重建 LuaEnv，除非你要整包重启 Lua 逻辑。
- 每帧 Tick 一次，清理委托和对象引用。

最小启动流程：

1. 游戏启动。
2. C# 初始化 ResourceManager。
3. C# 初始化 UIManager。
4. C# 初始化 LuaEnvManager。
5. 执行 `Lua/Main.lua`。
6. Lua 里再启动 `GameBoot.lua`。
7. 进入登录、主城或地图流程。

## 6. 热更不是“神奇开关”，要先把资源发布方案定下来

很多人说“我要 Lua 热更”，实际第一步不是写 Lua，而是先决定 Lua 文件怎么发布。

你这个项目已经用了 Addressables，所以最适合的方案是：

1. Lua 脚本打成 TextAsset 或 ByteAsset。
2. 通过 Addressables 分组管理 Lua 资源。
3. 更新时先下载远端 Catalog 和对应 Lua 资源包。
4. LuaLoader 优先从热更目录或已下载资源中读取。
5. 读取失败时再回退到包内默认脚本。

推荐加载优先级：

1. PersistentDataPath 下的热更 Lua
2. Addressables 下载后的 Lua 资源
3. StreamingAssets 或内置默认 Lua

这才是商业项目真正稳定的热更链路。

如果你现在只是做个人项目，不要一上来做远端热更后台。第一版只做：

- 本地 Addressables 加载 Lua
- 支持替换 Lua 资源后重进游戏生效

等功能跑稳，再做远端增量更新。

## 7. 2D RPG 的核心系统怎么拆

你提到的四套系统，建议全部做成“配置 + Model + System + UI”结构。

统一原则：

- Config：静态定义。
- Model：玩家当前状态。
- System：业务规则。
- UI：展示和交互。

### 7.1 背包系统

#### 配置层

`item_config.lua`：

- id
- name
- type
- icon
- quality
- maxStack
- useEffectType
- useEffectValue
- sellPrice

#### 运行时数据

`BagModel.lua`：

- `slotList`
- `itemId -> count`
- `capacity`

#### 系统职责

`BagSystem.lua`：

- AddItem
- RemoveItem
- HasItem
- UseItem
- SortBag
- ExpandBag
- GetItemList

#### 关键规则

- 可堆叠物品先找已有堆，再找空格子。
- 不可堆叠装备类直接占一个格子。
- 所有变更都通过 System 入口，不允许 UI 直接改 Model。

Lua 伪代码：

```lua
function BagSystem:AddItem(itemId, count)
    local cfg = ItemConfig[itemId]
    if not cfg then
        return false, "item config missing"
    end

    if cfg.maxStack > 1 then
        count = self:_fillExistStack(itemId, count, cfg.maxStack)
    end

    count = self:_fillEmptySlot(itemId, count, cfg.maxStack)
    EventBus:Dispatch("BagChanged")
    return count == 0, count
end
```

### 7.2 装备系统

#### 配置层

`equip_config.lua`：

- equipId
- slotType
- part
- atk
- def
- hp
- quality
- canEquipJob
- levelRequire

#### 运行时数据

`EquipModel.lua`：

- 当前穿戴：weapon、helmet、body、ring 等
- 装备评分
- 套装激活状态

#### 系统职责

`EquipSystem.lua`：

- EquipItem
- UnEquipItem
- CheckCanEquip
- CalcFightValue
- GetEquippedList

#### 推荐规则

- 穿装备前先校验职业、等级、部位。
- 换装时，把旧装备退回背包。
- 战力、属性汇总只在 System 中计算，不在 UI 中拼。

### 7.3 任务系统

任务系统是 RPG 的主心骨，建议从第一天就按状态机来做。

#### 配置层

`quest_config.lua`：

- questId
- title
- desc
- questType
- targetId
- targetCount
- rewardList
- nextQuestId
- acceptNpcId
- submitNpcId

#### 运行时数据

`QuestModel.lua`：

- acceptedQuestMap
- finishedQuestMap
- progressMap

#### 系统职责

`QuestSystem.lua`：

- AcceptQuest
- UpdateProgress
- CanSubmitQuest
- SubmitQuest
- GetQuestList

#### 任务状态建议

- NotAccepted
- Accepted
- Completed
- Submitted

#### 关键原则

- 任何击杀、采集、对话触发，都不要直接改任务数据。
- 统一发事件给 QuestSystem，由 QuestSystem 判断哪些任务该推进。

### 7.4 对话系统

对话系统一定要配置化，不要把剧情写死在代码里。

#### 配置层

`dialogue_config.lua`：

- dialogueId
- speakerName
- content
- portrait
- nextId
- optionList
- triggerQuestId
- jumpCondition

#### 系统职责

`DialogueSystem.lua`：

- StartDialogue
- NextSentence
- SelectOption
- EndDialogue

#### 对话的商业项目常见需求

- 单句顺播
- 分支选项
- 条件跳转
- 对话触发任务
- 任务推进反过来改对话内容

所以你不要把对话只理解成“弹框显示文字”，它本质上是剧情状态机。

## 8. 数据流应该怎么走

你做 RPG 时，最容易写乱的地方是 UI 直接改数据。建议从一开始就定死下面这条链路：

```text
UI点击
-> Lua UI Controller
-> Lua System
-> Lua Model
-> EventBus
-> UI刷新
```

比如点击装备按钮：

```text
UIEquipView 点击“装备”
-> EquipSystem:EquipItem(itemUid)
-> EquipModel 更新穿戴槽
-> BagSystem 移除背包中的该装备
-> EventBus 派发 EquipChanged 和 BagChanged
-> UIEquipView / UIBagView 同时刷新
```

只要你遵守这条链路，项目就不容易失控。

## 9. 推荐先做事件总线，不要让模块互相直连

你项目里已经有 C# 事件系统 [Assets/Scripts/Framework/Other/EventController.cs](Assets/Scripts/Framework/Other/EventController.cs)，Lua 层也建议做一份轻量 EventBus。

Lua 模块之间不要直接互相改数据，尤其是：

- 对话系统直接改背包
- 背包系统直接改任务
- 装备系统直接改 UI

都不要这样写。

正确做法是：

- System 改自己的 Model。
- 发事件。
- 需要响应的模块自行监听。

## 10. UI 怎么和 Lua 交互最舒服

你现有 UI 框架已经很好用了，所以别为了“纯 Lua”把好用的东西全扔掉。

推荐你保留下面这些 C# 能力：

- [Assets/Scripts/Framework/UI/UIManager.cs](Assets/Scripts/Framework/UI/UIManager.cs) 继续负责 UI 打开关闭。
- [Assets/Scripts/Framework/UI/UIControlBinding/Scripts/UIControlData.cs](Assets/Scripts/Framework/UI/UIControlBinding/Scripts/UIControlData.cs) 继续负责控件绑定。
- [Assets/Scripts/Framework/UI/UIScrollView/UIScrollView.cs](Assets/Scripts/Framework/UI/UIScrollView/UIScrollView.cs) 继续负责大列表。

Lua 只负责：

- 页面打开时拉数据
- 组装显示内容
- 处理按钮点击
- 调系统接口

这比从 Lua 自己拼一整套 UI 框架高效得多。

## 11. XLua 配置建议

你工程里已经有 [Assets/XLua/Editor/ExampleConfig.cs](Assets/XLua/Editor/ExampleConfig.cs)，但现在基本还是示例状态。真正落项目时，至少要补三类配置：

1. LuaCallCSharp
2. CSharpCallLua
3. BlackList

建议你先只暴露必要类型，不要一把梭把整个 Assembly-CSharp 全丢给 Lua。

第一批建议开放：

- UnityEngine 基础类型
- UnityEngine.UI 常用控件
- TMPro
- 你自己的 UI 基类、UIManager、UIScrollView
- 你准备让 Lua 调用的 Game Service 和 Model API

原则很简单：

- 底层类尽量少暴露。
- 给 Lua 的 API 要稳定、收敛。
- 不要让 Lua 随便穿透到所有 C# 细节。

## 12. 存档一定要早做，不然系统会返工

你这几个 RPG 系统全都依赖持久化，所以 SaveManager 不要拖到最后。

建议保存内容：

- 玩家基础属性
- 背包内容
- 已穿装备
- 任务状态
- 对话节点状态
- 当前地图和坐标

建议格式：

- 第一版直接 JSON
- 后续再考虑压缩、加密、版本迁移

关键点不是“格式多高级”，而是：

- 每个系统都要有 `ExportData` / `ImportData`
- 存档版本号必须保留
- 配置表数据不要存，只存运行时状态

## 13. 你现在最该做的开发顺序（以最快看到可操控角色为目标）

如果目标是尽快做出一个**能操控角色、能玩**的 2D RPG 原型，优先把"人物能动"搞出来，再补 UI 系统。

### 第 1 步：角色 & 地图（最快见效）

**目标：场景里有一个能 WASD 移动的 2D 角色。**

1. C# 创建 `PlayerController.cs` — 2D 顶视角/横版移动、动画状态机
2. 创建简单的 Tilemap 地图或拼一个测试场景
3. 摄像机跟随角色
4. 能碰到墙壁（Collider2D）

验收：**按方向键角色能走动，有简单动画**

### 第 2 步：NPC & 交互

**目标：场景里有 NPC，走近后可以交互。**

1. C# 创建 `NPCController.cs` — 碰撞检测 + 交互提示
2. Lua 创建 `NPCData` 配置（NPC 名字、对话ID、任务ID）
3. 交互触发 Lua 事件

验收：**走到 NPC 旁边按交互键，弹出对话框**

### 第 3 步：对话系统（Lua）

**目标：和 NPC 对话，有文字逐字显示和选项分支。**

1. Lua: `Config/dialogue_config.lua` + `System/DialogueSystem.lua`
2. Lua: `UI/Dialogue/UIDialogueView.lua` — 对话界面控制器
3. Unity: 创建对话 UI 预制体，挂 `UILuaView`
4. 对话结束后能触发事件（接任务等）

验收：**和 NPC 对话，看到文字逐字播放，能选选项**

### 第 4 步：任务系统（Lua）

**目标：NPC 给任务，完成后交付领奖。**

1. Lua: `Config/quest_config.lua` + `Model/QuestModel.lua` + `System/QuestSystem.lua`
2. Lua: `UI/Quest/UIQuestView.lua` — 任务列表界面
3. 任务类型：对话、击杀、采集
4. 对话系统联动：对话结束自动接任务

验收：**接任务 → 任务追踪显示 → 完成条件 → 交任务拿奖励**

### 第 5 步：背包 & 装备系统完善

**目标：拾取物品、使用消耗品、穿脱装备。**

- 背包系统已经做好（BagSystem / UIBagView），补充场景中的拾取交互
- 新增 `EquipSystem` + `UIEquipView`
- 任务奖励 → 自动添加到背包

验收：**地上捡东西 → 背包里看到 → 装备穿上属性变化**

### 第 6 步：战斗（最简版）

**目标：能打怪。**

1. C# 创建 `EnemyController.cs` — 简单 AI（巡逻/追击）
2. Lua: `System/CombatSystem.lua` — 伤害计算、属性叠加
3. 攻击动画 + 碰撞检测
4. 怪物掉落物品 → 接入背包系统

验收：**打怪 → 掉血 → 怪物死亡 → 掉落物品**

### 第 7 步：存档 & 主流程串联

1. `SaveManager` — JSON 序列化所有 Model 数据
2. `GameFlow.lua` — 主流程状态机（登录 → 主城 → 副本）
3. 场景切换

验收：**退出再进，所有进度还在**

---

**核心原则：先让角色动起来，再堆系统。** 
看到角色在场景里跑来跑去才有动力继续做，一上来全是 UI 系统会让人失去兴趣。

## 14. 三阶段路线（更新版）

### 阶段一：角色能动 ✦ 最高优先级

目标：场景里有一个能操控的角色，能和 NPC 交互。

要做的事：

- C# `PlayerController` — 移动、动画
- C# `CameraFollow` — 摄像机跟随
- 简单测试地图（Tilemap 或手拼）
- C# `NPCController` — 交互触发
- Lua `DialogueSystem` + 对话 UI

验收标准：

- WASD 控制角色移动
- 走到 NPC 旁按键弹出对话
- 对话有文字、有选项

### 阶段二：最小可玩闭环

目标：游戏有任务、有战斗、有物品。

要做的事：

- Lua 任务系统（接任务 → 完成 → 交付）
- Lua 战斗系统（打怪 → 掉落）
- 背包/装备系统完善（已有基础）
- 存档系统

验收标准：

- 接任务"消灭3只史莱姆" → 打怪 → 拾取掉落 → 交任务拿奖励 → 装备奖励装备
- 退出重进进度还在

### 阶段三：热更发布链路

目标：Lua 和配置可以独立更新。

要做的事：

- Lua 打包到 Addressables
- 下载远端 Catalog
- 检查版本、下载更新
- 重载 Lua 入口

验收标准：

- 不改客户端包体，只更新 Lua 和配置，就能改变任务流程或 UI 逻辑

## 15. 你要避免的三个坑

### 坑一：一开始就想全项目纯 Lua

这样很容易把现有成熟 UI 框架全绕开，结果重造轮子。你这个项目不该这么干。

### 坑二：UI 直接改 Model

短期看很快，后面任务、对话、装备联动一多，立刻失控。

### 坑三：没先设计资源热更路径就说“支持热更”

Lua 热更的难点从来不是 `DoString`，而是“脚本从哪里来，版本怎么切，回退怎么办”。

## 16. 对你这个项目的直接建议

**现在最该做的事（按优先级排序）：**

1. 创建一个 2D 测试场景，放一个角色 Sprite + `PlayerController.cs`（移动 + 动画）
2. 加一个摄像机跟随脚本
3. 放几个 NPC，挂交互碰撞
4. Lua 做 `DialogueSystem` + 对话 UI（预制体挂 `UILuaView`）
5. 跑通"走到 NPC → 按键对话 → 对话结束触发任务"

**Lua 基础设施已经就绪**（LuaEnvManager、LuaUIBridge、EventBus、通用 UILuaView/UILuaItem），背包系统也已经做好。现在缺的是"角色在场景里动起来"。

把角色搞出来，后面的系统才有意义。

## 17. 最后的结论

你现在最需要的不是“学一堆 XLua API”，而是先定清楚项目架构边界。

对你这个工程，最优解非常明确：

- 保留现有 C# UI 框架和资源框架。
- Lua 先接管 UI 逻辑和 RPG 业务规则。
- 第一阶段用 C# 薄壳 UIView 过渡。
- 第二阶段再补 LuaViewRunner，升级成更纯的 Lua UI。
- 所有 RPG 模块统一按 Config + Model + System + UI 拆分。
- 热更链路先本地化，再远端化。

如果你按这个方向走，这个项目不但能做出一个小型 2D RPG，而且后面继续加商店、技能、成就、引导、活动，都不会乱。
