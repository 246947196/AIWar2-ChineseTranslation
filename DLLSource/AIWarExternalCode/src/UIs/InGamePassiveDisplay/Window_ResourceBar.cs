using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_ResourceBar : WindowControllerAbstractBase
    {
        public Window_ResourceBar()
        {
            this.OnlyShowInGame = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
        }

        public class customParent : CustomUIAbstractBase
        {
            public static float lastTimeWasAttackingHomePlanet = 0;
            public static float lastTimeWasAttackingNonHomePlanet = 0;
            public static int totalAttack = 0;
            public static int newAttack = 0;
            //0 = no attacking, 1 = attacking non-home, 2 = attacking home
            public static int currentDangerIndex;

            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "ResourceBarScale" );
                //make sure the back bar of the header stays wide enough as it gets smaller
                this.Element.RelevantRect.UI_SetWidth( 980 / this.WindowController.myScale );

                this.HandleLogicForAttackButtons();
                this.HandleButtonPositions();
            }

            private float nextRecalculateAttackButtons = 0;

            #region HandleLogicForAttackButtons
            public void HandleLogicForAttackButtons()
            {
                if ( nextRecalculateAttackButtons > ArcenTime.TimeSinceStartF )
                    return;
                nextRecalculateAttackButtons = ArcenTime.TimeSinceStartF + Engine_Universal.PermanentQualityRandom.NextFloat( 0.2f, 0.5f );
                if ( ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() )
                    return;

                //Asynchronously run the logic to keep this moving along
                ArcenThreading.RunTaskOnBackgroundThread( "_UI.HandleLogicForAttackButtons", false, false, delegate
                {
                    Faction playerFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    if ( playerFaction == null )
                        return;
                    totalAttack = 0;
                    newAttack = 0;
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        bool doPlayersLoseIfUnitLostHere = false;
                        //if attacking a human homeworld (or other planet with a unit that can cause a loss)
                        foreach ( GameEntity_Squad squad in planet.Squads( EntityRollupType.PlayerLosesIfAnyDie ) )
                        {
                            if ( squad == null || squad.PlanetFaction == null )
                            {
                                continue;
                            }
                            if ( planet.GetPlanetFactionForFaction( playerFaction ) == squad.PlanetFaction )
                            {
                                //if attacking MY homeworld
                                doPlayersLoseIfUnitLostHere = true;
                            }
                        }
                        if ( planet.GetControllingFactionType() != FactionType.Player && !doPlayersLoseIfUnitLostHere )
                            continue;
                        newAttack = planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                        if ( newAttack <= 0 )
                            continue;
                        totalAttack += newAttack;
                        lastTimeWasAttackingNonHomePlanet = ArcenTime.TimeSinceStartF;

                        if ( doPlayersLoseIfUnitLostHere )
                        {
                            lastTimeWasAttackingHomePlanet = ArcenTime.TimeSinceStartF;
                        }
                    }

                    if ( ArcenTime.TimeSinceStartF - lastTimeWasAttackingHomePlanet < 1f )
                         System.Threading.Interlocked.Exchange( ref currentDangerIndex, 2 );
                    else if ( ArcenTime.TimeSinceStartF - lastTimeWasAttackingNonHomePlanet < 1f )
                        System.Threading.Interlocked.Exchange( ref currentDangerIndex, 1 );
                    else
                        System.Threading.Interlocked.Exchange( ref currentDangerIndex, 0 );
                } );
                
            }
            #endregion

            private const float SPACE = 2;

            public void HandleButtonPositions()
            {
                float currentX = tPlanetName.Instance.Element.RelevantRect.anchoredPosition.x + tPlanetName.Instance.Element.RelevantRect.rect.width + SPACE;

                if ( tMetal.LastWasVisible )
                    SetPosFor( tMetal.Instance, ref currentX );
                if ( tEnergy.LastWasVisible )
                    SetPosFor( tEnergy.Instance, ref currentX );
                if ( tFuelArgon.LastWasVisible )
                    SetPosFor( tFuelArgon.Instance, ref currentX );
                if ( tFuelRadon.LastWasVisible )
                    SetPosFor( tFuelRadon.Instance, ref currentX );
                if ( tFuelXenon.LastWasVisible )
                    SetPosFor( tFuelXenon.Instance, ref currentX );

                SetPosFor( tScienceDivider.Instance, ref currentX );

                SetPosFor( tScience.Instance, ref currentX );
                SetPosFor( tHacking.Instance, ref currentX );

                if ( tNecromancerEsssence.LastWasVisible )
                    SetPosFor( tNecromancerEsssence.Instance, ref currentX );
                if ( tFactionResource2.LastWasVisible )
                    SetPosFor( tFactionResource2.Instance, ref currentX );
                if ( tFactionResource3.LastWasVisible )
                    SetPosFor( tFactionResource3.Instance, ref currentX );

                SetPosFor( tHackingDivider.Instance, ref currentX );

                SetPosFor( tAIP.Instance, ref currentX );
                SetPosFor( tThreat.Instance, ref currentX );

                SetPosForWithoutMovement( tAttackSafe.Instance, currentX );
                SetPosForWithoutMovement( tAttackNonHome.Instance, currentX );
                SetPosFor( tAttackHomePlanet.Instance, ref currentX ); //the last one needs to move it forward again

                SetPosFor( tEncyclopedia.Instance, ref currentX );

                currentX += 6;
                SetPosFor( tGeneralTextMessage.Instance, ref currentX );
            }

            private static void SetPosFor( ElementAbstractBase Button, ref float currentX )
            {
                Button.Element.RelevantRect.anchoredPosition = new Vector2( currentX, Button.Element.RelevantRect.anchoredPosition.y );

                currentX += Button.Element.RelevantRect.rect.width + SPACE;
            }

            private static void SetPosForWithoutMovement( ElementAbstractBase Button, float currentX )
            {
                Button.Element.RelevantRect.anchoredPosition = new Vector2( currentX, Button.Element.RelevantRect.anchoredPosition.y );
            }
        }

        #region tMetal
        public class tMetal : ButtonAbstractBase
        {
            public static tMetal Instance;
            private static bool IsDysonSidekick = false;
            public tMetal() { Instance = this; }

            private float timeMetalSpentHasBeenZero = 0f;

            public static bool LastWasVisible = true;

            #region GetShouldBeHidden
            public override bool GetShouldBeHidden()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction != null )
                {
                    IsDysonSidekick = DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( localFaction );
                    PlayerTypeData playerTypeData = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( !playerTypeData.UsesMetal )
                        LastWasVisible = false;
                    else
                        LastWasVisible = true;
                }
                else
                    LastWasVisible = true;
                return !LastWasVisible;
            }
            #endregion

            private bool isSecondLineShowingStarvedTimer = false;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                {
                    Buffer.Add( "<color=#ddff3d>Spectator Mode" );
                    return;
                }
                PlayerTypeData playerType = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType == null || playerType.InternalName == "Spectator" )
                {
                    Buffer.Add( "<color=#ddff3d>Spectator Mode" );
                    return;
                }

                int integerToShow = localFaction.StoredMetal.IntValue;

                bool loggedStarvingAlready = false;
                isSecondLineShowingStarvedTimer = false;

                //PART ONE
                if ( localFaction.LastFrame_TotalMetalFlowRequested > 0 &&
                     localFaction.LastFrame_MetalFlowRequestPortionMet < FInt.One &&
                     localFaction.LastFrame_MetalProduced > FInt.Zero )
                {
                    Buffer.Add( "<color=#ff0000>" );
                    {
                        Buffer.Add( "Drain" );

                        if ( localFaction.LastFrame_MetalFlowRequestPortionMet < FInt.One && localFaction.LastFrame_MetalProduced > FInt.Zero )
                        {
                            FInt percent = localFaction.LastFrame_MetalFlowRequestPortionMet * 100;
                            int percentAsInt = percent.IntValue;
                            Buffer.Add( " (" ).Add( percentAsInt.ToString() ).Add( "%)" );
                            loggedStarvingAlready = true;
                        }
                    }

                    Buffer.Add( "</color>" );
                }
                else
                {
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, integerToShow, true, false );
                }

                //PART TWO
                Buffer.Add( "     " );

                if ( localFaction.MetalStorage == localFaction.StoredMetal.IntValue &&
                     localFaction.LastFrame_MetalSpent < localFaction.LastFrame_MetalProduced )
                {
                    Buffer.Add( "<color=#ffda47><size=80%>" ).Add( "Aiding Civil Authorities" ).Add( "</size></color>" );
                }
                else if ( localFaction.LastFrame_MetalFlowRequestPortionMet < FInt.One &&
                     localFaction.LastFrame_MetalProduced > FInt.Zero &&
                     localFaction.LastFrame_TotalMetalFlowRequested > 0 )
                {
                    timeMetalSpentHasBeenZero = 0f;
                    if ( loggedStarvingAlready )
                    {
                        isSecondLineShowingStarvedTimer = true;
                        int framesLeft = (localFaction.LastFrame_TotalMetalFlowProjectedRequests / localFaction.LastFrame_MetalProduced).GetNearestIntPreferringHigher();
                        int secondsLeft = Mathf.CeilToInt( framesLeft * World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim );
                        Buffer.AddHoursAndMinutes( secondsLeft );
                        if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) )
                        {
                            FInt pct = localFaction.LastFrame_MetalFlowRequestPortionMet * 100;
                            ArcenExternalUIUtilities.AppendBar( Buffer, pct.IntValue, EntityText.GetProportionalStrengthColor( pct.ToFloat() / 100f ), 8 );
                        }
                    }
                    else
                    {
                        FInt percent = localFaction.LastFrame_MetalFlowRequestPortionMet * 100;
                        int percentAsInt = percent.IntValue;
                        Buffer.Add( "<color=#BBBB00>" ).Add( percentAsInt ).Add( "%" ).Add( "</color>" );
                        if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) )
                            ArcenExternalUIUtilities.AppendBar( Buffer, percentAsInt, EntityText.GetProportionalStrengthColor( percent.ToFloat() / 100f ), 8 );
                    }
                }
                else
                {
                    if ( localFaction.LastFrame_TotalMetalFlowRequested > 0 )
                    {
                        timeMetalSpentHasBeenZero = 0f;
                        FInt amountSpentLastFrame = localFaction.LastFrame_MetalSpent;
                        FInt incomeLastFrame = localFaction.LastFrame_MetalProduced;
                        FInt netIncome = incomeLastFrame - amountSpentLastFrame;
                        if ( netIncome > 0 )
                            Buffer.Add( "+" );
                        Buffer.AddNumberMoreReadable( Mathf.CeilToInt( netIncome.ToFloatNonSim() / World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim ) );
                        Buffer.Add( "/s" );
                    }
                    else
                    {
                        if ( timeMetalSpentHasBeenZero < 1f )
                            timeMetalSpentHasBeenZero += Engine_Universal.UnscaledDeltaTime;

                        if ( timeMetalSpentHasBeenZero > 0.8f )
                        {
                            FInt incomeLastFrame = localFaction.LastFrame_MetalProduced;
                            Buffer.Add( "+" );
                            Buffer.Add( Mathf.CeilToInt( incomeLastFrame.ToFloatNonSim() / World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim ) );
                            Buffer.Add( "/s" );
                        }
                    }
                }
            }

            public override void HandleMouseover()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                if ( IsDysonSidekick )
                {
                    DysonSidekickFactionBaseInfo info = localFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Metal: Required to build things.  The Dyson Sidekick gets metal income differently from the player. To get metal you need to build Metal Generators; these are primarily available at the Neinzul Stronghold, but the Spire or Zenith can build some as well.\n\nYour metal income is <color=#ccccee>" + info.MetalIncomeLastSecond.ToString() + "</color> per second.\nYour science income is <color=#7CE9FF>" + info.ScienceIncomeLastSecond.ToString() +"</color> per second.\nYour hacking income is <color=#dd3377>" + info.HackingIncomeLastSecond.ToString() +"</color> per second.\n\n Left clicking will show more detailed economic status (and other critical information).");
                    return;
                }
                int totalIncome = Mathf.CeilToInt( localFaction.LastFrame_MetalProduced.ToFloatNonSim() / World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim );
                int totalAmountSpend = Mathf.CeilToInt( localFaction.LastFrame_MetalSpent.ToFloatNonSim() / World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim );

                Int64 balanceWithCivilianAuthorities = localFaction.MetalSentToCivilianAuthorities - localFaction.MetalReturnedFromCivilianAuthorities;
                int amountToReturn = ExternalConstants.Instance.GetRepaymentAmountForAmountGivenToCivilAuthorities( balanceWithCivilianAuthorities );

                if ( totalIncome > 0 )
                {

                    if ( !isSecondLineShowingStarvedTimer )
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Metal: Required to build things. Your max metal is <color=#ccccee>" + localFaction.MetalStorage.ToString( "#,##0" ) + 
                            "</color>.  The number on the right shows the rate at which you are gaining or losing it.\n\nTotal Stored: <color=#ccccee>" + localFaction.StoredMetal.IntValue.ToString( "#,##0" ) + "</color>" + 
                            "\nTotal Inflow: <color=#ccccee>" + totalIncome.ToString( "#,##0" ) + "</color>" + "\nTotal Outflow: <color=#ffa1a1>" + totalAmountSpend.ToString( "#,##0" ) +
                            "</color>\nBalance With Civilian Authorities: <color=#ffd940>" + balanceWithCivilianAuthorities.ToString( "#,##0" ) +
                            "</color>  (Repayment Rate: <color=#ffe479>" + amountToReturn.ToString( "#,##0" ) + "/sec</color> in times of need)" +
                            "\n\nLeft clicking on this icon will show active metal flows." );
                    else
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Metal: Required to build things. Your max metal is <color=#ccccee>" + localFaction.MetalStorage.ToString( "#,##0" ) + 
                            "</color>.  <color=#ffa1a1>At the moment, you have run out of metal and do not have enough income to keep all your build projects running.\n\nThe number on the right is currently showing the estimated time left until your build projects will complete.</color>\n\nTotal Stored: <color=#ccccee>" + 
                            localFaction.StoredMetal.IntValue.ToString( "#,##0" ) + "</color>" + "\nTotal Inflow: <color=#ccccee>" + totalIncome.ToString( "#,##0" ) + "</color>" + "\nTotal Outflow: <color=#ffa1a1>" + 
                            totalAmountSpend.ToString( "#,##0" ) +
                            "</color>\nBalance With Civilian Authorities: <color=#ffd940>" + balanceWithCivilianAuthorities.ToString( "#,##0" ) +
                            "</color>  (Repayment Rate: <color=#ffe479>" + amountToReturn.ToString( "#,##0" ) + "/sec</color> in times of need)" +
                            "\n\nLeft clicking on this icon will show active metal flows." );
                }
                else
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Metal: Required to build things.  After you un-pause the game, the number on the right will show the rate at which you are gaining or losing it.\n\nTotal Stored: <color=#ccccee>" + 
                        localFaction.StoredMetal.IntValue.ToString( "#,##0" ) + "</color>" + "\nTotal Inflow: <color=#ccccee>" + totalIncome.ToString( "#,##0" ) +
                        "</color>\nBalance With Civilian Authorities: <color=#ffd940>" + balanceWithCivilianAuthorities.ToString( "#,##0" ) +
                        "</color>  (Repayment Rate: <color=#ffe479>" + amountToReturn.ToString( "#,##0" ) + "/sec</color> in times of need)" +
                        "\n\nLeft clicking on this icon will show active metal flows." );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                if ( IsDysonSidekick )
                {
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Dyson Sidekick Income", "Close",
                            delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetDysonSidekickIncome( Buffer ); } );
                    return MouseHandlingResult.None;
                }

                if ( input.LeftButtonClicked )
                {
                    Window_ModalSelfUpdatingTextWindow_UltraWide.Instance.Open( 0.5f, 2f, "Current Metal Flows", "Close",
                    delegate ( ArcenDoubleCharacterBuffer Buffer )
                    {
                        Buffer.Add( Faction.LastSeenMetalFlows );

                        //priorBufferString = Buffer.GetStringAndResetForNextUpdate();
                        return true;
                    } );
                }
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tPlanetName
        public class tPlanetName : ButtonAbstractBase
        {
            public static tPlanetName Instance;
            public tPlanetName() { Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                Buffer.Add( planet.Name );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( input.RightButtonClicked )
                {
                    Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                    if ( pFaction != null )
                        EndpointFunctions.TogglePlanetFactionBooleanFlag( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), pFaction, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, PlanetFactionBooleanFlag.DoNotPathThrough );
                }
                else
                    EndpointFunctions.ToggleGalaxyMap();

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                string text = ArcenExternalUIUtilities.GetPlanetNameTooltipForLocalPlanet();
                if ( ArcenStrings.IsEmpty( text ) )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction != null )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                    if ( pFaction != null )
                    {
                        text += "\nRight Clicking on this will toggle whether your ships will pass through this planet.";
                        if ( pFaction != null && pFaction.GetPlanetFactionBooleanFlag( PlanetFactionBooleanFlag.DoNotPathThrough ) )
                            text += "\nYour ships will NOT pass through this planet.";
                    }
                }

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, text );
            }
        }
        #endregion

        #region tEncyclopedia
        public class tEncyclopedia : ButtonAbstractBase
        {
            public static tEncyclopedia Instance;
            public tEncyclopedia() { Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_UnitEncyclopedia.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                string text = "The unit encyclopedia is very helpful -- it allows you to sort, filter, and categorize units, and find what you need.  When opened during the game, you can also see specifically what various factions have (fog of war notwithstanding).";

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, text );
            }
        }
        #endregion

        #region tEnergy
        public class tEnergy : ButtonAbstractBase
        {
            public static tEnergy Instance;
            public tEnergy() { Instance = this; }

            public static bool LastWasVisible = true;

            #region GetShouldBeHidden
            public override bool GetShouldBeHidden()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction != null )
                {
                    PlayerTypeData playerTypeData = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( !playerTypeData.UsesEnergyAndFuel )
                        LastWasVisible = false;
                    else
                        LastWasVisible = true;
                }
                else
                    LastWasVisible = true;
                return !LastWasVisible;
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, localFaction.NetEnergy, true, false );
            }

            public override void HandleMouseover()
            {
                string text = ArcenExternalUIUtilities.GetEnergyTooltip();
                if( ArcenStrings.IsEmpty( text ) )
                    return;
                text += "\n\nGoing below " + ExternalConstants.Instance.AmountEnergyHasToGoBelowToBrownout + " energy (likely by having a Command Station destroyed) for " + 
                    (ExternalConstants.Instance.FramesToWaitBeforeBrownoutStarts / 10 ) +
                    " seconds will cause a <color=#cc8400>Brownout</color>. During a <color=#cc8400>Brownout</color>, all of your Forcefields are disabled. When power is restored, they take " +
                    ExternalConstants.Instance.SecondsToWaitBeforeBrownoutEnds +
                    " seconds to come back online. \n\nClicking on this icon will give you a detailed breakdown of energy usage and production.";
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, text );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.5f, 2f, "Current Energy Production and Consumption", "Close",
                      delegate( ArcenDoubleCharacterBuffer Buffer ) { return GetEnergyData( Buffer ); } );
                return MouseHandlingResult.None;
            }

            private static readonly List<PlannedMetalFlow> handledFlows = List<PlannedMetalFlow>.Create_WillNeverBeGCed( 500, "Window_ResourceBar-tEnergy-handledFlows", 500 );

            private static readonly List<KeyValuePair<Fleet, int>> workingFleets = List<KeyValuePair<Fleet, int>>.Create_WillNeverBeGCed( 15, "Window_ResourceBar-tEnergy-workingFleets", 15 );
            private static readonly List<KeyValuePair<Planet, int>> workingPlanets = List<KeyValuePair<Planet, int>>.Create_WillNeverBeGCed( 15, "Window_ResourceBar-tEnergy-energyIncomPerPlanetForLocalPlayer",15 );

            public static bool GetEnergyData( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return false;
                return GetEnergyData( localFaction, Buffer );
            }

            public static bool GetEnergyData( Faction forFaction, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( forFaction == null )
                    return false;

                if ( forFaction.UI_EnergyGiftedFromMe > 0 )
                    Buffer.Add( "Energy sent from me to other empires: <color=#FFDE00>" ).AddNumberMoreReadable( forFaction.UI_EnergyGiftedFromMe ).Add( "</color>\n\n" );

                workingPlanets.Clear();
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    int consumed = planet.EnergyCount_energyConsumed_Final;
                    if ( consumed != 0 )
                    {
                        //we do this instead of just using the EnergyCount_energyConsumed_Final directly to avoid Sort errors below from cross-thread updates
                        workingPlanets.Add( new KeyValuePair<Planet, int>( planet, consumed ) );
                    }
                }

                workingFleets.Clear();
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( forFaction, FleetStatus.AnyStatus ) )
                {
                    int consumed = fleet.EnergyCount_energyConsumed_Final;
                    if ( consumed != 0 )
                    {
                        //we do this instead of just using the EnergyCount_energyConsumed_Final directly to avoid Sort errors below from cross-thread updates
                        workingFleets.Add( new KeyValuePair<Fleet, int>( fleet, consumed ) );
                    }
                }

                workingPlanets.Sort( static delegate ( KeyValuePair<Planet, int> L, KeyValuePair<Planet, int> R )
                {
                    int val = R.Value.CompareTo( L.Value ); //desc
                    if ( val != 0 )
                        return val;
                    return L.Key.Name.CompareTo( R.Key.Name );
                } );

                workingFleets.Sort( static delegate ( KeyValuePair<Fleet, int> L, KeyValuePair<Fleet, int> R )
                {
                    int val = R.Value.CompareTo( L.Value ); //desc
                    if ( val != 0 )
                        return val;
                    return L.Key.NameRaw.CompareTo( R.Key.NameRaw );
                } );

                if ( workingPlanets.Count > 0 )
                {
                    Buffer.Add( "Energy consumed per planet:\n" );
                    for ( int i = 0; i < workingPlanets.Count; i++ )
                    {
                        KeyValuePair<Planet, int> pair = workingPlanets[i];
                        Buffer.Add( "\t" ).Add( pair.Key.Name, "7fe0f2" );
                        Buffer.Add( " consumes " ).Add( ((pair.Value) / 1000).ToString( "#,##0" ) + "K", "e59400" ).Add( " (x" ).Add( pair.Key.EnergyCount_numUnits_Final ).Add( ")\n" );
                    }
                    Buffer.Add( "\n\n" );
                }

                if ( workingFleets.Count > 0 )
                {
                    Buffer.Add( "Energy consumed per fleet:\n" );
                    for ( int i = 0; i < workingFleets.Count; i++ )
                    {
                        KeyValuePair<Fleet, int> pair = workingFleets[i];
                        Buffer.Add( "\t" ).Add( pair.Key.GetName(), "7ff27f" );
                        Buffer.Add( " consumes " ).Add( ((pair.Value) / 1000).ToString( "#,##0" ) + "K", "e59400" ).Add( " (x").Add( pair.Key.EnergyCount_numUnits_Final ).Add( ")\n" );
                    }
                    Buffer.Add( "\n\n" );
                }

                workingPlanets.Clear();
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.LocalPlayer_EnergyProducedPerPlanet_ForUIOnly_Final != 0 )
                    {
                        //we do this instead of just using the LocalPlayer_EnergyProducedPerPlanet_ForUIOnly directly to avoid Sort errors below from cross-thread updates
                        workingPlanets.Add( new KeyValuePair<Planet, int>( planet, planet.LocalPlayer_EnergyProducedPerPlanet_ForUIOnly_Final ) );
                    }
                }

                if ( workingPlanets.Count > 0 )
                {
                    Buffer.Add( "Energy produced per planet:\n" );
                    workingPlanets.Sort( static delegate ( KeyValuePair<Planet, int> L, KeyValuePair<Planet, int> R )
                    {
                        int val = R.Value.CompareTo( L.Value ); //desc
                        if ( val != 0 )
                            return val;
                        return L.Key.Name.CompareTo( R.Key.Name );
                    } );
                    for ( int i = 0; i < workingPlanets.Count; i++ )
                    {
                        KeyValuePair<Planet, int> pair = workingPlanets[i];
                        Buffer.Add( "\t" ).Add( pair.Key.Name, "faf866" );
                        Buffer.Add( " produces " ).Add( ((pair.Value) / 1000).ToString( "#,##0" ) + "K", "e59400" ).Add( "\n" );
                    }
                    Buffer.Add( "\n\n" );
                }

                if ( forFaction.UI_EnergyGiftedToMe > 0 )
                {
                    Buffer.Add( "Energy sent to me from other empires: <color=#e59400>" ).AddNumberMoreReadable( forFaction.UI_EnergyGiftedToMe ).Add( "</color>\n" );
                    Buffer.Add( "\n\n" );
                }

                return true;
            }
        }
        #endregion

        public class tFuelArgon : tFuelBase
        {
            public static tFuelArgon Instance;
            public tFuelArgon() { Instance = this; }

            public override ResourceType FuelType => ResourceType.FuelArgon;
        }

        public class tFuelXenon : tFuelBase
        {
            public static tFuelXenon Instance;
            public tFuelXenon() { Instance = this; }

            public override ResourceType FuelType => ResourceType.FuelXenon;
        }

        public class tFuelRadon : tFuelBase
        {
            public static tFuelRadon Instance;
            public tFuelRadon() { Instance = this; }

            public override ResourceType FuelType => ResourceType.FuelRadon;
        }

        #region tFuelBase
        public abstract class tFuelBase: ButtonAbstractBase
        {
            public abstract ResourceType FuelType { get; }

            public static bool LastWasVisible = true;

            #region GetShouldBeHidden
            public override bool GetShouldBeHidden()
            {
                if ( World_AIW2.Instance.IsFuelEnabled )
                {
                    Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if ( localFaction != null )
                    {
                        PlayerTypeData playerTypeData = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( !playerTypeData.UsesEnergyAndFuel )
                            LastWasVisible = false;
                        else
                            LastWasVisible = true;
                    }
                    else
                        LastWasVisible = false;
                }
                else
                    LastWasVisible = false;
                return !LastWasVisible;
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                int fuelAmount = 0;
                switch ( FuelType )
                {
                    case ResourceType.FuelArgon:
                        fuelAmount = localFaction.NetFuelArgon;
                        break;
                    case ResourceType.FuelRadon:
                        fuelAmount = localFaction.NetFuelRadon;
                        break;
                    case ResourceType.FuelXenon:
                        fuelAmount = localFaction.NetFuelXenon;
                        break;
                }
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, fuelAmount, true, false );
            }

            public override void HandleMouseover()
            {
                string text = ArcenExternalUIUtilities.GetFuelTooltip( this.FuelType );
                if ( ArcenStrings.IsEmpty( text ) )
                    return;
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, text );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                string fuelName = string.Empty;
                switch ( FuelType )
                {
                    case ResourceType.FuelArgon:
                        fuelName = "Argon Fuel";
                        break;
                    case ResourceType.FuelRadon:
                        fuelName = "Radon Fuel";
                        break;
                    case ResourceType.FuelXenon:
                        fuelName = "Xenon Fuel";
                        break;
                }

                Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.5f, 2f, "Current " + fuelName + " Production and Consumption", "Close",
                      delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetFuelData( Buffer, FuelType ); } );
                return MouseHandlingResult.None;
            }

            private static readonly List<PlannedMetalFlow> handledFlows = List<PlannedMetalFlow>.Create_WillNeverBeGCed( 500, "Window_ResourceBar-tFuel-handledFlows", 500 );

            private static readonly List<KeyValuePair<Fleet, int>> workingFleets = List<KeyValuePair<Fleet, int>>.Create_WillNeverBeGCed( 15, "Window_ResourceBar-tFuel-workingFleets", 15 );
            private static readonly List<KeyValuePair<Planet, int>> workingPlanets = List<KeyValuePair<Planet, int>>.Create_WillNeverBeGCed( 15, "Window_ResourceBar-tFuel-energyIncomPerPlanetForLocalPlayer", 15 );

            public static bool GetFuelData( ArcenDoubleCharacterBuffer Buffer, ResourceType FuelType )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return false;
                return GetFuelData( localFaction, FuelType, Buffer );
            }

            public static bool GetFuelData( Faction forFaction, ResourceType FuelType, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( forFaction == null )
                    return false;

                string fuelName = string.Empty;
                string fuelUse = string.Empty;
                string colorGood = string.Empty;
                string colorTotal = string.Empty;
                int consumedTotal = 0;
                int extraConsumed = 0;
                int producedTotal = 0;
                FInt overuseRatio = FInt.One;
                switch ( FuelType )
                {
                    case ResourceType.FuelArgon:
                        fuelName = "Argon Fuel";
                        fuelUse = "Argon is a global resource required to run your main combat ships.";
                        colorGood = "ff8e32";
                        colorTotal = "eb481d";
                        consumedTotal = forFaction.FuelArgonConsumption;
                        producedTotal = forFaction.FuelArgonProduction;
                        extraConsumed = World_AIW2.Instance.PermaSpentArgon;
                        overuseRatio = forFaction.FuelArgonOveruseRatio;
                        break;
                    case ResourceType.FuelRadon:
                        fuelName = "Radon Fuel";
                        fuelUse = "Radon is a global resource required to run your turrets and forcefields.";
                        colorGood = "be69ff";
                        colorTotal = "9622d8";
                        consumedTotal = forFaction.FuelRadonConsumption;
                        producedTotal = forFaction.FuelRadonProduction;
                        extraConsumed = World_AIW2.Instance.PermaSpentRadon;
                        overuseRatio = forFaction.FuelRadonOveruseRatio;
                        break;
                    case ResourceType.FuelXenon:
                        fuelName = "Xenon Fuel";
                        fuelUse = "Xenon is a global resource required to run your officers, elites, and outguard.";
                        colorGood = "5bcbff";
                        colorTotal = "28a8e3";
                        consumedTotal = forFaction.FuelXenonConsumption;
                        producedTotal = forFaction.FuelXenonProduction;
                        extraConsumed = World_AIW2.Instance.PermaSpentXenon;
                        overuseRatio = forFaction.FuelXenonOveruseRatio;
                        break;
                }

                if ( extraConsumed > 0 )
                {
                    Buffer.Add( "\nExtra " ).Add( fuelName ).Add( " Perma-Consumed From Past Actions: <color=#" ).Add( colorGood ).Add( ">" )
                        .AddNumberMoreReadable( extraConsumed ).Add( "</color>\n<size=80%>Usually perma-consumption is from things like hacking to contact Outguard.\n</size>" );
                }

                workingPlanets.Clear();
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    int consumed = 0;
                    switch (FuelType)
                    {
                        case ResourceType.FuelArgon:
                            consumed = planet.FuelArgonCount_Consumed_Final;
                            break;
                        case ResourceType.FuelRadon:
                            consumed = planet.FuelRadonCount_Consumed_Final;
                            break;
                        case ResourceType.FuelXenon:
                            consumed = planet.FuelXenonCount_Consumed_Final;
                            break;
                    }
                    if ( consumed != 0 )
                    {
                        //we do this instead of just using the EnergyCount_energyConsumed_Final directly to avoid Sort errors below from cross-thread updates
                        workingPlanets.Add( new KeyValuePair<Planet, int>( planet, consumed ) );
                    }
                }

                workingFleets.Clear();
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( forFaction, FleetStatus.AnyStatus ) )
                {
                    int consumed = 0;
                    switch ( FuelType )
                    {
                        case ResourceType.FuelArgon:
                            consumed = fleet.FuelArgonCount_Consumed_Final;
                            break;
                        case ResourceType.FuelRadon:
                            consumed = fleet.FuelRadonCount_Consumed_Final;
                            break;
                        case ResourceType.FuelXenon:
                            consumed = fleet.FuelXenonCount_Consumed_Final;
                            break;
                    }
                    if ( consumed != 0 )
                    {
                        //we do this instead of just using the EnergyCount_energyConsumed_Final directly to avoid Sort errors below from cross-thread updates
                        workingFleets.Add( new KeyValuePair<Fleet, int>( fleet, consumed ) );
                    }
                }

                workingPlanets.Sort( static delegate ( KeyValuePair<Planet, int> L, KeyValuePair<Planet, int> R )
                {
                    int val = R.Value.CompareTo( L.Value ); //desc
                    if ( val != 0 )
                        return val;
                    return L.Key.Name.CompareTo( R.Key.Name );
                } );

                workingFleets.Sort( static delegate ( KeyValuePair<Fleet, int> L, KeyValuePair<Fleet, int> R )
                {
                    int val = R.Value.CompareTo( L.Value ); //desc
                    if ( val != 0 )
                        return val;
                    return L.Key.NameRaw.CompareTo( R.Key.NameRaw );
                } );

                if ( workingPlanets.Count > 0 )
                {
                    Buffer.Add( fuelName ).Add( " consumed per planet:\n" );
                    for ( int i = 0; i < workingPlanets.Count; i++ )
                    {
                        KeyValuePair<Planet, int> pair = workingPlanets[i];
                        Buffer.Add( "\t" ).Add( pair.Key.Name, "7fe0f2" );
                        Buffer.Add( " consumes " ).Add( ((pair.Value) / 1000).ToString( "#,##0" ), colorGood ).Add( "\n" );
                    }
                    Buffer.Add( "\n\n" );
                }

                if ( workingFleets.Count > 0 )
                {
                    Buffer.Add( fuelName ).Add( " consumed per fleet:\n" );
                    for ( int i = 0; i < workingFleets.Count; i++ )
                    {
                        KeyValuePair<Fleet, int> pair = workingFleets[i];
                        Buffer.Add( "\t" ).Add( pair.Key.GetName(), "7ff27f" );
                        Buffer.Add( " consumes " ).Add( ((pair.Value) / 1000).ToString( "#,##0" ), colorGood ).Add( "\n" );
                    }
                    Buffer.Add( "\n\n" );
                }

                workingPlanets.Clear();
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    int produced = 0;
                    switch ( FuelType )
                    {
                        case ResourceType.FuelArgon:
                            produced = planet.LocalPlayer_FuelArgonProducedPerPlanet_ForUIOnly_Final;
                            break;
                        case ResourceType.FuelRadon:
                            produced = planet.LocalPlayer_FuelRadonProducedPerPlanet_ForUIOnly_Final;
                            break;
                        case ResourceType.FuelXenon:
                            produced = planet.LocalPlayer_FuelXenonProducedPerPlanet_ForUIOnly_Final;
                            break;
                    }
                    if ( produced != 0 )
                    {
                        //we do this instead of just using the LocalPlayer_EnergyProducedPerPlanet_ForUIOnly directly to avoid Sort errors below from cross-thread updates
                        workingPlanets.Add( new KeyValuePair<Planet, int>( planet, produced ) );
                    }
                }

                if ( workingPlanets.Count > 0 )
                {
                    Buffer.Add( fuelName ).Add( "  produced per planet:\n" );
                    workingPlanets.Sort( static delegate ( KeyValuePair<Planet, int> L, KeyValuePair<Planet, int> R )
                    {
                        int val = R.Value.CompareTo( L.Value ); //desc
                        if ( val != 0 )
                            return val;
                        return L.Key.Name.CompareTo( R.Key.Name );
                    } );
                    for ( int i = 0; i < workingPlanets.Count; i++ )
                    {
                        KeyValuePair<Planet, int> pair = workingPlanets[i];
                        Buffer.Add( "\t" ).Add( pair.Key.Name, "faf866" );
                        Buffer.Add( " produces " ).AddNumberMoreReadable( ((pair.Value) / 1000), "e59400" ).Add( "\n" );
                    }
                    Buffer.Add( "\n\n" );
                }

                return true;
            }
        }
        #endregion

        #region tScience
        public class tScience : ButtonAbstractBase
        {
            public static tScience Instance;
            public tScience() { Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;

                int scienceValue = localFaction.StoredScience.IntValue;

                if ( scienceValue >= 10000 )
                    Buffer.Add( "<size=90%>" );

                Buffer.AddNumberMoreReadable( scienceValue );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ResourceBar-tScience-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                tooltipBuffer.Add( Window_InGameSidebarScience.ScienceTooltipText );

                tooltipBuffer.Add( "\n\n" ).Add( "Current Science: <color=#7CE9FF>" ).AddNumberMoreReadable( localFaction.StoredScience.IntValue ).Add("</color>");
                bool printedHeader = false;
                int totalScienceAvailable = 0;
                for ( int i = 0; i < World_AIW2.Instance.AllPlayerFactions.Count; i++ )
                {
                    Faction playerFac = World_AIW2.Instance.AllPlayerFactions[i];
                    if(playerFac.PlanetsHavingScienceExtracted.Count > 0)
                    {
                        if ( !printedHeader )
                        {
                            printedHeader = true;
                            tooltipBuffer.Add( "\n" ).Add( "Science is being extracted from:\n" );
                        }
                        for(int j = 0; j < playerFac.PlanetsHavingScienceExtracted.Count; j++)
                        {
                            Planet planet = playerFac.PlanetsHavingScienceExtracted[j];
                            tooltipBuffer.Add("\t").AddFactionColoredString(planet.Name, playerFac).Add( ":  <color=#7CE9FF>" ).AddNumberMoreReadable( planet.GetScienceLeftForHumans().IntValue ).Add("</color> science left.").Add("\n");
                            totalScienceAvailable += planet.GetScienceLeftForHumans().IntValue;
                        }
                    }
                }
                if( printedHeader )
                    tooltipBuffer.Add( "Total science left to collect: <color=#7CE9FF>" + totalScienceAvailable +"</color>.");

                // tooltipBuffer.Add( "\n" ).Add( "Current Ark Upgrade Points: " ).AddNumberMoreReadable( localFaction.StoredArkUpgradePoints.IntValue );
                // tooltipBuffer.Add( "\n" ).Add( "Current Destruction Points: " ).AddNumberMoreReadable( localFaction.StoredDestructionPoints.IntValue );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "History of Tech Unlocks", "Close",
                    delegate( ArcenDoubleCharacterBuffer Buffer ) { return GetTechHistory( Buffer ); } );
                return MouseHandlingResult.None;
            }

            public static bool GetTechHistory( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return false;
                return GetTechHistory( localFaction, Buffer );
            }

            public static bool GetTechHistory( Faction forFaction, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( forFaction == null )
                    return false;
                if ( forFaction.TechHistory.Count == 0 )
                {
                    Buffer.Add("没有历史记录");
                    return false;
                }
                int totalCost = forFaction.GetTotalSpentScience();
                Buffer.Add("你共花费了 ").AddNumberMoreReadable( totalCost, "4ebeff" ).Add( " 科技。\n");
                for ( int i = forFaction.TechHistory.Count - 1; i >= 0; i-- )
                {
                    TechHistoryEvent tEvent = forFaction.TechHistory[i];
                    tEvent.AppendToBuffer( forFaction, Buffer );
                }
                return true;
            }


        }
        #endregion

        #region tAIP
        public class tAIP : ButtonAbstractBase
        {
            public static tAIP Instance;
            public tAIP() { Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                FInt highestAIP = FactionUtilityMethods.Instance.GetCurrentAIP();
                Buffer.AddNumberMoreReadable( highestAIP.IntValue ).Add( " AIP" );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ResourceBar-tAIP-tooltipBuffer" );
            public override void HandleMouseover()
            {
                AIDifficulty lowestDifficulty = null;
                AIDifficulty highestDifficulty = null;
                bool areReconquestWavesEnabled = false;

                int aipFloor = GlobalAIWorldBaseInfo.Instance.AIProgress_Floor.IntValue;
                int aipNeverReducesBelow = GlobalAIWorldBaseInfo.Instance.AIPNeverReducesBelow;
                int aipTotal = GlobalAIWorldBaseInfo.Instance.AIProgress_Total.IntValue;
                int aipReduction = GlobalAIWorldBaseInfo.Instance.AIProgress_Reduction.IntValue;

                int minAIPForWormholeInvasion = -1;
                int maxAIPForWormholeInvasion = -1;
                int minAIPForWormholeBorer = -1;
                int maxAIPForWormholeBorer = -1;

                //int AIPForNextMarkLevel = -1;
                FInt highestAIP = FactionUtilityMethods.Instance.GetCurrentAIP();
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( faction.Type != FactionType.AI )
                        continue;

                    AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if ( factionExternal == null || factionExternal.AIDifficulty == null )
                        continue;

                    if ( lowestDifficulty == null || factionExternal.AIDifficulty.Difficulty < lowestDifficulty.Difficulty )
                        lowestDifficulty = factionExternal.AIDifficulty;
                    if ( highestDifficulty == null || factionExternal.AIDifficulty.Difficulty > highestDifficulty.Difficulty )
                        highestDifficulty = factionExternal.AIDifficulty;
                    bool reconquestWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "ReconquestWave" );
                    if ( reconquestWavesOn )
                        areReconquestWavesEnabled = true;
                    if(factionExternal.AIDifficulty.AIPUnlockWormholeInvasion != -1)
                    {
                        if(factionExternal.AIDifficulty.AIPUnlockWormholeInvasion < minAIPForWormholeInvasion || minAIPForWormholeInvasion == -1)
                            minAIPForWormholeInvasion = factionExternal.AIDifficulty.AIPUnlockWormholeInvasion;
                        if(factionExternal.AIDifficulty.AIPUnlockWormholeInvasion > maxAIPForWormholeInvasion || maxAIPForWormholeInvasion == -1)
                            maxAIPForWormholeInvasion = factionExternal.AIDifficulty.AIPUnlockWormholeInvasion;
                    }
                    if(factionExternal.AIDifficulty.AIPUnlockWormholeBorer != -1)
                    {
                        if(factionExternal.AIDifficulty.AIPUnlockWormholeBorer < minAIPForWormholeBorer || minAIPForWormholeBorer == -1)
                            minAIPForWormholeBorer = factionExternal.AIDifficulty.AIPUnlockWormholeBorer;
                        if(factionExternal.AIDifficulty.AIPUnlockWormholeBorer > maxAIPForWormholeBorer || maxAIPForWormholeBorer == -1)
                            maxAIPForWormholeBorer = factionExternal.AIDifficulty.AIPUnlockWormholeBorer;
                    }
                }

                tooltipBuffer.Add( "AI Progress (AIP)\n\nHow much the AI cares about you (higher is worse news for you).\n<color=#ffbca1>The AI is distracted by other threats outside the galaxy, and your only hope is to not raise AIP so high that it views you as the larger threat." );
                tooltipBuffer.Add( "</color>\n" );

                if ( lowestDifficulty == null )
                    lowestDifficulty = highestDifficulty;
                if ( lowestDifficulty == null )
                    tooltipBuffer.Add( "\nDifficulty data could not be found for current AI(s)." );
                else
                {
                    if ( lowestDifficulty.Difficulty != highestDifficulty.Difficulty )
                    {
                        tooltipBuffer.Add( "\nAI Difficulties Range From: " );
                        tooltipBuffer.Add( lowestDifficulty.DisplayName );
                        tooltipBuffer.Add( " to " );
                        tooltipBuffer.Add( highestDifficulty.DisplayName );
                        if ( highestAIP.IntValue >= lowestDifficulty.AIPUnlockCounterattacks )
                            tooltipBuffer.Add( "\nCounterattacks Unlocked for all AIs", "F5bc10" );
                        else
                        {
                            tooltipBuffer.Add( "\nCounterattacks Unlock At AIP: " );
                            tooltipBuffer.Add( highestDifficulty.AIPUnlockCounterattacks );
                            tooltipBuffer.Add( " to " );
                            tooltipBuffer.Add( lowestDifficulty.AIPUnlockCounterattacks );
                        }
                        if(areReconquestWavesEnabled)
                        {
                            if ( highestAIP.IntValue >= lowestDifficulty.AIPUnlockReconquestWave )
                                tooltipBuffer.Add( "\nReconquest Waves Unlocked for all AIs", "F5bc10" );
                            else
                            {
                                tooltipBuffer.Add( "\nReconquest Waves Unlock At AIP: " );
                                tooltipBuffer.Add( highestDifficulty.AIPUnlockReconquestWave );
                                tooltipBuffer.Add( " to " );
                                tooltipBuffer.Add( lowestDifficulty.AIPUnlockReconquestWave );
                            }
                        }
                        if(minAIPForWormholeBorer != -1 || maxAIPForWormholeBorer != -1)
                        {
                            if ( (minAIPForWormholeBorer == -1 && highestAIP.IntValue >= maxAIPForWormholeBorer ) ||
                                  highestAIP.IntValue >= minAIPForWormholeBorer)
                                tooltipBuffer.Add( "\nWormhole Borers Unlocked for all AIs", "F5bc10" );
                            else
                            {
                                if(minAIPForWormholeBorer == maxAIPForWormholeBorer || minAIPForWormholeBorer == -1)
                                {
                                    tooltipBuffer.Add( "\nWormhole Borer Unlock At AIP: " );
                                    tooltipBuffer.Add( maxAIPForWormholeBorer );
                                }
                                else
                                {
                                    tooltipBuffer.Add( "\nWormhole Borer Unlock At AIP: " );
                                    tooltipBuffer.Add( minAIPForWormholeBorer);
                                    tooltipBuffer.Add( " to " );
                                    tooltipBuffer.Add( maxAIPForWormholeBorer);
                                }
                            }
                        }

                        if(minAIPForWormholeInvasion != -1 || maxAIPForWormholeInvasion != -1)
                        {
                            if ( (minAIPForWormholeInvasion == -1 && highestAIP.IntValue >= maxAIPForWormholeInvasion ) ||
                                  highestAIP.IntValue >= minAIPForWormholeInvasion)
                                tooltipBuffer.Add( "\nWormhole Invasions Unlocked for all AIs", "F5bc10" );
                            else
                            {
                                if(minAIPForWormholeInvasion == maxAIPForWormholeInvasion || minAIPForWormholeInvasion == -1)
                                {
                                    tooltipBuffer.Add( "\nWormhole Invasion Unlock At AIP: " );
                                    tooltipBuffer.Add( maxAIPForWormholeInvasion );
                                }
                                else
                                {
                                    tooltipBuffer.Add( "\nWormhole Invasion Unlock At AIP: " );
                                    tooltipBuffer.Add( minAIPForWormholeInvasion);
                                    tooltipBuffer.Add( " to " );
                                    tooltipBuffer.Add( maxAIPForWormholeInvasion);
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        tooltipBuffer.Add( "\nAI Difficulty: " ).Add( lowestDifficulty.DisplayName ).Add("\n");
                        if ( highestAIP.IntValue >= lowestDifficulty.AIPUnlockCounterattacks )
                            tooltipBuffer.Add( "Counterattacks Unlocked ", "F5bc10" );
                        else
                        {
                            tooltipBuffer.Add( "Counterattacks Unlock At AIP: " );
                            tooltipBuffer.Add( "<color=#ff0000>" + lowestDifficulty.AIPUnlockCounterattacks + "</color>" );
                        }
                        
                        if(areReconquestWavesEnabled)
                        {
                            tooltipBuffer.Add("\n");
                            if ( highestAIP.IntValue >= lowestDifficulty.AIPUnlockReconquestWave )
                                tooltipBuffer.Add( "Reconquest Waves Unlocked ", "F5bc10" );
                            else
                            {
                                tooltipBuffer.Add( "Reconquest Waves Unlock At AIP: " );
                                tooltipBuffer.Add( "<color=#ff0000>" + lowestDifficulty.AIPUnlockReconquestWave + "</color>" );
                            }
                        }
                        
                        if(minAIPForWormholeBorer != -1)
                        {
                            tooltipBuffer.Add("\n");
                            if ( highestAIP.IntValue >= minAIPForWormholeBorer )
                                tooltipBuffer.Add( "Wormhole Borers Unlocked ", "F5bc10" );
                            else
                            {
                                tooltipBuffer.Add( "Wormhole Borers Unlock At AIP: " );
                                tooltipBuffer.Add( "<color=#ff0000>" + minAIPForWormholeBorer + "</color>" );
                            }
                        }
                        
                        if(minAIPForWormholeInvasion != -1)
                        {
                            tooltipBuffer.Add("\n");
                            if ( highestAIP.IntValue >= minAIPForWormholeInvasion )
                                tooltipBuffer.Add( "Wormhole Invasions Unlocked ", "F5bc10" );
                            else
                            {
                                tooltipBuffer.Add( "Wormhole Invasions Unlock At AIP: " );
                                tooltipBuffer.Add( "<color=#ff0000>" + minAIPForWormholeInvasion + "</color>" );
                            }
                        }
                    }
                }

                if ( aipFloor < aipNeverReducesBelow )
                    aipFloor = aipNeverReducesBelow;
                if ( highestDifficulty != null )
                {
                    if ( aipFloor < highestDifficulty.AIPAbsoluteFloor )
                        aipFloor = highestDifficulty.AIPAbsoluteFloor;
                }
                if ( aipFloor > aipTotal )
                    aipFloor = aipTotal;

                tooltipBuffer.Add( "\n\nTotal AIP earned: <color=#ff0000>" + aipTotal + "</color> AIP Reduction: <color=#ffbca1>" + aipReduction +"</color>" );

                tooltipBuffer.Add( " AIP Floor: <color=#ff422e>" + aipFloor + "</color>");
                tooltipBuffer.Add( " AIP Never Reduces Below: <color=#f97331>" + aipNeverReducesBelow + "</color>" );

                int excessAipReduction = aipReduction - aipTotal + aipFloor;
                if (excessAipReduction > 0) {
                    tooltipBuffer.Add( "\nUnused AIP Reduction: ").StartColor( "f97331" ).Add( excessAipReduction).EndColor();
                } else if (excessAipReduction < 0) {
                    tooltipBuffer.Add( "\nAIP above the Floor: ").StartColor( "ff422e" ).Add( -excessAipReduction ).EndColor();;
                }

                tooltipBuffer.Add( "\n\n<color=#ff422e>AIP Floor</color> is the minimum value that AIP can be currently reduced to, and is increased by " ).Add( 
                    (highestDifficulty.AIPFloorMultiplierPercent.ToString() + "%"), "a1ffa1").Add(" of any AIP increase. If reduction would lower AIP below the current floor, it is not wasted - it will instead absorb later increases, so there is no need to delay a reduction.  " );
                tooltipBuffer.Add( "At this difficulty level, the absolute minimum that the AIP can ever go to is also set to be " ).Add( highestDifficulty.AIPAbsoluteFloor ).Add( ".  " );

                int startingAIP = World_AIW2.Instance.Setup.GetIntBySetting( "AIP_Starting" ) * World_AIW2.Instance.EmpireStylePlayerFactions.Count;

                if ( highestDifficulty.AIPAbsoluteFloor > startingAIP && aipTotal < highestDifficulty.AIPAbsoluteFloor )
                {
                    tooltipBuffer.Add( "It is worth noting that your starting AIP is only " ).Add( startingAIP ).Add( ", so the first " ).Add(
                        highestDifficulty.AIPAbsoluteFloor - startingAIP ).Add( " of AIP that you incur are effectively 'free.'" );
                }

                tooltipBuffer.Add( "\n<color=#f97331>AIP Never Reduces Below: " + aipNeverReducesBelow + "</color>" );
                tooltipBuffer.Add( "\nNo matter the AIP Floor, no reduction will happen that would cause the AIP to shrink below this amount, or shrink while it is below this amount." );
                //

                tooltipBuffer.Add("\n");
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++)
                {
                    Faction faction = World_AIW2.Instance.AIFactions[i];
                    AISentinelsCoreData localFactionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    Balance_MarkLevel thisMark = Balance_MarkLevelTable.Instance.Rows[faction.CurrentGeneralMarkLevel];
                    int currentMarkLevelForAI = faction.CurrentGeneralMarkLevel;
                    if ( faction.FactionIsDefeated )
                    {
                        continue;
                    }

                    tooltipBuffer.Add("\nCurrent Mark Level For ").Add( "AI:", faction.FactionCenterColor.ColorHexBrighter).Add(" <color=#" + thisMark.ColorHex + ">" + currentMarkLevelForAI + "</color>.");
                    if ( currentMarkLevelForAI == Balance_MarkLevelTable.Instance.Rows.Count - 1 )
                        tooltipBuffer.Add("\nThis is the Maximum mark level.");
                    else
                    {
                        //this is the normal case
                        Balance_MarkLevel nextMark = Balance_MarkLevelTable.Instance.Rows[currentMarkLevelForAI + 1];
                        int aipForNextMark = localFactionExternal.AIDifficulty.AIPForMarkLevel[currentMarkLevelForAI + 1];
                        int aipUntilNextMark = GlobalAIWorldBaseInfo.Instance.CalculateAIPRemainingUntil(aipForNextMark);
                        tooltipBuffer.Add("\nAIP for next mark level increase: ").StartColor(nextMark.ColorHex).Add(aipForNextMark).EndColor();
                        tooltipBuffer.Add(" (").StartColor(nextMark.ColorHex).Add(aipUntilNextMark).EndColor().Add(" remaining).");
                        if ( localFactionExternal.AIDifficulty.Difficulty >= 10 )
                        {
                            if ( currentMarkLevelForAI < 2 )
                                tooltipBuffer.Add( "Mark 2 will automatically be reached if you have at least 10 strength outside of transports on one of their Mark 7 worlds.  " );
                            if ( currentMarkLevelForAI < 3 )
                                tooltipBuffer.Add( "Mark 3 will automatically be reached if you have at least 10 strength outside of transports on their homeworld.  " );
                        }
                    }
                }
                tooltipBuffer.Add("\n\nThe game will become significantly harder when the AI mark level increases. Note that the AI Mark Level will never go down, regardless of AIP reduction.");
                tooltipBuffer.Add("\n\nClicking on the AIP icon will show you the history of AIP changes.");
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "AIP Change History", "Close",
                    delegate( ArcenDoubleCharacterBuffer Buffer ) { return GetAIPHistory( Buffer ); } );
                return MouseHandlingResult.None;
            }
            private static ProtectedList<AIPChange> localAIPChangeHistory = ProtectedList<AIPChange>.Create_WillNeverBeGCed( 3000, "GlobalAIWorldBaseInfo-AIPChangeHistory" );
            public static bool GetAIPHistory( ArcenDoubleCharacterBuffer Buffer )
            {
                //At the moment changing the AIP for one AI faction changes it for all of them,
                //so I only need to print the AIP for a single faction. This could concievably change someday.
                Buffer.Add("完整 AIP 变化历史：\n");
                Faction faction = World_AIW2.Instance.AIFactions[0];
                if ( faction == null )
                    return false;
                AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                FInt runningTotal = FInt.Zero;
                FInt superterminalChange = FInt.Zero;
                localAIPChangeHistory.Clear( false );
                localAIPChangeHistory.AddRange( GlobalAIWorldBaseInfo.Instance.AIPChangeHistory );
                for ( int j = localAIPChangeHistory.Count - 1; j >= 0; j--)
                {
                    AIPChange change = localAIPChangeHistory[j];
                    if ( change.ResultingAIP == FInt.Zero )
                    {
                        //this is from a save before we tracked the ResultingAIP
                        FInt sum = FInt.Zero;
                        for ( int k = 0; k < localAIPChangeHistory.Count; k++ )
                        {
                            sum += localAIPChangeHistory[k].Change;
                            if ( change == localAIPChangeHistory[k] )
                                break;
                        }
                        change.ResultingAIP = sum;
                    }
                    runningTotal = change.ResultingAIP;
                    if ( change.Reason == AIPChangeReason.Hacking && change.RelatedEntityTypeData != null &&
                         change.RelatedEntityTypeData.GetHasTag("SuperTerminal") )
                    {
                        superterminalChange += change.Change;
                        //consolidate all the Superterminal AIP changes into a single row
                        if ( j > 0)
                        {
                            //only print the last superterminal entry, so look ahead to the next entry
                            AIPChange nextChange = GlobalAIWorldBaseInfo.Instance.AIPChangeHistory[j - 1];
                            if ( (nextChange.Reason == AIPChangeReason.Hacking && nextChange.RelatedEntityTypeData != null &&
                                    nextChange.RelatedEntityTypeData.GetHasTag("SuperTerminal") ) )
                                continue; //if the last thing that changed was superterminal, make sure we print it
                        }
                    }

                    if ( change != null)
                    {
                        string floorOverride = "";
                        FInt runningTotalForOutput = runningTotal;
                        if ( change.Floor != FInt.Zero &&
                             change.Floor.IntValue > runningTotal.IntValue )
                        {
                            floorOverride = " Floor (actual: " + runningTotal.IntValue + ")";
                            runningTotalForOutput = change.Floor;
                        }
                        if ( superterminalChange != FInt.Zero )
                        {
                            Buffer.Add( "<color=#ff0000>" ).Add( runningTotalForOutput.IntValue ).Add( "</color>" ).Add( floorOverride ).Add( ": " );
                            change.AppendStateForInterfaceDisplay_OverrideAIP( Buffer, superterminalChange );
                            Buffer.Add( "\n" );
                            superterminalChange = FInt.Zero;
                        }
                        else
                        {
                            Buffer.Add( "<color=#ff0000>" ).Add( runningTotalForOutput.IntValue ).Add( "</color>" ).Add( floorOverride ).Add( ": " );
                            change.AppendStateForInterfaceDisplay( Buffer );
                            Buffer.Add( "\n" );
                        }
                    }
                }
                Buffer.Add("\n");
                return true;
            }
        }
        #endregion

        #region tHacking
        public class tHacking : ButtonAbstractBase
        {
            public static tHacking Instance;
            public tHacking() { Instance = this; }
            private static bool IsDarkZenithSidekick = false; //the DZ Sidekick doesn't really use hacking, so use this to show economic details
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                IsDarkZenithSidekick = DarkZenithSidekickFactionBaseInfo.GetIsThisADZFaction( localFaction );
                Buffer.AddNumberMoreReadable( localFaction.StoredHacking.IntValue );
            }

            public override void HandleMouseover()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( IsDarkZenithSidekick )
                {
                    string dzFleetsTip = ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled()
                        ? "\nUse the <color=#a1d4ff>DZ Economy</color> entry in the Fleets sidebar to view and control Epistyle production directly."
                        : "\nYou can use the Hacking menu to modify Epistyle production.";
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element,
                        "The Dark Zenith have a different economy, and the player does not have much control over it. " +
                        "Their Terminii will produce resources, which are brought to the Epistyles with Transports.\n" +
                        "They get science by spreading the Fimbulwinter." +
                        dzFleetsTip +
                        "\n\nLeft clicking will show more detailed economic status, right clicking will show Upgrades." );
                    return;
                }

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Hacking: general-purpose nanomachines used to improve your own units, sabotage enemies, or steal from enemies.  Note that you are in a target-rich environment, and likely cannot afford even a third of the hacks available to you.  Choose your hacks wisely, and capture more planets or destroy distribution nodes to get more hacking points.  Also note: the more hacking points you use against an AI, the stronger that AI's response to your hacks will be.  Only points spent against an AI-owned structure count. \n" + GetHackerAddendum() + GetHackingLevelText() + "\n\nClicking on this icon will show a detailed history of all the hacks you have done." );
            }
            private string GetHackingLevelText()
            {
                string output = "";
                //int AIs = 0;
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( faction.Type != FactionType.AI )
                        continue;
                    AIDifficulty difficulty = faction.TryGetAISentinelsCoreData()?.SentinelInfo?.AIDifficulty;
                    FInt hackingSoFar = faction.HackingPointsUsedAgainstThisFaction;
                    int hackingLevel = 0;
                    int hackingPointsForNextLevel = 0;
                    for(int i = 0; i < difficulty.HackingDifficultyLevel.Count; i++)
                    {
                        if(hackingSoFar <= difficulty.HackingDifficultyLevel[i])
                            break;
                        hackingLevel = i;
                    }
                    if ( hackingLevel == difficulty.HackingDifficultyLevel.Count - 1 )
                        hackingPointsForNextLevel = 0;
                    else
                        hackingPointsForNextLevel = difficulty.HackingDifficultyLevel[hackingLevel + 1];
                    FInt multiplier = difficulty.HackingDifficultyMultiplier[hackingLevel];

                    string colorString = faction.FactionCenterColor.ColorHexBrighter;
                    output += "\n<color=#" + colorString + ">" ;
                    output += "AI response level " +  multiplier.ToFloatNonSim().ToString("#,##0.0") + " (";
                    if(hackingLevel == 0)
                        output += "Very Easy";
                    else if(hackingLevel == 1)
                        output += "Easy";
                    else if(hackingLevel == 2)
                        output += "Slothful";
                    else if(hackingLevel == 3)
                        output += "Indifferent";
                    else if(hackingLevel == 4)
                        output += "Moderate";
                    else if(hackingLevel == 5)
                        output += "High";
                    else if(hackingLevel == 6)
                        output += "Extreme";
                    else if(hackingLevel == 7)
                        output += "Terrifying";
                    output += ").</color>";
                    output += "  Note that the hack itself may have a response that is far more aggressive than the generalized AI response component.\n";
                    output += "\tHacking points spent against this faction: <color=#3DE799>" + hackingSoFar.IntValue+ "</color>.\n";
                    if ( hackingPointsForNextLevel > 0 )
                        output += "\tSpending <color=#3DE799>" + (hackingPointsForNextLevel - hackingSoFar.IntValue) + "</color> additional hacking points will increase the response level.";
                }
                return output;
            }
            private string GetHackerAddendum()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return "";
                string output = "";
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                {
                    GameEntity_Squad hacker = fleet.Centerpiece.GetSquad();
                    if ( hacker == null || hacker.ActiveHack == null )
                        continue;
                    GameEntity_Squad target = World_AIW2.Instance.GetEntityByID_Squad( hacker.ActiveHack_Target );
                    if ( target == null )
                        continue;
                    int secondsSoFar = hacker.ActiveHack_DurationThusFar;
                    int totalDuration = hacker.ActiveHack.GetEffectiveHackDuration( World_AIW2.Instance.GetEntityByID_Squad( hacker.ActiveHack_Target ),
                        World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( hacker.ActiveHack_Planet ) );
                    int secondsLeft = totalDuration - secondsSoFar;
                    if ( totalDuration > 0 ) //any hack that has an explicit time duration
                        output += "  <color=#f5a1ff>Time Left For Hacker on " + hacker.GetPlanetName_Safe() +" To Complete Work: " + Engine_Universal.ToHoursAndMinutesString( secondsLeft ) + "</color>\n";
                    else //this is for things with a variable time, like the superterminal hack
                        output += "  <color=#f5a1ff>Time Elapsed For Hacker on " + hacker.GetPlanetName_Safe() +"'s Work: " + Engine_Universal.ToHoursAndMinutesString( secondsSoFar ) + "</color>\n";
                }
                return output;
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( IsDarkZenithSidekick )
                {
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Dark Zenith Sidekick Income", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetDarkZenithSidekickIncome( Buffer, input ); } );
                    return MouseHandlingResult.None;
                }

                Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "History of Hacks", "Close",
                    delegate( ArcenDoubleCharacterBuffer Buffer ) { return GetHackingHistory( Buffer ); } );
                return MouseHandlingResult.None;
            }

            public static bool GetHackingHistory( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return false;
                return GetHackingHistory( localFaction, Buffer );
            }

            public static bool GetHackingHistory( Faction forFaction, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( forFaction == null )
                    return false;
                ExternalFactionBaseInfo factionBaseInfo = forFaction.BaseInfo;
                if ( factionBaseInfo != null )
                {
                    if ( factionBaseInfo.GetCustomHackingHistory( Buffer ) )
                        return true;
                }

                if ( forFaction.HackingHistory.Count == 0 )
                {
                    Buffer.Add("没有历史记录");
                    return false;
                }
                for ( int i = forFaction.HackingHistory.Count - 1; i >= 0; i-- )
                {
                    HackingEvent hEvent = forFaction.HackingHistory[i];
                    Buffer.Add( hEvent.ToString() );
                }

                return true;
            }
        }
        #endregion

        #region tNecromancerEsssence
        public class tNecromancerEsssence : ButtonAbstractBase
        {
            public static tNecromancerEsssence Instance;
            public tNecromancerEsssence() { Instance = this; }

            public static bool LastWasVisible = true;
            private static bool IsDysonSidekick = false;
            private static bool IsScourgeEmpire = false;
            private static bool IsArmadaEmpire = false;
            private static bool IsApkallu = false;

            private static string lastInitializedIcon = string.Empty;
            private static Material normalMaterial = null;
            private static Material hoverMaterial = null;

            #region GetShouldBeHidden
            public override bool GetShouldBeHidden()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                IsDysonSidekick = DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( localFaction );
                IsScourgeEmpire = ScourgeInfusedHumanEmpireFactionBaseInfo.GetIsThisAScourgeEmpireFaction( localFaction );
                IsArmadaEmpire = ArmadaFactionBaseInfo.GetIsThisAnArmadaFaction( localFaction );
                IsApkallu = ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( localFaction );
                LastWasVisible = NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction ) || IsDysonSidekick || IsScourgeEmpire || IsArmadaEmpire || IsApkallu;
                return !LastWasVisible;
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                Buffer.AddNumberMoreReadable( localFaction.StoredFactionResourceOne.IntValue );
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                string iconName = World_AIW2.Instance.GetLocalPlayerFactionOrNull()?.Resource1Icon ?? World_AIW2.Instance.Resource1Icon;
                if ( string.IsNullOrEmpty( iconName ) )
                    return;
                if ( iconName == lastInitializedIcon )
                    return;
                if ( Button.ReferenceImagesOptional == null || Button.ReferenceImagesOptional.Length == 0 )
                    return;

                TextEmbededSprite spriteRow = TextEmbededSpriteTable.Instance.GetRowByName( iconName, LookupSwapAllowed.No, false );
                if ( spriteRow == null || spriteRow.UnitySprite == null )
                    return;

                Button.ReferenceImagesOptional[0].sprite = spriteRow.UnitySprite;

                if ( Button.ReferenceImagesNormalMaterials != null && Button.ReferenceImagesNormalMaterials.Length > 0 &&
                     Button.ReferenceImagesHoverMaterials != null && Button.ReferenceImagesHoverMaterials.Length > 0 &&
                     Button.ReferenceImagesNormalMaterials[0] != null )
                {
                    string colorHex = World_AIW2.Instance.GetLocalPlayerFactionOrNull()?.Resource1Color ?? World_AIW2.Instance.Resource1Color;
                    if ( !string.IsNullOrEmpty( colorHex ) )
                    {
                        Color baseColor = ColorMath.HexToColor( colorHex );
                        float normalFactor = Mathf.Pow( 2f, 1.6f );
                        float hoverFactor = Mathf.Pow( 2f, 2.6f );

                        normalMaterial = Material.Instantiate( Button.ReferenceImagesNormalMaterials[0] );
                        normalMaterial.SetColor( "_Color", new Color( baseColor.r * normalFactor, baseColor.g * normalFactor, baseColor.b * normalFactor, baseColor.a ) );

                        hoverMaterial = Material.Instantiate( Button.ReferenceImagesNormalMaterials[0] );
                        hoverMaterial.SetColor( "_Color", new Color( baseColor.r * hoverFactor, baseColor.g * hoverFactor, baseColor.b * hoverFactor, baseColor.a ) );

                        Button.ReferenceImagesOptional[0].material = normalMaterial;
                        Button.ReferenceImagesNormalMaterials[0] = normalMaterial;
                        Button.ReferenceImagesHoverMaterials[0] = hoverMaterial;
                    }
                }

                lastInitializedIcon = iconName;
            }

            public override void HandleMouseover()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( IsDysonSidekick )
                {
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Dyson Cuendillar: used for flagship and stronghold related upgrades. Click here to see information about your current income." );
                    return;
                }
                if ( IsScourgeEmpire )
                {
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Shows how much Corbomite you have; used for upgrades.\n\nClick on this icon to see the state of your Scourge Vassals." );
                    return;
                }
                if ( IsArmadaEmpire )
                {
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Shows how much Tyderian you have; used for upgrades. Click here to see information about your Mines." );
                    return;
                }
                if ( IsApkallu )
                {
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Shows how much Lapis you have.\n\nClick here to see information about your faction." );
                    return;
                }

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Necromancer Essence: used for flagship and necropolis related upgrades.  Essence is obtained from Rift Hacks and fighting Elderlings; many Elderlings will grant us Essence on their death." );

            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

                if ( localFaction != null )
                {
                    if ( IsScourgeEmpire )
                    {
                        Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Scourge State", "Close",
                            delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetScourgeState( Buffer ); } );
                    }
                    else if ( IsDysonSidekick )
                        Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Dyson Sidekick Income", "Close",
                            delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetDysonSidekickIncome( Buffer ); } );
                    else if ( IsArmadaEmpire )
                        Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Armada Mining Overview", "Close",
                            delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetArmadaOverview( Buffer ); } );
                    else if ( IsApkallu )
                        Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Apkallu Overview", "Close",
                            delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetApkalluOverview( Buffer ); } );

                    else
                        Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Necromancer Resource Acquisition", "Close",
                            delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetNecromancerAcquisitionHistory( Buffer ); } );

                }
                return MouseHandlingResult.None;
            }
            public static bool GetNecromancerAcquisitionHistory( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null || !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction ) )
                    return false;
                return GetNecromancerAcquisitionHistory( localFaction, Buffer );
            }
            public static bool GetNecromancerAcquisitionHistory( Faction forFaction, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( forFaction == null )
                    return false;
                NecromancerEmpireFactionBaseInfo factionBaseInfo = forFaction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                if ( factionBaseInfo.HackingEarnedPerUnitType.Count > 0 )
                    Buffer.Add( "Here is how you got your hacking: \n" );
                int total = 0;
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in factionBaseInfo.HackingEarnedPerUnitType )
                {
                    if ( pair.Key != null && pair.Key.TexEmbedSprite_Icon != null )
                    {
                        Buffer.Add( "<size=50%><voffset=1.5em>" );

                        Buffer.StartDoNotAdvanceXTagIfTrue( pair.Key.TexEmbedSprite_IconBorder != null || pair.Key.TexEmbedSprite_IconOverlay != null );
                        Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_Icon.InternalName )
                            .Add( "\" color=\"#" ).Add( ArcenExternalUIUtilities.HackingTextColor ).Add( "\">" );
                        if ( pair.Key.TexEmbedSprite_IconBorder != null )
                        {
                            Buffer.EndDoNotAdvanceXTagIfTrue( pair.Key.TexEmbedSprite_IconOverlay == null );
                            Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_IconBorder.InternalName )
                                .Add( "\" color=\"#" ).Add( ArcenExternalUIUtilities.HackingTextColor ).Add( "\">" );
                        }
                        if ( pair.Key.TexEmbedSprite_IconOverlay != null )
                        {
                            Buffer.EndDoNotAdvanceXTagIfTrue( true );
                            Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_IconOverlay.InternalName ).Add( "\">" );
                        }
                        Buffer.Add( "</size></voffset> " );
                    }

                    Buffer.Add( pair.Key.GetDisplayName(), "ffa1a1" ).Add( ": " ).Add( pair.Value, "a1ffa1" ).Add( "\n" );
                    total += pair.Value;
                }
                if ( total > 0 )
                    Buffer.Add( "\nTotal " ).Add( ArcenExternalUIUtilities.HackingTextColorAndIcon ).Add( ": " + total ).Add( "\n" );
                total = 0;
                if ( factionBaseInfo.EssenceEarnedPerUnitType.Count > 0 )
                    Buffer.Add( "\nHere is how you got your Essence: \n" );
                string essenceColor = forFaction.Resource1Color.Length > 0 ? forFaction.Resource1Color : World_AIW2.Instance.Resource1Color;
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in factionBaseInfo.EssenceEarnedPerUnitType )
                {
                    if ( pair.Key != null && pair.Key.TexEmbedSprite_Icon != null )
                    {
                        Buffer.Add( "<size=50%><voffset=1.5em>" );

                        Buffer.StartDoNotAdvanceXTagIfTrue( pair.Key.TexEmbedSprite_IconBorder != null || pair.Key.TexEmbedSprite_IconOverlay != null );
                        Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_Icon.InternalName )
                            .Add( "\" color=\"#" ).Add( essenceColor ).Add( "\">" );
                        if ( pair.Key.TexEmbedSprite_IconBorder != null )
                        {
                            Buffer.EndDoNotAdvanceXTagIfTrue( pair.Key.TexEmbedSprite_IconOverlay == null );
                            Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_IconBorder.InternalName )
                                .Add( "\" color=\"#" ).Add( essenceColor ).Add( "\">" );
                        }
                        if ( pair.Key.TexEmbedSprite_IconOverlay != null )
                        {
                            Buffer.EndDoNotAdvanceXTagIfTrue( true );
                            Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_IconOverlay.InternalName ).Add( "\">" );
                        }
                        Buffer.Add( "</size></voffset> " );
                    }

                    Buffer.Add( pair.Key.GetDisplayName(), "ffa1a1" ).Add( ": " ).Add( pair.Value, "a1ffa1" ).Add( "\n" );
                    total += pair.Value;
                }
                if ( total > 0 )
                    Buffer.Add( "\nTotal " ).Add( forFaction.Resource1TextColorAndIcon.Length > 0 ? forFaction.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon ).Add( ": " + total ).Add( "\n" );
                total = 0;
                if ( factionBaseInfo.ScienceEarnedPerUnitType.Count > 0 )
                    Buffer.Add( "\nHere is how you got your Science: \n" );
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in factionBaseInfo.ScienceEarnedPerUnitType )
                {
                    if ( pair.Key != null && pair.Key.TexEmbedSprite_Icon != null )
                    {
                        Buffer.Add( "<size=50%><voffset=1.5em>" );

                        Buffer.StartDoNotAdvanceXTagIfTrue( pair.Key.TexEmbedSprite_IconBorder != null || pair.Key.TexEmbedSprite_IconOverlay != null );
                        Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_Icon.InternalName )
                            .Add( "\" color=\"#" ).Add( ArcenExternalUIUtilities.ScienceTextColor ).Add( "\">" );
                        if ( pair.Key.TexEmbedSprite_IconBorder != null )
                        {
                            Buffer.EndDoNotAdvanceXTagIfTrue( pair.Key.TexEmbedSprite_IconOverlay == null );
                            Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_IconBorder.InternalName )
                                .Add( "\" color=\"#" ).Add( ArcenExternalUIUtilities.ScienceTextColor ).Add( "\">" );
                        }
                        if ( pair.Key.TexEmbedSprite_IconOverlay != null )
                        {
                            Buffer.EndDoNotAdvanceXTagIfTrue( true );
                            Buffer.Add( "<sprite=\"" ).Add( pair.Key.TexEmbedSprite_IconOverlay.InternalName ).Add( "\">" );
                        }
                        Buffer.Add( "</size></voffset> " );
                    }

                    Buffer.Add( pair.Key.GetDisplayName(), "ffa1a1" ).Add( ": " ).Add( pair.Value, "a1ffa1" ).Add( "\n" );
                    total += pair.Value;
                }
                if ( total > 0 )
                    Buffer.Add( "\nTotal " ).Add( ArcenExternalUIUtilities.ScienceTextColorAndIcon ).Add( ": " + total );

                return true;
            }
        }
        #endregion

        #region tFactionResource2
        public class tFactionResource2 : ButtonAbstractBase
        {
            public static tFactionResource2 Instance;
            public tFactionResource2() { Instance = this; }

            public static bool LastWasVisible = true;

            private static string lastInitializedIcon = string.Empty;
            private static Material normalMaterial = null;
            private static Material hoverMaterial = null;

            #region GetShouldBeHidden
            public override bool GetShouldBeHidden()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                LastWasVisible = ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( localFaction );
                return !LastWasVisible;
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                // if ( !string.IsNullOrEmpty( World_AIW2.Instance.Resource2TextColorAndIcon ) )
                //     Buffer.Add( World_AIW2.Instance.Resource2TextColorAndIcon );
                Buffer.AddNumberMoreReadable( localFaction.StoredFactionResourceTwo.IntValue );
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                string iconName = World_AIW2.Instance.Resource2Icon;
                if ( string.IsNullOrEmpty( iconName ) )
                    return;
                if ( iconName == lastInitializedIcon )
                    return;
                if ( Button.ReferenceImagesOptional == null || Button.ReferenceImagesOptional.Length == 0 )
                    return;

                TextEmbededSprite spriteRow = TextEmbededSpriteTable.Instance.GetRowByName( iconName, LookupSwapAllowed.No, false );
                if ( spriteRow == null || spriteRow.UnitySprite == null )
                    return;

                Button.ReferenceImagesOptional[0].sprite = spriteRow.UnitySprite;

                if ( Button.ReferenceImagesNormalMaterials != null && Button.ReferenceImagesNormalMaterials.Length > 0 &&
                     Button.ReferenceImagesHoverMaterials != null && Button.ReferenceImagesHoverMaterials.Length > 0 &&
                     Button.ReferenceImagesNormalMaterials[0] != null )
                {
                    string colorHex = World_AIW2.Instance.Resource2Color;
                    if ( !string.IsNullOrEmpty( colorHex ) )
                    {
                        Color baseColor = ColorMath.HexToColor( colorHex );
                        float normalFactor = Mathf.Pow( 2f, 1.6f );
                        float hoverFactor = Mathf.Pow( 2f, 2.6f );

                        normalMaterial = Material.Instantiate( Button.ReferenceImagesNormalMaterials[0] );
                        normalMaterial.SetColor( "_Color", new Color( baseColor.r * normalFactor, baseColor.g * normalFactor, baseColor.b * normalFactor, baseColor.a ) );

                        hoverMaterial = Material.Instantiate( Button.ReferenceImagesNormalMaterials[0] );
                        hoverMaterial.SetColor( "_Color", new Color( baseColor.r * hoverFactor, baseColor.g * hoverFactor, baseColor.b * hoverFactor, baseColor.a ) );

                        Button.ReferenceImagesOptional[0].material = normalMaterial;
                        Button.ReferenceImagesNormalMaterials[0] = normalMaterial;
                        Button.ReferenceImagesHoverMaterials[0] = hoverMaterial;
                    }
                }

                lastInitializedIcon = iconName;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Shows how much Naphtha you have.\n\nClick here to see your breach history." );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Apkallu Breach History", "Close",
                    delegate ( ArcenDoubleCharacterBuffer Buffer ) { return GetApkalluBreachHistory( Buffer ); } );
                return MouseHandlingResult.None;
            }

            public static bool GetApkalluBreachHistory( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null || !ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( localFaction ) )
                    return false;
                ApkalluFactionBaseInfo info = localFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
                if ( info == null )
                    return false;

                Buffer.Add( "History of the Apkallu counterstrikes in the Apsu.\n", "ffcc88" );
                Buffer.Add( "Here is how the Malware have been harmed:\n\n", "ffcc88" );

                if ( info.BreachHistory.Count == 0 )
                {
                    Buffer.Add( "No breaches completed yet.", "888888" );
                    return true;
                }

                for ( int i = 0; i < info.BreachHistory.Count; i++ )
                {
                    BreachHistoryEntry entry = info.BreachHistory[i];
                    int totalSeconds = entry.GameSecond;
                    int hours = totalSeconds / 3600;
                    int minutes = ( totalSeconds % 3600 ) / 60;
                    int seconds = totalSeconds % 60;
                    if ( hours > 0 )
                        Buffer.Add( "T+" ).Add( hours ).Add( "h" ).AddPaddedInt( minutes, 2 ).Add( "m  " );
                    else
                        Buffer.Add( "T+" ).Add( minutes ).Add( "m" ).AddPaddedInt( seconds, 2 ).Add( "s  " );
                    Buffer.Add( entry.PlanetName, "a1ffa1" ).Add( ":  " ).Add( entry.BreachDisplayName, "ffffff" );
                    if ( entry.Difficulty != MalwareBreachDifficulty.None )
                        Buffer.Add( "  [" ).Add( entry.Difficulty.ToString(), "ffaaaa" ).Add( "]" );
                    Buffer.Add( "\n" );
                }
                return true;
            }
        }
        #endregion

        #region tFactionResource3
        public class tFactionResource3 : ButtonAbstractBase
        {
            public static tFactionResource3 Instance;
            public tFactionResource3() { Instance = this; }

            public static bool LastWasVisible = true;

            private static string lastInitializedIcon = string.Empty;
            private static Material normalMaterial = null;
            private static Material hoverMaterial = null;

            #region GetShouldBeHidden
            public override bool GetShouldBeHidden()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                LastWasVisible = ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( localFaction );
                return !LastWasVisible;
            }
            #endregion

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Shows how much Ichor you have.\n\nClick here to see your breach history." );
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                // if ( !string.IsNullOrEmpty( World_AIW2.Instance.Resource3TextColorAndIcon ) )
                //     Buffer.Add( World_AIW2.Instance.Resource3TextColorAndIcon );
                Buffer.AddNumberMoreReadable( localFaction.StoredFactionResourceThree.IntValue );
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                string iconName = World_AIW2.Instance.Resource3Icon;
                if ( string.IsNullOrEmpty( iconName ) )
                    return;
                if ( iconName == lastInitializedIcon )
                    return;
                if ( Button.ReferenceImagesOptional == null || Button.ReferenceImagesOptional.Length == 0 )
                    return;

                TextEmbededSprite spriteRow = TextEmbededSpriteTable.Instance.GetRowByName( iconName, LookupSwapAllowed.No, false );
                if ( spriteRow == null || spriteRow.UnitySprite == null )
                    return;

                Button.ReferenceImagesOptional[0].sprite = spriteRow.UnitySprite;

                if ( Button.ReferenceImagesNormalMaterials != null && Button.ReferenceImagesNormalMaterials.Length > 0 &&
                     Button.ReferenceImagesHoverMaterials != null && Button.ReferenceImagesHoverMaterials.Length > 0 &&
                     Button.ReferenceImagesNormalMaterials[0] != null )
                {
                    string colorHex = World_AIW2.Instance.Resource3Color;
                    if ( !string.IsNullOrEmpty( colorHex ) )
                    {
                        Color baseColor = ColorMath.HexToColor( colorHex );
                        float normalFactor = Mathf.Pow( 2f, 1.6f );
                        float hoverFactor = Mathf.Pow( 2f, 2.6f );

                        normalMaterial = Material.Instantiate( Button.ReferenceImagesNormalMaterials[0] );
                        normalMaterial.SetColor( "_Color", new Color( baseColor.r * normalFactor, baseColor.g * normalFactor, baseColor.b * normalFactor, baseColor.a ) );

                        hoverMaterial = Material.Instantiate( Button.ReferenceImagesNormalMaterials[0] );
                        hoverMaterial.SetColor( "_Color", new Color( baseColor.r * hoverFactor, baseColor.g * hoverFactor, baseColor.b * hoverFactor, baseColor.a ) );

                        Button.ReferenceImagesOptional[0].material = normalMaterial;
                        Button.ReferenceImagesNormalMaterials[0] = normalMaterial;
                        Button.ReferenceImagesHoverMaterials[0] = hoverMaterial;
                    }
                }

                lastInitializedIcon = iconName;
            }
            //Not used yet
            // public override void HandleMouseover()
            // {
            //     Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Shows how much Ichor you have.\n\nClick here to see information about your faction." );
            // }

            // public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            // {
            //     Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Apkallu Overview", "Close",
            //         delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tNecromancerEsssence.GetApkalluOverview( Buffer ); } );
            //     return MouseHandlingResult.None;
            // }
        }
        #endregion

        #region tThreat
        public class tThreat : ButtonAbstractBase
        {
            public static tThreat Instance;
            public tThreat() { Instance = this; }

            private MaxIntValueOverTimeList valOverTimeHumans = new MaxIntValueOverTimeList( 5, 0.2f );
            private MaxIntValueOverTimeList valOverTimeOtherFactions = new MaxIntValueOverTimeList( 5, 0.2f );
            private readonly SortedDictionary<Planet, int> planetsWithThreat = SortedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "Window_ResourceBar-tThreat-planetsWithThreat" );
            private readonly SortedDictionary<Planet, int> planetsWithThreatIncoming = SortedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "Window_ResourceBar-tThreat-planetsWithThreatIncoming" );

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Faction playerFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if ( playerFaction == null )
                    return;
                FInt threatHumans = FInt.Zero;
                FInt threatOthers = FInt.Zero;
                int debugCode = 0;
                try{
                    debugCode = 100;
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        //if ( planet.GetControllingFactionType() == FactionType.Player )
                        //    return DelReturn.Continue;
                        debugCode = 200;
                        if ( planet == null )
                            continue;
                        debugCode = 300;
                        StrengthData_PlanetFaction_Stance data = planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Hostile];
                        debugCode = 400;
                        if ( data.RelativeToHumanTeam_ThreatStrength > 0 )
                            threatHumans += data.RelativeToHumanTeam_ThreatStrength;
                        debugCode = 500;
                        if ( data.RelativeToOtherFaction_ThreatStrength > 0 )
                            threatOthers += data.RelativeToOtherFaction_ThreatStrength;
                    }
                } catch (Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in tThreat GetTextToShowFromVolatile debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
                valOverTimeHumans.LogCurrentValue( threatHumans.IntValue, Engine_Universal.UnscaledDeltaTime );
                valOverTimeOtherFactions.LogCurrentValue( threatOthers.IntValue, Engine_Universal.UnscaledDeltaTime );
                int strengthToDraw = valOverTimeHumans.GetCurrentMax();
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, strengthToDraw, true, true );
            }

            public override void HandleMouseover() { Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Threat: AI forces actively waiting to strike.  Most AI forces will not bother you until you bother them. These units, however... <color=#ffa1a1>they're ready to come at any time they sense a weakness</color>.\n\nThreat is caused when an AI unit is angered or aggroed by your forces, and survives. If a threat unit lives long enough without doing anything, it will join the Hunter Fleet, a far smarter grouping of threat, with...<color=#ffa1a1>concerning intelligence</color>.\n\nClicking on the Threat icon will show a planet by planet breakdown of where the visible threat is." ); }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Visible Threat", "Close",
                    delegate( ArcenDoubleCharacterBuffer Buffer ) { return this.ThreatWindow_FillText( Buffer ); } );
                return MouseHandlingResult.None;
            }

            private static DictionaryOfDictionaries<Planet, string, int> threatDestination = 
                DictionaryOfDictionaries<Planet, string, int>.Create_WillNeverBeGCed( 300, 30, "Window_ResourceBar-tThreat-threatDestination" ); 
            
            private bool ThreatWindow_FillText( ArcenDoubleCharacterBuffer Buffer )
            {
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );

                bool show_only_visible = true;
                if ( debug )
                    show_only_visible = false;
                bool show_destinations = false;
                if ( debug )
                    show_destinations = true;

                if (show_destinations)
                    Buffer.Add("各星球可见威胁（+正在前往此处的威胁）：\n");
                else
                    Buffer.Add("各星球可见威胁：\n");
                
                FInt allThreatAgainstHumans = FInt.Zero;
                FInt allThreatAgainstOthers = FInt.Zero;
                FInt hunterThreatAgainstHumans = FInt.Zero;
                FInt hunterThreatAgainstOthers = FInt.Zero;
                FInt wardenStrength = FInt.Zero;
                planetsWithThreat.Clear();
                planetsWithThreatIncoming.Clear();
                FInt threatWithDestination = FInt.Zero;
                FInt threatWithoutDestination = FInt.Zero;

                Faction playerFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if ( playerFaction == null )
                    return false;

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    StrengthData_PlanetFaction_Stance hostleData = planet.GetPlanetFactionForFaction( playerFaction )
                        .DataByStance[FactionStance.Hostile];

                    int warden_here = 0;
                    int hunter_here_to_human = 0;
                    int hunter_here_to_other = 0;
                    int threat_here = 0;
                    int threat_here_to_human = 0;
                    int threat_here_to_other = 0;

                    if ( show_only_visible )
                    {
                        if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                            continue;

                        warden_here = hostleData.TotalWardenStrengthVisible;
                        hunter_here_to_human = hostleData.TotalHunterStrength_AgainstHumansVisible;
                        hunter_here_to_other = hostleData.TotalHunterStrength_AgainstOtherFactionsVisible;
                        threat_here_to_human = hostleData.RelativeToHumanTeam_ThreatStrengthVisible;
                        threat_here_to_other = hostleData.RelativeToOtherFaction_ThreatStrengthVisible;
                        threat_here = hostleData.ThreatStrengthVisible;
                    }
                    else
                    {
                        warden_here = hostleData.TotalWardenStrength;
                        hunter_here_to_human = hostleData.TotalHunterStrength_AgainstHumans;
                        hunter_here_to_other = hostleData.TotalHunterStrength_AgainstOtherFactions;
                        threat_here_to_human = hostleData.RelativeToHumanTeam_ThreatStrength;
                        threat_here_to_other = hostleData.RelativeToOtherFaction_ThreatStrength;
                        threat_here = hostleData.ThreatStrength;
                    }

                    if ( threat_here <= 0 )
                        continue;

                    if ( threat_here < 1000 )
                        planetsWithThreat[planet] =
                            -2; //-2 is a reserved value here to tell the code to put in "< 1" in the display
                    else
                        planetsWithThreat[planet] =
                            threat_here /
                            1000; //threat values are scaled down by 1000 to make the numbers more tractable

                    allThreatAgainstHumans += threat_here_to_human;
                    allThreatAgainstOthers += threat_here_to_other;
                    hunterThreatAgainstHumans += hunter_here_to_human;
                    hunterThreatAgainstOthers += hunter_here_to_other;
                    wardenStrength += warden_here;

                    if ( show_destinations )
                    {
                        foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.MobileCombatants ) )
                        {
                            if ( entity.GetFactionTypeSafe() != FactionType.AI ||
                                 entity.GuardedUnit.GetSquad() != null ||
                                 entity.Orders.Behavior == EntityBehaviorType.Guard_Guardian_Patrolling )
                            {
                                continue;
                            }

                            bool log = false;
                            /*
                            if ( GameEntity_Base.CurrentlyHoveredOver == entity )
                            {
                                log = true;
                            }
                            */

                            Planet dst_planet = null;
                            if ( entity.WaitingAgainstPlanetIndex != -1 )
                            {
                                dst_planet = World_AIW2.Instance.GetPlanetByIndex( entity.WaitingAgainstPlanetIndex );
                            }
                            else if ( entity.Orders != null )
                            {
                                dst_planet = entity.GetDestinationPlanet();
                            }

                            if ( log )
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "threat entity {0} on {1} WaitingAgainstPlanetIndex={2} GetFinalDestinationOrNull={3} so dst_planet={4}", 
                                    entity.ToString(), planet.Name, entity.WaitingAgainstPlanetIndex, entity.Orders?.GetFinalDestinationOrNull().Name??"null", dst_planet?.Name??"null" ), Verbosity.DoNotShow );
                            }


                            var str = entity.GetStrengthOfStack();

                            if ( dst_planet == null )
                            {
                                threatWithoutDestination += str;
                            }
                            else
                            {
                                threatWithDestination += str;

                                if ( threat_here < 1000 )
                                    planetsWithThreatIncoming[dst_planet] =
                                        -2;
                                else
                                    planetsWithThreatIncoming[dst_planet] =
                                        str /
                                        1000;
                            }

                        }
                    }
                }

                var list = planetsWithThreat.SortIntoList( delegate ( KeyValuePair<Planet, int> L, KeyValuePair<Planet, int> R )
                {
                    var l_inc = planetsWithThreatIncoming[L.Key];
                    var r_inc = planetsWithThreatIncoming[R.Key];
                    var l_val = L.Value + l_inc;
                    var r_val = R.Value + r_inc;

                    return r_val.CompareTo( l_val );
                } );

                foreach ( var pair in list )
                {
                    var planet = pair.Key;
                    var val = pair.Value;

                    if ( val == 0 )
                        continue;

                    Buffer.Add(planet.Name);
                    if ( pair.Value > 10000)
                        Buffer.Add(": <color=#ff0000>");
                    else if ( pair.Value > 1000)
                        Buffer.Add(": <color=#ffa1a1>");
                    else 
                        Buffer.Add(": <color=#ffce78>");

                    if ( pair.Value == -2 )
                        Buffer.Add( " ~1 " );
                    else
                        Buffer.Add(pair.Value);

                    var threat_here = planetsWithThreat[planet];
                    var threat_going_here = planetsWithThreatIncoming[planet];

                    //if ( threat_going_here == -2 )
                        //Buffer.Add( " (1)" );
                    //else 
                    if ( threat_going_here > 0 )
                        Buffer.Add( " (+" ).Add( threat_going_here ).Add( ")" );
                    
                    Buffer.Add("</color>\n");
                }

                Buffer.Add( "\nTotal Visible Threat Against Humans: <color=#ff0909>" + (allThreatAgainstHumans.IntValue / 1000).ToString( "#,##0" ) + "</color>." );
                Buffer.Add( "\nTotal Threat Against Humans In Entire Galaxy: <color=#ff0909>" + (valOverTimeHumans.GetCurrentMax() / 1000).ToString( "#,##0" ) + "</color>." );
                Buffer.Add( "\nTotal Visible Threat Against Other Factions: <color=#ff0909>" + ( allThreatAgainstOthers.IntValue / 1000).ToString( "#,##0" ) + "</color>." );
                Buffer.Add( "\nTotal Threat Against Other Factions In Entire Galaxy: <color=#ff0909>" + (valOverTimeOtherFactions.GetCurrentMax() / 1000).ToString( "#,##0" ) + "</color>." );
                Buffer.Add( "\n\nVisible Warden Strength: <color=#ff0909>" + (wardenStrength.IntValue / 1000).ToString( "#,##0" ) + "</color>." );
                Buffer.Add( "\nVisible Hunter Strength Against Humans: <color=#ff0909>" + (hunterThreatAgainstHumans.IntValue/1000).ToString( "#,##0" ) + "</color>. ");
                Buffer.Add( "\nVisible Hunter Strength Against Other Factions: <color=#ff0909>" + (hunterThreatAgainstOthers.IntValue / 1000).ToString( "#,##0" ) + "</color>.\n " );

                {
                    Buffer.Add( "\n\nEnemy Units Disbanded From Chasing Distant Other Factions: <color=#ff0909>" +
                        World_AIW2.Instance.KilledBecauseChasingAFactionWeAreTooFarFrom_Count.ToString( "#,##0" ) + "</color>." );
                    Buffer.Add( "\nEnemy Strength Disbanded From Chasing Distant Other Factions: <color=#ff0909>" +
                        (World_AIW2.Instance.KilledBecauseChasingAFactionWeAreTooFarFrom_Strength / 1000).ToString( "#,##0" ) + "</color>." );
                    foreach ( Faction fac in World_AIW2.Instance.Factions )
                    {
                        if ( fac.KilledBecauseChasingAFactionWeAreTooFarFrom_Count > 0 )
                        {
                            Buffer.Add( "\n   " ).StartColor( fac.FactionCenterColor.TeamColorBrighter ).Add( fac.GetDisplayName_Short( 999 ) ).EndColor().Add( " Disbanded <color=#ff0909>" +
                                fac.KilledBecauseChasingAFactionWeAreTooFarFrom_Count.ToString( "#,##0" ) + "</color> Units, <color=#ff0909>" +
                                (fac.KilledBecauseChasingAFactionWeAreTooFarFrom_Strength / 1000).ToString( "#,##0" ) + "</color> Strength. " );
                        }
                    }
                }

                Buffer.Add( "\n" );

                if ( debug )
                {
                    //for all AIs, dump data about their extragalctic war state
                    Buffer.Add("星系外战争状态：\n");
                    for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                    {
                        Faction faction = World_AIW2.Instance.AIFactions[i];
                        AISentinelsCoreData sentinelExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                        if ( sentinelExternal.WormholeBorerBudget > 0 )
                        {
                            Buffer.Add("虫洞钻机预算：").Add( sentinelExternal.WormholeBorerBudget, "a1ffa1" ).Add( "，建造费用 ");
                            GameEntityTypeData borerData = GameEntityTypeDataTable.Instance.GetRowByName( "MobileWormholeBorer" );
                            Buffer.Add( borerData.CostForAIToPurchase, "a1ffa1" ).Add("\n");
                        }
                        ProtectedList<ExtragalacticBudget> budgets = sentinelExternal.ExtragalacticBudgets;
                        for ( int j = 0; j < budgets.Count; j++ )
                        {
                            budgets[j].AppendStateForInterfaceDisplay( Buffer );
                            Buffer.Add(".\n");
                        }
                    }
                }

                if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Templar ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "Templar" )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        TemplarFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<TemplarFactionBaseInfo>();
                        info.GetTemplarStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }
                }
                if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Necromancer ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( otherFaction ) )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        NecromancerEmpireFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                        info.GetNecromancerStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }
                }
                
                if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AstroTrains ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "AstroTrains" )
                            continue;
                        Buffer.Add( otherFaction.ToString() + "\n" );
                        otherFaction.BaseInfo.AppendStateForDebugDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }
                }
                if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "Sappers" )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        SappersFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<SappersFactionBaseInfo>();
                        info.GetSappersStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }
                }
                if ( debug || ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.HumanMarauders ) ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "HumanMarauders" )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        MarauderFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<MarauderFactionBaseInfo>();
                        info.GetMarauderStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }
                }
                if ( debug || Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith ) )
                {
                     for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName == "DarkZenith")
                        {
                            Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                            DarkZenithFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfo>();
                            info.GetDarkZenithStateForDisplay( Buffer );
                            Buffer.Add( "\n" );
                        }
                        else if (otherFaction.SpecialFactionData.InternalName == "DarkZenithSvikari" )
                        {
                            Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                            DarkZenithSvikariFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<DarkZenithSvikariFactionBaseInfo>();
                            info.GetDarkZenithStateForDisplay( Buffer );
                            Buffer.Add( "\n" );
                        }
                    }
                }

                if ( debug || Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        ZenithArchitraveFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        info.GetZenithArchitraveStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }

                }
                if ( debug || Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.FallenSpire ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "FallenSpire" )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        FallenSpireFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
                        info.GetFallenSpireStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }
                }

                if ( debug || Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "Nanocaust" )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        NanocaustFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<NanocaustFactionBaseInfo>();
                        info.GetNanocaustStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }

                }
                
                if ( debug || Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithMiners ) )
                {
                     for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "ZenithMiners" )
                            continue;
                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        ZenithMinersFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<ZenithMinersFactionBaseInfo>();
                        info.GetZenithMinersStateForDisplay( Buffer );
                        Buffer.Add( "\n" );
                    }
                }
                
                if ( debug || Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge ) )
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        if (otherFaction.SpecialFactionData.InternalName != "Scourge" )
                            continue;

                        Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                        ScourgeFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<ScourgeFactionBaseInfo>();
                        info.GetScourgeStateForDisplay( Buffer );
                        Buffer.Add("\n");
                    }
                }

                // All ExternalFactionBaseInfo(s) now have a chance to write debug information by adding
                // AppendStateForDebugDisplay to the class. This allows mods to plug in here, for example.
                if ( debug )
                {
                    for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                    {
                        var faction = World_AIW2.Instance.AIFactions[i];

                        // outer loop is iterating only top level factions not subfactions
                        var parent = faction.GetParentFactionOrNull();
                        if (parent != null)
                            continue;

                        var info = faction.BaseInfo;
                        if ( info == null )
                            continue;

                        Buffer.AddFactionNameInItsColor( faction, true ).Add( ":\n" );
                        info.AppendStateForDebugDisplay(Buffer);
                        Buffer.Add("\n");

                        /*
                        bool firstsub = true;
                        for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                        {
                            var other = World_AIW2.Instance.Factions[j];
                            if (other.GetParentFactionOrNull() != faction)
                                continue;

                            if (firstsub)
                            {
                                Buffer.Add( "...subfactions\n\n" );
                                firstsub = false;
                            }

                            Buffer.AddFactionNameInItsColor( other ).Add( "\n" );
                            other.BaseInfo.AppendStateForDebugDisplay(Buffer);
                            Buffer.Add("\n");
                        }
                        */
                    }
                }
                
                return true;
            }
        }
        #endregion

        #region tAttackSafe
        public class tAttackSafe : ButtonAbstractBase
        {
            public static tAttackSafe Instance;
            public tAttackSafe() { Instance = this; }

            private MaxIntValueOverTimeList valOverTime = new MaxIntValueOverTimeList( 5, 0.2f );

            public static string[] MainTextColorsByIndex = new string[] { "<color=#9a9a9a>", "<color=#fff08a>", "<color=#fc5e44>" };
            public static string[] AlternatingTextColorsByIndex = new string[] { "<color=#9a9a9a>", "<color=#ffce78>", "<color=#ffd4d4>" };

            private bool useAlt = false;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                tAttackSafe.GetAttackButtonTextToShowFromVolatile( this, ref useAlt, ref valOverTime, Buffer );
            }

            public static void GetAttackButtonTextToShowFromVolatile( ButtonAbstractBase Button, ref bool useAlt, ref MaxIntValueOverTimeList valOverTime, ArcenDoubleCharacterBuffer Buffer )
            {
                Button.SetSkipGetTextFor( 0.3f );                

                //instead of using the nice new IsOn_EveryHalfSecondToggle, we must use our own useAlt
                //because this method only gets called a couple of times a second anyway.
                useAlt = !useAlt;

                valOverTime.LogCurrentValue( customParent.totalAttack, Engine_Universal.UnscaledDeltaTime );
                Buffer.Add( ( /*ArcenTime.IsOn_EveryHalfSecondToggle*/ useAlt ? MainTextColorsByIndex : AlternatingTextColorsByIndex )[customParent.currentDangerIndex] );
                int strengthToDraw = valOverTime.GetCurrentMax();
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, strengthToDraw, true, true );
                Buffer.Add( "</color>" );
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ResourceBar-tAttackSafe-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tAttackSafe.HandleAttackButtonMouseover( this.Element );
            }

            private static readonly Dictionary<string, int> planetsUnderAttack = Dictionary<string, int>.Create_WillNeverBeGCed( 500, "Window_ResourceBar-tAttackSafe-planetsUnderAttack" );
            public static void HandleAttackButtonMouseover( ArcenUI_Element Element )
            {
                //This basically redoes the logic from GetTextToShowFromVolatile(), but that function
                //is called very often so we want to minimize menmory/CPU from that. This function is infrequently
                //called.
                //TODO: color code the text based on whether you are outnumbered on that planet
                Galaxy galaxy =World_AIW2.Instance.CurrentGalaxy;
                if ( galaxy == null )
                    return;
                Faction playerFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if ( playerFaction == null )
                    return;

                int newAttack = 0;
                bool anyAttack = false;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.GetControllingFactionType() != FactionType.Player )
                    {
                        planetsUnderAttack[planet.Name] = 0;
                        continue;
                    }
                    newAttack = planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                    planetsUnderAttack[planet.Name] = newAttack;
                    if ( newAttack > 0 )
                        anyAttack = true;
                }
                tooltipBuffer.Add( "Total strength of attacking enemy ships on your team's planets\n" );
                if(anyAttack)
                {
                    List<KeyValuePair<string, int>> sortedPlanets = planetsUnderAttack.SortIntoList( delegate ( KeyValuePair <string, int> L, KeyValuePair <string, int> R)
                                         {
                                             return R.Value.CompareTo(L.Value);
                                         } );

                    for(int i = 0; i < sortedPlanets.Count; i++)
                    {
                        KeyValuePair<string,int> pair = sortedPlanets[i];
                        if ( pair.Value > 0 )
                        {
                            int strengthToDraw = pair.Value;
                            tooltipBuffer.Add( pair.Key );
                            tooltipBuffer.Add( " is under attack by " );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( tooltipBuffer, strengthToDraw, true, true );
                            tooltipBuffer.Add( " strength.\n" );
                        }
                    }
                }
                tooltipBuffer.Add("\n<size=60%>Left-click for performance stats.  Middle-click for allegiance debug info.  Right-click for NPC ship cap info.</size>");
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            public override bool GetShouldBeHidden()
            {
                return customParent.currentDangerIndex != 0; //0 = no attacking
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( input.MiddleButtonClicked )
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Faction Alliance Details", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetFactionAllianceDetails( Buffer ); } );
                else if ( input.RightButtonClicked )
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "NPC Ship Cap Details", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetNPCShipCapDetails( Buffer ); } );
                else
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Performance Stats", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetPerformanceStats( Buffer ); } );
                return MouseHandlingResult.None;
            }

            private static readonly List<Faction> factionsByGameCommands = List<Faction>.Create_WillNeverBeGCed( 300, "Window_InGameEscapeMenu-tAboutContent-factionsByGameCommands" );

            private static readonly List<KeyValuePair<string, int>> squadCreation_Reasons = List<KeyValuePair<string, int>>.Create_WillNeverBeGCed( 300, "Window_InGameEscapeMenu-tAboutContent-squadCreation_Reasons" );
            private static readonly List<GameEntityTypeData> squadCreation_Types = List<GameEntityTypeData>.Create_WillNeverBeGCed( 300, "Window_InGameEscapeMenu-tAboutContent-squadCreation_Types" );
            private static readonly List<Planet> squadCreation_Planets = List<Planet>.Create_WillNeverBeGCed( 300, "Window_InGameEscapeMenu-tAboutContent-squadCreation_Planets" );
            private static readonly List<Faction> squadCreation_Factions = List<Faction>.Create_WillNeverBeGCed( 300, "Window_InGameEscapeMenu-tAboutContent-squadCreation_Factions" );

            public static bool GetPerformanceStats( ArcenDoubleCharacterBuffer Buffer )
            {
                bool isTutorial = World_AIW2.Instance.TutorialOrNull != null;

                #region Write Sorted PlayerCommand Data
                if ( !isTutorial )
                {
                    factionsByGameCommands.Clear();
                    foreach ( Faction fac in World_AIW2.Instance.Factions )
                    {
                        if ( fac.NumberCommandsQueuedForExecutionSinceGameStarted > 0 )
                            factionsByGameCommands.Add( fac );
                    }
                    factionsByGameCommands.Sort( static delegate ( Faction left, Faction right )
                    {
                        int val = right.NumberCommandsQueuedForExecutionSinceGameStarted.CompareTo( left.NumberCommandsQueuedForExecutionSinceGameStarted ); //desc
                        if ( val != 0 )
                            return val;
                        return left.FactionIndex.CompareTo( right.FactionIndex );
                    } );
                    if ( factionsByGameCommands.Count > 0 )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>Commands Queued By Faction:</b>\n" ).EndColor();
                        foreach ( Faction fac in factionsByGameCommands )
                        {
                            Buffer.Add( "Id" ).Add( fac.FactionIndex ).Add( "  " );
                            InterfaceHelper.WriteFactionNameToBuffer( fac, Buffer );
                            Buffer.Add( " x" ).Add( fac.NumberCommandsQueuedForExecutionSinceGameStarted );
                            Buffer.Add( "\n" );
                        }
                        Buffer.Add( "\n" );
                    }

                    List<GameCommandType> sortedCommands = GameCommandTypeTable.SortAndReturnByNumberQueuedForExecution();

                    Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>Commands Queued By Type:</b>\n" ).EndColor();

                    if ( GameCommandType.NumberNullCommandsQueuedSinceGameStarted > 0 )
                    {
                        Buffer.StartColor( "ff7b4d" );
                        Buffer.Add( "NULL CMD QUEUED" ).Add( " x" ).Add( GameCommandType.NumberNullCommandTypesQueuedSinceGameStarted );
                        Buffer.EndColor();
                    }
                    if ( GameCommandType.NumberNullCommandTypesQueuedSinceGameStarted > 0 )
                    {
                        Buffer.StartColor( "ff7b4d" );
                        Buffer.Add( "NULL TYPE QUEUED" ).Add( " x" ).Add( GameCommandType.NumberNullCommandTypesQueuedSinceGameStarted );
                        Buffer.EndColor();
                    }
                    if ( GameCommandType.NumberNullCommandsFromClientNotSentSinceGameStarted > 0 )
                    {
                        Buffer.StartColor( "ff7b4d" );
                        Buffer.Add( "NULL CMD FROM CLIENT QUEUE" ).Add( " x" ).Add( GameCommandType.NumberNullCommandsFromClientNotSentSinceGameStarted );
                        Buffer.EndColor();
                    }
                    if ( GameCommandType.NumberCommandsFromClientNotDequeudProperlyinceGameStarted > 0 )
                    {
                        Buffer.StartColor( "ccc554" );
                        Buffer.Add( "TRY-FAIL FROM CLIENT QUEUE" ).Add( " x" ).Add( GameCommandType.NumberCommandsFromClientNotDequeudProperlyinceGameStarted );
                        Buffer.EndColor();
                    }
                    if ( GameCommandType.NumberNullCommandsFromServerNotSentSinceGameStarted > 0 )
                    {
                        Buffer.StartColor( "ff7b4d" );
                        Buffer.Add( "NULL CMD FROM HOST QUEUE" ).Add( " x" ).Add( GameCommandType.NumberNullCommandsFromServerNotSentSinceGameStarted );
                        Buffer.EndColor();
                    }
                    if ( GameCommandType.NumberCommandsFromServerNotDequeudProperlyinceGameStarted > 0 )
                    {
                        Buffer.StartColor( "ccc554" );
                        Buffer.Add( "TRY-FAIL FROM HOST QUEUE" ).Add( " x" ).Add( GameCommandType.NumberCommandsFromServerNotDequeudProperlyinceGameStarted );
                        Buffer.EndColor();
                    }

                    for ( int i = 0; i < sortedCommands.Count; i++ )
                    {
                        GameCommandType command = sortedCommands[i];
                        if ( command == null || (command.NumberQueuedForExecutionSinceGameStarted <= 0 && command.NumberRequestedFromPoolSinceGameStarted <= 0) )
                            continue;
                        bool showRed = command.NumberQueuedForExecutionSinceGameStarted < command.NumberRequestedFromPoolSinceGameStarted;
                        if ( showRed )
                            Buffer.StartColor( "ff7b4d" );
                        Buffer.Add( command.InternalName ).Add( " x" ).Add( command.NumberQueuedForExecutionSinceGameStarted )
                            .Add( "   (x" ).Add( command.NumberRequestedFromPoolSinceGameStarted ).Add( " Requested)" );
                        if ( showRed )
                            Buffer.EndColor();
                        Buffer.Add( "\n" );
                    }
                }
                #endregion

                Buffer.Add( "\n" );

                #region Write Sorted squadCreation_Reasons
                if ( !isTutorial )
                {
                    squadCreation_Reasons.Clear();
                    foreach ( KeyValuePair<string, int> kv in World_AIW2.Instance.SquadCreation_Reasons )
                    {
                        if ( kv.Value > 0 )
                            squadCreation_Reasons.Add( kv );
                    }
                    squadCreation_Reasons.Sort( static delegate ( KeyValuePair<string, int> left, KeyValuePair<string, int> right )
                    {
                        int val = right.Value.CompareTo( left.Value ); //desc
                        if ( val != 0 )
                            return val;
                        return left.Key.CompareTo( right.Key );
                    } );
                    if ( squadCreation_Reasons.Count > 0 )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>Ship Creation By Reason:</b>\n" ).EndColor();
                        foreach ( KeyValuePair<string, int> kv in squadCreation_Reasons )
                        {
                            Buffer.Add( kv.Key ).Add( " x" ).Add( kv.Value );
                            Buffer.Add( "\n" );
                        }
                        Buffer.Add( "\n" );
                    }
                }
                #endregion

                #region Write Sorted squadCreation_Types
                if ( !isTutorial )
                {
                    squadCreation_Types.Clear();
                    foreach ( GameEntityTypeData typeData in GameEntityTypeDataTable.Instance.Rows )
                    {
                        if ( typeData.HostOnly_NonSim_SquadsCreated > 0 )
                            squadCreation_Types.Add( typeData );
                    }
                    squadCreation_Types.Sort( static delegate ( GameEntityTypeData left, GameEntityTypeData right )
                    {
                        int val = right.HostOnly_NonSim_SquadsCreated.CompareTo( left.HostOnly_NonSim_SquadsCreated ); //desc
                        if ( val != 0 )
                            return val;
                        return left.DisplayName.CompareTo( right.DisplayName );
                    } );
                    if ( squadCreation_Types.Count > 0 )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>Ship Creation By Type:</b>\n" ).EndColor();
                        foreach ( GameEntityTypeData kv in squadCreation_Types )
                        {
                            Buffer.Add( kv.DisplayName ).Add( " x" ).Add( kv.HostOnly_NonSim_SquadsCreated );
                            Buffer.Add( "\n" );
                        }
                        Buffer.Add( "\n" );
                    }
                }
                #endregion

                #region Write Sorted squadCreation_Planets
                if ( !isTutorial )
                {
                    squadCreation_Planets.Clear();
                    foreach ( Planet planet in World_AIW2.Instance.Planets( true ) )
                    {
                        if ( planet.HostOnly_NonSim_SquadsCreated > 0 )
                            squadCreation_Planets.Add( planet );
                    }
                    squadCreation_Planets.Sort( static delegate ( Planet left, Planet right )
                    {
                        int val = right.HostOnly_NonSim_SquadsCreated.CompareTo( left.HostOnly_NonSim_SquadsCreated ); //desc
                        if ( val != 0 )
                            return val;
                        return left.Name.CompareTo( right.Name );
                    } );
                    if ( squadCreation_Planets.Count > 0 )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>Ship Creation By Planet:</b>\n" ).EndColor();
                        foreach ( Planet kv in squadCreation_Planets )
                        {
                            Buffer.Add( kv.Name ).Add( " x" ).Add( kv.HostOnly_NonSim_SquadsCreated );
                            Buffer.Add( "\n" );
                        }
                        Buffer.Add( "\n" );
                    }
                }
                #endregion

                #region Write Sorted squadCreation_Factions
                if ( !isTutorial )
                {
                    squadCreation_Factions.Clear();
                    foreach ( Faction kv in World_AIW2.Instance.Factions )
                    {
                        if ( kv.HostOnly_NonSim_SquadsCreated > 0 )
                            squadCreation_Factions.Add( kv );
                    }
                    squadCreation_Factions.Sort( static delegate ( Faction left, Faction right )
                    {
                        int val = right.HostOnly_NonSim_SquadsCreated.CompareTo( left.HostOnly_NonSim_SquadsCreated ); //desc
                        if ( val != 0 )
                            return val;
                        return left.GetDisplayName().CompareTo( right.GetDisplayName() );
                    } );
                    if ( squadCreation_Factions.Count > 0 )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>Ship Creation By Faction:</b>\n" ).EndColor();
                        foreach ( Faction kv in squadCreation_Factions )
                        {
                            Buffer.Add( "Id" ).Add( kv.FactionIndex ).Add( "  " );
                            InterfaceHelper.WriteFactionNameToBuffer( kv, Buffer );
                            Buffer.Add( " x" ).Add( kv.HostOnly_NonSim_SquadsCreated );
                            Buffer.Add( "\n" );
                        }
                        Buffer.Add( "\n" );
                    }
                }
                #endregion

                return true;
            }

            public static bool GetFactionAllianceDetails( ArcenDoubleCharacterBuffer Buffer )
            {
                foreach ( Faction fac in World_AIW2.Instance.Factions )
                {
                    Buffer.StartColor( ColorMath.Yellow ).Add( "\nFaction: " ).Add( fac.GetDisplayName() ).EndColor();
                    for ( int i = 0; i < fac.AlliedWith_Array.Length; i++ )
                    {
                        if ( fac.AlliedWith_Array[i] )
                            Buffer.Add( "\nAlliedWith_Array: " ).Add( World_AIW2.Instance.Factions[i].GetDisplayName() );
                    }
                    for ( int i = 0; i < fac.FactionIndicesIAmAlliedWith.Count; i++ )
                    {
                        Buffer.Add( "\nAllyIndex: " ).Add( World_AIW2.Instance.Factions[fac.FactionIndicesIAmAlliedWith[i]].GetDisplayName() );
                    }
                    for ( int i = 0; i < fac.HostileTo_Array.Length; i++ )
                    {
                        if ( fac.HostileTo_Array[i] )
                            Buffer.Add( "\nHostileTo_Array: " ).Add( World_AIW2.Instance.Factions[i].GetDisplayName() );
                    }
                    for ( int i = 0; i < fac.FactionIndicesIAmHostileTo.Count; i++ )
                    {
                        Buffer.Add( "\nHostileIndex: " ).Add( World_AIW2.Instance.Factions[fac.FactionIndicesIAmHostileTo[i]].GetDisplayName() );
                    }
                }

                return true;
            }

            public static bool GetNPCShipCapDetails( ArcenDoubleCharacterBuffer Buffer )
            {
                foreach ( Faction fac in World_AIW2.Instance.Factions )
                {
                    switch ( fac.Type )
                    {
                        case FactionType.Player:
                        case FactionType.NaturalObject:
                            continue; //skip these two
                    }
                    Buffer.StartColor( fac.FactionCenterColor.ColorHexBrighter ).Add( "\nFaction: " ).Add( fac.GetDisplayName() ).EndColor();
                    foreach ( NPCShipCapType row in NPCShipCapTypeTable.Instance.Rows )
                    {
                        Buffer.Add( "\n" );
                        int currentCount = fac.NPCShipCountsByCapType == null ? 0 : fac.NPCShipCountsByCapType[row.RowIndexNonSim];
                        int cap = fac.SpecialFactionData.NPCShipCapsByType[row.RowIndexNonSim];
                        if ( currentCount <= 0 )
                            Buffer.StartColor( "777777" );
                        else if ( currentCount >= cap )
                            Buffer.StartColor( "ff814a" );
                        else if ( currentCount <= cap / 2 )
                            Buffer.StartColor( "f2ffff" );
                        else
                            Buffer.StartColor( "fffb80" );

                        Buffer.Add( row.InternalName ).Add( ": " ).Add( currentCount ).Add( "/" ).Add( cap );
                        Buffer.EndColor();
                    }
                    Buffer.Add( "\n" );
                }

                return true;
            }
        }
        #endregion

        #region tAttackNonHome
        public class tAttackNonHome : ButtonAbstractBase
        {
            public static tAttackNonHome Instance;
            public tAttackNonHome() { Instance = this; }

            private MaxIntValueOverTimeList valOverTime = new MaxIntValueOverTimeList( 5, 0.2f );

            private bool useAlt = false;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                tAttackSafe.GetAttackButtonTextToShowFromVolatile( this, ref useAlt, ref valOverTime, Buffer );
            }

            public override void HandleMouseover()
            {
                tAttackSafe.HandleAttackButtonMouseover( this.Element );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( input.MiddleButtonClicked )
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Faction Alliance Details", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetFactionAllianceDetails( Buffer ); } );
                else if ( input.RightButtonClicked )
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "NPC Ship Cap Details", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetNPCShipCapDetails( Buffer ); } );
                else
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Performance Stats", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetPerformanceStats( Buffer ); } );
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return customParent.currentDangerIndex != 1; //1 = attacking non-home
            }
        }
        #endregion

        #region tAttackHomePlanet
        public class tAttackHomePlanet : ButtonAbstractBase
        {
            public static tAttackHomePlanet Instance;
            public tAttackHomePlanet() { Instance = this; }

            private MaxIntValueOverTimeList valOverTime = new MaxIntValueOverTimeList( 5, 0.2f );

            private bool useAlt = false;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                tAttackSafe.GetAttackButtonTextToShowFromVolatile( this, ref useAlt, ref valOverTime, Buffer );
            }

            public override void HandleMouseover()
            {
                tAttackSafe.HandleAttackButtonMouseover( this.Element );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( input.MiddleButtonClicked )
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Faction Alliance Details", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetFactionAllianceDetails( Buffer ); } );
                else if ( input.RightButtonClicked )
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "NPC Ship Cap Details", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetNPCShipCapDetails( Buffer ); } );
                else
                    Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "Performance Stats", "Close",
                        delegate ( ArcenDoubleCharacterBuffer Buffer ) { return tAttackSafe.GetPerformanceStats( Buffer ); } );
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return customParent.currentDangerIndex != 2; //2 = attacking home
            }
        }
        #endregion

        #region tScienceDivider
        public class tScienceDivider : TextAbstractBase
        {
            public static tScienceDivider Instance;
            public tScienceDivider() { Instance = this; }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer ) { }
        }
        #endregion

        #region tHackingDivider
        public class tHackingDivider : TextAbstractBase
        {
            public static tHackingDivider Instance;
            public tHackingDivider() { Instance = this; }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer ) { }
        }
        #endregion

        #region tGeneralTextMessage
        public class tGeneralTextMessage : TextAbstractBase
        {
            public static tGeneralTextMessage Instance;
            public tGeneralTextMessage() { Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( World.Instance.IsPaused )
                {
                    if ( !buffer.GetIsEmpty() )
                        buffer.Add( "   " );
                    buffer.Add( "<size=120%><color=#ffb21c>Paused</color></size>" );
                }

                if ( World.Instance.IsPaused && World.Instance.ConclusionType == CampaignConclusionType.Won )
                {
                    if ( !buffer.GetIsEmpty() )
                        buffer.Add( "   " );
                    buffer.Add( "<color=#1cff57><b>Victory!</b></color>" );
                }
                else if ( World.Instance.IsPaused && World.Instance.ConclusionType == CampaignConclusionType.Lost )
                {
                    if ( !buffer.GetIsEmpty() )
                        buffer.Add( "   " );
                    buffer.Add( "<color=#ff1cf7><b>You Have Lost...</b></color>" );
                }
            }

            public override void HandleMouseover()
            {
                if ( World.Instance.IsPaused && World.Instance.ConclusionType == CampaignConclusionType.Won )
                {
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "You have won the game!  Congratulations.  You can still continue playing, if you have unfinished business." );
                }
                else if ( World.Instance.IsPaused && World.Instance.ConclusionType == CampaignConclusionType.Lost )
                {
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "Everyone is dead, humanity has failed -- but that's okay!  Losing is fun, and way more instructive than winning.  You can start another game, or if you'd like to keep playing on this one from your current position, you can.  You can't turn this defeat into a victory at this point, but you can try to avenge yourself in other ways." );
                }
            }
        }
        #endregion

        #region Dark Zenith Sidekick
        public static bool GetDarkZenithSidekickIncome( ArcenDoubleCharacterBuffer Buffer, MouseHandlingInput input )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null || !DarkZenithSidekickFactionBaseInfo.GetIsThisADZFaction( localFaction ) )
                return false;
            return GetDarkZenithSidekickIncome( localFaction, Buffer, input );
        }
        public static bool GetDarkZenithSidekickIncome( Faction forFaction, ArcenDoubleCharacterBuffer Buffer, MouseHandlingInput input )
        {
            if ( forFaction == null )
                return false;
            DarkZenithSidekickFactionBaseInfo factionBaseInfo = forFaction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            if ( factionBaseInfo == null )
                return false;
            if (input.RightButtonClicked)
            {
                factionBaseInfo.GetDarkZenithSidekickUpgradesForDisplay( Buffer );
            }
            else
            {
                factionBaseInfo.GetDarkZenithSidekickStateForDisplay( Buffer );
            }
            return true;
        }
        #endregion
        #region Scourge Infused Empire
        public static bool GetScourgeState( ArcenDoubleCharacterBuffer Buffer )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.SpecialFactionData.InternalName != "ScourgeVassal" )
                    continue;
                Buffer.Add( otherFaction.ToString(), otherFaction.FactionCenterColor.ColorHexBrighter ).Add( "\n" );
                ScourgeVassalFactionBaseInfo info = otherFaction.GetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                info.GetScourgeStateForDisplay( Buffer );
                Buffer.Add("\n");
                return true;
            }
            return false;
        }
        #endregion

        #region Dyson Sidekick
        public static bool GetDysonSidekickIncome( ArcenDoubleCharacterBuffer Buffer )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null || !DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( localFaction ) )
                return false;
            return GetDysonSidekickIncome( localFaction, Buffer );
        }
        public static bool GetDysonSidekickIncome( Faction forFaction, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( forFaction == null )
                return false;
            DysonSidekickFactionBaseInfo factionBaseInfo = forFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if ( factionBaseInfo == null )
                return false;
            bool sphereUnlocked = false;
            if ( sphereUnlocked ) { }
            bool spireTwo = false;
            bool zenithTwo = false;
            bool templarTwo = false;
            bool neinzulTwo = false;
            if ( factionBaseInfo.SpireDistrictTier > 1 )
                spireTwo = true;
            if ( factionBaseInfo.ZenithDistrictTier > 1 )
                zenithTwo = true;

            if ( factionBaseInfo.NeinzulDistrictTier > 1 )
                neinzulTwo = true;
            if ( factionBaseInfo.TemplarDistrictTier > 1 )
                templarTwo = true;

            if ( !spireTwo || !zenithTwo || !neinzulTwo || !templarTwo )
            {
                Buffer.Add("<b>You will be able to build a Dyson Sphere once you have unlocked District Two for all factions. You need to upgrade\n");
                if ( !spireTwo )
                    Buffer.Add("\tSpire", "ffb37b").Add("\n");
                if ( !neinzulTwo )
                    Buffer.Add("\tNeinzul", "7bb3ff").Add("\n");
                if ( !zenithTwo )
                    Buffer.Add("\tZenith", "b3ff7b").Add("\n");
                if ( !templarTwo )
                    Buffer.Add("\tTemplar", "cccccc").Add("\n");
                Buffer.Add("</b>");
            }

            Buffer.Add( "Here is how you are getting your income (all numbers per-second): \n" );
            //Metal:
            FInt income = factionBaseInfo.MetalIncomeLastSecond;
            Buffer.Add("\nMetal Income: " ).Add( income, "ccccee" ).Add("\n\tBase Metal Income: ").Add( factionBaseInfo.Income.BaseMetalIncomePerSecond, "a1ffa1" );
            if ( factionBaseInfo.MetalGenerators.Count > 0 )
            {
                //FInt additionalIncome = factionBaseInfo.Income.MetalIncomePerGeneratorPerSecond * factionBaseInfo.MetalGenerators.Count; //no longer accurate, since we now allow per-mark-level increases
                Buffer.Add("\n\tFrom ").Add( factionBaseInfo.MetalGenerators.Count, "ffa1a1" ).Add(" metal generators, each generating ").Add( factionBaseInfo.Income.MetalIncomePerGeneratorPerSecond, "a1a1ff" ).Add(" plus ").Add( factionBaseInfo.Income.MetalIncomePerGeneratorPerSecondIncreasePerMarkLevel, "a1ffa1" ).Add(" per mark level.");
            }
            else
            {
                Buffer.Add("\n\tIf you need additional metal, you can build ").Add("Metal Generators", "ccccee" ).Add(" at a ").Add("Neinzul Stronghold", "7bb3ff").Add(".");
            }
            if ( forFaction.TotalMetalMetabolized > 0 )
            {
                Buffer.Add( "\nTotal Metal Metabolized by all ships: <color=#ccccee>" ).AddNumberMoreReadable( forFaction.TotalMetalMetabolized ).EndColor();
            }

            //Science
            income = factionBaseInfo.ScienceIncomeLastSecond;
            Buffer.Add("\nScience Income: " ).Add( income, "7CE9FF" ).Add("\n\tBase Science Income: ").Add( factionBaseInfo.Income.BaseScienceIncomePerSecond, "a1ffa1" );
            if ( factionBaseInfo.ScienceGenerators.Count > 0 )
            {
                //FInt additionalIncome = factionBaseInfo.Income.ScienceIncomePerGeneratorPerSecond * factionBaseInfo.ScienceGenerators.Count;
                Buffer.Add("\n\tFrom ").Add( factionBaseInfo.ScienceGenerators.Count, "ffa1a1" ).Add(" science generators, each generating ").Add( factionBaseInfo.Income.ScienceIncomePerGeneratorPerSecond, "a1a1ff" ).Add(" plus ").Add( factionBaseInfo.Income.ScienceIncomePerGeneratorPerSecondIncreasePerMarkLevel, "a1ffa1" ).Add(" per mark level.");
            }
            else
                Buffer.Add("\n\tIf you need additional science, you can build ").Add("Science Generators", "7CE9FF").Add(" at a ").Add("Spire Stronghold", "ffb37b").Add(".");
            //Hacking
            income = factionBaseInfo.HackingIncomeLastSecond;
            Buffer.Add("\nHacking Income: " ).Add( income.ToString(), ArcenExternalUIUtilities.HackingTextColor ).Add("\n\tBase Hacking Income: ").Add( factionBaseInfo.Income.BaseHackingIncomePerSecond, "a1ffa1" );
            if ( factionBaseInfo.HackingGenerators.Count > 0 )
            {
                //FInt additionalIncome = factionBaseInfo.Income.HackingIncomePerGeneratorPerSecond * factionBaseInfo.HackingGenerators.Count;
                Buffer.Add("\n\tFrom ").Add( factionBaseInfo.HackingGenerators.Count, "ffa1a1" ).Add(" hacking generators, each generating ").Add( factionBaseInfo.Income.HackingIncomePerGeneratorPerSecond, "a1a1ff" ).Add(" plus ").Add( factionBaseInfo.Income.HackingIncomePerGeneratorPerSecondIncreasePerMarkLevel, "a1ffa1" ).Add(" per mark level.");
            }
            /*
            //ResourceOneT
            income = factionBaseInfo.ResourceOneIncomeLastSecond;
            Buffer.Add("\nCuendillar Income: " ).Add( income.ToString(), World_AIW2.Instance.Resource1Color ).Add("\n\tBase Cuendillar Income: ").Add( factionBaseInfo.Income.BaseResourceOneIncomePerSecond, "a1ffa1" );
            if ( factionBaseInfo.ResourceOneGenerators.Count > 0 )
            {
                //FInt additionalIncome = factionBaseInfo.Income.ResourceOneIncomePerGeneratorPerSecond * factionBaseInfo.ResourceOneGenerators.Count;
                Buffer.Add("\n\tFrom ").Add( factionBaseInfo.ResourceOneGenerators.Count, "ffa1a1" ).Add(" resourceOne generators, each generating ").Add( factionBaseInfo.Income.ResourceOneIncomePerGeneratorPerSecond, "a1a1ff" ).Add(" plus ").Add( factionBaseInfo.Income.ResourceOneIncomePerGeneratorPerSecondIncreasePerMarkLevel, "a1ffa1" ).Add(" per mark level.");
            }
            */
            Buffer.Add("\n\n").Add("Spire: You have access to ").Add( "district " + factionBaseInfo.SpireDistrictTier, "a3ba22" ).Add(" ships\n");
            Buffer.Add("天顶：你可以使用 ").Add( "区域 " + factionBaseInfo.ZenithDistrictTier, "a3ba22" ).Add(" 级飞船\n");
            Buffer.Add("尼恩祖：你可以使用 ").Add( "区域 " + factionBaseInfo.NeinzulDistrictTier, "a3ba22" ).Add(" 级飞船\n");
            Buffer.Add("圣殿骑士：你可以使用 " ).Add( "区域 " + factionBaseInfo.TemplarDistrictTier, "a3ba22" ).Add(" 级飞船\n\n");
            if (spireTwo && zenithTwo && neinzulTwo && templarTwo)
            {
                Buffer.Add("<b>You have unlocked the Dyson Sphere</b>\n");
            }
            if ( factionBaseInfo.PlanetsDrilled > 0 )
                Buffer.Add("你已摧毁了 ").Add( factionBaseInfo.PlanetsDrilled, "22baa3" ).Add(" 个星球。\n");
            else
                Buffer.Add("你已摧毁了 ").Add( factionBaseInfo.PlanetsDrilled, "22baa3" ).Add(" 个星球。要摧毁星球，你必须建造天顶要塞\n");

            Buffer.Add("\n");
            Buffer.Add("你的旗舰等级为 ").Add( factionBaseInfo.FlagshipTierLevel, "22baa3" ).Add("。\n");

            Faction reaperFaction = FactionUtilityMethods.Instance.GetReapersFaction();
            ReapersFactionBaseInfo bInfo = reaperFaction.GetExternalBaseInfoAs<ReapersFactionBaseInfo>();
            int time = bInfo.TimeForNextChrysalis - World_AIW2.Instance.GameSecond;
            Buffer.Add("\nDebug: next chrysalis will spawn in : ").Add( time, "a1ffa1" );
            time = bInfo.TimeForNextPlanetoid - World_AIW2.Instance.GameSecond;
            Buffer.Add("\nDebug: next planetoid will spawn in : ").Add( time, "ffffa1" );
            time = bInfo.TimeForNextAIPlanetoidDrill - World_AIW2.Instance.GameSecond;
            Buffer.Add("\nDebug: next planetoid drill will spawn in : ").Add( time, "a1ffff" );
            time = bInfo.TimeForNextLunarInvasion - World_AIW2.Instance.GameSecond;
            Buffer.Add("\nDebug: next lunar invasion will spawn in : ").Add( time, "a1ffff" );

            //Buffer.Add("\tFor tier 2, you need to drill " ).Add( factionBaseInfo.Difficulty.PlanetsDrilledForFlagshipTierTwo, "bb5696" ).Add(" planets, and for tier 3 you must drill ").Add( factionBaseInfo.Difficulty.PlanetsDrilledForFlagshipTierThree, "9656bb" ).Add(" planets.\n");
            //Buffer.Add("\nDebug: Your enemies got " ).Add( factionBaseInfo.EnemyIncomeLastSecond, "ff0000" ).Add(" income to oppose you last second.\nTheir next attack will be in ").Add( (factionBaseInfo.TimeForNextEnemyAttack - World_AIW2.Instance.GameSecond), "ff4477" ).Add( ", and so far they have accumulated " ).Add( factionBaseInfo.StrengthForNextEnemyAttack, "ff1122" ).Add(" strength.");
            return true;
        }
        #endregion
        #region Armada Empire
        public static bool GetArmadaOverview( ArcenDoubleCharacterBuffer Buffer )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null || !ArmadaFactionBaseInfo.GetIsThisAnArmadaFaction( localFaction ) )
                return false;
            return GetArmadaOverview( localFaction, Buffer );
        }
        public static bool GetArmadaOverview( Faction forFaction, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( forFaction == null )
                return false;
            ArmadaFactionBaseInfo factionBaseInfo = forFaction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            Buffer.Add("<b>Active Mines:</b>\n");
            foreach ( GameEntity_Squad mine in factionBaseInfo.Mines.DisplaySquads() )
            {
                ArmadaPerUnitBaseInfo data = mine.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                int finishTime = data.MineFinishTime - World_AIW2.Instance.GameSecond;
                string color = "a1a1a1";
                if ( mine.TypeData.GetHasTag("ArmadaMetalMine"))
                    color = ArcenExternalUIUtilities.MetalTextColor;
                if ( mine.TypeData.GetHasTag("ArmadaHackingMine"))
                    color = ArcenExternalUIUtilities.HackingTextColor;
                if ( mine.TypeData.GetHasTag("ArmadaScienceMine"))
                    color = ArcenExternalUIUtilities.ScienceTextColor;
                Buffer.Add("\t").Add(mine.TypeData.GetDisplayName(), color).Add(" on ").Add( mine.Planet.Name).Add(". Done in ").Add( finishTime, "a1ffa1" ).Add(" seconds\n");
            }
            Buffer.Add("\n<b>Planets on Mining Cooldown:</b>\n");
            foreach ( KeyValuePair<Planet, int> pair in factionBaseInfo.MiningIneligiblePlanets )
            {
                int time = pair.Value - World_AIW2.Instance.GameSecond;
                if ( time >= factionBaseInfo.Income.IneligibleMiningInterval )
                    continue;
                Buffer.Add("\t").Add(pair.Key.Name, "a1a1ff").Add(" eligible in ").Add( time, "ffa1a1" ).Add(" seconds.\n");
            }
            Buffer.Add("\n<b>Planet Mine Depth:</b>\n");
            foreach ( KeyValuePair<Planet, int> pair in factionBaseInfo.PlanetMineCount )
            {
                int mineCount = pair.Value;
                int rate = factionBaseInfo.Income.MiningDepthIncreaseRate;
                int currentTier = Math.Min( mineCount / rate + 1, 4 );
                int minesUntilNextTier = ( currentTier * rate ) - mineCount;
                Buffer.Add( "\t" ).Add( pair.Key.Name, "a1a1ff" ).Add( " — " );
                if ( currentTier < 4 )
                {
                    string mineWord = minesUntilNextTier == 1 ? "mine" : "mines";
                    Buffer.Add( "you can mine this planet " ).Add( minesUntilNextTier, "ffa1a1" ).Add( " more " ).Add( mineWord );
                    if ( currentTier == 3 )
                        Buffer.Add( " until you need a Tier IV mine. The shafts have gone deeper than thought possible; only the most extreme drills can reach what remains.\n" );
                    else if ( currentTier == 2 )
                        Buffer.Add( " until you need a Tier III mine. The shafts are growing perilously deep; each lode harder to reach than the last.\n" );
                    else
                        Buffer.Add( " until you need a larger mine. Each mine must go down further into the planet to find new lodes.\n" );
                }
                else
                {
                    Buffer.Add( "Tier IV mines only — the deepest lodes have been breached on this planet.\n" );
                }
            }

            return true;
        }
        #endregion
        #region Apkallu
        public static bool GetApkalluOverview( ArcenDoubleCharacterBuffer Buffer )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null || !ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( localFaction ) )
                return false;
            return GetApkalluOverview( localFaction, Buffer );
        }
        public static bool GetApkalluOverview( Faction forFaction, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( forFaction == null )
                return false;
            ApkalluFactionBaseInfo factionBaseInfo = forFaction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( factionBaseInfo == null )
                return false;
            for ( int i = 0; i < factionBaseInfo.CompletedBreaches.Count; i++ )
            {
                if ( i == 0 )
                    Buffer.Add("已完成的突破：\n");
                Buffer.Add("\t" + factionBaseInfo.CompletedBreaches[i]).Add("\n");
            }
            Buffer.Add("杜鲁解锁：\n");
            for ( int i = 0; i < factionBaseInfo.UnlockedDuruStructures.Count; i++ )
            {
                Buffer.Add("\t" + factionBaseInfo.UnlockedDuruStructures[i]).Add("\n");
            }
            Buffer.Add("金字塔解锁：\n");
            for ( int i = 0; i < factionBaseInfo.UnlockedZigguratStructures.Count; i++ )
            {
                Buffer.Add("\t" + factionBaseInfo.UnlockedZigguratStructures[i]).Add("\n");
            }
            Buffer.Add("可用援军：\n");
            for ( int i = 0; i < factionBaseInfo.ActiveOutguardGroups.Count; i++ )
            {
                Buffer.Add("\t" + factionBaseInfo.ActiveOutguardGroups[i]).Add("\n");
            }
            Buffer.Add( "Pilgrims:\n" );
            foreach ( GameEntity_Squad pilgrim in factionBaseInfo.Pilgrims.DisplaySquads() )
            {
                bool isLesser = pilgrim.TypeData.GetHasTag( "ApkalluLesserPilgrim" );
                string typeLabel = isLesser ? "Lesser" : "Regular";
                ApkalluPerUnitBaseInfo data = pilgrim.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                Buffer.Add( "\t[" ).Add( typeLabel ).Add( "] on " ).Add( pilgrim.Planet.Name, "a1ff1a" );
                if ( data != null )
                {
                    if ( isLesser )
                        Buffer.Add( " — " ).Add( data.MetalAccumulated ).Add( " metal" );
                    else
                        Buffer.Add( " — " ).Add( data.ResourcePoints ).Add( " RP" );
                    if ( data.PlanetsVisited.Count > 0 )
                    {
                        Buffer.Add( ", visited: " );
                        for ( int v = 0; v < data.PlanetsVisited.Count; v++ )
                        {
                            if ( v > 0 ) Buffer.Add( ", " );
                            Buffer.Add( data.PlanetsVisited[v].Name );
                        }
                    }
                }
                Buffer.Add( "\n" );
            }

            Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malware != null )
            {
                MalwareFactionBaseInfo mBaseInfo = malware.TryGetExternalBaseInfoAs<MalwareFactionBaseInfo>();
                if ( mBaseInfo != null )
                {
                    Buffer.Add("\n\nMalware state:\n");
                    Buffer.Add("Current protocol: " + mBaseInfo.CurrentProtocol).Add("\n");
                    Buffer.Add("Adversary tier: " + mBaseInfo.CurrentAdversaryTierTag).Add("\n");
                    Buffer.Add("Next protocol choice: " + (mBaseInfo.TimeForNextProtocolChoice - World_AIW2.Instance.GameSecond)).Add("\n");
                    Buffer.Add("Protocol skipped (no adversary): " + mBaseInfo.DebugProtocolPicksSkippedNoAdversary).Add("\n");
                    Buffer.Add("Planet qualification failures: " + mBaseInfo.DebugPlanetQualificationFailures).Add("\n");
                    if ( !mBaseInfo.HasLinkedPlanets )
                    {
                        Buffer.Add("Planets not linked\n");
                    }
                    if ( mBaseInfo.TimeToLinkPlanets > World_AIW2.Instance.GameSecond )
                    {
                        int secondsTillLink = mBaseInfo.TimeToLinkPlanets - World_AIW2.Instance.GameSecond;

                        Buffer.Add( "Link time: " + secondsTillLink ).Add( "\n" );
                    }
                    foreach ( GameEntity_Squad splice in mBaseInfo.Splices.DisplaySquads() )
                    {
                        MalwarePerUnitBaseInfo sData = splice.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                        int lastHad = sData?.TimeLastHadAdversary ?? -1;
                        string lastHadStr = lastHad < 0 ? "never" : ( World_AIW2.Instance.GameSecond - lastHad ) + "s ago";
                        Buffer.Add( "Splice on " ).Add( splice.Planet.Name, "a1ff1a" ).Add( ": last adversary " ).Add( lastHadStr ).Add( "\n" );
                    }

                    foreach ( GameEntity_Squad nexus in mBaseInfo.Nexuses.DisplaySquads() )
                    {
                        MalwarePerUnitBaseInfo mData = nexus.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                        if ( mData == null )
                            continue;
                        if ( mData.Protocol == null )
                            continue;
                        Buffer.Add("Nexus on ").Add(nexus.Planet.Name, "a1ff1a").Add(" has protocol ").Add(mData.Protocol.ToDisplayString()).Add("\n");
                    }
                    Buffer.Add("Next fissure: " + (mBaseInfo.NextFissureTime - World_AIW2.Instance.GameSecond)).Add("\n");
                    foreach ( GameEntity_Squad fissure in mBaseInfo.Fissures.DisplaySquads() )
                    {
                        MalwarePerUnitBaseInfo mData = fissure.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                        if ( mData == null )
                            continue;
                        if ( mData.Protocol == null )
                            continue;
                        Buffer.Add("Fissure on ").Add(fissure.Planet.Name, "a1ff1a").Add("\n");
                    }

                    Buffer.Add( "\nCorrupted Ziggurats:\n" );
                    foreach ( GameEntity_Squad ziggurat in mBaseInfo.CorruptedZiggurats.DisplaySquads() )
                    {
                        Buffer.Add( "  " ).Add( ziggurat.TypeData.DisplayName, "ff6633" );
                        Buffer.Add( " on " ).Add( ziggurat.Planet?.Name ?? "unknown", "a1ffa1" );
                        Planet destination = ziggurat.Orders?.GetFinalDestinationOrNull();
                        if ( destination != null && destination != ziggurat.Planet )
                            Buffer.Add( " → heading to " ).Add( destination.Name, "ffaa44" );
                        Buffer.Add( "\n" );
                    }

                    Buffer.Add("\nConvergence:\n");
                    if ( mBaseInfo.ConvergenceCountdownEndTime != -1 )
                    {
                        int countdownRemaining = mBaseInfo.ConvergenceCountdownEndTime - World_AIW2.Instance.GameSecond;
                        Buffer.Add("\tCountdown: ").Add( countdownRemaining, "ff4444" ).Add("s remaining\n");
                        int generatorCount = mBaseInfo.ConvergenceGenerators.GetDisplayList().Count;
                        Buffer.Add("\tGenerators remaining: ").Add( generatorCount, "ffcc88" ).Add("\n");
                    }
                    else
                    {
                        int nextIn = mBaseInfo.NextConvergenceTime == -1
                            ? -1
                            : mBaseInfo.NextConvergenceTime - World_AIW2.Instance.GameSecond;
                        if ( nextIn <= 0 )
                            Buffer.Add("\tEligible to trigger\n");
                        else
                            Buffer.Add("\tNext eligible in: ").Add( nextIn ).Add("s\n");
                    }
                    if ( mBaseInfo.ConvergenceMainStrikeTime != -1 )
                    {
                        int strikeIn = mBaseInfo.ConvergenceMainStrikeTime - World_AIW2.Instance.GameSecond;
                        Buffer.Add("\tMain strike in: ").Add( strikeIn, "ff4444" ).Add("s (")
                              .Add( mBaseInfo.ConvergenceMainStrikeCarrierCount ).Add(" carriers)\n");
                    }
                    Buffer.Add("\tOccurrences so far: ").Add( mBaseInfo.ConvergenceOccurrenceCount ).Add("\n");

                }

            }
            return true;
        }
        #endregion

    }
}
