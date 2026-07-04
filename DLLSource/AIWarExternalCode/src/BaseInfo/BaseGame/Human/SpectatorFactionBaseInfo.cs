using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpectatorFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        public override void WriteFactionIcon( ArcenCharacterBufferBase buffer )
        {
            var fac = AttachedFaction;
            
            buffer.Add( "<pos=3><size=8px><voffset=6px>" );
            buffer.AddSprite( "PlayerIcon_Spectator", 
                              "PlayerIcon_Spectator_Border", 
                              null,
                              fac.FactionCenterColor.ColorHex,
                              fac.FactionTrimColor.ColorHex,
                              null );
            buffer.Add( "</pos></size></voffset>" );
        }

        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            buffer.Add( "<i>Omniscient But Uninvolved</i>" );
        }
    }
}
