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

            Buffer.Add( "\n间谍数量：" ).Add( this.GetHumanSpyCount( planet ) );

            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction == null || ownerFaction.Type == FactionType.NaturalObject )
            {}
            else if ( ownerFaction.GetIsFriendlyToLocalFaction() )
            {
                Buffer.StartColor( "5d9aff" ).Add( "\n盟军领地" ).EndColor().Add( "\n" );
                Buffer.Add( "<size=80%>这个星球由你或你的盟友控制，因此是AI预备队的安全避风港。  " );
                return;
            }

            if ( planet.GameSecondLastWatchedByCommandStation > 0 && ( World_AIW2.Instance.GameSecond - planet.GameSecondLastWatchedByCommandStation < 3) )
                Buffer.Add( "\n侦察：这个星球正受到附近指挥所或固定间谍摇篮的监视。" );

            if ( planet.IntelLevel == PlanetIntelLevel.CurrentlyWatched )
            {
                Buffer.Add( "\n监视中" );
                Buffer.Add( "：我们正在查看这个星球的当前数据。 " );
            }
            if ( planet.IntelLevel == PlanetIntelLevel.WatchedUntilReconquered )
            {
                if ( planet.GetControllingFactionType() == FactionType.AI )
                {
                    Buffer.Add( "\n监视中" );
                    Buffer.Add( "：除非AI失去并重新夺回这个星球，否则我们将继续看到当前数据。 " );
                }
                else
                {
                    Buffer.Add( "\n监视中" );
                    Buffer.Add( "：除非AI重新夺回这个星球，否则我们将继续看到当前数据。 " );
                }
            }
            if ( planet.IntelLevel == PlanetIntelLevel.PermanentlyWatched )
            {
                Buffer.Add( "\n永久监视" );
                Buffer.Add( "：我们将始终看到这个星球的当前数据。 " );
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
                    LeftBuffer.StartColor( "ff5f27" ).Add( spiesPresent ).Add( " 间谍" ).EndColor().Add( "\n" );
                else
                    LeftBuffer.StartColor( "3ac0ff" ).Add( "间谍" ).EndColor().Add( "\n" );
            }
            if ( planet.IntelLevel == PlanetIntelLevel.PermanentlyWatched )
                LeftBuffer.StartColor( "3a9bff" ).Add( "纳米机器人" ).EndColor().Add( "\n" );
            else if ( planet.IntelLevel == PlanetIntelLevel.WatchedUntilReconquered )
                LeftBuffer.StartColor( "3aa0ff" ).Add( "纳米机器人" ).EndColor().Add( "\n" );
            else if ( planet.IntelLevel == PlanetIntelLevel.CurrentlyWatched )
            {
                if ( planet.GameSecondLastWatchedByCommandStation > 0 && ( World_AIW2.Instance.GameSecond - planet.GameSecondLastWatchedByCommandStation < 3 ) )
                    LeftBuffer.StartColor( "3affd3" ).Add( "侦察" ).EndColor().Add( "\n" );
                else
                    LeftBuffer.StartColor( "3af3ff" ).Add( "监视中" ).EndColor().Add( "\n" );
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
                LeftBuffer.StartColor( Arcen.Universal.ColorMath.Gold ).Add( "危险" ).EndColor().Add( "\n" );
            }
        }
    }
}
