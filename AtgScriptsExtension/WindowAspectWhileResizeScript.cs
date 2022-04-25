using System;
using System.Windows.Forms;

namespace AtgScriptsExtension
{
    public class WindowAspectWhileResizeScript : MpvNetScriptBase
    {
        const string OptionName = "atg_invert-keepaspect-window-on-key";
        const string PropName = "keepaspect-window";
        private bool m_isResizing = false;
        private bool m_isKeepAspectInitial = false;

        public WindowAspectWhileResizeScript()
        {
            if (!mpvnet.App.Conf.TryGetValue(OptionName, out string val)) return;
            if (!bool.TryParse(val, out bool isEnabled)) return;
            if (!isEnabled) return;

            var form = mpvnet.MainForm.Instance;
            form.ResizeBegin += (s, v) =>
            {
                m_isResizing = true;
                m_isKeepAspectInitial = m_core.GetPropertyBool(PropName);
            };
            form.ResizeEnd += (s, v) =>
            {
                m_isResizing = false;
                m_core.SetPropertyBool(PropName, m_isKeepAspectInitial);
            };
            form.Resize += OnResize;
        }

        private void OnResize(object sender, EventArgs e)
        {
            if (!m_isResizing) return;

            bool isKeyPressed = mpvnet.MainForm.ModifierKeys == Keys.Control;
            m_core.SetPropertyBool(PropName, isKeyPressed ? !m_isKeepAspectInitial : m_isKeepAspectInitial);
        }
    }
}
