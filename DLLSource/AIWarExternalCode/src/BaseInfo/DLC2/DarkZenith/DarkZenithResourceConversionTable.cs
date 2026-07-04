using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public enum DZResource : byte
    {
        None = 0,
        Metal, //metal is the "base" resource
        Green,
        White,
        Blue,
        Black,
        Red,
        End
    }

    public class DZResourceConversion : ArcenDynamicTableRow, IConcurrentPoolable<DZResourceConversion>, IProtectedListable
    {
        //This is used for converting resources into either new resources (Terminii), units (Epistyles) or upgrades (Epistyles)
        //Input must always be set, and then at least one of OutputResource, Unit or Upgrade. In principle you could do both at once

        //NOTA BENE: data tables from xml should not be serialized (their contents), as these define a SCHEMA, with appropriate schema data, and not actual in-game runtime data.
        //We'd never want to alter data tables (stuff that is ArcenDynamicTableRow) from any source other than the xml itself, or something that is just post-parsing.
        //This is not considered part of savegame or network data by convention everywhere in the codebase, and altering that could have very strange results...
        public readonly Dictionary<DZResource, int> Cost = Dictionary<DZResource, int>.Create_WillNeverBeGCed( (int)(DZResource.End) + 1, "DZResourceConversion-Cost" );
        //for terminii
        public readonly Dictionary<DZResource, int> OutputResource = Dictionary<DZResource, int>.Create_WillNeverBeGCed( (int)(DZResource.End) + 1, "DZResourceConversion-OutputResource" );

        //for an epistyle creating units or structures
        public string TagForUnit; //the tag and the name are available options for XML to say "what to build"
        public string NameForUnit;
        public GameEntityTypeData Unit;
        public byte MarkLevel; //can't be specified with TakeFactionMarkLevelMinusThis
        public int TakeFactionMarkLevelMinusThis; //so we can say "Use our current faction mark level" or "take current mark level - 3"
        public int NumberOfUnits;
        public bool PirateOnly;
        public bool IsOffensive;
        public bool IsUtility;
        public bool IsResourceCreation;
        public bool IsDefensive;
        public DZResource RelatedResource;

        //For generic DZ upgrades.
        //There's a boolean to say "What kind of change" and then some variables to give that
        //change specific values
        public DZUpgrade Upgrade;
        public int _UpgradeIndex;

        public Int16 ConversionIndex; //an index into the XML table for the original value. only for debugging

        public bool ForHumanAllied;
        public bool ForMinorFactionAllied;
        public bool ForAIAllied;
        public bool ForPlayer;
        public string Description;

        public bool HasDoneOnLoadActions;


        public void ToBuffer( ArcenCharacterBufferBase Buffer )
        {
            //Used for logging to the UI
            Buffer.Add( this.DisplayName, "a1ffa1" ).Add( "\n" );
            Buffer.Add( "\t" );
            FactionUtilityMethods.Instance.PrintDZDictionary( this.Cost, ref Buffer, "Cost" );
            Buffer.Add( "\n" );
            if ( this.Upgrade != null )
            {
                this.Upgrade.ToBuffer( ref Buffer, false );
                Buffer.Add( "\n" );
            }
            if ( this.HasAnyOutputResources() )
            {
                FactionUtilityMethods.Instance.PrintDZDictionary( this.OutputResource, ref Buffer, "Output Resources" );
            }
            if ( this.Unit != null )
            {
                Buffer.Add( "Will build " ).Add( this.Unit.GetDisplayName(), "ffa1a1" ).Add( "\n" );
            }
        }
        public void ToHackBuffer( ArcenCharacterBufferBase Buffer )
        {
            //Used by the Sidekick to display in the Hacking menu
            //to allow the player to pick which resource conversion to use
            Buffer.Add( this.DisplayName, "a1ffa1" ).Add( ": " );
            FactionUtilityMethods.Instance.PrintDZDictionary( this.Cost, ref Buffer, "Cost" );
        }
        public bool HasAnyOutputResources()
        {
            foreach ( KeyValuePair<DZResource, int> kv in this.OutputResource )
            {
                if ( kv.Value > 0 )
                    return true;
            }
            return false;
        }
        public void DoOnLoad()
        {
            if ( this.HasDoneOnLoadActions )
                return;
            this.HasDoneOnLoadActions = true;
            //I'd rather build anything with the tag then specify here
            // if ( this.Unit == null && !String.IsNullOrEmpty(this.TagForUnit) )
            //     this.Unit = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, this.TagForUnit );
            // if ( this.Unit == null && !String.IsNullOrEmpty(this.NameForUnit) )
            //     this.Unit = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, this.NameForUnit );

            if ( this._UpgradeIndex > 0 && this.Upgrade == null )
                this.Upgrade = DarkZenithUpgradeTable.Instance.GetRowById( this._UpgradeIndex );
            if ( (this.TagForUnit == "WarpingInDZTerminus" || this.NameForUnit == "WarpingInDZTerminus") &&
                this.RelatedResource == DZResource.None )
                throw new Exception( "Found bad conversion <" + this.ToString() + "> in DoOnLoad; it looks like the related resource is DZResource.None" );

            //TODO: iterate over Cost and OutpostResource to remove unset ones?
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private DZResourceConversion() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "DZResourceConversions" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<DZResourceConversion> Pool = new ConcurrentPool<DZResourceConversion>( "DZResourceConversions", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DZResourceConversion(); } );

        public static DZResourceConversion GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<DZResourceConversion> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<DZResourceConversion>( new DZResourceConversion() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class DarkZenithResourceConversionTable : ArcenDynamicTable<DZResourceConversion>
    {
        public static DarkZenithResourceConversionTable Instance;
        public DarkZenithResourceConversionTable() : base( "DarkZenithResourceConversion", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Index, ReloadDuringRuntime.Allow )
        {
            Instance = this;
        }

        public override DZResourceConversion GetNewRowFromPool()
        {
            return DZResourceConversion.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }
        public override DelReturn NodeProcessor( ArcenXMLElement Data, DZResourceConversion TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "index", ref TypeDataObject.ConversionIndex, !Data.ReadingPartialRecord );

            //Costs
            for ( DZResource res = DZResource.None + 1; res < DZResource.End; res++ )
                Data.FillDictionaryKey( "CostIn" + res, TypeDataObject.Cost, res, false );

            //Output Resource
            for ( DZResource res = DZResource.None + 1; res < DZResource.End; res++ )
                Data.FillDictionaryKey( res + "Output", TypeDataObject.OutputResource, res, false );

            //Build Unit
            Data.Fill( "UnitTagToBuild", ref TypeDataObject.TagForUnit, false );
            Data.Fill( "UnitNameToBuild", ref TypeDataObject.NameForUnit, false );
            Data.Fill( "MarkLevel", ref TypeDataObject.MarkLevel, false );
            Data.Fill( "TakeFactionMarkLevelMinusThis", ref TypeDataObject.TakeFactionMarkLevelMinusThis, false );
            Data.Fill( "NumberOfUnits", ref TypeDataObject.NumberOfUnits, false );
            Data.Fill( "PirateOnly", ref TypeDataObject.PirateOnly, false );
            Data.Fill( "IsOffensive", ref TypeDataObject.IsOffensive, false );
            Data.Fill( "IsResourceCreation", ref TypeDataObject.IsResourceCreation, false );
            Data.Fill( "IsDefensive", ref TypeDataObject.IsDefensive, false );
            Data.Fill( "IsUtility", ref TypeDataObject.IsUtility, false );
            string tempString = "";
            Data.Fill( "RelatedResource", ref tempString, false );
            Data.Fill( "ForHumanAllied", ref TypeDataObject.ForHumanAllied, false ); //this can be 0
            Data.Fill( "ForAIAllied", ref TypeDataObject.ForAIAllied, false ); //this can be 0
            Data.Fill( "ForPlayer", ref TypeDataObject.ForPlayer, false ); //this can be 0
            Data.Fill( "Description", ref TypeDataObject.Description, false ); //this can be 0
            Data.Fill( "ForMinorFactionAllied", ref TypeDataObject.ForMinorFactionAllied, false ); //this can be 0

            TypeDataObject.RelatedResource = DarkZenithFactionBaseInfoRoot.GetDZResourceFromString( tempString );

            //Upgrades
            Data.Fill( "UpgradeIndex", ref TypeDataObject._UpgradeIndex, false );

            //TODO: make sure that a cost is provided? Other sanity checks as needed?
            if ( TypeDataObject.TagForUnit == "DZTransport" )
            {
                foreach ( KeyValuePair<DZResource, int> kv in TypeDataObject.Cost )
                {
                    if ( kv.Key != DZResource.Metal && kv.Value > 0 )
                        throw new Exception( TypeDataObject.InternalName + ": Transports should only cost metal to build, or it will prevent the DZ from bootstrapping from a single metal terminus. This cost " + kv.Value + " " + kv.Key.ToString() );
                }
            }
            if ( TypeDataObject.NameForUnit == "WarpingInDZTerminus" && TypeDataObject.RelatedResource == DZResource.None )
                throw new Exception( "Found DZ Terminus warp in conversion w/o a resource" );
            if ( !String.IsNullOrEmpty( TypeDataObject.TagForUnit ) || !String.IsNullOrEmpty( TypeDataObject.NameForUnit ) )
            {
                if ( TypeDataObject.MarkLevel >= 1 && TypeDataObject.TakeFactionMarkLevelMinusThis >= 0 )
                    throw new Exception( TypeDataObject.InternalName + " has invalid mark level settings; both set. Explicit Mark Level: " + TypeDataObject.MarkLevel + " and faction mark level code " + TypeDataObject.TakeFactionMarkLevelMinusThis + ". Only one can be set. To unset the mark level make it <= 0. To unset the faction mark level, set it to -1" );
                else if ( TypeDataObject.MarkLevel <= 0 && TypeDataObject.TakeFactionMarkLevelMinusThis < 0 )
                    throw new Exception( TypeDataObject.InternalName + " has invalid mark level settings; neither set. Explicit Mark Level: " + TypeDataObject.MarkLevel + " and faction mark level code " + TypeDataObject.TakeFactionMarkLevelMinusThis + ". One must be set." );
            }
            if ( String.IsNullOrEmpty( TypeDataObject.TagForUnit ) && String.IsNullOrEmpty( TypeDataObject.NameForUnit ) &&
                 TypeDataObject._UpgradeIndex <= 0 && !TypeDataObject.IsResourceCreation ) //if we aren't a resource conversion or an upgrade, there must be a unit associated
                throw new Exception( "Got invalid Dark Zenith Resource Conversion for " + TypeDataObject.InternalName + ". This conversion isn't an upgrade or resource creation, so it must have an associated unit tag or name" );
            bool foundAnyCost = false;
            foreach ( KeyValuePair<DZResource, int> kv in TypeDataObject.Cost )
            {
                if ( kv.Value > 0 )
                {
                    foundAnyCost = true;
                    break;
                }
            }
            if ( !foundAnyCost )
                throw new Exception( "Got invalid Dark Zenith Resource Conversion for " + TypeDataObject.InternalName + ". This conversion has no cost." );
            return DelReturn.Continue;
        }
        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            int errors = 0;
            for ( int i = 0; i < this.Rows.Count - 1; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( row.OutputResource.Count > 0 )
                {
                    foreach ( KeyValuePair<DZResource, int> kv in row.OutputResource )
                    {
                        if ( kv.Value > 0 && !row.IsResourceCreation )
                        {
                            errors++;
                            ArcenDebugging.ArcenDebugLogSingleLine( "In DZResourceConversion table, " + row.InternalName + " produces resources but is not tagged IsResourceCreation.", Verbosity.DoNotShow );
                        }
                    }
                }
                for ( int j = i + 1; j < this.Rows.Count; j++ )
                {
                    if ( i == j )
                        continue;
                    DZResourceConversion otherRow = this.Rows[j];
                    //there are different sets of conversions for dark alliance, human allied and minor faction allied
                    //so allow conflicts
                    if ( row.ConversionIndex == otherRow.ConversionIndex &&
                         row.ForHumanAllied && otherRow.ForHumanAllied )
                    {
                        errors++;
                        ArcenDebugging.ArcenDebugLogSingleLine( "Index collision in DZResourceconversion table; '" + row.InternalName + "' and '" + otherRow.InternalName + "' are both index " + row.ConversionIndex, Verbosity.DoNotShow );
                    }
                    else if ( row.ConversionIndex == otherRow.ConversionIndex &&
                         row.ForMinorFactionAllied && otherRow.ForMinorFactionAllied )
                    {
                        errors++;
                        ArcenDebugging.ArcenDebugLogSingleLine( "Index collision in DZResourceconversion table; '" + row.InternalName + "' and '" + otherRow.InternalName + "' are both index " + row.ConversionIndex, Verbosity.DoNotShow );
                    }
                    else if ( row.ConversionIndex == otherRow.ConversionIndex &&
                         row.ForAIAllied && otherRow.ForAIAllied )
                    {
                        errors++;
                        ArcenDebugging.ArcenDebugLogSingleLine( "Index collision in DZResourceconversion table; '" + row.InternalName + "' and '" + otherRow.InternalName + "' are both index " + row.ConversionIndex, Verbosity.DoNotShow );
                    }
                    else if (row.ConversionIndex == otherRow.ConversionIndex &&
                              row.ForPlayer && otherRow.ForPlayer )
                    {
                        errors++;
                        ArcenDebugging.ArcenDebugLogSingleLine( "Index collision in DZResourceconversion table; '" + row.InternalName + "' and '" + otherRow.InternalName + "' are both index " + row.ConversionIndex, Verbosity.DoNotShow );
                    }

                    else if ( row.ConversionIndex == otherRow.ConversionIndex &&
                              !row.ForMinorFactionAllied && !row.ForHumanAllied && !row.ForAIAllied &&
                              !row.ForPlayer && !otherRow.ForPlayer &&
                              !otherRow.ForMinorFactionAllied && !otherRow.ForHumanAllied  && !otherRow.ForAIAllied )
                    {
                        errors++;
                        ArcenDebugging.ArcenDebugLogSingleLine( "Index collision in DZResourceconversion table; '" + row.InternalName + "' and '" + otherRow.InternalName + "' are both index " + row.ConversionIndex, Verbosity.DoNotShow );
                    }
                }
            }
            if ( errors > 0 )
                throw new Exception( errors + " problems in DZResourceConversionTables. See above error messages\n" );

        }
        public void AddConversionToListByName( List<DZResourceConversion> list, string name, Faction faction )
        {
            int numPriorRows = list.Count;
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied )
                    continue;
                //note that we have the same display name for each allegiance variant conversion
                if ( row.DisplayName == name )
                    list.Add( row );
            }
            if ( list.Count == numPriorRows )
                throw new Exception( "Could not find a conversion list for <" + name + "> player allied? " + gData.PlayerAllied + " minor faction allied? " + gData.MinorFactionAllied );

        }

        public void AddBaseBuildablesToList( List<DZResourceConversion> list, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !row.ForHumanAllied ||
                     gData.MinorFactionAllied && !row.ForMinorFactionAllied ||
                     gData.AIAllied && !row.ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.AIAllied && row.ForAIAllied ||
                     !gData.MinorFactionAllied && row.ForMinorFactionAllied ||
                     !gData.PlayerAllied && row.ForHumanAllied )
                    continue;
                //note that we have the same display name for each allegiance variant conversion
                if ( row.DisplayName == "Build Metal Terminus" ||
                     row.DisplayName == "Build White Terminus" ||
                     row.DisplayName == "Build Blue Terminus" ||
                     row.DisplayName == "Build Epistyle" ||
                     row.DisplayName == "Build Transport" )
                    list.Add( row );
            }
        }
        public void AddMetalHarvesterBuildablesToList( List<DZResourceConversion> list, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                //ArcenDebugging.ArcenDebugLogSingleLine("ai allied? " + gData.AIAllied + " and my row's ai alliedndess: " + row.DisplayName + ", " + row.ForAIAllied, Verbosity.DoNotShow );
                if ( gData.PlayerAllied && !row.ForHumanAllied ||
                     gData.MinorFactionAllied && !row.ForMinorFactionAllied ||
                     gData.AIAllied && !row.ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.AIAllied && row.ForAIAllied ||
                     !gData.MinorFactionAllied && row.ForMinorFactionAllied ||
                     !gData.PlayerAllied && row.ForHumanAllied )
                    continue;
                //note that we have the same display name for each allegiance variant conversion
                if ( row.DisplayName == "Build Harvester" ||
                     row.DisplayName == "Build Epistyle" ||
                     row.DisplayName == "Build Transport" )
                {
                    list.Add( row );
                }
            }
        }
        public void AddUnlockedOffensiveConversionsToBag( DrawBag<DZResourceConversion> bag, List<DZUpgrade> upgrades, bool PirateOnly, int numCopiesToAdd, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            //first handle the pirate case
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Adding unlocked conversions", Verbosity.DoNotShow );
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                //First we handle the pirate case; pirates don't worry about upgrades
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied )
                    continue;

                if ( PirateOnly )
                {
                    if ( row.PirateOnly && row.IsOffensive )
                        bag.AddItem( row, numCopiesToAdd );
                }
            }
            if ( PirateOnly )
                return;
            for ( int i = 0; i < upgrades.Count; i++ )
            {
                DZUpgrade upgrade = upgrades[i];
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Unlocking things based on upgrade " + upgrade.ToString(), Verbosity.DoNotShow );
                if ( upgrade.UnlockShipVariant || upgrade.UnlockShipTier )
                {
                    string tag = upgrade.GetTagForUpgrade();
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Adding conversions with tag " + tag + " to bag", Verbosity.DoNotShow );
                    AddOffensiveConversionsWithTagToBag( bag, tag, numCopiesToAdd, faction );
                }
            }
        }
        public void AddAllOffensiveConversionsToBag( DrawBag<DZResourceConversion> bag, bool PirateOnly, int numCopiesToAdd, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied )
                    continue;
                if ( row.PirateOnly && !PirateOnly ||
                    PirateOnly && !row.PirateOnly )
                    continue;

                if ( row.IsOffensive )
                    bag.AddItem( row, numCopiesToAdd );
            }
        }
        public void AddOffensiveConversionsWithTagToBag( DrawBag<DZResourceConversion> bag, string tag, int numCopiesToAdd, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied )
                    continue;
                if ( row.TagForUnit != tag )
                    continue;
                if ( row.IsOffensive )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("\t\tAdding " + row.ToString(), Verbosity.DoNotShow );
                    bag.AddItem( row, numCopiesToAdd );
                }
            }
        }
        public void AddAllUtilityConversionsToBag( DrawBag<DZResourceConversion> bag, int numCopiesToAdd, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied )
                    continue;
                if ( row.IsUtility )
                    bag.AddItem( row, numCopiesToAdd );
            }
        }

        public void AddAllEconomicConversionsToBag( DrawBag<DZResourceConversion> bag, Faction faction )
        {
            //all non offensive, non-defensive and non-upgrade conversions
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied )
                    continue;
                if ( !row.IsOffensive && //not offensive unit
                     !row.IsDefensive &&
                     !row.IsUtility &&
                     !row.IsResourceCreation &&
                     row._UpgradeIndex <= 0 )
                    bag.AddItem( row, 1 );
            }
        }
        public void AddAllDefensiveConversionsToBag( DrawBag<DZResourceConversion> bag, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied )
                    continue;
                if ( gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied )
                    continue;
                if ( gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied )
                continue;
                if ( gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer )
                    continue;

                if ( row.IsDefensive )
                    bag.AddItem( row, 4 ); //extra copies of defensive stuff
            }
        }
        public void AddAllUpgradeConversionsToBag( DrawBag<DZResourceConversion> bag, Faction faction )
        {
            DarkZenithFactionBaseInfoRoot gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DZResourceConversion row = this.Rows[i];
                if ( gData.PlayerAllied && !this.Rows[i].ForHumanAllied ||
                     !gData.PlayerAllied && this.Rows[i].ForHumanAllied ||
                     gData.IsPlayer && !this.Rows[i].ForPlayer ||
                     !gData.IsPlayer && this.Rows[i].ForPlayer ||
                     gData.MinorFactionAllied && !this.Rows[i].ForMinorFactionAllied ||
                     !gData.MinorFactionAllied && this.Rows[i].ForMinorFactionAllied ||
                     gData.AIAllied && !this.Rows[i].ForAIAllied ||
                     !gData.AIAllied && this.Rows[i].ForAIAllied )
                    continue;
                if ( row._UpgradeIndex > 0 )
                {
                    row.DoOnLoad();
                    bag.AddItem( row, 1 );
                }
            }
        }

    }
}
