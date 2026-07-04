using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    //Note: these all happen the same way in single player, and are equally needed there because
    //      where we pull the data from is then consistent (and in the game).  But this was originally
    //      added for purposes of multiplayer.

    public abstract class GameCommand_SyncPersonalSetting : BaseGameCommand
    {
        protected bool GetBaseValues( GameCommand command, out PlayerAccount forAccount, out ArcenSetting setting )
        {
            forAccount = World.Instance.GetPlayerAccountByPrimaryID( command.RelatedMagnitude );
            if ( forAccount == null )
            {
                setting = null;
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_SyncPersonalSetting: Could not find player account with PKID " + command.RelatedMagnitude, Verbosity.ShowAsError );
                return false;
            }
            if ( !ArcenSettingTable.Instance.NetworkAndGameSyncedRows_ByName.ContainsKey( command.RelatedString2 ) )
            {
                setting = null;
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_SyncPersonalSetting: Could not find personal setting with serialization_name '" + command.RelatedString2 + "'", Verbosity.ShowAsError );
                return false;
            }
            setting = ArcenSettingTable.Instance.NetworkAndGameSyncedRows_ByName[command.RelatedString2];
            return true;
        }
    }

    public class GameCommand_SyncPersonalBool : GameCommand_SyncPersonalSetting
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( base.GetBaseValues( command, out PlayerAccount forAccount, out ArcenSetting setting ) )
            {
                forAccount.SetNetworkAttachedBoolBySetting( setting, command.RelatedBool );
            }
        }
    }

    public class GameCommand_SyncPersonalInt : GameCommand_SyncPersonalSetting
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( base.GetBaseValues( command, out PlayerAccount forAccount, out ArcenSetting setting ) )
            {
                forAccount.SetNetworkAttachedIntBySetting( setting, command.RelatedIntegers.First );
            }
        }
    }

    public class GameCommand_SyncPersonalFloat : GameCommand_SyncPersonalSetting
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( base.GetBaseValues( command, out PlayerAccount forAccount, out ArcenSetting setting ) )
            {
                forAccount.SetNetworkAttachedFloatBySetting( setting, command.RelatedFInts.First.ToFloatNonSim() );
            }
        }
    }

    public class GameCommand_SyncPersonalFInt : GameCommand_SyncPersonalSetting
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( base.GetBaseValues( command, out PlayerAccount forAccount, out ArcenSetting setting ) )
            {
                forAccount.SetNetworkAttachedFIntBySetting( setting, command.RelatedFInts.First );
            }
        }
    }

    public class GameCommand_SyncPersonalString : GameCommand_SyncPersonalSetting
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( base.GetBaseValues( command, out PlayerAccount forAccount, out ArcenSetting setting ) )
            {
                forAccount.SetNetworkAttachedStringBySetting( setting, command.RelatedString );
            }
        }
    }
}
