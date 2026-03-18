using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace WhatsATooltip;

public class WhatsATooltipSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);
}
