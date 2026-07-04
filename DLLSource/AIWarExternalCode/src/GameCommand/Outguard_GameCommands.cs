using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_QueueOutguard : BaseGameCommand
    {
        public static void QueueCommand(OutguardGroupData Group, Planet ToSpawnOn, ArcenPoint SpawnPoint, Faction ChargeFaction)
        {
            GameCommand cmd = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByNameOrNullIfNotFound( "GameCommand_QueueOutguard" ), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            cmd.RelatedString = Group.InternalName;
            cmd.RelatedIntegers.Add( World_AIW2.Instance.GameSecond + Group.SpawnDelay );
            cmd.PlanetOrderWasIssuedFrom = ToSpawnOn.Index;
            cmd.RelatedPoints.Add( SpawnPoint );
            cmd.RelatedFactionIndex = ChargeFaction.FactionIndex;
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), cmd, true );
        }

        public override void Execute( GameCommand Command, ArcenClientOrHostSimContextCore Context )
        {
            bool debug=false;
            if (debug)
                ArcenDebugging.ArcenDebugLogSingleLine("GameCommand_QueueOutguard.Execute() called.", Verbosity.DoNotShow);

            for ( int i = 0; i < OutguardFactionBaseInfo.Instance.QueuedRequests.Count; i++ )
            {
                if ( OutguardFactionBaseInfo.Instance.QueuedRequests[i].Group.InternalName == Command.RelatedString )
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("GameCommand_QueueOutguard.Execute() early out already queued.", Verbosity.DoNotShow);

                    //Accidentally the same group was sumoned twice - probably host and client clicking at basically the same time
                    return;
                }
            }
            OutguardFactionBaseInfo.Instance.QueuedRequests.Add( OutguardSpawnRequest.GetFromPoolOrCreate( Command ) );

            if (debug)
                ArcenDebugging.ArcenDebugLogSingleLine(string.Format("GameCommand_QueueOutguard.Execute() QueuedRequests is now {0}.", OutguardFactionBaseInfo.Instance.QueuedRequests.Count), Verbosity.DoNotShow);
        }
    }
}
