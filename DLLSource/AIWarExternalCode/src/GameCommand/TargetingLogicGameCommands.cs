using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_SetTargetList : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            //command.RelatedEntityIDs = List<Int64>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE );
            //command.RelatedIntegers = List<int>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE ); //systemIDs
            //command.RelatedIntegers2 = List<int>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE ); //countsOfTargets
            //command.RelatedIntegers4 = List<Int64>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE * MAX_TARGETS_PER_SYSYEM ); //targets
            //UnityEngine.Debug.Log( "GameCommand_SetTargetList!" );
            #region Inequality Checks (Bad Data)
            if ( command.RelatedEntityIDs.Count != command.RelatedIntegers.Count )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers.Count(" +
                        command.RelatedIntegers.Count + ") != " + command.RelatedEntityIDs.Count + " in GameCommand_SetTargetList!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedEntityIDs.Count != command.RelatedIntegers2.Count )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers2.Count(" +
                        command.RelatedIntegers2.Count + ") != " + command.RelatedEntityIDs.Count + " in GameCommand_SetTargetList!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            #endregion            

            int currentOverallTargetIndex = 0;
            //Parallel struct-enumerator walk over the three ChainLists, matching the count-equal
            //checks above. The previous form CopyTo'd each into a fresh int[] so it could index
            //three lists in lockstep -- 3 array allocs per Execute call.
            ChainList<int>.Enumerator eidsEnum = command.RelatedEntityIDs.GetEnumerator();
            ChainList<int>.Enumerator riEnum = command.RelatedIntegers.GetEnumerator();
            ChainList<int>.Enumerator ri2Enum = command.RelatedIntegers2.GetEnumerator();
            while ( eidsEnum.MoveNext() && riEnum.MoveNext() && ri2Enum.MoveNext() )
            {
                int squadID = eidsEnum.Current;
                int systemIndex = riEnum.Current;
                int countOfTargets = ri2Enum.Current;
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( squadID );
                #region Validate Entity
                if ( entity == null || entity.ToBeRemovedAtEndOfThisFrame )
                {
                    //skip this many targets
                    currentOverallTargetIndex += countOfTargets;
                    continue;
                }
                #endregion
                #region Validate systemIndex
                if ( systemIndex < 0 || systemIndex >= entity.Systems.Count )
                {
                    //don't complain about this; it's a GameEntity that got reprovisioned, and this is a safe skip check.
                    //this was only relevant when the targeting logic code was new and we weren't sure it worked yet.
                    //ArcenDebugging.ArcenDebugLog( "systemIndex " +
                    //    systemIndex + " does not work with actual system count of " + entity.Systems.Count + " in GameCommand_SetTargetList for " + 
                    //    entity.TypeData.InternalName + "!" + command.WriteToStringInefficient( true ), Verbosity.ShowAsError );
                    //skip this many targets
                    currentOverallTargetIndex += countOfTargets;
                    continue;
                }
                #endregion
                EntitySystem system = entity.Systems[systemIndex];

                system.ReplacePotentialTargetsByPriorityForSystemWithNewData( command.RelatedIntegers4, countOfTargets, ref currentOverallTargetIndex, "GCSetTarget" );
                //UnityEngine.Debug.Log( "system" + system.TypeData.InternalName + "." + system.targetPriorityList.Count + "/" + countOfTargets );
            }
        }
    }

    public class GameCommand_SetFRDTarget : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int timeUntilFRDDataExpires = World_AIW2.Instance.GameSecond + ExternalConstants.Instance.TimeForFRDChoicesToStay;
            //command.RelatedEntityIDs = List<Int64>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE );
            //command.RelatedIntegers = List<int>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE ); //systemIDs
            //command.RelatedIntegers4 = List<Int64>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE ); //targets
            //command.RelatedIntegers2 = List<int>.Create_WillNeverBeGCed( MAX_SYSTEMS_TO_CALCULATE ); //target priorities
            //UnityEngine.Debug.Log( "GameCommand_SetFRDTarget!" );
            #region Inequality Checks (Bad Data)
            if ( command.RelatedEntityIDs.Count != command.RelatedIntegers.Count )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers.Count(" +
                        command.RelatedIntegers.Count + ") != " + command.RelatedEntityIDs.Count + " in GameCommand_SetFRDTarget!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedEntityIDs.Count != command.RelatedIntegers4.Count )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers4.Count(" +
                        command.RelatedIntegers4.Count + ") != " + command.RelatedEntityIDs.Count + " in GameCommand_SetFRDTarget!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedEntityIDs.Count != command.RelatedIntegers2.Count )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers2.Count(" +
                        command.RelatedIntegers2.Count + ") != " + command.RelatedEntityIDs.Count + " in GameCommand_SetFRDTarget!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            #endregion

            int actualFRDAssignements = 0;
            //Walk the four parallel ChainLists with their struct enumerators in lockstep. The
            //previous version CopyTo'd each into a fresh int[] just to index in parallel,
            //allocating 4 arrays per Execute call (~10KB per call with 620 entries seen in the
            //profile). The count-equality checks above guarantee the four MoveNexts agree.
            ChainList<int>.Enumerator eidsEnum = command.RelatedEntityIDs.GetEnumerator();
            ChainList<int>.Enumerator riEnum = command.RelatedIntegers.GetEnumerator();
            ChainList<int>.Enumerator ri2Enum = command.RelatedIntegers2.GetEnumerator();
            ChainList<int>.Enumerator ri4Enum = command.RelatedIntegers4.GetEnumerator();
            while ( eidsEnum.MoveNext() && riEnum.MoveNext() && ri2Enum.MoveNext() && ri4Enum.MoveNext() )
            {
                int squadID = eidsEnum.Current;
                int systemIndex = riEnum.Current;
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( squadID );
                #region Validate Entity
                if ( entity == null || entity.ToBeRemovedAtEndOfThisFrame )
                    continue;
                #endregion
                #region Validate systemIndex
                if ( systemIndex < 0 || systemIndex >= entity.Systems.Count )
                {
                    //ArcenDebugging.ArcenDebugLog( "systemIndex " +
                    //    systemIndex + " does not work with actual system count of " + entity.Systems.Count + " in GameCommand_SetFRDTarget for " +
                    //    entity.TypeData.InternalName + "!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    continue;
                }
                #endregion
                EntitySystem system = entity.Systems[systemIndex];
                //set the new data
                system.CurrentFRDTarget = LazyLoadSquadWrapper.Create( ri4Enum.Current, false, "GCSetFRDTarget" );
                //and set the timer it should stay
                if ( system.CurrentFRDTarget.GetPrimaryKeyID() > 0 )
                {
                    system.TimeFRDTargetExpires = timeUntilFRDDataExpires;
                    system.CurrentFRDPriority = (Int16)ri2Enum.Current;
                    actualFRDAssignements++;
                }
                else
                {
                    system.TimeFRDTargetExpires = 0;
                    system.CurrentFRDPriority = 0;
                }
            }

            if ( actualFRDAssignements > 0 ) { }
            //UnityEngine.Debug.Log( command.RelatedEntityIDs.Count + " FRD assignments attempted, " + actualFRDAssignements + " actually assigned." );
        }
    }
}
