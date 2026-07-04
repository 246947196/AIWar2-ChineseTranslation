using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DZUpgrade : ArcenDynamicTableRow, IConcurrentPoolable<DZUpgrade>, IProtectedListable
    {
        //The DZ is capable of upgrading itself in a variety of ways
        //Note I don't think these are all implemented
        public bool IncreaseResourceProductionByPercentage;
        public bool GrantEpistylesPermanentResourceIncome;
        public bool IncreaseEpistylePerPlanetLimit;
        public bool IncreaseTerminusPerPlanetLimit;
        public bool IncreaseUtilityPerPlanetLimit;
        public bool IncreaseDamageAgainstScariestFaction;
        public bool DecreaseDamageFromScariestFaction;
        public bool SlowEnemiesWhenEnteringPlanet;
        public bool UnlockShipTier; //paired with a RelatedInteger
        public bool UnlockShipVariant; //paired with a Resource
        public bool AlwaysUnlocked;
        public bool UnlockMarkLevel;

        public DZResource RelatedResource;
        public int RelatedInteger1;
        public int RelatedInteger2;
        public int PrereqUpgrade1 = -1;
        public int PrereqUpgrade2 = -1;
        public int RequiredVariantUpgrades = -1;
        public int RequiredIntensity = -1;
        public Int16 RelatedFactionIndex;

        public GameEntityTypeData RelatedTypeData;

        //For the incremental upgrade mode for the DZ full invasion
        public int UnlocksIfInvasionAfter = -1;

        //Fancier upgrade options
        public bool SingleUpgradeOnly; //ie we can only upgrade this once
        public bool UnusualUpgrade; //some upgrades can't be built normally, and apply based on environmental triggers
        public FInt EnemyPowerLevelToTriggerOn; //an example environmental trigger

        public Int16 UpgradeIndex;//XML index. Must be > 0

        public bool PlayerEpistyleLimitOverride;

        public void ToBuffer( ref ArcenCharacterBufferBase Buffer, bool showWhetherApplied )
        {
            if ( this.IncreaseResourceProductionByPercentage )
                Buffer.Add( "Increase Resource Production of " ).Add( this.RelatedResource.ToString(), DarkZenithFactionBaseInfo.ResourceColour[this.RelatedResource] ).Add( " by " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );
            if ( this.GrantEpistylesPermanentResourceIncome )
                Buffer.Add( "Grants Epistyles Permanent additional Income of  " ).Add( this.RelatedInteger1.ToString(), DarkZenithFactionBaseInfo.ResourceColour[this.RelatedResource] ).Add( "\n" );
            if ( this.IncreaseEpistylePerPlanetLimit || this.PlayerEpistyleLimitOverride )
                Buffer.Add( "Increases Epistyle per planet limit by " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );
            if ( this.IncreaseTerminusPerPlanetLimit && !this.PlayerEpistyleLimitOverride )
                Buffer.Add( "Increases Terminus per planet limit by " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );
            if ( this.IncreaseUtilityPerPlanetLimit )
                Buffer.Add( "Increases Utility per planet limit by " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );

            if ( this.IncreaseDamageAgainstScariestFaction )
            {
                Faction faction = World_AIW2.Instance.GetFactionByIndex( this.RelatedFactionIndex );
                if ( faction == null )
                    Buffer.Add( "Increases damage against UNKNOWN FACTION. Is bug\n" );
                else
                    Buffer.Add( "Increases damage against " ).Add( faction.GetDisplayName(), faction.FactionCenterColor.ColorHexBrighter ).Add( " by " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );
            }
            if ( this.DecreaseDamageFromScariestFaction )
            {
                Faction faction = World_AIW2.Instance.GetFactionByIndex( this.RelatedFactionIndex );
                if ( faction == null )
                    Buffer.Add( "Decrease damage from UNKNOWN FACTION. Is bug\n" );
                else
                    Buffer.Add( "Decrease damage from " ).Add( faction.GetDisplayName(), faction.FactionCenterColor.ColorHexBrighter ).Add( " by " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );
            }
            if ( this.SlowEnemiesWhenEnteringPlanet )
            {
                Buffer.Add( "Slows enemies entering DZ planets by " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( " for " ).Add( this.RelatedInteger2 ).Add( " seconds." ).Add( "\n" );
            }

            if ( this.UnlockShipTier )
            {
                Buffer.Add( "\tUnlock Ship Tier " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );
            }
            if ( this.UnlockShipVariant )
            {
                Buffer.Add( "\tUnlock " ).Add( DarkZenithFactionBaseInfoRoot.GetShipVariantTypeFromResource( this.RelatedResource ), DarkZenithFactionBaseInfo.ResourceColour[this.RelatedResource] ).Add( " for Tier " ).Add( this.RelatedInteger1, "a1ffa1" ).Add( "\n" );
            }
            if ( this.UnlockMarkLevel )
            {
                Balance_MarkLevel mark = Balance_MarkLevelTable.Instance.RowsByOrdinal[this.RelatedInteger1];

                Buffer.Add( "\tUnlock Mark Level " ).Add( this.RelatedInteger1.ToString(), mark.ColorHex ).Add( "\n" );
            }
        }
        public string GetTagForUpgrade()
        {
            //this is used for the ship tier and variant upgrade
            string numStr = "";
            string tag = "DZTier";
            string variant = "Base";
            if ( this.RelatedInteger1 == 0 )
                numStr = "Zero";
            if ( this.RelatedInteger1 == 1 )
                numStr = "One";
            if ( this.RelatedInteger1 == 2 )
                numStr = "Two";
            if ( this.RelatedInteger1 == 3 )
                numStr = "Three";
            if ( this.UnlockShipVariant )
                variant = DarkZenithFactionBaseInfoRoot.GetShipVariantTypeFromResource( this.RelatedResource );
            return tag + numStr + variant;
        }


        #region Pooling
        private static ReferenceTracker RefTracker;
        private DZUpgrade() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "DZUpgrades" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<DZUpgrade> Pool = new ConcurrentPool<DZUpgrade>( "DZUpgrades", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DZUpgrade(); } );

        public static DZUpgrade GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<DZUpgrade> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<DZUpgrade>( DZUpgrade.GetFromPoolOrCreate() );
            typeAnalyzer.ApplyDefaults( this );
            this.EnemyPowerLevelToTriggerOn = FInt.Zero;
        }
        #endregion
    }

    public class DarkZenithUpgradeTable : ArcenDynamicTable<DZUpgrade>
    {
        public static DarkZenithUpgradeTable Instance;
        public readonly List<DZUpgrade> UnusualUpgrades = List<DZUpgrade>.Create_WillNeverBeGCed( 60, "DarkZenithUpgradeTable-UnusualUpgrades" );
        public DarkZenithUpgradeTable() : base( "DarkZenithUpgrades", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow,
            //apparently this also needs to be in order
            ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override DZUpgrade GetNewRowFromPool()
        {
            return DZUpgrade.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, DZUpgrade TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "GrantEpistylesPermanentResourceIncome", ref TypeDataObject.GrantEpistylesPermanentResourceIncome, false );
            Data.Fill( "IncreaseEpistylePerPlanetLimit", ref TypeDataObject.IncreaseEpistylePerPlanetLimit, false );
            Data.Fill( "IncreaseTerminusPerPlanetLimit", ref TypeDataObject.IncreaseTerminusPerPlanetLimit, false );
            Data.Fill( "IncreaseUtilityPerPlanetLimit", ref TypeDataObject.IncreaseUtilityPerPlanetLimit, false );
            string tempString = "";
            Data.Fill( "RelatedResource", ref tempString, false );
            Data.Fill( "RelatedInteger1", ref TypeDataObject.RelatedInteger1, false );
            Data.Fill( "RelatedInteger2", ref TypeDataObject.RelatedInteger2, false );
            Data.Fill( "PrereqUpgrade1", ref TypeDataObject.PrereqUpgrade1, false );
            Data.Fill( "PrereqUpgrade2", ref TypeDataObject.PrereqUpgrade2, false );
            Data.Fill( "RequiredVariantUpgrades", ref TypeDataObject.RequiredVariantUpgrades, false );
            Data.Fill( "RequiredIntensity", ref TypeDataObject.RequiredIntensity, false );
            Data.Fill( "UnlocksIfInvasionAfter", ref TypeDataObject.UnlocksIfInvasionAfter, false );

            Data.Fill( "UnlockShipTier", ref TypeDataObject.UnlockShipTier, false );
            Data.Fill( "UnlockShipVariant", ref TypeDataObject.UnlockShipVariant, false );
            Data.Fill( "AlwaysUnlocked", ref TypeDataObject.AlwaysUnlocked, false );
            Data.Fill( "UnlockMarkLevel", ref TypeDataObject.UnlockMarkLevel, false );

            Data.Fill( "UpgradeIndex", ref TypeDataObject.UpgradeIndex, !Data.ReadingPartialRecord );
            Data.Fill( "SingleUpgradeOnly", ref TypeDataObject.SingleUpgradeOnly, false );
            Data.Fill( "UnusualUpgrade", ref TypeDataObject.UnusualUpgrade, false );
            Data.Fill( "EnemyPowerLevelToTriggerOn", ref TypeDataObject.EnemyPowerLevelToTriggerOn, false );
            Data.Fill( "PlayerEpistyleLimitOverride", ref TypeDataObject.PlayerEpistyleLimitOverride, false );

            TypeDataObject.RelatedResource = DarkZenithFactionBaseInfoRoot.GetDZResourceFromString( tempString );

            if ( TypeDataObject.GrantEpistylesPermanentResourceIncome &&
                 (TypeDataObject.RelatedInteger1 <= 0 || TypeDataObject.RelatedResource == DZResource.None) )
                throw new Exception( "Problem parsing " + TypeDataObject.InternalName + ", related integer " + TypeDataObject.RelatedInteger1 + " resource " + TypeDataObject.RelatedResource );
            if ( (TypeDataObject.IncreaseTerminusPerPlanetLimit || TypeDataObject.IncreaseEpistylePerPlanetLimit || TypeDataObject.IncreaseUtilityPerPlanetLimit) &&
                 TypeDataObject.RelatedInteger1 <= 0 )
                throw new Exception( "Problem parsing " + TypeDataObject.InternalName + ", related integer " + TypeDataObject.RelatedInteger1 );

            return DelReturn.Continue;
        }

        // public void AddStartingUpgrades( List<DZUpgrade> Upgrades, Faction faction )
        // {
        //     This is now done in the faction DeepInfo code, HandleUpgrades()
        //     int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
        //     //Invading DZs start with all tiers and variants upgraded. DZs that join their allies do not
        //     throw new Exception("This code isn't ever called");
        //     for ( int i = 0; i < this.Rows.Count; i++ )
        //     {
        //         DZUpgrade upgrade = this.Rows[i];
        //         if ( upgrade.AlwaysUnlocked )
        //         {
        //             Upgrades.Add( upgrade );
        //         }
        //         if ( intensity < upgrade.RequiredIntensity )
        //             continue;

        //         if ( faction.SpecialFactionData.FullInvasionMode &&
        //              (upgrade.UnlockShipVariant || upgrade.UnlockShipTier) )
        //         {
        //             Upgrades.Add( upgrade );
        //         }
        //     }
        // }
        public DZUpgrade GetRowById( int id )
        {
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZUpgrade upgrade = this.Rows[i];
                if ( upgrade.UpgradeIndex == id )
                    return upgrade;
            }
            throw new Exception( "Could not find DZUpgrade with index " + id );
        }

        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            Dictionary<int, bool> validator = Mat.GetTemporaryIntBoolDict( "DarkZenithUpgradeTable-DoPostInitializationPreSortingLogic_BackgroundThreads-validator", 10f );
            if ( validator == null ) //blocked for teardown/shutdown; bail
                return;

            UnusualUpgrades.Clear(); //just in case
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZUpgrade row = this.Rows[i];
                if ( row.UnusualUpgrade )
                    UnusualUpgrades.Add( row );
                if ( validator[row.UpgradeIndex] == true )
                {
                    Mat.ReleaseTemporaryIntBoolDict( validator );
                    throw new Exception( "Row " + i + " " + row.InternalName + " index " + row.UpgradeIndex + " has a duplicate index " );
                }
                validator[row.UpgradeIndex] = true;
            }

            Mat.ReleaseTemporaryIntBoolDict( validator );
        }
    }
}
