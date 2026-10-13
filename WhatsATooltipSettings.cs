using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace WhatsATooltip;

public class WhatsATooltipSettings : ISettings
{
    [Menu("Enable", "Turn the plugin on or off (it serves rich tooltips to other plugins over PluginBridge)")]
    public ToggleNode Enable { get; set; } = new(true);
}
