using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_ElderlingInformation : BaseGalaxyMapDisplayMode
    {
        private GameEntity_Squad GetSelectedElderlingOrNull()
        {
            GameEntity_Squad elderling = null;
            try
            {
                foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                {
                    if ( workingFaction.Type != FactionType.SpecialFaction )
                        continue;

                    foreach ( GameEntity_Squad workingElderling in workingFaction.Squads( "Elderling" ) )
                    {
                        ElderlingsPerUnitBaseInfo info = workingElderling.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                        if ( workingElderling.GetIsSelected() && (info?.TrackedByPlayer ?? false) )
                        {
                            elderling = workingElderling;
                            break; // Only care about the first one.
                        }
                    }
                }
            }
            catch ( Exception )
            {
                return null; // Can happen.
            }
            return elderling;
        }

        public override bool GetShouldBeShownInCurrentCampaign()
        {
            // Need at least one Elderling faction to exist.
            return World_AIW2.Instance.GetFirstFactionWithBaseInfoSourceType( "ElderlingsFactionBaseInfo" ) != null;
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );

            // Add any Elderlings.
            foreach ( GameEntity_Squad elderling in planet.Squads( "Elderling" ) )
            {
                ElderlingsPerUnitBaseInfo data = elderling.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                if ( !elderling.GetShouldBeVisibleBasedOnPlanetIntel() && !(data?.TrackedByPlayer ?? false) )
                    continue; // Don't let the player see things they shouldn't.

                if ( newOtherGimbalIndex < ArrayToFill.Length )
                {
                    ArrayToFill[newOtherGimbalIndex] = elderling;
                    newOtherGimbalIndex++;
                }
            }

            // If we have room left; add some omelettes.
            foreach ( GameEntity_Squad elderlingEgg in planet.Squads( "ElderlingEgg" ) )
            {
                ElderlingsPerUnitBaseInfo data = elderlingEgg.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                if ( !elderlingEgg.GetShouldBeVisibleBasedOnPlanetIntel() && !(data?.TrackedByPlayer ?? false) )
                    continue; // Don't let the player see things they shouldn't.

                if ( newOtherGimbalIndex < ArrayToFill.Length )
                {
                    ArrayToFill[newOtherGimbalIndex] = elderlingEgg;
                    newOtherGimbalIndex++;
                }
            }
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            int debugStage = 0;
            try
            {
                // Show some useful Elderling specific information.
                int lowestSanity = -2, soonesLay = -2, soonestHatch = -2, lowestExpRequired = -2;

                debugStage = 100;
                foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                {
                    debugStage = 110;
                    if ( workingFaction.Type != FactionType.SpecialFaction )
                        continue;

                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( workingFaction );

                    debugStage = 120;
                    foreach ( GameEntity_Squad elderling in pFaction.Entities.Squads( "Elderling" ) )
                    {
                        debugStage = 121;
                        ElderlingsPerUnitBaseInfo info = elderling.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();

                        debugStage = 122;
                        if ( info == null || !info.TrackedByPlayer )
                            continue; // Data not loaded, or hacking tracker not yet applied.

                        debugStage = 123;
                        if ( lowestSanity < 0 || info.SanityRemaining < lowestSanity )
                            lowestSanity = info.SanityRemaining;

                        debugStage = 124;
                        if ( soonesLay < 0 || info.NextEggLayingTime < soonesLay )
                            soonesLay = info.NextEggLayingTime;

                        if ( !info.FullyUpgraded && lowestExpRequired < 0 || info.ExperienceRequired < lowestExpRequired )
                            lowestExpRequired = info.ExperienceRequired;
                    }

                    debugStage = 130;
                    foreach ( GameEntity_Squad elderlingEgg in pFaction.Entities.Squads( "ElderlingEgg" ) )
                    {
                        debugStage = 131;
                        ElderlingsPerUnitBaseInfo info = elderlingEgg.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();

                        debugStage = 132;
                        if ( info == null )
                            continue; // Data not loaded, for one reason or another.

                        if ( !elderlingEgg.GetShouldBeVisibleBasedOnPlanetIntel() && !info.TrackedByPlayer )
                            continue; // Don't let the player see things they shouldn't.

                        debugStage = 133;
                        if ( soonestHatch < 0 || info.HatchTime < soonesLay )
                        {
                            debugStage = 134;
                            soonestHatch = info.HatchTime;
                        }
                    }
                }

                debugStage = 200;
                // Left Buffer
                if ( lowestSanity > 0 )
                    LeftBuffer.StartColor( Color.magenta ).Add( "Sanity " ).EndColor().Add( lowestSanity );

                LeftBuffer.Add( "\n" );

                debugStage = 300;
                if ( lowestExpRequired > 0 )
                    LeftBuffer.StartColor( Color.blue ).Add( "Exp " ).EndColor().Add( lowestExpRequired );

                debugStage = 400;
                // Right Buffer
                if ( soonestHatch > 0 )
                {
                    RightBuffer.StartColor( Color.yellow ).Add( "Hatch " ).EndColor();

                    debugStage = 410;
                    int timeLeft = soonestHatch - World_AIW2.Instance.GameSecond;

                    debugStage = 420;
                    RightBuffer.Add( (timeLeft / 60) ).Add( ":" );

                    debugStage = 430;
                    RightBuffer.AddPaddedInt( (timeLeft % 60), 2 );
                }

                RightBuffer.Add( "\n" );

                debugStage = 500;
                if ( soonesLay > 0 )
                {
                    if ( soonesLay > 0 )
                    {
                        RightBuffer.StartColor( Color.cyan ).Add( "Lay " ).EndColor();

                        debugStage = 510;
                        int timeLeft = soonesLay - World_AIW2.Instance.GameSecond;

                        debugStage = 520;
                        RightBuffer.Add( (timeLeft / 60) ).Add( ":" );

                        debugStage = 530;
                        RightBuffer.AddPaddedInt( (timeLeft % 60), 2 );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "GalaxyMapDisplayMode_ElderlingInformation.WriteLeftRightTextPerDisplayModeType Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }

        // All of the below can change based on what the player has selected.
        // Due to the nature of UI; account for the player selecting something midway through calculating.
        #region Map Connections
        public override Color GetColorForLinkBetweenPlanets( Planet FirstPlanet, Planet SecondPlanet )
        {
            if ( FirstPlanet.IntelLevel <= PlanetIntelLevel.Unexplored || SecondPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return base.GetColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

            try
            {
                ElderlingsPerUnitBaseInfo info = GetSelectedElderlingOrNull()?.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();

                if ( info == null )
                    return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

                if ( info.Territory.Contains( FirstPlanet ) || info.Territory.Contains( SecondPlanet ) )
                {
                    // Its our territory. Have our color.
                    return GetSelectedElderlingOrNull().PlanetFaction.Faction.FactionCenterColor.TeamColor;
                }
                else
                {
                    // Not our territory. Don't care.
                    return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );
                }
            }
            catch ( Exception )
            {
                return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );
            }
        }

        public override Color GetStartColorForLinkBetweenPlanets( Planet FirstPlanet, Planet SecondPlanet )
        {
            if ( FirstPlanet.IntelLevel <= PlanetIntelLevel.Unexplored || SecondPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

            try
            {
                ElderlingsPerUnitBaseInfo info = GetSelectedElderlingOrNull()?.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();

                if ( info == null )
                    return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

                if ( info.Territory.Contains( FirstPlanet ) && info.Territory.Contains( SecondPlanet ) )
                {
                    // Our territory. Have our color.
                    return GetSelectedElderlingOrNull().PlanetFaction.Faction.FactionCenterColor.TeamColor;
                }
                else if ( info.Territory.Contains( FirstPlanet ) )
                {
                    // Leaving our territory. Have our color.
                    return GetSelectedElderlingOrNull().PlanetFaction.Faction.FactionCenterColor.TeamColor;
                }
                else if ( info.Territory.Contains( SecondPlanet ) )
                {
                    // Entering our territory. Have their color.
                    return FirstPlanet.GetControllingFaction().FactionCenterColor.TeamColor;
                }
                else
                {
                    // Not our territory. Don't care.
                    return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );
                }
            }
            catch ( Exception )
            {
                return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );
            }
        }

        public override Color GetEndColorForLinkBetweenPlanets( Planet FirstPlanet, Planet SecondPlanet )
        {
            if ( FirstPlanet.IntelLevel <= PlanetIntelLevel.Unexplored || SecondPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return base.GetEndColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

            try
            {
                ElderlingsPerUnitBaseInfo info = GetSelectedElderlingOrNull()?.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();

                if ( info == null )
                    return base.GetEndColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

                if ( info.Territory.Contains( FirstPlanet ) && info.Territory.Contains( SecondPlanet ) )
                {
                    // Our territory. Have our color.
                    return GetSelectedElderlingOrNull().PlanetFaction.Faction.FactionCenterColor.TeamColor;
                }
                else if ( info.Territory.Contains( FirstPlanet ) )
                {
                    // Leaving our territory. Have their color.
                    return FirstPlanet.GetControllingFaction().FactionCenterColor.TeamColor;
                }
                else if ( info.Territory.Contains( SecondPlanet ) )
                {
                    // Entering our territory. Have our color.
                    return GetSelectedElderlingOrNull().PlanetFaction.Faction.FactionCenterColor.TeamColor;
                }
                else
                {
                    // Not our territory. Don't care.
                    return base.GetStartColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );
                }
            }
            catch ( Exception )
            {
                return base.GetEndColorForLinkBetweenPlanets( FirstPlanet, SecondPlanet );
            }
        }

        public override bool GetShouldPlanetLinkBeDottedStyle( Planet FirstPlanet, Planet SecondPlanet )
        {
            if ( FirstPlanet == null || SecondPlanet == null )
                return false;

            if ( FirstPlanet.IntelLevel <= PlanetIntelLevel.Unexplored || SecondPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return base.GetShouldPlanetLinkBeDottedStyle( FirstPlanet, SecondPlanet );

            try
            {
                ElderlingsPerUnitBaseInfo info = GetSelectedElderlingOrNull()?.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();

                if ( info == null )
                    return base.GetShouldPlanetLinkBeDottedStyle( FirstPlanet, SecondPlanet );

                if ( info.Territory.Contains( FirstPlanet ) && info.Territory.Contains( SecondPlanet ) )
                {
                    // Its our territory. Be bold.
                    return false;
                }
                else
                {
                    // Not our territory. Be skinny.
                    return true;
                }
            }
            catch ( Exception )
            {
                return base.GetShouldPlanetLinkBeDottedStyle( FirstPlanet, SecondPlanet );
            }
        }

        public override float GetThicknessMultiplierForLinkBetweenPlanets( Planet FirstPlanet, Planet SecondPlanet )
        {
            if ( FirstPlanet.IntelLevel <= PlanetIntelLevel.Unexplored || SecondPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return base.GetThicknessMultiplierForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

            try
            {
                ElderlingsPerUnitBaseInfo info = GetSelectedElderlingOrNull()?.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();

                if ( info == null )
                    return base.GetThicknessMultiplierForLinkBetweenPlanets( FirstPlanet, SecondPlanet );

                if ( info.Territory.Contains( FirstPlanet ) && info.Territory.Contains( SecondPlanet ) )
                {
                    // Its our territory. Be big.
                    return 2f;
                }
                else if ( info.Territory.Contains( FirstPlanet ) || info.Territory.Contains( SecondPlanet ) )
                {
                    // Border planet. Be slightly bigger.
                    return 1.5f;
                }
                else
                {
                    // Not our territory. Be small.
                    return 1f;
                }
            }
            catch ( Exception )
            {
                return base.GetThicknessMultiplierForLinkBetweenPlanets( FirstPlanet, SecondPlanet );
            }
        }
        #endregion
    }
}