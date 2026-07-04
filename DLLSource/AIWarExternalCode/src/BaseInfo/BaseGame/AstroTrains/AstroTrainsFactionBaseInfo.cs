using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AstroTrainsFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //serialized
        public int TimeLastDepotSpawned;
        public int TotalTrainsSpawned;
        public int TotalTrainsKilled;
        public int TotalTrainsKilledByPlayer; //specifically, killed by players
        public bool HasWarnedForMedSpawns;
        public bool HasWarnedForHighSpawns;
        public int TrainSpawnCounterForMarkLevel;
        public string TrainToSpawnTag;

        //not serialized
        public static AstroTrainsFactionBaseInfo Instance; //there can only ever be one astro train faction at a time
        public bool ResetAllegiances = false;
        public int Intensity = 0;
        public int DepotSpawnInterval = 0;
        public int TrainSpawnInterval = 0;
        public int PercentTrainMultiSpawn = 0;
        public readonly DoubleBufferedList<Planet> PlanetsWithDepots = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "AstroTrains-PlanetsWithDepots" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Stations = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "AstroTrains-Stations" );
        public readonly DoubleBufferedList<SafeSquadWrapper> AllTrainGuards = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "AstroTrains-AllTrainGuards" );
        //the below ones are marked as concurrent, which is less performant, because the worker threads need to add to the display version of the list
        //as part of normal operations
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> TrainDepots = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "AstroTrains-TrainDepots" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> TrainsEnRoute = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "AstroTrains-TrainsEnRoute" );

        public const string ASTRO_TRAINS_TAG = "AstroTrains";

        #region Custom Data
        private bool HaveLoadedData;
        public int maxDepots = 0;

        public int TrainSpawnIntervalLow = 0;
        public int TrainSpawnIntervalMed = 0;
        public int TrainSpawnIntervalHigh = 0;

        public int DepotSpawnIntervalLow = 0;
        public int DepotSpawnIntervalMed = 0;
        public int DepotSpawnIntervalHigh = 0;
        public readonly List<int> MultiTrainSpawnByIntensity = List<int>.Create_WillNeverBeGCed( 12, "AstroTrainsFactionBaseInfo-MultiTrainSpawnByIntensity" );
        public int MinStationsBeforeDepot = 0;
        public int MaxStationsBeforeDepot = 0;
        public int trainsOnMapPerDepot = 0;
        public bool DebugEarlySpawnDepot = false;
        public int DebugEarlySpawnDepotId = -1;
        public int TrainsKilledForMedSpawn = 0;
        public int TrainsKilledForHighSpawn = 0;
        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            maxDepots = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_BaseMaxDepots" ); //this should be modified by the intensity
            TrainSpawnIntervalLow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_BaseTrainSpawnIntervalLow" );
            TrainSpawnIntervalMed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_BaseTrainSpawnIntervalMed" );
            TrainSpawnIntervalHigh = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_BaseTrainSpawnIntervalHigh" );

            DepotSpawnIntervalLow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_DepotSpawnIntervalLow" );
            DepotSpawnIntervalMed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_DepotSpawnIntervalMed" );
            DepotSpawnIntervalHigh = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_DepotSpawnIntervalHigh" );

            MinStationsBeforeDepot = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_MinStationsRequiredBeforeDepot" );
            MaxStationsBeforeDepot = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_MaxStationsRequiredBeforeDepot" );

            TrainsKilledForMedSpawn = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_TrainsKilledForMedSpawn" );
            TrainsKilledForHighSpawn = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_TrainsKilledForHighSpawn" );

            trainsOnMapPerDepot = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_TrainsAllowedOnMapPerDepot" );
            DebugEarlySpawnDepot = ExternalConstants.Instance.GetCustomBool_Slow( "custom_bool_AstroTrains_DebugEarlySpawnDepot" );
            MultiTrainSpawnByIntensity.Clear();
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity1" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity2" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity3" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity4" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity5" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity6" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity7" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity8" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity9" ) );
            MultiTrainSpawnByIntensity.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_PercentForMultiSpawnIntensity10" ) );
            DebugEarlySpawnDepotId = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AstroTrains_DebugEarlySpawnDepotId" );
            if ( MultiTrainSpawnByIntensity.Count != 10 )
                throw new Exception( "Could not get multi train spawn parsed from XML" );

            if ( TrainsKilledForHighSpawn <= TrainsKilledForMedSpawn )
                throw new Exception( "Problem parsing trains killed for various spawns. High: " + TrainsKilledForHighSpawn + " Med: " + TrainsKilledForMedSpawn );
        }
        #endregion

        public AstroTrainsFactionBaseInfo()
        {
            Cleanup();
        }

        #region Cleanup
        protected override void Cleanup()
        {
            this.TimeLastDepotSpawned = -1;
            this.TotalTrainsSpawned = 0;
            this.TotalTrainsKilledByPlayer = 0;
            this.TotalTrainsKilled = 0;
            this.HasWarnedForMedSpawns = false;
            this.HasWarnedForHighSpawns = false;
            this.TrainSpawnCounterForMarkLevel = 0;
            this.TrainToSpawnTag = "AstroTrainLow";

            Intensity = 0;
            DepotSpawnInterval = 0;
            TrainSpawnInterval = 0;
            PercentTrainMultiSpawn = 0;

            this.ResetAllegiances = false;
            PlanetsWithDepots.Clear();
            TrainDepots.Clear();
            TrainsEnRoute.Clear();
            Stations.Clear();
            AllTrainGuards.Clear();

            Instance = null;
            HaveLoadedData = false; //trigger a reload of xml
        }
        #endregion

        #region Serialization And Deserialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeLastDepotSpawned );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TotalTrainsSpawned );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TotalTrainsKilledByPlayer );
            Buffer.AddBool( MetaData, this.HasWarnedForMedSpawns );
            Buffer.AddBool( MetaData, this.HasWarnedForHighSpawns );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)this.TotalTrainsKilled );
            Buffer.AddString_Condensed( MetaData, this.TrainToSpawnTag );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TrainSpawnCounterForMarkLevel );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TimeLastDepotSpawned = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.TotalTrainsSpawned = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            this.TotalTrainsKilledByPlayer = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            this.HasWarnedForMedSpawns = Buffer.ReadBool( MetaData );
            this.HasWarnedForHighSpawns = Buffer.ReadBool( MetaData );
            this.TotalTrainsKilled = (int)Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.TrainToSpawnTag = Buffer.ReadString_Condensed( MetaData );
            this.TrainSpawnCounterForMarkLevel = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "20 Load From Astro Trains" );
            return 20;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            this.LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );

            if ( MultiTrainSpawnByIntensity.Count == 0 )
                LoadCustomDataIfNeeded();
            PercentTrainMultiSpawn = MultiTrainSpawnByIntensity[Intensity - 1];

            if ( Intensity <= 3 )
            {
                DepotSpawnInterval = DepotSpawnIntervalLow;
                TrainSpawnInterval = TrainSpawnIntervalLow;
            }
            else if ( Intensity <= 6 )
            {
                DepotSpawnInterval = DepotSpawnIntervalMed;
                TrainSpawnInterval = TrainSpawnIntervalMed;
            }
            else
            {
                DepotSpawnInterval = DepotSpawnIntervalHigh;
                TrainSpawnInterval = TrainSpawnIntervalHigh;
            }
        }
        #endregion

        #region GetAstroTrainDepots_Threadsafe
        public void GetAstroTrainDepots_Threadsafe( List<SafeSquadWrapper> ListToFill )
        {
            ListToFill.Clear();
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "AstroTrainDepot" ) )
            {
                ListToFill.Add( entity );
            }
        }
        #endregion

        #region AppendStateForDebugDisplay
        public override void AppendStateForDebugDisplay( ArcenCharacterBufferBase buffer )
        {
            List<SafeSquadWrapper> depots = GameEntity_Squad.GetTemporarySquadList( "AstroTrainsFactionBaseInfo-AppendStateForDebugDisplay-depots", 10f );
            if ( depots == null ) //blocked for teardown/shutdown; bail
                return;
            this.GetAstroTrainDepots_Threadsafe( depots );
            buffer.Add( "Trains Spawned: " ).Add( this.TotalTrainsSpawned ).Add( ", trains killed " ).Add( this.TotalTrainsKilled )
                .Add( ", trains killed by player " ).Add( this.TotalTrainsKilledByPlayer ).Add( "\n" );
            for ( int i = 0; i < depots.Count; i++ )
            {
                GameEntity_Squad depot = depots[i].GetSquad();
                if ( depot == null )
                    continue;
                AstroTrainsPerDepotBaseInfo depotData = depot.GetExternalBaseInfoAs<AstroTrainsPerDepotBaseInfo>();
                int nextTrainTime = depotData.LastTrainSpawnTime + TrainSpawnInterval - World_AIW2.Instance.GameSecond;
                buffer.Add( depot.ToStringWithPlanet() ).Add( " time for next train: " ).Add( nextTrainTime ).Add( "\n" );
            }
            GameEntity_Squad.ReleaseTemporarySquadList( depots );
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            //run the base logic first, just in case
            base.SetStartingFactionRelationships();
            //these are friendly with everyone since only the player should interact with them
            AllegianceHelper.AllyThisFactionToEveryoneButPlayers( this.AttachedFaction );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            //TEACHING_MOMENT: How to handle subdata, including onto squads all from a central Stage2 loop, in a very efficient fashion!
            //This file used to contain a dictionary of guards that went with each train, and it was a tricky mess that had to be serialized.
            //In the old version of the codebase, that wasn't bad code per se, and I'm not sure I could have improved it much.
            //But it was supremely hard to read, and a great example of why we needed a newer version of the codebase.
            //The pattern that is outlined here is something that should be emulated -- for clarity and performance reasons -- in any other
            //factions that have similar per-unit lists of data.

            this.TrainDepots.ClearConstructionListForStartingConstruction();
            this.TrainsEnRoute.ClearConstructionListForStartingConstruction();
            this.PlanetsWithDepots.ClearConstructionListForStartingConstruction();
            this.Stations.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "AstroTrainDepot" ) )
            {
                this.PlanetsWithDepots.AddToConstructionListIfNotAlreadyIn( entity.Planet );
                this.TrainDepots.AddToConstructionList( entity );
                // if created after the start of this planning cycle, skip
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "AstroTrain" ) )
            {
                this.TrainsEnRoute.AddToConstructionList( entity );
            }

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "AstroTrainStation" ) )
            {
                this.Stations.AddToConstructionList( entity );
                // if created after the start of this planning cycle, skip
            }

            //We have found our basic entities, so now we need to flip them to the display lists
            this.TrainDepots.SwitchConstructionToDisplay();
            this.TrainsEnRoute.SwitchConstructionToDisplay();
            this.PlanetsWithDepots.SwitchConstructionToDisplay();
            this.Stations.SwitchConstructionToDisplay();

            //Now that we have a reference to all active trains, we can also flip for their sub-lists that are double-buffered
            foreach ( GameEntity_Squad train in this.TrainsEnRoute.DisplaySquads() )
            {
                AstroTrainsPerTrainBaseInfo trainInfo = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                trainInfo.DeployedGuardsOfThisTrain.ClearConstructionListForStartingConstruction();
            }

            this.AllTrainGuards.ClearConstructionListForStartingConstruction();

            //We MUST flip the above before we do the below.  This is a 1, 2, 3 sequence.
            //First find the basic stuff, including trains.  Then flip the lists on the trains.
            //Then fill the lists on the trains.  And then...
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                if ( !entity.TypeData.IsMobile )
                    continue;
                if ( entity.TypeData.GetHasTag( "AstroTrain" ) )
                    continue;

                AstroTrainsPerTrainGuardUnitBaseInfo guardData = entity.TryGetExternalBaseInfoAs<AstroTrainsPerTrainGuardUnitBaseInfo>();
                if ( guardData == null )
                {
                    //Whatever this unit is, it's not a guard.  I prefer NOT to assume that "if it's not a train and is mobile, it must be a guard"
                    //That would be a trap, waiting to bite some other programmer who adds something new to this faction that is mobile but not a guard or train.
                    //So instead of calling GetExternalBaseInfoAs(), we call TryGetExternalBaseInfoAs(), and just gently ignore anything that gives us a false.
                    //Why make things less flexible if we can keep it more flexible?  I always assume someone else MIGHT want to add to code in the future.
                    continue;
                }
                //This variable is wholly unneeded, but makes the code below read more clearly
                //Since the code costs so little CPU, that seems like a good trade to me.
                //Being able to read code as if it was comments, even without comments, is a very good thing.
                //(That's a skill of the programmer doing the coding as much as it is the programmer doing the reading.)
                //Just how advanced do you want the reader to have to be?  I'd prefer it be novice-accessible.
                GameEntity_Squad guard = entity;
                this.AllTrainGuards.AddToConstructionList( guard );

                //So, we are a guard.  Of what train?
                //it is possible the train died, in which case GetSquad() would give us null, and that's fine.
                GameEntity_Squad train = guardData.TrainIGuard.GetSquad();
                if ( train != null ) 
                {
                    //assuming that the train lives, here we are.  Let's register ourself as a guard on the train,
                    //in the individual sense and as part of an aggregate.
                    AstroTrainsPerTrainBaseInfo trainInfo = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                    trainInfo.DeployedGuardsOfThisTrain.AddToConstructionList( guard );
                }
            }

            //...finally we do our last calculations for the trains themselves, and their finishing  flip (this would  be item 4 in the sequence)
            foreach ( GameEntity_Squad train in this.TrainsEnRoute.DisplaySquads() )
            {
                AstroTrainsPerTrainBaseInfo trainInfo = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                trainInfo.DeployedGuardsOfThisTrain.SwitchConstructionToDisplay();
                //We can do this now, because we finished calculating the above, including these last flips right above
                train.AdditionalStrengthFromFactions = calculateTrainGuardStrength( train );
            }

            this.AllTrainGuards.SwitchConstructionToDisplay();

            if ( this.TotalTrainsKilled >= TrainsKilledForMedSpawn && !this.HasWarnedForMedSpawns )
            {
                this.TrainToSpawnTag = "AstroTrainMed";
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "AstroTrainsTierTwo", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                this.TrainSpawnCounterForMarkLevel = 0;
                this.HasWarnedForMedSpawns = true;
            }
            if ( this.TotalTrainsKilledByPlayer >= TrainsKilledForHighSpawn && !this.HasWarnedForHighSpawns )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "AstroTrainsTierThree", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                this.TrainToSpawnTag = "AstroTrainHigh";
                this.TrainSpawnCounterForMarkLevel = 0;
                this.HasWarnedForHighSpawns = true;
            }
            if ( this.ResetAllegiances )
            {
                this.ResetAllegiances = false;
                this.SetStartingFactionRelationships();
            }
            if ( this.AttachedFaction.HasBeenSeenByPlayer )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "AstroTrainsFirstDetected", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }
        #endregion

        #region calculateTrainGuardStrength
        public int calculateTrainGuardStrength( GameEntity_Squad train )
        {
            if ( train == null )
                return 0;
            AstroTrainsPerTrainBaseInfo data = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
            int strengthInside = 0;
            List<SafeSquadWrapper> guards = data.DeployedGuardsOfThisTrain.GetDisplayList();
            foreach ( SafeSquadWrapper wrap in guards )
            {
                GameEntity_Squad guard = wrap.GetSquad();
                if ( guard == null )
                    continue;
                strengthInside += guard.GetStrengthOfSelfAndContents();
            }
            return strengthInside;
        }
        #endregion

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            AttritionLostGuards( Context );
            AllegianceHelper.AllyThisFactionToAI( AttachedFaction );
        }

        #region AttritionLostGuards
        private void AttritionLostGuards( ArcenSimContextAnyStatus Context )
        {
            FInt deadTrainAttritionPercent = FInt.FromParts( 2, 000 );
            FInt otherPlanetTainAttritionPercent = FInt.FromParts( 2, 000 );
            List<SafeSquadWrapper> allTrainGuards = this.AllTrainGuards.GetDisplayList();
            foreach ( SafeSquadWrapper wrap in allTrainGuards )
            {
                GameEntity_Squad guard = wrap.GetSquad();
                if ( guard == null )
                    continue;
                AstroTrainsPerTrainGuardUnitBaseInfo guardData = guard.GetExternalBaseInfoAs<AstroTrainsPerTrainGuardUnitBaseInfo>();

                //bool trainDead = false;
                FInt attritionPercent = FInt.FromParts( 0, 500 );
                GameEntity_Squad train = guardData.TrainIGuard.GetSquad();
                if ( train == null || train.TypeData == null || train.Planet == null ||
                     train.PlanetFaction.Faction != AttachedFaction )
                {
                    attritionPercent = FInt.FromParts( 3, 000 ); //if the train is dead, attrition quickly
                    train = null;
                }
                //if the train is dead or not on the same planet as the guard, then we attrition
                if ( train == null || train.Planet != guard.Planet )
                {
                    //if the train is dead or we aren't on a planet with our train, we attrition.
                    int damageToTake = (int)((attritionPercent / 100) * (guard.GetMaxHullPoints()));
                    guard.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
                }
            }
        }
        #endregion
    }
}
