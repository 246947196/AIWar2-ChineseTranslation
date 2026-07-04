
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

                        Buffer.Add( "此移民再次返回您的星系，在从 " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( " 旅程后，它在您的领地内安全。它将在此星球上协助击退攻击，然后在 " ).AddSecondsRemaining( timeLeft ).Add( " 后传送离开。任何伤害都会中断其传送过程，它需要完全修复后才能重新开始。" );
                    }
                    else
                    {
                        // On a neutral or hostile planet, but we know where our old friends are.
                        int timeLeft = migrantImp.MigrantShouldStayOnHostilePlanetForThisMuchLonger( RelatedEntityOrNull );

                        Planet nextPlanet = migrantImp.GetNextPlanetToMoveToToReachFriendlies( RelatedEntityOrNull, friendlyTerritory );

                        Buffer.Add( "此移民再次返回您的星系，无需引导即可到达您的领地。它在 " ).AddPlanetNameFormated( originPlanet, false ).EndColor( " 生成。" );
                        if ( timeLeft > 0 )
                        {
                                    Buffer.StartColor( "66ffef" ).Add( " 并将移动到 " ).AddPlanetNameFormated( nextPlanet, false ).EndColor().StartColor( "66ffef" ).Add( "，耗时 " ).AddSecondsRemaining( timeLeft ).EndColor();
                        }
                        else
                        {
                                    Buffer.StartColor( "66ffef" ).Add( " 并正在移动到 " ).AddPlanetNameFormated( nextPlanet, false ).EndColor();
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

                        Buffer.Add( "此移民已安全到达您的领地，自从从 " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( " 开始旅程以来一直安全。它将在此星球上协助击退攻击，然后在 " ).AddSecondsRemaining( timeLeft ).Add( " 后传送离开。任何伤害都会中断其传送过程，它需要完全修复后才能重新开始。" );
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
                                Buffer.StartColor( "66ffef" ).Add( "由于您在此星球上的军事优势，您正在引导此移民的旅程，缓慢将其驱向友好空间。它在 " ).AddPlanetNameFormated( originPlanet, false ).EndColor( " 生成。" );
                                if ( timeLeft > 0 )
                                {
                            Buffer.StartColor( "66ffef" ).Add( " 并将移动到 " ).AddPlanetNameFormated( nextPlanet, false ).EndColor().StartColor( "66ffef" ).Add( "，耗时 " ).AddSecondsRemaining( timeLeft ).EndColor();
                                }
                                else
                                {
                            Buffer.StartColor( "66ffef" ).Add( " 并正在移动到 " ).AddPlanetNameFormated( nextPlanet, false ).EndColor();
                                }
                            }
                            else
                            {
                                // On a neutral or hostile planet, with recent ally control
                                // The Migrant is currently running and will not respond.
                                Buffer.StartColor( "e79553" ).Add( "此移民是星系新成员。由于最近受到敌对势力威胁，它正试图逃往 " ).AddPlanetNameFormated( currentMovingToOrNull, false ).EndColor().Add( "。如果您在它再次逃跑前获得该星球的军事优势，您将能够慢慢将其引导回您的领地进行安全保管。它最初在 " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( " 生成。" ).EndColor();
                            }
                        }
                        else
                        {
                            // On a neutral or hostile planet, no idea where to go.
                            Buffer.StartColor( "e79553" ).Add( "此移民是星系新成员，正在随机游荡。如果您获得此星球的军事优势，您将能够慢慢将其引导回您的领地进行安全保管。它最初在 " ).AddPlanetNameFormated( originPlanet, false ).EndColor().Add( " 生成。" ).EndColor();
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
