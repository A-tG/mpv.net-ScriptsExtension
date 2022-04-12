using mpvnet;

namespace AtgScriptsExtension
{
    abstract public class MpvNetScriptBase
    {
        public readonly string Name = "";

        protected readonly CorePlayer m_core = Global.Core;

        public MpvNetScriptBase(string name)
        {
            Name = name;
        }
    }
}
