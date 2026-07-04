using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /// <summary>
    //Handles the logic for the Reapers. Reapers are the enemies of the Necromancer. They have defensive structures that act like Sapper Watchtowers,
    //and also just sometimes spawn units to go after the players.
    /// </summary>
    public sealed class ScourgeCivilWarFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public ScourgeCivilWarFactionBaseInfo BaseInfo;
        public static ScourgeCivilWarFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ScourgeCivilWarFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 30; //they aren't in a rush

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return; //only on host (don't bother doing anything as a client)
                //initialize the dysonFaction and dsBaseInfo fields immediately
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ScourgeCivilWar Stage 3 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Scourge Civil War Logic LRP. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
                //              FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            }
        }
        
    }
}
