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
            Buffer.Add( "这是 " ).Add( percent.IntValue, "a1a1ff" ).Add( "% 充能" ).Add( "暗能", "2F7063" ).Add( "。当达到 100% 时，它将与其他复仇生成器共享能量或对星系进行复仇。此复仇生成器的死亡到能量转换比为 " ).Add( perPlanet.ConversionRatio, "a1ffa1" ).Add( "%。" );
            if ( darkSpireCachedData.ConversionRatioCap > 0 )
                Buffer.Add( " 转换比上限为 " ).Add( darkSpireCachedData.ConversionRatioCap, "ffa1a1" ).Add( "%。" );
            if ( perPlanet.VGGeneratesEnergy )
                Buffer.Add( " 此复仇生成器将始终缓慢产生能量。" );
        }
    }
}
