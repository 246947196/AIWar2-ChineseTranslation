using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SphereDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;
            SphereFactionBaseInfo baseInfo = RelatedEntityOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
                return;

            int perc = ((baseInfo.Strength.Display * 100) / baseInfo.GetMaxStrength).GetNearestIntPreferringHigher() ;
            Buffer.Add( $"此星环当前正在支持其在星系中总战力的 {perc}%。" );
            if ( !baseInfo.IsCurrentlyAngryDueToHack && !baseInfo.IsAntagonized ) {
                Buffer.Add( "来自此星环的舰船最多可航行到距离星环<color=#a1ffa1>").Add( baseInfo.NormalHopLimit ).Add("</color> 跳的位置。" );
            } else {
                Buffer.Add( "来自此星环的舰船通常最多可航行到距离星环<color=#a1ffa1>" ).Add( baseInfo.NormalHopLimit)
                    .Add("</color> 跳的位置，但目前正在扩大范围" );
                if (baseInfo.IsAntagonized) {
                    Buffer.Add(" 以回应位�")
                        .Add( baseInfo.DysonAntagonizer.Display.TypeData.DisplayName, baseInfo.DysonAntagonizer.Display.GetFactionCenterColorHexBrighter_Safe() )
                        .Add(" 上的 ").Add( baseInfo.DysonAntagonizer.Display.GetPlanetName_Safe() ).Add("。");
                } else if (baseInfo.IsCurrentlyAngryDueToHack) {
                    Buffer.Add(" 以回应一次破解。");
                }
            }
            if ( baseInfo.BudgetMultiplierFromHacks > FInt.One || baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                Buffer.Add( "此星环的 " );
                bool wroteAboutProduction = false;
                if ( baseInfo.BudgetMultiplierFromHacks > FInt.One ) {
                    Buffer.Add("产量为正常的 <color=#a1ffa1>").Add( baseInfo.BudgetMultiplierFromHacks ).Add("x</color>");
                    wroteAboutProduction = true;
                }
                if ( baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                    if (wroteAboutProduction) {
                        Buffer.Add( " " );
                    }
                    Buffer.Add( "最大战力为正常<color=#a1ffa1>").Add( baseInfo.MaxStrengthMultiplierFromHacks ).Add("x</color>");
                }
                Buffer.Add("。");
            }
        }
    }
}
