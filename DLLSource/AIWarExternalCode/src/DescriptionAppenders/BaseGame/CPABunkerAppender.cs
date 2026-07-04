using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class CPABunkerAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            
            AISentinelsFactionBaseInfo baseInfo = RelatedEntityOrNull.PlanetFaction.Faction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            AIDifficulty difficulty = baseInfo.SentinelInfo.AIDifficulty;
            int strengthPerBunker = baseInfo.GetCPABunkerStrength();
            strengthPerBunker = strengthPerBunker / 1000; //for UI
            Buffer.Add("当CPA触发时，此碉堡将释放约").Add( strengthPerBunker, "a1ffa1" ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add(" 的舰船。" );
        }
    }
}
