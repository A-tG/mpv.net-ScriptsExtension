﻿using mpvnet;
using System.ComponentModel.Composition;

namespace AtgScriptsExtension
{
    [Export(typeof(IExtension))]
    public class AtgScriptsExtension : IExtension
    {
        // Add to input.conf
        // Ctrl+c script-message atg_screenshot-to-clipboard [flags]
        ScreenshotToClipboardScript screenshotToClipboardScript = new ScreenshotToClipboardScript();
        // Ctrl+c script-message atg_cycle-pause
        CyclePauseScript cyclePauseSript = new CyclePauseScript();
    }
}
