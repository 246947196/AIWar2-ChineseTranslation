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
            Buffer.Add( "杩欐槸 " ).Add( percent.IntValue, "a1a1ff" ).Add( "% 鍏呰兘鐨?" ).Add( "鏆楄兘閲?, "2F7063" ).Add( "銆傚綋杈惧埌 100% 鏃讹紝瀹冨皢涓庡叾浠栧浠囩敓鎴愬櫒鍏变韩鑳介噺鎴栧鏄熺郴杩涜澶嶄粐銆傛澶嶄粐鐢熸垚鍣ㄧ殑姝讳骸鍒拌兘閲忚浆鎹㈡瘮涓?" ).Add( perPlanet.ConversionRatio, "a1ffa1" ).Add( "%銆? );
            if ( darkSpireCachedData.ConversionRatioCap > 0 )
                Buffer.Add( " 杞崲姣斾笂闄愪负 " ).Add( darkSpireCachedData.ConversionRatioCap, "ffa1a1" ).Add( "%銆? );
            if ( perPlanet.VGGeneratesEnergy )
                Buffer.Add( " 姝ゅ浠囩敓鎴愬櫒灏嗗缁堢紦鎱骇鐢熻兘閲忋€? );
        }
    }
}
