# agents.md

The role of this file is to describe common mistakes and confusion points that agents might encounter as they work in this project. If you ever encounter something in the project that surprises you, please alert the developer working with you and indicate that this is the case in the AgentMD file to help prevent future agents from having the same issue.

- Surprise noted on 2026-04-06: the pause-menu save/load preview hover chain can be partially lost after migration. In `Main Scene`, save/load slot buttons may still have `Button`/`UIBehaviourProxy` or related UI structure, but the actual hover callbacks to `UISolariBoard` are no longer wired, and serialized preview event data in the old slot prefabs can also be incomplete/corrupted. When debugging save/load UI previews, inspect the runtime hover bridge first instead of assuming the board or textures are broken.
