using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SappersFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized
        public int TimeLastHadSappers;

        //not serialized
        public int Intensity = 0;
        public SappersDifficulty Difficulty = null;
        public readonly DoubleBufferedList<SafeSquadWrapper> SapperHabitats = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-Habitats" );
        public readonly DoubleBufferedList<SafeSquadWrapper> BaseCrystals = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-BaseCrystals" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Watchtowers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-Watchtowers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Beachheaders = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-Beachheaders" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Constructors = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-Constructors" );
        public readonly DoubleBufferedList<SafeSquadWrapper> CombatShips = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-CombatShips" );
        public readonly DoubleBufferedList<SafeSquadWrapper> BeachheadTurrets = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-BeachheadTurrets" );
        public readonly DoubleBufferedList<SafeSquadWrapper> BasicStructures = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-BasicStructures" );
        public readonly DoubleBufferedDictionary<Planet, int> BasicStructuresPerPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 10, "Sappers-BasicStructuresPerPlanet" );
        public readonly DoubleBufferedDictionary<Planet, int> AdvancedStructuresPerPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 10, "Sappers-AdvancedStructuresPerPlanet" );
        public readonly DoubleBufferedDictionary<Planet, int> BeachheaderStructuresPerPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 10, "Sappers-BeachheaderStructuresPerPlanet" );

        //These ones have items added to them during the sim execution, so they need to be the slower-but-more-robust DoubleBufferedConcurrentList type
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Sappers = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-Sappers" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> FloweredCrystals = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Sappers-FloweredCrystals" );

        public int MaxBasicStructuresPerPlanet = 0;
        public int MaxAdvancedStructuresPerPlanet = 0;
        public int MaxBeachheaderStructuresPerPlanet = 0;
        public int MaxWatchtowerStrength = 0;
        public bool AnyWatchtowersActive;

        public bool MinorFactionAllied = false;
        public bool PlayerAllied = false;
        public bool AIAllied = false;
        public bool ExtraStrongMode = false;

        public SappersFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            TimeLastHadSappers = -1;

            Intensity = 0;
            Difficulty = null;
            Sappers.Clear();
            SapperHabitats.Clear();
            BaseCrystals.Clear();
            FloweredCrystals.Clear();
            Watchtowers.Clear();
            Beachheaders.Clear();
            Constructors.Clear();
            CombatShips.Clear();
            BeachheadTurrets.Clear();
            BasicStructures.Clear();
            BasicStructuresPerPlanet.Clear();
            AdvancedStructuresPerPlanet.Clear();
            BeachheaderStructuresPerPlanet.Clear();

            MaxBasicStructuresPerPlanet = 0;
            MaxAdvancedStructuresPerPlanet = 0;
            MaxBeachheaderStructuresPerPlanet = 0;
            MaxWatchtowerStrength = 0;
            AnyWatchtowersActive = false;

            MinorFactionAllied = false;
            PlayerAllied = false;
            AIAllied = false;
            ExtraStrongMode = false;
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeLastHadSappers );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            TimeLastHadSappers = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );

            int load = 50 + (Intensity * 8);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Neinzul Sappers" );
            return load;
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
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            Difficulty = SappersDifficultyTable.Instance.GetRowByIntensity( this.Intensity, this.AttachedFaction );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                this.UpdateAllegiance();
                debugCode = 200;

                BasicStructuresPerPlanet.ClearConstructionDictForStartingConstruction();
                AdvancedStructuresPerPlanet.ClearConstructionDictForStartingConstruction();
                BeachheaderStructuresPerPlanet.ClearConstructionDictForStartingConstruction();

                debugCode = 300;

                Sappers.ClearConstructionListForStartingConstruction();
                SapperHabitats.ClearConstructionListForStartingConstruction();
                BaseCrystals.ClearConstructionListForStartingConstruction();
                FloweredCrystals.ClearConstructionListForStartingConstruction();
                Watchtowers.ClearConstructionListForStartingConstruction();
                CombatShips.ClearConstructionListForStartingConstruction();
                Beachheaders.ClearConstructionListForStartingConstruction();
                Constructors.ClearConstructionListForStartingConstruction();
                BeachheadTurrets.ClearConstructionListForStartingConstruction();
                BasicStructures.ClearConstructionListForStartingConstruction();

                debugCode = 500;

                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "Sapper" ) )
                {
                    Sappers.AddToConstructionList( entity );
                }
                debugCode = 600;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperHabitat" ) )
                {
                    SapperHabitats.AddToConstructionList( entity );
                }
                debugCode = 700;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperBaseCrystal" ) )
                {
                    BaseCrystals.AddToConstructionList( entity );
                }
                debugCode = 800;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "BeachheadTurret" ) )
                {
                    SappersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    if ( !data.IsBeachheadTurret )
                        continue;
                    BeachheadTurrets.AddToConstructionList( entity );
                }
                debugCode = 900;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperCombatShip" ) )
                {
                    debugCode = 910;
                    CombatShips.AddToConstructionList( entity );
                    debugCode = 920;
                    //this just gets it if it already exists, that's okay
                    SappersPerUnitBaseInfo combatData = entity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    //sapper combat ships have a "home" watchtower they will try to go back to, and only fight enemies near
                    //this isn't a strict rule
                    if ( combatData == null )
                        throw new Exception("Could not create SappersPerUnitBaseInfo for " + entity.ToStringWithPlanetAndOwner());
                    debugCode = 930;
                    if ( combatData.HomeWatchtowerId > 0 && combatData.HomeWatchtower == null )
                    {
                        debugCode = 940;
                        combatData.HomeWatchtower = World_AIW2.Instance.GetEntityByID_Squad( combatData.HomeWatchtowerId );
                        if ( combatData.HomeWatchtower == null || combatData.HomeWatchtower.TypeData == null || combatData.HomeWatchtower.Planet == null )
                        {
                            combatData.HomeWatchtower = null;
                            combatData.HomeWatchtowerId = -1;
                        }
                        debugCode = 950;
                        if ( combatData.HomeWatchtower != null &&
                             entity.PlanetFaction.Faction != combatData.HomeWatchtower.PlanetFaction.Faction )
                        {
                            combatData.HomeWatchtower = null;
                            combatData.HomeWatchtowerId = -1;
                        }
                        if ( combatData.HomeWatchtower == null )
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut ); //if we've lost our watchtower, we die
                    }
                }
                debugCode = 2100;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperBasicDefensiveStructure" ) )
                {
                    BasicStructures.AddToConstructionList( entity );
                    if ( BasicStructuresPerPlanet.Construction[entity.Planet] == 0 )
                        BasicStructuresPerPlanet.Construction[entity.Planet] = 1;
                    else
                        BasicStructuresPerPlanet.Construction[entity.Planet]++;
                }
                debugCode = 2300;

                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperFloweredCrystal" ) )
                {
                    FloweredCrystals.AddToConstructionList( entity );
                }
                debugCode = 2500;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperAdvancedDefensiveStructure" ) )
                {
                    Watchtowers.AddToConstructionList( entity );
                    if ( AdvancedStructuresPerPlanet.Construction[entity.Planet] == 0 )
                        AdvancedStructuresPerPlanet.Construction[entity.Planet] = 1;
                    else
                        AdvancedStructuresPerPlanet.Construction[entity.Planet]++;
                }
                debugCode = 2700;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperBeachheader" ) )
                {
                    if ( BeachheaderStructuresPerPlanet.Construction[entity.Planet] == 0 )
                        BeachheaderStructuresPerPlanet.Construction[entity.Planet] = 1;
                    else
                        BeachheaderStructuresPerPlanet.Construction[entity.Planet]++;

                    Beachheaders.AddToConstructionList( entity );
                }
                debugCode = 3300;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SapperBeachheadConstructor" ) )
                {
                    Constructors.AddToConstructionList( entity );
                }

                debugCode = 3700;

                BasicStructuresPerPlanet.SwitchConstructionToDisplay();
                AdvancedStructuresPerPlanet.SwitchConstructionToDisplay();
                BeachheaderStructuresPerPlanet.SwitchConstructionToDisplay();

                debugCode = 3900;

                Sappers.SwitchConstructionToDisplay();
                SapperHabitats.SwitchConstructionToDisplay();
                BaseCrystals.SwitchConstructionToDisplay();
                FloweredCrystals.SwitchConstructionToDisplay();
                Watchtowers.SwitchConstructionToDisplay();
                CombatShips.SwitchConstructionToDisplay();
                Beachheaders.SwitchConstructionToDisplay();
                Constructors.SwitchConstructionToDisplay();
                BeachheadTurrets.SwitchConstructionToDisplay();
                BasicStructures.SwitchConstructionToDisplay();

                debugCode = 4400;

                if ( SapperHabitats.Count > 0 )
                    this.TimeLastHadSappers = World_AIW2.Instance.GameSecond;

                debugCode = 4600;

                int aipIntervals = (FactionUtilityMethods.Instance.GetCurrentAIP() / Difficulty.AIPIntervalForCalculatingBonusStructures).IntValue;

                debugCode = 4900;

                this.MaxBasicStructuresPerPlanet = Difficulty.BaseBasicStructuresPerPlanet + Difficulty.BasicStructuresPerPlanetIncreasePerAIPInterval * aipIntervals + Difficulty.BasicStructuresPerPlanetIncreasePerSpireCity * FactionUtilityMethods.Instance.GetFallenSpireCities();
                this.MaxAdvancedStructuresPerPlanet = Difficulty.BaseAdvancedStructuresPerPlanet + Difficulty.AdvancedStructuresPerPlanetIncreasePerAIPInterval * aipIntervals + Difficulty.AdvancedStructuresPerPlanetIncreasePerSpireCity * FactionUtilityMethods.Instance.GetFallenSpireCities();
                this.MaxBeachheaderStructuresPerPlanet = Difficulty.BaseBeachheaderStructuresPerPlanet + Difficulty.BeachheaderStructuresPerPlanetIncreasePerAIPInterval * aipIntervals + Difficulty.BeachheaderStructuresPerPlanetIncreasePerSpireCity * FactionUtilityMethods.Instance.GetFallenSpireCities();
                this.MaxWatchtowerStrength = Difficulty.BaseWatchtowerMaxStrength + Difficulty.WatchtowerStrengthIncreasePerAIPInterval * aipIntervals + Difficulty.WatchtowerStrengthIncreasePerSpireCity * FactionUtilityMethods.Instance.GetFallenSpireCities();
                //ArcenDebugging.ArcenDebugLogSingleLine("We have " + this.MaxAdvancedStructuresPerPlanet + " advanced max. Calculated with " + Difficulty.BaseAdvancedStructuresPerPlanet + " + aip component: " + aipIntervals + " * " + Difficulty.AdvancedStructuresPerPlanetIncreasePerAIPInterval + " and spire component: " + Difficulty.AdvancedStructuresPerPlanetIncreasePerSpireCity + " * " + FactionUtilityMethods.Instance.GetFallenSpireCities(), Verbosity.DoNotShow );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception debugCode " + debugCode + " in sappers stage 2 " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion end DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            UpdateBeachheadTurrets( Context );
        }

        #region UpdateBeachheadTurrets
        public void UpdateBeachheadTurrets( ArcenClientOrHostSimContextCore Context )
        {
            List<SafeSquadWrapper> beachheadTurrets = this.BeachheadTurrets.GetDisplayList();
            for ( int i = 0; i < beachheadTurrets.Count; i++ )
            {
                GameEntity_Squad entity = beachheadTurrets[i].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 500 )
                    continue;

                if ( World_AIW2.Instance.GameSecond % 10 == 0 )
                {
                    int damageToTake = entity.GetMaxHullPoints() / 10;
                    entity.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
                }
            }
        }
        #endregion

        #region UpdateAllegiance
        private void UpdateAllegiance()
        {
            bool localDebug = false;
            Faction faction = this.AttachedFaction;
            string allegiance = this.Allegiance;
            if ( string.IsNullOrEmpty( allegiance ) )
            {
                this.SetNewAllegianceIntoCoreSettings( "Friendly To Players" );
            }
            ExtraStrongMode = faction.GetBoolValueForCustomFieldOrDefaultValue( "ExtraStrongMode", true );
            if ( ArcenStrings.Equals( allegiance, "Allied To AI" ) )
            {
                AllegianceHelper.AllyThisFactionToAI( faction );
                AIAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "Friendly To Players" ) )
            {
                AllegianceHelper.AllyThisFactionToHumans( faction );
                PlayerAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( faction.ToString() + " is on team red", Verbosity.DoNotShow );
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Red" );
            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( faction.ToString() + " is on team blue", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Blue" );
            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Green" ) )
            {
                MinorFactionAllied = true;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( faction.ToString() + " is on team green", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Green" );
            }
        }
        #endregion

        #region GetSecondsTillMarkup
        public static int GetSecondsTillMarkup( GameEntity_Squad entity )
        {
            Faction faction = entity.PlanetFaction.Faction;
            int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            SappersDifficulty diff = SappersDifficultyTable.Instance.GetRowByIntensity( intensity, faction );
            if ( entity.CurrentMarkLevel >= 7 )
                return -1;
            int markInterval = 0;
            if ( entity.TypeData.GetHasTag( "SapperBeachheader" ) )
                markInterval = diff.SecondsForBeachheaderToMarkUp;
            if ( entity.TypeData.GetHasTag( "SapperAdvancedDefensiveStructure" ) )
                markInterval = diff.SecondsForAdvancedToMarkUp;
            if ( entity.TypeData.GetHasTag( "SapperBasicDefensiveStructure" ) )
                markInterval = diff.SecondsForBasicToMarkUp;
            if ( markInterval == 0 )
                throw new Exception( "Unable to figure out how to mark up a " + entity.ToStringWithPlanet() );
            int seconds = (entity.CurrentMarkLevel) * markInterval;
            return (seconds - entity.GetSecondsSinceCreation());
        }
        #endregion

        #region GetSappersStateForDisplay
        public void GetSappersStateForDisplay( ArcenDoubleCharacterBuffer buffer )
        {
            //For debug, this goes in the Threat menu
            buffer.Add( "\n" );
            if ( World_AIW2.Instance.GameSecond - this.TimeLastHadSappers > 2 )
                buffer.Add( "我们上次拥有工兵是 " ).Add( (World_AIW2.Instance.GameSecond - this.TimeLastHadSappers), "a1a1ff" ).Add( " 秒前。" );

            List<SafeSquadWrapper> habitats = this.SapperHabitats.GetDisplayList();
            for ( int i = 0; i < habitats.Count; i++ )
                buffer.Add( "我们有栖息地：" + habitats[i].ToStringWithPlanet() ).Add( "\n" );
            foreach ( GameEntity_Squad sapper in this.Sappers.DisplaySquads() )
            {
                buffer.Add( "我们有栖息地：" + sapper.ToStringWithPlanet() ).Add( "\n" );
            }
            buffer.Add( "每星球最大基础建筑：" ).Add( this.MaxBasicStructuresPerPlanet, "a1ffa1" ).Add( "\n" );
            buffer.Add( "每星球最大高级建筑：" ).Add( this.MaxAdvancedStructuresPerPlanet, "a1ffa1" ).Add( "\n" );
            buffer.Add( "每星球最大滩头建筑：" ).Add( this.MaxBeachheaderStructuresPerPlanet, "a1ffa1" ).Add( "\n" );
            buffer.Add( "最大瞭望塔强度：" ).Add( this.MaxWatchtowerStrength, "a1ffa1" ).Add( "\n" );
        }
        #endregion

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
                    if ( otherFaction.SpecialFactionData.InternalName != "Sappers" )
                        continue;

                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Sappers notification code, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
    }
}
