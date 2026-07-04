using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    //TODO: make sure the GeneralHumanFaction Base/DeepInfo code appears properly in the spire infused variants
    public class DarkZenithSidekickFactionBaseInfo : DarkZenithFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //Serialized

        //Not Serialized
        public static DarkZenithSidekickFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        protected override void SubCleanup()
        {
            Instance = null;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 60 + (Intensity * 10);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Dark Zenith" );
            return load;
        }
        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }

        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
        public static bool GetIsThisADZFaction( Faction fac )
        {
            if ( fac == null )
                return false;
            if ( fac.Type != FactionType.Player )
                return false;
            PlayerTypeData playerTypeData = fac.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerTypeData == null )
                return false;
            switch (playerTypeData.InternalName)
            {
                case "DarkZenithSidekick":
                case "DarkZenithEmpire":
                    return true;
            }
            return false;
        }
        #region CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying
        /// <summary>
        /// This is called on every player faction when any unit is killed, period.  It identifies who the killing faction is, and allows for custom logic to be run.
        /// Many times, this will be utterly unrelated to anything the player faction needs to do.  But if the player faction is "the strongest faction of type X on that planet where the thing died,"
        /// for instance, then this is a method where that sort of thing can be calculated and then some reward can be granted.
        /// Note that the dyson's logic here is different because the killing faction can be null
        /// </summary>
        public override void CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying( Faction factionThatKilledEntityOrNull, bool IsFromOnlyPartOfStackDying, GameEntity_Squad entity,
            DamageSource Damage, EntitySystem FiringSystemOrNull, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            FInt multiplier = FInt.One;
            bool debug = false;
            if ( this.GetShouldThisDZFactionGetARewardBasedOnThisKill( factionThatKilledEntityOrNull, entity, ref multiplier, Context ) )
            {
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( debug ) {
                    int bonusMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "BonusNecromancerResources" );
                    ArcenDebugging.ArcenDebugLogSingleLine( "\t We get resources; multiplier " + multiplier + " (bonus multiplier component: " + bonusMultiplier +")" , Verbosity.DoNotShow );
                }

                if ( entity_DLC3TypeData != null )
                {
                    // FInt scienceToGrantOnDeath = FInt.Zero;
                    // FInt hackingToGrantOnDeath = FInt.Zero;
                    // FInt resourceOneToGrantOnDeath = FInt.Zero;
                    entity_DLC3TypeData.GetDarkZenithSidekickResourcesToGrantOnDeath(entity,
                        out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out DZResource resource, out int resourceAmount );
                    if ( hackingToGrantOnDeath > FInt.Zero )
                    {
                        hackingToGrantOnDeath *= multiplier;
                        if ( hackingToGrantOnDeath < FInt.One )
                            hackingToGrantOnDeath = FInt.One;

                        this.AttachedFaction.StoredHacking += hackingToGrantOnDeath;
                    }

                    if ( scienceToGrantOnDeath > FInt.Zero )
                    {
                        scienceToGrantOnDeath *= multiplier;
                        if ( scienceToGrantOnDeath < FInt.One )
                            scienceToGrantOnDeath = FInt.One;
                        this.AttachedFaction.StoredScience += scienceToGrantOnDeath;
                    }
                    if ( resourceAmount > 0 && resource != DZResource.None )
                    {
                        GameEntity_Squad flagship = FactionUtilityMethods.Instance.GetNearestFlagshipToPlanet_OrNull( this.AttachedFaction, entity.Planet, Context, WorkingFlagshipsList);
                        if ( flagship != null )
                        {
                            DarkZenithPerUnitBaseInfo data = flagship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                            data.Inventory[resource] += resourceAmount;
                        }
                    }
                }
            }
        }
        #endregion
        #region GetShouldThisDZFactionGetARewardBasedOnThisKill
        private bool GetShouldThisDZFactionGetARewardBasedOnThisKill( Faction killingFactionOrNull, GameEntity_Squad entity, ref FInt multiplier, ArcenHostOnlySimContext Context )
        {
            //A bunch of rules; fundamentally, the rules are:
            //if we killed the unit, or were part of the fight, we get full resources
            //If this was killed by an Allied NPC faction, we get partial resources
            //If this was killed by an allied, non-dyson faction we get partial resources
            FInt resourceMultiplierForAlliedHumanKill = FInt.FromParts( 0, 500 );
            FInt resourceMultiplierForAlliedNPCKill = FInt.FromParts( 0, 750 );

            if ( !entity.PlanetFaction.Faction.GetIsHostileTowards( this.AttachedFaction ) ) 
                return false; //We must be must be hostile to the killed unit
            if ( killingFactionOrNull == this.AttachedFaction )
                return true; //if we killed the unit, we get full resources
            PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( this.AttachedFaction );
            if ( pFaction.DataByStance[FactionStance.Self].TotalStrength > 500 )
                return true; //if we are involved with the fight, we get full resources

            //now for the case where we aren't involved with the fight
            if ( killingFactionOrNull != null &&
                 killingFactionOrNull.GetIsFriendlyTowards( this.AttachedFaction ) )
            {
                //Killed by an ally
                if ( killingFactionOrNull.Type == FactionType.Player )
                {
                    //30% resources from a human player
                    multiplier = resourceMultiplierForAlliedHumanKill;
                    return true;
                }
                else
                {
                    //75% resources from an NPC faction
                    multiplier = resourceMultiplierForAlliedNPCKill;
                    return true;
                }
            }
            return false; //if this specific dark zenith doesn't get these resources, someone else still might

        }
        #endregion

    }
    
}
