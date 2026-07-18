using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SphereFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        // Important Values
        public int Intensity { get; private set; }
        public string SphereType { get; private set; }
        public SphereFactionDifficulty Difficulty { get; private set; }

        #region Constants
        public const string FactionField_SphereType = "SphereType";

        public const string Tag_DysonSphere = "DysonSphere";
        public const string Tag_GraySphere = "GreySphere";
        public const string Tag_ChromaticSphere = "ChromaticSphere";
        public const string Tag_DarkSphere = "DarkSpireSphere";
        public const string Tag_ZenithSphere = "ZenithSphere";

        public const string Tag_ZenithAntagonizer = "DysonAntagonizer";
        public const string Tag_ChromaticAntagonizer = "ChromaticAntagonizer";

        public const byte MaxUnitTier = 3;

        public const string SphereType_GraySpire = "Gray";
        public const string SphereType_ChromaticSpire = "Chromatic";
        public const string SphereType_ImperialSpire = "Imperial";
        public const string SphereType_DarkSpire = "Dark";
        public const string SphereType_Zenith = "Zenith";

        private const string ZenithDysonSphereFactionInternalName = "ZenithDysonSphere";
        #endregion

        #region Serialized
        public int GameSecondLastAnnoyanceStarted;
        public int GameSecondLastAngered;
        public int GameSecondLastHacked;
        public int GameSecondLastAIAntagonizerWasKilled;
        public byte TimesHackedForUnits;
        public byte TimesHackedForHops;
        public byte TimesHackedForBudget;
        public byte TimesHackedForStrength;
        private readonly List<FInt> BudgetByTier = List<FInt>.Create_WillNeverBeGCed( 3, "SphereFactionBaseInfo-BudgetByTier", 1 );
        public FInt AddedBudgetMultiplierFromExternalSources;
        public FInt AddedMaxStrengthMultiplierFromExternalSources;
        #endregion

        #region Non Serialized
        public DoubleBufferedValue<GameEntity_Squad> Sphere = new DoubleBufferedValue<GameEntity_Squad>( null );
        public DoubleBufferedValue<int> Strength = new DoubleBufferedValue<int>( 0 );
        public DoubleBufferedList<SafeSquadWrapper> MilitaryUnits = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 100, "SphereFaction-BaseInfo-MilitaryUnits" );
        public DoubleBufferedList<Planet> PlanetsWithUnits = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 20, "SphereFaction-BaseInfo-PlanetsWithUnits" );
        public DoubleBufferedValue<GameEntity_Squad> DysonAntagonizer = new DoubleBufferedValue<GameEntity_Squad>( null );

        private bool? DysonFoundLastSecond;
        //private bool PlayerIsNear;

        public static int OldDysonFactionsLeftToProcess = 0;

        // Following can't change once determined. Regenerate on load to avoid serializing strings.
        private string cachedValue_globalUnitTag;
        private string cachedValue_tier0UnitTag;
        private string cachedValue_tier1UnitTag;
        private string cachedValue_tier2UnitTag;
        private string cachedValue_sphereTag;
        #endregion

        // Various utilities.
        #region Annoyance
        public int SecondsSinceBeingAnnoyed => GameSecondLastAnnoyanceStarted != 0 ? World_AIW2.Instance.GameSecond - GameSecondLastAnnoyanceStarted : -1;
        public bool IsCurrentlyAnnoyed => SecondsSinceBeingAnnoyed >= 0;

        public void SetIsCurrentlyBeingAnnoyed()
        {
            if ( GameSecondLastAnnoyanceStarted == 0 )
                GameSecondLastAnnoyanceStarted = World_AIW2.Instance.GameSecond;
        }
        public void SetIsNotCurrentlyBeingAnnoyed()
        {
            // If we're either not in an angry state, or we've been angry long enough, stop being as such.
            if ( GameSecondLastAnnoyanceStarted != 0 )
                GameSecondLastAnnoyanceStarted = 0;
        }

        public int GetSecondsOfAnnoyanceUntilAngry( bool forSpherePlanet )
        {
            switch ( SphereType )
            {
                case SphereType_DarkSpire:
                    return 30; // No chill.
                case SphereType_ImperialSpire:
                    if ( forSpherePlanet )
                        return 120; // Protective of their Sphere. Given a short notice to go away.
                    else
                        return 600; // Generous timer as long as they aren't on the Sphere planet.
                case SphereType_ChromaticSpire:
                    if ( forSpherePlanet )
                        return 60; // Very defensive of Sphere Planet
                    else
                        return 180; // Still annoyed, but gives the player a, small, chance to fall back.
                case SphereType_GraySpire:
                    if ( forSpherePlanet )
                        return 180; // Very trusting, for the most part, but they still don't want you hanging around near the sphere for too long.
                    else
                        return -1; // Otherwise? They don't mind.
                case SphereType_Zenith:
                    if ( forSpherePlanet )
                        return 600; // Very chill, for the most part. They get nervous though.
                    else
                        return -1;
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( $"Unknown sphere type of {SphereType}", Verbosity.ShowAsError );
                    return -1;
            }
        }

        public int GetStrengthToBeAnnoyedAt( bool forSpherePlanet )
        {
            switch ( SphereType )
            {
                case SphereType_DarkSpire: // Actually has 0 chill, but 0 leads to weird logic.
                    return 1;
                case SphereType_ImperialSpire: // Somewhat merciful.
                    return (forSpherePlanet ? GetMaxStrength / 10 : GetMaxStrength / 4).GetNearestIntPreferringHigher();
                case SphereType_ChromaticSpire: // Pretty much no mercy.
                    return (forSpherePlanet ? GetMaxStrength / 25 : GetMaxStrength / 10).GetNearestIntPreferringHigher();
                case SphereType_GraySpire:
                case SphereType_Zenith:
                    return (GetMaxStrength / 2).GetNearestIntPreferringHigher();
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( $"Unknown sphere type of {SphereType}", Verbosity.ShowAsError );
                    return -1;
            }
        }
        #endregion

        #region Anger From Annoyance
        public int SecondsSinceBeingAngry => GameSecondLastAngered != 0 ? World_AIW2.Instance.GameSecond - GameSecondLastAngered : -1;
        public bool IsCurrentlyAngry => SecondsSinceBeingAngry >= 0 && SecondsSinceBeingAngry < AngerDurationInSeconds;

        public int AngerDurationInSeconds
        {
            get
            {
                switch ( SphereType )
                {
                    case SphereType_DarkSpire:
                        return 120; // Quick to cool off and go back to chewing the AI.
                    case SphereType_ImperialSpire:
                        return 600; // Holds a grudge.
                    case SphereType_ChromaticSpire:
                        return 900; // Holds a bigger grudge.
                    case SphereType_GraySpire:
                    case SphereType_Zenith:
                        return 300; // Is cautious for a while.
                    default:
                        ArcenDebugging.ArcenDebugLogSingleLine( $"Unknown sphere type of {SphereType}", Verbosity.ShowAsError );
                        return -1;
                }
            }
        }
        #endregion

        #region Anger From Hacking
        public int SecondsSinceLastHack => GameSecondLastHacked != 0 ? World_AIW2.Instance.GameSecond - GameSecondLastHacked : -1;
        public bool IsCurrentlyAngryDueToHack => SecondsSinceLastHack >= 0 && SecondsSinceLastHack < HackedAngerDurationInSeconds;

        public int HackedAngerDurationInSeconds
        {
            get
            {
                switch ( SphereType )
                {
                    case SphereType_DarkSpire:
                        return Difficulty.DurationOfAngerFromHack_Dark;
                    case SphereType_ImperialSpire:
                        return Difficulty.DurationOfAngerFromHack_Imperial;
                    case SphereType_ChromaticSpire:
                        return Difficulty.DurationOfAngerFromHack_Chromatic;
                    case SphereType_GraySpire:
                        return Difficulty.DurationOfAngerFromHack_Gray;
                    case SphereType_Zenith:
                        return Difficulty.DurationOfAngerFromHack_Zenith;
                    default:
                        ArcenDebugging.ArcenDebugLogSingleLine( $"Unknown sphere type of {SphereType}", Verbosity.ShowAsError );
                        return -1;
                }
            }
        }
        #endregion

        #region Anger From AI Antagonizer
        public bool CanBeAntagonizedByAI => SphereType == SphereType_Zenith && Difficulty.CanAntagonizerSpawn;
        public bool CanBeAntagonizedByHumans => SphereType == SphereType_ChromaticSpire;
        public int SecondsSinceLastAntagonizerDied => World_AIW2.Instance.GameSecond - GameSecondLastAIAntagonizerWasKilled;

        public bool ShouldSpawnAntagonizer => CanBeAntagonizedByAI && Difficulty.CanAntagonizerSpawn && SecondsSinceLastAntagonizerDied >= Difficulty.CooldownBetweenAntagonizerSpawns;

        public bool IsAntagonized => DysonAntagonizer.Display != null && Sphere.Display != null && DysonAntagonizer.Display.GetSecondsSinceCreation() >= SecondsAntagonizerMustLiveBeforeActivating( Sphere.Display, DysonAntagonizer.Display );
        public int SecondsAntagonizerMustLiveBeforeActivating( GameEntity_Squad sphere, GameEntity_Squad antagonizer ) => Difficulty.SecondsForAntagonizerToActivate_Base + Difficulty.SecondsForAntagonizerToActivate_IncreasePerHop * sphere.Planet.GetHopsTo( antagonizer.Planet );
        #endregion

        #region Budget Access
        public void ClearBudget()
        {
            BudgetByTier.Clear();
            while ( BudgetByTier.Count < MaxUnitTier )
                BudgetByTier.Add( FInt.Zero );
        }
        public FInt GetStoredBudgetForTier( int tier )
        {
            if ( tier >= MaxUnitTier )
                ArcenDebugging.ArcenDebugLog( $"Attemped to get Sphere budget for tier {tier}, max is {MaxUnitTier - 1}.", Verbosity.ShowAsError );
            return BudgetByTier[tier];
        }
        public void ChangeStoredBudgetForTier( int tier, int value, bool addToInsteadOfChange = false )
        {
            if ( tier >= MaxUnitTier )
                ArcenDebugging.ArcenDebugLog( $"Attemped to get Sphere budget for tier {tier}, max is {MaxUnitTier - 1}.", Verbosity.ShowAsError );
            if ( addToInsteadOfChange )
                BudgetByTier[tier] += value;
            else
                BudgetByTier[tier] = FInt.Zero + value;
        }
        public void ChangeStoredBudgetForTier( int tier, FInt value, bool addToInsteadOfChange = false )
        {
            if ( tier >= MaxUnitTier )
                ArcenDebugging.ArcenDebugLog( $"Attemped to get Sphere budget for tier {tier}, max is {MaxUnitTier - 1}.", Verbosity.ShowAsError );
            if ( addToInsteadOfChange )
                BudgetByTier[tier] += value;
            else
                BudgetByTier[tier] = value;
        }
        #endregion

        #region Budget/Max Strength Calculations
        public FInt PerSecondBudgetBeforeMultiplier => Difficulty.BudgetPerSecond_Base +
            (Difficulty.BudgetPerSecond_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
            (Difficulty.BudgetPerSecond_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600));
        public FInt BudgetMultiplierFromHacks => FInt.One + Difficulty.BudgetPerSecond_MultiplierPerHack * TimesHackedForBudget + AddedBudgetMultiplierFromExternalSources;
        public FInt GetPerSecondBudget => PerSecondBudgetBeforeMultiplier * BudgetMultiplierFromHacks;

        public FInt MaxStrengthBeforeMultiplier => Difficulty.MaxStrength_Base +
            (Difficulty.MaxStrength_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
            (Difficulty.MaxStrength_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600));

        public FInt MaxStrengthMultiplierFromHacks => FInt.One + Difficulty.MaxStrength_MultiplierPerHack * TimesHackedForStrength + AddedMaxStrengthMultiplierFromExternalSources;
        public FInt GetMaxStrength
        {
            get
            {
                FInt baseMaxStrength = MaxStrengthBeforeMultiplier;
                FInt strength = baseMaxStrength * MaxStrengthMultiplierFromHacks;
                if ( IsCurrentlyAngryDueToHack && strength < baseMaxStrength * 2 )
                    strength = baseMaxStrength * 2; // Angry when hacked.
                return strength;
            }
        }
        // Used for Hacking Estimates
        public FInt GetMaxStrengthWhileBeingHacked
        {
            get
            {
                FInt baseMaxStrength = MaxStrengthBeforeMultiplier;
                FInt strength = baseMaxStrength * MaxStrengthMultiplierFromHacks;
                if ( strength < baseMaxStrength * 2 )
                    strength = baseMaxStrength * 2; // Angry when hacked.
                return strength;
            }
        }
        #endregion

        #region Tags
        public string GetGlobalUnitTag() => cachedValue_globalUnitTag;
        public string GetUnitTagForTier( int tier )
        {
            switch ( tier )
            {
                case 0:
                    return cachedValue_tier0UnitTag;
                case 1:
                    return cachedValue_tier1UnitTag;
                default:
                    return cachedValue_tier2UnitTag;
            }
        }
        public string GetSphereTag() => cachedValue_sphereTag;
        #endregion

        #region Misc
        public bool CanSphereBeFriendly
        {
            get
            {
                switch ( SphereType )
                {
                    case SphereType_GraySpire:
                    case SphereType_Zenith:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public int GetBaseHopLimit()
        {
            switch ( SphereType )
            {
                case SphereType_DarkSpire:
                    return 1;
                case SphereType_ChromaticSpire:
                    return 2;
                case SphereType_ImperialSpire:
                    return 1;
                case SphereType_GraySpire:
                    return 4;
                case SphereType_Zenith:
                    return 8; // Very far by default.
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( $"Unknown sphere type of {SphereType}", Verbosity.ShowAsError );
                    return -1;
            }
        }
        public int NormalHopLimit => GetBaseHopLimit() + TimesHackedForHops;
        public int HopLimit => IsAntagonized ? 999 : NormalHopLimit;

        public int TotalTimesHacked => TimesHackedForUnits + TimesHackedForHops + TimesHackedForBudget + TimesHackedForStrength;
        #endregion

        #region Cleanup
        public SphereFactionBaseInfo() => Cleanup();

        protected override void Cleanup()
        {
            Intensity = 0;
            SphereType = null;
            Difficulty = null;

            GameSecondLastAnnoyanceStarted = 0;
            GameSecondLastAngered = 0;
            GameSecondLastHacked = 0;
            GameSecondLastAIAntagonizerWasKilled = 0;
            TimesHackedForUnits = 0;
            TimesHackedForHops = 0;
            TimesHackedForBudget = 0;
            TimesHackedForStrength = 0;
            ClearBudget();
            AddedBudgetMultiplierFromExternalSources = FInt.Zero;
            AddedMaxStrengthMultiplierFromExternalSources = FInt.Zero;

            Sphere.Clear();
            Strength.Clear();
            MilitaryUnits.Clear();
            PlanetsWithUnits.Clear();
            DysonAntagonizer.Clear();

            DysonFoundLastSecond = null;
            //PlayerIsNear = false;

            cachedValue_globalUnitTag = null;
            cachedValue_tier0UnitTag = null;
            cachedValue_tier1UnitTag = null;
            cachedValue_tier2UnitTag = null;
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, GameSecondLastAnnoyanceStarted, "Annoyance Start Second" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, GameSecondLastAngered, "Anger Last Second" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, GameSecondLastHacked, "Hacked Last Second" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, GameSecondLastAIAntagonizerWasKilled, "Last Antagonizer Killed Second" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, TimesHackedForUnits, "Times Hacked Units" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, TimesHackedForHops, "Times Hacked Hops" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, TimesHackedForBudget, "Times Hacked Budget" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, TimesHackedForStrength, "Times Hacked Strength" );

            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)BudgetByTier.Count, "Budget Count" );
            BudgetByTier.ForEach( budget => Buffer.AddFInt( MetaData, budget, "Budget Value" ) );

            Buffer.AddFInt( MetaData, AddedBudgetMultiplierFromExternalSources, "Added Budget Multiplier From External Sources" );
            Buffer.AddFInt( MetaData, AddedMaxStrengthMultiplierFromExternalSources, "Added Strength Multiplier From External Sources" );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            try
            {
                DoRefreshFromFactionSettings();
            }
            catch ( Exception )
            {
                // This is a harmless error, we simply want to do an initial refresh of settings to detect what type of sphere this faction is for to detect if it's Zenith, and from an old save. Skip it.
                //ArcenDebugging.ArcenDebugLogSingleLine( "Error when trying to load faction settings prior to Deserialization. We need to load this in order to detect prior Zenith factions, but loading it this early fails on Spire spheres. Error is not fatal, so is being skipped. If, however, you are having issues loading an old Dyson Sphere save, please report this error to Mantis; " + e.ToString(), Verbosity.DoNotShow );
            }

            #region Old Zenith Dyson Sphere Compatibility
            if ( SphereType == SphereType_Zenith && Buffer.FromGameVersion.GetLessThan( 3, 803 ) )
            {
                OldDysonFactionsLeftToProcess++;

                // We've got a lot of data to sift through. Most we'll be leaving behind.
                if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 3, 702 ) ) //3702_Bugfixes
                    Buffer.ReadFactionIndex_Neg1ToPos( MetaData, "originalFactionIndex" );
                else
                    Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "originalFactionIndex" );
                Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "CurrentStrength" );
                Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "MaxStrength" );

                // Convert to anger.
                Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "AntagonizedTimeRemaining" );

                AddedBudgetMultiplierFromExternalSources = Buffer.ReadFInt( MetaData, "IncomeModifier" ) - 1;

                byte numElements = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "this.metalByTier.Count" );
                ClearBudget();
                for ( int i = 0; i < numElements; i++ )
                {
                    if ( i < MaxUnitTier )
                        ChangeStoredBudgetForTier( i, Buffer.ReadFInt( MetaData, "metalByTier.Value" ) );
                    else
                        Buffer.ReadFInt( MetaData, "metalByTier.Value" );
                }
                Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeOfLastHack" );
                if ( Buffer.ReadBool( MetaData, "HasBeenHackedForShips" ) )
                    TimesHackedForUnits = 1;

                Buffer.ReadFInt( MetaData, "IntensityMultiplier" );

                AddedMaxStrengthMultiplierFromExternalSources = Buffer.ReadFInt( MetaData, "MaxStrengthModifier" ) - 1;


                Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "NumTimesHacked" );
                DysonFoundLastSecond = Buffer.ReadBool( MetaData, "PlayerHasGainedIntel" );
            }
            #endregion
            else
            {
                GameSecondLastAnnoyanceStarted = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Annoyance Start Second" );
                GameSecondLastAngered = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Anger Last Second" );
                GameSecondLastHacked = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Hacked Last Second" );
                if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 3, 805 ) )
                    GameSecondLastAIAntagonizerWasKilled = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Last Antagonizer Killed Second" );
                else
                    GameSecondLastAIAntagonizerWasKilled = World_AIW2.Instance.GameSecond;

                TimesHackedForUnits = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Times Hacked Units" );
                TimesHackedForHops = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Times Hacked Hops" );
                TimesHackedForBudget = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Times Hacked Budget" );
                TimesHackedForStrength = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Times Hacked Strength" );

                ClearBudget();
                byte count = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Budget Count" );
                for ( byte x = 0; x < count; x++ )
                    if ( x < MaxUnitTier )
                        ChangeStoredBudgetForTier( x, Buffer.ReadFInt( MetaData, "Budget Value" ) );
                    else
                        Buffer.ReadFInt( MetaData, "Budget Value Over Tier Limit" );

                AddedBudgetMultiplierFromExternalSources = Buffer.ReadFInt( MetaData, "Percentage Budget Modifier From External Sources" );
                AddedMaxStrengthMultiplierFromExternalSources = Buffer.ReadFInt( MetaData, "Percentage Strength Modifier From External Sources" );

                #region Fixes
                // In this version, we accounted for an oversight that was causing Budget values to be generated faster than they could be spent.
                // Without this reset; it would result in potentially hundreds of ships spawning at once on load, and that's no good.
                if ( Buffer.FromGameVersion.GetLessThan( 3, 805 ) )
                    ClearBudget();
                #endregion
            }
        }
        #endregion

        #region Faction Settings
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant() => Intensity;

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused() { }

        private void LoadTagData()
        {
            switch ( SphereType )
            {
                case SphereType_DarkSpire:
                    cachedValue_globalUnitTag = "DarkSpireSpawn";
                    cachedValue_tier0UnitTag = "WeakDarkSpireSpawn";
                    cachedValue_tier1UnitTag = "MediumDarkSpireSpawn";
                    cachedValue_tier2UnitTag = "StrongDarkSpireSpawn";
                    cachedValue_sphereTag = "DarkSphere";
                    break;
                case SphereType_ImperialSpire:
                    cachedValue_globalUnitTag = "ImperialSpire";
                    cachedValue_tier0UnitTag = "ImperialSpireFrigate";
                    cachedValue_tier1UnitTag = "ImperialSpireDestroyer";
                    cachedValue_tier2UnitTag = "ImperialSpireCruiser";
                    cachedValue_sphereTag = "ImperialSphere";
                    break;
                case SphereType_ChromaticSpire: 
                    cachedValue_globalUnitTag = "ChromaticSpire";
                    cachedValue_tier0UnitTag = "ChromaticSpireTierZero";
                    cachedValue_tier1UnitTag = "ChromaticSpireTierOne";
                    cachedValue_tier2UnitTag = "ChromaticSpireTierTwo";
                    cachedValue_sphereTag = "ChromaticSphere";
                    break;
                case SphereType_GraySpire:
                    cachedValue_globalUnitTag = "GraySpire";
                    cachedValue_tier0UnitTag = "GraySpireTierZero";
                    cachedValue_tier1UnitTag = "GraySpireTierOne";
                    cachedValue_tier2UnitTag = "GraySpireTierTwo";
                    cachedValue_sphereTag = "GreySphere";
                    break;
                case SphereType_Zenith:
                    cachedValue_globalUnitTag = "DysonSpawn";
                    cachedValue_tier0UnitTag = "DysonTierZero";
                    cachedValue_tier1UnitTag = "DysonTierOne";
                    cachedValue_tier2UnitTag = "DysonTierTwo";
                    cachedValue_sphereTag = "ZenithSphere";
                    break;
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( $"Unknown sphere type of {SphereType}", Verbosity.ShowAsError );
                    return;
            }
        }

        protected override void DoRefreshFromFactionSettings()
        {
            // If we're a subfaction added by Splintering Spire, inherit their Intensity.
            if ( AttachedFaction.Config.GetBoolValueForCustomFieldOrDefaultValue( SplinteringSpireFactionBaseInfo.FactionField_SplinteringSpireSubfaction, false ) )
                Intensity = SplinteringSpireFactionBaseInfo.Instance?.Intensity ?? AttachedFaction.Config.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            else
                Intensity = AttachedFaction.Config.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );

            //if ( Difficulty == null )
            Difficulty = SphereFactionDifficultyTable.Instance.GetRowForFaction( AttachedFaction );

            if ( AttachedFaction.SpecialFactionData.InternalName == ZenithDysonSphereFactionInternalName )
                SphereType = SphereType_Zenith;
            else
                SphereType = AttachedFaction.Config.GetStringValueForCustomFieldOrDefaultValue( FactionField_SphereType, true );

            if ( cachedValue_globalUnitTag == null )
                LoadTagData();
        }
        #endregion

        public override void SetStartingFactionRelationships() => AllegianceHelper.EnemyThisFactionToAll( AttachedFaction );

        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            switch ( SphereType )
            {
                case SphereType_ChromaticSpire:
                case SphereType_DarkSpire:
                case SphereType_GraySpire:
                case SphereType_ImperialSpire:
                    if ( Target.TypeData.IsCommandStation && Sphere.Display != null && Sphere.Display.Planet == Target.Planet )
                        return true; // We are the mighty Spire. The AI needs to get off our territory.
                    break;
                default:
                    break;
            }

            return Target.TypeData.GetHasTag( "VengeanceGeneratorVulnerable" ) || Target.TypeData.GetHasTag( "VengeanceGeneratorConquestSpawn" ) || Target.TypeData.GetHasTag( "VengeanceGeneratorLocus" );
        }

        #region Notifier
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            // We'll want to get the count of annoyed and angry Sphere factions, as well as all respective timers.
            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();

            try
            {
                bool useSphereWorldValues = false;

                foreach ( Faction playerFaction in World_AIW2.Instance.Factions )
                {
                    if ( playerFaction.Type != FactionType.Player )
                        continue;

                    if ( Sphere.Display.Planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Self].TotalStrength >= GetStrengthToBeAnnoyedAt( true ) )
                    {
                        useSphereWorldValues = true;
                        break;
                    }
                }

                int timeLeft = -1;
                short maxStrength = (short)Math.Max( 1, GetStrengthToBeAnnoyedAt( useSphereWorldValues ) / 1000 );

                if ( CanBeAntagonizedByAI && DysonAntagonizer.Display != null )
                    timeLeft = SecondsAntagonizerMustLiveBeforeActivating( Sphere.Display, DysonAntagonizer.Display ) - DysonAntagonizer.Display.GetSecondsSinceCreation();
                else if ( IsCurrentlyAngryDueToHack )
                    timeLeft = HackedAngerDurationInSeconds - SecondsSinceLastHack;
                else if ( IsCurrentlyAngry )
                    timeLeft = AngerDurationInSeconds - SecondsSinceBeingAngry;
                else if ( IsCurrentlyAnnoyed )
                    timeLeft = GetSecondsOfAnnoyanceUntilAngry( useSphereWorldValues ) - SecondsSinceBeingAnnoyed;

                if ( !IsCurrentlyAnnoyed && !IsCurrentlyAngryDueToHack && !IsCurrentlyAngry && !IsAntagonized && DysonAntagonizer.Display == null )
                {
                    fillData.ReturnToPool();
                    return; // Don't need the notifier.
                }

                fillData.Faction = AttachedFaction;
                fillData.BoolList.Add( IsCurrentlyAnnoyed );
                fillData.BoolList.Add( IsCurrentlyAngry );
                fillData.BoolList.Add( IsCurrentlyAngryDueToHack );
                fillData.BoolList.Add( useSphereWorldValues );
                fillData.BoolList.Add( IsAntagonized );
                fillData.BoolList.Add( DysonAntagonizer.Display != null );
                fillData.Int64List.Add( timeLeft );
                fillData.Int16List.Add( maxStrength );
                fillData.PlanetList.Add( Sphere.Display?.Planet );
                fillData.PlanetList.Add( DysonAntagonizer.Display?.Planet );

                if ( SphereType != SphereType_Zenith )
                    fillData.StringList.Add( SphereType );
                else
                    fillData.StringList.Add( string.Empty ); // Zenith's name is built into its faction name.

                NotificationNonSim notification = new NotificationNonSim();
                notification.Assign( SphereFactionNotifier.Instance, fillData, "", 0, "Sphere Faction Notifier", SortedNotificationPriorityLevel.Medium );

            }
            catch ( Exception )
            {
                //ArcenDebugging.SingleLineQuickDebug("Failed Sphere Notifier");
                fillData.ReturnToPool();
            }

        }
        #endregion

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            Sphere.ClearConstructionValueForStartingConstruction();
            Strength.ClearConstructionValueForStartingConstruction();
            MilitaryUnits.ClearConstructionListForStartingConstruction();
            PlanetsWithUnits.ClearConstructionListForStartingConstruction();
            DysonAntagonizer.ClearConstructionValueForStartingConstruction();

            Sphere.Construction = AttachedFaction.GetFirstMatching( GetSphereTag(), true, true );
            #region Player Discovery
            if ( DysonFoundLastSecond != true && Sphere.Construction != null )
            {
                bool foundDyson = Sphere.Construction.Planet.IntelLevel > PlanetIntelLevel.Unexplored;
                if ( DysonFoundLastSecond == false && foundDyson )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = Sphere.Construction.Planet;

                    World_AIW2.Instance.QueueChatMessageOrCommand( "You've found a " + AttachedFaction.StartFactionColourForLog() + SphereType + " Dyson Sphere</color> on " + Sphere.Construction.GetPlanetName_Safe() + ".", ChatType.LogToCentralChat, "ArkChiefOfStaff_PlayerGainsDysonIntel", chatHandlerOrNull );
                    switch ( SphereType )
                    {
                        case SphereType_GraySpire:
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SpireSphere_Tip", string.Empty, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SpireSphere_Gray", string.Empty, AttachedFaction, null, Sphere.Construction.Planet,
                                OnClient.DoThisOnHostOnly_WillBeSentToClients );
                            break;
                        case SphereType_ChromaticSpire:
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SpireSphere_Tip", string.Empty, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SpireSphere_Chromatic", string.Empty, AttachedFaction, null, Sphere.Construction.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                            break;
                        default:
                            break;
                    }
                }
                DysonFoundLastSecond = foundDyson;
            }
            #endregion

            #region Antagonizer
            #region AI
            if ( CanBeAntagonizedByAI )
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( faction.Type != FactionType.AI )
                        continue;

                    if ( DysonAntagonizer.Construction != null )
                        break;

                    foreach ( GameEntity_Squad antagonizer in faction.Squads( Tag_ZenithAntagonizer ) )
                    {
                        if ( antagonizer.MinorFactionStackingID == AttachedFaction.FactionIndex )
                        {
                            DysonAntagonizer.Construction = antagonizer;
                            GameSecondLastAIAntagonizerWasKilled = World_AIW2.Instance.GameSecond;
                            break;
                        }
                    }
                }
            #endregion
            #region Humans
            else if ( CanBeAntagonizedByHumans )
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( faction.Type != FactionType.Player )
                        continue;

                    foreach ( GameEntity_Squad antagonizer in faction.Squads( Tag_ChromaticAntagonizer ) )
                    {
                        // In case the player gets cheeky and tries to break their logic by getting multiple Antagonizers, focus on one at a time.
                        if ( DysonAntagonizer.Construction == null || antagonizer.PrimaryKeyID < DysonAntagonizer.Construction.PrimaryKeyID )
                            DysonAntagonizer.Construction = antagonizer;
                    }
                }
            #endregion
            #endregion

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( GetGlobalUnitTag() ) )
            {
                Strength.Construction += entity.GetStrengthOfSelfAndContents();
                MilitaryUnits.AddToConstructionList( entity );

                // See if the player is near any of our units.
                if ( !PlanetsWithUnits.ConstructionContains( entity.Planet ) )
                {
                    PlanetsWithUnits.AddToConstructionList( entity.Planet );
                }
            }

            Sphere.SwitchConstructionToDisplay();
            Strength.SwitchConstructionToDisplay();
            MilitaryUnits.SwitchConstructionToDisplay();
            PlanetsWithUnits.SwitchConstructionToDisplay();
            DysonAntagonizer.SwitchConstructionToDisplay();
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( Sphere.Display != null )
                UpdateAllegiance();
        }

        private void UpdateAllegiance()
        {
            if ( CanBeAntagonizedByHumans && IsAntagonized )
                AllegianceHelper.EnemyThisFactionToAll( AttachedFaction );
            else if ( CanBeAntagonizedByAI && IsAntagonized )
                AllegianceHelper.AllyThisFactionToAI( AttachedFaction );
            else
                UpdateAllegiance_Regular();
        }

        private void UpdateAllegiance_Regular()
        {
            bool isBeingAnnoyedByPlayerOnSpherePlanet = false;
            bool isBeingAnnoyedByPlayerElsewhere = false;
            bool shouldStartOrContinueToBeAngryAtPlayer = false;

            int maxStrengthAtSphere = GetStrengthToBeAnnoyedAt( true );
            int maxStrengthOtherwise = GetStrengthToBeAnnoyedAt( false );

            // For every player faction, see if they're sticking their nose in our business more than we like.
            foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
            {
                if ( workingFaction.Type != FactionType.Player )
                    continue;

                if ( isBeingAnnoyedByPlayerOnSpherePlanet || isBeingAnnoyedByPlayerElsewhere )
                    continue; // Another player is being rude to us already.

                // Sphere check first, its far more limiting.
                if ( Sphere.Display.Planet.GetPlanetFactionForFaction( workingFaction ).DataByStance[FactionStance.Self].TotalStrength >= maxStrengthAtSphere )
                    isBeingAnnoyedByPlayerOnSpherePlanet = true;

                // If we care about non sphere planets, and our sphere planet isn't agitated, check.
                if ( !isBeingAnnoyedByPlayerOnSpherePlanet && GetSecondsOfAnnoyanceUntilAngry( false ) > 0 )
                    foreach ( Planet planet in PlanetsWithUnits.GetDisplayList() )
                    {
                        if ( planet.GetPlanetFactionForFaction( workingFaction ).DataByStance[FactionStance.Self].TotalStrength >= maxStrengthOtherwise )
                        {
                            isBeingAnnoyedByPlayerElsewhere = true;
                            break;
                        }
                    }
            }

            if ( isBeingAnnoyedByPlayerOnSpherePlanet )
            {
                SetIsCurrentlyBeingAnnoyed();
                if ( SecondsSinceBeingAnnoyed >= GetSecondsOfAnnoyanceUntilAngry( true ) )
                    shouldStartOrContinueToBeAngryAtPlayer = true;
            }
            else if ( isBeingAnnoyedByPlayerElsewhere )
            {
                SetIsCurrentlyBeingAnnoyed();
                if ( SecondsSinceBeingAnnoyed >= GetSecondsOfAnnoyanceUntilAngry( false ) )
                    shouldStartOrContinueToBeAngryAtPlayer = true;
            }
            else if ( IsCurrentlyAngry )
                SetIsCurrentlyBeingAnnoyed();
            else
                SetIsNotCurrentlyBeingAnnoyed();

            if ( shouldStartOrContinueToBeAngryAtPlayer )
                GameSecondLastAngered = World_AIW2.Instance.GameSecond;

            if ( IsCurrentlyAngry || IsCurrentlyAngryDueToHack || Sphere.Display?.Planet.GetControllingFactionType() == FactionType.AI )
                AllegianceHelper.EnemyThisFactionToAll( AttachedFaction ); // We want blood.
            else //if ( PlayerIsNear )
                AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
            //else
            //NeutralThisFactionToHumansAndTheirAllies();
        }

        private void NeutralThisFactionToHumansAndTheirAllies()
        {
            // Be allied to any player or player aligned faction that is near us, neutral otherwise.
            foreach ( Faction primaryFaction in World_AIW2.Instance.Factions )
            {
                if ( primaryFaction.Type != FactionType.Player )
                    continue;

                primaryFaction.MakeNeutralTo( AttachedFaction );
                AttachedFaction.MakeNeutralTo( primaryFaction );

                foreach ( Faction secondaryFaction in World_AIW2.Instance.Factions )
                {
                    if ( !primaryFaction.GetIsFriendlyTowards( secondaryFaction ) )
                        continue; // We only want friends of humans here.

                    secondaryFaction.MakeNeutralTo( AttachedFaction );
                    AttachedFaction.MakeNeutralTo( secondaryFaction );
                }
            }
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 10 + (Intensity * 6);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( $" 来自 {SphereType} 尖塔领域的负载" );
            return load;
        }
        #if false
        public void JustFreed()
        {
            debugStage = 2000;
                if ( entity.TypeData.IsCommandStation // thing dying is a command station
                    && FiringSystemOrNull != null ) // the game knows who killed it
                {
                    debugStage = 2100;
                    bool dysonEffectPlayed = false;
                    //First check if killing this controller freed the Dyson Sphere; if so then that message takes priority
                    if ( entity.Planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true ) != null )
                    {
                        dysonEffectPlayed = true;

                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = entity.Planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( entity.GetPlanetName_Safe() + " command station destroyed!", ChatType.LogToCentralChat,
                            "ArkChiefOfStaff_DysonLiberatedFromPlayer", chatHandlerOrNull );
                    }
                    debugStage = 2400;
                    if ( !dysonEffectPlayed )
                    {
                        //                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SFXItemType_NonPositional.PlayerControllerDestroyed );
                    }

                    debugStage = 3000;
                    //Handle the Shark Plots if this is an AI or an AI-allied faction (instigators, hunters, etc)
                    if ( (factionThatKilledEntity.Type == FactionType.AI || factionThatKilledEntity.SpecialFactionData.AlliedToAIByDefault)
                        && entity.SelfBuildingMetalRemaining <= 0 )
                    {
                        debugStage = 3100;
                        //Shark plots aren't triggered off of partially built command stations
                        bool sharkA = World_AIW2.Instance.Setup.GetBoolBySetting( "SharkA" );
                        if ( sharkA )
                        {
                            GlobalAIWorldBaseInfo.Instance.ChangeAIP( ExternalConstants.Instance.SharkAAIPBoost, AIPChangeReason.EntityDeath, entity.TypeData, factionThatKilledEntity.FactionIndex, entity.Planet.Index, -1 );
                        }
                        debugStage = 3200;
                        //Shark B (exo on command station death) is always enabled
                        GlobalGeneralDeepInfoCommandHandler.TriggerSharkB( entity.PlanetFaction.Faction, factionThatKilledEntity, entity, Context );
                    }
                }
        }
        #endif
    }
}
