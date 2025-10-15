# UIRouter 节点过滤

在同一层存在多个监督（Layer Manager）时，可以通过节点过滤来限制哪些面板会在层切换时被激活：

- **Settings 面板**：`activationFilter = MatchExactId`，`nodeIdOrPrefix = "Settings"`
- **Save 面板**：`activationFilter = MatchExactId`，`nodeIdOrPrefix = "Save"`

若监督负责一组子页，可改用前缀匹配，例如：

- `activationFilter = MatchPrefix`
- `nodeIdOrPrefix = "Settings/"`

这样在执行 `UIRouter.GoTo("Pause/Settings/Audio", PrimaryMenu)` 时，只会激活匹配此前缀的监督。需要的 payload 可以在 `onLayerEntered` 内读取，深链路径解析已经由 `UIRoute.BuildPath()` 处理。

路由切换前，Router 会统一执行 `Kill(false)` 以清理残留的过渡动画，因此无需额外处理。
