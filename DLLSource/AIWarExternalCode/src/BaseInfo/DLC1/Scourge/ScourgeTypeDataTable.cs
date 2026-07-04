using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    /* These classes are for the XML data in the ScourgeTypeData xml directory. We read the data from the XML, then put it in this table
     for later use*/
    public class ScourgeTypeData : ArcenDynamicTableRow, IConcurrentPoolable<ScourgeTypeData>, IProtectedListable
    {
        public string name;
        public string TagForWarrior;
        public Int16 id;
        public string color = "a1a1a1";
        public string description = "";
        public readonly Dictionary<string, string> UnitForSynergy = Dictionary<string, string>.Create_WillNeverBeGCed( 30, "ScourgeTypeData-UnitForSynergy" );
        //The UnitForSynergy is a map from <other scourge type unit> to <unit type this can be upgraded into
        public override string ToString()
        {
            return "This is an Armory of corrupted <color=#" + this.color + ">" + this.name + "</color>";
        }
        public bool DoesThisArmoryUpgradeThisUnit( GameEntity_Squad entity )
        {
            foreach ( KeyValuePair<string, string> pair in UnitForSynergy )
            {
                if ( entity.TypeData.GetHasTag( pair.Key ) )
                    return true;
            }
            return false;
        }
        public GameEntityTypeData EntityUnitUpgradesInto( GameEntity_Squad entity, ArcenSimContextAnyStatus Context )
        {
            foreach ( KeyValuePair<string, string> pair in UnitForSynergy )
            {
                if ( entity.TypeData.GetHasTag( pair.Key ) )
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, pair.Value );
                    return entityData;
                }
            }
            return null;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ScourgeTypeData() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ScourgeTypeDatas" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<ScourgeTypeData> Pool = new ConcurrentPool<ScourgeTypeData>( "ScourgeTypeDatas", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ScourgeTypeData(); } );

        public static ScourgeTypeData GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<ScourgeTypeData> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<ScourgeTypeData>( new ScourgeTypeData() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class ScourgeTypeDataTable : ArcenDynamicTable<ScourgeTypeData>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static ScourgeTypeDataTable Instance;
        public readonly Dictionary<int, ScourgeTypeData> RowById = Dictionary<int, ScourgeTypeData>.Create_WillNeverBeGCed( 500, "ScourgeTypeData-RowById" );
        public readonly Dictionary<int, ScourgeTypeData> RowByIdWithoutSpire = Dictionary<int, ScourgeTypeData>.Create_WillNeverBeGCed( 500, "ScourgeTypeData-RowByIdWithoutSpire" );

        public ScourgeTypeData SpireData = null;

        public override ScourgeTypeData GetNewRowFromPool()
        {
            return ScourgeTypeData.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public ScourgeTypeDataTable() : base( "ScourgeTypeData", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow,
            //another one that needs to be in order in the xml or it dies
            ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }
        public override DelReturn NodeProcessor( ArcenXMLElement Data, ScourgeTypeData TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "id", ref TypeDataObject.id, !Data.ReadingPartialRecord );
            Data.Fill( "color", ref TypeDataObject.color, false );
            Data.Fill( "description", ref TypeDataObject.description, false );
            Data.Fill( "tag_for_warrior", ref TypeDataObject.TagForWarrior, !Data.ReadingPartialRecord );

            string tagForSynergy = ""; //a unit with this tag can be transformed into tagForSynergyTransformation
            string tagForSynergyTransformation = "";
            Data.Fill( "synergy_one_tag", ref tagForSynergy, false );
            Data.Fill( "synergy_one_transformation", ref tagForSynergyTransformation, false );
            if ( !String.IsNullOrEmpty( tagForSynergy ) )
                TypeDataObject.UnitForSynergy[tagForSynergy] = tagForSynergyTransformation;

            tagForSynergy = "";
            tagForSynergyTransformation = "";

            Data.Fill( "synergy_two_tag", ref tagForSynergy, false );
            Data.Fill( "synergy_two_transformation", ref tagForSynergyTransformation, false );
            if ( !String.IsNullOrEmpty( tagForSynergy ) )
                TypeDataObject.UnitForSynergy[tagForSynergy] = tagForSynergyTransformation;

            tagForSynergy = "";
            tagForSynergyTransformation = "";

            Data.Fill( "synergy_three_tag", ref tagForSynergy, false );
            Data.Fill( "synergy_three_transformation", ref tagForSynergyTransformation, false );
            if ( !String.IsNullOrEmpty( tagForSynergy ) )
                TypeDataObject.UnitForSynergy[tagForSynergy] = tagForSynergyTransformation;

            tagForSynergy = "";
            tagForSynergyTransformation = "";

            Data.Fill( "synergy_four_tag", ref tagForSynergy, false );
            Data.Fill( "synergy_four_transformation", ref tagForSynergyTransformation, false );
            if ( !String.IsNullOrEmpty( tagForSynergy ) )
                TypeDataObject.UnitForSynergy[tagForSynergy] = tagForSynergyTransformation;
            Data.Fill( "synergy_five_tag", ref tagForSynergy, false );
            Data.Fill( "synergy_five_transformation", ref tagForSynergyTransformation, false );
            if ( !String.IsNullOrEmpty( tagForSynergy ) )
                TypeDataObject.UnitForSynergy[tagForSynergy] = tagForSynergyTransformation;
            Data.Fill( "synergy_six_tag", ref tagForSynergy, false );
            Data.Fill( "synergy_six_transformation", ref tagForSynergyTransformation, false );
            if ( !String.IsNullOrEmpty( tagForSynergy ) )
                TypeDataObject.UnitForSynergy[tagForSynergy] = tagForSynergyTransformation;

            if ( TypeDataObject.name == "Spire" )
                SpireData = TypeDataObject;

            return DelReturn.Continue;
        }
        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            bool debug = false;
            RowById.Clear();
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Doing post initialization logic for Scourge Type Data", Verbosity.DoNotShow );
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                ScourgeTypeData row = this.Rows[i];
                //This is no longer a restriction, but it's left as an example of the sort of XML scrubbing it's probably a good
                //idea to have for modders
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( i + " " + row.name, Verbosity.DoNotShow );
                RowById[row.id] = row;
                if ( row.id != i + 1 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Bug: Your Scourge Type ids are out of order for " + row.name + " id " + row.id + ". The id must be in ascending order in the XML file, each id going up one at a time. So row 1 must be id 1, row 2 == id 2, etc... The code was expecting it to be id " + i + 1, Verbosity.DoNotShow );
            }
        }
        public ScourgeTypeData GetRowById( int id )
        {
            return RowById[id];
        }
        public ScourgeTypeData GetRowByArmoryName( string request, ArcenSimContextAnyStatus Context )
        {
            if ( request == "Random" )
                return GetRandomRow( null, false, Context );

            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                ScourgeTypeData row = this.Rows[i];
                if ( row.name == request )
                    return row;
            }
            return null;

        }

        public ScourgeTypeData GetRandomRow( List<SafeSquadWrapper> currentArmories, bool spireEnabled, ArcenSimContextAnyStatus Context )
        {
            //Gets a "random" row, with the proviso that if spire isn't enabled then Spire won't be returned, and also
            //that if spire is enabled and there aren't any spire armories, the next one must be a spire armory
            List<string> rowsToExclude = ArcenStrings.GetTemporaryStringList( "ScourgeTypeData-GetRandomRow-rowsToExclude", 10f );
            if ( rowsToExclude == null ) //blocked for teardown/shutdown; bail
                return null;

            if ( !spireEnabled )
                rowsToExclude.Add( "Spire" );
            if ( currentArmories == null || currentArmories.Count <= 2 )
                rowsToExclude.Add( "Neinzul" ); //neinzul suck as an early type
            if ( currentArmories != null && currentArmories.Count <= 4 )
            {
                //make sure there's lots of variety
                for ( int i = 0; i < currentArmories.Count; i++ )
                {
                    GameEntity_Squad armory = currentArmories[i].GetSquad();
                    if ( armory == null )
                        continue;
                    ScourgePerUnitBaseInfo armorydata = armory.GetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    ScourgeTypeData typedata = ScourgeTypeDataTable.Instance.GetRowById( armorydata.ScourgeTypeId );
                    if ( typedata == null ) //this is allowed because we are usually calling this function with one armory with an unset scourge type (since this is how we get the scourget type)
                        continue;

                    rowsToExclude.Add( typedata.name );
                }
            }

            //First check if the spire is enabled but we don't have a spire armory; if so, make a spire armory
            bool debug = false;
            if ( spireEnabled && currentArmories != null )
            {
                bool spireFound = false;
                for ( int i = 0; i < currentArmories.Count; i++ )
                {
                    GameEntity_Squad armory = currentArmories[i].GetSquad();
                    if ( armory == null )
                        continue;
                    ScourgePerUnitBaseInfo armorydata = armory.GetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    if ( armorydata.ScourgeTypeId == SpireData.id )
                    {
                        spireFound = true;
                        break;
                    }
                }
                if ( !spireFound )
                {
                    ArcenStrings.ReleaseTemporaryStringList( rowsToExclude );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Returning spire armory", Verbosity.DoNotShow );
                    return SpireData;
                }
            }
            ScourgeTypeData selection = getRandomRowWithExclusion( rowsToExclude, Context );
            ArcenStrings.ReleaseTemporaryStringList( rowsToExclude );
            return selection;
        }
        private ScourgeTypeData getRandomRowWithExclusion( List<string> rowsToExclude, ArcenSimContextAnyStatus Context )
        {
            ScourgeTypeData output = null;
            int maxRetries = 30; //was 100, and that's likely to break the game in the late game.
            bool debug = false;
            if ( debug )
            {
                if ( rowsToExclude.Count == 0 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "no rows to exclude", Verbosity.DoNotShow );
                else
                {
                    for ( int i = 0; i < rowsToExclude.Count; i++ )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\tExcluding " + rowsToExclude[i], Verbosity.DoNotShow );
                }
            }
            do
            {
                int randomNumber = Context.RandomToUse.Next( 0, RowById.Count ) + 1; //we don't use index 0
                output = RowById[randomNumber];
                for ( int i = 0; i < rowsToExclude.Count; i++ )
                {
                    if ( output.name == rowsToExclude[i] )
                    {
                        output = null;
                        break;
                    }
                }
            } while ( output == null && maxRetries-- > 0 );
            return output;
        }
    }
}
