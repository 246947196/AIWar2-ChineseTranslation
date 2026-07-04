using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_AllocVariantShipType : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int debugStage = 0;

            ArcenCharacterBuffer traceBuffer = null;
            if (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Variants))
                traceBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate("GameCommand_AllocVariantShipType.traceBuffer");

            try
            {
                debugStage = 10;

                var base_type = GameEntityTypeDataTable.Instance.GetRowByName(command.RelatedString);
                var variant_type = GameEntityTypeData_Variant_Table.Instance.GetRowByName(command.RelatedString2);
                var created_type = GameEntityTypeDataTable.Instance.GetRowByName(command.RelatedString3, LookupSwapAllowed.No, true);

                debugStage = 20;

                traceBuffer?.Add(string.Format("GameCommand_AllocVariantShipType.Execute called with base={0} variant={1} created={2}\n", 
                                    base_type?.InternalName??"null", variant_type?.InternalName??"null", created_type?.InternalName??"null"));

                debugStage = 30;

                if (created_type != null)
                {
                    debugStage = 40;

                    traceBuffer?.Add("  Already exists, so done.\n");
                    return;
                }

                debugStage = 50;

                // create it
                var info = new VariantInfo();
                info.BaseType = base_type;
                info.VariantType = variant_type;
                info.CreatedType = GameEntityTypeData_Variant_Table.Instance.AllocVariantShipType(info.BaseType, info.VariantType, VariantUsage.Permanent);

                debugStage = 60;

                World_AIW2.Instance.VariantInfos.Add(info);

                debugStage = 70;

                FactionUtilityMethods.Instance.FinishTracing( traceBuffer );
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
