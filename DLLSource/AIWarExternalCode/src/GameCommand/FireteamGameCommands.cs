using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_ResetFireteams : BaseGameCommand
    {
        //This takes as arguments a faction and undoes all the fireteams for that faction
        //High impact. Used as a testing tool for fireteam logic,
        //though vassals could perhaps use this too?
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            Faction faction = command.GetRelatedFaction();
            if ( faction == null )
                throw new Exception( "no faction passed in for resetting fireteams" );
            foreach ( GameEntity_Squad entity in faction.Squads() )
            {
                entity.FireteamId = -1;
            }
        }
    }
}
