using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class MacrophageFactionBaseInfoCore : ExternalFactionBaseInfoRoot
    {
        //serialized

        //not serialized
        public abstract bool IsTamed { get; }
        public abstract bool IsEnraged { get; }
        public int EffectiveIntensity = -1;

        public readonly DoubleBufferedList<SafeSquadWrapper> Telia = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Macrophage-Telia" );
        public readonly DoubleBufferedDictionary<Planet, int> TeliaPlanets = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 300, "Macrophage-TeliaPlanets" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Harvesters = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Macrophage-Harvesters" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Spores = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Macrophage-Spores" );
        public readonly DoubleBufferedList<SafeSquadWrapper> SpireTelia = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Macrophage-SpireTelia" );

        public static List<MacrophageFactionBaseInfoCore> AllMacrophageFactionsOfAnyTypes = List<MacrophageFactionBaseInfoCore>.Create_WillNeverBeGCed( 10, "MacrophageFactionBaseInfoCore-AllMacrophageFactionsOfAnyTypes" );
        public static readonly DictionaryOfDoubleBufferedLists<int, SafeSquadWrapper> CrossFaction_SporesPerPlanet = DictionaryOfDoubleBufferedLists<int, SafeSquadWrapper>.Create_WillNeverBeGCed( 500, 200, "MacrophageFactionBaseInfoCore-CrossFaction_SporesPerPlanet" );
        public static bool CrossFaction_SporesPerPlanet_CanRun = false;
        private static bool working_HaveCrossFaction_SporesPerPlanetBeenClearedYetThisCycle = false;
        private static bool working_HaveCrossFaction_SporesPerPlanetBeenSwappedYetThisCycle = false;

        public bool FinishedLoadingLogic = false; // Whenever we load, recalculate some things.
        public bool aiAllied = false;
        public bool humanAllied = false;
        public bool canBerserk = false;
        public bool isBerserk = false;
        public bool canUseSmartExpansionLogic = false;
        public bool isUsingSmartExpansionLogic = false;
        public bool isLoner = false;

        //metal generation and planetary telium caps are done on a subfaction by subfaction basis
        public int TeliaPerPlanet = -1;
        public int MetalGenerationPerSecond = -1;

        //let the player know when we're berserk every now and then without spamming
        public int GameSecondForLastMessage;

        #region Constants
        //constants
        public const string MACROPHAGE_TAG = "Macrophage";
        public const string HarvesterTag = "MacrophageHarvester"; // General tag for any Harvester that needs our movement and collection logic.
        public const string RegularHarvesterTag = "RegularMacrophageHarvester"; // Naturally spawnt in vanilla.
        public const string SpireHarvesterTag = "SpireMacrophageHarvester"; // 50% chance of being spawnt instead of Regular if Wild or Tamed get Debris.
        public const string EnragedHarvesterTag = "MacrophageEnragedHarvester"; // General tag for angry Harvesters that just want to eat things.
        public const string RegularEnragedHarvesterTag = "RegularMacrophageEnragedHarvester"; // Enraged version of Regular Harvesters.
        public const string SpireEnragedHarvesterTag = "SpireMacrophageEnragedHarvester"; // Enraged version of Spire Harvesters.
        public const string TeliumTag = "MacrophageTelium";
        public const string SpireTeliumTag = "SpireMacrophageTelium";
        public const string SporeTag = "MacrophageSpore";
        #endregion

        public static readonly bool debug = false;

        #region From Xml
        //Here are some constants
        private bool hasInitializedConstants = false;
        public int MetalHarvesterCanHold;
        public int EarlyHarvesterSpawnTime; //Ordinarily it will take some time for Telia to get enough metal to spawn their first harvester. If we want them to just start out having a harvester early then set this value
        public int MetalForEvent;
        public int MetalForEventPlayerOnlyHostileMode; //this is an override
        public FInt EventCostMultiplierPerHarvesterBase; //multiply the above costs by this for every harvester
        public FInt EventCostMultiplierPerHarvesterDecreasePerIntensity; //lowers the above value by this for every intensity level
        public int SporeLifespan;
        public int TeliaPerPlanetLow;
        public int TeliaPerPlanetMed;
        public int TeliaPerPlanetHigh;
        public int NumDifferentTeliaRequired; 
        public int PercentSporeRelease;
        public int PercentSporeReleaseIncreasePerHarvester;
        public int SporesPerRelease;
        public int SpireSporesPerRelease;
        public int HarvesterLimitBeforeEnraging;
        public int SpireHarvesterLimitBeforeEnraging;
        public int HarvestersToEnrage;
        public int SpireHarvestersToEnrage;
        public int MetalGenerationPerSecondLow;
        public int MetalGenerationPerSecondMed;
        public int MetalGenerationPerSecondHigh;
        public FInt MetalGenerationMultiplierWithLivingHarvesters = FInt.One; //we want to decrease the base metal income when the Macrophage has harvesters (since harvesting should be the primary means of income)
        public int HarvesterMetalFromKillMultiplier;
        public int MetalGainedFromVisitingMine;
        public int BasePercentChanceToMarkUp;
        public int ReductionPerMarkForPercentChanceToMarkUp;
        public int BerserkCostDivisor;
        public int SpireTeliumCostDivisor;
        public int MinBerserkIntensity;
        public int MinSmartExpansionLogicIntensity;
        public int MaxTeliaForSmartExpansionLogic;
        public int MinimumSnackTimeOnSpawn;
        public int MaximumSnackTimeOnSpawn;
        public int MinimumSnackTimeOnNewPlanet;
        public int MaximumSnackTimeOnNewPlanet;
        public int SnackTimeIncreasePerMark;
        public int MinimumBreakTime;
        public int MaximumBreakTime;
        public int BreakTimeIncreasePerMark;

        private void InitializeConstantsIfNecessary()
        {
            if ( !hasInitializedConstants )
            {
                hasInitializedConstants = true;
                MetalForEvent = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MetalForSporeOrHarvesterSpawn" );
                EventCostMultiplierPerHarvesterBase = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Macrophage_SpawnCostMultiplierPerOwnedHarvester" );
                EventCostMultiplierPerHarvesterDecreasePerIntensity = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Macrophage_SpawnCostMultiplierDecreasePerIntensity" );
                SporeLifespan = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_SporesLifetimeInSeconds" );
                NumDifferentTeliaRequired = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_NumTeliumSporesNeededForNewTelium" );
                MetalHarvesterCanHold = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MaxMetalHarvesterCanHold" );
                PercentSporeRelease = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_PercentSporeRelease" );
                PercentSporeReleaseIncreasePerHarvester = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_PercentSporeReleaseIncreasePerHarvester" );
                SporesPerRelease = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_SporesPerRelease" );
                SpireSporesPerRelease = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_SpireSporesPerRelease" );
                HarvesterLimitBeforeEnraging = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_HarvesterLimitBeforeEnraging" );
                SpireHarvesterLimitBeforeEnraging = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_SpireHarvesterLimitBeforeEnraging" );
                HarvestersToEnrage = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_HarvestersToEnrage" );
                SpireHarvestersToEnrage = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_SpireHarvestersToEnrage" );
                EarlyHarvesterSpawnTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_EarlyHarvesterSpawnTime" );
                TeliaPerPlanetLow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_TeliaPerPlanetLow" );
                TeliaPerPlanetMed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_TeliaPerPlanetMed" );
                TeliaPerPlanetHigh = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_TeliaPerPlanetHigh" );
                MetalGenerationPerSecondLow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MetalGenerationPerSecondLow" );
                MetalGenerationPerSecondMed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MetalGenerationPerSecondMed" );
                MetalGenerationPerSecondHigh = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MetalGenerationPerSecondHigh" );
                MetalForEventPlayerOnlyHostileMode = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MetalForSporeOrHarvesterSpawnPlayerOnlyHostileMode" );
                MetalGenerationMultiplierWithLivingHarvesters = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_Macrophage_MetalGenerationMultiplierWithLivingHarvesters" );
                HarvesterMetalFromKillMultiplier = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_HarvesterMetalFromKillMultiplier" );
                MetalGainedFromVisitingMine = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MetalGainedFromVisitingMine" );
                BasePercentChanceToMarkUp = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_BasePercentChanceToMarkUp" );
                ReductionPerMarkForPercentChanceToMarkUp = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_ReductionPerMarkForPercentChanceToMarkUp" );
                BerserkCostDivisor = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_BerserkCostDivisor" );
                SpireTeliumCostDivisor = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_SpireTeliumCostDivisor" );
                MinBerserkIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MinIntensityForBerserk" );
                MinSmartExpansionLogicIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MinIntensityForSmartExpansionLogic" );
                MaxTeliaForSmartExpansionLogic = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MaxTeliumForSmartExpansionLogic" );
                MinimumSnackTimeOnSpawn = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MinimumSnackTimeOnSpawn" );
                MaximumSnackTimeOnSpawn = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MaximumSnackTimeOnSpawn" );
                MinimumSnackTimeOnNewPlanet = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MinimumSnackTimeOnNewPlanet" );
                MaximumSnackTimeOnNewPlanet = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MaximumSnackTimeOnNewPlanet" );
                MinimumBreakTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MinimumBreakTime" );
                MaximumBreakTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MaximumBreakTime" );
            }
        }
        #endregion

        public MacrophageFactionBaseInfoCore()
        {
            Cleanup();
        }

        #region Cleanup
        protected sealed override void Cleanup()
        {
            //serialized

            //not serialized
            EffectiveIntensity = -1;
            Telia.Clear();
            TeliaPlanets.Clear();
            Harvesters.Clear();
            Spores.Clear();
            SpireTelia.Clear();

            AllMacrophageFactionsOfAnyTypes.Clear();
            CrossFaction_SporesPerPlanet_CanRun = false;

            CrossFaction_SporesPerPlanet.Clear();

            working_HaveCrossFaction_SporesPerPlanetBeenClearedYetThisCycle = false;
            working_HaveCrossFaction_SporesPerPlanetBeenSwappedYetThisCycle = false;

            FinishedLoadingLogic = false;
            aiAllied = false;
            humanAllied = false;
            canBerserk = false;
            isBerserk = false;
            canUseSmartExpansionLogic = false;
            isUsingSmartExpansionLogic = false;
            isLoner = false;

            TeliaPerPlanet = -1;
            MetalGenerationPerSecond = -1;
            GameSecondForLastMessage = 0;

            hasInitializedConstants = false; //force reload

            this.SubCleanup();
        }

        protected abstract void SubCleanup();
        #endregion end Cleanup

        public sealed override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.SubSerializeFactionTo( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public sealed override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.SubDeserializeFactionIntoSelf( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        protected sealed override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            InitializeConstantsIfNecessary();
            DoFinishedLoadingLogicIfNeedBe();

            if ( !AllMacrophageFactionsOfAnyTypes.Contains( this ) )
                AllMacrophageFactionsOfAnyTypes.Add( this );

            this.SubDoGeneralAggregationsPausedOrUnpaused();
        }
        protected abstract void SubDoGeneralAggregationsPausedOrUnpaused();

        #region DoFinishedLoadingLogicIfNeedBe
        private void DoFinishedLoadingLogicIfNeedBe()
        {
            if ( !FinishedLoadingLogic )
            {
                this.EffectiveIntensity = this.IsTamed ? (byte)10 : this.AttachedFaction.GetIntValueForCustomFieldOrDefaultValue( "Intensity", !this.IsEnraged && !this.IsTamed );
                if ( this.IsEnraged )
                {
                    //Enraged macrophages don't have an Intensity setting directly, and neither do Tamed.
                    //It looks like Tamed just go all the way to 10 no matter what, which is fine.
                    //But for Enraged, let's set the intensity based on the highest of the main macrophage factions we find
                    foreach ( Faction faction in World_AIW2.Instance.Factions )
                    {
                        if ( faction.SpecialFactionData.InternalName != "MacrophageInfestation" )
                            continue; //look for root macrophage factions
                        int otherIntensity = faction.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
                        if ( otherIntensity > this.EffectiveIntensity )
                            this.EffectiveIntensity = otherIntensity;
                    }
                }

                if ( this.EffectiveIntensity > 7 || this.IsTamed )
                {
                    TeliaPerPlanet = TeliaPerPlanetHigh;
                    MetalGenerationPerSecond = MetalGenerationPerSecondHigh;
                }
                else if ( this.EffectiveIntensity > 3 )
                {
                    TeliaPerPlanet = TeliaPerPlanetMed;
                    MetalGenerationPerSecond = MetalGenerationPerSecondMed;
                }
                else
                {
                    TeliaPerPlanet = TeliaPerPlanetLow;
                    MetalGenerationPerSecond = MetalGenerationPerSecondLow;
                }

                if ( !this.IsEnraged && !this.IsTamed &&
                    ArcenStrings.Equals( this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "SpawningOptions", true ), "Lone Telium" ) )
                    isLoner = true; // Give up on expanding.
                if ( this.EffectiveIntensity >= MinBerserkIntensity || isLoner )
                    canBerserk = true; // Get angry when threatened.
                if ( this.EffectiveIntensity >= MinSmartExpansionLogicIntensity || this.IsTamed )
                    canUseSmartExpansionLogic = true; // Group spores together when Telia count is low.

                FinishedLoadingLogic = true;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Finished Loading Logic for Macrophage " + this.AttachedFaction.FactionIndex +
                        " Is Loner: " + isLoner + " Can Berserk: " + canBerserk + " Can Use Smart Expansion Logic: " + canUseSmartExpansionLogic +
                        " Tame: " + this.IsTamed + " Enraged: " + this.IsEnraged, Verbosity.DoNotShow );
            }
        }
        #endregion

        #region WriteFactionSlotStatus
        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            base.WriteFactionSlotStatus( buffer );

            if ( isLoner )
            {
                buffer.Add( "  Lone Telium" );
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim_ClientAndHost
        public sealed override void DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( !working_HaveCrossFaction_SporesPerPlanetBeenClearedYetThisCycle )
            {
                //only do this on the first faction, doesn't matter which one it is.
                //doing it more times won't hurt anything, but wastes CPU
                working_HaveCrossFaction_SporesPerPlanetBeenClearedYetThisCycle = true;

                //ready these for across all factions
                CrossFaction_SporesPerPlanet.ClearConstructionListForStartingConstruction();

                //go ahead and set this to be false, so that later this can be processed
                //right now it is probably true, from the last cycle.
                working_HaveCrossFaction_SporesPerPlanetBeenSwappedYetThisCycle = false;
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public sealed override void DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            //down below in the main Stage2Aggregating we're filling CrossFaction_SporesPerPlanet.Construction
            //this is at least 3 factions (tamed, enraged, and 1+ macrophage factions), but could be more.
            //at this point we need to swap CrossFaction_SporesPerPlanet, but we can ONLY do that once, regardless of how many factions there are
            if ( !working_HaveCrossFaction_SporesPerPlanetBeenSwappedYetThisCycle )
            {
                //this makes sure we only do it once
                working_HaveCrossFaction_SporesPerPlanetBeenSwappedYetThisCycle = true;

                //without this, the display lists will remain blank.  But it can only happen once per loop, not once per faction per loop!
                CrossFaction_SporesPerPlanet.SwitchConstructionToDisplay();

                //Key note!  This is what gives it permission to run ONCE
                CrossFaction_SporesPerPlanet_CanRun = true;

                //we had better set THIS one to false now, so that next time we get back to Stage1Clearing
                //that logic will kick off properly again.
                working_HaveCrossFaction_SporesPerPlanetBeenClearedYetThisCycle = false;
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public sealed override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( !FinishedLoadingLogic )
                return; // Do not process if not entirely loaded.

            // Add to our main Macrophage class's statics.

            this.Spores.ClearConstructionListForStartingConstruction();
            this.Telia.ClearConstructionListForStartingConstruction();
            this.TeliaPlanets.ClearConstructionDictForStartingConstruction();
            this.Harvesters.ClearConstructionListForStartingConstruction();
            this.SpireTelia.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( SporeTag ) )
            {
                if ( entity == null )
                    continue;
                MacrophagePerSporeBaseInfo sDataOrNull = entity.TryGetExternalBaseInfoAs<MacrophagePerSporeBaseInfo>();
                if ( sDataOrNull != null && sDataOrNull.SpawnTime + SporeLifespan <= World_AIW2.Instance.GameSecond )
                {
                    //kill this instead of adding it to the list
                    entity.Despawn( Context, true, InstancedRendererDeactivationReason.MyLifespanRanOut );
                    continue;
                }

                Spores.AddToConstructionList( entity );
                CrossFaction_SporesPerPlanet[entity.Planet.Index].AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( TeliumTag ) )
            {
                if ( entity == null )
                    continue;
                Telia.AddToConstructionList( entity );

                Planet plan = entity.Planet;
                if ( plan == null )
                    continue;

                TeliaPlanets.Construction[plan]++;
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( SpireTeliumTag ) )
            {
                if ( entity == null )
                    continue;
                SpireTelia.AddToConstructionList( entity );
                Telia.AddToConstructionListIfNotAlreadyIn( entity );

                Planet plan = entity.Planet;
                if ( plan == null )
                    continue;

                TeliaPlanets.Construction[plan]++;
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( HarvesterTag ) )
            {
                if ( entity == null )
                    continue;
                //Note that the Harvesters list is not constant, since we will remove
                //entries from it and add it to each Telium's list
                this.Harvesters.AddToConstructionList( entity );
            }

            this.Spores.SwitchConstructionToDisplay();
            this.Telia.SwitchConstructionToDisplay();
            this.TeliaPlanets.SwitchConstructionToDisplay();
            this.Harvesters.SwitchConstructionToDisplay();
            this.SpireTelia.SwitchConstructionToDisplay();

            this.SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );

            UpdateAllegiance();
        }
        protected abstract void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context );
        #endregion end DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost

        #region GetSporeChance
        public int GetSporeChance( int harvesterCount )
        {
            // Chance to spawn spores:
            // Min 25%, Max 75%
            // Base chance: 50%
            // Increase by 10% for each Harvester beyond the first
            // Decrease by 2% for each Telia in the galaxy
            return Math.Max( 25, Math.Min( 75, PercentSporeRelease + (harvesterCount * PercentSporeReleaseIncreasePerHarvester) - (2 * GlobalTeliaCount()) ) );
        }
        #endregion

        #region GlobalTeliaCount
        public static int GlobalTeliaCount()
        {
            int count = 0;
            //first get all of the regular macrophage telia
            if ( MacrophageFactionBaseInfo.AllRegularMacrophageFactions.Count > 0 )
                for ( int x = 0; x < MacrophageFactionBaseInfo.AllRegularMacrophageFactions.Count; x++ )
                    if ( MacrophageFactionBaseInfo.AllRegularMacrophageFactions[x] != null && MacrophageFactionBaseInfo.AllRegularMacrophageFactions[x].Telia != null )
                        count += MacrophageFactionBaseInfo.AllRegularMacrophageFactions[x].Telia.Count;
            //then get the enraged ones
            if ( MacrophageEnragedFactionBaseInfo.Instance != null )
                count += MacrophageEnragedFactionBaseInfo.Instance.Telia.Count;
            //then get the tamed ones
            if ( MacrophageTamedFactionBaseInfo.Instance != null )
                count += MacrophageTamedFactionBaseInfo.Instance.Telia.Count;
            return count;
        }
        #endregion

        #region UpdateAllegiance
        protected virtual void UpdateAllegiance()
        {
            if ( this.IsEnraged )
            {
                AllegianceHelper.EnemyThisFactionToAll( AttachedFaction );
                return;
            }
            if ( this.IsTamed )
            {
                // Always be friendly to players.
                AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
                return;
            }

            if ( ArcenStrings.Equals( this.Allegiance, "对所有敌对" ) ||
               ArcenStrings.Equals( this.Allegiance, "HostileToAll" ) ||
               isLoner ||
               string.IsNullOrEmpty( this.Allegiance ) )
            {
                this.humanAllied = false;
                this.aiAllied = false;
                if ( string.IsNullOrEmpty( this.Allegiance ) )
                    throw new Exception( "empty Macrophage allegiance '" + this.Allegiance + "'" );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Macrophage faction should be hostile to all (default)", Verbosity.DoNotShow );
                //make sure this isn't set wrong somehow
                AllegianceHelper.EnemyThisFactionToAll( AttachedFaction );
            }
            else if ( ArcenStrings.Equals( this.Allegiance, "仅对玩家敌对" ) ||
                    ArcenStrings.Equals( this.Allegiance, "HostileToPlayers" ) )
            {
                this.aiAllied = true;
                AllegianceHelper.AllyThisFactionToAI( AttachedFaction );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Macrophage faction should be friendly to the AI and hostile to players", Verbosity.DoNotShow );

            }
            else if ( ArcenStrings.Equals( this.Allegiance, "小派系小队红" ) )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This macrophage faction is on team red", Verbosity.DoNotShow );
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, "小派系小队红" );
            }
            else if ( ArcenStrings.Equals( this.Allegiance, "小派系小队蓝" ) )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This macrophage faction is on team blue", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, "小派系小队蓝" );
            }
            else if ( ArcenStrings.Equals( this.Allegiance, "小派系小队绿" ) )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This macrophage faction is on team green", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, "小派系小队绿" );
            }

            else if ( ArcenStrings.Equals( this.Allegiance, "HostileToAI" ) ||
                    ArcenStrings.Equals( this.Allegiance, "对玩家友好" ) )
            {
                this.humanAllied = true;
                AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Macrophage faction should be hostile to the AI and friendly to players", Verbosity.DoNotShow );
            }
            else
            {
                throw new Exception( "unknown Macrophage allegiance '" + this.Allegiance + "'" );
            }
        }
        #endregion

        #region GetEventCost
        public int GetEventCost( int harvesterCount, GameEntity_Squad telium )
        {
            // Get base cost based on allegience. If we only hate players, our events are more expensive.
            int cost;
            if ( aiAllied && !humanAllied )
                cost = MetalForEventPlayerOnlyHostileMode;
            else
                cost = MetalForEvent;

            // Multiply cost based on our harvester count, and our defined multiplier, treating Tamed as intensity 10.
            int intensity = this.IsTamed ? (byte)10 : this.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            FInt multiplier = EventCostMultiplierPerHarvesterBase - (EventCostMultiplierPerHarvesterDecreasePerIntensity * intensity);
            cost += (cost * (harvesterCount * multiplier)).ToInt();

            // If berserk or Spire Telium; lower cost further.
            int divisor = 1;
            if ( isBerserk )
                divisor = BerserkCostDivisor;
            if ( telium != null && telium.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) )
                divisor = Math.Max( divisor, SpireTeliumCostDivisor );
            cost /= divisor;

            return cost;
        }
        #endregion
    }
}
