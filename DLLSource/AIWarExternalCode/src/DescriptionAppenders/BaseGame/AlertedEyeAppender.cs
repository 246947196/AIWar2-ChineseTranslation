using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AlertedEyeAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;

            if ( RelatedEntityOrNull.TypeData.OutnumberedByStrength )
            {
                Buffer.Add( " The Strength of AI-opposed units on this planet is or has recently been greater than the AI's strength, so this unit is alerted for now. " );
            }
            else if ( RelatedEntityOrNull.TypeData.OutnumberedByXFleets > 0 )
            {
                int numFleetsAntagonizing = RelatedEntityOrNull.Planet.PlayerFleetsAtPlanet_Current.Count;
                Buffer.Add( " You have units from " ).Add( numFleetsAntagonizing, "ff1010" ).Add( " fleets on this planet, and this unit is Outnumbered at " ).Add( RelatedEntityOrNull.TypeData.OutnumberedByXFleets, "cfd988" ).Add( " fleets." );
            }
            else
            {
                int numUnitsAntagonizing = RelatedEntityOrNull.PlanetFaction.DataByStance[FactionStance.Hostile].TotalUnits - ( ( RelatedEntityOrNull.PlanetFaction.DataByStance[FactionStance.Self].TotalUnits + RelatedEntityOrNull.PlanetFaction.DataByStance[FactionStance.Friendly].TotalUnits ) * RelatedEntityOrNull.TypeData.OutnumberedByRatio );
                if ( numUnitsAntagonizing > 0 )
                    Buffer.Add( " You have " ).Add( numUnitsAntagonizing, "cfd988" ).Add( " too many units on this planet, so this unit is alerted." );
                else
                    Buffer.Add( " You do not have too many units on this planet, so this unit will probably stop being alerted before long." );
            }

        }
    }
}
