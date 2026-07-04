namespace Arcen.AIW2.External
{
    /// <summary>
    /// Shared semantic color palette for objective (intel menu) tooltips, so the same concept
    /// reads the same everywhere instead of being a scatter of near-duplicate raw hex strings.
    /// These are bare hex (no '#', no &lt;color&gt; tag) for use with buffer.Add( text, color ).
    ///
    /// This palette is intentionally about SEMANTIC ROLES, not faction styling: per-faction accent
    /// colors (e.g. a faction's keyword tint) deliberately stay as their own literals.
    /// </summary>
    public static class ObjectiveColors
    {
        /// <summary>Gains and positive quantities: science, resources, counts, rewards.</summary>
        public const string Reward = "a1ffa1"; // green

        /// <summary>AI Progress increases and other hard downsides. The one true "this costs you" red.</summary>
        public const string AIP = "ff5454"; // medium warning red

        /// <summary>Body-text tips and calls to action.</summary>
        public const string Hint = "ffcc88"; // soft gold

        /// <summary>Inline highlighted nouns within a sentence (unit names, mechanics).</summary>
        public const string Keyword = "ffdd88"; // brighter gold

        /// <summary>Section headers inside a tooltip.</summary>
        public const string Header = "ffdd66"; // gold

        /// <summary>Strong "this triggers something big" warnings.</summary>
        public const string Warning = "ff4400"; // bright orange-red

        /// <summary>De-emphasized asides and parentheticals.</summary>
        public const string Muted = "888888"; // gray
    }
}
