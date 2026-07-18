using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZenithArchitraveFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //Set immediately before the expansion-planet sorts so the comparisons can be non-capturing
        //static delegates.  [ThreadStatic] because faction expansion logic runs on worker threads.
        [ThreadStatic]
        private static Faction cb_expansionSortAttachedFaction;

        //Some planets are in the ZA's Territory; if you go there they will spawn endless numbers of ships to come after you
        public readonly List<Planet> Territory = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionBaseInfo-Territory" );
        public readonly List<Planet> PlanetsEverTaken = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionBaseInfo-PlanetsEverTaken" );

        public int TimeForNextPlanetInTerritory;
        public int PioneerSpawnTime; //The ZA spawns Pioneers to take planets outside of its Territory; this is referred to as Expansion Mode
        public int TimesPioneersInterrupted; //Pioneers only spawn if the ZA isn't under attack; this lets us modify the time Pioneers will attack
        public int PioneerSpawnTimeElapsed;

        //has the faction indices of the player factions
        public readonly List<Int16> PlayersArchitraveIsFriendlyToward = List<Int16>.Create_WillNeverBeGCed( 300, "ZenithArchitraveFactionBaseInfo-PlayersArchitraveIsFriendlyToward" );
        //has the faction indices of the player factions
        public readonly List<Int16> PlayersArchitraveHates = List<Int16>.Create_WillNeverBeGCed( 300, "ZenithArchitraveFactionBaseInfo-PlayersArchitraveHates" );
        //has the faction indices of any other Architraves I am at war with (ie "this architrave is Too Big"). If I am too big then this isn't set, but ShouldOtherArchitravesAttackMe will be set
        public readonly List<Int16> CivilWarEnemies = List<Int16>.Create_WillNeverBeGCed( 300, "ZenithArchitraveFactionBaseInfo-CivilWarEnemies" );
        //has the faction indices of ZenithAwakening (now the Dark Zenith) factions I am at war with. Note this is Unused, and its not clear if it ever will be
        public readonly List<Int16> ZenithAwakeningWarEnemies = List<Int16>.Create_WillNeverBeGCed( 300, "ZenithArchitraveFactionBaseInfo-ZenithAwakeningWarEnemies" ); 

        public int WhenIBeganCivilWar; //ie "When I got Too Big"
        public int MetalReserves = -1; //for building regular ships
        public int GolemMetalReserves = -1; //the Architrave can also buy full Golems when in civil war, or its enemies are very strong
        public int DefensiveMetalReserves = -1; //used to buy defensive structures
        public int TimeEnteredWarFooting;
        public bool HasDoneInitialStrike; //when the ZA initially spawns, we cripple the AI units on the planet to make it easy to capture the first planet
        public int TimeLastCivilWarEnded;
        public bool HasBeenHackedForShips; //related to player hacks
        public bool IsQuiesced; //related to player hacks
        public int QuiesceStartTime;
        public int QuiesceEndTime = -1;

        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "ZenithArchitraveFactionBaseInfo-Teams" );
        //the ZA income code is fun! There are a lot of XML modifiers in the ZA Difficulty xml
        public readonly List<ArchitraveIncomeModifier> AppliedModifiers = List<ArchitraveIncomeModifier>.Create_WillNeverBeGCed( 300, "ZenithArchitraveFactionBaseInfo-AppliedModifiers" );

        public bool PlayerAllied = false;
        public bool MinorFactionAllied = false;
        public bool IsInWarFooting = false;

        #region NonSerialized
        //Not serialized; generally for UI stuff
        public int Intensity = 0;
        public ZenithArchitraveDifficulty Difficulty = null;
        public bool CivilWarEnabled = true;
        public FInt OverallPowerLevelOfEnemies = FInt.Zero;
        public int MaxTerritorySize = -1; //obtained from the "NumberToSeed" in the faction, but stored here for ease of use and UI stuff

        public bool WasJustTriggeringOtherZAsToAttackMe = false;
        public bool IsBetrayingTruce = false; //you can hack the ZA for a truce, but it will break the truce for the civil war or Expansion Mode
        public bool IsBeingHacked = false; //this is refreshed every sim step, no need to serialize
        public bool ShouldOtherArchitravesAttackMe; //have I expanded enough
        public int TimeUntilOtherArchitravesShouldAttackMe = -1; //I am warping in my last spawner

        public readonly DoubleBufferedValue<SafeSquadWrapper> Portal = new DoubleBufferedValue<SafeSquadWrapper>( SafeSquadWrapper.Create( null ) ); //our home spawner
        public readonly DoubleBufferedList<SafeSquadWrapper> Spawners = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZA-Spawners" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Pioneers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZA-Pioneers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> NonTerritorySpawners = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZA-NonTerritorySpawners" ); //the planets we own that aren't in our Territory

        //these need to be the more-costly DoubleBufferedConcurrentList, because the sim needs to be able to add to their display list at ad-hoc points.
        //the LRP is kept separate from all of this for performance reasons and code simplicity reasons.
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> WarpingInSpawners = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZA-WarpingInSpawners" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Castra = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZA-Castra" );
        //some logic below requires that the spawners be sorted, which is not possible with a DoubleBufferedConcurrentList like it was with a DoubleBufferedList
        //so we are instead using this secondary list.
        public readonly List<SafeSquadWrapper> SortedWarpingInSpawners = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZenithArchitraveFactionBaseInfo-SortedWarpingInSpawners" );

        public bool IsBelowMaxTerritory;
        public int CurrentTotalStrength;
        public int CurrentNonGolemStrength;
        public int TotalAllowedStrengthInWarFooting;
        public int TotalAllowedStrengthInPeace;
        public int IncomeLastSecond;
        public string AppliedModifiers_ForUI;
        public readonly ArcenDoubleCharacterBuffer Buffer_ForShipsInside = new ArcenDoubleCharacterBuffer( "ZenithArchitraveFactionBaseInfo-Buffer_ForShipsInside" );
        #endregion

        //constants
        public readonly int TimeAfterCivilWarForRetreat = 180; //after a civil war ends, we keep the during-civil-war allegiances while we retreat; since the ZAs are allied to the AI during a civil war, this way the ZA's retreating forces won't kill all the AI units
        public readonly int PioneerWarningTime = 300;

        public ZenithArchitraveFactionBaseInfo()
        {
            Cleanup();
        }

        #region Cleanup
        protected override void Cleanup()
        {
            Territory.Clear();
            PlanetsEverTaken.Clear();

            TimeForNextPlanetInTerritory = -1;
            PioneerSpawnTime = -1;
            TimesPioneersInterrupted = 0;
            PioneerSpawnTimeElapsed = 0;

            PlayersArchitraveIsFriendlyToward.Clear();
            PlayersArchitraveHates.Clear();
            CivilWarEnemies.Clear();
            ZenithAwakeningWarEnemies.Clear();

            WhenIBeganCivilWar = -1;
            MetalReserves = -1;
            GolemMetalReserves = -1;
            DefensiveMetalReserves = -1;
            TimeEnteredWarFooting = -1;
            HasDoneInitialStrike = false;
            MaxTerritorySize = -1;
            TimeLastCivilWarEnded = -1;
            HasBeenHackedForShips = false;
            IsQuiesced = false;
            QuiesceEndTime = -1;
            QuiesceStartTime = -1;

            Teams.Clear();
            AppliedModifiers.Clear();

            PlayerAllied = false;
            MinorFactionAllied = false;
            IsInWarFooting = false;

            Intensity = 0;
            Difficulty = null;
            CivilWarEnabled = true;
            OverallPowerLevelOfEnemies = FInt.Zero;

            WasJustTriggeringOtherZAsToAttackMe = false;
            IsBetrayingTruce = false;
            IsBeingHacked = false;
            ShouldOtherArchitravesAttackMe = false;
            TimeUntilOtherArchitravesShouldAttackMe = -1;

            this.Portal.Clear();
            Spawners.Clear();
            Pioneers.Clear();
            WarpingInSpawners.Clear();
            NonTerritorySpawners.Clear();
            Castra.Clear();
            SortedWarpingInSpawners.Clear();

            IsBelowMaxTerritory = false;
            CurrentTotalStrength = 0;
            CurrentNonGolemStrength = 0;
            TotalAllowedStrengthInWarFooting = 0;
            TotalAllowedStrengthInPeace = 0;
            IncomeLastSecond = 0;
            AppliedModifiers_ForUI = string.Empty;
            Buffer_ForShipsInside.Clear();

            PossibleExpansionPlanets.Clear();
            PreferredExpansionPlanets.Clear();

            SpawnersWithSpace.Clear();

            HaveLoadedData = false; //trigger reload of xml
        }
        #endregion

        #region Serialization and Deserialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Buffer.WriteHeaderStringToLogIfLoggingActive( "ZenithArchitraveFactionBaseInfo" );
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, Territory.Count, "Territory.Count" );
                for ( int i = 0; i < Territory.Count; i++ )
                    Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, Territory[i].Index, "Territory.Index" );
                debugCode = 200;
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, PlanetsEverTaken.Count, "PlanetsEverTaken.Index" );
                for ( int i = 0; i < PlanetsEverTaken.Count; i++ )
                    Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, PlanetsEverTaken[i].Index, "PlanetsEverTaken.Index" );
                debugCode = 300;
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)PlayersArchitraveIsFriendlyToward.Count, "PlayersArchitraveIsFriendlyToward.Count" );
                for ( int i = 0; i < PlayersArchitraveIsFriendlyToward.Count; i++ )
                    Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, PlayersArchitraveIsFriendlyToward[i], "PlayersArchitraveIsFriendlyToward.FactionIndex" );
                debugCode = 400;
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)CivilWarEnemies.Count, "CivilWarEnemies.Count" );
                for ( int i = 0; i < CivilWarEnemies.Count; i++ )
                    Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, CivilWarEnemies[i], "CivilWarEnemies.Index" );
                debugCode = 500;
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)ZenithAwakeningWarEnemies.Count, "ZenithAwakeningWarEnemies.Count" );
                for ( int i = 0; i < ZenithAwakeningWarEnemies.Count; i++ )
                    Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, ZenithAwakeningWarEnemies[i], "ZenithAwakeningWarEnemies.Index" );
                debugCode = 600;
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)AppliedModifiers.Count, "AppliedModifiers.Count" );
                for ( int i = 0; i < AppliedModifiers.Count; i++ )
                    AppliedModifiers[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
                debugCode = 700;
                FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
                debugCode = 800;
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)PlayersArchitraveHates.Count, "PlayersArchitraveHates.Count" );
                for ( int i = 0; i < PlayersArchitraveHates.Count; i++ )
                    Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, PlayersArchitraveHates[i], "PlayersArchitraveHates.Item" );
                Buffer.AddBool( MetaData, IsQuiesced, "IsQuiesced" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, QuiesceStartTime, "QuiesceStartTime" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, QuiesceEndTime, "QuiesceEndTime" );
                Buffer.AddBool( MetaData, PlayerAllied, "PlayerAllied" );
                Buffer.AddBool( MetaData, MinorFactionAllied, "MinorFactionAllied" );
                debugCode = 900;
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextPlanetInTerritory, "TimeForNextPlanetInTerritory" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, PioneerSpawnTime, "PioneerSpawnTime" );
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)TimesPioneersInterrupted, "TimesPioneersInterrupted" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, PioneerSpawnTimeElapsed, "PioneerSpawnTimeElapsed" );
                Buffer.AddInt32( MetaData, ReadStyle.Signed, MetalReserves, "MetalReserves" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, GolemMetalReserves, "GolemMetalReserves" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, DefensiveMetalReserves, "DefensiveMetalReserves" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, WhenIBeganCivilWar, "WhenIBeganCivilWar" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeLastCivilWarEnded, "TimeLastCivilWarEnded" );
                Buffer.AddBool( MetaData, HasBeenHackedForShips, "HasBeenHackedForShips" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeEnteredWarFooting, "TimeEnteredWarFooting" );
                Buffer.AddBool( MetaData, HasDoneInitialStrike, "HasDoneInitialStrike" );
                Buffer.AddBool( MetaData, IsInWarFooting, "IsInWarFooting" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ZA GLobalData SerializeTo debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ZenithArchitraveFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "ZenithArchitraveFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
            Territory.Clear();
            PlanetsEverTaken.Clear();
            // filled elsewhere
            //    Spawners_ForUI.Clear();
            PlayersArchitraveIsFriendlyToward.Clear();
            PlayersArchitraveHates.Clear();
            CivilWarEnemies.Clear();
            ZenithAwakeningWarEnemies.Clear();
            AppliedModifiers.Clear();
            int count = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "Territory.Count" );
            for ( int i = 0; i < count; i++ )
                 Territory.AddButRejectIfNull( World_AIW2.Instance.GetPlanetByIndex( (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "Territory.Index" ) ) );

            count = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "PlanetsEverTaken.Index" );
            for ( int i = 0; i < count; i++ )
                PlanetsEverTaken.AddButRejectIfNull( World_AIW2.Instance.GetPlanetByIndex( (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "PlanetsEverTaken.Index" ) ) );

            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "PlayersArchitraveIsFriendlyToward.Count" );
            for ( int i = 0; i < count; i++ )
                PlayersArchitraveIsFriendlyToward.Add( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PlayersArchitraveIsFriendlyToward.FactionIndex" ) );

            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "CivilWarEnemies.Count" );
            for ( int i = 0; i < count; i++ )
                CivilWarEnemies.Add( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "CivilWarEnemies.Index" ) );
            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ZenithAwakeningWarEnemies.Count" );
            for ( int i = 0; i < count; i++ )
                ZenithAwakeningWarEnemies.Add( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "ZenithAwakeningWarEnemies.Index" ) );

            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "AppliedModifiers.Count" );
            for ( int i = 0; i < count; i++ )
                AppliedModifiers.Add( new ArchitraveIncomeModifier( MetaData, Buffer, SerializationCmdType ) );
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "zenith architrave" );

            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "PlayersArchitraveHates.Count" );
            for ( int i = 0; i < count; i++ )
                PlayersArchitraveHates.Add( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PlayersArchitraveHates.Item" ) );
            IsQuiesced = Buffer.ReadBool( MetaData, "IsQuiesced" );
            QuiesceStartTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "QuiesceStartTime" );
            QuiesceEndTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "QuiesceEndTime" );

            this.PlayerAllied = Buffer.ReadBool( MetaData, "PlayerAllied" );
            this.MinorFactionAllied = Buffer.ReadBool( MetaData, "MinorFactionAllied" );

            TimeForNextPlanetInTerritory = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextPlanetInTerritory" );
            PioneerSpawnTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "PioneerSpawnTime" );
            TimesPioneersInterrupted = (int)Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "TimesPioneersInterrupted" );
            PioneerSpawnTimeElapsed = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "PioneerSpawnTimeElapsed" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 4, 015 ) )
                MetalReserves = Buffer.ReadInt32( MetaData, ReadStyle.Signed, "MetalReserves" );
            else
                MetalReserves = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "MetalReserves" );
            GolemMetalReserves = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "GolemMetalReserves" );
            DefensiveMetalReserves = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DefensiveMetalReserves" );
            WhenIBeganCivilWar = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "WhenIBeganCivilWar" );

            TimeLastCivilWarEnded = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeLastCivilWarEnded" );
            HasBeenHackedForShips = Buffer.ReadBool( MetaData, "HasBeenHackedForShips" );

            TimeEnteredWarFooting = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeEnteredWarFooting" );
            HasDoneInitialStrike = Buffer.ReadBool( MetaData, "HasDoneInitialStrike" );
            IsInWarFooting = Buffer.ReadBool( MetaData, "IsInWarFooting" );
            Buffer.StopTrackerByName( "ZenithArchitraveFactionBaseInfo Ext" );
        }
        #endregion end Serialization and Deserialization

        #region Xml Data
        public bool HaveLoadedData = false;
        //Stuff from External Constants
        public int AttritionInterval; //how quickly extra ships attrition during peacetime

        public int CivilWarBonusAttackStartTime; //when a civil war has been going on "too long", other ZAs get bonus attacks against the overly-strong ZAs
        public int CivilWarBonusAttackBaseStrength;
        public int CivilWarBonusAttackInterval;
        public FInt CivilWarBonusAttackStrengthMultiplier;
        public int QuiesceTime;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            AttritionInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_AttritionInterval" );
            CivilWarBonusAttackStartTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_CivilWarBonusAttackStartTime" );
            CivilWarBonusAttackBaseStrength = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_CivilWarBonusAttackBaseStrength" );
            CivilWarBonusAttackInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_CivilWarBonusAttackInterval" );
            CivilWarBonusAttackStrengthMultiplier = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_ZenithArchitrave_CivilWarBonusAttackStrengthMultiplier" );
            QuiesceTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_QuiesceTime" );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 60 + (Intensity * 10);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " 来自天顶拱门的负载" );
            return load;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            this.LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            Difficulty = ZenithArchitraveDifficultyTable.Instance.GetRowByIntensity( this.Intensity );
            if ( this.MaxTerritorySize == -1 )
                this.MaxTerritorySize = AttachedFaction.CustomData_NumberToSeed( true );
            CivilWarEnabled = AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "CivilWarEnabled", true );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 1000;
                OverallPowerLevelOfEnemies = GetOverallPowerLevelOfEnemies();
                debugCode = 1100;

                Portal.ClearConstructionValueForStartingConstruction();
                Spawners.ClearConstructionListForStartingConstruction();
                WarpingInSpawners.ClearConstructionListForStartingConstruction();
                NonTerritorySpawners.ClearConstructionListForStartingConstruction();
                Pioneers.ClearConstructionListForStartingConstruction();
                Castra.ClearConstructionListForStartingConstruction();

                this.CurrentTotalStrength = 0;
                this.CurrentNonGolemStrength = 0;
                int strength = 0;
                int strengthNonGolem = 0;
                debugCode = 1200;
                //now scan over all units and update lists/strengths
                //also set influence
                debugCode = 1400;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 2000;
                    if ( entity.TypeData.GetHasTag( "WarpingInZenithArchitraveSpawner" ) )
                    {
                        debugCode = 2050;
                        WarpingInSpawners.AddToConstructionList( entity );
                        //if in the nomad galaxy, being taken by the ZA de-nomadifies the planet
                        //otherwise the ZA will try not to take nomad planets
                        if ( World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy && entity.Planet.TypeData.Type == PlanetType.Nomad )
                            entity.Planet.TypeData = PlanetType.Normal.GetData();

                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "ZenithArchitraveSpawner" ) )
                    {
                        debugCode = 2100;
                        Spawners.AddToConstructionList( entity );
                        if ( !this.Territory.Contains( entity.Planet ) )
                            NonTerritorySpawners.AddToConstructionList( entity );
                        debugCode = 2150;
                        //update CurrentTotalStrength by the contents of the Spawner
                        ZenithArchitravePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ZenithArchitravePerUnitBaseInfo>( "ZenithArchitravePerUnitBaseInfo" );
                        int strengthForUnit = 0;
                        debugCode = 2160;
                        byte markLevel = data.MarkLevelForShips;
                        debugCode = 2165;
                        Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[markLevel];
                        debugCode = 2170;
                        foreach ( KeyValuePair<GameEntityTypeData, int> kv in data.ShipsInside )
                        {
                            debugCode = 2200;
                            GameEntityTypeData.MarkLevelStats markLevelStats = kv.Key.MarkStatsFor( data.MarkLevelForShips );
                            strengthForUnit += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * kv.Value);
                            if ( kv.Value > 0 )
                                Buffer_ForShipsInside.Add( kv.Key.GetDisplayName(), "909090" ).Add( " x" + kv.Value.ToString(), markByOrdinal.ColorHex ).Add( ", " );
                        }
                        debugCode = 2300;
                        strength += strengthForUnit;
                        Buffer_ForShipsInside.Add( " with total strength " ).Add( ((strengthForUnit) / 1000), "a1ffa1" ).Add( ".\n" );
                        data.ShipsInside_ForUI = Buffer_ForShipsInside.GetStringAndResetForNextUpdate();
                        entity.AdditionalStrengthFromFactions = strengthForUnit;
                    }
                    debugCode = 2400;
                    if ( entity.TypeData.GetHasTag( "ZenithArchitravePortal" ) )
                    {
                        Portal.Construction = SafeSquadWrapper.Create( entity );
                        if ( entity.Planet != null )
                            entity.Planet.IsZenithArchitraveHome = true;
                    }
                    debugCode = 2500;
                    if ( entity.TypeData.GetHasTag( "ArchitravePioneer" ) )
                    {
                        Pioneers.AddToConstructionList( entity );
                    }
                    debugCode = 2550;
                    if ( entity.TypeData.GetHasTag( "ArchitraveCastra" ) )
                    {
                        Castra.AddToConstructionList( entity );
                    }

                    debugCode = 2600;
                    if ( entity.TypeData.IsMobile )
                    {
                        strength += entity.GetStrengthOfSelfAndContents();
                        if ( !entity.TypeData.GetHasTag("ArchitraveWarGolem") )
                            strengthNonGolem += entity.GetStrengthOfSelfAndContents();
                    }
                }
                debugCode = 3000;
                this.CurrentTotalStrength = strength;
                this.CurrentNonGolemStrength = strengthNonGolem;

                //if this were used by the UI, it would flicker
                SortedWarpingInSpawners.Clear();
                foreach ( KeyValuePair<SafeSquadWrapper, bool> _kv in WarpingInSpawners.GetConstructionList() )
                {
                    SafeSquadWrapper spawner = _kv.Key;
                    SortedWarpingInSpawners.Add( spawner );
                }
                SortedWarpingInSpawners.Sort( static delegate ( SafeSquadWrapper L, SafeSquadWrapper R )
                {
                    return L.TransformsIntoAfterTime.CompareTo( R.TransformsIntoAfterTime );
                } );

                Portal.SwitchConstructionToDisplay();
                Spawners.SwitchConstructionToDisplay();
                WarpingInSpawners.SwitchConstructionToDisplay();
                NonTerritorySpawners.SwitchConstructionToDisplay();
                Pioneers.SwitchConstructionToDisplay();
                Castra.SwitchConstructionToDisplay();

                this.UpdateTerritory();
                this.UpdateWarFooting( Context ); //SortedWarpingInSpawners is used in UpdateWarFooting, as well
                this.RunPostTerritoryPostWarFootingLogic();

                this.WasJustTriggeringOtherZAsToAttackMe = false;//used to check whether we were just in a civil war later in the Sim code
                if ( FactionUtilityMethods.Instance.GetNumZenithArchitraves() > 1 && CivilWarEnabled && NumCivilWarParticipants() > 1 )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("checking if we are in civil war. " + Spawners.Count + ", " + WarpingInSpawners.Count + " vs " + (this.MaxTerritorySize + Difficulty.ExcessSpawnersToTriggerOtherArchitravesToAttackMe ), Verbosity.DoNotShow );
                    if ( Spawners.Count >= (this.MaxTerritorySize + Difficulty.ExcessSpawnersToTriggerOtherArchitravesToAttackMe) )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("path a", Verbosity.DoNotShow );
                        //if we are now too strong, start the civil war; other ZAs check this flag to see if they should join the civil war
                        debugCode = 3100;
                        this.ShouldOtherArchitravesAttackMe = true;
                        this.WasJustTriggeringOtherZAsToAttackMe = true;
                        this.TimeUntilOtherArchitravesShouldAttackMe = -1;
                        if ( this.WhenIBeganCivilWar == -1 )
                            this.WhenIBeganCivilWar = World_AIW2.Instance.GameSecond;
                    }
                    else
                    {
                        this.ShouldOtherArchitravesAttackMe = false;
                        this.WhenIBeganCivilWar = -1;
                        if ( (Spawners.Count + WarpingInSpawners.Count) >= (this.MaxTerritorySize + Difficulty.ExcessSpawnersToTriggerOtherArchitravesToAttackMe) )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("path b", Verbosity.DoNotShow );
                            debugCode = 3200;
                            int requiredSpawners = (this.MaxTerritorySize + Difficulty.ExcessSpawnersToTriggerOtherArchitravesToAttackMe) - Spawners.Count;

                            //ArcenDebugging.ArcenDebugLogSingleLine("required: " + requiredSpawners, Verbosity.DoNotShow );
                            for ( int i = 0; i < SortedWarpingInSpawners.Count; i++ )
                            {
                                GameEntity_Squad spawner = SortedWarpingInSpawners[i].GetSquad();
                                if ( spawner == null )
                                    continue;

                                requiredSpawners--;
                                if ( requiredSpawners == 0 )
                                {
                                    this.TimeUntilOtherArchitravesShouldAttackMe = spawner.SecondsTillTransformation;
                                    //ArcenDebugging.ArcenDebugLogSingleLine("I have " + this.TimeUntilOtherArchitravesShouldAttackMe + " because it will be " + WarpingInSpawners[i].SecondsTillTransformation + " seconds until " + WarpingInSpawners[i].ToStringWithPlanet() + " becomes a spawner" , Verbosity.DoNotShow );
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                    this.ShouldOtherArchitravesAttackMe = false; //this is defensive code for builds in the DLC2 press builds. Can be removed eventually

                debugCode = 4000;

                this.UpdatePlanetBGs();

                debugCode = 5000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in ZA stage 2, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion end DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            UpdateMetalReserves();
            AttritionOrAbsorbShips( Context );
        }

        #region AttritionOrAbsorbShips
        public static readonly List<SafeSquadWrapper> SpawnersWithSpace = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-SpawnersWithSpace" );
        public void AttritionOrAbsorbShips( ArcenClientOrHostSimContextCore Context )
        {
            //If we are in war mode, this is a noop
            if ( this.IsInWarFooting )
                return;
            //If we were at war but are now at peace, ships either go back inside spawners or attrition.
            //Lore Justification: The ZA can't spend the energy to maintain a fleet unless it really needs to.
            //These ships either go back into storage inside the Spawners or slowly attrition from lack of energy if they can't fit inside a spawner.
            //This is intended to take a relatively brief abmount of time (only just after a war), so hopefully not too much of a performance hit
            SpawnersWithSpace.Clear();
            List<SafeSquadWrapper> spawners = this.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                //Each spawner has a limited amount of capacity for units
                GameEntity_Squad spawner = spawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                ZenithArchitravePerUnitBaseInfo data = spawner.TryGetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                if ( data.GetStrengthInside() < this.GetAllowedPeaceStrengthForSpawner( spawner, this.Difficulty ) )
                {
                    SpawnersWithSpace.Add( spawners[i] );
                }
            }
            int range = 1000;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( !entity.TypeData.IsMobile )
                    continue;


                for ( int i = 0; i < SpawnersWithSpace.Count; i++ )
                {
                    GameEntity_Squad spawner = SpawnersWithSpace[i].GetSquad();
                    if ( spawner == null )
                        continue;
                    if ( spawner.Planet != entity.Planet )
                        continue;
                    //get into a spawner if we are close enough to one
                    if ( Mat.DistanceBetweenPointsImprecise( entity.WorldLocation, spawner.WorldLocation ) < range )
                    {
                        ZenithArchitravePerUnitBaseInfo data = spawner.TryGetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                        if ( data.ShipsInside.ContainsKey( entity.TypeData ) )
                            data.ShipsInside[entity.TypeData]++;
                        else
                            data.ShipsInside[entity.TypeData] = 1;
                        entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                }
                bool isOnSpawnerPlanet = false;
                int attritionInterval = 45;
                for ( int i = 0; i < spawners.Count; i++ )
                {
                    if ( spawners[i].Planet == entity.Planet )
                    {
                        isOnSpawnerPlanet = true;
                        break;
                    }
                }
                //now attrition. Units attrition extra fast after a civil war
                if ( !isOnSpawnerPlanet || this.IsQuiesced )
                {
                    //attrition really fast if this unit is not in territory, or if the ZA is quiesced
                    attritionInterval = 5;
                }
                else if ( World_AIW2.Instance.GameSecond - this.TimeLastCivilWarEnded <= this.TimeAfterCivilWarForRetreat )
                {
                    attritionInterval = 25; //also faster than usual attrition after a civil war ends
                }
                if ( World_AIW2.Instance.GameSecond % attritionInterval == 0 )
                {
                    int damageToTake = (int)(((float)this.Difficulty.AttritionPercent / 100) * (entity.GetMaxHullPoints()));
                    entity.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
                }
            }
        }
        #endregion

        #region IsPlanetInTerritory
        public bool IsPlanetInTerritory( Planet planet )
        {
            //this is called from LRP, and Territory is updated in Sim.
            //So might be a problem
            for ( int i = 0; i < this.Territory.Count; i++ )
                if ( planet == this.Territory[i] )
                    return true;
            return false;
        }
        #endregion

        #region GetAllowedPeaceStrengthForSpawner
        public int GetAllowedPeaceStrengthForSpawner( GameEntity_Squad spawner, ZenithArchitraveDifficulty diff )
        {
            if ( spawner == null )
                return 0;
            //this is called from Description Appender code as well as the main sim code
            return diff.StrengthPerSpawner + spawner.CurrentMarkLevel * diff.SpawnerStrengthIncreasePerMark;
        }
        #endregion

        #region GetSpawnerUpgradeTime
        public int GetSpawnerUpgradeTime( GameEntity_Squad spawner )
        {
            if ( spawner.CurrentMarkLevel >= this.Difficulty.MaxSpawnerMarkLevel )
                return -1;
            if ( this.IsQuiesced ) //static function
                return -1;

            int nextUpgrade = spawner.CurrentMarkLevel + 1;
            int time = nextUpgrade * this.Difficulty.SecondsToUpgrade + spawner.GameSecondCreated;
            if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                time /= 15;
            return time;
        }
        #endregion

        #region IsPlanetInAnyZATerritory
        public static bool IsPlanetInAnyZATerritory( Planet planet )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                    continue;
                ZenithArchitraveFactionBaseInfo data = otherFaction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                if ( data.Territory.Contains( planet ) )
                    return true;
            }
            return false;
        }
        #endregion

        #region GetZenithArchitraveStateForDisplay
        public void GetZenithArchitraveStateForDisplay( ArcenDoubleCharacterBuffer buffer )
        {
            //For debug only; with ZA traces, click on the Threat icon on the resource bar
            ZenithArchitraveDifficulty difficulty = ZenithArchitraveDifficultyTable.Instance.GetRowByIntensity( this.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant() );
            buffer.Add( this.AttachedFaction.StartFactionColourForLog() + "Zenith Architrave</color> " + this.AttachedFaction.FactionIndex + " has " ).Add( this.Spawners.Count, "a1ffa1" ).Add( " spawners ( " ).Add( this.WarpingInSpawners.Count, "a1ff00" ).Add( " warping in) and " ).Add( this.Pioneers.Count ).Add( " pioneers. When it reaches " ).Add( (this.MaxTerritorySize + difficulty.ExcessSpawnersToTriggerOtherArchitravesToAttackMe), "a1ffa1" ).Add( " planets, it will trigger a civil war.\n" );
            if ( !this.AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "CivilWarEnabled", true ) )
                buffer.Add( "Civil War Disabled For Me\n" );
            if ( this.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                buffer.Add( "Other Architraves will attack me in " + this.TimeUntilOtherArchitravesShouldAttackMe + " seconds\n" );

            if ( this.PioneerSpawnTime > -1 )
                buffer.Add( "Pioneers will spawn in " ).Add( (this.PioneerSpawnTime - World_AIW2.Instance.GameSecond), "a1ffa1" ).Add( " seconds.\n" );
            if ( this.TimeForNextPlanetInTerritory > 0 )
                buffer.Add( "The Architrave will add to its territory in " ).Add( (this.TimeForNextPlanetInTerritory - World_AIW2.Instance.GameSecond), "a1ffa1" ).Add( " seconds.\n" );
            if ( this.PlayersArchitraveIsFriendlyToward.Count > 0 )
            {
                buffer.Add( "This ZA is friendly towards " ).Add( this.PlayersArchitraveIsFriendlyToward.Count ).Add( " player factions: " );
                for ( int i = 0; i < this.PlayersArchitraveIsFriendlyToward.Count; i++ )
                    buffer.Add( " faction " ).Add( this.PlayersArchitraveIsFriendlyToward[i] );
                buffer.Add( "\n" );
            }
            buffer.Add( "This Architrave is in " );

            if ( this.CivilWarEnemies.Count > 0 )
                buffer.Add( "civil war", "ff0000" );
            else if ( this.IsInWarFooting )
                buffer.Add( "war", "00ff00" );
            else
                buffer.Add( "peace", "2222ff" );
            if ( this.IsBelowMaxTerritory )
                buffer.Add( " and is below max territory size (num spawners " ).Add( this.Spawners.Count ).Add( " (warping " + this.WarpingInSpawners.Count + ") , territory size " ).Add( this.Territory.Count + ", max territory size " ).Add( this.MaxTerritorySize ).Add( ").\n" );
            else if ( this.ShouldOtherArchitravesAttackMe )
                buffer.Add( " and is TOO DAMN BIG\n" );
            else
                buffer.Add( " and is at or above max territory size.\n" );

            if ( this.TimesPioneersInterrupted > 0 )
                buffer.Add( "Pioneer spawning was interrupted " ).Add( this.TimesPioneersInterrupted ).Add( " times.\n" );

            if ( this.TimeLastCivilWarEnded != -1 )
                buffer.Add( "Time last civil war ended " ).Add( this.TimeLastCivilWarEnded, "a1ffa1" ).Add( "\n" );
            buffer.Add( "Metal reserves: " ).Add( this.MetalReserves, "ffa1a1" ).Add( ", golem reserves: " ).Add( this.GolemMetalReserves ).Add( ", defensive metal reserves: " ).Add( this.DefensiveMetalReserves ).Add( "\n" );
            buffer.Add( "Income last second " ).Add( this.IncomeLastSecond, "ddbbbb" ).Add( ".\n" );
            buffer.Add( this.AppliedModifiers_ForUI );
            for ( int i = 0; i < this.Territory.Count; i++ )
            {
                buffer.Add( "\tTerritory: " ).Add( this.Territory[i].Name, "a1a1ff" );
            }
            buffer.Add( "\nTotal Strength Allowed in peace " ).Add( this.TotalAllowedStrengthInPeace ).Add( " and in war: " ).Add( this.TotalAllowedStrengthInWarFooting );
            buffer.Add( "\nFireteams:\n" );
            int totalStrength = 0;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( this.Teams ) )
            {
                totalStrength += team.DeepInfo.TeamStrength;
                if ( team.status != FireteamStatus.Disbanded )
                {
                    team.DeepInfo.GetStringForDisplay( buffer );
                    buffer.Add( "\n" );
                }
            }
            buffer.Add( "Total strength in fireteams: " ).Add( totalStrength, "a1ffa1" ).Add( "\n\n" );
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region ConvertPortalToHackedPortal
        public static void ConvertPortalToHackedPortal( GameEntity_Squad portal, ArcenHostOnlySimContext Context )
        {
            if ( portal == null )
                return;
            
            //this is only called from Hacking code paths

            var newType = GameEntityTypeDataTable.Instance.GetRowByName( "ZAHackedPortal" );
            var oldData = portal.TryGetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();

            GameEntity_Squad newPortal 
                = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( 
                    portal.PlanetFaction, newType, 1,
                    portal.GetFactionLooseFleetOrNull_Safe(), 0, portal.WorldLocation,
                    Context, "DarkZenithConversion-PortalHack" );

            if ( newPortal != null )
            {
                var newData = newPortal.CreateExternalBaseInfo<ZenithArchitravePerUnitBaseInfo>( "ZenithArchitravePerUnitBaseInfo" );
                oldData.CopyTo( newData );
                
                byte oldMark = portal.CurrentMarkLevel;
                newPortal.SetCurrentMarkLevel( oldMark );
                portal.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            }
        }
        #endregion
        
        #region GetOverallPowerLevelOfEnemies
        private FInt GetOverallPowerLevelOfEnemies()
        {
            FInt sum = FInt.Zero;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {

                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( AttachedFaction == otherFaction )
                    continue;
                if ( !AttachedFaction.GetIsHostileTowards( otherFaction ) )
                    continue; //only for hostile factions
                sum += otherFaction.OverallPowerLevel;
            }
            return sum;
        }
        #endregion

        #region UpdateWarFooting
        private void UpdateWarFooting( ArcenClientOrHostSimContextCore Context )
        {
            //figure out if we are in War Footing or not
            //bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            //ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("ZA-UpdateWarFooting-trace", 10f) : null;

            bool wasAtWar = this.IsInWarFooting;
            this.IsInWarFooting = CheckForWarFooting( Context );
            if ( wasAtWar != this.IsInWarFooting  &&
                 World_AIW2.Instance.GetIsHostAnyShouldPrepareToSendNewEntitiesToClients() )
            {
                //if we've just changed our war status, sync us to MP clients to make sure they see the correct behaviour/notifications ASAP
                World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction );
            }
            if ( !wasAtWar && this.IsInWarFooting )
            {
                this.TimeEnteredWarFooting = World_AIW2.Instance.GameSecond;
            }
            if ( !this.IsInWarFooting )
            {
                this.TimeEnteredWarFooting = -1;
            }
        }
        #endregion

        #region CheckForWarFooting
        private bool CheckForWarFooting( ArcenSimContextAnyStatus Context )
        {
            //This checks for A. civil war
            //B. Pioneers (or warping in spawners)
            //C. Enemies in territory (note that here "territory" means "planets with spawners", since pioneers might expanded us

            //Also updates IsBelowMaxTerritory

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-CheckForWarFooting-trace", 10f ) : null;

            if ( this.Territory.Count < this.MaxTerritorySize )
                this.IsBelowMaxTerritory = true;
            else
                this.IsBelowMaxTerritory = false;

            if ( this.IsQuiesced )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are Quiesced\n" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return false;
            }

            if ( Pioneers.Count > 0 || this.CivilWarEnemies.Count > 0 )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are in war footing, path A. Pioneers: " + Pioneers.Count + " civil war enemies " + this.CivilWarEnemies.Count + "\n" );
                return true;
            }
            if ( WarpingInSpawners.Count > 0 )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are in war footing, path B. WarpingInSpawners: " + WarpingInSpawners.Count + "\n" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }

            if ( this.ShouldOtherArchitravesAttackMe )
            {
                if ( tracing )
                {
                    tracingBuffer.Add( "We are in war footing, civil war path A1\n" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }
            List<SafeSquadWrapper> spawners = this.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                Planet planet = spawners[i].Planet;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( !Context.IsLongRangePlanning )
                {
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > pFaction.DataByStance[FactionStance.Self].TotalStrength / 100 ||
                         pFaction.DataByStance[FactionStance.Hostile].TotalStrength >= 2000 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check B, on " + planet.Name + ".. enemy strength " + pFaction.DataByStance[FactionStance.Hostile].TotalStrength + " my strength " + pFaction.DataByStance[FactionStance.Self].TotalStrength + "." );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                }
                else
                {
                    EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( AttachedFaction );
                    StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                    StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];
                    if ( hostileData.TotalStrength > selfData.TotalStrength / 100 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check C, on " + planet.Name + ".\n" );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                }
            }
            if ( SortedWarpingInSpawners.Count > 0 ) //we can safely use SortedWarpingInSpawners in here, because this is only called from Stage2, above.  No chance for flicker.
            {
                Planet planet = SortedWarpingInSpawners[0].Planet;
                if ( tracing )
                {
                    tracingBuffer.Add( "War footing check B1, warping in spawner on " + planet.Name + ".." );
                    tracingBuffer.Add( " we are in war footing\n" );
                }
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }

            for ( int i = 0; i < this.Territory.Count; i++ )
            {
                Planet planet = this.Territory[i];
                if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( AttachedFaction ) )
                {
                    if ( tracing )
                    {
                        tracingBuffer.Add( "War footing check E1, on " + planet.Name + " an enemy faction owns me." );
                        tracingBuffer.Add( " we are in war footing\n" );
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    return true;
                }
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( !Context.IsLongRangePlanning )
                {
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > pFaction.DataByStance[FactionStance.Self].TotalStrength / 5 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check E2, on " + planet.Name + ". enemy strength " + pFaction.DataByStance[FactionStance.Hostile].TotalStrength + " my strength " + pFaction.DataByStance[FactionStance.Self].TotalStrength / 5 + "." );
                            tracingBuffer.Add( " we are in war footing\n" );
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                    bool foundEnemyGuardPost = false;
                    foreach ( GameEntity_Squad guardPost in planet.Squads( EntityRollupType.ReinforcementLocations ) )
                    {
                        switch ( guardPost.TypeData.SpecialType )
                        {
                            case SpecialEntityType.GuardPost:
                            case SpecialEntityType.DireGuardPost:
                                break;
                            default:
                                continue; //Data centers and other things can also be reinforcement locations
                        }
                        if ( guardPost.GetIsHostileTowards_Safe( AttachedFaction ) )
                        {
                            foundEnemyGuardPost = true;
                            break;
                        }
                    }
                    if ( foundEnemyGuardPost )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check E3, enemy guard post detected." );
                            tracingBuffer.Add( " we are in war footing\n" );
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                }
                else
                {
                    EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( AttachedFaction );
                    StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                    StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];
                    if ( hostileData.TotalStrength > selfData.TotalStrength / 3 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check F, on " + planet.Name + ".\n" );
                            tracingBuffer.Add( " we are in war footing\n" );
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                    bool foundEnemyGuardPost = false;
                    foreach ( GameEntity_Squad guardPost in planet.Squads( EntityRollupType.ReinforcementLocations ) )
                    {
                        if ( guardPost.GetIsHostileTowards_Safe( AttachedFaction ) )
                        {
                            foundEnemyGuardPost = true;
                            break;
                        }
                    }
                    if ( foundEnemyGuardPost )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check G, enemy guard post detected." );
                            tracingBuffer.Add( " we are in war footing\n" );
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }

                }
            }
            if ( tracing )
            {
                tracingBuffer.Add( "We are not in war footing, LRP " + Context.IsLongRangePlanning + "\n" );
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return false;
        }
        #endregion end CheckForWarFooting

        #region NumCivilWarParticipants
        public int NumCivilWarParticipants()
        {
            //go through all the ZAs and see how many have the civil war enabled.
            int count = 0;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                    continue;
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "CivilWarEnabled", true ) )
                    count++;
            }
            return count;
        }
        #endregion

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            //update overall power level based on empire size
            //civil war doesn't affect power level, since they aren't hostile to the AI
            FInt powerLevel = FInt.Zero;
            if ( Spawners.Count >= this.MaxTerritorySize - 2 )
                powerLevel = FInt.FromParts( 0, 250 );
            if ( Spawners.Count >= this.MaxTerritorySize )
                powerLevel = FInt.FromParts( 0, 890 );
            if ( Spawners.Count >= this.MaxTerritorySize + 5 )
                powerLevel = FInt.FromParts( 1, 000 );
            if ( Spawners.Count >= this.MaxTerritorySize + 8 )
                powerLevel = FInt.FromParts( 1, 500 );
            if ( Spawners.Count >= this.MaxTerritorySize + 12 )
                powerLevel = FInt.FromParts( 2, 000 );

            AttachedFaction.OverallPowerLevel = powerLevel;
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) ||
                 Target.TypeData.GetHasTag( "DSAA" ) ||
                 Target.TypeData.GetHasTag( "WarpGate" ) ||
                    Target.TypeData.IsCommandStation )
                return true;
            return false;
        }
        #endregion

        #region DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;

            int debugCode = 0;
            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            try
            {
                debugCode = 100;
                NotifierFillData civilWarFillData = null;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                        continue;

                    ZenithArchitraveFactionBaseInfo data = otherFaction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                    if ( data == null )
                        continue;
                    if ( data.ShouldOtherArchitravesAttackMe || data.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                    {
                        if ( civilWarFillData == null )
                        {
                            civilWarFillData = NotifierFillData.GetFromPoolOrCreate();
                        }
                        civilWarFillData.FactionList.Add( otherFaction );
                        if ( data.ShouldOtherArchitravesAttackMe )
                            civilWarFillData.WarStarted = true;
                    }

                    if ( data.Pioneers.Count > 0 ||
                         data.PioneerSpawnTime != -1 && (data.PioneerSpawnTime - World_AIW2.Instance.GameSecond <= this.PioneerWarningTime) &&
                         (data.CivilWarEnemies.Count == 0 && !data.ShouldOtherArchitravesAttackMe) )
                    {
                        //Architrave is in expansion mode. Note that if I am in a civil war but am not large enough to trigger it, the pioneers will go home
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        //PrivateArchitraveExpansionNotifier notifier = NonSimPrivateArchitraveExpansionNotifierList.GetOrAddEntryForUnderConstruction();
                        fillData.Faction = otherFaction;
                        if ( data.ShouldOtherArchitravesAttackMe && FactionUtilityMethods.Instance.GetNumZenithArchitraves() > 1 )
                            fillData.ProvokingWar = true;
                        fillData.NumPioneers = data.Pioneers.Count;
                        fillData.EntityList.AddRange( data.Pioneers.GetDisplayList() );
                        fillData.eventTimeRemaining = data.PioneerSpawnTime - World_AIW2.Instance.GameSecond;
                        fillData.TimesPioneersInterrupted = data.TimesPioneersInterrupted;
                        if ( localFactionOrNull != null &&
                             data.PlayersArchitraveIsFriendlyToward.Contains( localFactionOrNull.FactionIndex ) )
                            fillData.anyTruce = true;
                        else
                            fillData.anyTruce = false;
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicArchitraveExpansionNotifier.Instance, fillData, "", 0, "Architrave Expansion", SortedNotificationPriorityLevel.Medium );
                    }
                }
                if ( civilWarFillData != null )
                {
                    //after having gone over all the factions once, we've figured out if we need
                    //a civil war notifier (and how many factions are triggering it)
                    civilWarFillData.anyTruce = false;
                    for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                    {
                        //Go over the other ZA factions to fill in the "smaller" factions
                        Faction civilWarSmallerFaction = World_AIW2.Instance.Factions[j];
                        if ( civilWarSmallerFaction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                            continue;

                        ZenithArchitraveFactionBaseInfo smallerData = civilWarSmallerFaction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( smallerData == null )
                            continue;
                        if ( !civilWarSmallerFaction.GetBoolValueForCustomFieldOrDefaultValue( "CivilWarEnabled", true ) )
                            continue; //this is for civil war disabled
                        if ( localFactionOrNull != null &&
                             smallerData.PlayersArchitraveIsFriendlyToward.Contains( localFactionOrNull.FactionIndex ) )
                            civilWarFillData.anyTruce = true;
                        if ( !smallerData.ShouldOtherArchitravesAttackMe && (smallerData.TimeUntilOtherArchitravesShouldAttackMe < 0) )
                            civilWarFillData.FactionList2.Add( civilWarSmallerFaction );
                    }
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( PublicZenithArchitraveNotifier.Instance, civilWarFillData, "", 0, "Architrave CivilWar", SortedNotificationPriorityLevel.Major );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ZA notifications debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion end DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost

        #region UpdatePlanetBGs
        private void UpdatePlanetBGs()
        {
            for ( int i = 0; i < this.Territory.Count; i++ )
            {
                Planet plan = null;
                try
                {
                    plan = this.Territory[i];
                }
                catch { continue; }
                if ( plan != null )
                {
                    plan.SpaceBox_TagMustMatch = "ZenithArchitrave";
                    if ( plan.IsZenithArchitraveHome )
                        plan.Planet_TagMustMatch = "ZenithArchitrave_HomePlanet";
                }
            }
        }
        #endregion

        #region UpdateTerritory
        private void UpdateTerritory()
        {
            //if at max territory, noop
            //If we don't have enemies in our territory right now and
            //we aren't at max territory and we don't have  TimeForNextPlanetInTerritory
            //   Set TimeForNextPlanetInTerritory
            //If we are past TimeForNextPlanetInTerritory,
            //   Unset TimeForNextPlanetinTerritory
            //   Add a new planet to territory
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-UpdateTerritory-trace", 10f ) : null;
            List<SafeSquadWrapper> spawners = this.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                Planet plan = spawners[i].Planet;
                if ( plan == null || plan.HasPlanetBeenDestroyed || this.Territory.Contains( plan ))
                    continue;
                if ( plan.IsZenithArchitraveTerritory ||
                     ( spawners[i].TypeData.GetHasTag("ZenithArchitravePortal") &&
                      !plan.IsZenithArchitraveTerritory) ) //at the beginning of the game, make sure we include this planet in the Territory
                {
                    if ( tracing )
                        tracingBuffer.Add( "Adding " + spawners[i].GetPlanetName_Safe() + " to territory, Stage2\n" );

                    this.Territory.AddButRejectIfNull( plan );
                }
            }

            for ( int i = this.Territory.Count - 1; i >= 0; i-- )
            {
                Planet planet = this.Territory[i];
                if ( planet.HasPlanetBeenDestroyed )
                {
                    //if the ZM has eaten a ZA planet, remove it from the Territory so the ZA will
                    //replace it
                    this.Territory.RemoveAt( i );
                    continue;
                }
                //Set the ZA territory flag
                planet.AdditionalDescriptionTextFromFactions = "This planet is in the Territory of a " + AttachedFaction.StartFactionColourForLog() + "Zenith Architrave</color>, who will fight to the death to keep it.";
                planet.IsZenithArchitraveTerritory = true;

            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion

        #region RunPostTerritoryPostWarFootingLogic
        private void RunPostTerritoryPostWarFootingLogic()
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-RunPostTerritoryPostWarFootingLogic-trace", 10f ) : null;

            if ( World_AIW2.Instance.GameSecond % 2 == 0 )
            {
                for ( int i = 0; i < this.Territory.Count; i++ )
                {
                    Planet plan = this.Territory[i];
                    plan.AdditionalDescriptionTextFromFactions = "This planet is in the Territory of a " + AttachedFaction.StartFactionColourForLog() + "Zenith Architrave</color>, who will fight to the death to keep it.";
                    plan.IsZenithArchitraveTerritory = true;
                }

            }
            if ( this.IsQuiesced || this.IsInWarFooting || this.Territory.Count >= this.MaxTerritorySize )
            {
                if ( tracing )
                    tracingBuffer.Add( AttachedFaction.GetDisplayName() + " index " + AttachedFaction.FactionIndex + ". Not allowed to expand territory. War footing " + this.IsInWarFooting + " quiesced " + this.IsQuiesced + " and territory " + this.Territory.Count + "\n" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return;
            }
            //we know we're not quiesced or in war mode now, and we are below MaxTerritorySize
            if ( this.TimeForNextPlanetInTerritory == -1 && Spawners.Count == this.Territory.Count )
            {
                //only add new territory once we've conquered our existing territory
                this.TimeForNextPlanetInTerritory = World_AIW2.Instance.GameSecond + Difficulty.TimeBetweenTerritoryIncrease;
                if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                    this.TimeForNextPlanetInTerritory = World_AIW2.Instance.GameSecond + 10; //debug mode

                if ( tracing )
                    tracingBuffer.Add( "Next planet will be added to territory at " + this.TimeForNextPlanetInTerritory + " seconds in (now " + World_AIW2.Instance.GameSecond + " + " + Difficulty.TimeBetweenTerritoryIncrease + ")\n" );

            }
            if ( this.TimeForNextPlanetInTerritory != -1 &&
                 World_AIW2.Instance.GameSecond >= this.TimeForNextPlanetInTerritory )
            {
                Planet newPlanet = GetNextPlanetInTerritory();
                if ( newPlanet == null )
                {
                    //we've run out of planets to expand to, so we're done here. Maybe we're stuck on the end of a snake map or something
                    if ( tracing )
                        tracingBuffer.Add( "No planets to expand to; we're done expanding.\n" );

                    this.MaxTerritorySize = this.Territory.Count;
                    return;
                }
                if ( tracing )
                    tracingBuffer.Add( "Adding planet " + newPlanet.Name + " to territory at " + World_AIW2.Instance.GameSecond + " seconds in. Trigger time " + this.TimeForNextPlanetInTerritory + "\n" );
                this.Territory.AddButRejectIfNull( newPlanet );
                this.TimeForNextPlanetInTerritory = -1;
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion

        #region GetNextPlanetInTerritory
        private readonly List<Planet> PossibleExpansionPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionBaseInfo-PossibleExpansionPlanets" );
        private readonly List<Planet> PreferredExpansionPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionBaseInfo-PreferredExpansionPlanets" );
        public Planet GetNextPlanetInTerritory()
        {
            //called by UpdateTerritory
            //Rules are
            //  must be adjacent to existing territory
            //  can't be a King planet or adjacent to a King planet
            //  prefer a planet adjacent to our home spawner
            //  prefer the weakest planet
            PossibleExpansionPlanets.Clear();
            PreferredExpansionPlanets.Clear();
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-GetNextPlanetInTerritory-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add( "GetNextPlanetInTerritory: " );

            List<SafeSquadWrapper> spawners = this.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                GameEntity_Squad spawner = spawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                if ( spawner.Planet.HasPlanetBeenDestroyed )
                    continue;
                if ( tracing )
                    tracingBuffer.Add( " Checking spawner " + spawner.ToStringWithPlanet() + " for likely neighbors.\n" );
                foreach ( Planet neighbor in spawner.Planet.LinkedNeighbors( false ) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\tChecking " + neighbor.Name + ".\n" );
                    if ( neighbor.IsPlanetToBeDestroyed || neighbor.HasPlanetBeenDestroyed )
                        continue;
                    if ( FactionUtilityMethods.Instance.IsPlanetNearKing( neighbor, 2, true ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", too near a stationary king.\n" );
                        continue;
                    }
                    if ( neighbor.TypeData.Type == PlanetType.Nomad && !World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", its a nomad.\n" );
                        continue;
                    }
                    if ( neighbor.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", its a bastion.\n" );
                        continue;
                    }
                    for ( int j = 0; j < Spawners.Count; j++ )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tChecking spawner " + spawners[j].ToStringWithPlanet() + " to see if it overlaps\n" );

                        if ( spawners[j].Planet == neighbor )
                            continue;
                    }
                    if ( this.Territory.Contains( neighbor ) )
                        continue;
                    if ( neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\t\tRemoving " + neighbor.Name + " since it is owned by another ZA\n" );
                        continue;
                    }
                    if ( neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithDysonSphere" ||
                         neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "AntagonizedDysonSphere" )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\t\tRemoving " + neighbor.Name + " since it is owned by a Dyson Sphere\n" );
                        continue;
                    }
                    for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                    {
                        Faction playerfaction = World_AIW2.Instance.Factions[j];
                        if ( playerfaction.Type != FactionType.Player )
                            continue;
                        bool foundSpireCity = false;
                        foreach ( GameEntity_Squad entity in playerfaction.Squads( EntityRollupType.CityCenter ) )
                        {
                            if ( !entity.TypeData.GetHasTag( "SpireCity" ) )
                                continue;

                            if ( entity.Planet == neighbor )
                            {
                                foundSpireCity = true;
                                break;
                            }
                        }
                        if ( foundSpireCity )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\t\t\tRemoving " + neighbor.Name + " since it has a Spire City\n" );
                            continue;
                        }
                    }

                    if ( tracing )
                        tracingBuffer.Add( "\t\t\tAdding " + neighbor.Name + " to possible\n" );
                    PossibleExpansionPlanets.Add( neighbor );

                    if ( FactionUtilityMethods.Instance.IsPlanetNearKing( neighbor, 2, false ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", too near a mobile king.\n" );
                        continue;
                    }

                    if ( spawner.TypeData.GetHasTag( "ZenithArchitravePortal" ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\t\tAdding " + neighbor.Name + " to preferred\n" );
                        PreferredExpansionPlanets.Add( neighbor );
                    }
                }
            }

            if ( PossibleExpansionPlanets.Count == 0 )
            {
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return null;
            }

            for ( int i = PossibleExpansionPlanets.Count - 1; i >= 0; i-- )
            {
                //make sure no other Architrave has this planet
                //this is a bit heavy of a loop, but it's called infrequently and
                //there should never be that many architraves
                Planet thisPlanet = PossibleExpansionPlanets[i];
                bool foundOverlap = false;
                for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[j];
                    if ( otherFaction == AttachedFaction ||
                         otherFaction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                        continue;
                    ZenithArchitraveFactionBaseInfo otherData = otherFaction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                    if ( otherData.Territory.Contains( thisPlanet ) )
                    {
                        foundOverlap = true;
                        break;
                    }
                }
                if ( foundOverlap )
                    PossibleExpansionPlanets.Remove( thisPlanet );
            }
            if ( PossibleExpansionPlanets.Count == 0 )
                return null;

            if ( PreferredExpansionPlanets.Count > 0 )
            {
                cb_expansionSortAttachedFaction = AttachedFaction;
                PreferredExpansionPlanets.Sort( static delegate ( Planet Left, Planet Right )
                {
                    int leftHostileStrength = Left.GetPlanetFactionForFaction( cb_expansionSortAttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                    int rightHostileStrength = Right.GetPlanetFactionForFaction( cb_expansionSortAttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                    return leftHostileStrength.CompareTo( rightHostileStrength );
                } );
                if ( tracing ) tracingBuffer.Add( "\tChoosing preferred\n" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return PreferredExpansionPlanets[0];
            }
            cb_expansionSortAttachedFaction = AttachedFaction;
            PossibleExpansionPlanets.Sort( static delegate ( Planet Left, Planet Right )
            {
                int leftHostileStrength = Left.GetPlanetFactionForFaction( cb_expansionSortAttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                int rightHostileStrength = Right.GetPlanetFactionForFaction( cb_expansionSortAttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                if ( Left.GetControllingOrInfluencingFaction().Type == FactionType.Player &&
                     Right.GetControllingOrInfluencingFaction().Type != FactionType.Player )
                    return 1;
                if ( Right.GetControllingOrInfluencingFaction().Type == FactionType.Player &&
                     Left.GetControllingOrInfluencingFaction().Type != FactionType.Player )
                    return -1;

                return leftHostileStrength.CompareTo( rightHostileStrength );
            } );
            if ( tracing )
            {
                tracingBuffer.Add( "\tChoosing possible from list: \n" );
                for ( int i = 0; i < PossibleExpansionPlanets.Count; i++ )
                {
                    tracingBuffer.Add( "\t" ).Add( PossibleExpansionPlanets[i].Name ).Add( "\n" );
                }
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return PossibleExpansionPlanets[0];
        }
        #endregion end GetNextPlanetInTerritory

        #region UpdateMetalReserves
        public void UpdateMetalReserves()
        {
            //figure out how much metal we have to work with! Metal is for building ships.
            //this relies on some non-trivial math to figure out how the various ZA income modifiers apply
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-UpdateMetalReserves-trace", 10f ) : null;
            int income = this.Difficulty.BaseMetalIncome;
            int defensiveIncome = this.Difficulty.BaseDefensiveMetalIncome;
            if ( this.IsQuiesced )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are quiesced, so no income\n" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return;
            }
            if ( this.CivilWarEnemies.Count > 0 || this.ShouldOtherArchitravesAttackMe )
                income = this.Difficulty.BaseMetalIncomeCivilWar;
            //Update the ZA income for when the ZA is hacked.
            if ( this.IsBeingHacked )
                income *= 2;
            if ( tracing )
                tracingBuffer.Add( " Update Metal Reserves: base income " + income ).Add( " at " ).Add( World_AIW2.Instance.GameSecond ).Add( ". BeingHacked " + this.IsBeingHacked + "\n" );
            this.AppliedModifiers_ForUI = "Here's how we calculated the above income: Base Income " + income + ".\n";
            if ( this.IsInWarFooting )
            {
                int timeAtWar = World_AIW2.Instance.GameSecond - this.TimeEnteredWarFooting;
                //first scan all the available modifiers to see if any need to be added to the list
                //adding them to the AppliedModifiers list (or updating that list)
                for ( int i = 0; i < this.Difficulty.IncomeModifiers.Count; i++ )
                {
                    ArchitraveIncomeModifier modifier = this.Difficulty.IncomeModifiers[i];
                    if ( !DoesModifierApply( modifier ) )
                        continue;

                    bool foundModifier = false;
                    for ( int j = 0; j < this.AppliedModifiers.Count; j++ )
                    {
                        if ( this.AppliedModifiers[j].Name == modifier.Name )
                        {
                            foundModifier = true;
                            break;
                        }
                    }
                    if ( !foundModifier )
                    {
                        if ( tracing ) tracingBuffer.Add( "Adding : " + modifier.ToString() + " to the modifier list" );
                        this.AppliedModifiers.Add( modifier );
                    }
                }
                //then actually apply those modifiers to update the income

                for ( int i = this.AppliedModifiers.Count - 1; i >= 0; i-- )
                {
                    ArchitraveIncomeModifier modifier = this.AppliedModifiers[i];
                    if ( !DoesModifierApply( modifier ) )
                    {
                        //looks like this modifier no longer applies (maybe we conquered all our Territory but are still at war, so we should remove the "Without Full Territory" modifier.
                        this.AppliedModifiers.RemoveAt( i );
                        if ( tracing ) tracingBuffer.Add( "Removing : " + modifier.ToString() + " from the modifier list (no longer applies)" );
                        continue;
                    }
                    if ( modifier.TimeInterval == -1 ) //this is just a flat modification; not based on TimesApplied
                        modifier.TimesApplied = 1;
                    if ( timeAtWar % modifier.TimeInterval == 0 )
                    {
                        modifier.TimesApplied++;
                        this.AppliedModifiers[i] = modifier; //update the value in the list, not just the local copy
                        if ( tracing ) tracingBuffer.Add( "Just updated Times Applied for : " + modifier.ToString() + "." );
                    }
                    if ( modifier.TimesApplied == 0 )
                    {
                        if ( tracing ) tracingBuffer.Add( "Skipping : " + modifier.ToString() + ", it hasn't been applied yet" );
                        continue;
                    }
                    int previousincome = income;
                    //now we apply the modifier
                    if ( modifier.AdditiveIncrease > 0 )
                    {
                        income += modifier.AdditiveIncrease * modifier.TimesApplied;
                    }
                    if ( modifier.MultiplicativeIncrease > FInt.Zero )
                    {
                        income = income + ((income * modifier.MultiplicativeIncrease).IntValue * modifier.TimesApplied);
                    }
                    if ( modifier.Multiplier > FInt.Zero )
                    {
                        FInt mult = modifier.Multiplier;
                        for ( int j = 0; j < modifier.TimesApplied; j++ )
                            mult *= modifier.Multiplier;

                        income = (income * mult).IntValue;
                    }
                    if ( !String.IsNullOrEmpty( modifier.UnitTag ) )
                        throw new Exception( "Unit tag not implemented for ZA income modifier" );
                    if ( tracing )
                        tracingBuffer.Add( "\tAfter applying modifier " ).Add( modifier.ToString() ).Add( " our income is updated; " ).Add( previousincome ).Add( " --> " ).Add( income ).Add( "\n" );
                    this.AppliedModifiers_ForUI += "\t" + modifier.ToStringForDisplay() + "\n";
                }
            }
            else
            {
                //not at war; clear our modifiers
                this.AppliedModifiers.Clear();
            }

            this.IncomeLastSecond = income;
            this.MetalReserves += income;

            this.DefensiveMetalReserves += defensiveIncome; //defensive metal reserves don't get bigger if in war

            if ( this.IsInWarFooting && //we are at war AND
                 (this.CivilWarEnemies.Count > 0 || //we are in a civil war  OR
                   this.ShouldOtherArchitravesAttackMe ||
                   this.OverallPowerLevelOfEnemies >= FInt.FromParts( 4, 000 )) ) //our enemies are pretty strong
                this.GolemMetalReserves += income; //then we can build golems

            if ( this.MetalReserves > this.Difficulty.MaxMetalReserves )
                this.MetalReserves = this.Difficulty.MaxMetalReserves;
            if ( this.GolemMetalReserves > this.Difficulty.MaxGolemMetalReserves )
                this.GolemMetalReserves = this.Difficulty.MaxGolemMetalReserves;
            if ( this.DefensiveMetalReserves > this.Difficulty.MaxMetalReserves )
                this.DefensiveMetalReserves = this.Difficulty.MaxMetalReserves;

            if ( tracing ) tracingBuffer.Add( " New metal regular : " + this.MetalReserves + " golem metal: " + this.GolemMetalReserves + "\n" );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion end UpdateMetalReserves

        #region DoesModifierApply
        private bool DoesModifierApply( ArchitraveIncomeModifier modifier )
        {
            //used by the Income code
            if ( !IsInWarFooting )
                return false;
            if ( modifier.FallenSpireCities > 0 )
            {
                int cities = FallenSpireFactionBaseInfo.Instance == null ? 0 : FallenSpireFactionBaseInfo.Instance.SpireCities.Count;
                if ( cities == 0 || cities < modifier.FallenSpireCities )
                    return false;
            }
            if ( modifier.CivilWarOffensiveOnly &&
                 CivilWarEnemies.Count <= 0 )
                return false;
            if ( modifier.CivilWarDefensive && !ShouldOtherArchitravesAttackMe )
                return false;
            if ( (CivilWarEnemies.Count > 0 || ShouldOtherArchitravesAttackMe)
                 && !(modifier.CivilWarDefensive || modifier.CivilWarOffensiveOnly) ) //if we are in the civil war, only civil war modifiers apply
                return false;
            if ( modifier.WithoutFullTerritory && this.Spawners.Count >= MaxTerritorySize )
                return false;
            if ( modifier.ControlsLessThanXPlanets > 0 &&
                 this.Spawners.Count >= modifier.ControlsLessThanXPlanets )
                return false;
            return true;
        }
        #endregion
    }
}
