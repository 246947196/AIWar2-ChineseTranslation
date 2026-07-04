using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class RenegadeSpireFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        // Fireteams for combat ships
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "RenegadeSpireFactionBaseInfo-Teams" );

        // Relic / defiler spawn timers
        public int LastRelicSpawnAttemptSecond = -1;
        public int TimeForNextDefilerSpawn = -1;
        public bool MinorFactionAllied = false;
        public int TimeForNextRenegadeInvasionCheck = -1;
        public int RenegadeInvasionBudget = 0;
        public readonly Dictionary<GameEntityTypeData, int> RenegadeInvasionForce = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 500, "RenegadeSpireFactionBaseInfo-RenegadeInvasionForce" );
        public int RenegadeInvasionForceStrength = 0;

        // Fractures in spawn order 鈥?oldest first (front = chief fracture candidate)
        public readonly List<int> FractureSpawnOrder = List<int>.Create_WillNeverBeGCed( 30, "RenegadeSpireFactionBaseInfo-FractureSpawnOrder" );

        // Populated each Stage2 tick
        public readonly DoubleBufferedList<SafeSquadWrapper> Fractures = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "RenegadeSpireFactionBaseInfo-Fractures" );
        public readonly DoubleBufferedList<SafeSquadWrapper> CombatShips = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 100, "RenegadeSpireFactionBaseInfo-CombatShips" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Defilers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "RenegadeSpireFactionBaseInfo-Defilers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Relics = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "RenegadeSpireFactionBaseInfo-Relics" );

        // Settings
        public int Intensity;

        // Difficulty row 鈥?loaded from table by Intensity in DoRefreshFromFactionSettings
        public RenegadeSpireDifficulty Difficulty;

        
        public RenegadeSpireFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Teams.Clear();
            LastRelicSpawnAttemptSecond = -1;
            TimeForNextDefilerSpawn = -1;
            MinorFactionAllied = false;
            TimeForNextRenegadeInvasionCheck = -1;
            RenegadeInvasionBudget = 0;
            RenegadeInvasionForce.Clear();
            RenegadeInvasionForceStrength = 0;
            FractureSpawnOrder.Clear();
            Fractures.Clear();
            CombatShips.Clear();
            Defilers.Clear();
            Relics.Clear();
            Intensity = 5;
            Difficulty = null;
        }

        #region DoRefreshFromFactionSettings
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            if ( Intensity == -1 )
                DoRefreshFromFactionSettings();
            return Intensity;
        }

        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            string allegiance = this.Allegiance;
            MinorFactionAllied = ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) ||
                                 ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) ||
                                 ArcenStrings.Equals( allegiance, "Minor Faction Team Green" );
            if ( AttachedFaction.MinFireteamStrength == -1 )
                AttachedFaction.MinFireteamStrength = 2000;
            if ( AttachedFaction.MaxFireteamStrength == -1 )
                AttachedFaction.MaxFireteamStrength = 5000;

            if ( RenegadeSpireDifficultyTable.Instance != null )
            {
                Difficulty = RenegadeSpireDifficultyTable.Instance.GetRowByIntensity( Intensity, AttachedFaction );
            }
            else
                throw new Exception( "No RenegadeSpireDifficultyTable found" );

            if ( Difficulty == null )
                throw new Exception( " Unable to load Renegade Spire Difficulty" );
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            string allegiance = this.Allegiance;
            Faction faction = this.AttachedFaction;
            if ( ArcenStrings.Equals( allegiance, "AI Allied" ) || string.IsNullOrEmpty( allegiance ) )
                AllegianceHelper.AllyThisFactionToAI( faction );
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Red" );
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Blue" );
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Green" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Green" );
            else
                AllegianceHelper.AllyThisFactionToAI( faction );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            Fractures.ClearConstructionListForStartingConstruction();
            CombatShips.ClearConstructionListForStartingConstruction();
            Defilers.ClearConstructionListForStartingConstruction();
            Relics.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity.TypeData.GetHasTag( "SpireFracture" ) )
                {
                    Fractures.AddToConstructionList( entity );
                    entity.CreateExternalBaseInfo<RenegadeSpirePerUnitBaseInfo>( "RenegadeSpirePerUnitBaseInfo" );
                }
                else if ( entity.TypeData.GetHasTag( "RenegadeSpireDefiler" ) )
                {
                    Defilers.AddToConstructionList( entity );
                    entity.CreateExternalBaseInfo<RenegadeSpirePerUnitBaseInfo>( "RenegadeSpirePerUnitBaseInfo" );
                }
                else if ( entity.TypeData.GetHasTag( "RenegadeRelic" ) )
                    Relics.AddToConstructionList( entity );
                else if ( entity.TypeData.GetHasTag( "FireteamEligible" ) )
                    CombatShips.AddToConstructionList( entity );
            }

            // Prune dead entries from FractureSpawnOrder
            for ( int i = FractureSpawnOrder.Count - 1; i >= 0; i-- )
            {
                int id = FractureSpawnOrder[i];
                bool found = false;
                List<SafeSquadWrapper> constructionList = Fractures.GetConstructionList();
                for ( int j = 0; j < constructionList.Count; j++ )
                {
                    GameEntity_Squad fracture = constructionList[j].GetSquad();
                    if ( fracture != null && fracture.PrimaryKeyID == id )
                    {
                        found = true;
                        break;
                    }
                }
                if ( !found )
                    FractureSpawnOrder.RemoveAt( i );
            }

            Fractures.SwitchConstructionToDisplay();
            CombatShips.SwitchConstructionToDisplay();
            Defilers.SwitchConstructionToDisplay();
            Relics.SwitchConstructionToDisplay();
        }
        #endregion

        #region GetChiefFracture
        public GameEntity_Squad GetChiefFracture()
        {
            List<SafeSquadWrapper> display = Fractures.GetDisplayList();
            if ( display.Count == 0 )
                return null;

            // Chief = oldest fracture, tracked via FractureSpawnOrder (front = first spawned)
            for ( int i = 0; i < FractureSpawnOrder.Count; i++ )
            {
                int id = FractureSpawnOrder[i];
                for ( int j = 0; j < display.Count; j++ )
                {
                    GameEntity_Squad fracture = display[j].GetSquad();
                    if ( fracture != null && fracture.PrimaryKeyID == id )
                        return fracture;
                }
            }
            return display[0].GetSquad();
        }
        #endregion

        #region Serialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "RenegadeSpireFactionBaseInfo" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, LastRelicSpawnAttemptSecond );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextDefilerSpawn );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextRenegadeInvasionCheck );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, RenegadeInvasionBudget );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, RenegadeInvasionForceStrength );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)RenegadeInvasionForce.Count );
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in RenegadeInvasionForce )
            {
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, pair.Key, Buffer, "RenegadeInvasionForce" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, pair.Value );
            }

            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, FractureSpawnOrder.Count );
            for ( int i = 0; i < FractureSpawnOrder.Count; i++ )
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, FractureSpawnOrder[i] );

            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, Teams );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "RenegadeSpireFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "RenegadeSpireFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
            LastRelicSpawnAttemptSecond = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            TimeForNextDefilerSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            TimeForNextRenegadeInvasionCheck = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            RenegadeInvasionBudget = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            RenegadeInvasionForceStrength = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            RenegadeInvasionForce.Clear();
            int invasionForceCount = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            for ( int i = 0; i < invasionForceCount; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "RenegadeInvasionForce" );
                RenegadeInvasionForce[data] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            }

            FractureSpawnOrder.Clear();
            int fractureCount = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
            for ( int i = 0; i < fractureCount; i++ )
                FractureSpawnOrder.Add( Buffer.ReadInt32( MetaData, ReadStyle.NonNeg ) );

            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, Teams, "RenegadeSpire" );
            Buffer.StopTrackerByName( "RenegadeSpireFactionBaseInfo Ext" );
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( Teams, id );
        }
        #endregion
    }
}
