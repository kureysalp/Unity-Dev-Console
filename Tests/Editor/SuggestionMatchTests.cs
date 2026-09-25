using NUnit.Framework;

namespace AlpTheDev.DevConsole.Tests
{
    public class SuggestionMatchTests
    {
        [Test]
        public void EveryTokenPrefixingASegmentInOrderMatches()
        {
            Assert.IsTrue(ConsoleCommandRegistry.TryMatchSegments("set_player_health", new[] { "se", "pl", "he" }, out var skipped));
            Assert.AreEqual(0, skipped);
        }

        [Test]
        public void FewerTokensMatchEveryCommandSharingTheLeadingSegments()
        {
            Assert.IsTrue(ConsoleCommandRegistry.TryMatchSegments("set_player_health", new[] { "se", "pla" }, out _));
            Assert.IsTrue(ConsoleCommandRegistry.TryMatchSegments("set_player_speed", new[] { "se", "pla" }, out _));
            Assert.IsFalse(ConsoleCommandRegistry.TryMatchSegments("set_time_scale", new[] { "se", "pla" }, out _));
        }

        [Test]
        public void MatchingIgnoresCase()
        {
            Assert.IsTrue(ConsoleCommandRegistry.TryMatchSegments("set_player_health", new[] { "SET", "Pl" }, out _));
        }

        [Test]
        public void SkippedSegmentsStillMatchButCountAgainstTheRank()
        {
            Assert.IsTrue(ConsoleCommandRegistry.TryMatchSegments("set_player_health", new[] { "se", "he" }, out var skipped));
            Assert.AreEqual(1, skipped);
        }

        [Test]
        public void TokensOutOfOrderDoNotMatch()
        {
            Assert.IsFalse(ConsoleCommandRegistry.TryMatchSegments("set_player_health", new[] { "he", "se" }, out _));
        }

        [Test]
        public void ATokenLongerThanItsSegmentDoesNotMatch()
        {
            Assert.IsFalse(ConsoleCommandRegistry.TryMatchSegments("set_player_health", new[] { "setp" }, out _));
        }
    }
}
