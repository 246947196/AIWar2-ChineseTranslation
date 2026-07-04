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
                Buffer.Add( " If the Strength of AI-opposed units on this planet becomes greater than the AI's strength, this unit will become alerted. " );
            }
            else if ( RelatedEntityTypeData.OutnumberedByXFleets > 0 )
            {
                debugCode = 300;
                if ( numFleetsAntagonizing == 0 )
                    Buffer.Add( " If ships from " ).Add( RelatedEntityTypeData.OutnumberedByXFleets, "cfd988" ).Add( " fleets are on this planet, this unit will become alerted. " );
                else
                    Buffer.Add( " If ships from " ).Add( (RelatedEntityTypeData.OutnumberedByXFleets - numFleetsAntagonizing), "cfd988" ).Add( " more fleets are on this planet, this unit will become alerted. " );
            }
            else
            {
                debugCode = 400;
                if ( NumUnitsUntilAntagonizing != NumAlliedUnitsOnPlanet )
                    Buffer.Add( " If " ).Add( NumUnitsUntilAntagonizing, "cfd988" ).Add( " more AI-opposed units are on this planet, this unit will become alerted. " );
                else
                    Buffer.Add( " If more than " ).Add( NumAlliedUnitsOnPlanet, "cfd988" ).Add( " AI-opposed units are on this planet, this unit will become alerted. " );
            }
            } catch ( Exception e)
            {
                ArcenDebugging.LogSingleLine("Hit exception " + e.ToString() + " in QuiescentEyeAppender debugCode " + debugCode, Verbosity.DoNotShow );
            }
        }
    }
}
