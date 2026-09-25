# Dev Console

A drop-down developer console for Unity, for the editor and development builds only.

- **Backquote** Toggles the console. The key is fixed, so it works on any keyboard layout.
- **Self-registering commands.** Declare a `static readonly` field and the command exists. There is no scene object to add and no list to maintain.
- **One typed argument per command**, parsed strictly: `string`, `int`, `float`, `bool` (`true`/`false` only), `Vector3` (`"1 2 3"`) and any enum.
- **Suggestions as you type.** Each typed word matches the start of a command segment, in order: `se pl he` finds `set_player_health`, and `se pla` finds every `set_player_*`. Up/Down move the highlight, and Tab or Enter accepts it.
- **Compiled out of release builds.** The `AlpTheDev.DevConsole` assembly only exists under `UNITY_EDITOR || DEVELOPMENT_BUILD`.

## Requirements

- Unity 6000.0 or newer
- Input System, set as the active input handler (pulled in by the package)

## Install

**Window > Package Manager > + > Add package from git URL**:

```
https://github.com/kureysalp/Unity-Dev-Console.git
```

Press Play and hit backquote. The console bootstraps itself after the first scene loads.

## Built-in commands

| Command | Does |
|---|---|
| `help` | Lists every command |
| `clear` | Clears the console log |
| `quit` | Leaves play mode, or exits a build |
| `set_time_scale <float>` | Sets `Time.timeScale` |

## Adding commands

Put your commands in a static class marked `[ConsoleCommandSet]`, one `public static readonly` field per command. At startup the console finds every such class, in any assembly that references `AlpTheDev.DevConsole`, and runs its static constructor, so the fields register themselves.

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AlpTheDev.DevConsole;
using UnityEngine;

[ConsoleCommandSet]
public static class GameConsoleCommands
{
    public static readonly ConsoleCommand<float> SET_GRAVITY = new(
        "set_gravity",
        "Sets downward gravity in m/s^2.",
        "set_gravity <float>",
        SetGravity);

    private static void SetGravity(float metresPerSecondSquared)
    {
        Physics.gravity = Vector3.down * metresPerSecondSquared;
        DevConsole.Log($"gravity {metresPerSecondSquared:0.###}");
    }
}
#endif
```

- `ConsoleCommand` takes no argument and rejects one. `ConsoleCommand<T>` requires one and converts it with `ConsoleArgumentParser`.
- Report from the handler with `DevConsole.Log` and `DevConsole.LogError`. Both accept rich-text colour tags.
- A duplicate id logs an error and keeps the first one registered.
- Wrap your command files in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, because the package assembly does not exist in a release build.

## Blocking game input while it is open

`DevConsoleUI.IsOpen` is true while the console has focus. Check it wherever your game reads input:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
if (AlpTheDev.DevConsole.DevConsoleUI.IsOpen) return;
#endif
```

The console unlocks and shows the cursor while it is open, and restores the previous cursor state when it closes.

## Running commands from code

`DevConsole.Execute("set_time_scale 0.5")` runs a line exactly as if it had been typed, echo included. `DevConsole.GetText()` returns the log, which is useful for editor automation and tests.
