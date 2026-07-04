using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpireInfusedHumanEmpireFactionBaseInfo : FallenSpireFactionBaseInfo
    {
        public override void WriteFactionIcon( ArcenCharacterBufferBase buffer )
        {
            //return;

            //using (var icon = SpriteComposite.GetRenderable( "PlayerIcon_SpireInfused" ))
            //{
            //    if (icon == null)
            //    {
            //        base.WriteFactionIcon( buffer );
            //        return;
            //    }

                var fac = AttachedFaction;
                
                //icon.Layout( 6 );
                //icon.Colorize( "TeamColor", fac.FactionCenterColor.ColorHex );
                //icon.Colorize( "TeamColorBorder", fac.FactionTrimColor.ColorHex);
                //buffer.AddSprite( icon, 5, 5 );

                buffer.Add( "<pos=4><size=8px><voffset=6px>" );
                buffer.AddSprite( "PlayerIcon_SpireInfused", 
                                  "PlayerIcon_SpireInfused_Border", 
                                  null,
                                  fac.FactionCenterColor.ColorHex,
                                  fac.FactionTrimColor.ColorHex,
                                  null );
                buffer.Add( "</pos></size></voffset>" );

                //buffer.AddSprite( data.TexEmbedSprite_Icon, data.TexEmbedSprite_IconBorder,
                //                  data.TexEmbedSprite_IconOverlay,
                //                  fac.FactionCenterColor.ColorHex, fac.FactionTrimColor.ColorHex,
                //                  null );


            //}
        }

        //public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        //{
        //}
    }
}
