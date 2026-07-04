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
                Buffer.Add( "空数据！" );
                return;
            }
            Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                Buffer.Add( "空阵营！" );
                return;
            }
            MarauderFactionBaseInfo factionData = facOrNull.TryGetExternalBaseInfoAs<MarauderFactionBaseInfo>();
            if ( factionData == null || factionData.RaidersPerOutpost == null )
            {
                Buffer.Add( "空MarauderFactionBaseInfo！" );
                return;
            }

            if ( factionData.NoMark3Outposts )
                return;
            if ( (factionData.PlayerAllied || factionData.aiAllied) && factionData.NoMark3OutpostsOnAlliedPlanets )
                Buffer.Add( "盟友（即人类友好或AI友好）的掠夺者前哨站不能在拥有盟军指挥站的星球上达到3级。" );

            if ( RelatedEntityOrNull.CurrentMarkLevel == 3 )
            {
                Buffer.Add( "此前哨站正在支持其允许的 " + factionData.MaxRaidersPerMark3Outpost + " 名掠夺者中的 " + factionData.RaidersPerOutpost.Display[RelatedEntityOrNull.PrimaryKeyID] + " 名。" );
            }
            else
            {
                if ( RelatedEntityOrNull.Planet.GetControllingFactionType() == FactionType.Player && (factionData.PlayerAllied && factionData.NoMark3OutpostsOnAlliedPlanets) )
                {
                    Buffer.Add( "在你拥有的星球上的盟友掠夺者前哨站无法充分发挥其建造进攻舰队的潜力；它们在该星球上也会建造更少的前哨站。它们需要自己的星球才能完全升级。" );
                }
                else
                {
                    Buffer.Add( "此前哨站将在达到3级后开始生产掠夺者。掠夺者是掠夺者用来征服新星球的强大护卫舰。" );
                }
            }
        }
    }
}
