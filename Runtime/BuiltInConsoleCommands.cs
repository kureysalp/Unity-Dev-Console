using System.Text;
using UnityEngine;

namespace Alp.DevConsole
{
    [ConsoleCommandSet]
    public static class BuiltInConsoleCommands
    {
        private const int FormatColumnWidth = 30;
        private const float MaxTimeScale = 10f;

        public static readonly ConsoleCommand HELP = new(
            "help",
            "Lists every command.",
            "help",
            Help);

        public static readonly ConsoleCommand CLEAR = new(
            "clear",
            "Clears the console log.",
            "clear",
            DevConsole.Clear);

        public static readonly ConsoleCommand QUIT = new(
            "quit",
            "Leaves play mode, or exits a build.",
            "quit",
            Quit);

        public static readonly ConsoleCommand<float> SET_TIME_SCALE = new(
            "set_time_scale",
            "Sets how fast time runs.",
            "set_time_scale <float>",
            SetTimeScale);

        private static void Help()
        {
            var listing = new StringBuilder();

            foreach (var command in ConsoleCommandRegistry.Commands)
            {
                if (listing.Length > 0) listing.Append('\n');
                listing.Append(command.CommandFormat.PadRight(FormatColumnWidth)).Append(command.CommandDescription);
            }

            DevConsole.Log(listing.ToString());
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void SetTimeScale(float scale)
        {
            if (scale < 0f || scale > MaxTimeScale)
            {
                DevConsole.LogError($"time scale must be between 0 and {MaxTimeScale:0}");
                return;
            }

            Time.timeScale = scale;
            DevConsole.Log($"time scale {scale:0.###}");
        }
    }
}
