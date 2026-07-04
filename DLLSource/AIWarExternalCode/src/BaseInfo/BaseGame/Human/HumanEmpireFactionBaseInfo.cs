using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HumanEmpireFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        #region WriteFactionSlotStatus
        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            if ( World_AIW2.Instance == null )
                return;
            
            var fac = AttachedFaction;

            var str = fac.GetStringValueForCustomFieldOrDefaultValue( "StartingFleet", false );
            if ( str == "RandomCombatFleet" )
            {
                buffer.Add( "Random" ).Add( " Fleet" );
                goto done;
            }

            if ( string.IsNullOrWhiteSpace( str ) )
            {
                buffer.Add( "???" );
                goto done;
            }

            var fleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( str );
            if ( fleet != null )
            {
                buffer.Add( fleet.GetDisplayName() );
                goto done;
            }

            buffer.Add( str );

            done:
            // in game
            WriteControllingAccount( buffer );
        }
        #endregion

        public override void UpdatePowerLevel()
        {
            this.AttachedFaction.OverallPowerLevel = FactionUtilityMethods.Instance.GetOverallPowerLevelForGeneralHuman( this.AttachedFaction );
        }
    }
}
