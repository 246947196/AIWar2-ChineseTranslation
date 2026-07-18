using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

//Reapers are anti-Dyson Sidekick forces
//they only get troops when the Dyson's code sends them
//They get troops directly from ravaged planets, and also sometimes spawn
//Ravagers. These Ravagers will ravage planets (and as they ravage, they produce ships)
namespace Arcen.AIW2.External
{
    public class ReapersFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //Serialized
        public int TimeForNextChrysalis;
        public int TimeForNextPlanetoid;
        public int TimeForNextAIPlanetoidDrill;
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "ReaperFactionBaseInfo-Teams" );
        public int TimeForNextRavagerAttack;
        public int StrengthForNextRavagerAttack;
        public int LastTimeHadGateway;
        public int LastTimeSpawnedLarva;
        public int TimeForNextLunarInvasion;
        //Not Serialized
        int Intensity = -1;
        public DysonSidekickDifficulty Difficulty;
        public static ReapersFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public readonly DoubleBufferedList<SafeSquadWrapper> MobileRavagers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Reapers-MobileRavagers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> ImmobileRavagers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Reapers-ImmobileRavagers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Chrysalises = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Reapers-Chrysalises" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Moons = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Reapers-Moons" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Gateways = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Reapers-Gateways" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Larvae = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Reapers-Larvae" );

        public readonly DoubleBufferedList<Planet> RavagedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 20, "Reapers-RavagedPlanets" );

        public ReapersFactionBaseInfo()
        {
            Cleanup();
        }
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            if ( Intensity == -1 )
                DoRefreshFromFactionSettings();

            return Intensity;
        }
        protected override void Cleanup()
        {
            Intensity = -1;
            MobileRavagers.Clear();
            ImmobileRavagers.Clear();
            RavagedPlanets.Clear();
            Chrysalises.Clear();
            Gateways.Clear();
            Moons.Clear();
            Larvae.Clear();
            Instance = null;
            Teams.Clear();
            TimeForNextChrysalis = -1;
            Difficulty = null;
            TimeForNextRavagerAttack = -1;
            StrengthForNextRavagerAttack = -1;
            LastTimeHadGateway = -1;
            LastTimeSpawnedLarva = -1;
            TimeForNextLunarInvasion = -1;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 40 + (Intensity * 5);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " 来自收割者的负载" );
            return load;
        }

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ReapersFactionBaseInfo" );
            // Buffer.AddBool( MetaData, HasGeneratedCastles );
             Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextChrysalis, "TimeForNextChrysalis" );
             Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextPlanetoid, "TimeForNextPlanetoid" );
             Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextAIPlanetoidDrill, "TimeForNextAIPlanetoidDrill" );
             FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
             Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextRavagerAttack, "TimeForNextRavagerAttack");
             Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.StrengthForNextRavagerAttack, "StrengthForNextRavagerAttack");
             Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.LastTimeHadGateway, "LastTimeHadGateway");
            Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.LastTimeSpawnedLarva, "LastTimeSpawnedLarva");
            Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextLunarInvasion, "TimeForNextLunarInvasion");

            // Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, WaveLeadersToSpawn, "WaveLeadersToSpawn" );
            // Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, RiftsPopulated, "RiftsPopulated" );
            // Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)LastTimePlanetWasOwned.Count, "LastTimePlanetWasOwned_Count" );
            // foreach ( KeyValuePair<Planet, int> pair in LastTimePlanetWasOwned )
            // {
            //     Buffer.AddPlanetIndex_Neg1ToPos( MetaData, pair.Key.Index, "planetIdx" );
            //     Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, (Int32)pair.Value, "planetLastTime" );
            // }
            // Buffer.AddBool( MetaData, SpawnReapersWaveNextSecond );
            // Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)InitialEncampmentsSpawned, "InitialEncampmentsSpawned" );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ReapersFactionBaseInfo" );
            // Buffer.ActivateOrAddTrackerByNameIfTracking( "ReapersFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
            // HasGeneratedCastles = Buffer.ReadBool( MetaData );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 520 ) )
                TimeForNextChrysalis = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextChrysalis" );
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 524))
            {
                TimeForNextPlanetoid = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "TimeForNextPlanetoid");
                TimeForNextAIPlanetoidDrill = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "TimeForNextAIPlanetoidDrill");
            }
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 546))
            {
                FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers(MetaData, Buffer, SerializationCmdType, this.Teams, "Reapers");
            }
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 548))
            {
                TimeForNextRavagerAttack = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "TimeForNextRavagerAttack");
                StrengthForNextRavagerAttack = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "StrengthForNextRavagerAttack");
            }
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 550))
                LastTimeHadGateway = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "LastTimeHadGateway");
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 567))
                LastTimeSpawnedLarva = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "LastTimeSpawnedLarva");
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 604))
                TimeForNextLunarInvasion = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "TimeForNextLunarInvasion");

            // WaveLeadersToSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "WaveLeadersToSpawn" );
            // RiftsPopulated = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RiftsPopulated" );
            // LastTimePlanetWasOwned.Clear();
            // int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "LastTimePlanetWasOwned_Count" );
            // for ( int i = 0; i < count; i++ )
            // {
            //     Int16 planetIdx = Buffer.ReadPlanetIndex_Neg1ToPos( MetaData, "planetIdx" );
            //     Planet planet = World_AIW2.Instance.GetPlanetByIndex( planetIdx );
            //     LastTimePlanetWasOwned[planet] = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "planetLastTime" );
            // }
            // if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 4, 004 ) )
            //     SpawnReapersWaveNextSecond = Buffer.ReadBool( MetaData );
            // if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 4, 017 ) )
            //     this.InitialEncampmentsSpawned = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "InitialEncampmentsSpawned" );
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ((Target.TypeData.GetHasTag("WarpGate") || Target.TypeData.IsCommandStation))
            {
                return false;
            }
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) || Target.TypeData.GetHasTag( "DSAA" ) )
                return true;
            return false;
        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }
        #endregion

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            if ( AttachedFaction.MinFireteamStrength == -1 )
                AttachedFaction.MinFireteamStrength = 2000;
            if ( AttachedFaction.MaxFireteamStrength == -1 )
                AttachedFaction.MaxFireteamStrength = 4000;
            if ( this.AttachedFaction.SpecialFactionData.TakesDifficultyFromDysonSidekick ) 
                Intensity = FactionUtilityMethods.Instance.GetDifficultyFromDysonSidekickSettings( this.AttachedFaction );
            if ( Intensity == -1 )
                Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );

            Difficulty = DysonSidekickDifficultyTable.Instance.GetRowByIntensity(this.Intensity, this.AttachedFaction);

        }
        #endregion
        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                MobileRavagers.ClearConstructionListForStartingConstruction();
                ImmobileRavagers.ClearConstructionListForStartingConstruction();
                Chrysalises.ClearConstructionListForStartingConstruction();
                Gateways.ClearConstructionListForStartingConstruction();
                Moons.ClearConstructionListForStartingConstruction();
                Larvae.ClearConstructionListForStartingConstruction();
                RavagedPlanets.ClearConstructionListForStartingConstruction();

                debugCode = 400;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                   if (entity.TypeData.GetHasTag("MobileRavager"))
                   {
                       ReapersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );
                       MobileRavagers.AddToConstructionList(entity);
                       continue;
                   }
                   if (entity.TypeData.GetHasTag("ImmobileRavager"))
                   {
                       ReapersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );
                       ImmobileRavagers.AddToConstructionList(entity);
                       continue;
                   }
                   if (entity.TypeData.GetHasTag("ReaperChrysalis"))
                   {
                       ReapersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );
                       Chrysalises.AddToConstructionList(entity);
                       continue;
                   }
                   if (entity.TypeData.GetHasTag("ReaperGateway"))
                   {
                       ReapersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );
                       Gateways.AddToConstructionList(entity);
                       continue;
                   }
                    if (entity.TypeData.GetHasTag("ReaperMoon"))
                   {
                       ReapersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );
                       Moons.AddToConstructionList(entity);
                       continue;
                   }

                   if (entity.TypeData.GetHasTag("ReaperLarva"))
                   {
                       ReapersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );
                       Larvae.AddToConstructionList(entity);
                       continue;
                   }

                }

                foreach ( Planet plan in World_AIW2.Instance.Planets( false ) )
                {
                    if (plan.IsRavaged)
                        RavagedPlanets.AddToConstructionList(plan);
                }
                MobileRavagers.SwitchConstructionToDisplay();
                ImmobileRavagers.SwitchConstructionToDisplay();
                Chrysalises.SwitchConstructionToDisplay();
                Gateways.SwitchConstructionToDisplay();
                Moons.SwitchConstructionToDisplay();
                Larvae.SwitchConstructionToDisplay();
                RavagedPlanets.SwitchConstructionToDisplay(); 
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Reapers DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
