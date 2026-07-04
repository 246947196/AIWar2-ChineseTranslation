using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public partial class BattlefieldVisualSingleton : IBattlefieldVisualHandler
    {
        //Promoted from a method local to a [ThreadStatic] field so the faction sort in
        //ExpensiveCalculateSortedListOfFactionsForDisplay can be a non-capturing static delegate
        //while still recording crash-diagnostic breadcrumbs the catch can read.
        [ThreadStatic] private static int cb_factionSortDebugCode;
        public void ReactToSquadReplacementShipJustDeployed( GameEntity_Squad SquadReceivingReplacement, GameEntity_Squad EntityProvidingReplacement )
        {
            if ( SquadReceivingReplacement != null && SquadReceivingReplacement.InstancedRenderer != null )
                SquadReceivingReplacement.InstancedRenderer.ReactToSquadReplacementShipJustDeployed( EntityProvidingReplacement );
        }

        public void PlayNetworkSoundPlaybackRequest( NetworkSoundPlaybackRequest Request )
        {
            if ( Request.SoundClipName == null || Request.SoundClipName.Length <= 0 )
                return;

            bool shouldActuallyPlay = true;
            if ( Request.RequiresVoiceToPlay )
            {
                //we still pass voice requests along, because they may need to write text or journal entries even if they play no sounds
                shouldActuallyPlay = GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableVoice ) && GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound );
            }
            else
            {
                if ( !GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound ) )
                    return; //if not a voice clip, just return
            }

            SFXItem soundClip = SFXItemTable.Instance.GetRowByNameOrNullIfNotFound( Request.SoundClipName );
            if ( soundClip == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PlayNetworkSoundPlaybackRequeste error! The sound clip '" + Request.SoundClipName + "' could not be found!", Verbosity.ShowAsError );
                return;
            }

            //turn the network request into a local request
            if ( Request.IsDelayedStyle )
            {
                SoundPlaybackRequestDelayed request;
                request.SoundClip = soundClip;
                request.PlayAs2D = Request.PlayAs2D;
                request.Unity3DLocation = Request.Unity3DLocation;
                request.TimeAtWhichToPlay = ArcenTime.TimeSinceStartF + Request.Delay;
                request.ActuallyPlay = GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableVoice ) && GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound );
                this.SoundPlaybackRequestsDelayed.Enqueue( request );
            }
            else
            {

                SoundPlaybackRequestImmediate request;
                request.SoundClip = soundClip;
                request.PlayAs2D = Request.PlayAs2D;
                request.Unity3DLocation = Request.Unity3DLocation;
                request.ActuallyPlay = shouldActuallyPlay;
                this.SoundPlaybackRequestsImmediate.Enqueue( request );
            }
        }

        public void PlaySoundAtCamera( SoundPropagation Propagation, SFXItem SoundClip )
        {
            switch ( Propagation )
            {
                case SoundPropagation.HostDirectedPlay:
                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.Client:
                            return;
                        case DesiredMultiplayerStatus.Host:
                            {
                                World_AIW2.Instance.OnServer_NetworkSoundsToOmniBlastToClientsAndSelf.Enqueue(
                                    NetworkSoundPlaybackRequest.CreateImmediateStyle( SoundClip, Mat.V3_Zero,
                                    true, false ) );
                            }
                            return;
                    }
                    break;
            }

            if ( !GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound ) )
                return;

            SoundPlaybackRequestImmediate request;
            request.SoundClip = SoundClip;
            request.PlayAs2D = true;
            request.Unity3DLocation = Mat.V3_Zero;
            request.ActuallyPlay = true; //we are handling sfx being disabled above

            this.SoundPlaybackRequestsImmediate.Enqueue( request );
        }

        public void PlayVoiceAtCamera( SoundPropagation Propagation, SFXItem SoundClip )
        {
            switch ( Propagation )
            {
                case SoundPropagation.HostDirectedPlay:
                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.Client:
                            return;
                        case DesiredMultiplayerStatus.Host:
                            {
                                World_AIW2.Instance.OnServer_NetworkSoundsToOmniBlastToClientsAndSelf.Enqueue(
                                    NetworkSoundPlaybackRequest.CreateImmediateStyle( SoundClip, Mat.V3_Zero,
                                    true, true ) );
                            }
                            return;
                    }
                    break;
            }

            //we still pass voice requests along, because they may need to write text or journal entries even if they play no sounds
            SoundPlaybackRequestImmediate request;
            request.SoundClip = SoundClip;
            request.PlayAs2D = true;
            request.Unity3DLocation = Mat.V3_Zero;
            request.ActuallyPlay = GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableVoice ) && GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound );
            this.SoundPlaybackRequestsImmediate.Enqueue( request );
        }

        public void PlayVoiceAtCameraWithDelay( SoundPropagation Propagation, SFXItem SoundClip, float Delay )
        {
            if ( Delay > 10 ) //do not allow delays more than 10 seconds
                Delay = 10;
            switch ( Propagation )
            {
                case SoundPropagation.HostDirectedPlay:
                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.Client:
                            return;
                        case DesiredMultiplayerStatus.Host:
                            {
                                World_AIW2.Instance.OnServer_NetworkSoundsToOmniBlastToClientsAndSelf.Enqueue(
                                    NetworkSoundPlaybackRequest.CreateDelayedStyle( SoundClip, Mat.V3_Zero,
                                    true, Delay, true ) );
                            }
                            return;
                    }
                    break;
            }

            //we still pass voice requests along, because they may need to write text or journal entries even if they play no sounds
            //yes this uses the heap, but it's rare
            SoundPlaybackRequestDelayed request;
            request.SoundClip = SoundClip;
            request.PlayAs2D = true;
            request.Unity3DLocation = Mat.V3_Zero;
            request.TimeAtWhichToPlay = ArcenTime.TimeSinceStartF + Delay;
            request.ActuallyPlay = GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableVoice ) && GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound );

            this.SoundPlaybackRequestsDelayed.Enqueue( request );
        }

        public void PlaySoundAtMainGameLocation( SoundPropagation Propagation, SFXItem SoundClip, ArcenPoint Location )
        {
            switch ( Propagation )
            {
                case SoundPropagation.HostDirectedPlay:
                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.Client:
                            return;
                        case DesiredMultiplayerStatus.Host:
                            {
                                Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                                if ( currentPlanet == null )
                                    return;
                                World_AIW2.Instance.OnServer_NetworkSoundsToOmniBlastToClientsAndSelf.Enqueue(
                                    NetworkSoundPlaybackRequest.CreateImmediateStyle( SoundClip, Location.ToVisualMainGameCoordinates_Unity( currentPlanet ),
                                    false, false ) );
                            }
                            return;
                    }
                    break;
            }
            if ( !GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound ) )
                return;

            {
                Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( currentPlanet == null )
                    return;

                SoundPlaybackRequestImmediate request;
                request.SoundClip = SoundClip;
                request.PlayAs2D = false;
                request.Unity3DLocation = Location.ToVisualMainGameCoordinates_Unity( currentPlanet );
                request.ActuallyPlay = true; //we handle the muting above

                this.SoundPlaybackRequestsImmediate.Enqueue( request );
            }
        }

        public void PlaySoundAtGalaxyMapLocation( SoundPropagation Propagation, SFXItem SoundClip, ArcenPoint Location )
        {
            switch ( Propagation )
            {
                case SoundPropagation.HostDirectedPlay:
                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.Client:
                            return;
                        case DesiredMultiplayerStatus.Host:
                            {
                                World_AIW2.Instance.OnServer_NetworkSoundsToOmniBlastToClientsAndSelf.Enqueue(
                                    NetworkSoundPlaybackRequest.CreateImmediateStyle( SoundClip, Location.ToVisualGalaxyMapPlanetCoordinates_Unity(),
                                    false, false ) );
                            }
                            return;
                    }
                    break;
            }
            if ( !GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound ) )
                return;

            SoundPlaybackRequestImmediate request;
            request.SoundClip = SoundClip;
            request.PlayAs2D = false;
            request.Unity3DLocation = Location.ToVisualGalaxyMapPlanetCoordinates_Unity();
            request.ActuallyPlay = true; //we handle the muting above

            this.SoundPlaybackRequestsImmediate.Enqueue( request );
        }

        public void PlaySoundAtUnity3DLocation( SoundPropagation Propagation, SFXItem SoundClip, Vector3 Location )
        {
            switch ( Propagation )
            {
                case SoundPropagation.HostDirectedPlay:
                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.Client:
                            return;
                        case DesiredMultiplayerStatus.Host:
                            {
                                World_AIW2.Instance.OnServer_NetworkSoundsToOmniBlastToClientsAndSelf.Enqueue(
                                    NetworkSoundPlaybackRequest.CreateImmediateStyle( SoundClip, Location,
                                    false, false ) );
                            }
                            return;
                    }
                    break;
            }

            if ( !GameSettings.Current.GetBool( ArcenBoolSetting_Universal.EnableSound ) )
                return;

            SoundPlaybackRequestImmediate request;
            request.SoundClip = SoundClip;
            request.PlayAs2D = false;
            request.Unity3DLocation = Location;
            request.ActuallyPlay = true; //we handle the muting above

            this.SoundPlaybackRequestsImmediate.Enqueue( request );
        }

        public void WriteBattlefieldVisualHandelerDetailsToEscapeMenu( ArcenDoubleCharacterBuffer Buffer )
        {
            //ROW 1
            Buffer.Add( "\n" ).Add( "<pos=20>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( BurningDyingShips.Count ).EndColor().Add( " 濒死舰船<pos=200>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ActiveSpecialEffects.Count ).EndColor().Add( " 活跃特效" );
            //ROW 2
            Buffer.Add( "\n" ).Add( "<pos=20>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ActiveShots.GetActiveListLength() ).EndColor().Add( " 活跃射击<pos=200>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ActiveSquads.Count ).EndColor().Add( " 活跃舰船" );
            //ROW 3
            Buffer.Add( "\n" ).Add( "<pos=20>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( LooseOtherObjects.Count ).EndColor().Add( " 活跃其他<pos=200>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( SpecialEffectRequests.Count ).EndColor().Add( " 特效请求" );
            //ROW 4
            Buffer.Add( "\n" ).Add( "<pos=20>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( VisualObjectRemovalRequests.Count ).EndColor().Add( " 对象移除<pos=200>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( IInstancedRendererRemovalRequests.Count ).EndColor().Add( " 舰船移除" );
            //ROW 4
            Buffer.Add( "\n" ).Add( "<pos=20>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( SoundPlaybackRequestsImmediate.Count ).EndColor().Add( " 音效请求<pos=200>" );
            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( SoundPlaybackRequestsDelayed.Count ).EndColor().Add( " 音效延迟请求" );
        }

        public void ShowTooltipNarrow( string Tooltip )
        {
            Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( null, Tooltip );
        }

        public void ShowTooltipWide( string Tooltip )
        {
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, Tooltip );
        }

        public void ShowBurningAndDyingEffectForShipInStackIfVisualObjectExistsForStack( GameEntity_Squad SquadThatLostAShip )
        {
            if ( SquadThatLostAShip != null && SquadThatLostAShip.InstancedRenderer != null )
                SquadThatLostAShip.InstancedRenderer.ShowBurningAndDyingEffectForShipInStackIfVisualObjectExistsForStack( SquadThatLostAShip );
        }

        public void ReactToShotHittingSquad( GameEntity_Shot Shot, GameEntity_Squad TargetSquad, GameEntity_Squad ProtectingShieldThatTookTheHitOrNull, int NumberOfShipsKilled, bool WasEntireSquadKilled )
        {
            ShotVisualizer shotVis = (ShotVisualizer)Shot.InstancedRenderer;
            if ( shotVis == null )
            {
                //ArcenDebugging.ArcenDebugLog( "Null ArcenShot InstancedRenderer on " + Shot.TypeData.InternalName , Verbosity.DoNotShow );
                return;
            }

            shotVis.ReactToShotHittingSquad( TargetSquad, ProtectingShieldThatTookTheHitOrNull, NumberOfShipsKilled, WasEntireSquadKilled );
        }

        #region DropVisualEffectAtSuperGlobal3DPositionWithTTL
        public void DropVisualEffectAtSuperGlobal3DPositionWithTTL( ArcenSpecialEffectGroup Group, Vector3 SuperGlobal3DPosition, float TimeToLive, int AOESize )
        {
            if ( Group == null )
            {
                ArcenDebugging.ArcenDebugLog( "DropVisualEffectAtSuperGlobal3DPositionWithTTL Warning: Null ArcenSpecialEffectGroup passed in, so could not do explosion.", Verbosity.DoNotShow );
                return;
            }

            SpecialEffectRequest request;
            request.Group = Group;
            request.SuperGlobal3DPosition = SuperGlobal3DPosition;
            request.TimeToLive = TimeToLive;
            request.AOESize = AOESize;

            this.SpecialEffectRequests.Enqueue( request );
        }
        #endregion

        public IVisualObject RequestIVisualObject( ArcenGameObjectResourcePool FromPool, GameEntity_Base RelatedTo )
        {
            if ( RelatedTo.VisualLinkObject_GenericOnly != null )
                return null; //no double-requests, buddy!
            if ( RelatedTo.IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject )
                return null;

            IVisualObject vObj = FromPool.GetAndActivateIVisualObject( RelatedTo, RelatedTo.TypeData );
            return vObj;
        }

        public void ReactToEnteringPlanetView( Planet NewPlanet )
        {
            PresentationLayer_AIW2.Instance.DoFadeInOut();
            this.ClearPlanetLooseStuffAndSpecialEffects( InstancedRendererDeactivationReason.VisLayerReactToEnteringPlanetView );
            this.ClearPlanetEntities( InstancedRendererDeactivationReason.VisLayerReactToEnteringPlanetView );
            this.FastTrackDisplayRequestsForRemaining = 2f;
        }

        public void ReactToLeavingPlanetView( Planet OldPlanet )
        {
            PresentationLayer_AIW2.Instance.DoFadeInOut();
            this.ClearPlanetLooseStuffAndSpecialEffects( InstancedRendererDeactivationReason.VisLayerReactToLeavingPlanetView );
            this.ClearPlanetEntities( InstancedRendererDeactivationReason.VisLayerReactToLeavingPlanetView );
        }

        public InstancedRendererBase GetOrCreateNewInstancedRenderer( GameEntityTypeData FirstRelatedEntityType, ArcenAssetBundlePath PathOfPrototype, IArcenGameObjectResourcePoolable Prototype )
        {
            if ( Prototype is ArcenShot )
                return ShotRenderManagerGroup.GetShotInstanceRendererByName( FirstRelatedEntityType.InternalName, PathOfPrototype, Prototype );
            else if ( Prototype is ArcenVisualSolomeshShip )
                return ShipRenderManagerGroup.GetShipInstanceRendererByName( FirstRelatedEntityType, PathOfPrototype, Prototype );
            return null;
        }

        public InstancedRendererBase GetOrCreateNewInstancedRenderer( SquadDisplayType DisplayType )
        {
            return SquadRenderManagerGroup.GetSquadInstanceRendererBySquadDisplayType( DisplayType );
        }

        public void ClaimPlanet( Planet planet )
        {
            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                return; //don't do this on client
            Faction newOwnerFaction = planet.GetControllingFaction();

            /* These checks in particular for Dyson claims */
            if ( newOwnerFaction != null )
            {
                bool playedDysonSound = false;
                if ( newOwnerFaction.Config.IsFactionControlledByLocalPlayer )
                {
                    GameEntity_Squad thisSphere = planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true );
                    if ( thisSphere == null )
                        return;
                    if ( thisSphere.Planet == planet )
                    {
                        playedDysonSound = true;
                        Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.DysonPlanetCapturedByPlayer );
                    }
                }
                else if ( newOwnerFaction.Type == FactionType.AI && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                {
                    GameEntity_Squad thisSphere = planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true );
                    if ( thisSphere == null )
                        return;
                    if ( thisSphere.Planet == planet )
                    {
                        playedDysonSound = true;
                        Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.DysonPlanetCapturedByAI );
                    }
                }
                if ( playedDysonSound ) { } //prevents compiler warning
            }
        }

        #region ExpensiveCalculateSortedListOfFactionsForDisplay
        /// <summary>
        /// Don't call this directly!  Instead call FactionFilter.GetLatestSortedFactionCompleteList() to get this result which is cached and only updated once every 10th of a second.
        /// </summary>
        public void ExpensiveCalculateSortedListOfFactionsForDisplay( List<Faction> FactionsToFill )
        {
            //Sorts the factions for display via the Esc menu
            FactionsToFill.Clear();

            foreach ( Faction fac in World_AIW2.Instance.Factions )
                FactionsToFill.Add( fac );

            cb_factionSortDebugCode = 0;
            try
            {
                cb_factionSortDebugCode = 100;
                FactionsToFill.Sort( static delegate ( Faction L, Faction R )
                {
                    cb_factionSortDebugCode = 200;
                    if ( L == null || R == null )
                        return 0;
                    cb_factionSortDebugCode = 300;
                    //Sort the factions; players first, then AI and their allies, then minor factions by teams (and sorted by intensity there)
                    if ( L.Type == FactionType.Player && R.Type == FactionType.Player ||
                         L.Type == FactionType.AI && R.Type == FactionType.AI ||
                         (L.SpecialFactionData.AlliedToAIByDefault && R.SpecialFactionData.AlliedToAIByDefault) )
                        return L.FactionIndex.CompareTo( R.FactionIndex );
                    if ( L.Type == FactionType.Player &&
                         R.Type != FactionType.Player )
                        return -1; //players at the top
                    if ( R.Type == FactionType.Player &&
                         L.Type != FactionType.Player )
                        return 1;
                    cb_factionSortDebugCode = 400;
                    if ( L.Type == FactionType.AI &&
                         R.Type != FactionType.AI && R.Type != FactionType.Player )
                        return -1; //AIs next
                    if ( R.Type == FactionType.AI &&
                         L.Type != FactionType.AI && L.Type != FactionType.Player )
                        return 1; //AIs next

                    cb_factionSortDebugCode = 500;
                    if ( L.SpecialFactionData.AlliedToAIByDefault &&
                         R.Type != FactionType.AI && R.Type != FactionType.Player &&
                         !R.SpecialFactionData.AlliedToAIByDefault )
                        return -1; //AI-allies next
                    if ( R.SpecialFactionData.AlliedToAIByDefault &&
                         L.Type != FactionType.AI && L.Type != FactionType.Player &&
                         !L.SpecialFactionData.AlliedToAIByDefault )
                        return 1; //AI-allies next

                    cb_factionSortDebugCode = 600;
                    string LAllegiance = World_AIW2.Instance.GetFactionByIndex( L.FactionIndex ).BaseInfo.Allegiance;
                    string RAllegiance = World_AIW2.Instance.GetFactionByIndex( R.FactionIndex ).BaseInfo.Allegiance;
                    cb_factionSortDebugCode = 700;
                    int LIntensity = World_AIW2.Instance.GetFactionByIndex( L.FactionIndex ).BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                    int RIntensity = World_AIW2.Instance.GetFactionByIndex( R.FactionIndex ).BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                    cb_factionSortDebugCode = 800;
                    if ( LIntensity == 0 )
                        return 1;
                    if ( RIntensity == 0 )
                        return -1;
                    if ( LAllegiance == RAllegiance )
                        return RIntensity.CompareTo( LIntensity );
                    if ( LAllegiance == null && RAllegiance != null )
                        return -1;
                    if ( RAllegiance == null && LAllegiance != null )
                        return 1;
                    //friendly to players first
                    if ( (LAllegiance == "Friendly To Players" && RAllegiance != "Friendly To Players") )
                        return -1;
                    if ( (RAllegiance == "Friendly To Players" && LAllegiance != "Friendly To Players") )
                        return 1;
                    // //hostile to all at the bottom
                    if ( (LAllegiance == "Hostile To All" && RAllegiance != "Hostile To All") )
                        return 1;
                    if ( (RAllegiance == "Hostile To All" && LAllegiance != "Hostile To All") )
                        return -1;
                    return LAllegiance.CompareTo( RAllegiance );
                } );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in SortFactionsForDisplay cb_factionSortDebugCode " + cb_factionSortDebugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
