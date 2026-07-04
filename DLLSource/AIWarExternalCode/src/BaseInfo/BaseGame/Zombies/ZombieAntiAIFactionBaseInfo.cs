using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZombieAntiAIFactionBaseInfo : ZombieFactionBaseInfo, IExternalBaseInfo_Singleton
    {
        //serialized

        //non-serialized
        public static ZombieAntiAIFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        //constants

        public ZombieAntiAIFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void SubCleanup()
        {
            Instance = null;

            HaveLoadedData = false; //trigger xml reload
        }

        #region Xml Data
        public bool HaveLoadedData = false;
        public bool AllowedToLeaveSpawnPlanet = false;
        public int MaxShipsAllowedToLeavePerPlanetPerIteration = -1;
        public int MinShipsAllowedToLeavePerPlanetPerIteration = -1;
        public int AttritionPercentPerMinute = 0;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;

            this.HaveLoadedData = true;
            AllowedToLeaveSpawnPlanet = ExternalConstants.Instance.GetCustomBool_Slow( "custom_bool_AntiAIZombie_AllowedToLeaveSpawnPlanet" );
            MaxShipsAllowedToLeavePerPlanetPerIteration = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AntiAIZombie_MaxShipsAllowedToLeavePerPlanetPerIteration" );
            MinShipsAllowedToLeavePerPlanetPerIteration = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AntiAIZombie_MinShipsAllowedToLeavePerPlanetPerIteration" );
            AttritionPercentPerMinute = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AntiAIZombie_AttritionPercentPerMinute" );
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
            //AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
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
                    faction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( faction );
                }
                switch ( otherFaction.Type )
                {
                    case FactionType.Player:
                        faction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( faction );
                        break;
                    case FactionType.AI:
                        faction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( faction );
                        break;
                }
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            //TEACHING_MOMENT: Many Stage3 bits of logic can be run on BaseInfo (aka on the client AND host), or in DeepInfo (aka just on the host).  Which to do?
            //
            //Since this is just basic attrition logic, we're going to have many fewer desyncs on clients if we run this here.
            //The main question is if we're more likely to introduce desyncs by having the client run the logic, or having the client not run it.
            //A desync isn't a crisis -- this codebase is wolverine, and self-heals fast.  The cost of a desync is a bit of CPU and a bit of bandwith.
            //Don't stress about thinking TOO hard about it, because floating point imprecision and similar things will cause desyncs without any help from you.
            //But when it's something like this... well, hey, if there's a ton of zombies, we just reduced likely bandwidth requirements at least a bit
            //by moving this logic into BaseInfo.

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "DefensiveZombies" ) )
                this.AttritionPercentPerMinute += 15; //faster attrition for anti-ai zombies in defensive mode

            if ( World_AIW2.Instance.GameSecond % 60 == 0 && this.AttritionPercentPerMinute > 0 )
            {
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    int damageToTake = (int)(((float)this.AttritionPercentPerMinute / 100) * (entity.GetMaxHullPoints()));
                    entity.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("entity " + entity.TypeData.InternalName + " " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is taking " + damageToTake + " damage", Verbosity.DoNotShow );
                }
            }
        }
        #endregion
    }
}
