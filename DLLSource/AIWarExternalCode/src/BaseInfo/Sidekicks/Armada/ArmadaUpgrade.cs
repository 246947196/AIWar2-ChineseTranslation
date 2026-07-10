using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public enum ArmadaUpgradeType : byte
    {
        None = 0,
        UnlockSkeletonType,
        UnlockWightType,
        UnlockMummyType,
        UnlockNewShip,
        GrantEssence,
        IncreaseSkeletonCap,
        IncreaseWightCap,
        IncreaseSkeletonSoftCap,
        IncreaseWightSoftCap,
        ClaimBlueprints,
        End
    }
    public enum ArmadaUpgradeTarget : byte
    {
        None = 0,
        Fleet = 1,
        Faction = 2,
        End
    }
    public class ArmadaUpgrade : ArcenDynamicTableRow, IConcurrentPoolable<ArmadaUpgrade>, IProtectedListable, IFleetTransformTarget
    {
        //Here's how this works. The ArmadaGlobalData serializes to disk a list of achieved index upgrades
        //After we reload a game, we check if this list has been transformed into a list of ArmadaUpgrades
        //If not then we translate the indices into a list of upgrades for later use. I think there's a way to make sure we do this at game load time, so we don't have to sweat about it not being loaded at any point during normal sim

        //In the rest of the code we assume the list has been  upgraded
        public int Index;
        public ArmadaUpgradeType Type;
        //we have two types of ships we record. First, there are the ships (or structures) whose cap is increased. For example, we might increase the cap for Skeleton Warriors
        //However, we also have situations where we increase the cap of the Skeleton Warrior Home (which unlocks skeleton warriors); in that case for the UI we want to say things like
        //"This upgrade grants Skeleton Warrior Homes. And here's the stats for skeleton warriors so you can understand what's happening".  We use the RelatedShipTypeName for skeleton warriors in that case
        public string ShipTypeNameForCapIncrease;
        public string RelatedShipTypeName;
        public int CapIncrease;
        public int PrereqUpgradeIndex1;
        public int PrereqUpgradeIndex2;
        public ArmadaUpgradeTarget Target;
        public bool ReplaceIfAlreadyUpgraded; // if this is available at a rift and you upgrade it at another rift, this is automatically replaced
        public bool AlwaysAvailable; // This is always available at a rift, if not removed removed for some other reason.
        public int MinFlagshipLevel;
        public bool ForInitialRift;
        public bool ForSecondRift;
        public bool CanBeBonusStartingSkeleton;
        public bool CanBeBonusStartingWight;
        public GameEntityTypeData ShipForCapIncrease;
        public GameEntityTypeData RelatedShip;
        public FInt RelatedResource_Normal;
        public FInt RelatedResource_Scarce;
        public bool ShouldNotAppearInRift;
        public bool UtilityUpgrade;
        public int MustHaveAnyStrongholdAtThisLevel = 0;
        public int AdditionalAIPCost = 0;
        public readonly List<string> TagsList = List<string>.Create_WillNeverBeGCed( 30, "ArmadaUpgrade-TagsList" );
        public FInt BlueprintTransformCostInHacking = FInt.Zero;
        public FInt BlueprintTransformCostInResourceOne = FInt.Zero;

        public FInt RelatedResource {
            get {
                if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("ArmadaScarceResources")) {
                    return RelatedResource_Scarce;
                } else {
                    return RelatedResource_Normal;
                }
            }
        }

        public static ArmadaUpgrade DeserializeNewFrom( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            ArmadaUpgrade result = new ArmadaUpgrade();
            result.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType );
            return result;
        }
        public void DeserializedIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Index = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "Index" );
        }
        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (Int16)this.Index, "Index" );
        }
        public void ToDisplayBuffer( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add( "\t" ).Add( this.DisplayName ).Add( "\n" );
        }
        public bool GetHasTag( string Tag )
        {
            //this isn't as efficient as the GameEntityTypeData version, but it's used very infrequently.
            for ( int i = 0; i < TagsList.Count; i++ )
            {
                if ( TagsList[i] == Tag )
                    return true;
            }
            return false;
        }
        /// <summary>Describe the upgrade for use as options, in particular, starting options.</summary>
        public override void AddDescription(ArcenCharacterBufferBase buffer)
        {
            switch (this.Type) {
                case ArmadaUpgradeType.UnlockSkeletonType:
                case ArmadaUpgradeType.UnlockWightType:
                case ArmadaUpgradeType.UnlockMummyType:
                    buffer.Add(this.RelatedShip.Description).SkipLine();
                    buffer.Add("此升级将允许建造 ").Add( this.CapIncrease, "ffa1a1" ).Add(" 个额外的 ").Add(this.ShipForCapIncrease.GetDisplayName() ).Add( " 在任何据点。这将允许该据点的旗舰获得 ").Add(this.RelatedShip.GetDisplayName(), "a1ffa1").Add(" 当你击杀合适的敌人时。\n");
                    // TODO: Mention bodyguards
                    break;
                case ArmadaUpgradeType.UnlockNewShip:
                    buffer.Add(this.RelatedShip.Description).SkipLine();
                    buffer.Add("此升级将允许建造 ").Add( this.CapIncrease, "ffa1a1" ).Add(" 个额外的 ").Add(this.ShipForCapIncrease.GetDisplayName() ).Add(" 在任何据点。该据点的旗舰随后将能在任何舰队船坞建造 ").Add(this.RelatedShip.GetDisplayName(), "a1ffa1").Add("。\n");
                    break;
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine("Can't display summary of ArmadaUpgrade " + this.InternalName + " of " + this.Type + " in options.", Verbosity.ShowAsError );
                    break;
            }
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ArmadaUpgrade() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArmadaUpgrades" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<ArmadaUpgrade> Pool = new ConcurrentPool<ArmadaUpgrade>( "ArmadaUpgrades", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ArmadaUpgrade(); } );

        public static ArmadaUpgrade GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<ArmadaUpgrade> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<ArmadaUpgrade>( new ArmadaUpgrade() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion

        #region IFleetTransformTarget
        string IFleetTransformTarget.DisplayName { get { return this.RelatedShip.DisplayName; } }
        string IFleetTransformTarget.InternalName { get { return this.InternalName; } }
        GameEntityTypeData IFleetTransformTarget.TypeData { get { return this.RelatedShip; } }
        int IFleetTransformTarget.HackingCost { get { return this.BlueprintTransformCostInHacking.IntValue; } }
        int IFleetTransformTarget.ResourceOneCost { get { return this.BlueprintTransformCostInResourceOne.IntValue; } }
        bool IFleetTransformTarget.CanTransformInto(Fleet fleet, out string InvalidReason)
        {
            GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
            if (centerpiece == null) {
                InvalidReason = "null centerpice!";
                return false;
            } else if (centerpiece.CurrentMarkLevel < this.MinFlagshipLevel) {
                InvalidReason = "旗舰等级太低。此物品需要等级为 " + this.MinFlagshipLevel + " 的旗舰";
                return false;
            } else if (centerpiece.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 500) {
                InvalidReason = "你无法在拥有大量敌方兵力的星球上进行变形。";
                return false;
            } else {
                InvalidReason = null;
                return true;
            }
        }
        #endregion
    }
    public class ArmadaUpgradeTable : ArcenDynamicTable<ArmadaUpgrade>
    {
        public static ArmadaUpgradeTable Instance;
        private static int counter;
        public override ArmadaUpgrade GetNewRowFromPool()
        {
            return ArmadaUpgrade.GetFromPoolOrCreate();
        }

        public readonly List<ArmadaUpgrade> AvailableStartingBonusSkeleton = List<ArmadaUpgrade>.Create_WillNeverBeGCed( 600, "ArmadaUpgrade-AvailableStartingBonusSkeleton" );
        public readonly List<ArmadaUpgrade> AvailableStartingBonusWights = List<ArmadaUpgrade>.Create_WillNeverBeGCed( 600, "ArmadaUpgrade-AvailableStartingBonusWights" );
        public readonly List<ArmadaUpgrade> AvailableStartingBonusUtility = List<ArmadaUpgrade>.Create_WillNeverBeGCed( 600, "ArmadaUpgrade-AvailableStartingBonusUtility" );
        public readonly DictionaryOfLists<string, ArcenDynamicTableRow> RowsByTag = DictionaryOfLists<string, ArcenDynamicTableRow>.Create_WillNeverBeGCed( 400, 200, "GETypeData-BaseRowsByTag" );

        public ArmadaUpgradeTable() : base( "ArmadaUpgrade", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow,
            //apparently this also needs to be in order
            ReadXml.InOrderOnMainThread )
        {
            Instance = this;
            counter = 0;
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced

            AvailableStartingBonusSkeleton.Clear();
            AvailableStartingBonusWights.Clear();
            AvailableStartingBonusUtility.Clear();
            RowsByTag.Clear();
            counter = 0;
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, ArmadaUpgrade TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "Index", ref TypeDataObject.Index, false );
            Data.Fill( "ShipTypeNameForCapIncrease", ref TypeDataObject.ShipTypeNameForCapIncrease, false );
            Data.Fill( "RelatedShipTypeName", ref TypeDataObject.RelatedShipTypeName, false );
            Data.Fill( "CapIncrease", ref TypeDataObject.CapIncrease, false );
            Data.FillEnum( "Type", ref TypeDataObject.Type, false );
            Data.Fill( "PrereqUpgradeIndex1", ref TypeDataObject.PrereqUpgradeIndex1, false );
            Data.Fill( "PrereqUpgradeIndex2", ref TypeDataObject.PrereqUpgradeIndex2, false );
            Data.FillEnum( "Target", ref TypeDataObject.Target, false );
            Data.Fill( "ReplaceIfAlreadyUpgraded", ref TypeDataObject.ReplaceIfAlreadyUpgraded, false );
            Data.Fill( "AlwaysAvailable", ref TypeDataObject.AlwaysAvailable, false );
            Data.Fill( "ForInitialRift", ref TypeDataObject.ForInitialRift, false );
            Data.Fill( "ForSecondRift", ref TypeDataObject.ForSecondRift, false );
            Data.Fill( "MinFlagshipLevel", ref TypeDataObject.MinFlagshipLevel, false );
            Data.Fill( "CanBeBonusStartingSkeleton", ref TypeDataObject.CanBeBonusStartingSkeleton, false );
            Data.Fill( "CanBeBonusStartingWight", ref TypeDataObject.CanBeBonusStartingWight, false );
            Data.Fill( "RelatedResource_Normal", ref TypeDataObject.RelatedResource_Normal, false );
            Data.Fill( "RelatedResource_Scarce", ref TypeDataObject.RelatedResource_Scarce, false );
            Data.Fill( "AdditionalAIPCost", ref TypeDataObject.AdditionalAIPCost, false );
            Data.Fill( "ShouldNotAppearInRift", ref TypeDataObject.ShouldNotAppearInRift, false );
            Data.Fill( "UtilityUpgrade", ref TypeDataObject.UtilityUpgrade, false );
            Data.Fill( "MustHaveAnyStrongholdAtThisLevel", ref TypeDataObject.MustHaveAnyStrongholdAtThisLevel, false );
            Data.Fill( "BlueprintTransformCostInHacking", ref TypeDataObject.BlueprintTransformCostInHacking, false );
            // We don't have BlueprintTransformCostInResourceOne here, since it is
            // currently forced to be the same as specified for the TransformArmadaFlagship
            // since the hacking UI don't allow customizing that by type.
            Data.FillList( "tags", TypeDataObject.TagsList, false, IfPresent.ReplaceExistingList, Uniqueness.Required, ClearBeforeReading.IfNotPartialRecord );

            if ( TypeDataObject.Type == ArmadaUpgradeType.ClaimBlueprints ) {
                if ( TypeDataObject.BlueprintTransformCostInHacking == FInt.Zero) {
                    FInt hackingCost = (FInt)20;
                    int markLevel = TypeDataObject.MinFlagshipLevel;
                    for ( int i = 2; i <= markLevel; i++ ) {
                        hackingCost *= FInt.FromParts(1, 250);
                    }
                    TypeDataObject.BlueprintTransformCostInHacking = hackingCost;
                }
            }

            return DelReturn.Continue;
        }

        public ArmadaUpgrade GetRowByIndex( int index )
        {
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                ArmadaUpgrade upgrade = this.Rows[i];
                if ( upgrade.Index == index )
                    return upgrade;
            }
            throw new Exception( "Could not find ArmadaUpgrade with index " + index );
        }
        private readonly Dictionary<int, bool> validator = Dictionary<int, bool>.Create_WillNeverBeGCed( 400, "ArmadaUpgrade-validator" );
        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            validator.Clear();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                ArmadaUpgrade row = this.Rows[i];
                if ( validator[row.Index] == true )
                    throw new Exception( "Armada Upgrade Row " + i + " " + row.InternalName + " index " + row.Index + " has a duplicate index " );
                validator[row.Index] = true;
            }
        }
        public override void DoPostInitializationAndSortingLogic_BackgroundThreads()
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( "Armada DoPostInitializationAndSortingLogic_BackgroundThreads RowCount: " + this.Rows.Count, Verbosity.DoNotShow );
            if ( this.Rows.Count == 0 )
                return;

            FInt BlueprintTransformCostInResourceOne = HackingTypeTable.Instance.GetRowByName("TransformArmadaFlagship").BaseCostInResourceOne;

            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                ArmadaUpgrade row = this.Rows[i];
                if ( !String.IsNullOrEmpty( row.ShipTypeNameForCapIncrease ) )
                    row.ShipForCapIncrease = GameEntityTypeDataTable.Instance.GetRowByName( row.ShipTypeNameForCapIncrease );
                if ( !String.IsNullOrEmpty( row.RelatedShipTypeName ) )
                    row.RelatedShip = GameEntityTypeDataTable.Instance.GetRowByName( row.RelatedShipTypeName );
                else
                    row.RelatedShip = null;
                if ( row.CanBeBonusStartingSkeleton )
                {
                    AvailableStartingBonusSkeleton.Add( row );
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Armada CanBeBonusStartingSkeleton: " + row.InternalName + " " + AvailableStartingBonusSkeleton.Count, Verbosity.DoNotShow );
                }
                if ( row.CanBeBonusStartingWight )
                    AvailableStartingBonusWights.Add( row );
                if ( row.UtilityUpgrade )
                    AvailableStartingBonusUtility.Add( row );

                for ( int j = 0; j < row.TagsList.Count; j++ )
                {
                    string tag = row.TagsList[j];
                    List<ArcenDynamicTableRow> list2 = RowsByTag[tag]; //this is us doing BaseRowsByTag.Add, basically
                    if ( !list2.Contains( row ) )
                        list2.Add( row );
                }

                if ( row.Type == ArmadaUpgradeType.ClaimBlueprints ) {
                    row.BlueprintTransformCostInResourceOne = BlueprintTransformCostInResourceOne;
                }
            }

            foreach ( KeyValuePair<string,List<ArcenDynamicTableRow>> kv in RowsByTag )
            {
                ArcenDynamicTableAggregator.CoreTableSubsetsByName["NTag_" + kv.Key].ClearAndAddRange( kv.Value );
            }

        }
        public ArmadaUpgrade GetRandomBonusStartingSkeletonType( ArcenHostOnlySimContext Context )
        {
            if ( AvailableStartingBonusSkeleton.Count == 0 )
                throw new Exception( "No available starting bonus skeletons" );
            return AvailableStartingBonusSkeleton[Context.RandomToUse.Next( 0, AvailableStartingBonusSkeleton.Count )];
        }
        public ArmadaUpgrade GetRandomBonusStartingWightType( ArcenHostOnlySimContext Context )
        {
            if ( AvailableStartingBonusWights.Count == 0 )
                throw new Exception( "No available starting bonus wights" );
            return AvailableStartingBonusWights[Context.RandomToUse.Next( 0, AvailableStartingBonusWights.Count )];
        }
        public ArmadaUpgrade GetRandomBonusStartingUtilityType( ArcenHostOnlySimContext Context )
        {
            if ( AvailableStartingBonusWights.Count == 0 )
                throw new Exception( "No available starting bonus utility" );
            return AvailableStartingBonusUtility[Context.RandomToUse.Next( 0, AvailableStartingBonusUtility.Count )];
        }


        public static DrawBag<ArmadaUpgrade> WorkingOptions = DrawBag<ArmadaUpgrade>.Create_WillNeverBeGCed( 20, "ArmadaUpgrade-WorkingOptions" );
        public static DrawBag<ArmadaUpgrade> WorkingSkelOptions = DrawBag<ArmadaUpgrade>.Create_WillNeverBeGCed( 3, "ArmadaUpgrade-WorkingSkelOptions" ); //only for the first rift
        public static DrawBag<ArmadaUpgrade> WorkingWightOptions = DrawBag<ArmadaUpgrade>.Create_WillNeverBeGCed( 3, "ArmadaUpgrade-WorkingWightOptions" ); //only for the first rift
        public void GetUpgradesForRift_HostOnly( List<ArmadaUpgrade> ListToFill, ArcenHostOnlySimContext Context, List<ArmadaUpgrade> completedUpgradesOrNull, List<ArmadaUpgrade> upgradesToAvoidOrNull, int numOptionsToAdd, int riftsPopulated, int highestStrongholdMark, int highestFlagshipMark )
        {
            //this is called by the Templar code to populate a rift's available hacks
            WorkingOptions.Clear();
            WorkingSkelOptions.Clear();
            WorkingWightOptions.Clear();
            ListToFill.Clear();
            //ArcenDebugging.ArcenDebugLogSingleLine("getting some upgrades! we currently have " + this.Rows.Count + " rows.", Verbosity.DoNotShow );
            int upgradesPlayerCantGet = 0;
            if ( numOptionsToAdd == 3 )
                upgradesPlayerCantGet = 1;
            else if ( numOptionsToAdd > 3 )
                upgradesPlayerCantGet = 2;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                ArmadaUpgrade row = this.Rows[i];
                if ( riftsPopulated == 0 )
                {
                    if ( row.ForInitialRift )
                        ListToFill.Add( row );
                    if ( row.GetHasTag("EligibleForFirstRift") )
                    {
                        if ( row.Type == ArmadaUpgradeType.UnlockSkeletonType )
                            WorkingSkelOptions.AddItem( row, 1 );
                        else
                            WorkingWightOptions.AddItem( row, 1 );
                    }
                    continue;
                }
                if ( row.AlwaysAvailable )
                {
                    ListToFill.Add( row );
                    continue;
                }
                if ( riftsPopulated == 1 && row.ForSecondRift )
                {
                    //If the player hasn't already unlocked this,
                    //make sure its available in the second rift. This is for things like Banshees and Igors
                    bool alreadyUnlocked = false;
                    if ( completedUpgradesOrNull != null )
                    {
                        for ( int j = 0; j < completedUpgradesOrNull.Count; j++ )
                        {
                            if ( completedUpgradesOrNull[j] == row )
                            {
                                alreadyUnlocked = true;
                                break;
                            }
                        }
                    }
                    if ( !alreadyUnlocked )
                        ListToFill.Add( row );
                    continue;
                }

                if ( row.ShouldNotAppearInRift )
                    continue;
                if ( upgradesToAvoidOrNull != null &&
                     upgradesToAvoidOrNull.Contains( row ) )
                    continue;
                if ( row.ReplaceIfAlreadyUpgraded &&
                     completedUpgradesOrNull != null &&
                     completedUpgradesOrNull.Contains( row ) )
                    continue;
                if ( !HasUpgradedPrereqsFor( row, completedUpgradesOrNull ) )
                    continue;
                WorkingOptions.AddItem( row, 1 );
            }
            //ArcenDebugging.ArcenDebugLogSingleLine("we have " + WorkingOptions.InternalListSize + " working options", Verbosity.DoNotShow );
            if ( riftsPopulated == 0 && completedUpgradesOrNull != null )
            {
                //this is the first rift, and it has some unique contents. It gets the options with the ForInitialRift setting,
                //and the EligibleForFirstRift tag. The "eligible for first rift tag" is applied to the skeleton/wight low-tier variants,
                //and you will get an option that you don't have already

                //Note that if the player gives themselves bonus upgrades early (say 2 wights, or "unlock all") then the "don't give players what they have already unlocked"
                //option isn't going to work and that's okay. The goal is just to make sure players don't get screwed early with unfun options.
                ArmadaUpgrade currentSkeletonUpgrade = null;
                ArmadaUpgrade currentWightUpgrade = null;
                for ( int i = 0; i < completedUpgradesOrNull.Count; i++ )
                {
                    if ( completedUpgradesOrNull[i].Type == ArmadaUpgradeType.UnlockWightType )
                        currentWightUpgrade = completedUpgradesOrNull[i];
                    if ( completedUpgradesOrNull[i].Type == ArmadaUpgradeType.UnlockSkeletonType )
                        currentSkeletonUpgrade = completedUpgradesOrNull[i];
                }
                do{
                    ArmadaUpgrade upgrade = WorkingSkelOptions.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                    if ( upgrade != currentSkeletonUpgrade )
                    {
                        ListToFill.Add( upgrade );
                        break;
                    }
                } while ( WorkingSkelOptions.InternalListSize > 0 );

                do{
                    ArmadaUpgrade upgrade = WorkingWightOptions.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                    if ( upgrade != currentWightUpgrade )
                    {
                        ListToFill.Add( upgrade );
                        break;
                    }
                } while ( WorkingWightOptions.InternalListSize > 0 );

            }
            while ( WorkingOptions.InternalListSize > 0 && ListToFill.Count < 5 )
            {
                //It's frustrating if a rift has a bunch of upgrades the player can't get yet;
                //this happens if they don't have Flagships or Strongholds at a high enough mark level
                //We try to make sure not to add too many things the player can't afford
                ArmadaUpgrade upgrade = WorkingOptions.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                bool canGetThis = true;
                if ( upgrade.MinFlagshipLevel > highestFlagshipMark ||
                     upgrade.MustHaveAnyStrongholdAtThisLevel > highestStrongholdMark )
                    canGetThis = false;
                if ( !canGetThis )
                {
                    if ( upgradesPlayerCantGet <= 0 )
                        continue;
                    else
                        upgradesPlayerCantGet--;
                }

                ListToFill.Add( upgrade );
            }
        }
        public bool HasUpgradedPrereqsFor( ArmadaUpgrade upgrade, List<ArmadaUpgrade> completedUpgradesOrNull )
        {
            if ( upgrade.PrereqUpgradeIndex1 <= 0 && upgrade.PrereqUpgradeIndex1 <= 0 )
                return true; //no prereqs for this!
            if ( completedUpgradesOrNull == null || completedUpgradesOrNull.Count == 0 )
                return true;
            if ( upgrade.PrereqUpgradeIndex1 > 0 )
            {
                bool foundPrereq = false;
                for ( int i = 0; i < completedUpgradesOrNull.Count; i++ )
                {
                    if ( completedUpgradesOrNull[i].Index == upgrade.PrereqUpgradeIndex1 )
                    {
                        foundPrereq = true;
                        break;
                    }
                }
                if ( !foundPrereq )
                    return false;
            }
            if ( upgrade.PrereqUpgradeIndex2 > 0 )
            {
                bool foundPrereq = false;
                for ( int i = 0; i < completedUpgradesOrNull.Count; i++ )
                {
                    if ( completedUpgradesOrNull[i].Index == upgrade.PrereqUpgradeIndex2 )
                    {
                        foundPrereq = true;
                        break;
                    }
                }
                if ( !foundPrereq )
                    return false;
            }
            return true;
        }
        public ArmadaUpgrade GetNextFactionUpgrade( )
        {
            ArmadaUpgrade output = null;
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("Getting next faction upgrade (starting with counter " + counter +")", Verbosity.DoNotShow );
            while ( counter < this.Rows.Count )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Checking " + this.Rows[counter].ToString() + " with target " + this.Rows[counter].Target, Verbosity.DoNotShow );
                if ( this.Rows[counter].Target == ArmadaUpgradeTarget.Faction )
                {
                    output = this.Rows[counter];
                }
                counter++;
                if ( output != null )
                    break;
            }
            return output;
        }

    }

    public class ArmadaUpgradeEvent : IBetweenMapGenPoolable<ArmadaUpgradeEvent>
    {
        //Anything set in here should be cleared in WipeForReuseAsNewObject()

        //Data about the hack itself
        //The constructor fills in this information
        public int UpgradeTime;
        public Int16 PlanetIndexOrNull;
        public GameEntityTypeData RelatedEntityTypeData;
        public int RelatedUpgradeIdx;
        public int ArmadaFactionIdx;
        public int RelatedFactionIdx;
        public int RelatedFleetId;

        //Data about the outcome of the hack; this is updated by the external hacking code
        public FInt HackingPointsSpent;
        public FInt HackingPointsLeftAfterHack;

        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "ArmadaUpgradeEvents" );
        public ArmadaUpgrade Upgrade; //this is the upgrade from the RelatedUpgradeIdx

        private ArmadaUpgradeEvent()
        {
            if ( RefTracker != null ) //it will be null for the two above in the static definitions
                RefTracker.IncrementObjectCount();
        }

        #region Pooling
        private static readonly BetweenMapGenPool<ArmadaUpgradeEvent> Pool = BetweenMapGenPool<ArmadaUpgradeEvent>.Create_WillNeverBeGCed( "ArmadaUpgradeEvent", 30, 30,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ArmadaUpgradeEvent(); } );

        public void WipeForReuseAsNewObject()
        {
            this.UpgradeTime = 0;
            this.RelatedEntityTypeData = null;
            this.PlanetIndexOrNull = -1;
            this.RelatedUpgradeIdx = -1;
            this.RelatedFactionIdx = -1;
            this.RelatedFleetId = -1;
            this.ArmadaFactionIdx = -1;
            this.HackingPointsLeftAfterHack = FInt.Zero;
            this.HackingPointsSpent = FInt.Zero;
            this.Upgrade = null;
        }
        #endregion

        public static ArmadaUpgradeEvent Create( Int16 ArmadaFactionIndex, Int16 RelatedFactionIndex, Int16 PlanetIndexOrNull, int RelatedUpgradeIndex, int RelatedFleetIndex, GameEntityTypeData RelatedEntityTypeOrNull )
        {
            ArmadaUpgradeEvent result = Pool.GetFromPoolOrCreate();
            result.UpgradeTime = World_AIW2.Instance.GameSecond;
            result.ArmadaFactionIdx = ArmadaFactionIndex;
            result.RelatedFactionIdx = RelatedFactionIndex;
            result.PlanetIndexOrNull = PlanetIndexOrNull;
            result.RelatedUpgradeIdx = RelatedUpgradeIndex;
            result.RelatedFleetId = RelatedFleetIndex;
            result.HackingPointsSpent = FInt.Zero;
            result.HackingPointsLeftAfterHack = FInt.Zero;
            result.RelatedEntityTypeData = RelatedEntityTypeOrNull;
            return result;
        }
        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteCanary( "h1", "ArmadaUpgradeEventCanary1", SerializationCmdType );

            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.UpgradeTime, "UpgradeTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.ArmadaFactionIdx, "ArmadaFactionIdx" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.RelatedFactionIdx, "RelatedFactionIdx" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.PlanetIndexOrNull, "PlanetIndexOrNull" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.RelatedFleetId, "RelatedFleetId" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.RelatedUpgradeIdx, "RelatedUpgradeIdx" );
            GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, this.RelatedEntityTypeData, Buffer, "RelatedEntityTypeData" );

            Buffer.AddFInt( MetaData, this.HackingPointsSpent, "HackingPointsSpent" );
            Buffer.AddFInt( MetaData, this.HackingPointsLeftAfterHack, "HackingPointsLeftAfterHack" );

            Buffer.WriteCanary( "h2", "ArmadaUpgradeEventCanary2", SerializationCmdType );

            Buffer.WriteCanary( "h3", "ArmadaUpgradeEventCanary3", SerializationCmdType );
        }

        public static ArmadaUpgradeEvent GetFromPoolOrCreate()
        {
            ArmadaUpgradeEvent result = Pool.GetFromPoolOrCreate();
            return result;
        }

        public void DeserializedIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.ValidateCanary( "h1", "ArmadaUpgradeEventCanary1", SerializationCmdType );

            this.UpgradeTime = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "UpgradeTime" );
            this.ArmadaFactionIdx = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "ArmadaFactionIdx" );
            this.RelatedFactionIdx = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RelatedFactionIdx" );
            this.PlanetIndexOrNull = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PlanetIndexOrNull" );
            this.RelatedFleetId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RelatedFleetId" );
            this.RelatedUpgradeIdx = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RelatedUpgradeIdx" );
            this.RelatedEntityTypeData = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "RelatedEntityTypeData" );

            this.HackingPointsSpent = Buffer.ReadFInt( MetaData, "HackingPointsSpent" );
            this.HackingPointsLeftAfterHack = Buffer.ReadFInt( MetaData, "HackingPointsLeftAfterHack" );

            Buffer.ValidateCanary( "h2", "ArmadaUpgradeEventCanary2", SerializationCmdType );

            Buffer.ValidateCanary( "h3", "ArmadaUpgradeEventCanary3", SerializationCmdType );
        }

        public void ToDebugString(ArcenCharacterBufferBase buffer)
        {
            int debugCode = 0;
            try
            {
                debugCode = 1000;
                Faction myFaction = World_AIW2.Instance.GetFactionByIndex( this.ArmadaFactionIdx );
                Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( this.RelatedFactionIdx );
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( this.PlanetIndexOrNull );
                if ( targetFaction == null )
                    targetFaction = World_AIW2.Instance.GetNeutralFaction(); //to avoid any null refs
                if ( this.UpgradeTime <= 1 )
                    buffer.Add( "Initial", "ffa1a1" );
                else
                    buffer.Add( Engine_Universal.ToHoursAndMinutesString( this.UpgradeTime ), "ffa1a1" );
                buffer.Add( ": Upgrade " );
                if ( this.Upgrade == null )
                    this.Upgrade = ArmadaUpgradeTable.Instance.GetRowByIndex( this.RelatedUpgradeIdx );

                buffer.Add( this.Upgrade.DisplayName, "a1ffa1" );
                if ( this.UpgradeTime > 1 )
                    buffer.Add( " on " ).Add( planet.Name, "a1a1ff" ).Add( " against " ).Add( targetFaction.GetDisplayName(), targetFaction.FactionCenterColor.ColorHexBrighter ).Add( "." );
                else
                    buffer.Add( "." );
                if ( this.HackingPointsSpent > 0 )
                    buffer.Add( "\n\tHacking points spent: " ).Add( this.HackingPointsSpent.IntValue, "a1a1ff" ).Add( " and points remaining afterwards: " ).Add( this.HackingPointsLeftAfterHack.IntValue, "a1ffff" );
                if ( this.RelatedFleetId >= 0 )
                {
                    Fleet fleet = World_AIW2.Instance.GetFleetByID( this.RelatedFleetId );
                    if ( fleet != null )
                        buffer.Add( "\n\tFor fleet: " ).Add( fleet.GetName(), "a1a1ff" ).Add( "." );
                }
                buffer.Add( "\n" );
                debugCode = 1500;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ArmadaUpgradeEvent::ToDebugString debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
}
