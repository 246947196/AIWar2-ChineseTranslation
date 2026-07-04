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
            Buffer.Add( $"姝ゆ槦鐜綋鍓嶆鍦ㄦ敮鎸佸叾鍦ㄦ槦绯讳腑鎬绘垬鍔涚殑 {perc}%銆? );
            if ( !baseInfo.IsCurrentlyAngryDueToHack && !baseInfo.IsAntagonized ) {
                Buffer.Add( "鏉ヨ嚜姝ゆ槦鐜殑鑸拌埞鏈€澶氬彲鑸鍒拌窛绂绘槦鐜?<color=#a1ffa1>").Add( baseInfo.NormalHopLimit ).Add("</color> 璺崇殑浣嶇疆銆? );
            } else {
                Buffer.Add( "鏉ヨ嚜姝ゆ槦鐜殑鑸拌埞閫氬父鏈€澶氬彲鑸鍒拌窛绂绘槦鐜?<color=#a1ffa1>" ).Add( baseInfo.NormalHopLimit)
                    .Add("</color> 璺崇殑浣嶇疆锛屼絾鐩墠姝ｅ湪鎵╁ぇ鑼冨洿" );
                if (baseInfo.IsAntagonized) {
                    Buffer.Add(" 浠ュ洖搴斾綅浜?").Add( baseInfo.DysonAntagonizer.Display.GetPlanetName_Safe() ).Add(" 涓婄殑 ")
                        .Add( baseInfo.DysonAntagonizer.Display.TypeData.DisplayName, baseInfo.DysonAntagonizer.Display.GetFactionCenterColorHexBrighter_Safe() )
                        .Add("銆? );
                } else if (baseInfo.IsCurrentlyAngryDueToHack) {
                    Buffer.Add(" 浠ュ洖搴斾竴娆＄牬瑙ｃ€? );
                }
            }
            if ( baseInfo.BudgetMultiplierFromHacks > FInt.One || baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                Buffer.Add( "姝ゆ槦鐜殑 " );
                bool wroteAboutProduction = false;
                if ( baseInfo.BudgetMultiplierFromHacks > FInt.One ) {
                    Buffer.Add("浜ч噺涓烘甯哥殑 <color=#a1ffa1>").Add( baseInfo.BudgetMultiplierFromHacks ).Add("x</color>");
                    wroteAboutProduction = true;
                }
                if ( baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                    if (wroteAboutProduction) {
                        Buffer.Add( " 涓?");
                    }
                    Buffer.Add( "鏈€澶ф垬鍔涗负姝ｅ父鐨?<color=#a1ffa1>").Add( baseInfo.MaxStrengthMultiplierFromHacks ).Add("x</color>");
                }
                Buffer.Add(". ");
            }
        }
    }
}
