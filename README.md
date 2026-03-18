# What's a Tooltip?

> A shared dependency that provides rich tooltips across WhatsA plugins -- install and forget.

Part of the **WhatsA** plugin family for ExileApi.

## What It Does

- Provides a shared tooltip rendering service used by other WhatsA plugins (Crowd Control, Mirage, etc.)
- Renders styled tooltip cards with headers, badges, stat lines, and bullet points
- Automatically available to any plugin that needs it -- no configuration required

## Getting Started

1. Download and place in `Plugins/Source/What's a Tooltip/`
2. HUD auto-compiles on next launch
3. Enable in plugin list
4. That's it -- other WhatsA plugins will detect and use it automatically

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Enable | On | Master enable/disable |

No other settings needed. This plugin runs as a background service.

<details>
<summary>Technical Details</summary>

### PluginBridge API

All rendering methods are exposed via `GameController.PluginBridge` using the `WhatsATooltip.*` namespace. This allows other plugins to render tooltips without compile-time dependencies.

**Available methods:**

| Bridge Key | Purpose |
|-----------|---------|
| `WhatsATooltip.Begin` | Initialize tooltip with color scheme and max width |
| `WhatsATooltip.End` | Finalize tooltip rendering |
| `WhatsATooltip.Header` | Header text with optional icon |
| `WhatsATooltip.Badge` | Colored pill badge |
| `WhatsATooltip.Separator` | Horizontal separator line |
| `WhatsATooltip.StatLine` | Label + right-aligned value |
| `WhatsATooltip.Body` | Word-wrapped subdued text |
| `WhatsATooltip.BodyBright` | Word-wrapped bright text |
| `WhatsATooltip.Bullet` | Bullet point with colored dot |
| `WhatsATooltip.ForCcEntry` | Full tooltip for a CC definition (icon, tier, buffs) |
| `WhatsATooltip.ForLiveBuff` | Full tooltip for a live buff (name, timer, charges) |

### Usage from Other Plugins

```csharp
// Get the method via PluginBridge (no compile-time dependency needed)
var begin = GameController.PluginBridge.GetMethod<Action<...>>("WhatsATooltip.Begin");
var header = GameController.PluginBridge.GetMethod<Action<...>>("WhatsATooltip.Header");
var end = GameController.PluginBridge.GetMethod<Action>("WhatsATooltip.End");

// Render a tooltip when an ImGui item is hovered
if (ImGui.IsItemHovered())
{
    begin?.Invoke(accentColor, maxWidth);
    header?.Invoke("Title", iconPtr);
    end?.Invoke();
}
```

### Architecture

- Zero-allocation static API -- only called when `ImGui.IsItemHovered()` is true
- ImGui DrawList-based immediate-mode rendering inside `BeginTooltip`/`EndTooltip`
- All delegate signatures use primitives, strings, and `IntPtr` for safe cross-assembly communication
- Word wrapping with space-based line breaking
- Configurable color scheme: accent, accent dim, label, description, card background

</details>

## About This Project

These plugins are built with AI-assisted development using Claude Code and the
ExileApiScaffolding (private development workspace) workspace.

The developer works professionally in cybersecurity and high-risk software --
AI compensates for a C# knowledge gap specifically, not engineering judgment.
Plugin data comes from the PoE Wiki and PoEDB data mining.

The focus is on UX: friction points and missing expected features that the
existing plugin ecosystem doesn't address. Every hour spent developing is an
hour not spent on league progression, so feedback is the best way to support
the project.

## WhatsA Plugin Family

| Plugin | Description |
|--------|-------------|
| [What's a Breakpoint?](https://github.com/ParogDev/WhatsABreakpoint) | Kinetic Fusillade attack speed breakpoint visualizer |
| [What's a Crowd Control?](https://github.com/ParogDev/WhatsACrowdControl) | OmniCC-style CC effect overlay with timers |
| [What's a Mirage?](https://github.com/ParogDev/WhatsAMirage) | League mechanic overlay for spawners, chests, and wishes |
| [What's a Tincture?](https://github.com/ParogDev/WhatsATincture) | Automated tincture management with burn stack tracking |
| **What's a Tooltip?** | Shared rich tooltip service for WhatsA plugins |
| [What's an AI Bridge?](https://github.com/ParogDev/WhatsAnAiBridge) | File-based IPC for AI-assisted plugin development |
| [What's an Unbound Avatar?](https://github.com/ParogDev/WhatsAnUnboundAvatar) | Auto-activation for Avatar of the Wilds at 100 fury |

Built with ExileApiScaffolding (private development workspace)
