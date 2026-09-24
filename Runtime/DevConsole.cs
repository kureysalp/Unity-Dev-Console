using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Alp.DevConsole
{
    public static class DevConsole
    {
        private const int MaxLines = 256;
        private const string EchoColor = "#8fb8ff";
        private const string ErrorColor = "#ff6b6b";
        private static readonly List<string> _lines = new();
        private static readonly StringBuilder _builder = new();

        private static string _cachedText = string.Empty;
        private static bool _isTextDirty = true;

        public static int Revision { get; private set; }

        public static void Log(string message)
        {
            AddLine(message);
        }

        public static void LogError(string message)
        {
            AddLine($"<color={ErrorColor}>{message}</color>");
        }

        public static void Clear()
        {
            _lines.Clear();
            MarkDirty();
        }

        public static string GetText()
        {
            if (!_isTextDirty) return _cachedText;

            _builder.Clear();
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0) _builder.Append('\n');
                _builder.Append(_lines[i]);
            }

            _cachedText = _builder.ToString();
            _isTextDirty = false;
            return _cachedText;
        }

        public static void Execute(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            string trimmed = line.Trim();
            AddLine($"<color={EchoColor}>> {trimmed}</color>");

            int split = trimmed.IndexOf(' ');
            string commandId = split < 0 ? trimmed : trimmed.Substring(0, split);
            string argument = split < 0 ? string.Empty : trimmed.Substring(split + 1).Trim();

            if (!ConsoleCommandRegistry.TryGet(commandId, out ConsoleCommandBase command))
            {
                LogError($"unknown command: {commandId}");
                return;
            }

            if (!command.TryExecute(argument, out string error))
                LogError(error);
        }

        private static void AddLine(string line)
        {
            _lines.Add(line ?? string.Empty);

            if (_lines.Count > MaxLines)
                _lines.RemoveAt(0);

            MarkDirty();
        }

        private static void MarkDirty()
        {
            _isTextDirty = true;
            Revision++;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            RegisterCommandSets();

            var host = new GameObject("[DevConsole]");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<DevConsoleUI>();
        }

        private static void RegisterCommandSets()
        {
            var packageAssembly = typeof(DevConsole).Assembly;
            var packageName = packageAssembly.GetName().Name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly != packageAssembly && !References(assembly, packageName)) continue;

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsDefined(typeof(ConsoleCommandSetAttribute), false))
                        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
            }
        }

        private static bool References(Assembly assembly, string assemblyName)
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == assemblyName) return true;
            }

            return false;
        }
    }
}
