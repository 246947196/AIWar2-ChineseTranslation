using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


using System.Text;

namespace Arcen.AIW2.External
{
    public class ElderlingsFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized
        public int TimeLastHadElderlings;
        public int TimeLastSpawnedMaddenedEgg;

        //not serialized
        public int Intensity = 0;
        public ElderlingsDifficulty Difficulty = null;
        public readonly DoubleBufferedList<SafeSquadWrapper> Elderlings = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "Elderlings-Elderlings" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Eggs = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "Elderlings-Eggs" );

        public Dictionary<Planet, int> ElderlingsPerPlanet = Dictionary<Planet, int>.Create_WillNeverBeGCed( 50, "Elderlings-ElderlingsPerPlanet"); //tracks elderlings with this planet in Territory

        //Set immediately before WorkingPlanets.Sort(...) so the sort comparison can be a non-capturing
        //static delegate.  [ThreadStatic] for safety since faction logic can run on worker threads.
        [ThreadStatic]
        private static Dictionary<Planet, int> cb_elderlingsPerPlanet;
        public Dictionary<Planet, int> EggsPerPlanet = Dictionary<Planet, int>.Create_WillNeverBeGCed( 50, "Elderlings-EggsPerPlanet");
        public DictionaryOfLists<Planet, SafeSquadWrapper> ElderlingsOnPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "TemplarFactionDeepInfo-UnassignedShipsByPlanetLRP" );
        public bool MinorFactionAllied = false;
        public bool PlayerAllied = false;
        public bool AIAllied = false;
        public bool ExtraStrongMode = false;

        public readonly List<Planet> WorkingPlanets = List<Planet>.Create_WillNeverBeGCed( 200, "Elderlings-WorkingPlanets" );

        public ElderlingsFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            TimeLastHadElderlings = -1;
            TimeLastSpawnedMaddenedEgg = -1;

            Intensity = 0;
            Difficulty = null;
            Elderlings.Clear();
            Eggs.Clear();
            ElderlingsPerPlanet.Clear();
            EggsPerPlanet.Clear();
            ElderlingsOnPlanet.Clear();

            MinorFactionAllied = false;
            PlayerAllied = false;
            AIAllied = false;
            ExtraStrongMode = false;

            WorkingPlanets.Clear();
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "30 Load From Neinzul Elderlings" );
            return 30;
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeLastHadElderlings );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeLastSpawnedMaddenedEgg );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            TimeLastHadElderlings = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 4, 010 ) )
                TimeLastSpawnedMaddenedEgg = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            if ( Intensity == -1 )
                DoRefreshFromFactionSettings();
            return Intensity;
        }

        #region Xml Data
        public bool HaveLoadedData = false;
        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;

        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            this.LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            int debugCode = 0;
            try{
                ConfigurationForFaction cfg = this.AttachedFaction.Config;
                debugCode = 100;
                Intensity = -1;
                if ( this.AttachedFaction.SpecialFactionData.TakesDifficultyFromNecromancer ) 
                {
                    Intensity = FactionUtilityMethods.Instance.GetDifficultyFromNecromancerSettings( this.AttachedFaction );
                    cfg.SetCustomFieldValue( "Intensity", Intensity.ToString() );
                }
                if ( Intensity == -1 )
                    Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
                debugCode = 200;
                ExtraStrongMode = cfg.GetBoolValueForCustomFieldOrDefaultValue( "ExtraStrongMode", true );
                debugCode = 300;
                Difficulty = ElderlingsDifficultyTable.Instance.GetRowByIntensity( this.Intensity, this.AttachedFaction );
                debugCode = 400;
            } catch( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("hit exception in Elderlings DoRefreshFromFactionSettings debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                debugCode = 200;
                debugCode = 300;
                debugCode = 400;
                this.UpdateAllegiance();
                debugCode = 500;
                Elderlings.ClearConstructionListForStartingConstruction();
                debugCode = 510;
                Eggs.ClearConstructionListForStartingConstruction();
                debugCode = 520;
                ElderlingsPerPlanet.Clear();
                debugCode = 530;
                ElderlingsOnPlanet.Clear();
                debugCode = 540;
                EggsPerPlanet.Clear();
                debugCode = 550;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "Elderling" ) )
                {
                    debugCode = 600;
                    Elderlings.AddToConstructionList( entity );
                    ElderlingsOnPlanet[entity.Planet].Add( entity );
                    ElderlingsPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                    if ( data != null && data.Territory != null && data.Territory.Count > 0 )
                    {
                        for ( int i = 0; i < data.Territory.Count; i++ )
                        {
                            ElderlingsPerPlanet[data.Territory[i]]++;
                        }
                    }
                    debugCode = 610;
                }
                debugCode = 700;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ElderlingEgg" ) )
                {
                    debugCode = 800;
                    Eggs.AddToConstructionList( entity );
                    debugCode = 805;
                    EggsPerPlanet[entity.Planet]++;
                    debugCode = 810;
                    ElderlingsPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                    if ( data == null )
                        throw new Exception("Could not create ElderlingsPerUnitBaseInfo for " + entity.ToStringWithPlanetAndOwner() );
                    debugCode = 820;
                    if ( data.HatchTime <= 0 )
                    {
                        debugCode = 830;
                        //this value has not been initialized (it was seeded by mapgen)
                        data.HatchTime = World_AIW2.Instance.GameSecond + this.Difficulty.BaseEggHatchingInterval / 2;
                    }
                }
                debugCode = 900;
                Elderlings.SwitchConstructionToDisplay();
                Eggs.SwitchConstructionToDisplay();
                debugCode = 1000;
                if ( Elderlings.Count > 0 || Eggs.Count > 0 )
                    this.TimeLastHadElderlings = World_AIW2.Instance.GameSecond;

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception debugCode " + debugCode + " in elderlings stage 2 " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion end DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost

        #region UpdateAllegiance
        private bool hasUpdatedAllegiance = false;
        private void UpdateAllegiance()
        {
            bool localDebug = false;
            Faction faction = this.AttachedFaction;
            string allegiance = this.Allegiance;
            if( hasUpdatedAllegiance && World_AIW2.Instance.GameSecond % 10 != 0  )
                return;
            if ( string.IsNullOrEmpty( allegiance ) )
            {
                this.SetNewAllegianceIntoCoreSettings( "对玩家友好" );
            }
            if ( ArcenStrings.Equals( allegiance, "对AI友好" ) )
            {
                AllegianceHelper.AllyThisFactionToAI( faction );
                AIAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "对玩家友好" ) )
            {
                AllegianceHelper.AllyThisFactionToHumans( faction );
                PlayerAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "小派系小队红" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( faction.ToString() + " is on team red", Verbosity.DoNotShow );
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "小派系小队红" );
            }
            else if ( ArcenStrings.Equals( allegiance, "小派系小队蓝" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( faction.ToString() + " is on team blue", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "小派系小队蓝" );
            }
            else if ( ArcenStrings.Equals( allegiance, "小派系小队绿" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( faction.ToString() + " is on team green", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "小派系小队绿" );
            }
            hasUpdatedAllegiance = true;
        }
        #endregion

        // #region GetSecondsTillMarkup
        // public int GetSecondsTillMarkup( GameEntity_Squad entity )
        // {
        //     Faction faction = entity.PlanetFaction.Faction;
        //     int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
        //     ElderlingsDifficulty diff = ElderlingsDifficultyTable.Instance.GetRowByIntensity( intensity, faction );
        //     if ( entity.CurrentMarkLevel >= 7 )
        //         return -1;

        //     if ( !entity.TypeData.GetHasTag( "Elderling" ) )
        //         return -1;
        //     int markInterval = diff.MarkLevelIncreaseInterval;
        //     if ( markInterval == 0 )
        //         throw new Exception( "Unable to figure out how to mark up a " + entity.ToStringWithPlanet() );
        //     int seconds = (entity.CurrentMarkLevel) * markInterval;
        //     return (seconds - entity.GetSecondsSinceCreation());
        // }
        // #endregion


        #region DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;
            int debugCode = 0;
            try
            {
                debugCode = 100;

                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    debugCode = 260010;
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "Elderlings" )
                        continue;

                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Elderlings notification code, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
        public Planet GetTranscendantPlanet_HostOnly(ArcenHostOnlySimContext Context)
        {
            WorkingPlanets.Clear();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int myAndAlliedStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength +
                    pFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                //Only consider spawning a transcendant overlord on friendly planet without foes and with significant strength
                if ( !planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                    continue;
                if ( enemyStrength > 0 ||//no enemies
                     myAndAlliedStrength < 40 * 1000 ) //and a lot of allies
                    continue;
                WorkingPlanets.Add( planet );
            }
            //if no good planets, return 0
            if ( WorkingPlanets.Count == 0 )
                return World_AIW2.Instance.CurrentGalaxy.GetRandomPlanet(false, Context);
            //try to find a planet with few other elderlings
            cb_elderlingsPerPlanet = ElderlingsPerPlanet;
            WorkingPlanets.Sort( static delegate ( Planet L, Planet R )
            {
                if ( cb_elderlingsPerPlanet[L] == cb_elderlingsPerPlanet[R] )
                    return L.Name.CompareTo(R.Name);
                return cb_elderlingsPerPlanet[L].CompareTo( cb_elderlingsPerPlanet[R] );
            } );
            return WorkingPlanets[0];

        }
    }
}
