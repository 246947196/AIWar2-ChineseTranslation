using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class RandomFactionType : ArcenDynamicTableRow, IConcurrentPoolable<RandomFactionType>, IProtectedListable
    {
        public readonly List<string> IncludedFactionStr = List<string>.Create_WillNeverBeGCed( 90, "RandomFactionType-IncludedFactionStr" );
        public readonly List<SpecialFactionData> IncludedFactions = List<SpecialFactionData>.Create_WillNeverBeGCed( 90, "RandomFactionType-IncludedFactions" );
        public string Description;
        public bool IncludesAllPossibleOptions;
        public bool AlwaysHostileToAll;
        //public string Description;

        public override void AddDescription(ArcenCharacterBufferBase Buffer)
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Buffer.Add(this.Description).NewLine();
                debugCode = 200;
                Buffer.Add("此类型包含以下阵营：\n");
                for ( int i = 0; i < this.IncludedFactions.Count; i++ )
                {
                    debugCode = 300;
                    Buffer.Add("\t").Add(this.IncludedFactions[i].GetDisplayName()).Add(": <color=" + this.IncludedFactions[i].Impact.GetHexColor() +">").Add( this.IncludedFactions[i].Impact.ToString() ).Add("</color>\n");
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in RandomFactionType.GetDescription() debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private RandomFactionType() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "RandomFactionTypes" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<RandomFactionType> Pool = new ConcurrentPool<RandomFactionType>( "RandomFactionTypes", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new RandomFactionType(); } );

        public static RandomFactionType GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<RandomFactionType> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<RandomFactionType>( new RandomFactionType() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class RandomFactionTypeTable : ArcenDynamicTable<RandomFactionType>
    {
        public static RandomFactionTypeTable Instance;
        public RandomFactionTypeTable() : base( "RandomFactionType", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow )
        {
            Instance = this;
            this.DoesSortRowsAndFindDefaultsAndNoneRowsDirectlyAtEndOfInitialize = true;
        }

        public override RandomFactionType GetNewRowFromPool()
        {
            return RandomFactionType.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, RandomFactionType TypeDataObject )
        {
            Data.Fill( "description", ref TypeDataObject.Description, !Data.ReadingPartialRecord );
            Data.Fill( "includes_all_possible_options", ref TypeDataObject.IncludesAllPossibleOptions, false );
            Data.Fill( "always_hostile_to_all", ref TypeDataObject.AlwaysHostileToAll, false );
            foreach ( ArcenXMLElement node in Data.ChildrenOfType_AndParentsAndPartials( "IncludedFactions" ) )
            {
                string name = "";
                node.Fill( "faction_name", ref name, !node.ReadingPartialRecord );
                TypeDataObject.IncludedFactionStr.Add( name );
            }
            return DelReturn.Continue;
        }
        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            // RowsByTier.Clear();
            // WorkingIds.Clear();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                RandomFactionType row = this.Rows[i];
                if ( row.IncludesAllPossibleOptions )
                {
                    for ( int k = 0; k < SpecialFactionDataTable.Instance.Rows.Count; k++ )
                    {
                        SpecialFactionData specialRow = SpecialFactionDataTable.Instance.Rows[k];
                        if ( specialRow.BlockedFromRandomFaction )
                        {
                            continue;
                        }
                        if ( specialRow.Impact != TypeDifficulty.Unset &&
                             (specialRow.CanBeOnMinorFactionTeam ||
                               specialRow.CanBeFriendlyToPlayer ||
                               specialRow.CanBeHostileToAll ||
                               specialRow.CanBeDarkAlliance ||
                               specialRow.CanBeAlliedToAI) )
                            row.IncludedFactions.Add( specialRow );
                    }
                    row.IncludedFactions.Sort( static delegate (SpecialFactionData L, SpecialFactionData R )
                    {
                        int impactDiff = L.Impact.CompareTo( R.Impact );
                        if ( impactDiff != 0 )
                            return impactDiff;
                        return L.InternalName.CompareTo( R.InternalName );
                    } );
                    continue;
                }
                if ( row.IncludedFactionStr.Count == 0 )
                    throw new Exception( "Random faction type " + row.DisplayName + " has no possible factions" );

                for ( int j = 0; j < row.IncludedFactionStr.Count; j++ )
                {
                    string factionName = row.IncludedFactionStr[j];
                    SpecialFactionData foundFaction = null;
                    bool isValid = false;
                    for ( int k = 0; k < SpecialFactionDataTable.Instance.Rows.Count; k++ )
                    {
                        SpecialFactionData specialRow = SpecialFactionDataTable.Instance.Rows[k];
                        if ( specialRow.BlockedFromRandomFaction )
                        {
                            continue;
                        }
                        if ( specialRow.InternalName == factionName ||
                             specialRow.DisplayName == factionName )
                            foundFaction = specialRow;
                        if ( specialRow.Impact != TypeDifficulty.Unset &&
                             (specialRow.CanBeOnMinorFactionTeam ||
                               specialRow.CanBeFriendlyToPlayer ||
                               specialRow.CanBeHostileToAll ||
                               specialRow.CanBeDarkAlliance ||
                               specialRow.CanBeAlliedToAI) )
                        {
                            isValid = true;
                        }
                        if ( foundFaction != null && isValid )
                            break;
                    }
                    if ( foundFaction == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Could not find a faction with name <" + factionName + "> that is eligible to be random for " + row.InternalName, Verbosity.DoNotShow );
                        continue;
                    }
                    if ( !isValid )//this is possible if someone has added (say) Kaizer's Marauders here but then the mod was disabled. Leaving this here as a warning error message in case of actual mistakes though
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "We found <" + factionName + " but it was not valid. It needs to have an Impact set and some information about what its possible allegiance is", Verbosity.DoNotShow );
                        continue;
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine("Row " + row.InternalName + " has found a faction with the name " + factionName + " which is " + foundFaction.GetDisplayName(), Verbosity.DoNotShow ); //useful for debugging
                    row.IncludedFactions.Add( foundFaction );
                }
                row.IncludedFactions.Sort( static delegate (SpecialFactionData L, SpecialFactionData R )
                {
                    int impactDiff = L.Impact.CompareTo( R.Impact );
                    if ( impactDiff != 0 )
                        return impactDiff;
                    return L.InternalName.CompareTo( R.InternalName );
                } );
            }

        }
    }
}
