using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public struct EntityDescText
    {
        public readonly GameEntity_Squad Squad;
        public readonly EntityText.Config Config;
        
        public EntityDescText(GameEntity_Squad squad, EntityText.Config config)
        {
            Squad = squad;
            Config = config;
        }
        
        public void Write(ArcenCharacterBufferBase buffer)
        {
            var block_style = TextStyle.Desc_Block;
            var block_padding = TextStyle.Desc_Pad;

            string desc = null;
            string desc_full = Squad.TypeData.Description;
            string desc_short = Squad.TypeData.DescriptionShort;
            
            if ( Config.Detail < TooltipDetail.Full && 
                 !string.IsNullOrEmpty(desc_short) &&
                 desc_short != "~*~" )
            {
                desc = desc_short;
            }
            else 
            if ( !string.IsNullOrEmpty(desc_full) &&
                 desc_full != "~*~" )
            {
                desc = desc_full;
            }
            
            if (!string.IsNullOrEmpty(desc))
            {
                buffer
                    .Pad(block_padding)
                    .Add(desc, block_style)
                    .Pad(block_padding);
                    ;
            }
        }
    }
}