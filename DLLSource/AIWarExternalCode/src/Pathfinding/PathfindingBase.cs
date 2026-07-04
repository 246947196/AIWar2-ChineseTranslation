using Arcen.Universal;
using System;

using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public abstract class ArcenPathfinder<N>
        where N : Pathfindable
    {
        protected string DebugName
        {
            get { return (this.Faction == null ? "nullfac" : this.Faction.GetDisplayName()) + " " + this.DebugAddendum; }
        }

        protected Faction Faction;
        protected string DebugAddendum;
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private System.DateTime lastLogTime = DateTime.Now;
        public int callCount = 0;

        public static int TotalPathfindersEver = 1;
        
        protected abstract NodePassability CalculateIsNodePassable_Slow( N node );
        protected abstract int GetAccurateCrowFliesDistanceBetweenNodes( N left, N right );
        protected abstract int HeuristicCostEstimate( N Origin, N Target );
        protected abstract string GetDebugLogString( N node );
        protected abstract void SetDebugText( N node, string CostToGetHere, string GuessCostToGetToTarget );

        /// <summary>
        /// If any node maps larger than this are submitted, they will fail.
        /// Having this lets us do fixed memory allocations and avoid dictionary lookups
        /// </summary>
        public const int MAX_NODES_EVER_ALLOWED = 300;

        protected readonly List<NodePassability> Passabilities = List<NodePassability>.Create_WillNeverBeGCed( MAX_NODES_EVER_ALLOWED, "ArcenPathfinder-Passabilities", 100 );

        private static readonly ReferenceTracker RefTracker = new ReferenceTracker( "Pathfinders" );

        protected ArcenPathfinder()
        {
            System.Threading.Interlocked.Add( ref TotalPathfindersEver, 1 );
            RefTracker.IncrementObjectCount();
        }

        public abstract void ReturnToPool(); //these all need to be concurrent-pooled now, or the simulation will cut you

        public void WipeForReuseAsNewObject()
        {
            this.callCount = 0;
            this.Faction = null;
            this.DebugAddendum = string.Empty;
            this.Passabilities.Clear();
            this.lastContextCycleOfPathingRecalc = -1;
            this.MidWipeForReuseAsNewObject();
            this.isRunningNow.MarkAsNoLongerBusy();
        }
        protected abstract void MidWipeForReuseAsNewObject();

        private void RecalculatePassabilities( N AnyNode, int PathableCount )
        {
            Passabilities.Clear();
            for ( Int16 i = 0; i < PathableCount; i++ )
            {
                N newNode = (N)AnyNode.GetPathfindableAtIndexInParentList( i );
                //if ( newNode == null )
                //    Passabilities.Add( NodePassability.NEVER_PASSABLE );
                //else
                    Passabilities.Add( this.CalculateIsNodePassable_Slow( newNode ) );
            }
        }

        /// <summary>
        /// Find the path between Orgin and Target, filling PathToFill with the nodes to visit (excluding Origin and including Target).
        /// </summary>
        public void FindPath( Faction Fac, string DebugAddendum, List<N> PathToFill, N Origin, N Target, int NeedToGetWithinXRangeOfTarget, int RequiresNoMoreThanXRangeFromOrigin, ArcenSimContextAnyStatus ContextOrNull )
        {
            System.Threading.Interlocked.Add( ref PathBetweenPlanetsForFaction.PathRecalculations, 1 ); //count these per FindPath inner call
            //callCount++;
            //sw.Start();
            try
            {
                FindPathInner( Fac, DebugAddendum, PathToFill, Origin, Target, NeedToGetWithinXRangeOfTarget, RequiresNoMoreThanXRangeFromOrigin, false, ContextOrNull );
            }
            catch ( ArcenPleaseStopThisThreadException ) //no problems!
            {
                this.isRunningNow.MarkAsNoLongerBusy();
                PathToFill.Clear();
            }
            catch ( Exception e )
            {
                this.isRunningNow.MarkAsNoLongerBusy();
                PathToFill.Clear();
                if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                    return; //no problems!
                throw e;
            }
            //finally
            //{
            //    sw.Stop();
            //    if ( (DateTime.Now - lastLogTime).TotalSeconds > 0.5f )
            //    {
            //        lastLogTime = DateTime.Now;
            //        ArcenDebugging.ArcenDebugLogSingleLine( this.DebugName + " ms: " + sw.ElapsedMilliseconds.ToString( "#,##0" ) + " calls: " + callCount, Verbosity.DoNotShow );
            //    }
            //}

            this.isRunningNow.MarkAsNoLongerBusy();
        }

        /// <summary>
        /// Find the path between Orgin and Target, filling PathToFill with the nodes to visit (excluding Origin and including Target).
        /// </summary>
        public void FindPath( Faction Fac, string DebugAddendum, List<N> PathToFill, N Origin, N Target, int NeedToGetWithinXRangeOfTarget, int RequiresNoMoreThanXRangeFromOrigin, bool DoDebugLog, 
            ArcenSimContextAnyStatus ContextOrNull )
        {
            System.Threading.Interlocked.Add( ref PathBetweenPlanetsForFaction.PathRecalculations, 1 ); //count these per FindPath inner call
            //callCount++;
            //sw.Start();
            try
            {
                FindPathInner( Fac, DebugAddendum, PathToFill, Origin, Target, NeedToGetWithinXRangeOfTarget, RequiresNoMoreThanXRangeFromOrigin, DoDebugLog, ContextOrNull );
            }
            catch ( ArcenPleaseStopThisThreadException ) //no problems!
            {
                this.isRunningNow.MarkAsNoLongerBusy();
                PathToFill.Clear();
            }
            catch ( Exception e )
            {
                this.isRunningNow.MarkAsNoLongerBusy();
                PathToFill.Clear();
                if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                    return; //no problems!
                throw e;
            }
            //finally
            //{
            //    sw.Stop();
            //    if ( (DateTime.Now - lastLogTime).TotalSeconds > 0.5f )
            //    {
            //        lastLogTime = DateTime.Now;
            //        ArcenDebugging.ArcenDebugLogSingleLine( this.DebugName + " ms: " + sw.ElapsedMilliseconds.ToString( "#,##0" ) + " calls: " + callCount, Verbosity.DoNotShow );
            //    }
            //}

            this.isRunningNow.MarkAsNoLongerBusy();
        }

        //these things should only be called one at a time PER THREAD
        //the class is threadsafe, but you can't use one object on multiple threads for this class
        private N[] cameFrom = new N[MAX_NODES_EVER_ALLOWED];
        private int[] costToGetHere = new int[MAX_NODES_EVER_ALLOWED];
        private int[] guessCostToGetToTarget = new int[MAX_NODES_EVER_ALLOWED];
        private bool[] isClosed = new bool[MAX_NODES_EVER_ALLOWED];
        private bool[] isOpen = new bool[MAX_NODES_EVER_ALLOWED];
        private readonly List<N> openList = List<N>.Create_WillNeverBeGCed( 30, "ArcenPathfinder-openList", 30 );
        private readonly List<N> workingResult = List<N>.Create_WillNeverBeGCed( 30, "ArcenPathfinder-workingResult", 30 );
        private ThreadingExchanger isRunningNow = new ThreadingExchanger( "ArcenPathfinder", 30f );
        private DateTime lastRunStart = DateTime.Now.AddSeconds( -3 );

        #region SetOpen
        private void SetOpen( N Node, bool MakeOpen )
        {
            int index = Node.Index;
            if ( MakeOpen )
            {
                //if ( isOpen[index] )
                //    return; //already open
                isOpen[index] = true;
                openList.Add( Node );
            }
            else //take out of open
            {
                //if ( !isOpen[index] )
                //    return; //already not open
                isOpen[index] = false;
                openList.Remove( Node );
            }
        }
        #endregion

        protected int lastContextCycleOfPathingRecalc = -1;
        private int lastFindPathTotalEntries = MAX_NODES_EVER_ALLOWED;

        private bool hasShownDeadFromPlanetCountMessage = false;

        /// <summary>
        /// in general this method is very non-heap-static, but it's also fairly thread-safe; can be made more heap-static later if necessary
        /// </summary>
        private void FindPathInner( Faction Fac, string DebugAddendum, List<N> PathToFill, N Origin, N Target, int NeedToGetWithinXRangeOfTarget, int RequiresNoMoreThanXRangeFromOrigin, bool DoDebugLog, 
            ArcenSimContextAnyStatus ContextOrNull )
        {
            this.Faction = Fac;
            this.DebugAddendum = DebugAddendum;

            if ( World_AIW2.Instance.CurrentGalaxy.GetTotalPlanetCount() >= MAX_NODES_EVER_ALLOWED )
            {
                if ( !hasShownDeadFromPlanetCountMessage )
                {
                    hasShownDeadFromPlanetCountMessage = true;
                    ArcenDebugging.ArcenDebugLogSingleLine( "You must have fewer planets than " + MAX_NODES_EVER_ALLOWED + ", but you have " + World_AIW2.Instance.CurrentGalaxy.GetTotalPlanetCount() +
                        ", so no pathfinding can be done.", Verbosity.ShowAsError );
                }
                return;
            }

            if ( this.isRunningNow.IsBusy() )
            {
                if ( Engine_Universal.RunStatus != RunStatus.GameStart )
                {
                    TimeSpan timeSince = (DateTime.Now - lastRunStart);
                    ArcenDebugging.ArcenDebugLog( "Called " + this.DebugName + " pathfinder again before it finished finding the first path!  This probably means it's being called by two threads! Run started: " +
                        timeSince.Ticks + " ticks ago (" + timeSince.TotalMilliseconds + "ms)", Verbosity.ShowAsError );
                }
                return;// alwaysEmptyList;
            }

            int debugStage = 1;

            if ( !this.isRunningNow.DoNextOnlyIfNotAlreadyBusy( DebugAddendum ) )
                return;
            lastRunStart = DateTime.Now;

            try
            {
                debugStage = 1000;

                PathToFill.Clear();
                ArcenCharacterBuffer logBuffer = null;
                if ( DoDebugLog )
                {
                    logBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "PathfindingBase-FindPathInner-logBuffer", 10f );
                    logBuffer.Add( "FindPath called from " )
                        .Add( this.GetDebugLogString( Origin ) )
                        .Add( " to " )
                        .Add( this.GetDebugLogString( Target ) );

                    SetDebugText( Origin, ColorMath.LightGreenString + "O" + ColorMath.WhiteString, HeuristicCostEstimate( Origin, Target ).ToString() );
                    SetDebugText( Target, ColorMath.LightRedString + "T" + ColorMath.WhiteString, string.Empty );
                }

                debugStage = 1300;
                if ( Origin == null || Target == null || Origin == Target )
                {
                    this.isRunningNow.MarkAsNoLongerBusy();
                    return;
                }

                debugStage = 1400;
                int newPathableCount = Origin.GetTotalPathfindableInParentList();
                int newContextIndex = ContextOrNull == null ? -1 : ContextOrNull.NonsimLoopIndex;
                if ( this.Passabilities.Count <= 0 || newContextIndex < 0 || this.lastContextCycleOfPathingRecalc != newContextIndex || this.Passabilities.Count != newPathableCount )
                {
                    this.lastContextCycleOfPathingRecalc = newContextIndex;
                    debugStage = 1500;
                    this.RecalculatePassabilities( Origin, newPathableCount );
                }

                debugStage = 1600;
                if ( Passabilities[Origin.Index] == NodePassability.NEVER_PASSABLE )
                {
                    this.isRunningNow.MarkAsNoLongerBusy();
                    PathToFill.Clear();
                    return;
                }
                debugStage = 1700;
                switch ( Passabilities[Target.Index] )
                {
                    case NodePassability.ALWAYS_PASSABLE:
                    case NodePassability.ONLY_PASSABLE_FOR_ORIGIN_AND_TARGET:
                        break;
                    default:
                        {
                            this.isRunningNow.MarkAsNoLongerBusy();
                            PathToFill.Clear();
                            return;
                        }
                }

                debugStage = 1800;
                workingResult.Clear();

                for ( int i = 0; i < lastFindPathTotalEntries; i++ )
                {
                    cameFrom[i] = null;
                    isClosed[i] = false;
                    isOpen[i] = false;
                    costToGetHere[i] = -1;
                    guessCostToGetToTarget[i] = -1;
                }
                openList.Clear();
                SetOpen( Origin, true );

                //save us some work!
                lastFindPathTotalEntries = this.Passabilities.Count;

                costToGetHere[Origin.Index] = 0;
                
                debugStage = 1900;
                guessCostToGetToTarget[Origin.Index] = costToGetHere[Origin.Index] + HeuristicCostEstimate( Origin, Target );

                while ( openList.Count > 0 )
                {
                    N bestNode = null;
                    debugStage = 2000;
                    for ( int i = 0; i < openList.Count; i++ )
                    {
                        N node = openList[i];
                        debugStage = 2100;
                        if ( bestNode == null ||
                            guessCostToGetToTarget[bestNode.Index] > guessCostToGetToTarget[node.Index] )
                            bestNode = node;
                    }
                    if ( DoDebugLog )
                        logBuffer.Add( "\n" ).Add( "bestNode=" ).Add( this.GetDebugLogString( bestNode ) )
                            .Add( ":costToGetHere=" ).Add( costToGetHere[bestNode.Index] )
                            .Add( ":guessCostToGetToTarget=" ).Add( guessCostToGetToTarget[bestNode.Index] )
                            ;
                    debugStage = 2200;
                    if ( bestNode.Index == Target.Index )
                    {
                        if ( DoDebugLog )
                        {
                            logBuffer.Add( ":found target" );
                            SetDebugText( bestNode, ColorMath.LightGreenString + "T" + ColorMath.WhiteString, costToGetHere[bestNode.Index].ToString() );
                        }
                        workingResult.Add( bestNode );
                        break;
                    }
                    else
                    {
                        debugStage = 2300;
                        if ( NeedToGetWithinXRangeOfTarget > 0 &&
                              GetAccurateCrowFliesDistanceBetweenNodes( bestNode, Target ) <= NeedToGetWithinXRangeOfTarget )
                        {
                            if ( DoDebugLog )
                            {
                                logBuffer.Add( ":found within range of target" );
                                SetDebugText( bestNode, ColorMath.LightYellowString + "T" + ColorMath.WhiteString, costToGetHere[bestNode.Index].ToString() );
                            }
                            workingResult.Add( bestNode );
                            break;
                        }
                        else
                        {
                            debugStage = 2400;
                            if ( RequiresNoMoreThanXRangeFromOrigin > 0 && costToGetHere[bestNode.Index] >= RequiresNoMoreThanXRangeFromOrigin )
                            {
                                if ( DoDebugLog )
                                {
                                    logBuffer.Add( ":hit end of tether" );
                                    SetDebugText( bestNode, ColorMath.LightYellowString + "T" + ColorMath.WhiteString, costToGetHere[bestNode.Index].ToString() );
                                }
                                workingResult.Add( bestNode );
                                break;
                            }
                        }
                    }

                    debugStage = 2500;
                    SetOpen( bestNode, false );
                    debugStage = 2600;
                    isClosed[bestNode.Index] = true;

                    debugStage = 2700;
                    int neighborCount = bestNode.LinkedPathfindables.Count;
                    debugStage = 2800;
                    for ( int i = 0; i < neighborCount; i++ )
                    {
                        debugStage = 2900;
                        N neighbor = (N)bestNode.LinkedPathfindables[i];
                        if ( neighbor == null )
                            continue;
                        debugStage = 3000;
                        bool inOpenSet = isOpen[neighbor.Index];
                        debugStage = 3100;
                        bool inClosedSet = isClosed[neighbor.Index];
                        debugStage = 3200;
                        bool isReconsideration = inOpenSet || inClosedSet;
                        debugStage = 3300;
                        int costToGetToNeighbor = costToGetHere[bestNode.Index];
                        debugStage = 3400;
                        costToGetToNeighbor += 1;
                        debugStage = 3500;
                        if ( RequiresNoMoreThanXRangeFromOrigin > 0 && costToGetToNeighbor > RequiresNoMoreThanXRangeFromOrigin )
                            continue;
                        debugStage = 3600;
                        if ( isReconsideration &&
                             costToGetToNeighbor >= costToGetHere[neighbor.Index] )
                            continue;
                        debugStage = 3700;

                        if ( DoDebugLog )
                        {
                            logBuffer.Add( "\n" );
                            if ( isReconsideration )
                                logBuffer.Add( "re" );
                            logBuffer.Add( "considering neighbor:" ).Add( this.GetDebugLogString( neighbor ) );
                        }
                        if ( Passabilities[neighbor.Index] != NodePassability.ALWAYS_PASSABLE &&
                             neighbor != Origin &&
                             neighbor != Target )
                        {
                            if ( DoDebugLog ) logBuffer.Add( ":not passable" );
                            continue;
                        }

                        debugStage = 3800;
                        cameFrom[neighbor.Index] = bestNode;
                        debugStage = 3900;
                        costToGetHere[neighbor.Index] = costToGetToNeighbor;
                        debugStage = 4000;
                        guessCostToGetToTarget[neighbor.Index] = costToGetToNeighbor + HeuristicCostEstimate( neighbor, Target );
                        debugStage = 4100;
                        if ( !inOpenSet )
                            SetOpen( neighbor, true );
                        debugStage = 4200;
                        if ( inClosedSet )
                            isClosed[neighbor.Index] = false;

                        if ( DoDebugLog )
                        {
                            logBuffer.Add( ":passable" )
                                .Add( ":costToGetHere=" ).Add( costToGetHere[neighbor.Index] )
                                .Add( ":guessCostToGetToTarget=" ).Add( guessCostToGetToTarget[neighbor.Index] )
                                ;
                            SetDebugText( neighbor, costToGetHere[neighbor.Index].ToString(), guessCostToGetToTarget[neighbor.Index].ToString() );
                        }
                    }
                }

                debugStage = 5000;
                if ( workingResult.Count > 0 ) // path found
                {
                    debugStage = 5100;
                    N tracingBackFrom = workingResult[workingResult.Count - 1];
                    debugStage = 5200;
                    while ( cameFrom[tracingBackFrom.Index] != null && cameFrom[tracingBackFrom.Index] != Origin)
                    {
                        debugStage = 5300;
                        tracingBackFrom = cameFrom[tracingBackFrom.Index];
                        debugStage = 5400;
                        workingResult.Add( tracingBackFrom );
                        if ( DoDebugLog )
                        {
                            if ( tracingBackFrom != Origin )
                                SetDebugText( tracingBackFrom,
                                    ColorMath.LightBlueString + costToGetHere[tracingBackFrom.Index].ToString() + ColorMath.WhiteString,
                                    ColorMath.LightBlueString + guessCostToGetToTarget[tracingBackFrom.Index].ToString() + ColorMath.WhiteString );
                        }
                    }
                    debugStage = 6000;
                    PathToFill.Clear();
                    debugStage = 6100;
                    for ( int i = workingResult.Count - 1; i >= 0; i-- )
                        PathToFill.Add( workingResult[i] );
                    //result.Reverse(); do the equivalent of this, above

                    if ( DoDebugLog )
                    {
                        ArcenDebugging.ArcenDebugLog( logBuffer.ToString(), Verbosity.Chat );
                        logBuffer.ReturnToPool();
                        logBuffer = null;
                    }

                    debugStage = 6200;
                    this.isRunningNow.MarkAsNoLongerBusy();
                }
                else
                {
                    debugStage = 7000;
                    if ( DoDebugLog )
                    {
                        ArcenDebugging.ArcenDebugLog( logBuffer.ToString(), Verbosity.Chat );
                        logBuffer.ReturnToPool();
                        logBuffer = null;
                    }
                    this.isRunningNow.MarkAsNoLongerBusy();
                    PathToFill.Clear();
                    return ;
                }
            }
            catch ( ArcenPleaseStopThisThreadException ) //no problems!
            {
                this.isRunningNow.MarkAsNoLongerBusy();
                PathToFill.Clear();
            }
            catch ( Exception e )
            {
                this.isRunningNow.MarkAsNoLongerBusy();
                PathToFill.Clear();
                if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                    return; //no problems!
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in pathfinding debugStage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }

            this.isRunningNow.MarkAsNoLongerBusy();
        }

        public enum NodePassability
        {
            NEVER_PASSABLE,
            ONLY_PASSABLE_FOR_ORIGIN,
            ONLY_PASSABLE_FOR_ORIGIN_AND_TARGET,
            ALWAYS_PASSABLE
        }
    }
}
