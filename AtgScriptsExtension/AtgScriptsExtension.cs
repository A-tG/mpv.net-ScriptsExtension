using mpvnet;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace AtgScriptsExtension
{
    [Export(typeof(IExtension))]
    public class AtgScriptsExtension : IExtension
    {
        readonly MpvNetScriptBase[] scripts = {
            // Add to input.conf
            // Ctrl+c script-message atg_screenshot-to-clipboard [flags]
            new ScreenshotToClipboardScript("atg_screenshot-to-clipboard"),
            // Ctrl+c script-message atg_cycle-pause
            new CyclePauseScript("atg_cycle-pause")
        };
    }
}
