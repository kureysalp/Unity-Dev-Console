# Changelog

## [0.1.3] - 2026-09-30

- Minimum Unity version lowered from 6000.0 to 2021.3 LTS, and the Input System dependency from 1.11.0 to 1.7.0.

## [0.1.2] - 2026-09-30

- The console toggles with the key under Esc on every keyboard layout, and the character that key types no longer reaches the input line.

## [0.1.1] - 2026-09-26

- Renamed the package to `com.alpthedev.dev-console` and the namespace and assemblies to `AlpTheDev.DevConsole`.

## [0.1.0] - 2026-09-24

- First release.
- `DevConsole`, `DevConsoleUI`, `ConsoleCommand`, `ConsoleCommand<T>`, `ConsoleCommandRegistry`, `ConsoleArgumentParser`.
- Segment-prefix suggestions under the input line, with Up/Down navigation and Tab/Enter to accept.
- `[ConsoleCommandSet]` discovery, so a game's command class registers without the package naming it.
- Built-in commands: `help`, `clear`, `quit`, `set_time_scale`.
