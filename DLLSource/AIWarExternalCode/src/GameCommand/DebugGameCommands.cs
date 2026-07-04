using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_Debug_RevealAll : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            bool isConsideredCheat = World_AIW2.Instance.Setup.GetBoolBySetting( "RevealingMapDetailsIsConsideredCheating" );
            if ( isConsideredCheat )
                World.Instance.HaveDoneAnyCheatingThatBlocksAchievemets = true;
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( isConsideredCheat )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "Cheat: Watch Entire Map.", ChatType.LogToCentralChat, null );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( "Lifestyle Choice: Watch Entire Map.", ChatType.LogToCentralChat, null );
            }

            Galaxy galaxy = World_AIW2.Instance.CurrentGalaxy;
            if ( galaxy == null )
                return;
            World_AIW2.Instance.Debug_JustShowEverything = true;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                planet.IntelLevel = PlanetIntelLevel.PermanentlyWatched;
                planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
            }

            foreach ( Faction fac in World_AIW2.Instance.Factions )
            {
                if ( !fac.HasBeenSeenByPlayer )
                    fac.HasBeenSeenByPlayer = true;
            }
        }
    }

    public class GameCommand_Debug_ExploreAll : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            bool isConsideredCheat = World_AIW2.Instance.Setup.GetBoolBySetting( "RevealingMapDetailsIsConsideredCheating" );
            if ( isConsideredCheat )
                World.Instance.HaveDoneAnyCheatingThatBlocksAchievemets = true;
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( isConsideredCheat )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "Cheat: Explore Entire Map.", ChatType.LogToCentralChat, null );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( "Lifestyle Choice: Explore Entire Map.", ChatType.LogToCentralChat, null );
            }
            //            World_AIW2.Instance.Debug_JustShowEverything = true;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                planet.IntelLevel = PlanetIntelLevel.ExploredByNaturalMeans;
                planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
            }

            foreach ( Faction fac in World_AIW2.Instance.Factions )
            {
                if ( !fac.HasBeenSeenByPlayer )
                    fac.HasBeenSeenByPlayer = true;
            }
        }
    }
}
