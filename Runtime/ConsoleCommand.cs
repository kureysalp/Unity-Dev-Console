using System;

namespace Alp.DevConsole
{
    public class ConsoleCommand : ConsoleCommandBase
    {
        private readonly Action _commandAction;

        public ConsoleCommand(string commandId, string commandDescription, string commandFormat, Action commandAction)
            : base(commandId, commandDescription, commandFormat)
        {
            _commandAction = commandAction;
        }

        public override bool TryExecute(string argument, out string error)
        {
            if (!string.IsNullOrEmpty(argument))
            {
                error = $"{CommandId} takes no parameters - usage: {CommandFormat}";
                return false;
            }

            error = null;
            _commandAction?.Invoke();
            return true;
        }
    }

    public class ConsoleCommand<T> : ConsoleCommandBase
    {
        private readonly Action<T> _commandAction;

        public ConsoleCommand(string commandId, string commandDescription, string commandFormat, Action<T> commandAction)
            : base(commandId, commandDescription, commandFormat)
        {
            _commandAction = commandAction;
        }

        public override bool TryExecute(string argument, out string error)
        {
            if (string.IsNullOrEmpty(argument))
            {
                error = $"{CommandId} needs a parameter - usage: {CommandFormat}";
                return false;
            }

            if (!ConsoleArgumentParser.TryParse(typeof(T), argument, out object parsed))
            {
                error = $"could not read \"{argument}\" as {ConsoleArgumentParser.Describe(typeof(T))} - usage: {CommandFormat}";
                return false;
            }

            error = null;
            _commandAction?.Invoke((T)parsed);
            return true;
        }
    }
}
