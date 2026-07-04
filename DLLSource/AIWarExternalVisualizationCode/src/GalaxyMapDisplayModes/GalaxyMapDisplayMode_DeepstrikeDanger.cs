using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_DeepstrikeDanger : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldReplaceNormalPlanetTooltip()
        {
            return true;
        }
        public override void WriteToPlanetTooltip( Planet planet, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction == null || ownerFaction.Type == FactionType.NaturalObject )
            {
                Buffer.StartColor( "5d9aff" ).Add( "\n深袭安全区" ).EndColor().Add( "\n" );
                Buffer.Add( "<size=80%>这个星球不属于任何人，因此是AI预备队的安全避风港。  " );
            }
            else if ( ownerFaction.GetIsFriendlyToLocalFaction() )
            {
                Buffer.StartColor( "5d9aff" ).Add( "\n盟军领地" ).EndColor().Add( "\n" );
                Buffer.Add( "<size=80%>这个星球由你或你的盟友控制，因此是AI预备队的安全避风港。  " );
            }
            else
            {
                if ( planet.IsEligibleForDeepStrike )
                {
                    bool isOnExtraAlert = planet.WillBeOnExtraDeepstrikeAlertUntilGameSecond >= World_AIW2.Instance.GameSecond;

                    Buffer.StartColor( "ff3939" ).Add( "\n深袭危险区" ).EndColor().Add( "\n" );
                    Buffer.Add( "<size=80%>这个星球深入AI领地（远离你的星球），因此<color=#ff3939>AI会对任何不在运输舰内的部队感到非常警觉</color>。  " );
                    Buffer.Add( "AI预备队子派系<color=#ff3939>将开始部署紧急部队</color>，如果你在这里停留足够长的时间。他们没有无限的资源，需要时间积累，但不要低估他们反应的危险。  " );
                    Buffer.Add( "\n\n最好<color=#ff3939>快速进入</color>，根据需要造成伤害，占领或入侵任何重要目标，然后<color=#ff3939>迅速撤退</color>。或者，尽快摧毁AI指挥所和防御岗哨以取消任何反击。</size>" );

                    if ( isOnExtraAlert )
                    {
                        Buffer.StartColor( "ff2a7f" ).Add( "\n\n<size=90%>深袭全面警报！</size>" ).EndColor().Add( "\n" );
                        Buffer.Add( "<size=70%>这个星球注意到人类带来了未卸载的运输舰，因此将在接下来的 " )
                            .AddHoursAndMinutes( planet.WillBeOnExtraDeepstrikeAlertUntilGameSecond - World_AIW2.Instance.GameSecond ).Add( " 内对深袭保持高度警戒。  " );
                        Buffer.Add( "在此高度警戒期间，这个星球将监控自身及相邻星球上人类运输舰的进出。你必须击败这个星球，或撤退到至少两个跳跃距离之外，以避免AI预备队的部署。</size>" );
                    }
                }
                else
                {
                    Buffer.StartColor( "5d9aff" ).Add( "\n深袭安全区" ).EndColor().Add( "\n" );
                    Buffer.Add( "<size=80%>这个星球要么不受AI控制，要么离你的领地足够近，因此<color=#5d9aff>如果在这里卸载运输舰，AI预备队不会追击你</color>。  " );
                }
            }
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            GalaxyMapDisplayMode_Normal.ShowNormalIcons( planet, EntityToSkip, ArrayToFill, OnlyShowThingsThatShouldBeInFarZoom );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            if ( planet.IsEligibleForDeepStrike )
            {
                bool isOnExtraAlert = planet.WillBeOnExtraDeepstrikeAlertUntilGameSecond >= World_AIW2.Instance.GameSecond;
                string colorToUse = isOnExtraAlert ? "ff2a7f" : "ff3939";
                AIReservesFactionBaseInfo aiReservesInfo = AIReservesFactionBaseInfo.Instance;
                if ( aiReservesInfo.AbsorbShipsMode )
                {
                    if ( isOnExtraAlert )
                        colorToUse = "ff6c39";
                }

                //LEFT ONLY
                LeftBuffer.StartColor( colorToUse ).Add( "深袭" ).EndColor().Add( "\n" );
                if ( isOnExtraAlert )
                {
                    if ( aiReservesInfo.AbsorbShipsMode )
                        LeftBuffer.StartColor( colorToUse ).Add( "残留" ).EndColor().Add( "\n" );
                    else
                        LeftBuffer.StartColor( colorToUse ).Add( "全面" ).EndColor().Add( "\n" );
                }

                //RIGHT ONLY
                RightBuffer.StartColor( colorToUse ).Add( "突袭" ).EndColor().Add( "\n" );
                if ( isOnExtraAlert )
                    RightBuffer.StartColor( colorToUse ).Add( "警报" ).EndColor().Add( "\n" );
            }
        }
    }
}
