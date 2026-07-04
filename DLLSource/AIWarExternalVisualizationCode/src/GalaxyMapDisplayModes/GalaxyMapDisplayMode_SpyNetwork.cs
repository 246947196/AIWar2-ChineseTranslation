using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_SpyNetwork : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldReplaceNormalPlanetTooltip()
        {
            return true;
        }
        public override void WriteToPlanetTooltip( Planet planet, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            Buffer.Add( "\nSpies Present: " ).Add( this.GetHumanSpyCount( planet ) );

            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction == null || ownerFaction.Type == FactionType.NaturalObject )
            {}
            else if ( ownerFaction.GetIsFriendlyToLocalFaction() )
            {
                Buffer.StartColor( "5d9aff" ).Add( "\nAllied Territory" ).EndColor().Add( "\n" );
                Buffer.Add( "<size=80%>This planet is controlled by you or one of your allies, so is a natural safe haven from the AI Reserves.  " );
                return;
            }

            if ( planet.GameSecondLastWatchedByCommandStation > 0 && ( World_AIW2.Instance.GameSecond - planet.GameSecondLastWatchedByCommandStation < 3) )
                Buffer.Add( "\nRecon: This planet is under surveillance by a nearby command station or stationary spy cradle." );

            if ( planet.IntelLevel == PlanetIntelLevel.CurrentlyWatched )
            {
                Buffer.Add( "\nWatched" );
                Buffer.Add( ": We are seeing current data for this planet. " );
            }
            if ( planet.IntelLevel == PlanetIntelLevel.WatchedUntilReconquered )
            {
                if ( planet.GetControllingFactionType() == FactionType.AI )
                {
                    Buffer.Add( "\nWatched" );
                    Buffer.Add( ": We will see current data unless the AI loses and then reconquers this planet. " );
                }
                else
                {
                    Buffer.Add( "\nWatched" );
                    Buffer.Add( ": We will see current data unless the AI reconquers this planet. " );
                }
            }
            if ( planet.IntelLevel == PlanetIntelLevel.PermanentlyWatched )
            {
                Buffer.Add( "\nPermanently Watched" );
                Buffer.Add( ": We will always see current data for this planet. " );
            }
        }

        #region GetHumanSpyCount
        public int GetHumanSpyCount( Planet planet )
        {
            int spyCount = 0;
            foreach ( Faction fac in World_AIW2.Instance.EmpireStylePlayerFactions )
            {
                PlanetFaction pFac = planet.GetPlanetFactionForFaction( fac );
                if ( pFac == null )
                    continue;
                foreach ( GameEntity_Squad entity in pFac.Entities.Squads( "Spy" ) )
                {
                    spyCount++;
                }
            }
            return spyCount;
        }
        #endregion

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( EntityToSkip != null && EntityToSkip.TypeData.IsMobile )
                EntityToSkip = null;

            bool evaluator( GameEntity_Squad entity )
            {
                if (entity.TypeData.GetHasTag("DSAA"))
                {
                    if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                        return true;
                }
                if (entity.TypeData.GetHasTag("Spy"))
                {
                    if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                        return true;
                }
                if ( OnlyShowThingsThatShouldBeInFarZoom )
                {
                    if ( !entity.GetIsSelected() )
                        return false;
                }
                if ( entity.TypeData.DrawInGalaxyView || entity.TypeData.GetHasTag( "ShowsOnNormalDisplayMode" ) || entity.TypeData.GetHasTag( "ProgressReducer" ) || entity.TypeData.GetHasTag( "Capturable" ) )
                {
                    if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                        return true;
                }
                return false;
            }

            int currentIndex = 0;
            bool canSeeEnemies = (planet.IntelLevel > PlanetIntelLevel.Unexplored);
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( EntityToSkip != null )
                {
                    if ( entity == EntityToSkip )
                        continue;
                }
                if ( !canSeeEnemies )
                {
                    if ( entity.PlanetFaction == null )
                        continue;
                    if ( entity.GetIsHostileToLocalFaction_Safe() )
                        continue;
                }

                if (!evaluator(entity))
                    continue;

                ArrayToFill[currentIndex] = entity;
                currentIndex++;
                if ( currentIndex >= ArrayToFill.Length )
                    break;

            }
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            int spiesPresent = this.GetHumanSpyCount( planet );

            //LEFT ONLY
            if ( spiesPresent > 0 )
            {
                if ( spiesPresent > 1 )
                    LeftBuffer.StartColor( "ff5f27" ).Add( spiesPresent ).Add( " SPIES" ).EndColor().Add( "\n" );
                else
                    LeftBuffer.StartColor( "3ac0ff" ).Add( "SPY" ).EndColor().Add( "\n" );
            }
            if ( planet.IntelLevel == PlanetIntelLevel.PermanentlyWatched )
                LeftBuffer.StartColor( "3a9bff" ).Add( "NANITES" ).EndColor().Add( "\n" );
            else if ( planet.IntelLevel == PlanetIntelLevel.WatchedUntilReconquered )
                LeftBuffer.StartColor( "3aa0ff" ).Add( "NANITES" ).EndColor().Add( "\n" );
            else if ( planet.IntelLevel == PlanetIntelLevel.CurrentlyWatched )
            {
                if ( planet.GameSecondLastWatchedByCommandStation > 0 && ( World_AIW2.Instance.GameSecond - planet.GameSecondLastWatchedByCommandStation < 3 ) )
                    LeftBuffer.StartColor( "3affd3" ).Add( "RECON" ).EndColor().Add( "\n" );
                else
                    LeftBuffer.StartColor( "3af3ff" ).Add( "WATCHED" ).EndColor().Add( "\n" );
            }

            bool dsaaHere = false;
            foreach ( GameEntity_Squad e in planet.Squads( "DSAA" ) )
            {
                if (e.TypeData.GetHasTag("DSAA"))
                {
                    dsaaHere = true;
                    break;
                }
            }

            if (dsaaHere)
            {
                LeftBuffer.StartColor( Arcen.Universal.ColorMath.Gold ).Add( "DANGER" ).EndColor().Add( "\n" );
            }
        }
    }
}
