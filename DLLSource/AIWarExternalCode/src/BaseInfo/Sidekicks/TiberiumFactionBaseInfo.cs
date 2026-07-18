using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

//Tiberium buffs the AI
namespace Arcen.AIW2.External
{
    public class TiberiumFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //Serialized

        //Non-Serialized
        public static TiberiumFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public TiberiumDifficulty Difficulty;
        //public TiberiumDifficulty Difficulty;
        public readonly DoubleBufferedList<SafeSquadWrapper> Veins = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Tiberium-Veins" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Summoners = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Tiberium-Summoners" );
        public readonly DoubleBufferedList<SafeSquadWrapper> PatrolShips = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Tiberium-PatrolShips" );
        public readonly DoubleBufferedValue<int> VeinsBuildingSummoners = new DoubleBufferedValue<int>( 0 );
        int Intensity = 0;

        public TiberiumFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Instance = null;
            Veins.Clear();
            VeinsBuildingSummoners.Clear();
            Difficulty = null;
            Summoners.Clear();
            PatrolShips.Clear();
        }
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "TiberiumFactionBaseInfo" );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "TiberiumFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "TiberiumFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
        }
        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "10 来自泰伯利亚的负载" );
            return 10;
        }
        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }
        #endregion
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            if ( Intensity == -1 )
                DoRefreshFromFactionSettings();
            return Intensity;
        }
        #region DoRefreshFromFactionSettings 
        protected override void DoRefreshFromFactionSettings()
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                ConfigurationForFaction cfg = this.AttachedFaction.Config;
                if ( this.AttachedFaction.SpecialFactionData.TakesDifficultyFromArmada ) 
                    Intensity = FactionUtilityMethods.Instance.GetDifficultyFromArmadaSettings( this.AttachedFaction );
                else
                    Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue("Intensity", true);

                debugCode = 200;
                if ( ArmadaDifficultyTable.Instance == null)
                {
                    debugCode = 310;
                    ArcenDebugging.ArcenDebugLogSingleLine("null instance of diff table; you probably need to add Surrogate Table XML", Verbosity.DoNotShow );
                }

                Difficulty = TiberiumDifficultyTable.Instance.GetRowByIntensity(this.Intensity, this.AttachedFaction);
                debugCode = 300;
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("hit exception in DoRefreshFromFactionSettings debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            AllegianceHelper.AllyThisFactionToAI( this.AttachedFaction );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Veins.ClearConstructionListForStartingConstruction();
                Summoners.ClearConstructionListForStartingConstruction();
                PatrolShips.ClearConstructionListForStartingConstruction();
                VeinsBuildingSummoners.ClearConstructionValueForStartingConstruction();
                debugCode = 150;
                if ( Intensity == -1 )
                    DoRefreshFromFactionSettings();

                debugCode = 200;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 300;
                    if ( entity == null )
                        continue;
                    if (entity.TypeData.GetHasTag("TiberiumVein"))
                    {
                        TiberiumPerUnitBaseInfo data = entity.CreateExternalBaseInfo<TiberiumPerUnitBaseInfo>( "TiberiumPerUnitBaseInfo" );
                        Veins.AddToConstructionList(entity);
                        if ( data.NextUpgrade == TiberiumUpgrade.SpawnSummoner)
                            VeinsBuildingSummoners.Construction++;
                    }
                    if (entity.TypeData.GetHasTag("TiberiumSummoner"))
                    {
                        Summoners.AddToConstructionList(entity);
                    }

                    if ( entity.TypeData.GetHasTag( "TiberiumAutoDefenseShip" ) )
                    {
                        PatrolShips.AddToConstructionList(entity);
                        TiberiumPerUnitBaseInfo data = entity.CreateExternalBaseInfo<TiberiumPerUnitBaseInfo>( "TiberiumPerUnitBaseInfo" );
                        GameEntity_Squad HomeVein = data.HomeVein.GetSquad();
                        if ( entity.PlanetFaction.Faction != HomeVein?.PlanetFaction.Faction ) {
                            HomeVein = null;
                            data.HomeVein.Clear();
                        }
                        if ( HomeVein == null ) {
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut ); //if we've lost our Vein, we die
                        }
                    }

                }

                Veins.SwitchConstructionToDisplay();
                Summoners.SwitchConstructionToDisplay();
                PatrolShips.SwitchConstructionToDisplay();
                VeinsBuildingSummoners.SwitchConstructionToDisplay();
                debugCode = 350;

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during tiberium stage 2 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
//            AllegianceHelper.AllyThisFactionAI( AttachedFaction );

            // HandleTransportArrival(Context);
            // HandleMetalIncome();
            // HandleScienceIncome();
            // HandleResourceOneIncome();
            // HandleHackingIncome();
            // HandleEnemyIncome();
            // HandleTransportArrival(Context);
            // LoadOrUnloadFlagships( Context );
            // UpdateFleetState(Context);
        }
        public bool DoesPlanetHaveVein( Planet planet )
        {
            bool foundVein = false;
            foreach ( GameEntity_Squad vein in this.Veins.DisplaySquads() )
            {
                if ( vein.Planet == planet ) {
                    foundVein = true;
                    break;
                }
            }
            return foundVein;

        }
    }
}
