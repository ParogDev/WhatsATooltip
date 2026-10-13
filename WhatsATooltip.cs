using ExileCore;

namespace WhatsATooltip;

public class WhatsATooltip : BaseSettingsPlugin<WhatsATooltipSettings>
{
    public override bool Initialise()
    {
        Name = "Whats A Tooltip";
        // RichTooltip's [BridgeExport] methods go on PluginBridge ("PluginName.MethodName"), each as the Func/Action its
        // signature gives. Their types use only primitives/IntPtr so they cross assembly boundaries safely.
        BridgeExports.Register(this, GameController.PluginBridge);
        return true;
    }
}
