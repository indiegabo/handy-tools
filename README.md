# HandyTools

HandyTools is a modular Unity package built around a small mandatory kernel
and a set of opt-in runtime and editor modules. The package is organized so
humans and AI agents can work on one slice at a time without pulling optional
dependencies back into the root assemblies.

The markdown files under `Docs/` are the canonical package documentation.
HandyTools no longer maintains a separate generated documentation site, so keep
these package-local documents aligned with code changes.

This README is the documentation entry point. Read it first, then follow the
linked documents in the suggested order.

## Documentation Menu

### Core Documentation

- [Package Overview](Docs/00-package-overview.md)
- [Kernel and Boot Flow](Docs/01-kernel-and-boot-flow.md)
- [Editor Setup and Configuration](Docs/02-editor-setup-and-configuration.md)
- [Assembly Layout and Dependency Rules](Docs/03-assembly-layout-and-dependency-rules.md)
- [Utils and Reclassifications](Docs/04-utils-and-reclassifications.md)
- [AI Agent Playbook](Docs/05-ai-agent-playbook.md)
- [Module Authoring Guide](Docs/06-module-authoring-guide.md)

### Module Documentation

- [Configurable Modules](Docs/10-configurable-modules.md)
- [Logging](Docs/10-configurable-modules.md#logging)
- [Input](Docs/10-configurable-modules.md#input)
- [Gameplay](Docs/10-configurable-modules.md#gameplay)
- [Cutscenes](Docs/12-cutscenes-module.md)
- [Conversations](Docs/19-conversations-module.md)
- [Command Pattern](Docs/16-command-pattern-guide.md)
- [FSM](Docs/11-auto-activated-modules.md#fsm)
- [Save System](Docs/10-configurable-modules.md#save-system)
- [Globals](Docs/10-configurable-modules.md#globals)
- [Debugging](Docs/10-configurable-modules.md#debugging)
- [Steam](Docs/10-configurable-modules.md#steam)
- [ScreenShooter](Docs/10-configurable-modules.md#screenshooter)
- [FSM Guide](Docs/15-fsm-guide.md)
- [FSM Module Reference](Docs/FSMModule/README.md)
- [Auto-Activated Modules](Docs/11-auto-activated-modules.md)
- [Animation Events](Docs/14-animation-events-guide.md)
- [Web](Docs/11-auto-activated-modules.md#web)
- [Command Pattern](Docs/11-auto-activated-modules.md#command-pattern)
- [Pooling](Docs/11-auto-activated-modules.md#pooling)
- [Identifying](Docs/11-auto-activated-modules.md#identifying)
- [Rendering](Docs/11-auto-activated-modules.md#rendering)

## Quick Orientation

- The mandatory kernel lives under `Runtime/Scripts/Core`,
  `Runtime/Scripts/EventBus`, and `Runtime/Scripts/ServiceLocator`.
- Module namespaces now follow the `*Module` convention. Use namespaces such as
  `IndieGabo.HandyTools.HandyBusModule`,
  `IndieGabo.HandyTools.HandyServiceLocatorModule`, and
  `IndieGabo.HandyTools.GameplayModule` or
  `IndieGabo.HandyTools.FSMModule` in new code.
- Module activation is driven by `HandyModuleSettings` at
  `Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset`.
- Configurable modules are edited through `Handy Tools/Modules`.
- The `Cutscenes` module now ships a shared modules panel, a director
  inspector, and a graph editor window on top of the runtime slice and
  package samples.
- Auto-activated modules such as Web, Pooling, Identifying, and Rendering do
  not have dedicated editor panels, though they may still expose standalone
  tooling such as the Command Pattern monitor window.
- The root runtime and root editor asmdefs must stay clean. Optional packages
  belong to module-specific asmdefs only.

## Recommended Reading Order

1. [Package Overview](Docs/00-package-overview.md)
2. [Kernel and Boot Flow](Docs/01-kernel-and-boot-flow.md)
3. [Editor Setup and Configuration](Docs/02-editor-setup-and-configuration.md)
4. [Assembly Layout and Dependency Rules](Docs/03-assembly-layout-and-dependency-rules.md)
5. [AI Agent Playbook](Docs/05-ai-agent-playbook.md)
6. [Configurable Modules](Docs/10-configurable-modules.md)
7. [Auto-Activated Modules](Docs/11-auto-activated-modules.md)
8. [Cutscenes Module](Docs/12-cutscenes-module.md)
9. [Conversations Module](Docs/19-conversations-module.md)
10. [ConversationTable Window And Presenter Prefabs](Docs/20-conversation-table-window-and-presenter-prefabs.md)
11. [Command Pattern Guide](Docs/16-command-pattern-guide.md)
12. [FSM Guide](Docs/15-fsm-guide.md)
13. [Animation Events Guide](Docs/14-animation-events-guide.md)

## Documentation Goals

The documents in `Assets/HandyTools/Docs` are written for two audiences at the
same time:

- Unity developers who need to install, configure, and extend the package.
- AI agents that need explicit ownership, boot, and dependency rules before
  changing code safely.

If you change module ownership, boot order, asmdef boundaries, editor menus, or
config asset paths, update the matching document in `Docs` in the same change.

## Licensing

- Main package license: [LICENSE.md](LICENSE.md)
- Third-party notices for redistributed material: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)
- Separate notice texts: [Licenses/README.md](Licenses/README.md)
