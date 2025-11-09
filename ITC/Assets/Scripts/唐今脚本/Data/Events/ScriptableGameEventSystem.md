# ScriptableObject 泛型事件系统使用说明

本系统以 ScriptableObject 作为事件载体，实现“触发者（Raiser）”与“监听者（Listener）”完全解耦的同时，保留了 Inspector 拖拽配置的便捷性。以下内容将帮助你快速上手并扩展。

---

## 核心角色与文件

- **事件资产（Event）**：`BaseGameEvent<T>` 的具体实现，位于 `Assets/Scripts/唐今脚本/Data/Events/`。常用类型包含：
  - `StringGameEvent`、`IntGameEvent`、`FloatGameEvent`、`BoolGameEvent`
  - `Vector3GameEvent`、`UnityObjectGameEvent`
  - 自定义的 `PanelChangedGameEvent`（携带新旧面板名）
- **监听器（Listener）**：挂在 GameObject 上的组件，继承自 `BaseGameEventListener<T>`，通过 `UnityEvent<T>` 在 Inspector 内配置回调。
- **触发器 / 桥接器（Raiser）**：继承自 `BaseGameEventRaiser<T>` 的组件（如 `StringGameEventRaiser`），用于从 Button 等原生 UnityEvent 中直接触发事件。

所有基类、常用类型的事件/监听器/触发器都已预置，无需重复编写样板代码。

---

## 快速配置流程

1. **创建事件资产**
   - 在 Project 窗口中右键 → `Create/Game Events/...` → 选择所需类型。
   - 例如：创建 `StringGameEvent`，命名为 `OnPanelRequest.asset`。

2. **配置触发方**
   - 在需要触发事件的对象上添加对应的 `*GameEventRaiser` 组件。
   - 将上述事件资产拖入 `Event` 字段。
   - 可选：设置 `Payload` 为默认参数，使 `Raise()`（无参调用）即可触发。
   - Button 等 UI，可在 `onClick` 中选择 `Raise(string value)` 重写参数。

3. **配置监听方**
   - 在目标对象上添加对应的 `*GameEventListener` 组件。
   - 将同一事件资产拖入 `Event` 字段。
   - 在 `Response` 中通过 UnityEvent 配置回调（可直接选择本对象上的方法）。

4. **运行验证**
   - 播放场景后，触发方调用 `Raise`，监听方将收到并执行回调。

---

## 面板系统案例（PanelManager ↔ FeelButtonFSM）

### Request 面板切换
1. 创建 `StringGameEvent`（如 `RequestPanelChange.asset`）。
2. 在可交互按钮上添加 `StringGameEventRaiser`，拖入事件：
   - `Button.onClick` → `StringGameEventRaiser.Raise(string)` → 在参数框填写目标面板名。
3. 在 `PanelManager` 上：
   - Inspector 中将 `请求面板事件` 字段指向 `RequestPanelChange.asset`。
   - `PanelManager` 会在运行时自动注册为监听者并调用 `ChangePanel()`。

### 广播面板切换结果
1. 创建 `PanelChangedGameEvent`（如 `PanelChanged.asset`）。
2. `PanelManager` 的 `面板切换事件` 字段指向该资产，即可在成功切换后广播新旧面板名称。
3. `FeelButtonFSM`：
   - `事件响应/面板切换事件` 字段指向 `PanelChanged.asset`。
   - 勾选 “面板专属动效预设” 后，可为不同面板配置独立的 hover/idle 动效。
   - 若指定 `GamePanelLibrarySO`，自定义编辑器会提供面板名称下拉列表，加速配置。

---

## 扩展与进阶

- **自定义数据类型**
  - 新建一个脚本继承 `BaseGameEvent<T>`，并为 `T` 提供 `[Serializable]` 的结构或类。
  - 若需要 Inspector 友好的响应事件，可额外声明 `UnityEvent<T>` 的派生类。
  - 可同时创建对应的 Listener/Raiser（通常只需一行空类）。

- **运行时动态注册**
  - 任何实现 `IGameEventListener<T>` 的对象都可以在代码中手动注册到事件。
  - 例如：`myEvent.RegisterListener(customListener);`

- **常见问题**
  1. _未触发回调？_ → 检查触发器与监听器是否引用了同一个事件资产。
  2. _Inspector 中没有参数输入框？_ → 确保使用的 Listener 与 Raiser 类型与事件泛型一致。
  3. _需要多个参数？_ → 定义携带多个字段的结构体，并创建对应的事件类型。

---

## 关联脚本中的关键字段

- `PanelManager`
  - `请求面板事件 (StringGameEvent)`：外部请求切换入口。
  - `面板切换事件 (PanelChangedGameEvent)`：成功切换后广播。

- `FeelButtonFSM`
  - `事件响应/面板切换事件`：接收面板切换推送。
  - `面板专属动效预设`：基于面板名称覆盖 hover/idle 动效，可结合 `GamePanelLibrarySO` 快速配置。

完成上述配置后，即可在完全解耦的架构下，通过 Inspector 拖拽实现灵活的 UI → 逻辑连接，满足策划与程序的双重诉求。祝使用愉快！

