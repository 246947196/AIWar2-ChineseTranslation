using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public sealed class AIPraetorianGuardFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public AIPraetorianGuardFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AIPraetorianGuardFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            this.BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context)
        {
        }

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return;
            AIPraetorianGuardCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().PraetorianInfo;
            if ( factionExternal == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "SeedStartingEntities_LaterEverythingElse: GetPraetorianGuardExternal was null on faction " +
                    AttachedFaction.GetDisplayName() + " (index " + AttachedFaction.FactionIndex + ")" );
                return;
            }

            if ( factionExternal.SubType == null )
            {
                factionExternal.SubType = PraetorianGuardTypeDataTable.Instance.DefaultRow;
                if ( factionExternal.SubType == null )
                    factionExternal.SubType = PraetorianGuardTypeDataTable.Instance.Rows[0];
            }
            factionExternal.DoGameStartLogic( Context );
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return;
            AIPraetorianGuardCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().PraetorianInfo;

            if ( !factionExternal.HaveCheckedForInitialDonation )
            {
                factionExternal.HaveCheckedForInitialDonation = true;
                FInt donation = (FInt)factionExternal.AIDifficulty.PraetorianStartingBudget;
                if ( donation > 0 )
                    factionExternal.ReceiveDonation( donation, AttachedFaction, null );
            }
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return;
            if ( AttachedFaction.HasBeenSeenByPlayer )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_Praetorian", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

            AIPraetorianGuardCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().PraetorianInfo;
            factionExternal.DoPerSecondLogic_OnMainThreadAndPartOfSim_HostOnly( Context );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return;
            AIPraetorianGuardCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().PraetorianInfo;
            if ( factionExternal == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Could not find factionExternal for praetorian guard", Verbosity.ShowAsError );
                return;
            }
            if (factionExternal.DisableLongRangePlanning)
                return;
            if ( factionExternal.AIDifficulty == null )
            {
                factionExternal.AIDifficulty = AIDifficulty_PraetorianGuardTable.Instance.DefaultRow;
                if ( factionExternal.AIDifficulty == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Could not find factionExternal AI Difficulty for praetorian guard, and there was no default row!", Verbosity.ShowAsError );
                    return;
                }
            }
            factionExternal.DoLongRangePlanning( Context );
        }
    }
}
