using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    /* The scourge do offense/defense in units of "Fireteams" */
    public class ScourgeFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //This is currently just used for the master Fireteams list, and interfaces into that
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "ScourgeFactionBaseInfo-Teams" );
        public Int16 CountdownTimerForNemesis = -1;
        public Int16 CountdownTimerForNextSubjugator = -1;

        //for high intensity ai-allied scourge, build an early spawner very far from the AI homeworld and the player homeworld; this is to make it hard for a clever player
        //to trap the scourge in one part of the galaxy
        public bool HasDoneFarFlungBuild;
        //scourge infrastructure isn't allowed to rebuild  for "a while" after being killed
        //this dictionary maps  from <planet index> to <seconds till can rebuild>
        public readonly Dictionary<Int16, int> SecondsUntilCanRebuild = Dictionary<short, int>.Create_WillNeverBeGCed( 300, "ScourgeFactionBaseInfo-SecondsUntilCanRebuild" ); 
                                                                                                      
        public int LastTimeNemesisExisted; //we want a nice delay between Nemesis appearances, if the player can kill it
        public int MetalStoredForCloakedBuilders;

        //these control periodic scourge invasions mid-game
        public int TimeForNextScourgeInvasionCheck;
        public int ScourgeInvasionBudget;
        public readonly Dictionary<GameEntityTypeData, int> ScourgeInvasionForce = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 500, "ScourgeFactionBaseInfo-ScourgeInvasionForce" );
        public int ScourgeInvasionForceStrength;

        //non-serialized data (mostly used for UI)
        public List <VassalMission> EconomicMissions = List<VassalMission>.Create_WillNeverBeGCed( 50, "Scourge-EconomicVassalMissions" );

        public int Intensity = 0;
        public bool IsFactionTrapped; //for ai-allied scourge, to know if the player is trying to cut it off from places it can expand. Is refreshed regularly, so don't serialize it
        public FInt OverCap = FInt.Zero; //if this number is > 0, start making fewer ships. The higher the number the fewer ships
        public readonly DoubleBufferedValue<int> NumTopTierSpawnersInGalaxy = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> EvolvedWarriors_ForUI = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> HybridWarriors_ForUI = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> WarriorsThatCouldUpgrade = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> WarriorsOffToUpgrade = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<bool> ScourgeIsSuppressed = new DoubleBufferedValue<bool>( false );

        public readonly DoubleBufferedArray<int> BaseWarriorsByMark_ForUI = DoubleBufferedArray<int>.Create_WillNeverBeGCed( 10, 0, "Scourge-BaseWarriorsByMark_ForUI" );
        public readonly DoubleBufferedArray<int> EvolvedWarriorsByMark_ForUI = DoubleBufferedArray<int>.Create_WillNeverBeGCed( 10, 0, "Scourge-EvolvedWarriorsByMark_ForUI" );
        public readonly DoubleBufferedArray<int> HybridWarriorsByMark_ForUI = DoubleBufferedArray<int>.Create_WillNeverBeGCed( 10, 0, "Scourge-HybridWarriorsByMark_ForUI" );

        //Some scourge structures aren't allowed to be too close to eachother (lest the Scourge be too OP)
        //So rather than iterate over lots of planets Entities lists, we instead track all of them here
        public readonly DoubleBufferedList<SafeSquadWrapper> SpawnersInGalaxy = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-SpawnersInGalaxy" );
        public readonly DoubleBufferedList<SafeSquadWrapper> ArmoriesInGalaxy = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-ArmoriesInGalaxy" );
        public readonly DoubleBufferedList<SafeSquadWrapper> FortressesInGalaxy = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-FortressesInGalaxy" );

        //most scourge structures warp in over time
        //These require a DoubleBufferedConcurrentList rather than the usual DoubleBufferedList, because we're adding to them
        //from the sim thread directly.
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> WarpingInSpawners = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-WarpingInSpawners");
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> WarpingInArmories = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-WarpingInArmories" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> WarpingInFortresses = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-SpawnersInGalaxy" );
        public readonly Dictionary<Planet, int> NeophytesPerPlanet = Dictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ScourgeFactionBaseInfoRoot-NeophytesPerPlanet" );

        public readonly DoubleBufferedList<SafeSquadWrapper> NemesesInGalaxy = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-NemesesInGalaxy" );
        public readonly DoubleBufferedList<SafeSquadWrapper> SubjugatorsInGalaxy = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-SubjugatorsInGalaxy" );

        public readonly DoubleBufferedList<SafeSquadWrapper> BuildersInGalaxy = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-BuildersInGalaxy" );
        //we use these to dynamically adjust whether builders should be required to upgrade
        public readonly DoubleBufferedValue<int> UpgradableInfrastructureCount = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> TotalInfrastructureCount = new DoubleBufferedValue<int>( 0 );

        public ScourgeDifficulty Difficulty = null;
        public int HighestScienceEarnedByPlayer = 0;
        public bool PlayerAllied = false;
        public bool MinorFactionAllied = false;
        public bool AIAllied = false;
        public bool InCivilWar = false;
        public bool SmartBuilders = false;

        //constants

        public readonly int RangeForUpgrade = 2000; //for stuff like "Warriors getting to armories to upgrade"

        //here are some C#-only constants
        public readonly FInt RetreatPathRemoteShipDangerBaseDivisor = FInt.Zero;
        public readonly FInt RetreatPathRemoteShipDivisorIncreaseRate = FInt.FromParts( 3, 00 );
        public readonly FInt RoutingPastRemoteShipDangerBaseDivisor = FInt.FromParts( 2, 000 );
        public readonly FInt RoutingPastRemoteShipDivisorIncreaseRate = FInt.FromParts( 5, 000 );
        //for the attack path, we either need to beat the defenses (conquest/neuter) or snipe the target
        public readonly FInt FullBattleRemoteShipDangerBaseDivisor = FInt.FromParts( 2, 000 );
        public readonly FInt FullBattleRemoteShipDivisorIncreaseRate = FInt.FromParts( 2, 000 );
        public readonly FInt TargetSnipeRemoteShipDangerBaseDivisor = FInt.FromParts( 5, 000 );
        public readonly FInt TargetSnipeRemoteShipDivisorIncreaseRate = FInt.FromParts( 2, 000 );

        public ScourgeFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Teams.Clear();
            CountdownTimerForNemesis = -1;
            CountdownTimerForNextSubjugator = -1;

            HasDoneFarFlungBuild = false;
            SecondsUntilCanRebuild.Clear();
            LastTimeNemesisExisted = -1;
            MetalStoredForCloakedBuilders = -1;

            TimeForNextScourgeInvasionCheck = -1;
            ScourgeInvasionBudget = -1;
            ScourgeInvasionForce.Clear();
            ScourgeInvasionForceStrength = -1;

            //non-serialized
            Intensity = 0;
            OverCap = FInt.Zero;
            IsFactionTrapped = false;
            NumTopTierSpawnersInGalaxy.Clear();
            ScourgeIsSuppressed.Clear();
            EvolvedWarriors_ForUI.Clear();
            HybridWarriors_ForUI.Clear();
            WarriorsThatCouldUpgrade.Clear();
            WarriorsOffToUpgrade.Clear();
            BaseWarriorsByMark_ForUI.Clear();
            EvolvedWarriorsByMark_ForUI.Clear();
            HybridWarriorsByMark_ForUI.Clear();
            NeophytesPerPlanet.Clear();

            EconomicMissions.Clear();

            SpawnersInGalaxy.Clear();
            ArmoriesInGalaxy.Clear();
            FortressesInGalaxy.Clear();

            WarpingInSpawners.Clear();
            WarpingInArmories.Clear();
            WarpingInFortresses.Clear();

            NemesesInGalaxy.Clear();
            SubjugatorsInGalaxy.Clear();

            BuildersInGalaxy.Clear();
            UpgradableInfrastructureCount.Clear();
            TotalInfrastructureCount.Clear();

            this.Difficulty = null;
            HighestScienceEarnedByPlayer = 0;
            PlayerAllied = false;
            MinorFactionAllied = false;
            AIAllied = false;
            InCivilWar = false;
            SmartBuilders = false;

            HaveLoadedDataForThisthis = false; //force reload of xml
        }

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ScourgeFactionBaseInfo" );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Serialize Scourge Fireteams" );
            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.CountdownTimerForNemesis );
            Buffer.AddBool( MetaData, this.HasDoneFarFlungBuild );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)SecondsUntilCanRebuild.Count, "SecondsUntilCanRebuild.Count" );
            foreach ( KeyValuePair<short, int> kv in SecondsUntilCanRebuild )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, kv.Key, "planetIndex" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, kv.Value, "SecondsUntilCanRebuild" );
            }
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, LastTimeNemesisExisted );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, CountdownTimerForNextSubjugator );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, MetalStoredForCloakedBuilders );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Scourge Invasion" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextScourgeInvasionCheck );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, ScourgeInvasionBudget );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, ScourgeInvasionForceStrength );
            if ( ScourgeInvasionForce == null )
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, 0 );
            else
            {
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)ScourgeInvasionForce.Count );
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in ScourgeInvasionForce )
                {
                    GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, pair.Key, Buffer, "ScourgeInvasionForce" );
                    Buffer.AddInt32( MetaData, ReadStyle.NonNeg, pair.Value );
                }
            }
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ScourgeFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "ScourgeFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Serialize Scourge Fireteams" );
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "scourge" );
            CountdownTimerForNemesis = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            HasDoneFarFlungBuild = Buffer.ReadBool( MetaData );
            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "SecondsUntilCanRebuild.Count" );
            for ( int i = 0; i < count; i++ )
            {
                Int16 planetIndex = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "planetIndex" );
                SecondsUntilCanRebuild[planetIndex] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "SecondsUntilCanRebuild" );
            }
            LastTimeNemesisExisted = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            CountdownTimerForNextSubjugator = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            MetalStoredForCloakedBuilders = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Scourge Invasion" );
            TimeForNextScourgeInvasionCheck = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            ScourgeInvasionBudget = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            ScourgeInvasionForceStrength = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );

            count = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            for ( int i = 0; i < count; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "ScourgeInvasionForce" );
                ScourgeInvasionForce[data] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            }
            Buffer.StopTrackerByName( "ScourgeFactionBaseInfo Ext" );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 70 + (Intensity * 5);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Scourge" );
            return load;
        }

        #region Xml Constants
        private bool HaveLoadedDataForThisthis = false;
        //Note that Adjacent planets are 1 hop apart (hop == wormhole)
        public int RequiredHopsBetweenSpawners;
        public int RequiredHopsBetweenArmories;
        public int InfrastructureRebuildDelay; //If you kill a scourge spawner/armory, it can't rebuild instantly
        public int MaxFortressLevel = 7;
        public readonly List<FInt> NeophyteCostMultiplierList = List<FInt>.Create_WillNeverBeGCed( 12, "ScourgeFactionBaseInfo-NeophyteCostMultiplierList" );
        public FInt NeophyteCostMultiplier = FInt.One;
        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedDataForThisthis )
                return;
            this.HaveLoadedDataForThisthis = true;

            //This is settings read directly from the XML
            this.RequiredHopsBetweenSpawners = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Scourge_RequiredHopsBetweenSpawners" );
            this.RequiredHopsBetweenArmories = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Scourge_RequiredHopsBetweenArmories" );
            this.AttachedFaction.MinFireteamStrength = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Scourge_MinFireteamStrength" );
            this.AttachedFaction.MaxFireteamStrength = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Scourge_MaxFireteamStrength" );
            this.InfrastructureRebuildDelay = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Scourge_InfrastructureRebuildDelay" );
            this.NeophyteCostMultiplierList.Clear();
            this.NeophyteCostMultiplierList.Add( ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Scourge_NeophyteCostMultiplierEnemyTier1" ) );
            this.NeophyteCostMultiplierList.Add( ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Scourge_NeophyteCostMultiplierEnemyTier2" ) );
            this.NeophyteCostMultiplierList.Add( ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Scourge_NeophyteCostMultiplierEnemyTier3" ) );
            this.NeophyteCostMultiplierList.Add( ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Scourge_NeophyteCostMultiplierEnemyTier4" ) );
            this.NeophyteCostMultiplierList.Add( ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Scourge_NeophyteCostMultiplierEnemyTier5" ) );
            if ( this.AttachedFaction.MinFireteamStrength == 0 || this.AttachedFaction.MaxFireteamStrength == 0 )
                throw new Exception( "Problem parsing scourge external constants" );
            //some of these values are cached in this so I can send them to the UI for Nemesis-related notifications
            if ( this.NeophyteCostMultiplierList.Count < 3 )
                throw new Exception( "more scourge xml problems" );
        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            this.Difficulty = ScourgeDifficultyTable.Instance.GetDifficultyForFaction( this.AttachedFaction );
        }
        #endregion

        #region GetScourgeStateForNotifications
        public static void GetScourgeStateForNotifications( Faction faction, out int TopTierSpawners_out, out int TopTierSpawnersPerSubjugator_out, out int CountdownForNemesisSpawning_out )
        {
            if ( faction == null )
            {
                TopTierSpawners_out = 0;
                TopTierSpawnersPerSubjugator_out = 0;
                CountdownForNemesisSpawning_out = 0;
                return;
            }
            //since this is a static method we need to get the correct values from this particular faction, which are stored in this as a result
            ScourgeFactionBaseInfo localCopyOfthis = faction.TryGetExternalBaseInfoAs<ScourgeFactionBaseInfo>();
            if ( localCopyOfthis == null )
            {
                TopTierSpawners_out = 0;
                TopTierSpawnersPerSubjugator_out = 0;
                CountdownForNemesisSpawning_out = 0;
                return;
            }
            TopTierSpawners_out = localCopyOfthis.NumTopTierSpawnersInGalaxy.Display;
            TopTierSpawnersPerSubjugator_out = localCopyOfthis.Difficulty.TopTierSpawnersRequiredPerSubjugator;
            CountdownForNemesisSpawning_out = localCopyOfthis.CountdownTimerForNemesis;
        }
        #endregion

        #region GetScourgeStateForDisplay
        public void GetScourgeStateForDisplay( ArcenDoubleCharacterBuffer output )
        {
            if ( this.TimeForNextScourgeInvasionCheck > 0 )
            {
                output.Add( "Invasion details\n" );
                output.Add( "\tWill next check for scourge invasion in " ).Add( this.TimeForNextScourgeInvasionCheck - World_AIW2.Instance.GameSecond ).Add( " seconds" ).Add( "\n" );
                output.Add( "\tBudget: " ).Add( this.ScourgeInvasionBudget ).Add( "\n" );
                output.Add( "\tApprox strength: " ).Add( this.ScourgeInvasionForceStrength ).Add( "\n" );
                if ( this.ScourgeInvasionForce != null )
                {
                    foreach ( KeyValuePair<GameEntityTypeData, int> pair in this.ScourgeInvasionForce )
                    {
                        output.Add( "\t\t" ).Add( pair.Key.GetDisplayName() ).Add( ": " ).Add( pair.Value ).Add( "\n" );
                    }
                }
            }
            if ( this == null || this.Teams.GetItemCount() == 0 )
                return;
            output.Add( "\nState of Scourge Fireteams for intensity " ).Add( this.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant(), "22a199" ).Add( "  <" ).Add( this.Allegiance ).Add( ">:\n" );
            output.Add( "There are " ).Add( this.AttachedFaction.NumFireteams ).Add( " fireteams, min strength " ).Add( this.AttachedFaction.MinFireteamStrength ).Add( " max strength " ).Add( this.AttachedFaction.MaxFireteamStrength ).Add( "\n" );

            if ( this.AttachedFaction.HasObtainedSpireDebris )
                output.Add( "Has obtained spire debris\n" );
            if ( this.IsFactionTrapped )
                output.Add( "This faction is trapped. Cloaked builder metal " ).Add( this.MetalStoredForCloakedBuilders ).Add( "\n" );
            {
                bool isFirst = true;
                List<SafeSquadWrapper> armories = this.ArmoriesInGalaxy.GetDisplayList();
                foreach ( SafeSquadWrapper wrap in armories )
                {
                    GameEntity_Squad squad = wrap.GetSquad();
                    if ( squad == null )
                        continue;
                    if ( isFirst )
                        isFirst = false;
                    else
                        output.Add( ", " );

                    ScourgePerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    ScourgeTypeData typedata = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
                    string typename = "Unknown";
                    if ( typedata != null )
                        typename = typedata.name;

                    output.Add( "<color=#33bbff>" ).Add( typename ).Add( " Armory</color> on <color=#aa4411>" ).Add( squad.GetPlanetName_Safe() 
                        ).Add( "</color> mark <color=#adda44>" ).Add( squad.CurrentMarkLevel ).Add( "</color>" );
                }
                foreach ( GameEntity_Squad armory in this.WarpingInArmories.DisplaySquads() )
                {
                    if ( isFirst )
                        isFirst = false;
                    else
                        output.Add( ", " );
                    output.Add( "Warping In Armory on " ).Add( armory.GetPlanetName_Safe() );
                }
            }
            {
                bool isFirst = true;
                List<SafeSquadWrapper> spawners = this.SpawnersInGalaxy.GetDisplayList();
                foreach ( SafeSquadWrapper wrap in spawners )
                {
                    GameEntity_Squad squad = wrap.GetSquad();
                    if ( squad == null )
                        continue;
                    if ( isFirst )
                        isFirst = false;
                    else
                        output.Add( ", " );

                    ScourgePerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    ScourgeTypeData typedata = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
                    string typename = "Unknown";
                    if ( typedata != null )
                        typename = typedata.name;

                    output.Add( "<color=#bb33ff> Spawner</color> on <color=#aa44bb>" ).Add( squad.GetPlanetName_Safe() ).Add( "</color> mark <color=#adda44>" 
                        ).Add( squad.CurrentMarkLevel + "</color>" );
                }
                foreach ( GameEntity_Squad spawner in this.WarpingInSpawners.DisplaySquads() )
                {
                    if ( isFirst )
                        isFirst = false;
                    else
                        output.Add( ", " );
                    output.Add( "Warping In Spawner on " ).Add( spawner.GetPlanetName_Safe() );
                }
            }
            {
                bool isFirst = true;
                List<SafeSquadWrapper> fortresss = this.FortressesInGalaxy.GetDisplayList();
                foreach ( SafeSquadWrapper wrap in fortresss )
                {
                    GameEntity_Squad squad = wrap.GetSquad();
                    if ( squad == null )
                        continue;
                    if ( isFirst )
                        isFirst = false;
                    else
                        output.Add( ", " );

                    ScourgePerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    ScourgeTypeData typedata = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
                    string typename = "Unknown";
                    if ( typedata != null )
                        typename = typedata.name;

                    output.Add( "<color=#bb33ff> Fortress</color> on <color=#aa44bb>" ).Add( squad.GetPlanetName_Safe() ).Add( "</color> mark <color=#adda44>"
                        ).Add( squad.CurrentMarkLevel + "</color>" );
                }
                foreach ( GameEntity_Squad fortress in this.WarpingInFortresses.DisplaySquads() )
                {
                    if ( isFirst )
                        isFirst = false;
                    else
                        output.Add( ", " );
                    output.Add( "Warping In Fortress on " ).Add( fortress.GetPlanetName_Safe() );
                }
            }
            output.Add( "\n" );
            output.Add( "\n" ).Add( SpawnersInGalaxy.Count ).Add( " spawners, " ).Add( ArmoriesInGalaxy.Count ).Add( " armories." ).Add( "\n" );
            List<SafeSquadWrapper> builders = this.BuildersInGalaxy.GetDisplayList();
            output.Add( "Num Builders: " ).Add( builders.Count ).Add( " (" );
            if ( builders.Count == 0 )
                output.Add( "No builders" );
            else
            {
                output.Add( "Builders on " );
                for ( int i = 0; i < builders.Count; i++ )
                {
                    if ( i > 0 )
                        output.Add( ", " );
                    output.Add( builders[i].GetPlanetName_Safe() );
                }
                output.Add(".\n");
                if ( this.AttachedFaction.IsVassal )
                {
                    for ( int i = 0; i < builders.Count; i++ )
                    {
                        GameEntity_Squad builder = builders[i].GetSquad();
                        if ( builder == null )
                            continue;
                        ScourgePerUnitBaseInfo bData = builder.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                        if ( bData.IsAssignedToMission )
                        {
                            output.Add( builder.ToStringWithPlanet() ).Add(" has a mission. ");
                            if ( bData.MyMission != null )
                                output.Add( bData.MyMission.ToStringForDisplay() );
                            output.Add("\n");
                        }
                    }

                }
            }
            output.Add( "metal income " ).Add( this.Difficulty.BaseSpawnerMetalIncome ).Add( "\n" );
            output.Add( "Top tier spawners in galaxy: <color=#aaaaff>" ).Add( this.NumTopTierSpawnersInGalaxy.Display ).Add( "</color>.\n" );
            output.Add( "Top Tier spawners required per Subjugator: <color=#aaaaff>" ).Add( this.Difficulty.TopTierSpawnersRequiredPerSubjugator ).Add( "</color>.\n" );
            output.Add( "Hybrids by mark level: " );
            for ( int i = 1; i <= this.HybridWarriorsByMark_ForUI.Length && i <= 7; i++ )
                output.Add( i ).Add( ": " ).Add( this.HybridWarriorsByMark_ForUI.Display[i], "80ff80" ).Add( ", " );
            output.Add( "\nEvolved by mark level: " );
            for ( int i = 1; i <= this.EvolvedWarriorsByMark_ForUI.Length && i <= 7; i++ )
                output.Add( i ).Add( ": " ).Add( this.EvolvedWarriorsByMark_ForUI.Display[i], "a1ffa1" ).Add( ", " );
            output.Add( "\nBase Warriors by mark level: " );
            for ( int i = 1; i <= this.BaseWarriorsByMark_ForUI.Length && i <= 7; i++ )
                output.Add( i ).Add( ": " ).Add( this.BaseWarriorsByMark_ForUI.Display[i], "60ff60" ).Add( ", " );

            output.Add( "\nTotal number of Evolved warriors: " ).Add( this.EvolvedWarriors_ForUI.Display, "a1ffa1" ).Add( " Hybrids: " ).Add( this.HybridWarriors_ForUI.Display, "a1ffa1" ).Add( " Subjugators " ).Add( this.SubjugatorsInGalaxy.Count, "a1ffa1" ).Add( "\n" );
            output.Add( " Warriors that could upgrade but that are busy: " ).Add( this.WarriorsThatCouldUpgrade.Display, "ffa1a1" ).Add( ", warriors that are off to upgrade " ).Add( this.WarriorsOffToUpgrade.Display, "a1a1ff" ).Add( "\n" );
            int totalStrength = 0;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( this.Teams ) )
            {
                totalStrength += team.DeepInfo.TeamStrength;
                if ( team.status != FireteamStatus.Disbanded )
                {
                    team.DeepInfo.GetStringForDisplay( output );
                    output.Add( "\n" );
                }
            }
            output.Add( "Total Scourge Strength in " ).Add( this.Teams.GetItemCount() ).Add( " fireteams: <color=#ff0000>" ).Add( (totalStrength / 1000) ).Add( "</color>.\n" );
            if ( this.Teams.GetItemCount() > 10 )
                output.Add( this.Allegiance, "ffa155" ).Add( "\n" );

            foreach ( KeyValuePair<short, int> kv in SecondsUntilCanRebuild )
            {
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( kv.Key );
                if ( kv.Value < 580 )
                    output.Add( "\tCan't rebuild on " ).Add( planet.Name ).Add( " for " ).Add( kv.Value ).Add( " seconds\n" );
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugStage = 1;
            try
            {
                debugStage = 100;
                //We iterate over all units and make some lists of things that we refer to later here
                UpdateAllegiance();

                debugStage = 200;
                //Count the spawners and armories here
                SpawnersInGalaxy.ClearConstructionListForStartingConstruction();
                ArmoriesInGalaxy.ClearConstructionListForStartingConstruction();
                FortressesInGalaxy.ClearConstructionListForStartingConstruction();
                WarpingInSpawners.ClearConstructionListForStartingConstruction();
                WarpingInArmories.ClearConstructionListForStartingConstruction();
                WarpingInFortresses.ClearConstructionListForStartingConstruction();
                NemesesInGalaxy.ClearConstructionListForStartingConstruction();
                SubjugatorsInGalaxy.ClearConstructionListForStartingConstruction();
                BuildersInGalaxy.ClearConstructionListForStartingConstruction();
                NeophytesPerPlanet.Clear();
                EconomicMissions.Clear();

                debugStage = 500;
                if ( Difficulty == null )
                    this.Difficulty = ScourgeDifficultyTable.Instance.GetDifficultyForFaction( this.AttachedFaction );

                debugStage = 600;
                this.HighestScienceEarnedByPlayer = GetHighestScienceEarnedByPlayerFaction();

                debugStage = 900;
                this.UpgradableInfrastructureCount.ClearConstructionValueForStartingConstruction();
                this.TotalInfrastructureCount.ClearConstructionValueForStartingConstruction();
                this.HybridWarriors_ForUI.ClearConstructionValueForStartingConstruction();
                this.EvolvedWarriors_ForUI.ClearConstructionValueForStartingConstruction();
                this.WarriorsThatCouldUpgrade.ClearConstructionValueForStartingConstruction();
                this.WarriorsOffToUpgrade.ClearConstructionValueForStartingConstruction();
                this.ScourgeIsSuppressed.ClearConstructionValueForStartingConstruction();
                this.NumTopTierSpawnersInGalaxy.ClearConstructionValueForStartingConstruction();

                this.BaseWarriorsByMark_ForUI.ClearConstructionArrayForStartingConstruction();
                this.EvolvedWarriorsByMark_ForUI.ClearConstructionArrayForStartingConstruction();
                this.HybridWarriorsByMark_ForUI.ClearConstructionArrayForStartingConstruction();

                debugStage = 2100;
                
                FactionUtilityMethods.Instance.GetActiveVassalMissions( AttachedFaction, VassalMissionType.Construction, this.EconomicMissions );
                debugStage = 2101;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeBeacon" ) )
                {
                    ScourgeIsSuppressed.Construction = true;
                    break;
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeNeophyte" ) )
                {
                    this.NeophytesPerPlanet[entity.Planet]++;
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeBuilder" ) )
                {
                    BuildersInGalaxy.AddToConstructionList( entity );
                    ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    if ( data == null )
                        continue;
                    if ( data.IsAssignedToMission && data.MyMission == null )
                    {
                        GetMyAssignedEconomicMission( entity, data );
                        if ( data.MyMission == null )
                            data.IsAssignedToMission = false;
                    }
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeNemesis", "WarpingInScourgeNemesis" ) )
                {
                    NemesesInGalaxy.AddToConstructionList( entity );
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
                {
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeSubjugator" ) )
                {
                    SubjugatorsInGalaxy.AddToConstructionList( entity );
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "WarpingInScourgeSpawner" ) )
                {
                    WarpingInSpawners.AddToConstructionList( entity );
                    this.SecondsUntilCanRebuild[entity.Planet.Index] = InfrastructureRebuildDelay;
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
                {
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "WarpingInScourgeArmory" ) )
                {
                    WarpingInArmories.AddToConstructionList( entity );
                    this.SecondsUntilCanRebuild[entity.Planet.Index] = InfrastructureRebuildDelay;
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "WarpingInScourgeFortress" ) )
                {
                    WarpingInFortresses.AddToConstructionList( entity );
                }
                debugStage = 99100;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeWarrior" ) )
                {
                    debugStage = 99200;
                    ScourgePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    debugStage = 99300;
                    if ( data == null )
                        continue;
                    if ( data.Experience >= data.ExperienceForNextLevel )
                    {
                        debugStage = 99400;
                        if ( data.IsOffToUpgrade )
                            this.WarriorsOffToUpgrade.Construction++;
                        else
                            this.WarriorsThatCouldUpgrade.Construction++;
                    }
                    debugStage = 99500;
                    if ( data.IsHybrid )
                    {
                        debugStage = 99600;
                        this.HybridWarriors_ForUI.Construction++;
                        this.HybridWarriorsByMark_ForUI.Construction[entity.CurrentMarkLevel]++;
                    }
                    else if ( data.IsEvolved )
                    {
                        debugStage = 99700;
                        this.EvolvedWarriors_ForUI.Construction++;
                        this.EvolvedWarriorsByMark_ForUI.Construction[entity.CurrentMarkLevel]++;
                    }
                    else
                    {
                        debugStage = 99800;
                        this.BaseWarriorsByMark_ForUI.Construction[entity.CurrentMarkLevel]++;
                    }
                    debugStage = 99900;
                }
                debugStage = 101100;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeSpawner" ) )
                {
                    TotalInfrastructureCount.Construction++;
                    ScourgePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    if ( data.ExperienceForNextLevel <= data.Experience && entity.CurrentMarkLevel != 7 )
                        UpgradableInfrastructureCount.Construction++;
                    this.SecondsUntilCanRebuild[entity.Planet.Index] = InfrastructureRebuildDelay;
                    SpawnersInGalaxy.AddToConstructionList( entity );
                    if ( entity.CurrentMarkLevel == 7 )
                        this.NumTopTierSpawnersInGalaxy.Construction++;
                }
                debugStage = 102100;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeArmory" ) )
                {
                    TotalInfrastructureCount.Construction++;
                    this.SecondsUntilCanRebuild[entity.Planet.Index] = InfrastructureRebuildDelay;
                    ArmoriesInGalaxy.AddToConstructionList( entity );
                    ScourgePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    if ( data.ExperienceForNextLevel <= data.Experience && entity.CurrentMarkLevel != 7 )
                        UpgradableInfrastructureCount.Construction++;
                }
                debugStage = 103100;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ScourgeFortress" ) )
                {
                    FortressesInGalaxy.AddToConstructionList( entity );
                }

                debugStage = 108100;
                this.ScourgeIsSuppressed.SwitchConstructionToDisplay();
                this.NumTopTierSpawnersInGalaxy.SwitchConstructionToDisplay();
                this.UpgradableInfrastructureCount.SwitchConstructionToDisplay();
                this.TotalInfrastructureCount.SwitchConstructionToDisplay();
                this.HybridWarriors_ForUI.SwitchConstructionToDisplay();
                this.EvolvedWarriors_ForUI.SwitchConstructionToDisplay();
                this.WarriorsThatCouldUpgrade.SwitchConstructionToDisplay();
                this.WarriorsOffToUpgrade.SwitchConstructionToDisplay();

                this.BaseWarriorsByMark_ForUI.SwitchConstructionToDisplay();
                this.EvolvedWarriorsByMark_ForUI.SwitchConstructionToDisplay();
                this.HybridWarriorsByMark_ForUI.SwitchConstructionToDisplay();

                debugStage = 112100;
                SpawnersInGalaxy.SwitchConstructionToDisplay();
                ArmoriesInGalaxy.SwitchConstructionToDisplay();
                FortressesInGalaxy.SwitchConstructionToDisplay();
                WarpingInSpawners.SwitchConstructionToDisplay();
                WarpingInArmories.SwitchConstructionToDisplay();
                WarpingInFortresses.SwitchConstructionToDisplay();
                NemesesInGalaxy.SwitchConstructionToDisplay();
                SubjugatorsInGalaxy.SwitchConstructionToDisplay();
                BuildersInGalaxy.SwitchConstructionToDisplay();

                debugStage = 113100;
                debugStage = 114100;
                if ( NemesesInGalaxy.Count > 0 )
                    this.LastTimeNemesisExisted = World_AIW2.Instance.GameSecond;

                debugStage = 115100;
                NeophyteCostMultiplier = FInt.One;
                FInt EnemyOverallPowerLevel = GetMinorFactionEnemyPowerLevel();

                debugStage = 116100;
                if ( EnemyOverallPowerLevel >= FInt.One &&
                     NeophyteCostMultiplierList != null && NeophyteCostMultiplierList.Count > 0 )
                {
                    debugStage = 116200;
                    //Scourge get a slight power boost as enemy power level rises; it makes neophytes cheaper
                    int idx = EnemyOverallPowerLevel.IntValue;
                    if ( idx >= NeophyteCostMultiplierList.Count )
                        idx = NeophyteCostMultiplierList.Count - 1;
                    debugStage = 116300;
                    NeophyteCostMultiplier = NeophyteCostMultiplierList[idx];
                }

                //if we need to throttle
                this.OverCap = FactionUtilityMethods.Instance.GetCapRatio( this.AttachedFaction );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in Scourge Stage2 debugStage " + debugStage + ", Exception: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion end DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost

        #region GetMyAssignedEconomicMission
        private void GetMyAssignedEconomicMission( GameEntity_Squad entity, ScourgePerUnitBaseInfo data )
        {
            for ( int i = 0; i < this.EconomicMissions.Count; i++ )
            {
                VassalMission mission = this.EconomicMissions[i];
                if ( mission.OptionalEntityHandlingMission.GetSquad() == entity )
                {
                    data.MyMission = mission;
                    return;
                }
            }
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            Faction faction = AttachedFaction;
            if ( ArcenStrings.Equals( faction.BaseInfo.Allegiance, "对AI友好" ) )
            {
                AllegianceHelper.AllyThisFactionToAI( faction );
            }
            else if ( ArcenStrings.Equals( faction.BaseInfo.Allegiance, "对玩家友好" ) )
            {
                AllegianceHelper.AllyThisFactionToHumans( faction );
            }
        }
        #endregion

        #region UpdateAllegiance
        private void UpdateAllegiance()
        {
            bool localDebug = false;
            string allegiance = this.Allegiance;
            if ( string.IsNullOrEmpty( allegiance ) )
                this.SetNewAllegianceIntoCoreSettings( "对AI友好" );
            if ( ArcenStrings.Equals( allegiance, "对AI友好" ) )
            {
                AllegianceHelper.AllyThisFactionToAI( this.AttachedFaction );
                AIAllied = true;
            }
            if ( ArcenStrings.Equals( allegiance, "内战" ) )
            {
                AllegianceHelper.SetAlliesForScourgeCivilWar( this.AttachedFaction );
                this.InCivilWar = true;
                this.SmartBuilders = true;
                this.HasDoneFarFlungBuild = true; //civil war scourge don't do the Far Flung Build mechanism
            }
            else if ( ArcenStrings.Equals( allegiance, "对玩家友好" ) )
            {

                AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
                PlayerAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "小派系小队红" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.AttachedFaction.ToString() + " is on team red", Verbosity.DoNotShow );
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( this.AttachedFaction, "小派系小队红" );
            }
            else if ( ArcenStrings.Equals( allegiance, "小派系小队蓝" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.AttachedFaction.ToString() + " is on team blue", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( this.AttachedFaction, "小派系小队蓝" );
            }
            else if ( ArcenStrings.Equals( allegiance, "小派系小队绿" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.AttachedFaction.ToString() + " is on team green", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( this.AttachedFaction, "小派系小队绿" );
            }
        }
        #endregion

        #region GetHighestScienceEarnedByPlayerFaction
        private int GetHighestScienceEarnedByPlayerFaction()
        {
            int highestScienceEarned = 0;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                FInt scienceEarned = FInt.Zero;
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( otherFaction.Type != FactionType.Player )
                    continue;
                scienceEarned += otherFaction.StoredScience;
                TechHistoryEvent tEvent;
                for ( int j = otherFaction.TechHistory.Count - 1; j >= 0; j-- )
                {
                    tEvent = otherFaction.TechHistory[j];
                    if ( tEvent.GetUpgradeResourceStyle() != UpgradeResourceStyle.Science )
                        continue;

                    scienceEarned += tEvent.costInWhateverResource;
                }
                if ( scienceEarned > highestScienceEarned )
                    highestScienceEarned = scienceEarned.IntValue;
            }
            highestScienceEarned -= 12000; //this is the amount you get at game start + first planet
                                           //actually that can vary, based on campaign type, and galaxy settings
            return highestScienceEarned;
        }
        #endregion

        #region GetMinorFactionEnemyPowerLevel
        public FInt GetMinorFactionEnemyPowerLevel()
        {
            //This is for non-ai factions; ai factions work differently
            FInt totalEnemyFactionPower = FInt.Zero;
            Faction faction = this.AttachedFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( faction == otherFaction )
                    continue;

                if ( !faction.GetIsHostileTowards( otherFaction ) )
                    continue; //only for hostile factions
                if ( otherFaction.Type == FactionType.AI && otherFaction.FactionIsDefeated )
                    continue; //don't count dead AIs
                totalEnemyFactionPower += otherFaction.OverallPowerLevel;
            }
            return totalEnemyFactionPower;
        }
        #endregion

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            //power level is based on A. infrastructure and B. number of hybrids/subjugators, to a max of One
            FInt newResult = FInt.Zero;
            FInt infrastructureComponent = FInt.Zero;
            int infrastructureCount = SpawnersInGalaxy.Count + ArmoriesInGalaxy.Count;
            if ( infrastructureCount > 3 )
            {

                if ( infrastructureCount > 8 )
                {
                    infrastructureComponent = (FInt)(infrastructureCount - 8) / 8;
                    if ( infrastructureComponent > FInt.FromParts( 0, 500 ) )
                        infrastructureComponent = FInt.FromParts( 0, 500 );
                }
                else
                    infrastructureComponent = FInt.FromParts( 0, 100 );
            }
            FInt unitComponent = (FInt)(HybridWarriors_ForUI.Display + SubjugatorsInGalaxy.Count) / 12;
            if ( unitComponent > FInt.FromParts( 0, 500 ) )
                unitComponent = FInt.FromParts( 0, 500 );

            FInt nemesisComponent = FInt.Zero;
            if ( NemesesInGalaxy.Count >= 1 )
                nemesisComponent = FInt.FromParts( 0, 500 );
            newResult = unitComponent + infrastructureComponent + nemesisComponent;

            this.AttachedFaction.OverallPowerLevel = newResult;
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            try
            {
                if ( !this.PlayerAllied && !this.AIAllied && !this.MinorFactionAllied )
                    return false; //the sim code hasn't had a chance to run fully yet
                if ( !PlayerAllied || AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MaraudersKillCommandStations" ) )
                {
                    //Human allied factions will optionally leave an AI command station intact, so as to not drive AIP up so high
                    if ( Target.TypeData.IsCommandStation )
                        return true;
                }
                if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) || Target.TypeData.GetHasTag( "DSAA" ) )
                    return true;
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //this one is ok -- just means the thread is ending for some reason.  guess we will skip fully reporting this one
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception " + e.ToString(), Verbosity.DoNotShow );
            }
            return false;
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion
    }
}
