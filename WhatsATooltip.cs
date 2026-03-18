using System;
using ExileCore;

namespace WhatsATooltip;

public class WhatsATooltip : BaseSettingsPlugin<WhatsATooltipSettings>
{
    public override bool Initialise()
    {
        // Register all rendering methods on PluginBridge — "PluginName.MethodName" convention.
        // Delegate types use only primitives/IntPtr so they cross assembly boundaries safely.

        GameController.PluginBridge.SaveMethod("WhatsATooltip.Begin",
            (Func<uint, uint, uint, uint, uint, float, bool>)RichTooltip.Begin);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.End",
            (Action)RichTooltip.End);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.Header",
            (Action<string, IntPtr, float>)RichTooltip.Header);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.Badge",
            (Action<string, uint>)RichTooltip.Badge);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.Separator",
            (Action)RichTooltip.Separator);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.StatLine",
            (Action<string, string, uint>)RichTooltip.StatLine);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.Body",
            (Action<string>)RichTooltip.Body);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.BodyBright",
            (Action<string>)RichTooltip.BodyBright);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.Bullet",
            (Action<string, uint>)RichTooltip.Bullet);

        // Convenience methods for common tooltip patterns
        GameController.PluginBridge.SaveMethod("WhatsATooltip.ForCcEntry",
            (Action<string, string, string[], IntPtr, uint,
                    uint, uint, uint, uint, uint>)RichTooltip.ForCcEntry);
        GameController.PluginBridge.SaveMethod("WhatsATooltip.ForLiveBuff",
            (Action<string, string, float, int, bool,
                    uint, uint, uint, uint, uint>)RichTooltip.ForLiveBuff);

        return true;
    }
}
