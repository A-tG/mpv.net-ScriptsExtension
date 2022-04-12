using mpvnet;

namespace AtgScriptsExtension
{
    public class CyclePauseScript : MpvNetScriptBase
    {
        private bool m_isEof = false;

        public CyclePauseScript(string name) : base(name)
        {
            m_core.ObservePropertyBool("eof-reached", (isEof) => m_isEof = isEof);
            m_core.ClientMessage += OnMessage;
        }

        private void OnMessage(string[] args)
        {
            if ((args == null) || (args.Length == 0)) return;

            if (args[0] != Name) return;

            if (m_isEof)
            {
                m_core.CommandV("seek", "0", "absolute");
                if (m_core.Duration != System.TimeSpan.Zero)
                {
                    m_core.CommandV("show-text", "End of file, rewind");
                }
            }
            else
            {
                Commands.PlayPause();
            }
        }
    }
}
