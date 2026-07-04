using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_ModderCommand : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //no running these on clients!

            Faction faction = command.GetRelatedFaction();
            if ( faction == null )
                ArcenDebugging.ArcenDebugLog( "Modder command issued with null faction. ModderCommands require a faction as one of their arguments" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
            if ( command.RelatedModdableCommandCode == null )
                ArcenDebugging.ArcenDebugLog( "Modder command issued with null RelatedModdableCommandCode.  For faction:" + faction.GetDisplayName(), Verbosity.ShowAsError );
            try
            {
                faction.DeepInfo.ModdableGameCommandExecution( command.RelatedModdableCommandCode, command.RelatedString, command.RelatedIntegers, context.GetHostOnlyContext() );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Modder command had error.  Faction:" + faction.GetDisplayName() + " command code: " + command.RelatedModdableCommandCode +
                    "error:\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
