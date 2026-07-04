using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_AutoPlaceBuildable : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int debugStage = 0;

            ArcenCharacterBuffer traceBuffer = null;
            if (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Variants))
                traceBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate("GameCommand_AutoPlaceBuildable.traceBuffer");

            try
            {
                debugStage = 10;

                var hostCtx = context.GetHostOnlyContext();
                if (hostCtx == null)
                    return;
                var fac = command.GetRelatedFaction();
                if (fac == null)
                    return;
                int planetIdx = 0, builderID = 0, commandStationID = 0, fleetID = 0, uniqueID = 0;
                int _ri_i = 0;
                foreach ( var _ri_v in command.RelatedIntegers )
                {
                    switch ( _ri_i )
                    {
                        case 0: planetIdx = _ri_v; break;
                        case 1: builderID = _ri_v; break;
                        case 2: commandStationID = _ri_v; break;
                        case 3: fleetID = _ri_v; break;
                        case 4: uniqueID = _ri_v; break;
                    }
                    _ri_i++;
                }
                var planet = World_AIW2.Instance.GetPlanetByIndex( (short)planetIdx );
                if (planet == null)
                    return;
                var builder = World_AIW2.Instance.GetEntityByID_Squad( builderID );
                if (builder == null)
                    return;

                // this one is allowed to be null, ie. autobuilding on a planet without a station via battlestations
                var commandStation = World_AIW2.Instance.GetEntityByID_Squad( commandStationID );

                var fleet = World_AIW2.Instance.GetFleetByID(fleetID);
                if (fleet == null)
                    return;
                var type = GameEntityTypeDataTable.Instance.GetRowByName( command.RelatedString );
                if (type == null)
                    return;
                var mem = fleet.GetButDoNotAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates(type, uniqueID);
                if (mem == null)
                    return;

                var currentCountToBuild = mem.EffectiveSquadCap;
                
                StateOfMatterTypeData stateOfMatter = mem.TypeData.ForcedToAlwaysBeThisStateOfMatter;
                if ( stateOfMatter == null )
                    stateOfMatter = StateOfMatterTypeDataTable.Instance.DefaultRow;

                if ( mem.TypeData.AutoplacementAnchor == PlacementAnchor.CenterEntity )
                {
                    var aroundEntity = commandStation;
                    if (aroundEntity == null)
                        aroundEntity = builder;
                    PlacementCondition.PlaceUnitsUpToCap_AroundEntity( hostCtx, mem, planet, aroundEntity, stateOfMatter, currentCountToBuild, true, true, false );
                }
                else 
                if ( mem.TypeData.AutoplacementAnchor == PlacementAnchor.CenterOfPlanet )
                    PlacementCondition.PlaceUnitsUpToCap_AroundPlanetCenter( hostCtx, mem, planet, stateOfMatter, currentCountToBuild, true, true, false );
                else
                    PlacementCondition.PlaceUnitsUpToCap_InSniperRing( hostCtx, mem, planet, commandStation, stateOfMatter, currentCountToBuild, true, true, false );
            }
            catch (Exception e)
            {
                FactionUtilityMethods.Instance.FinishTracing( traceBuffer );
                ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Exception occured at debugStage {0}:\n{1}", debugStage, e), Verbosity.ShowAsError);
            }
        }
    }

    /*
    public class GameCommand_DeallocVariantShipType : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            var base_type = GameEntityTypeDataTable.Instance.GetRowByName(command.RelatedString);
            var variant_type = GameEntityTypeDataTable.Instance.GetRowByName(command.RelatedString2);
            var created_type = GameEntityTypeDataTable.Instance.GetRowByName(command.RelatedString3);

            ArcenDebugging.ArcenDebugLogSingleLine(string.Format("GameCommand_DeallocVariantShipType.Execute for base={0} variant={1} created={2}", 
                base_type?.InternalName??"null", variant_type?.InternalName??"null", created_type?.InternalName??"null"), Verbosity.DoNotShow);

            GameEntityTypeData_Variant_Table.Instance.DeallocVariantShipType(created_type);
        }
    }
    */
}
