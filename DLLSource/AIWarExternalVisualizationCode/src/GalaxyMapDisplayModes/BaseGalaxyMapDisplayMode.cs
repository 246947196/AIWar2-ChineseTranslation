using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public static class UtilityMethodsFor_GalaxyMapDisplayMode
    {
        public static void BasePickGalaxyMapOtherThingsToShow( ref int currentIndex, Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, GameEntity_Squad.EvaluatorDelegate evaluator)
        {
            int workingArrayIndex = currentIndex;
            bool canSeeEnemies = (planet.IntelLevel > PlanetIntelLevel.Unexplored);
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.DrawsInGalaxyView ) )
            {
                if ( EntityToSkip != null )
                {
                    if ( entity == EntityToSkip )
                        continue;
                }
                if ( !evaluator( entity ) )
                    continue;
                if ( !canSeeEnemies )
                {
                    if ( entity.PlanetFaction == null )
                        continue;
                    if ( entity.GetIsHostileToLocalFaction_Safe() )
                        continue;
                }

                ArrayToFill[workingArrayIndex] = entity;
                workingArrayIndex++;
                if ( workingArrayIndex >= ArrayToFill.Length )
                    break;
            }
            currentIndex = workingArrayIndex;
        }

        public static void AddPlayerFleetsToGalaxyMapOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, ref int currentArrayIndex )
        {
            int workingArrayIndex = currentArrayIndex;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.DrawsInGalaxyView ) )
            {
                if ( EntityToSkip != null )
                {
                    if ( entity == EntityToSkip )
                        continue;
                }
                if ( entity.PlanetFaction == null )
                    continue;
                if ( entity.GetFactionTypeSafe() != FactionType.Player )
                    continue; //if it's not a player faction, ignore it
                if ( !entity.TypeData.IsFleetLeader )
                    continue;

                ArrayToFill[workingArrayIndex] = entity;
                workingArrayIndex++;
                if ( workingArrayIndex >= ArrayToFill.Length )
                    break;
            }
            currentArrayIndex = workingArrayIndex;
        }

        public static void ResetForCollectTopmostSquads( RefPair<SafeSquadWrapper, int>[] ArrayOfTopItems )
        {
            for ( int i = 0; i < ArrayOfTopItems.Length; i++ )
            {
                RefPair<SafeSquadWrapper, int> currentItem = ArrayOfTopItems[i];
                if ( currentItem == null )
                    ArrayOfTopItems[i] = RefPair<SafeSquadWrapper, int>.Create( SafeSquadWrapper.Create( null ), 0 );
                else
                {
                    currentItem.LeftItem = SafeSquadWrapper.Create( null );
                    currentItem.RightItem = 0;
                }
            }
        }

        public static void CollectTopmostSquads( RefPair<SafeSquadWrapper,int>[] ArrayOfTopItems, GameEntity_Squad SquadToConsider, int ValueOfSquad, 
            bool HigherIsBetter, bool SkipAfterFindingSameType, int MaxCount )
        {
            int insertAt = -1;
            for ( int i = 0; i < ArrayOfTopItems.Length && i < MaxCount; i++ )
            {
                RefPair<SafeSquadWrapper, int> currentItem = ArrayOfTopItems[i];
                if ( currentItem.LeftItem.GetSquad() == null ) //we found a hole!
                {
                    currentItem.LeftItem = SafeSquadWrapper.Create( SquadToConsider );
                    currentItem.RightItem = ValueOfSquad;
                    return; //once we have added, then stop adding!
                }
                else //already something there
                {
                    if ( SkipAfterFindingSameType && currentItem.LeftItem.TypeData == SquadToConsider.TypeData )
                        return; //we already added this type, so skip

                    if ( HigherIsBetter )
                    {
                        if ( ValueOfSquad > currentItem.RightItem )
                        {
                            insertAt = i;
                            break;
                        }
                    }
                    else
                    {
                        if ( ValueOfSquad < currentItem.RightItem )
                        {
                            insertAt = i;
                            break;
                        }
                    }
                }
            }

            if ( insertAt >= 0 )
            {
                for ( int i = Mathf.Min( ArrayOfTopItems.Length - 1, MaxCount ); i > insertAt; i-- )
                {
                    RefPair<SafeSquadWrapper, int> lowerItem = ArrayOfTopItems[i];
                    RefPair<SafeSquadWrapper, int> higherItem = ArrayOfTopItems[i-1];
                    if ( higherItem.LeftItem.GetSquad() != null )
                    {
                        lowerItem.LeftItem = higherItem.LeftItem;
                        lowerItem.RightItem = higherItem.RightItem;
                    }
                }
                {
                    RefPair<SafeSquadWrapper, int> insertItem = ArrayOfTopItems[insertAt];
                    insertItem.LeftItem = SafeSquadWrapper.Create( SquadToConsider );
                    insertItem.RightItem = ValueOfSquad;
                }
            }
        }
    }

    public abstract class BaseGalaxyMapDisplayMode : IGalaxyMapDisplayModeImplementation
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "BaseGalaxyMapDisplayModes" );
        public BaseGalaxyMapDisplayMode()
        {
            RefTracker.IncrementObjectCount();
        }

        public virtual bool GetShouldReplaceNormalPlanetTooltip()
        {
            return false;
        }
        public virtual void WriteToPlanetTooltip( Planet planet, ArcenDoubleCharacterBuffer Buffer )
        {

        }

        //The GetStart and GetEnd color modes are used for when we want a gradient
        //the "GetColor" one is used for when we want a solid color
        public virtual Color GetStartColorForLinkBetweenPlanets(Planet FirstPlanet, Planet SecondPlanet)
          {
              if (FirstPlanet == null || SecondPlanet == null)
                  return Color.gray;
              
              ProjectedLocalPlayerMultiPathData pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly;

              //if we are in additive selection mode, keep track of currently queued orders as well as newly generated ones
              if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                  pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection;

              if ( pathData != null && pathData.GetAreConnected( FirstPlanet, SecondPlanet ) )
              {
                  Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                  if (localFaction == null)
                    return Color.gray;
                  
                  return localFaction.FactionCenterColor?.TeamColorHighlighted??Color.gray;
              }

              Faction FirstFaction = FirstPlanet.GetControllingOrInfluencingFaction();
              if ( FirstFaction == null )
                  return Color.gray;
              
              return FirstFaction.FactionCenterColor?.TeamColor?? Color.gray;
          }
        public virtual Color GetEndColorForLinkBetweenPlanets(Planet FirstPlanet, Planet SecondPlanet)
          {
              if (FirstPlanet == null || SecondPlanet == null)
                  return Color.gray;
              
              ProjectedLocalPlayerMultiPathData pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly;

              //if we are in additive selection mode, keep track of currently queued orders as well as newly generated ones
              if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                  pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection;

              if ( pathData != null && pathData.GetAreConnected( FirstPlanet, SecondPlanet ) )
              {
                  Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                  if (localFaction == null)
                    return Color.gray;
                  
                  return localFaction.FactionCenterColor.TeamColorHighlighted;
              }

              Faction SecondFaction = SecondPlanet.GetControllingOrInfluencingFaction();
              if ( SecondFaction == null )
                  return Color.gray;
              
              return SecondFaction.FactionCenterColor.TeamColor;
          }
        public virtual Color GetColorForLinkBetweenPlanets(Planet FirstPlanet, Planet SecondPlanet)
        {
            if ( FirstPlanet == null || SecondPlanet == null )
                return Color.gray;
            
            ProjectedLocalPlayerMultiPathData pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly;

            //if we are in additive selection mode, keep track of currently queued orders as well as newly generated ones
            if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection;

            if ( pathData != null && pathData.GetAreConnected( FirstPlanet, SecondPlanet ) )
            {
                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if ( localFaction == null )
                    return Color.gray;
                
                return localFaction.FactionCenterColor.TeamColorHighlighted;
            }

            Faction FirstFaction = FirstPlanet.GetControllingFaction();
            if ( FirstFaction == null )
                return Color.gray;
            
            if ( FirstFaction.Type == FactionType.NaturalObject )
            {
                Faction newFac = FirstPlanet.GetFactionWithSpecialInfluenceHere();
                if ( newFac != null )
                    FirstFaction = newFac;
            }

            Faction SecondFaction = SecondPlanet.GetControllingFaction();
            if ( SecondFaction == null )
                return Color.gray;
            if ( SecondFaction.Type == FactionType.NaturalObject )
            {
                Faction newFac = SecondPlanet.GetFactionWithSpecialInfluenceHere();
                if ( newFac != null )
                    SecondFaction = newFac;
            }

            if ( FirstFaction == SecondFaction )
                return FirstFaction.FactionCenterColor.TeamColor;
            
            if ( FirstFaction.GetIsFriendlyTowards( SecondFaction ) )
                return Color.blue;
            
            if ( FirstFaction.GetIsHostileTowards( SecondFaction ) )
                return Color.red;
            
            return Color.gray;
        }

        public virtual void WriteNameText( Planet planet, ArcenDoubleCharacterBuffer buffer )
        {
            if ( planet == null )
            {
                ArcenDebugging.ArcenDebugLog( "Warning: WriteNameText found planet == null", Verbosity.Chat );
                return;
            }
            if ( World_AIW2.Instance == null )
                return;

            #region Do This Instead If During Setup
            if ( World_AIW2.Instance.InSetupPhase )
            {
                buffer.Add( "<size=50>" );
                List<ConfigurationForFaction> factionConfigs = World_AIW2.Instance.Setup.FactionConfigurations;
                for ( int i = 0; i < factionConfigs.Count; i++ )
                {
                    ConfigurationForFaction factionConfig = factionConfigs[i];
                    if ( factionConfig.StartingIndex == planet.Index && factionConfig.SpecialFactionData.Type == FactionType.Player )
                    {
                        buffer.Add( "<color=#" ).Add( factionConfig.FactionCenterColor.ColorHexLinearBrighter ).Add( ">" );
                        buffer.Add( planet.Name );
                        //if ( factionConfig.ControlledByPlayerAccounts_OnlyUseInLobby.Count > 0 )
                        //{
                        //    byte playerAccountPKID = factionConfig.ControlledByPlayerAccounts_OnlyUseInLobby[0];
                        //    PlayerAccount account = World.Instance.GetPlayerAccountByPrimaryID( playerAccountPKID );
                        //    if ( account != null )
                        //        buffer.Add( "\n" ).Add( account.Username );
                        //}
                        buffer.Add( "</color>" );
                        return;
                    }
                }
                buffer.Add( "<color=#aaaaaa>" );
                buffer.Add( planet.Name );
                buffer.Add( "</color>" );
                return;
            }
            #endregion

            Faction faction = planet.GetFactionWithSpecialInfluenceHere();
            if ( faction == null )
            {
                //ArcenDebugging.ArcenDebugLog( "Warning: WriteNameText found faction == null", Verbosity.Chat );
                buffer.Add( planet.Name );
                return;
            }
            if ( faction.FactionCenterColor == null )
            {
                //ArcenDebugging.ArcenDebugLog( "Warning: WriteNameText found faction.TeamColor == null", Verbosity.Chat );
                buffer.Add( planet.Name );
                return;
            }
            if ( faction.Type == FactionType.NaturalObject )
                faction = planet.GetControllingFaction();
            if ( faction == null )
                faction = planet.GetFactionWithSpecialInfluenceHere();

            bool haveIntel = planet.IntelLevel > PlanetIntelLevel.Unexplored;
            if ( haveIntel || faction.Type == FactionType.Player )
                buffer.Add( "<color=#" ).Add( faction.FactionCenterColor.ColorHexLinearBrighter ).Add( ">" );
            else
                buffer.Add( "<color=#aaaaaa>" );
            buffer.Add( planet.Name );
            buffer.Add( "</color>" );

            /*
            if ( haveIntel &&
                 faction.Type == FactionType.AI &&
                 !Engine_AIW2.Instance.IsTestChamber )
                buffer.Add( "" ).Add("<sub>").Add( planet.MarkLevelForAIOnly.MapDisplayWithColor ).Add("</sub>");
            */
        }

        public virtual float GetThicknessMultiplierForLinkBetweenPlanets( Planet FirstPlanet, Planet SecondPlanet )
        {
            ProjectedLocalPlayerMultiPathData pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly;
            if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection;

            //If the player has ships selected and we are showing the path, make these extra thick
            if ( pathData != null && pathData.GetAreConnected( FirstPlanet, SecondPlanet ) )
                return 4.0f;

            float extraMultiplier = 1f;
            if ( GalaxyMapPlanetLink.CurrentlyHoveredOver != null &&
                (GalaxyMapPlanetLink.CurrentlyHoveredOver?.PlanetOne?.RelatedPlanet == FirstPlanet
                || GalaxyMapPlanetLink.CurrentlyHoveredOver?.PlanetTwo?.RelatedPlanet == FirstPlanet) &&
                (GalaxyMapPlanetLink.CurrentlyHoveredOver?.PlanetOne?.RelatedPlanet == SecondPlanet
                || GalaxyMapPlanetLink.CurrentlyHoveredOver?.PlanetTwo?.RelatedPlanet == SecondPlanet) )
                extraMultiplier = 2f;

            Planet aiPlanetOrNull;
            bool hasGate;
            GalaxyMapLinkUtils.DetectAiAndWarpGateAdjacencyToPlayer( FirstPlanet, SecondPlanet, out aiPlanetOrNull, out hasGate );
            if ( aiPlanetOrNull != null && hasGate )
            {
                return 1.5f * extraMultiplier;
            }

            return extraMultiplier;
        }

        public virtual bool GetShouldPlanetLinkBeDottedStyle( Planet FirstPlanet, Planet SecondPlanet)
        {
            if ( FirstPlanet == null || SecondPlanet == null )
                return false;
            if ( GameSettings.Current.GetBoolBySetting( "GalaxyMapPlanetOmitDottedStyle" ) )
                return false;
            try
            {
                ProjectedLocalPlayerMultiPathData pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly;
                if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                    pathData = LocalPlayerWorldBaseInfo.Instance.NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection;

                if ( pathData != null && pathData.GetAreConnected( FirstPlanet, SecondPlanet ) )
                    return false; //it's confusing when the lines are dashed and part of a path

                //For non-AI planets adjacent to AI planets in the absence) of a warp gate, we use a dotted linePlanet aiPlanetOrNull;
                Planet aiPlanetOrNull;
                bool hasGate;
                GalaxyMapLinkUtils.DetectAiAndWarpGateAdjacencyToPlayer( FirstPlanet, SecondPlanet, out aiPlanetOrNull, out hasGate );
                return aiPlanetOrNull != null && !hasGate;
            }
            catch { return false; }
        }

        public MapLinkWarningLevel GetWarningLevelForPlanetLink( Planet firstPlanet, Planet secondPlanet, out bool forwardDirection )
        {
            bool foundWave = false;
            bool forward = true;
            MapLinkWarningLevel warningLevel = MapLinkWarningLevel.None;
            foreach ( PlannedWave wave in WaveUtils.KnownWavesAgainstHumanWorlds )
            {
                if ( wave == null )
                    continue;
                Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                Planet sourcePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                if ( targetPlanet == null )
                    continue;
                if ( targetPlanet == secondPlanet && sourcePlanet == firstPlanet )
                {
                    foundWave = true;
                    forward = true;
                    break;
                }
                else if ( targetPlanet == firstPlanet && sourcePlanet == secondPlanet)
                {
                    foundWave = true;
                    forward = false;
                    break;
                }
            }

            if ( foundWave )
            {
                warningLevel = MapLinkWarningLevel.IncomingWave;
            }
            else
            {
                Planet aiPlanetOrNull;
                bool hasGate;
                GalaxyMapLinkUtils.DetectAiAndWarpGateAdjacencyToPlayer( firstPlanet, secondPlanet, out aiPlanetOrNull, out hasGate );

                if ( aiPlanetOrNull != null && hasGate )
                {
                    forward = aiPlanetOrNull == firstPlanet;
                    warningLevel = MapLinkWarningLevel.WarpGateActive;
                }
            }

            forwardDirection = forward;
            return warningLevel;
        }

        public int GetWaveLaunchTime( Planet firstPlanet, Planet secondPlanet )
        {
            int launchTime = int.MaxValue;
            foreach ( PlannedWave wave in WaveUtils.KnownWavesAgainstHumanWorlds )
            {
                if ( wave == null )
                    continue;
                Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                Planet sourcePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                if ( targetPlanet == null )
                    continue;
                if ( targetPlanet == secondPlanet && sourcePlanet == firstPlanet
                     || targetPlanet == firstPlanet && sourcePlanet == secondPlanet )
                {
                    if ( wave.gameTimeInSecondsForLaunchWave < launchTime )
                    {
                        launchTime = wave.gameTimeInSecondsForLaunchWave;
                    }
                }
            }

            return launchTime == int.MaxValue ? 0 : launchTime;
        }

        #region WriteImportanceLeftRightText
        /// <summary>
        /// The idea of this one is to write the number of low and high priority objectives on the galaxy map at the top of the buffer.
        /// </summary>
        public static void WriteImportanceLeftRightText( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if (planet == null)
                throw new ArgumentNullException("planet");
            if (LeftBuffer == null)
                throw new ArgumentNullException("LeftBuffer");
            if (RightBuffer == null)
                throw new ArgumentNullException("RightBuffer");
            
            int drawsOnLeft = 0;
            int drawsOnRight = 0;

            int highCount = 0;
            int lowCount = 0;
            ObjectiveImportance mostRelevantImportance = planet.ImportanceUIOnly;
            switch ( planet.ImportanceUIOnly )
            {
                case ObjectiveImportance.Lower:
                    lowCount++;
                    break;
                case ObjectiveImportance.Higher:
                    highCount++;
                    break;
            }
            //don't worry about visibility and so forth, because it's based on user-input importance and so they must have seen it to set it.
            foreach ( GameEntity_Squad e in planet.Squads( EntityRollupType.DrawsInGalaxyView ) )
            {
                switch ( e.ImportanceUIOnly )
                {
                    case ObjectiveImportance.Lower:
                        lowCount++;
                        break;
                    case ObjectiveImportance.Higher:
                        highCount++;
                        break;
                }
            }

            if ( highCount > 0 )
            {
                RightBuffer.StartColor( "ffcc1d" ); //orange
                RightBuffer.Add( "H" ).Add( highCount );
                RightBuffer.EndColor().Add( "\n" );
                
                drawsOnRight++;
            }
            else 
            if ( lowCount > 0 )
            {
                RightBuffer.StartColor( "43fff8" ); //cyan
                RightBuffer.Add( "L" ).Add( lowCount );
                RightBuffer.EndColor().Add( "\n" );
                
                drawsOnRight++;
            }

            if ( planet.PlanetImportanceUIOnly != PlanetImportance.None )
            {
                LeftBuffer.StartColor( planet.PlanetImportanceUIOnly.GetColor() );
                LeftBuffer.Add( planet.PlanetImportanceUIOnly.ToString() );
                LeftBuffer.EndColor().Add( "\n" );
                
                drawsOnLeft++;
            }

            //even out the rows
            while ( drawsOnLeft < drawsOnRight )
            {
                LeftBuffer.Add( "\n" );
                drawsOnLeft++;
            }
            
            while ( drawsOnRight < drawsOnLeft )
            {
                RightBuffer.Add( "\n" );
                drawsOnRight++;
            }
        }
        #endregion

        public void WriteLeftRightText( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( World_AIW2.Instance == null )
                return;
            if ( World_AIW2.Instance.InSetupPhase )
                return;

            //write this first in every display mode
            GalaxyMapDisplayMode_Normal.WriteImportanceLeftRightText( planet, LeftBuffer, RightBuffer );

            this.WriteLeftRightTextPerDisplayModeType( planet, LeftBuffer, RightBuffer );
        }

        public virtual bool GetShouldBeShownInCurrentCampaign()
        {
            return true;
        }

        public virtual void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            
        }

        public abstract void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom );

        public void WriteEntityPlanetViewText( GameEntity_Base entity, ArcenCharacterBufferBase Buffer )
        {
            int debugLine = 0;
            try
            {
                debugLine = 1;
                bool debug = false;
                if ( entity == null )
                    return;
                debugLine = 2;
                if ( entity.TypeData.OtherSpecialType != OtherSpecialEntityType.Wormhole )
                    return;
                GameEntity_Other wormhole = (GameEntity_Other)entity;
                debugLine = 3;
                Planet currentPlanet = entity.Planet;
                debugLine = 4;
                Planet wormholeDestPlanet = wormhole.GetLinkedPlanet();
                debugLine = 5;
                if ( wormholeDestPlanet == null )
                    return;
                debugLine = 6;
                bool shouldBeSelectedColorOnText = entity == GameEntity_Base.CurrentlyHoveredOver;
                debugLine = 6;
                Faction faction = wormholeDestPlanet.GetControllingFaction();
                debugLine = 7;
                if ( debug ) 
                    ArcenDebugging.ArcenDebugLogSingleLine( "currentPlanet  " + currentPlanet.Name + " wormhole to " + wormholeDestPlanet.Name + " figuring out colour", Verbosity.DoNotShow );
                debugLine = 8;
                
                if ( faction != null && 
                     faction.Type != FactionType.NaturalObject && 
                     wormholeDestPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    debugLine = 9;
                    bool isWaveIncomingThroughWormhole = false;
                    debugLine = 10;
                    Faction controllingFaction = currentPlanet.GetControllingFaction();
                    
                    debugLine = 1001;
                    if ( controllingFaction != null && 
                         controllingFaction.Type == FactionType.Player && 
                         faction.Type == FactionType.AI )
                    {
                        debugLine = 11;
                        AISentinelsFactionBaseInfo sentinelsInfo = faction.GetAISentinelsCoreData();
                        //check whether this wormhole has a wave going to come through it,
                        //and if so then show that
                        debugLine = 12;
                        ProtectedList<PlannedWave> localList = sentinelsInfo.WaveList;
                        debugLine = 13;
                        for ( int i = 0; i < localList.Count; i++ )
                        {
                            debugLine = 14;
                            PlannedWave wave = localList[i];
                            debugLine = 15;
                            if ( wave.playerBeingAlerted )
                            {
                                debugLine = 16;
                                Planet warpGatePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                                debugLine = 17;
                                Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                                debugLine = 18;
                                if ( targetPlanet == currentPlanet && wormholeDestPlanet == warpGatePlanet )
                                {
                                    debugLine = 19;
                                    isWaveIncomingThroughWormhole = true;
                                    debugLine = 20;
                                    i = localList.Count;
                                    debugLine = 21;
                                }
                            }
                        }
                    }
                    else 
                    if ( controllingFaction != null && 
                         controllingFaction.Type == FactionType.NaturalObject &&
                         currentPlanet.UnderInfluenceOfFactionIndex.Count > 0 )
                    {

                    }
                    debugLine = 22;
                    if ( isWaveIncomingThroughWormhole ) //not a AI wave coming, so just show the faction team colour
                    {
                        debugLine = 23;
                        if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "currentPlanet  " + currentPlanet.Name + " wormhole to " + wormholeDestPlanet.Name + " showing incoming wave colour", Verbosity.DoNotShow );
                        debugLine = 24;
                        Color incomingWaveColour = ColorMath.Red;
                        debugLine = 25;
                        Color colorToShow = ColorMath.GetTimeLerpedColor( incomingWaveColour, faction.FactionCenterColor.TeamColor, 3000 );
                        debugLine = 26;
                        Buffer.Add( "<color=#" ).Add( shouldBeSelectedColorOnText ? faction.FactionCenterColor.ColorHexLinearHighlighted : colorToShow.linear.GetHexCode() ).Add( ">" );
                        debugLine = 27;
                    }
                    else
                    {
                        debugLine = 28;
                        if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "currentPlanet  " + currentPlanet.Name + " wormhole to " + wormholeDestPlanet.Name + " showing faction colour " + faction.FactionCenterColor, Verbosity.DoNotShow );
                        debugLine = 29;
                        Buffer.Add( "<color=#" ).Add( shouldBeSelectedColorOnText ? faction.FactionCenterColor.ColorHexLinearHighlighted : faction.FactionCenterColor.ColorHexLinearBrighter ).Add( ">" );
                        debugLine = 30;
                    }
                }
                else 
                if ( faction != null && 
                     faction.Type == FactionType.NaturalObject && 
                     wormholeDestPlanet.IntelLevel > PlanetIntelLevel.Unexplored &&
                     wormholeDestPlanet.UnderInfluenceOfFactionIndex.Count > 0)
                {
                    debugLine = 31;
                    Faction influencer = World_AIW2.Instance.GetFactionByIndex(wormholeDestPlanet.UnderInfluenceOfFactionIndex[0]);
                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "currentPlanet  " + currentPlanet.Name + " wormhole to " + wormholeDestPlanet.Name + " is under the influence of another faction", Verbosity.DoNotShow );
                    debugLine = 32;
                    Buffer.Add( "<color=#" ).Add( shouldBeSelectedColorOnText ? influencer.FactionCenterColor.ColorHexLinearHighlighted : influencer.FactionCenterColor.ColorHexLinearBrighter  ).Add( ">" );
                    debugLine = 33;
                }
                else
                {
                    debugLine = 34;
                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "currentPlanet  " + currentPlanet.Name + " wormhole to " + wormholeDestPlanet.Name + " showing showing no owner", Verbosity.DoNotShow );
                    debugLine = 35;
                    Buffer.Add( "<color=#" ).Add( shouldBeSelectedColorOnText ?  TeamColorDefinitionTable.Instance.Hovered.ColorHexLinear : TeamColorDefinitionTable.Instance.White.ColorHexLinear ).Add( ">" );
                    debugLine = 36;
                }
                debugLine = 37;
                Buffer.Add("<size=40>");
                Buffer.Add( wormholeDestPlanet.Name );
                debugLine = 38;
                Buffer.Add( "</color>" );
                Buffer.Add("</size>");
                debugLine = 39;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Warning: WriteEntityPlanetViewText encountered exception on debugLine==" + debugLine + "\n" + e.ToString(), Verbosity.Chat );
            }
        }
    }
}
