# UI 路由 / 下推式状态机使用说明

本文件描述了 `UIRouter`、`UIHierarchyLayerManager` 以及改进版 `UIStateMachine` 的整体协作方式，以及示例菜单配置策略。

## 核心结构

- **UIRouter**：全局单例（`[UIRouter]`）负责维护层级路由（MainMenu → GameUI → Primary → Secondary → Tertiary）与模态下推栈。使用 `DontDestroyOnLoad`，在场景切换时依旧保持。
- **UIHierarchyLayerManager**：挂在具体的 Canvas/面板上。每个层级一个管理器，需手动配置本层可控制的 UI 对象、过渡策略、模态射线屏蔽。支持复合层级（一个元素被多个管理器托管）。
- **UIStateMachine**：按钮/元素级动画状态机，现已支持 Profile 与层级映射，可根据所处层级切换不同的动画绑定。

## 路由层级（默认）

| 枚举值              | 含义               |
| ------------------- | ------------------ |
| `MainMenu`          | 主菜单（游戏初始界面） |
| `GameUI`            | 游戏 HUD（进行中界面） |
| `PrimaryMenu`       | 一级菜单（暂停/大面板） |
| `SecondaryMenu`     | 二级菜单（设置、读档等） |
| `TertiaryMenu`      | 三级菜单（确认弹窗等） |

> 可根据需要扩展，但建议保持「越往下层级值越大」。

## 典型配置示例

1. **创建全局路由器**：无需手动添加，`UIRouter` 会在 `BeforeSceneLoad` 自动生成。若需手动配置，可在场景中放置一个空物体并挂上 `UIRouter` 组件。
2. **GameUI 层管理器**：在游戏 HUD 根 Canvas 上添加 `UIHierarchyLayerManager`，`managedLevel` 设为 `GameUI`，配置默认 Tween Player、托管所有 HUD 元素。
3. **PrimaryMenu（暂停面板）**：为暂停面板根节点添加管理器，设为 `PrimaryMenu`。配置 `transitionPolicies`：
   - `fromLevel=GameUI`、`toLevel=PrimaryMenu` → 进入暂停面板播放 Track1。
   - `fromLevel=PrimaryMenu`、`toLevel=GameUI` → 退出暂停播放 Track1 的反向。
   - `fromLevel=PrimaryMenu`、`toLevel=SecondaryMenu` → 一级菜单退到背景播放 Track2（例如模糊）。
4. **SecondaryMenu（设置、读档等）**：为设置面板等二级界面添加管理器；可让其在 `from=PrimaryMenu → to=SecondaryMenu` 时播放入场 Track、在回退时播放离场 Track。
5. **TertiaryMenu（弹窗）**：常见用法是所有弹窗共用一个管理器，`transitionPolicies` 负责定义从 `SecondaryMenu`/`PrimaryMenu` 入栈时的动画。

## 托管元素配置

- `UIManagedElement.canvasGroup`：用于自动开关射线、交互；当层级不是当前栈顶时会被禁用。
- `profileOverrideId`：指定该元素在本层级应使用的 `UIStateMachine` Profile；留空则根据 `UIStateMachine.layerProfiles` 自动匹配。
- `resetStateOnActivate`：进入层级时是否把状态机重置到 Profile 定义的起始状态。
- `tweenPlayers`：额外需要在路由切换前 Kill 的动画播放器，防止残留动画。

## UIStateMachine Profile 配置

1. `stateAnimations` + `startingState`：默认 Profile 的绑定，沿用旧版本配置。
2. `additionalProfiles`：新增的 Profile 列表，每个 Profile 拥有独立的 `startingState` 与状态绑定。
3. `layerProfiles`：层级 → Profile Id 的映射；当管理器激活该层级时，调用 `ApplyLevelProfile` 自动切换动画表现。

示例：同一按钮在 `GameUI` 里使用常规高亮，在 `PrimaryMenu` 中使用弱化版动效，可将按钮的 `UIStateMachine`：
- 默认 Profile：GameUI 样式。
- 新建 Profile：`profileId = "PrimaryDimmed"`，绑定较弱的动画。
- `layerProfiles` 添加一条 `{ level = PrimaryMenu, profileId = "PrimaryDimmed" }`。

## 编程接口速览

```csharp
UIRouter.Instance.GoTo("Game", UIHierarchyLevel.GameUI);                 // 切换到游戏 HUD
UIRouter.Instance.GoTo("Pause/Settings", UIHierarchyLevel.PrimaryMenu); // 深链路由：暂停 → 设置
UIRouter.Instance.Push("Confirm", UIHierarchyLevel.TertiaryMenu);       // 压入模态弹窗
UIRouter.Instance.Pop(UIHierarchyLevel.TertiaryMenu);                    // 关闭弹窗
```

- 所有请求都会排队执行，`UIRouter.IsTransitioning` 为 `true` 时仍可安全排队。
- 每次切换前，路由器会调用 `Kill(false)`，防止 Tween 残留；如需打断立即进入下一个状态，可在对应 `transitionPolicies` 中设置期望时长。

## 深链路由（Deep Link）

- `UIRouter.GoTo("Settings/Audio", UIHierarchyLevel.PrimaryMenu, payload)`：从任意界面直接跳到二级「设置/音频」。
- 传入的 `payload` 会绑定在最深层级（示例中为 `Audio`），可在对应 `UIHierarchyLayerManager.onLayerEntered` 中读取。

## 射线屏蔽

- `UIHierarchyLayerManager.raycastShield` 可指向一个全屏透明 `CanvasGroup`，用于在模态层激活时阻止下层响应。
- 若 UI 元素自身存在 `CanvasGroup`，可将其拖入 `UIManagedElement.canvasGroup`，系统会在非当前层级时自动关闭 `blocksRaycasts` 与 `interactable`。

## 推荐的层级管理器组合

| 管理器 | managedLevel       | 说明 |
| ------ | ------------------ | ---- |
| Main   | `MainMenu`         | 负责开场主界面、Logo、开始按钮。 |
| Game   | `GameUI`           | 负责 HUD、战斗 UI、交互提示。 |
| Pause  | `PrimaryMenu`      | 负责暂停/背包等大面板。 |
| Sub    | `SecondaryMenu`    | 负责设置、存档、角色面板等子页面。 |
| Modal  | `TertiaryMenu`     | 负责确认弹窗、提示对话框等最上层模态。 |

> 可根据项目实际情况拆分多个 Secondary/Tertiary 管理器（例如设置、读档各一个），互相之间不会互斥，但建议保持同一层级仅有一个处于激活状态。

## 注意事项

- 若需手动抢占动画控制，可直接调用 `UIManagedElement.tweenPlayers` 中的 `UITweenPlayer.PlayMaster`，框架不会阻止。
- 推荐在 `transitionPolicies` 中维护统一命名的 Track，方便策划配置（例如 `Pause_Enter`, `Pause_Blur`, `Modal_Pop`）。
- 若存在特殊路由（例如从主菜单直接跳读档界面），可组合 `GoTo("MainMenu")` 与 `Push` 请求实现。
