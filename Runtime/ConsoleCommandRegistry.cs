using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alp.DevConsole
{
    public static class ConsoleCommandRegistry
    {
        private static readonly Dictionary<string, ConsoleCommandBase> _lookup = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ConsoleCommandBase> _commands = new();
        private static readonly List<(int Skipped, ConsoleCommandBase Command)> _candidates = new();
        private static readonly char[] SegmentSeparators = { ' ', '_' };

        private const int MaxSuggestions = 6;

        public static IReadOnlyList<ConsoleCommandBase> Commands => _commands;

        public static void Add(ConsoleCommandBase command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.CommandId)) return;

            if (_lookup.ContainsKey(command.CommandId))
            {
                Debug.LogError($"console command \"{command.CommandId}\" is already registered");
                return;
            }

            _lookup.Add(command.CommandId, command);
            _commands.Add(command);
            _commands.Sort((left, right) => string.Compare(left.CommandId, right.CommandId, StringComparison.OrdinalIgnoreCase));
        }

        public static bool TryGet(string commandId, out ConsoleCommandBase command)
        {
            if (string.IsNullOrEmpty(commandId))
            {
                command = null;
                return false;
            }

            return _lookup.TryGetValue(commandId, out command);
        }

        public static void FindSuggestions(string typed, List<ConsoleCommandBase> results)
        {
            results.Clear();

            if (string.IsNullOrWhiteSpace(typed)) return;

            var tokens = typed.Split(SegmentSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0) return;

            _candidates.Clear();

            foreach (var command in _commands)
            {
                if (TryMatchSegments(command.CommandId, tokens, out var skipped))
                    _candidates.Add((skipped, command));
            }

            _candidates.Sort(CompareCandidates);

            var count = Math.Min(MaxSuggestions, _candidates.Count);

            for (var i = 0; i < count; i++)
                results.Add(_candidates[i].Command);

            _candidates.Clear();
        }

        internal static bool TryMatchSegments(string commandId, string[] tokens, out int skipped)
        {
            var segments = commandId.Split('_', StringSplitOptions.RemoveEmptyEntries);
            var segmentIndex = 0;
            var lastMatchedIndex = -1;

            foreach (var token in tokens)
            {
                while (segmentIndex < segments.Length && !segments[segmentIndex].StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    segmentIndex++;

                if (segmentIndex >= segments.Length)
                {
                    skipped = 0;
                    return false;
                }

                lastMatchedIndex = segmentIndex;
                segmentIndex++;
            }

            skipped = lastMatchedIndex + 1 - tokens.Length;
            return true;
        }

        private static int CompareCandidates((int Skipped, ConsoleCommandBase Command) left, (int Skipped, ConsoleCommandBase Command) right)
        {
            var bySkipped = left.Skipped.CompareTo(right.Skipped);

            if (bySkipped != 0) return bySkipped;

            return string.Compare(left.Command.CommandId, right.Command.CommandId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
