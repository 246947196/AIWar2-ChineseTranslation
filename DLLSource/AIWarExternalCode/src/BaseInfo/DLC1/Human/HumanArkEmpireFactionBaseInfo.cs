using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HumanArkEmpireFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        public override void WriteFactionIcon( ArcenCharacterBufferBase buffer )
        {
            var fac = AttachedFaction;

            buffer.Add( "<pos=5><size=6px><voffset=5px>" );
            buffer.AddSprite( "PlayerIcon_Ark", "PlayerIcon_Ark_Border", null,
                              fac.FactionCenterColor.ColorHex, fac.FactionTrimColor.ColorHex, null );
            buffer.Add( "</pos></size></voffset>" );
        }

        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            if ( World_AIW2.Instance == null )
                return;
            
            var fac = AttachedFaction;

            var str = fac.GetStringValueForCustomFieldOrDefaultValue( "StartingArk", false );
            if ( str == "RandomArk" )
            {
                buffer.Add( "随机" ).Add( "方舟" );
                goto done;
            }

            if ( string.IsNullOrWhiteSpace( str ) )
            {
                buffer.Add( "???" );
                goto done;
            }

            var ark = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( str );
            if ( ark != null )
            {
                buffer.Add( ark.GetDisplayName() );
                goto done;
            }

            buffer.Add( str );
            
            done:
            // in game
            WriteControllingAccount( buffer );
        }

        public override void UpdatePowerLevel()
        {
            this.AttachedFaction.OverallPowerLevel = FactionUtilityMethods.Instance.GetOverallPowerLevelForGeneralHuman( this.AttachedFaction );
        }
    }
}
