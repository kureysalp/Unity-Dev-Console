using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlpTheDev.DevConsole
{
    public class DevConsoleUI : MonoBehaviour
    {
        private const string InputControlName = "DevConsoleInput";
        private const string Prompt = ">";
        private const float HeightFraction = 0.4f;
        private const float SlideSeconds = 0.15f;
        private const int FontSize = 14;
        private const int SuggestionFormatFontSize = 12;
        private const float SuggestionIdWidthFraction = 0.3f;

        private static readonly Color TextColor = new(0.87f, 0.91f, 0.96f);
        private static readonly Color PromptColor = new(0.56f, 0.72f, 1f);
        private static readonly Color PanelColor = new(0.02f, 0.03f, 0.05f, 0.92f);
        private static readonly Color SuggestionRowColor = new(0.56f, 0.72f, 1f, 0.18f);
        private static readonly Color SuggestionFormatColor = new(0.55f, 0.6f, 0.68f);

        [SerializeField] private Color _suggestionColor = new(0.62f, 0.68f, 0.76f);
        [SerializeField] private Color _suggestionHighlightColor = new(0.56f, 0.72f, 1f);

        private readonly List<string> _history = new();
        private readonly List<ConsoleCommandBase> _suggestions = new();

        public static bool IsOpen { get; private set; }

        private string _line = string.Empty;
        private Vector2 _scroll;
        private bool _isCaretMoveRequested;
        private float _slide;
        private int _historyIndex = -1;
        private int _revision = -1;
        private int _suggestionIndex;

        private CursorLockMode _previousLockState;
        private bool _wasCursorVisible;

        private GUIStyle _logStyle;
        private GUIStyle _promptStyle;
        private GUIStyle _lineStyle;
        private GUIStyle _suggestionStyle;
        private GUIStyle _suggestionFormatStyle;
        private Texture2D _background;
        private Texture2D _suggestionHighlight;

        private void OnDestroy()
        {
            if (IsOpen) RestoreCursor();

            IsOpen = false;

            if (_background != null) Destroy(_background);
            if (_suggestionHighlight != null) Destroy(_suggestionHighlight);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame) SetOpen(!IsOpen);

            float step = SlideSeconds > 0f ? Time.unscaledDeltaTime / SlideSeconds : 1f;
            _slide = Mathf.MoveTowards(_slide, IsOpen ? 1f : 0f, step);
        }

        private void OnGUI()
        {
            if (!IsOpen && _slide <= 0f) return;

            EnsureStyles();

            if (IsOpen) HandleKeys();

            if (_revision != DevConsole.Revision)
            {
                _revision = DevConsole.Revision;
                _scroll.y = float.MaxValue;
            }

            GUI.depth = -1000;
            GUI.FocusControl(IsOpen ? InputControlName : null);

            float height = Screen.height * HeightFraction;
            float offset = (1f - Mathf.SmoothStep(0f, 1f, _slide)) * height;
            Rect area = new(0f, -offset, Screen.width, height);

            GUI.DrawTexture(area, _background);
            GUILayout.BeginArea(area);

            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            GUILayout.Label(DevConsole.GetText(), _logStyle);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Prompt, _promptStyle, GUILayout.ExpandWidth(false));
            GUI.SetNextControlName(InputControlName);
            var typed = GUILayout.TextField(_line, _lineStyle, GUILayout.ExpandWidth(true)).Replace("`", string.Empty);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            if (typed != _line)
            {
                _line = typed;
                _historyIndex = -1;
                RefreshSuggestions();
            }

            DrawSuggestions(area.yMax);

            if (_isCaretMoveRequested && Event.current.type == EventType.Repaint)
            {
                TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                editor.text = _line;
                editor.MoveTextEnd();
                _isCaretMoveRequested = false;
            }
        }

        private void DrawSuggestions(float top)
        {
            if (_suggestions.Count == 0) return;

            var rowHeight = _suggestionStyle.CalcHeight(GUIContent.none, Screen.width);
            var idWidth = Screen.width * SuggestionIdWidthFraction;

            GUI.DrawTexture(new Rect(0f, top, Screen.width, rowHeight * _suggestions.Count), _background);

            for (var i = 0; i < _suggestions.Count; i++)
            {
                var command = _suggestions[i];
                var row = new Rect(0f, top + rowHeight * i, Screen.width, rowHeight);
                var isHighlighted = i == _suggestionIndex;

                if (isHighlighted) GUI.DrawTexture(row, _suggestionHighlight);

                _suggestionStyle.normal.textColor = isHighlighted ? _suggestionHighlightColor : _suggestionColor;
                GUI.Label(new Rect(row.x, row.y, idWidth, row.height), command.CommandId, _suggestionStyle);
                GUI.Label(new Rect(row.x + idWidth, row.y, row.width - idWidth, row.height), DescribeUsage(command), _suggestionFormatStyle);
            }
        }

        private static string DescribeUsage(ConsoleCommandBase command)
        {
            if (string.IsNullOrEmpty(command.CommandFormat)) return command.CommandDescription;

            if (string.IsNullOrEmpty(command.CommandDescription)) return command.CommandFormat;

            return $"{command.CommandFormat}  —  {command.CommandDescription}";
        }

        private void RefreshSuggestions()
        {
            _suggestionIndex = 0;

            var typed = _line.TrimStart();
            var split = typed.IndexOf(' ');
            var hasArgumentSeparator = split > 0;
            var commandId = hasArgumentSeparator ? typed.Substring(0, split) : typed;

            if (hasArgumentSeparator && ConsoleCommandRegistry.TryGet(commandId, out _))
            {
                _suggestions.Clear();
                return;
            }

            ConsoleCommandRegistry.FindSuggestions(typed, _suggestions);

            if (IsOnlySuggestion(commandId)) _suggestions.Clear();
        }

        private bool IsOnlySuggestion(string commandId)
        {
            return _suggestions.Count == 1 && string.Equals(_suggestions[0].CommandId, commandId, StringComparison.OrdinalIgnoreCase);
        }

        private void ClearSuggestions()
        {
            _suggestions.Clear();
            _suggestionIndex = 0;
        }

        private void SetOpen(bool isOpen)
        {
            IsOpen = isOpen;
            _historyIndex = -1;
            ClearSuggestions();

            if (!isOpen)
            {
                RestoreCursor();
                return;
            }

            _previousLockState = Cursor.lockState;
            _wasCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _line = string.Empty;
            _isCaretMoveRequested = true;
            _scroll.y = float.MaxValue;
        }

        private void RestoreCursor()
        {
            Cursor.lockState = _previousLockState;
            Cursor.visible = _wasCursorVisible;
        }

        private void EnsureStyles()
        {
            if (_background == null) _background = CreateTexture(PanelColor);

            if (_suggestionHighlight == null) _suggestionHighlight = CreateTexture(SuggestionRowColor);

            GUI.skin.settings.cursorColor = TextColor;

            _logStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize,
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 2, 2),
                normal = { textColor = TextColor }
            };

            _promptStyle ??= new GUIStyle(_logStyle)
            {
                wordWrap = false,
                padding = new RectOffset(8, 0, 2, 2),
                normal = { textColor = PromptColor }
            };

            _lineStyle ??= new GUIStyle(_logStyle)
            {
                richText = false,
                wordWrap = false,
                padding = new RectOffset(4, 8, 2, 2),
                normal = { background = null, textColor = TextColor },
                hover = { background = null, textColor = TextColor },
                focused = { background = null, textColor = TextColor },
                active = { background = null, textColor = TextColor }
            };

            _suggestionStyle ??= new GUIStyle(_logStyle)
            {
                richText = false,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(20, 8, 3, 3),
                normal = { textColor = _suggestionColor }
            };

            _suggestionFormatStyle ??= new GUIStyle(_suggestionStyle)
            {
                fontSize = SuggestionFormatFontSize,
                padding = new RectOffset(8, 8, 3, 3),
                normal = { textColor = SuggestionFormatColor }
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void HandleKeys()
        {
            Event current = Event.current;

            if (current.type != EventType.KeyDown) return;

            switch (current.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (IsTypingUnknownCommand()) AcceptSuggestion();
                    else Submit();
                    current.Use();
                    break;

                case KeyCode.UpArrow:
                    if (_suggestions.Count > 1) StepSuggestion(-1);
                    else StepHistory(1);
                    current.Use();
                    break;

                case KeyCode.DownArrow:
                    if (_suggestions.Count > 1) StepSuggestion(1);
                    else StepHistory(-1);
                    current.Use();
                    break;

                case KeyCode.Tab:
                    AcceptSuggestion();
                    current.Use();
                    break;

                case KeyCode.Escape:
                    SetOpen(false);
                    current.Use();
                    break;
            }
        }

        private bool IsTypingUnknownCommand()
        {
            if (_suggestions.Count == 0) return false;

            var typed = _line.Trim();
            var split = typed.IndexOf(' ');
            var commandId = split < 0 ? typed : typed.Substring(0, split);

            return !ConsoleCommandRegistry.TryGet(commandId, out _);
        }

        private void StepSuggestion(int direction)
        {
            _suggestionIndex = (_suggestionIndex + direction + _suggestions.Count) % _suggestions.Count;
        }

        private void AcceptSuggestion()
        {
            if (_suggestions.Count == 0) return;

            var command = _suggestions[_suggestionIndex];

            if (_line.TrimStart().StartsWith(command.CommandId + " ", StringComparison.OrdinalIgnoreCase)) return;

            _line = command.CommandId + " ";
            _historyIndex = -1;
            _isCaretMoveRequested = true;
            RefreshSuggestions();
        }

        private void Submit()
        {
            string line = _line.Trim();

            _line = string.Empty;
            _historyIndex = -1;
            _isCaretMoveRequested = true;
            ClearSuggestions();

            if (line.Length == 0) return;

            if (_history.Count == 0 || _history[^1] != line) _history.Add(line);

            DevConsole.Execute(line);
        }

        private void StepHistory(int direction)
        {
            if (_history.Count == 0) return;

            _historyIndex = Mathf.Clamp(_historyIndex + direction, -1, _history.Count - 1);
            _line = _historyIndex < 0 ? string.Empty : _history[_history.Count - 1 - _historyIndex];
            _isCaretMoveRequested = true;
            ClearSuggestions();
        }
    }
}
