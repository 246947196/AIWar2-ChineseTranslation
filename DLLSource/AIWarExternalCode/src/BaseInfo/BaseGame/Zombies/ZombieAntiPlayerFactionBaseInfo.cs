using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZombieAntiPlayerFactionBaseInfo : ZombieFactionBaseInfo, IExternalBaseInfo_Singleton
    {
        //serialized

        //non-serialized
        public static ZombieAntiPlayerFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        //constants

        public ZombieAntiPlayerFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void SubCleanup()
        {
            Instance = null;

            HaveLoadedData = false; //force reload of the xml
        }

        #region Xml Data
        private bool HaveLoadedData = false;
        public int AttritionPercentPerMinute = -1;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;

            this.HaveLoadedData = true;
            AttritionPercentPerMinute = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AntiPlayerZombie_AttritionPercentPerMinute" );
        }
        #endregion

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        #region SubDoGeneralAggregationsPausedOrUnpaused
        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
        }
        #endregion

        #region SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            Faction faction = this.AttachedFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue;
                if ( otherFaction.Type == FactionType.NaturalObject )
                    continue;
                if ( otherFaction.SpecialFactionData.InternalName == "HunterFleet" || otherFaction.SpecialFactionData.InternalName == "AIWarden" ||
                    otherFaction.SpecialFactionData.InternalName == "PraetorianGuard" )
                {
                    faction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( faction );
                }
                switch ( otherFaction.Type )
                {
                    case FactionType.Player:
                        faction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( faction );
                        break;
                    case FactionType.AI:
                        faction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( faction );
                        break;
                }
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( AttritionPercentPerMinute == -1 )
                throw new Exception( "Failed to load attrition percent for anti player zombies\n" );
            if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            {
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    int damageToTake = (int)(((float)this.AttritionPercentPerMinute / 100) * (entity.GetMaxHullPoints()));
                    damageToTake += (entity.GetSecondsSinceCreation() / 300) * damageToTake; //every 5 minutes, double the attrition rate
                    entity.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
                    //ArcenDebugging.ArcenDebugLogSingleLine("entity " + entity.TypeData.InternalName + " " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is taking " + damageToTake + " damage", Verbosity.DoNotShow );
                }
            }
        }
        #endregion
    }
}
