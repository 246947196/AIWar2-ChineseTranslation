
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class MigrantDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            if ( RelatedEntityOrNull.PlanetFaction.Faction.SpecialFactionData.InternalName != "MigrantFleets" )
                return; // Skip if not migrant.

            Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
            MigrantFleetsFactionBaseInfo migrantImp = faction.TryGetExternalBaseInfoAs<MigrantFleetsFactionBaseInfo>();

            if ( migrantImp == null || !migrantImp.humanAlly )
                return; // Skip if null or not allied to us.

            Planet originPlanet = migrantImp.GetMigrantOriginPlanetOrNull( RelatedEntityOrNull );
            Planet currentMovingToOrNull = RelatedEntityOrNull.Orders.GetFinalDestinationOrNull();
            List<Planet> friendlyTerritory = Planet.GetTemporaryPlanetList( "Migrant-DescApp-friendlyTerritory", 10f );
            if ( friendlyTerritory == null ) //blocked for teardown/shutdown; bail
                return;

            try
            {
                migrantImp.GetFriendlyTerritory( friendlyTerritory );

                if ( migrantImp.GetMigrantHasBeenHereBefore( RelatedEntityOrNull ) )
                {
                    // An old friend.
                    if ( friendlyTerritory.Contains( RelatedEntityOrNull.Planet ) )
                    {
                        // On a friendly planet, waiting to warp out.
                        int timeLeft = migrantImp.MigrantSafeTimeLeftUntilWarpingOutOfGalaxy( RelatedEntityOrNull, migrantImp.GetButDoNotStartMigrantSafetyTimer( RelatedEntityOrNull ) );

                        Buffer.Add( "This Migrant has returned to your galaxy yet again, and is safe in your territory after its journey from " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( ". It will assist in repelling attacks on this planet before warping out in " ).AddSecondsRemaining( timeLeft ).Add( ". Any damage it takes will interrupt its warp process, and it will need to fully repair before beginning anew." );
                    }
                    else
                    {
                        // On a neutral or hostile planet, but we know where our old friends are.
                        int timeLeft = migrantImp.MigrantShouldStayOnHostilePlanetForThisMuchLonger( RelatedEntityOrNull );

                        Planet nextPlanet = migrantImp.GetNextPlanetToMoveToToReachFriendlies( RelatedEntityOrNull, friendlyTerritory );

                        Buffer.Add( "This Migrant has returned to your galaxy yet again, and needs no guidance to reach your territory. It spawned in on " ).AddPlanetNameFormated( originPlanet, false ).EndColor();
                        if ( timeLeft > 0 )
                        {
                            Buffer.StartColor( "66ffef" ).Add( " and will be moving to " ).AddPlanetNameFormated( nextPlanet, false ).EndColor().StartColor( "66ffef" ).Add( " in " ).AddSecondsRemaining( timeLeft ).EndColor();
                        }
                        else
                        {
                            Buffer.StartColor( "66ffef" ).Add( " and is moving to " ).AddPlanetNameFormated( nextPlanet, false ).EndColor();
                        }
                    }
                }
                else
                {
                    // A new face.
                    if ( friendlyTerritory.Contains( RelatedEntityOrNull.Planet ) )
                    {
                        // On a friendly planet, waiting to warp out.
                        int timeLeft = migrantImp.MigrantSafeTimeLeftUntilWarpingOutOfGalaxy( RelatedEntityOrNull, migrantImp.GetButDoNotStartMigrantSafetyTimer( RelatedEntityOrNull ) );

                        Buffer.Add( "This Migrant has managed to safely reach your territory, and is now safe since starting its journey from " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( ". It will assist in repelling attacks on this planet before warping out in " ).AddSecondsRemaining( timeLeft ).Add( ". Any damage it takes will interrupt its warp process, and it will need to fully repair before beginning anew." );
                    }
                    else
                    {
                        int timeLeft = migrantImp.MigrantShouldStayOnHostilePlanetForThisMuchLonger( RelatedEntityOrNull );

                        if ( migrantImp.MigrantKnowsWhereToGoNext( RelatedEntityOrNull ) )
                        {
                            Planet nextPlanet = migrantImp.GetNextPlanetToMoveToToReachFriendlies( RelatedEntityOrNull, friendlyTerritory );

                            if ( currentMovingToOrNull == null || currentMovingToOrNull == nextPlanet )
                            {
                                // On a neutral or hostile planet, guided by allies.
                                Buffer.StartColor( "66ffef" ).Add( "Due to your military advantage on this planet, you are guiding this Migrant on its journey, slowly herding it towards friendly space. It spawned in on " ).AddPlanetNameFormated( originPlanet, false ).EndColor();
                                if ( timeLeft > 0 )
                                {
                                    Buffer.StartColor( "66ffef" ).Add( " and will be moving to " ).AddPlanetNameFormated( nextPlanet, false ).EndColor().StartColor( "66ffef" ).Add( " in " ).AddSecondsRemaining( timeLeft ).EndColor();
                                }
                                else
                                {
                                    Buffer.StartColor( "66ffef" ).Add( " and is moving to " ).AddPlanetNameFormated( nextPlanet, false ).EndColor();
                                }
                            }
                            else
                            {
                                // On a neutral or hostile planet, with recent ally control
                                // The Migrant is currently running and will not respond.
                                Buffer.StartColor( "e79553" ).Add( "This Migrant is new to this galaxy. Due to being recently threatened by hostiles, it is currently attempting to flee to " ).AddPlanetNameFormated( currentMovingToOrNull, false ).EndColor().Add( ". If you gain a military advantage on that planet before it flees again, you will be able to slowly guide it back to your territory for safe keeping. It originally warped in on " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( "." ).EndColor();
                            }
                        }
                        else
                        {
                            // On a neutral or hostile planet, no idea where to go.
                            Buffer.StartColor( "e79553" ).Add( "This Migrant is new to this galaxy and is randomly wandering around. If you gain a military advantage on this planet, you will be able to slowly guide it back to your territory for safe keeping. It originally warped in on " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( " ." ).EndColor();
                        }
                    }
                }
            }
            finally
            {
                Planet.ReleaseTemporaryPlanetList( friendlyTerritory );
            }
        }
    }
}
