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
            Buffer.Add( $"This Sphere is currently supporting {perc}% of it's total strength in the galaxy. " );
            if ( !baseInfo.IsCurrentlyAngryDueToHack && !baseInfo.IsAntagonized ) {
                Buffer.Add( "Ships from this Sphere can travel up to <color=#a1ffa1>").Add( baseInfo.NormalHopLimit ).Add("</color> hops away from the Sphere. " );
            } else {
                Buffer.Add( "Ships from this Sphere can normally travel up to <color=#a1ffa1>" ).Add( baseInfo.NormalHopLimit)
                    .Add("</color> hops away from the Sphere, but are ranging further" );
                if (baseInfo.IsAntagonized) {
                    Buffer.Add(" in response to a ")
                        .Add( baseInfo.DysonAntagonizer.Display.TypeData.DisplayName, baseInfo.DysonAntagonizer.Display.GetFactionCenterColorHexBrighter_Safe() )
                        .Add(" on ").Add( baseInfo.DysonAntagonizer.Display.GetPlanetName_Safe() ).Add(". ");
                } else if (baseInfo.IsCurrentlyAngryDueToHack) {
                    Buffer.Add(" in response to a hack. ");
                }
            }
            if ( baseInfo.BudgetMultiplierFromHacks > FInt.One || baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                Buffer.Add( "This sphere's " );
                bool wroteAboutProduction = false;
                if ( baseInfo.BudgetMultiplierFromHacks > FInt.One ) {
                    Buffer.Add("production is <color=#a1ffa1>").Add( baseInfo.BudgetMultiplierFromHacks ).Add("x</color> normal");
                    wroteAboutProduction = true;
                }
                if ( baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                    if (wroteAboutProduction) {
                        Buffer.Add( " and ");
                    }
                    Buffer.Add( "maximum strength is <color=#a1ffa1>").Add( baseInfo.MaxStrengthMultiplierFromHacks ).Add("x</color> normal");
                }
                Buffer.Add(". ");
            }
        }
    }
}
