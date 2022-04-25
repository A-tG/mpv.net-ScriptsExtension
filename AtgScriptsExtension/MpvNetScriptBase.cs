using mpvnet;
using System;
using System.Linq;

namespace AtgScriptsExtension
{
    abstract public class MpvNetScriptBase
    {
        public readonly string Name = "";

        protected readonly CorePlayer m_core = Global.Core;
        protected Action<string[]> MessageReceived;

        public MpvNetScriptBase(string name)
        {
            Name = name;
            m_core.ClientMessage += OnMessage;
        }

        public MpvNetScriptBase() { }

        private void OnMessage(string[] args)
        {
            bool isEmpty = !(args?.Length > 0);
            if (isEmpty || (args[0] != Name)) return;

            var argsWithoutScriptName = args.Skip(1).ToArray();
            MessageReceived?.Invoke(argsWithoutScriptName);
        }
    }
}
