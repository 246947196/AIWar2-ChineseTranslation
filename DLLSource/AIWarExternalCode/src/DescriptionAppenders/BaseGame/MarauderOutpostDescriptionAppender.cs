using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MarauderOutpostDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            if ( MarauderFactionBaseInfo.AllMarauderFactions.Count <= 0 )
                return; //this is a valid condition if the game has just been loaded
            
            MarauderOutpostRaiderPerUnitBaseInfo unitData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MarauderOutpostRaiderPerUnitBaseInfo>();
            if ( unitData == null )
            {
                Buffer.Add( "Null unitData!" );
                return;
            }
            Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                Buffer.Add( "Null faction!" );
                return;
            }
            MarauderFactionBaseInfo factionData = facOrNull.TryGetExternalBaseInfoAs<MarauderFactionBaseInfo>();
            if ( factionData == null || factionData.RaidersPerOutpost == null )
            {
                Buffer.Add( "Null MarauderFactionBaseInfo!" );
                return;
            }

            if ( factionData.NoMark3Outposts )
                return;
            if ( (factionData.PlayerAllied || factionData.aiAllied) && factionData.NoMark3OutpostsOnAlliedPlanets )
                Buffer.Add( "Allied (ie human friendly or ai friendly) Marauder Outposts cannot be Level 3 on a planet with an Allied Command Station. " );

            if ( RelatedEntityOrNull.CurrentMarkLevel == 3 )
            {
                Buffer.Add( "This outpost is supporting " + factionData.RaidersPerOutpost.Display[RelatedEntityOrNull.PrimaryKeyID] + " of its allowed " + factionData.MaxRaidersPerMark3Outpost + " raiders. " );
            }
            else
            {
                if ( RelatedEntityOrNull.Planet.GetControllingFactionType() == FactionType.Player && (factionData.PlayerAllied && factionData.NoMark3OutpostsOnAlliedPlanets) )
                {
                    Buffer.Add( "Allied marauder outposts on planets you own cannot reach their full potential to build offensive fleets; they will also build fewer outposts on that planet. They need their own planets to fully upgrade." );
                }
                else
                {
                    Buffer.Add( "This outpost will begin to produce Raiders once it reaches Mark 3. Raiders are powerful Frigates the Marauders will use to conquer new planets. " );
                }
            }
        }
    }
}
