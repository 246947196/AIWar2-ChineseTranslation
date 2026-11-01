using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class QuiescentEyeAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            int debugCode = 0;
            try{
            if ( RelatedEntityOrNull == null )
                return;
            if ( RelatedEntityTypeData == null )
                return;
            debugCode = 100;
            int NumAlliedUnitsOnPlanet = RelatedEntityOrNull.PlanetFaction.DataByStance[FactionStance.Friendly].TotalUnits + RelatedEntityOrNull.PlanetFaction.DataByStance[FactionStance.Self].TotalUnits;
            int NumUnitsUntilAntagonizing = ( NumAlliedUnitsOnPlanet * RelatedEntityTypeData.OutnumberedByRatio ) - RelatedEntityOrNull.PlanetFaction.DataByStance[FactionStance.Hostile].TotalUnits;
            debugCode = 200;
            int numFleetsAntagonizing = RelatedEntityOrNull.Planet.PlayerFleetsAtPlanet_Current.Count;
            if ( RelatedEntityTypeData.OutnumberedByStrength )
            {
                Buffer.Add( " 当此行星上对抗 AI 的单位的战力超过 AI 的战力时，该单位将进入警戒状态。 " );
            }
            else if ( RelatedEntityTypeData.OutnumberedByXFleets > 0 )
            {
                debugCode = 300;
                if ( numFleetsAntagonizing == 0 )
                    Buffer.Add( " 当来自 " ).Add( RelatedEntityTypeData.OutnumberedByXFleets, "cfd988" ).Add( " 支舰队的舰船位于此行星上时，该单位将进入警戒状态。 " );
                else
                    Buffer.Add( " 当再有 " ).Add( (RelatedEntityTypeData.OutnumberedByXFleets - numFleetsAntagonizing), "cfd988" ).Add( " 支舰队的舰船位于此行星上时，该单位将进入警戒状态。 " );
            }
            else
            {
                debugCode = 400;
                if ( NumUnitsUntilAntagonizing != NumAlliedUnitsOnPlanet )
                    Buffer.Add( " 当再有 " ).Add( NumUnitsUntilAntagonizing, "cfd988" ).Add( " 个对抗 AI 的单位位于此行星上时，该单位将进入警戒状态。 " );
                else
                    Buffer.Add( " 当超过 " ).Add( NumAlliedUnitsOnPlanet, "cfd988" ).Add( " 个对抗 AI 的单位位于此行星上时，该单位将进入警戒状态。 " );
            }
            } catch ( Exception e)
            {
                ArcenDebugging.LogSingleLine("Hit exception " + e.ToString() + " in QuiescentEyeAppender debugCode " + debugCode, Verbosity.DoNotShow );
            }
        }
    }
}
