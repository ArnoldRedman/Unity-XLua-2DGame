# SkierFramework UI系统 - 完整使用文档

> 一套基于Unity UGUI的通用UI管理框架，支持UI分层管理、安全区适配、黑边适配、UI跳转导航、循环列表、UI动画、3D模型显示、控件自动绑定等功能。

---

## 目录

- [1. 项目结构](#1-项目结构)
- [2. 快速开始](#2-快速开始)
- [3. 核心架构](#3-核心架构)
- [4. UI生命周期](#4-ui生命周期)
- [5. UI分层系统（UILayer）](#5-ui分层系统uilayer)
- [6. 安全区适配（UIAdapter）](#6-安全区适配uiadapter)
- [7. 黑边适配](#7-黑边适配)
- [8. UI配置系统（UIConfig）](#8-ui配置系统uiconfig)
- [9. UI控件绑定](#9-ui控件绑定)
- [10. UI动画系统](#10-ui动画系统)
- [11. UI跳转导航](#11-ui跳转导航)
- [12. 循环滚动列表（UIScrollView）](#12-循环滚动列表uiscrollview)
- [13. 3D模型显示](#13-3d模型显示)
- [14. Loading加载系统](#14-loading加载系统)
- [15. 消息弹窗（MessageBox）](#15-消息弹窗messagebox)
- [16. 事件系统](#16-事件系统)
- [17. Tween动画工具](#17-tween动画工具)
- [18. 资源管理（ResourceManager）](#18-资源管理resourcemanager)
- [19. 创建新UI的完整步骤](#19-创建新ui的完整步骤)
- [20. URP管线支持](#20-urp管线支持)
- [21. 常见问题](#21-常见问题)

---

## 1. 项目结构

```
Assets/Scripts/
├── Framework/                          # 框架核心代码
│   ├── UI/
│   │   ├── UIManager.cs                # UI管理器（核心入口）
│   │   ├── UIAdapter.cs                # 安全区适配组件
│   │   ├── Extends/
│   │   │   └── UIExtension.cs          # UI扩展工具方法
│   │   ├── UIViewBase/
│   │   │   ├── UIType.cs               # UI类型枚举
│   │   │   ├── UIConfig.cs             # UI配置加载
│   │   │   ├── UILayer.cs              # UI层级管理逻辑
│   │   │   ├── UIView.cs               # UI界面基类
│   │   │   ├── UIViewController.cs     # UI控制器
│   │   │   ├── UIViewAnim.cs           # UI开关动画
│   │   │   └── UISubView.cs            # 子UI基类
│   │   ├── UIControlBinding/
│   │   │   └── Scripts/
│   │   │       ├── UIControlData.cs    # 控件绑定数据
│   │   │       ├── ControlBindingAttribute.cs
│   │   │       ├── SubUIBindingAttribute.cs
│   │   │       └── IBindableUI.cs
│   │   ├── UIScrollView/
│   │   │   ├── UIScrollView.cs         # 循环滚动列表
│   │   │   └── UILoopItem.cs           # 列表项基类
│   │   ├── UIModel/
│   │   │   ├── UIModelManager.cs       # 3D模型管理
│   │   │   └── UIRenderToTexture.cs    # 渲染到纹理
│   │   └── Tweening/
│   │       └── Tweening/
│   │           ├── UITweener.cs         # Tween基类
│   │           ├── TweenAlpha.cs        # 透明度动画
│   │           ├── TweenScale.cs        # 缩放动画
│   │           ├── TweenPosition.cs     # 位置动画
│   │           ├── TweenRotation.cs     # 旋转动画
│   │           ├── TweenColor.cs        # 颜色动画
│   │           └── ...                  # 其他Tween
│   ├── Resources/
│   │   ├── ResourceManager.cs          # 资源管理器(Addressables)
│   │   └── InstancePool.cs             # 实例对象池
│   └── Other/
│       ├── Singleton.cs                # 单例基类
│       ├── EventController.cs          # 事件系统
│       ├── ObjectPool.cs               # 通用对象池
│       ├── PrefabPool.cs               # 预制体对象池
│       ├── ObjectExtension.cs          # 扩展方法
│       └── TreeNode.cs                 # 树结构
├── Application/                        # 业务层示例
│   └── UIViews/
│       ├── Loading.cs                  # Loading管理
│       ├── UILoadingView.cs            # Loading界面
│       ├── UILoginView.cs              # 登录界面（示例）
│       ├── UIMessageBoxView.cs         # 消息弹窗
│       ├── UITestView.cs               # 测试界面
│       ├── UITestView1/2/3.cs          # 跳转测试界面
│       └── UIEvent.cs                  # 业务事件枚举
└── Scenes/
    └── Launcher.cs                     # 启动入口

Assets/AssetsPackage/
└── UI/
    ├── UIConfig.json                   # UI配置文件
    └── Prefabs/                        # UI预制体目录
```

---

## 2. 快速开始

### 2.1 初始化流程

框架的初始化在 `Launcher.cs` 中完成：

```csharp
IEnumerator StartCor()
{
    // 1. 初始化资源管理器（Addressables）
    yield return StartCoroutine(ResourceManager.Instance.InitializeAsync());

    // 2. 初始化UI管理器（创建UIRoot、UICamera、各层Canvas）
    UIManager.Instance.Initialize();

    // 3. 加载UI配置文件（解析 UIConfig.json）
    yield return UIManager.Instance.InitUIConfig();

    // 4. 预加载Loading界面
    yield return UIManager.Instance.Preload(UIType.UILoadingView);

    // 5. 启动Loading，在Loading回调中执行游戏初始化
    Loading.Instance.StartLoading(EnterGameCor);
}
```

### 2.2 初始化后的场景结构

初始化后，场景中会自动创建以下对象：

```
UIRoot (DontDestroyOnLoad)
├── UICamera                    # UI专用相机（正交、仅渲染UI层）
├── SceneLayer (Canvas)         # 3D场景UI层 (World Space, Order=1000)
├── BackgroundLayer (Canvas)    # 背景层 (Order=2000)
├── NormalLayer (Canvas)        # 普通UI层 (Order=3000)
├── InfoLayer (Canvas)          # 信息层 (Order=4000)
├── TopLayer (Canvas)           # 顶层 (Order=5000)
├── TipLayer (Canvas)           # 提示层 (Order=6000)
└── BlackMaskLayer (Canvas)     # 黑色遮罩层 (Order=7000)
```

每个Canvas的 `CanvasScaler` 使用 `ScaleWithScreenSize` + `Expand` 模式，参考分辨率默认为 **1920×1080**。

---

## 3. 核心架构

框架的核心类关系：

```
UIManager (单例，总管理器)
  ├── UILayerLogic × 7层        # 每层管理该层的UI排序和遮挡
  ├── UIViewController × N个    # 每个UIType对应一个控制器
  │   ├── UIView                # 界面显示（基类）
  │   └── UIViewAnim            # 界面动画（可选）
  └── UIConfig                  # 配置数据
```

**职责分离：**
- `UIManager`：对外接口（Open、Close、JumpUI等）
- `UILayerLogic`：层级排序 + UI遮挡管理
- `UIViewController`：单个UI的加载/开关/生命周期
- `UIView`：界面显示逻辑（业务子类继承此类）
- `UIConfig`：从JSON读取配置，映射UIType到资源路径和层级

---

## 4. UI生命周期

一个UI从打开到关闭的完整生命周期：

```
首次打开:
  Load（异步加载预制体）
    → OnInit（初始化、控件绑定）
      → OnOpen（打开，传入userData）
        → OnAddListener（注册事件）
          → OnResume（界面恢复到前台）

被其他全屏UI覆盖:
  → OnPause（被遮挡暂停）

覆盖UI关闭后:
  → OnResume（恢复前台）

关闭:
  → OnClose（关闭）
    → OnRemoveListener（移除事件）
      → OnPause（暂停）

再次打开（已加载过）:
  → OnOpen → OnAddListener → OnResume

释放:
  → OnRelease → Destroy
```

### 生命周期方法说明

| 方法 | 调用时机 | 用途 |
|------|---------|------|
| `OnInit(UIControlData, UIViewController)` | 首次加载完成 | 初始化控件引用、一次性设置 |
| `OnOpen(object userData)` | 每次打开时 | 刷新数据、设置初始状态 |
| `OnAddListener()` | 每次打开时 | 注册按钮点击、事件监听 |
| `OnResume()` | 界面恢复到前台时 | 刷新显示、恢复状态 |
| `OnPause()` | 被其他全屏UI覆盖时 | 暂停逻辑 |
| `OnClose()` | 关闭时 | 清理状态 |
| `OnRemoveListener()` | 关闭时 | 移除按钮点击、事件监听 |
| `OnCancel()` | 按下返回键时 | 默认行为是关闭自身 |
| `OnRelease()` | 释放销毁时 | 释放资源 |

---

## 5. UI分层系统（UILayer）

### 5.1 层级定义

```csharp
public enum UILayer
{
    SceneLayer      = 1000,  // 3DUI层（World Space模式）
    BackgroundLayer = 2000,  // 背景层（主界面背景、黑边）
    NormalLayer     = 3000,  // 普通层（绝大多数游戏UI）
    InfoLayer       = 4000,  // 信息层（需要显示在普通UI之上的信息）
    TopLayer        = 5000,  // 顶层（Loading等）
    TipLayer        = 6000,  // 提示层（Toast、跑马灯）
    BlackMaskLayer  = 7000,  // 黑色遮罩层（转场渐变）
}
```

### 5.2 层内排序机制

- 每个UI打开时，从当前层分配一个唯一的 `order` 值（递增，步长30）
- 步长为30是为了在两个UI之间预留空间给特效、粒子等排序使用
- UI关闭时归还 `order`，并重新计算当前层最大值

### 5.3 UI遮挡管理

框架自动管理同层内的UI遮挡关系，优化渲染性能：

- **全屏UI**（`isWindow = false`）打开时，会遮挡同层内 order 更小的所有UI
- **窗口UI**（`isWindow = true`）打开时，不会遮挡底层UI（因为窗口下方需要显示其他内容）
- 被遮挡的UI会调用 `SetActive(false)` 隐藏，恢复时调用 `SetActive(true)`
- 每个UI维护一个 `topViewNum` 计数器，记录上方有几个全屏UI，只有计数为0时才可见

**注意：** 遮挡管理仅用于渲染优化，不涉及业务逻辑。如果需要UI跳转恢复（如A→B→C，关闭C恢复B），请使用 `JumpUI` 接口。

---

## 6. 安全区适配（UIAdapter）

### 6.1 工作原理

`UIAdapter` 组件通过调整 `RectTransform` 的锚点，使UI内容避开手机刘海、圆角等非安全区域。

核心逻辑：
1. 获取设备安全区 `Screen.safeArea`
2. 检测设备朝向 `Screen.orientation`
3. 将安全区映射到锚点值：`anchorMin` 和 `anchorMax`
4. **每秒检测一次**，应对屏幕旋转、华为分屏等动态变化

### 6.2 三种适配模式

```csharp
public enum UIAdaptType
{
    All,            // 全方向适配（上下左右都适配安全区）
    LeftOrTop,      // 仅适配左侧/顶部安全区（刘海侧）
    RightOrBottom,  // 仅适配右侧/底部安全区
}
```

### 6.3 使用方法

1. **在需要适配安全区的UI节点上挂载 `UIAdapter` 组件**
2. 选择适配类型
3. 该节点的所有子元素会自动跟随安全区调整

```
┌─────────────────────────────────────┐
│  [UIAdapter: All]                    │ ← 挂载在最外层容器
│  ┌─────────────────────────────┐    │
│  │        安全区域内容          │    │
│  │    按钮、文字、列表等       │    │
│  └─────────────────────────────┘    │
│          ↑ 刘海区域 ↑               │
└─────────────────────────────────────┘
```

### 6.4 横屏适配（LandscapeLeft/Right）

- **`All`**: 左右两侧都避开安全区
  ```
  anchorMin.x = safeArea.xMin / Screen.width
  anchorMax.x = safeArea.xMax / Screen.width
  ```
- **`LeftOrTop`**: 仅避开刘海侧（根据朝向判断刘海在左还是右）
  - `LandscapeLeft`: 刘海在左 → 左侧避开
  - `LandscapeRight`: 刘海在右 → 右侧避开
- **`RightOrBottom`**: 与 LeftOrTop 相反

### 6.5 竖屏适配（Portrait/PortraitUpsideDown）

- **`All`**: 上下两侧都避开安全区
  ```
  anchorMin.y = safeArea.yMin / Screen.height
  anchorMax.y = safeArea.yMax / Screen.height
  ```
- **`LeftOrTop`**: 仅避开顶部（刘海侧）
- **`RightOrBottom`**: 仅避开底部

### 6.6 与黑边适配的协作

当使用黑边模式时，`UIManager.GetSafeArea()` 会在 `Screen.safeArea` 基础上扩展安全区范围，将黑边区域也纳入安全区计算，避免双重缩进。

---

## 7. 黑边适配

### 7.1 黑边类型

```csharp
public enum UIBlackType
{
    None,       // 无黑边，全适应（使用Expand模式拉伸）
    Height,     // 高度填满，左右留黑边
    Width,      // 宽度填满，上下留黑边
    AutoBlack,  // 自动选择黑边最少的方式
}
```

### 7.2 配置方式

在 `UIManager` 组件上设置：

```csharp
public int width = 1920;              // 设计分辨率宽度
public int height = 1080;             // 设计分辨率高度
public UIBlackType uiBlackType;       // 黑边模式
```

### 7.3 自动黑边（AutoBlack）

自动判断当前屏幕分辨率与设计分辨率的差距，选择黑边面积最小的方案：
- 宽度差距 > 高度差距 → 高度适配（左右黑边）
- 高度差距 > 宽度差距 → 宽度适配（上下黑边）

### 7.4 黑边模式下的UI加载

UI预制体加载后，根据黑边模式自动调整 `RectTransform`：

| 模式 | 锚点 | 说明 |
|------|------|------|
| `None` | StretchAll | 全屏拉伸，锚点需自行设置 |
| `Height` | VertStretchCenter | 垂直拉伸、水平居中，宽度=设计宽度 |
| `Width` | HorStretchMiddle | 水平拉伸、垂直居中，高度=设计高度 |

**黑边的优势：** 使用黑边模式后，UI画布实际分辨率与设计分辨率一致，锚点可以全部居中，不需要精细设置锚点。

---

## 8. UI配置系统（UIConfig）

### 8.1 配置文件

路径：`Assets/AssetsPackage/UI/UIConfig.json`

```json
[
  {
    "uiType": "UILoadingView",
    "path": "Assets/AssetsPackage/UI/Prefabs/UILoadingView/UILoadingView.prefab",
    "isWindow": true,
    "uiLayer": "TipLayer",
    "isAutoNavigation": false
  },
  {
    "uiType": "UILoginView",
    "path": "Assets/AssetsPackage/UI/Prefabs/UILoginView/UILoginView.prefab",
    "isWindow": false,
    "uiLayer": "NormalLayer",
    "isAutoNavigation": false
  }
]
```

### 8.2 配置字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `uiType` | string | UI类型名，必须与 `UIType` 枚举成员名一致 |
| `path` | string | 预制体的Addressable路径 |
| `isWindow` | bool | `true`=窗口（不遮挡底层UI），`false`=全屏界面（会遮挡底层） |
| `uiLayer` | string | 所在层级名，必须与 `UILayer` 枚举成员名一致 |
| `isAutoNavigation` | bool | 预留字段 |

### 8.3 类型解析

配置加载时通过反射将 `uiType` 字符串解析为：
- `UIType` 枚举值
- `UILayer` 枚举值
- 对应的 `UIView` 子类 `Type`（用于运行时 `AddComponent`）

---

## 9. UI控件绑定

### 9.1 绑定方式

框架使用 [UIControlBinding](https://github.com/Misaka-Mikoto-Tech/UIControlBinding) 方案：

1. 在预制体上挂载 `UIControlData` 组件
2. 在 `UIControlData` Inspector 中添加控件引用
3. 在代码中用 `[ControlBinding]` 特性标记字段
4. 框架自动通过反射将控件绑定到代码字段

### 9.2 代码示例

```csharp
public class UILoginView : UIView
{
    // 控件绑定（字段名必须与UIControlData中配置的名称一致）
    [ControlBinding]
    private Button BtnStart;
    
    [ControlBinding]
    private Button BtnSetting;
    
    [ControlBinding]
    private TextMeshProUGUI TxtTitle;
    
    [ControlBinding]
    private RawImage ImgModel;

    // 子UI绑定
    [SubUIBinding]
    private MySubPanel subPanel;

    public override void OnInit(UIControlData uIControlData, UIViewController controller)
    {
        base.OnInit(uIControlData, controller);
        // OnInit中base已经完成了绑定，此处可直接使用控件
    }

    public override void OnAddListener()
    {
        base.OnAddListener();
        BtnStart.AddClick(OnClickStart);
    }

    public override void OnRemoveListener()
    {
        base.OnRemoveListener();
        BtnStart.onClick.RemoveAllListeners();
    }
}
```

### 9.3 绑定原理

`UIControlData.BindDataTo(IBindableUI)` 方法：
1. 通过反射获取目标类的所有带 `[ControlBinding]` 特性的字段
2. 按名称匹配 `UIControlData` 中存储的控件引用
3. 将控件赋值给对应字段
4. 反射信息会被缓存，避免重复反射开销

---

## 10. UI动画系统

### 10.1 UIViewAnim 组件

挂载在UI预制体上，自动在UI打开/关闭时播放动画。

```csharp
public enum UIAppearType
{
    None,                // 无动画
    Animation,           // 播放 Animation 组件动画
    Alpha,               // 透明度渐变
    AlphaAndAnimation,   // 透明度 + Animation 动画
    Scale,               // 缩放动画
    ScaleAndAlpha,       // 缩放 + 透明度
}
```

### 10.2 配置方式

在预制体上挂载 `UIViewAnim` 组件，设置：
- `openType`：打开时的动画类型
- `closeType`：关闭时的动画类型
- `animTime`：动画时长（秒）
- `animtion`：Animation组件引用（如果使用Animation类型）
- `target`：动画目标Transform（可选，默认为自身）

### 10.3 动画流程

```
Open:  playRate 0→1, alpha 0→1, scale 0→1
Close: playRate 1→0, alpha 1→0, scale 1→0
```

关闭动画播放完成后才会真正执行 `TrueClose`（隐藏GO、触发OnClose）。

---

## 11. UI跳转导航

### 11.1 问题场景

需要实现：打开 A→B→C，关闭C后回到B，关闭B后回到A。

### 11.2 使用 JumpUI

```csharp
// 从当前UI跳转到目标UI
// 当目标UI被关闭时，自动重新打开当前UI
UIManager.Instance.JumpUI(
    curUIType: UIType.UITestView1,      // 当前UI类型
    curUserData: myData,                 // 当前UI的userData（恢复时传入）
    nextUIType: UIType.UITestView2,      // 目标UI类型
    nextUserData: null                   // 目标UI的userData
);
```

### 11.3 跳转原理

1. `JumpUI` 将跳转记录存入 `_uiJumpDatas` 列表
2. 打开目标UI，成功后关闭当前UI（使用 `isJump=true` 避免触发恢复）
3. 当目标UI被关闭时（非跳转关闭），`OnUIClose` 遍历跳转记录
4. 找到匹配项后，重新打开源UI并传入之前的 `userData`

### 11.4 跳转链示例

```csharp
// UITestView1 中
UIManager.Instance.JumpUI(UIType.UITestView1, null, UIType.UITestView2, null);

// UITestView2 中
UIManager.Instance.JumpUI(UIType.UITestView2, null, UIType.UITestView3, null);

// 关闭 UITestView3 → 自动打开 UITestView2
// 关闭 UITestView2 → 自动打开 UITestView1
```

**注意：** 跳转导航与UI遮挡是两套独立逻辑。遮挡管理仅做渲染优化，跳转管理处理业务流程。

---

## 12. 循环滚动列表（UIScrollView）

### 12.1 功能特性

- 虚拟化渲染：只实例化可见区域 +2 行/列的Item，节省内存
- 支持水平/垂直滚动
- 支持多行多列（网格布局）
- 支持选中功能
- 支持分页
- 自动回收复用Item
- 对齐方式：Left、Right、Top、Bottom、Center

### 12.2 使用步骤

**1. 场景设置：**
- 创建 `ScrollRect` + `Content` 结构
- 在 `ScrollRect` 同节点挂载 `UIScrollView` 组件
- 配置参数（方向、间距、对齐方式等）

**2. 创建Item类（继承 UILoopItem）：**

```csharp
public class MyListItem : UILoopItem
{
    [ControlBinding]
    private TextMeshProUGUI TxtName;
    
    [ControlBinding]
    private Button BtnSelect;

    public override void OnInit()
    {
        base.OnInit();
    }

    public override void OnAddListener()
    {
        BtnSelect.AddClick(() => {
            UIScrollView.Select(m_Index);
        });
    }

    protected override void OnUpdateData(IList dataList, int index, object userData)
    {
        var data = dataList[index] as MyData;
        TxtName.text = data.name;
    }

    public override void CheckSelect(int index)
    {
        // index == m_Index 时表示当前Item被选中
        BtnSelect.interactable = (index != m_Index);
    }
}
```

**3. 在父UI中调用：**

```csharp
public class UILoginView : UIView
{
    [ControlBinding]
    private UIScrollView scrollView;
    
    [ControlBinding]
    private GameObject itemPrefab;

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        List<MyData> dataList = GetDataList(); // 获取数据
        scrollView.UpdateList<MyListItem>(dataList, itemPrefab);
        scrollView.Select(0); // 默认选中第一项
    }
}
```

### 12.3 UIScrollView 参数说明

| 参数 | 说明 |
|------|------|
| `m_ScrollRect` | 关联的ScrollRect组件 |
| `m_Content` | Content RectTransform |
| `m_AxisType` | `Horizontal` 或 `Vertical` |
| `m_AlignType` | 对齐方式（Left/Right/Top/Bottom/Center） |
| `m_ItemPivot` | Item的中心点 |
| `m_HorizontalStartSpace` | 水平起始间距 |
| `m_VerticalStartSpace` | 垂直起始间距 |
| `m_HorizontalSpace` | 水平间距 |
| `m_VerticalSpace` | 垂直间距 |
| `m_CountOfOtherAxis` | 另一方向上的Item数量（0=自动计算） |
| `m_IsPaging` | 是否启用分页 |

### 12.4 常用API

```csharp
// 刷新列表
scrollView.UpdateList<T>(dataList, prefab, isKeepPos, userData);

// 选中某项
scrollView.Select(index);

// 滚动到某项
scrollView.MoveTo(index, duration);

// 选中变化回调
scrollView.OnSelectChanged += (index) => { };

// 释放
scrollView.Release();
```

---

## 13. 3D模型显示

### 13.1 原理

使用独立相机渲染3D模型到 `RenderTexture`，再通过 `RawImage` 显示在UI上。

### 13.2 加载模型

```csharp
// 方式1：通过Addressable路径加载
UIModelManager.Instance.LoadModelToRawImage(
    "Assets/AssetsPackage/Model/TestModel.prefab",  // 模型路径
    rawImage,           // 目标RawImage
    canDrag: true,      // 是否可拖拽旋转
    offset: Vector3.zero,
    rotation: Quaternion.identity,
    scale: Vector3.one * 5,
    isOrth: true,       // 是否正交相机
    orthSizeOrFOV: 60   // 正交大小 或 FOV
);

// 方式2：直接传入GameObject
UIModelManager.Instance.LoadModelToRawImage(
    modelGameObject, rawImage, canDrag: true
);
```

### 13.3 卸载模型

```csharp
UIModelManager.Instance.UnLoadModelByRawImage(rawImage);
// 或
UIModelManager.Instance.UnLoadModelByRawImage(rawImage, modelGO);
```

### 13.4 特性

- 支持拖拽旋转模型
- 支持正交/透视相机
- 支持点击模型检测（`OnTargetClick` 事件）
- 相机池化复用

---

## 14. Loading加载系统

### 14.1 使用方式

```csharp
Loading.Instance.StartLoading(MyLoadingCoroutine, isCleanupAsset: true);

IEnumerator MyLoadingCoroutine(Action<float, string> refresh)
{
    refresh?.Invoke(0.1f, "正在加载配置...");
    yield return LoadConfigs();

    refresh?.Invoke(0.5f, "正在加载场景...");
    yield return LoadScene();

    refresh?.Invoke(0.9f, "正在初始化...");
    yield return Init();

    refresh?.Invoke(1.0f, "加载完成");
}
```

### 14.2 Loading流程

1. 打开 `UILoadingView`
2. 如果 `isCleanupAsset=true`，先清理旧资源（`ResourceManager.Instance.Cleanup()`）
3. 执行用户传入的加载协程，协程中通过 `refresh` 回调更新进度
4. 加载完成后关闭 `UILoadingView`
5. 触发 `GC.Collect()` 回收内存

---

## 15. 消息弹窗（MessageBox）

### 15.1 双按钮弹窗

```csharp
UIManager.Instance.Open(UIType.UIMessageBoxView,
    ObjectPool<MessageBoxData>.Get().Set(
        title: "提示",
        content: "确定要退出游戏吗？",
        confirmCallback: () => { Application.Quit(); },
        cancelCallback: () => { Debug.Log("取消"); },
        buttonNames: new string[] { "确定", "取消" }  // 可选
    )
);
```

### 15.2 单按钮弹窗

```csharp
UIManager.Instance.Open(UIType.UIMessageBoxView,
    ObjectPool<MessageBoxData>.Get().SetOneButton(
        title: "通知",
        content: "操作已完成！",
        confirmCallback: () => { },
        buttonNames: new string[] { "确定" }
    )
);
```

---

## 16. 事件系统

### 16.1 定义事件

```csharp
// 在 UIEvent.cs 中定义业务事件
public enum UIEvent
{
    OnGoldChanged,
    OnLevelUp,
    // ...
}
```

### 16.2 使用事件

```csharp
// 注册事件（在 OnAddListener 中）
UIManager.Instance.Event.AddListener<int>(UIEvent.OnGoldChanged, OnGoldChanged);

// 移除事件（在 OnRemoveListener 中）
UIManager.Instance.Event.RemoveListener<int>(UIEvent.OnGoldChanged, OnGoldChanged);

// 触发事件
UIManager.Instance.Event.TriggerEvent(UIEvent.OnGoldChanged, newGold);

// 事件回调
private void OnGoldChanged(int gold)
{
    txtGold.text = gold.ToString();
}
```

事件系统支持 0~6 个参数的委托类型，且有类型检查机制。

---

## 17. Tween动画工具

框架内置了一套轻量级Tween系统（移植自NGUI UITweener），可在UI上快速制作简单动画：

| 组件 | 功能 |
|------|------|
| `TweenAlpha` | 透明度动画 |
| `TweenScale` | 缩放动画 |
| `TweenPosition` | 位移动画 |
| `TweenRotation` | 旋转动画 |
| `TweenColor` | 颜色动画 |
| `TweenFill` | Image填充动画 |
| `TweenWidth` | 宽度动画 |
| `TweenHeight` | 高度动画 |
| `TweenFOV` | 相机FOV动画 |
| `TweenOrthoSize` | 相机正交大小动画 |
| `TweenVolume` | 音量动画 |

### 通用参数

```csharp
public enum Method { Linear, EaseIn, EaseOut, EaseInOut, BounceIn, BounceOut }
public enum Style  { Once, Loop, PingPong }
```

- `from` / `to`：起始值和目标值
- `duration`：动画时长
- `method`：缓动曲线
- `style`：播放模式（一次/循环/往返）
- `delay`：延迟开始
- `ignoreTimeScale`：是否忽略时间缩放
- `animationCurve`：自定义动画曲线

---

## 18. 资源管理（ResourceManager）

### 18.1 基于Addressables

框架使用 Unity Addressables 进行资源管理，`ResourceManager` 封装了常用接口：

```csharp
// 初始化
yield return ResourceManager.Instance.InitializeAsync();

// 加载资源
ResourceManager.Instance.LoadAssetAsync<Sprite>(path, (sprite) => {
    image.sprite = sprite;
});

// 加载并实例化
ResourceManager.Instance.InstantiateAsync(prefabPath, (go) => {
    go.transform.SetParent(parent);
});

// 回收实例（放入对象池）
ResourceManager.Instance.Recycle(gameObject);

// 释放不再使用的资源
ResourceManager.Instance.Cleanup();
```

### 18.2 特性

- 异步加载 + Handle缓存
- GameObject实例对象池
- 常驻资源（Resident）支持，不会被Cleanup释放
- 引用计数管理
- SpriteAtlas缓存
- 场景加载
- 热更资源下载检查

---

## 19. 创建新UI的完整步骤

### 方式一：自动化创建（推荐）

1. 创建UI预制体，放到 `Assets/AssetsPackage/UI/Prefabs/` 目录下
2. 在Project面板选中预制体，右键 → **CreateUI**
3. 自动生成：
   - UI类文件（继承UIView）
   - UIType枚举新增成员
   - UIConfig.json新增配置

### 方式二：手动创建

**第一步：创建UI预制体**
- 创建预制体，设计UI布局
- 挂载 `UIControlData` 组件，绑定需要的控件引用

**第二步：新增UIType枚举**

```csharp
// UIType.cs
public enum UIType
{
    UILoadingView,
    UILoginView,
    UIMessageBoxView,
    UIMyNewView,       // ← 新增
    Max,
}
```

**第三步：添加UI配置**

在 `Assets/AssetsPackage/UI/UIConfig.json` 中添加：

```json
{
    "uiType": "UIMyNewView",
    "path": "Assets/AssetsPackage/UI/Prefabs/UIMyNewView/UIMyNewView.prefab",
    "isWindow": false,
    "uiLayer": "NormalLayer",
    "isAutoNavigation": false
}
```

**第四步：创建UI类**

```csharp
using SkierFramework;
using UnityEngine.UI;
using TMPro;

public class UIMyNewView : UIView
{
    [ControlBinding]
    private Button BtnClose;

    [ControlBinding]
    private TextMeshProUGUI TxtTitle;

    public override void OnInit(UIControlData uIControlData, UIViewController controller)
    {
        base.OnInit(uIControlData, controller);
    }

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        TxtTitle.text = userData as string ?? "默认标题";
    }

    public override void OnAddListener()
    {
        base.OnAddListener();
        BtnClose.AddClick(() => {
            UIManager.Instance.Close(Controller.uiType);
        });
    }

    public override void OnRemoveListener()
    {
        base.OnRemoveListener();
        BtnClose.onClick.RemoveAllListeners();
    }

    public override void OnClose()
    {
        base.OnClose();
    }
}
```

**第五步：使用**

```csharp
// 打开
UIManager.Instance.Open(UIType.UIMyNewView, "Hello World");

// 关闭
UIManager.Instance.Close(UIType.UIMyNewView);

// 判断是否打开
if (UIManager.Instance.IsOpen(UIType.UIMyNewView)) { }

// 获取实例（不推荐，建议用事件交互）
var view = UIManager.Instance.GetView<UIMyNewView>(UIType.UIMyNewView);
```

---

## 20. URP管线支持

如果项目使用 URP 管线，需要在 `UIManager.Initialize()` 中取消以下注释：

```csharp
_uiCamera.GetOrAddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()
    .renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;

if (_worldCamera != null)
{
    var cameraData = _worldCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
    if (cameraData != null)
    {
        cameraData.cameraStack.Add(_uiCamera);
    }
}
```

---

## 21. 常见问题

### Q: UI被覆盖后底层还在渲染？
检查 `UIConfig.json` 中上层UI的 `isWindow` 字段。`isWindow=false`（全屏界面）才会触发遮挡管理。

### Q: 关闭UI后没有回到上一个界面？
UI遮挡管理不处理跳转逻辑。如需跳转恢复，使用 `UIManager.Instance.JumpUI()`。

### Q: 安全区适配不生效？
确保 `UIAdapter` 挂载在正确的节点上。该节点的锚点和sizeDelta会被框架控制，不要手动设置。

### Q: 新创建的UI显示不出来？
检查：
1. `UIType` 枚举是否已添加
2. `UIConfig.json` 配置是否正确
3. 预制体路径是否正确（Addressable路径）
4. UI类名是否与 `uiType` 一致

### Q: UIControlData绑定变更后怎么处理？
右键 `UIControlData` 组件 → 复制到剪贴板 → 在代码中粘贴对应绑定信息。不要删除重建组件。

### Q: 如何注册常驻UI（不被CloseAll关闭）？
```csharp
UIManager.Instance.AddResidentUI(UIType.UIMyHUD);
```

### Q: 如何实现转场效果？
```csharp
// 淡入黑屏
UIManager.Instance.FadeIn(0.5f, () => {
    // 切换场景或UI
    UIManager.Instance.FadeOut(0.5f);
});

// 淡入淡出
UIManager.Instance.FadeInOut(1.0f, () => {
    // 中间回调
});
```

---

## 更新日志

- **v1.0** (2023/10/27) - 框架初版
- **v1.1** (2024/02/18) - 新增UI创建/删除管理界面，优化代码命名
- **v1.2** (2026/02/11) - 新增JumpUI跳转接口，优化OverrideUI创建，支持URP管线

## 项目链接

[UISystem - GitHub](https://github.com/Skierhou/UISystem.git)
