namespace AlpTheDev.DevConsole
{
    public abstract class ConsoleCommandBase
    {
        public string CommandId { get; }
        public string CommandDescription { get; }
        public string CommandFormat { get; }

        protected ConsoleCommandBase(string commandId, string commandDescription, string commandFormat)
        {
            CommandId = commandId;
            CommandDescription = commandDescription;
            CommandFormat = commandFormat;

            ConsoleCommandRegistry.Add(this);
        }

        public abstract bool TryExecute(string argument, out string error);
    }
}
