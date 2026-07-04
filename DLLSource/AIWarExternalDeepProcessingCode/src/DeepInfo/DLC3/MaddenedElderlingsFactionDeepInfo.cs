using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /*  Overview
        The elderlings patrol small areas of the galaxy. Then they lay eggs, which make more elderlings that patrol nearby regions

     */
    public sealed class MaddenedElderlingsFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public MaddenedElderlingsFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<MaddenedElderlingsFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 10;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host. I think a lot of what the miners do is just not client 

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ElderlingMad-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;

            //If we have no presence on the map, periodically rejoin our allies
        
            HandleJournals( Context );

            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        
        public void HandleJournals( ArcenHostOnlySimContext Context )
        {
            List<SafeSquadWrapper> elderlings = this.BaseInfo.Elderlings.GetDisplayList();
            for ( int i = 0; i < elderlings.Count; i++ )
            {
                if ( elderlings[i].GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Elderlings_Maddened", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                }
            }
        }
        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            foreach ( GameEntity_Squad otherElderling in entity.Planet.Squads( "Elderling" ) )
            {
                if ( otherElderling == null )
                    continue;
                if ( otherElderling.PlanetFaction.Faction.SpecialFactionData.InternalName == "MaddenedElderlings" )
                    continue;
                ElderlingsPerUnitBaseInfo data = otherElderling.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                data.ExperienceRequired -= 500;
            }
        }
    }
}
