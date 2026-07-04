using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ScourgeInfusedHumanEmpireFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //Serialized

        //Unserialized
        public int Intensity = 0;
        public int Difficulty = 0;
        public Faction VassalFaction = null;
        public ScourgeVassalFactionBaseInfo VassalBaseInfo;
        public readonly DoubleBufferedList<ScourgeTypeData> UnlockedRaces = DoubleBufferedList<ScourgeTypeData>.Create_WillNeverBeGCed( 200, "Scourge-UnlockedRaces" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Flagships = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Scourge-Flagships" );

        public ScourgeInfusedHumanEmpireFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Intensity = 0;
            Difficulty = 0;
            VassalFaction = null;
            VassalBaseInfo = null;
            UnlockedRaces.Clear();
            Flagships.Clear();
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

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
        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            LoadCustomDataIfNeeded();
        }
        public void LoadCustomDataIfNeeded()
        {

        }
        #endregion

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "ScourgeEmpireStrength", true );
            Difficulty = cfg.GetIntValueForCustomFieldOrDefaultValue( "ScourgeEmpireDifficulty", true );

        }
        #endregion
        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            GetVassalFaction();
            UnlockedRaces.ClearConstructionListForStartingConstruction();
            Flagships.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity == null )
                    continue;

                if (entity.TypeData.GetHasTag("ScourgeFlagship") )
                {
                    ScourgePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    Flagships.AddToConstructionList(entity);
                }
            }

            for ( int i = 0; i < ScourgeTypeDataTable.Instance.Rows.Count; i++ )
            {
                ScourgeTypeData typeData = ScourgeTypeDataTable.Instance.Rows[i];
                bool found = false;
                switch( typeData.InternalName )
                {
                    case "Burlust":
                        if ( BurlustWarriorUnlocked())
                            found = true;
                        break;
                    case "Evuck":
                        if ( EvuckWarriorUnlocked())
                            found = true;
                        break;
                    case "Thoraxian":
                        if ( ThoraxianWarriorUnlocked())
                            found = true;
                        break;
                    case "Peltian":
                        if ( PeltianWarriorUnlocked())
                            found = true;
                        break;
                    case "Neinzul":
                        if ( NeinzulWarriorUnlocked())
                            found = true;
                        break;
                    case "Spire":
                        if ( SpireWarriorUnlocked())
                            found = true;
                        break;
                    case "Zenith":
                        if ( ZenithWarriorUnlocked())
                            found = true;
                        break;

                    default:
                        found = false;
                        break;
                }
                if ( found )
                    UnlockedRaces.AddToConstructionList( typeData );
            }


            UnlockedRaces.SwitchConstructionToDisplay();
            Flagships.SwitchConstructionToDisplay();
        }
        #endregion
        private void GetVassalFaction()
        {
            if ( VassalFaction == null )
            {
                 foreach ( Faction faction in World_AIW2.Instance.Factions )
                 {
                     if ( faction.SpecialFactionData.InternalName == "ScourgeVassal")
                     {
                         VassalFaction = faction;
                         break;
                     }
                 }
            }
            if ( VassalFaction == null )
            {
                throw new Exception("Unable to find scourge vassal faction");
            }
            if ( VassalBaseInfo == null )
            {
                GetVassalBaseInfo();
            }
            if ( VassalFaction == null )
            {
                throw new Exception("Unable to find scourge vassal baseinfo");
            }
        }
        #region GetVassalBaseInfo
        public ScourgeVassalFactionBaseInfo GetVassalBaseInfo()
        {
            if ( this.VassalFaction == null )
                GetVassalFaction();

            return this.VassalFaction.GetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
        }
        #endregion
        #region Checking Unlocks
        //The scourge use TechUpgrades to unlock various scourge races

        /// <summary>
        /// Returns true if the race fortress (TierOne) for this entity's race is buildable.
        /// Checks the {Race}DefenseTierOne tags which are set directly on the vassal fortress entities.
        /// </summary>
        public bool IsFortressBuildable( GameEntityTypeData typeData )
        {
            if ( typeData.GetHasTag( "BurlustDefenseTierOne" ) )   return BurlustWarriorUnlocked();
            if ( typeData.GetHasTag( "EvuckDefenseTierOne" ) )      return EvuckWarriorUnlocked();
            if ( typeData.GetHasTag( "ThoraxianDefenseTierOne" ) )  return ThoraxianWarriorUnlocked();
            if ( typeData.GetHasTag( "NeinzulDefenseTierOne" ) )    return NeinzulWarriorUnlocked();
            if ( typeData.GetHasTag( "PeltianDefenseTierOne" ) )    return PeltianWarriorUnlocked();
            if ( typeData.GetHasTag( "ZenithDefenseTierOne" ) )     return ZenithWarriorUnlocked();
            if ( typeData.GetHasTag( "SpireDefenseTierOne" ) )      return SpireWarriorUnlocked();
            return true; // not a race-gated TierOne structure
        }

        /// <summary>
        /// Returns true if the race greater fortress (TierTwo) for this entity's race is buildable.
        /// Requires tech level 2 (HybridUnlocked) for the matching race.
        /// </summary>
        public bool IsGreaterFortressBuildable( GameEntityTypeData typeData )
        {
            if ( typeData.GetHasTag( "BurlustDefenseTierTwo" ) )   return BurlustHybridUnlocked();
            if ( typeData.GetHasTag( "EvuckDefenseTierTwo" ) )      return EvuckHybridUnlocked();
            if ( typeData.GetHasTag( "ThoraxianDefenseTierTwo" ) )  return ThoraxianHybridUnlocked();
            if ( typeData.GetHasTag( "NeinzulDefenseTierTwo" ) )    return NeinzulHybridUnlocked();
            if ( typeData.GetHasTag( "PeltianDefenseTierTwo" ) )    return PeltianHybridUnlocked();
            if ( typeData.GetHasTag( "ZenithDefenseTierTwo" ) )     return ZenithHybridUnlocked();
            if ( typeData.GetHasTag( "SpireDefenseTierTwo" ) )      return SpireHybridUnlocked();
            return true; // not a race-gated TierTwo structure
        }

        public bool IsFortressUnlocked( GameEntityTypeData typeData)
        {
            if ( typeData.GetHasTag("BurlustRace") && BurlustWarriorUnlocked() )
                return true;
            if ( typeData.GetHasTag("EvuckRace") && EvuckWarriorUnlocked() )
                return true;
            if ( typeData.GetHasTag("ThoraxianRace") && ThoraxianWarriorUnlocked() )
                return true;
            if ( typeData.GetHasTag("NeinzulRace") && NeinzulWarriorUnlocked() )
                return true;
            if ( typeData.GetHasTag("PeltianRace") && PeltianWarriorUnlocked() )
                return true;
            if ( typeData.GetHasTag("ZenithRace") && ZenithWarriorUnlocked() )
                return true;
            if ( typeData.GetHasTag("SpireRace") && SpireWarriorUnlocked() )
                return true;
            return false;
        }
        public bool BurlustWarriorUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeBurlustUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 1 )
                return true;
            return false;
        }
        public bool BurlustHybridUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeBurlustUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 2 )
                return true;
            return false;
            
        }
        public bool EvuckWarriorUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeEvuckUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 1 )
                return true;
            return false;
        }
        public bool EvuckHybridUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeEvuckUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 2 )
                return true;
            return false;
            
        }
        public bool NeinzulWarriorUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeNeinzulUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 1 )
                return true;
            return false;
        }
        public bool NeinzulHybridUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeNeinzulUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 2 )
                return true;
            return false;
            
        }
        public bool PeltianWarriorUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgePeltianUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 1 )
                return true;
            return false;
        }
        public bool PeltianHybridUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgePeltianUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 2 )
                return true;
            return false;
            
        }
        public bool ThoraxianWarriorUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeThoraxianUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 1 )
                return true;
            return false;
        }
        public bool ThoraxianHybridUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeThoraxianUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 2 )
                return true;
            return false;
            
        }
        public bool SpireWarriorUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeSpireUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 1 )
                return true;
            return false;
        }
        public bool SpireHybridUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeSpireUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 2 )
                return true;
            return false;
            
        }
        public bool ZenithWarriorUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeZenithUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 1 )
                return true;
            return false;
        }
        public bool ZenithHybridUnlocked()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("ScourgeZenithUnlock");
            byte upgradeLevel = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevel >= 2 )
                return true;
            return false;
            
        }

        #endregion

        public static bool GetIsThisAScourgeEmpireFaction( Faction fac )
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
                case "ScourgeInfusedHumanEmpire":
                    return true; 
            }
            return false;
        }
        public static int GetScourgeEmpireFactionCount()
        {
            int count = 0;
            PlayerTypeData playerType;
            
            playerType = PlayerTypeDataTable.Instance.GetRowByName( "ScourgeInfusedHumanEmpire", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;
            
            return count;
        }
        #region CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying
        /// <summary>
        /// This is to support the Spire Sidekick
        /// This is called on every player faction when any unit is killed, period.  It identifies who the killing faction is, and allows for custom logic to be run.
        /// Many times, this will be utterly unrelated to anything the player faction needs to do.  But if the player faction is "the strongest faction of type X on that planet where the thing died,"
        /// for instance, then this is a method where that sort of thing can be calculated and then some reward can be granted.
        /// Note that the dyson's logic here is different because the killing faction can be null
        /// </summary>
        public override void CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying( Faction factionThatKilledEntityOrNull, bool IsFromOnlyPartOfStackDying, GameEntity_Squad entity,
            DamageSource Damage, EntitySystem FiringSystemOrNull, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            FInt multiplier = FInt.One;
            //bool debug = false;
            if ( this.GetShouldThisScourgeInfusedFactionGetARewardBasedOnThisKill( factionThatKilledEntityOrNull, entity, ref multiplier, Context ) )
            {
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );

                if ( entity_DLC3TypeData != null )
                {
                    // FInt scienceToGrantOnDeath = FInt.Zero;
                    // FInt hackingToGrantOnDeath = FInt.Zero;
                    // FInt resourceOneToGrantOnDeath = FInt.Zero;
                    entity_DLC3TypeData.GetScourgeEmpireResourcesToGrantOnDeath(entity,
                            out FInt corbomiteToGrantOnDeath );
                    if ( corbomiteToGrantOnDeath > FInt.Zero )
                    {
                        corbomiteToGrantOnDeath *= multiplier;
                        if ( corbomiteToGrantOnDeath < FInt.One )
                            corbomiteToGrantOnDeath = FInt.One;

                        this.AttachedFaction.StoredFactionResourceOne += corbomiteToGrantOnDeath;
                    }

                }
            }
        }
        #endregion
        #region GetShouldThisScourgeInfusedFactionGetARewardBasedOnThisKill
        private bool GetShouldThisScourgeInfusedFactionGetARewardBasedOnThisKill( Faction killingFactionOrNull, GameEntity_Squad entity, ref FInt multiplier, ArcenHostOnlySimContext Context )
        {
            //A bunch of rules; fundamentally, the rules are:
            //if we killed the unit, or were part of the fight, we get full resources
            //If this was killed by an Allied NPC faction, we get partial resources
            //If this was killed by another necromancer (and we weren't in the fight), we get nothing
            //If this was killed by an allied, non-necromancer faction we get partial resources

            FInt resourceMultiplierForAlliedHumanKill = FInt.FromParts( 0, 300 );
            FInt resourceMultiplierForAlliedNPCKill = FInt.FromParts( 1, 000 ); //full credit if your scourge buddies kill the target

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

                if ( killingFactionOrNull.Type == FactionType.Player )
                {
                    //30% resources from a human player
                    multiplier = resourceMultiplierForAlliedHumanKill;
                    return true;
                }
                else
                {
                    //100% resources from an NPC faction (like the scourge vassals)
                    multiplier = resourceMultiplierForAlliedNPCKill;
                    return true;
                }
            }
            return false; //if this specific human empire doesn't get these resources, someone else still might
        }
        #endregion

    }
}
