﻿using mpvnet;
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

        private void OnMessage(string[] args)
        {
            if (args?.Length == 0) return;

            if (args[0] != Name) return;

            var argsWithoutScriptName = (args.Length > 1) ? 
                args.Skip(1).ToArray() :
                Array.Empty<string>();
            MessageReceived?.Invoke(argsWithoutScriptName);
        }
    }
}