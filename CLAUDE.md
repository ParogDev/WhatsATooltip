# CLAUDE.md — WhatsATooltip

## Plugin Context

WhatsATooltip is a shared tooltip rendering service for the WhatsA* plugin family. It provides an immediate-mode rich tooltip API (neon-styled cards with headers, badges, stat lines, bullets, and word-wrapped text) and exposes all rendering methods via `GameController.PluginBridge` so other plugins can call them at runtime without compile-time dependencies.

**Main class**: `WhatsATooltip` inherits `BaseSettingsPlugin<WhatsATooltipSettings>`
**Settings class**: `WhatsATooltipSettings` implements `ISettings`
**Key classes**: `RichTooltip` (static rendering engine)

### Architecture

WhatsATooltip is a "service plugin" — it has no Tick/Render loop of its own. In `Initialise()`, it registers all `RichTooltip` static methods on `PluginBridge` using the `"WhatsATooltip.MethodName"` convention. Consumer plugins (e.g., WhatsACrowdControl, WhatsAMirage) resolve these delegates once via `PluginBridge.GetMethod<T>()` and call them during their own ImGui rendering passes.

All bridge delegate signatures use only primitives, strings, string arrays, and `IntPtr` — no custom structs cross the assembly boundary.

### Lifecycle Methods Used

| Method | Purpose |
|---|---|
| `Initialise()` | Registers all RichTooltip methods on PluginBridge |

### Current Settings

| Setting | Type | Default |
|---|---|---|
| `Enable` | `ToggleNode` | `true` |

### Bridge API

Consumer plugins resolve these delegates from PluginBridge:

| Bridge Key | Delegate Type | Purpose |
|---|---|---|
| `WhatsATooltip.Begin` | `Func<uint, uint, uint, uint, uint, float, bool>` | Begin tooltip (accent, accentDim, label, desc, cardBg, maxWidth) |
| `WhatsATooltip.End` | `Action` | End tooltip |
| `WhatsATooltip.Header` | `Action<string, IntPtr, float>` | Header text with optional icon |
| `WhatsATooltip.Badge` | `Action<string, uint>` | Colored pill badge |
| `WhatsATooltip.Separator` | `Action` | Horizontal line |
| `WhatsATooltip.StatLine` | `Action<string, string, uint>` | Label + right-aligned value |
| `WhatsATooltip.Body` | `Action<string>` | Subdued wrapped text |
| `WhatsATooltip.BodyBright` | `Action<string>` | Bright wrapped text |
| `WhatsATooltip.Bullet` | `Action<string, uint>` | Bullet point with colored dot |
| `WhatsATooltip.ForCcEntry` | `Action<string, string, string[], IntPtr, uint, uint, uint, uint, uint, uint>` | CC entry convenience tooltip |
| `WhatsATooltip.ForLiveBuff` | `Action<string, string, float, int, bool, uint, uint, uint, uint, uint>` | Live buff convenience tooltip |

### Consumer Pattern

```csharp
// Cache delegates once (lazy init, null-safe):
private bool _bridgeChecked;
private Func<uint, uint, uint, uint, uint, float, bool> _ttBegin;
private Action _ttEnd;
// ... etc

private void EnsureBridge(GameController gc)
{
    if (_bridgeChecked) return;
    _ttBegin = gc.PluginBridge.GetMethod<Func<uint, uint, uint, uint, uint, float, bool>>("WhatsATooltip.Begin");
    _ttEnd = gc.PluginBridge.GetMethod<Action>("WhatsATooltip.End");
    // ... resolve all needed delegates
    _bridgeChecked = true;
}

// Use with null-guard (graceful degradation if WhatsATooltip not loaded):
if (ImGui.IsItemHovered() && _ttBegin != null)
{
    _ttBegin(accent, accentDim, label, desc, cardBg, 320f);
    _ttEnd?.Invoke();
}
```

## Project Setup

- This is an ExileApi plugin (game HUD overlay framework for Path of Exile)
- Do not edit anything outside this directory
- Target framework: net10.0-windows, OutputType: Library

### Namespace to DLL Mapping
| DLL | Key Namespaces |
|---|---|
| `ExileCore.dll` | `ExileCore` (BaseSettingsPlugin, GameController, Graphics, Input), `ExileCore.Shared` (Nodes, Enums, Interfaces, Attributes, Helpers), `ExileCore.PoEMemory` (Components, MemoryObjects) |
| `GameOffsets.dll` | `GameOffsets` (offsets structs), `GameOffsets.Native` (Vector2i, NativeStringU) |

## Build & Run

- NO manual build command — Loader.exe auto-compiles from Plugins/Source/
- HUD installation path: resolved from .csproj HintPath (parent dir of ExileCore.dll)
- For IDE support set env var: `exapiPackage` = `<HUD installation path>`

## API Reference

- **Default**: HUD installation (from .csproj HintPath) — compiled DLLs with intellisense
- **Enhanced**: If `.claude/override-path` exists, read it for a path to expanded
  API reference with full type definitions and source. Use that path for deep lookups
  when the compiled DLLs don't provide enough detail about a type, method, or pattern.

## Plugin Anatomy

Every plugin is a C# class library. The main class inherits `BaseSettingsPlugin<TSettings>` and the settings class implements `ISettings`.

### Plugin Lifecycle

| Method | When called | Notes |
|---|---|---|
| `Initialise()` | Once on load | Register hotkeys, wire up `OnPressed`/`OnValueChanged`, return `true` on success |
| `OnLoad()` | After Initialise | Load textures: `Graphics.InitImage("file.png")` |
| `AreaChange(AreaInstance area)` | Zone change | Clear cached entity lists here |
| `Tick()` | Every frame | Return `null` (no async job needed) or a `Job` for background work |
| `Render()` | Every frame | Draw overlays; check `Settings.Enable` and `GameController.InGame` |
| `EntityAdded(Entity entity)` | Entity enters range | Filter and cache relevant entities here |
| `EntityRemoved(Entity entity)` | Entity leaves range | Remove from caches |
| `DrawSettings()` | Settings panel open | Call `base.DrawSettings()` unless fully custom |

## GameController API

`GameController` is the main access point available in all plugin methods:

```csharp
GameController.InGame
GameController.Player
GameController.Area.CurrentArea
GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
GameController.PluginBridge.SaveMethod("MyPlugin.Method", delegate)
GameController.PluginBridge.GetMethod<TDelegate>("OtherPlugin.Method")
GameController.DeltaTime
```

## Inter-Plugin Communication (PluginBridge)

```csharp
// Expose a method
GameController.PluginBridge.SaveMethod("MyPlugin.GetData", (Func<MyData>)GetData);

// Consume another plugin's method
var getData = GameController.PluginBridge.GetMethod<Func<OtherData>>("OtherPlugin.GetData");
if (getData != null) { var data = getData(); }
```

**Rules**:
- Use `"PluginName.MethodName"` convention for bridge keys
- Delegate types must use only primitives, strings, arrays, IntPtr — no custom structs across assembly boundaries
- Consumer plugins should null-guard all bridge delegates for graceful degradation
- Cache resolved delegates (don't call GetMethod every frame)

## Settings Node Types

```csharp
public class MySettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public RangeNode<int> SomeRange { get; set; } = new RangeNode<int>(10, 1, 100);
    public RangeNode<float> SomeFloat { get; set; } = new RangeNode<float>(1.5f, 0f, 10f);
    public ColorNode SomeColor { get; set; } = new ColorNode(Color.White);
    public TextNode SomeText { get; set; } = new TextNode("default");
}
```

## Performance Rules

- NEVER call `GetComponent<T>()` in `Render()` — do it in `Tick()`, store in fields
- Use `EntityAdded`/`EntityRemoved` to maintain filtered entity lists
- Check `entity.IsValid` and `entity.IsAlive` before processing
- Load textures in `OnLoad()`, never in `Render()`
- Separate data reads (`Tick`) from drawing (`Render`)
