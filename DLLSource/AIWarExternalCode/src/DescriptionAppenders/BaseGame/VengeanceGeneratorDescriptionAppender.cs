using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class VengeanceGeneratorDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            
            //we're only getting the data for the actual VG we're hovering over
            DarkSpireFactionBaseInfo darkSpireCachedData = RelatedEntityOrNull.GetFactionBaseInfoOrNullAs_Safe<DarkSpireFactionBaseInfo>();
            if ( darkSpireCachedData == null || darkSpireCachedData.PerPlanet == null )
                return;
            DarkSpirePerPlanet perPlanet = darkSpireCachedData.PerPlanet[RelatedEntityOrNull.Planet.Index];
            if ( perPlanet == null )
                return;
            FInt percent;
            if ( perPlanet.EnergyThresholdForAttack == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: VG on " + RelatedEntityOrNull.GetPlanetName_Safe() + " has no energy attack threshold\n", Verbosity.DoNotShow );
                percent = FInt.Zero;
            }
            else
            {
                percent = 100 * perPlanet.NetEnergy / perPlanet.EnergyThresholdForAttack;
            }
            Buffer.Add( "This is " ).Add( percent.IntValue, "a1a1ff" ).Add( "% charged with " ).Add( "Dark Energy", "2F7063" ).Add( ". When it hits 100 it will either share energy with other Vengeance Generators or take its Vengeance on the galaxy. This VG has a conversion ratio of death to energy of " ).Add( perPlanet.ConversionRatio, "a1ffa1" ).Add( "%." );
            if ( darkSpireCachedData.ConversionRatioCap > 0 )
                Buffer.Add( " The conversion ratio cap is " ).Add( darkSpireCachedData.ConversionRatioCap, "ffa1a1" ).Add( "%." );
            if ( perPlanet.VGGeneratesEnergy )
                Buffer.Add( " This VG will always generate energy slowly." );
        }
    }
}
