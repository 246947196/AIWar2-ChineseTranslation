using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{

    public class FallenSpireSharedDeepInfo
    {
        public readonly static FallenSpireSharedDeepInfo Instance = new FallenSpireSharedDeepInfo();

        #region HandleSpireCitadelBuiltLogic
        public void HandleSelfBuildingCompleteLogic( GameEntity_Squad entity, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.InternalName == "SpireFrigateNeuralNet") {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_FrigateNeuralNet", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            } else if ( entity.TypeData.InternalName == "SpireDestroyerNeuralNet" ) {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_DestroyerNeuralNet", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            } else if ( entity.TypeData.GetHasTag( "SpireCitadel" ) ) {
                if ( entity.GetIsFactionControlledByAnyPlayerAccount_Safe()
                    && entity.GetNumberInFaction( entity.TypeData ) == 1 )//okay to use here, as it will be consistent per run
                {
                    //At the moment I think the AI should taunt all the players once one gets Spire allies, since
                    //I assume that the AI's counterstrikes will hit all the players
                    //TODO: Once the Spire Citadel is defined in XML, give it a tag and also check for the tag
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerBuildsFirstSpireCitadel );
                }
                if ( entity.GetIsFactionControlledByAnyPlayerAccount_Safe()
                    && entity.GetNumberInFaction( entity.TypeData ) > 1 )//okay to use here, as it will be consistent per run
                {
                    //At the moment I think the AI should taunt all the players once one gets Spire allies, since
                    //I assume that the AI's counterstrikes will hit all the players
                    //TODO: Once the Spire Citadel is defined in XML, give it a tag and also check for the tag
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerBuildsSubsequentSpireCitadel );
                }

                entity.FlagForForcedFullSyncToClients_FromHost();
            }
        }
        #endregion
        public void DoInitializationIfNecessary( Faction faction, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, bool tracing )
        {
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-DoInitializationIfNecessary-trace", 10f ) : null;

            if ( BaseInfo.exoData.StrengthRequiredForNextExo == FInt.Zero )
            {
                BaseInfo.exoData.StrengthRequiredForNextExo = (FInt)BaseInfo.Difficulty.BaseExoStrength;
                BaseInfo.exoData.CurrentExoStrength = FInt.Zero;
                BaseInfo.exoData.NumExosSoFar = 0;
                BaseInfo.exoData.FactionIndexOfExoSpawnFaction = World_AIW2.GetRandomAIFaction( Context ).FactionIndex; //send from a random ai faction each time
                BaseInfo.exoData.ExoReasonOverride = "Fallen Spire";
                BaseInfo.exoData.PercentToStartWarning = 75;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("pre: Time for next relic spawn " + BaseInfo.TimeForNextRelicSpawn + " debug mode " + faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ), Verbosity.DoNotShow );
            if ( BaseInfo.TimeForNextRelicSpawn == -1 )
            {
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                {
                    BaseInfo.TimesForNextSpireDebris.Add( BaseInfo.DebrisSpawnDelay + Context.RandomToUse.Next( BaseInfo.DebrisSpawnDelayRandomness / 10, BaseInfo.DebrisSpawnDelayRandomness ) ); //immediate debris
                    BaseInfo.TimesForNextSpireDebris.Add( BaseInfo.DebrisSpawnDelay + Context.RandomToUse.Next( BaseInfo.DebrisSpawnDelayRandomness / 10, BaseInfo.DebrisSpawnDelayRandomness ) ); //immediate debris
                }

                BaseInfo.TimeForNextRelicSpawn = BaseInfo.FirstRelicSpawnTime;
                if ( tracing )
                    tracingBuffer.Add( "Time for next relic spawn " + BaseInfo.TimeForNextRelicSpawn ).Add( "\n" );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void UpdateExoData( Faction faction, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, bool tracing )
        {
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-UpdateExoData-trace", 10f ) : null;

            int exoStrengthModifier = BaseInfo.NumRelicsCaptured; //this is either the number of relics captured (fallen spire) or the number of cities (infused empire). Used to gauge how large the exo should be

            if ( exoStrengthModifier < BaseInfo.SpireCities.Count )
                exoStrengthModifier = BaseInfo.SpireCities.Count; //for the spire infused empire; it doesn't use relics

            if ( exoStrengthModifier > 0 )
            {
                //the Exo is allowed to charge
                FInt aipAdjustedIncome = (FactionUtilityMethods.Instance.GetCurrentAIP() / 10) * BaseInfo.Difficulty.ExoIncomePer10AIP;
                FInt multiplier = BaseInfo.Difficulty.ExoIncomeMultiplierPerCity * exoStrengthModifier;
                FInt change = (BaseInfo.Difficulty.BaseExoIncome + aipAdjustedIncome) * (multiplier);
                BaseInfo.exoData.UpdateExoStrength (change);
                if ( tracing )
                    tracingBuffer.Add( "Exo strength additions: (base income: " + BaseInfo.Difficulty.BaseExoIncome + " aip income " + aipAdjustedIncome + ") * ( city-based income multiplier " + BaseInfo.Difficulty.ExoIncomeMultiplierPerCity + " * " + BaseInfo.NumRelicsCaptured + ") = " + change + ". exo strength " + BaseInfo.exoData.CurrentExoStrength + " target strength " + BaseInfo.exoData.StrengthRequiredForNextExo ).Add( "\n" );

            }
            BaseInfo.exoData.SyncExoToCPAIfAllowed( faction, Context );
            if ( BaseInfo.exoData.ShouldLaunchExo() )
            {
                List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "FallenSpire-UpdateExoData-workingTargets", 10f );
                if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
                int ExoStrengthToSend = BaseInfo.exoData.CurrentExoStrength.IntValue;
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                    ExoStrengthToSend /= 15;
                BaseInfo.exoData.ResetSync();
                //Sometimes we add some extra targets to make the exo a bit more exciting for the player
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    //For each city, there is a chance of targeting it
                    int percentAdditionalTarget = 20;
                    if ( Context.RandomToUse.Next( 0, 100 ) < percentAdditionalTarget )
                        workingTargets.Add( city );
                }
                ExoOptions options = ExoOptions.CreateWithDefaults( workingTargets, ExoStrengthToSend, World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), faction );
                if ( BaseInfo.SpireCities.Count > 3 )
                {
                    options.newExoLeaderTag="ExtragalacticWar";
                }

                GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );

                ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                BaseInfo.exoData.CurrentExoStrength = FInt.Zero;
                BaseInfo.exoData.NumExosSoFar++;
                BaseInfo.NumExosSent++;
                FInt multiplicativeBasedIncrease = BaseInfo.Difficulty.BaseExoStrength * BaseInfo.Difficulty.MultiplicativeExoStrengthIncreasePerExo * BaseInfo.exoData.NumExosSoFar;
                FInt additiveBaseIncrease = (FInt)BaseInfo.Difficulty.AdditiveExoStrengthIncreasePerExo * BaseInfo.exoData.NumExosSoFar;
                FInt cityBasedMultiplier = BaseInfo.Difficulty.ExoStrengthIncreaseMultiplierPerCity * exoStrengthModifier;
                BaseInfo.exoData.StrengthRequiredForNextExo = (FInt)(BaseInfo.Difficulty.BaseExoStrength +  multiplicativeBasedIncrease + additiveBaseIncrease ) * cityBasedMultiplier;
                
                if ( tracing )
                    tracingBuffer.Add( "Sending exo now.  Next Exo strength will be (" + BaseInfo.Difficulty.BaseExoStrength + " + " + multiplicativeBasedIncrease + " + " + additiveBaseIncrease + " ) * " + cityBasedMultiplier + " = " + BaseInfo.exoData.StrengthRequiredForNextExo + ". NumExosSoFar " + BaseInfo.exoData.NumExosSoFar + " cities: " + BaseInfo.NumRelicsCaptured).Add( "\n" );
                BaseInfo.exoData.FactionIndexOfExoSpawnFaction = World_AIW2.GetRandomAIFaction( Context ).FactionIndex; //send from a random ai faction each time

            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void HandleSpireDebris( List<Faction> AIFactionsForDebris, List<Faction> OtherFactionsForDebris, Faction faction, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, bool tracing )
        {
            int debugCode = 0;
            try
            {
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-HandleSpireDebris-trace", 10f ) : null;
                for ( int j = BaseInfo.TimesForNextSpireDebris.Count - 1; j >= 0; j-- )
                {
                    debugCode = 3100;
                    BaseInfo.TimesForNextSpireDebris[j]--;
                    if ( BaseInfo.TimesForNextSpireDebris[j] == 0 )
                    {
                        debugCode = 3200;
                        BaseInfo.TimesForNextSpireDebris.RemoveAt( j );
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SpireDebris" );
                        if ( entityData == null )
                            ArcenDebugging.ArcenDebugLogSingleLine( "No Spire Debris defined in XML", Verbosity.DoNotShow );
                        Planet spawnPlanet = GetDebrisSpawnPlanet( faction, BaseInfo, Context );
                        if ( spawnPlanet == null )
                        {
                            continue;
                        }
                        PlanetFaction pFaction = spawnPlanet.GetPlanetFactionForFaction( faction );

                        ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );

                        GameEntity_Squad debris = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                        pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "FallenSpire-NewDebris" );
                        if ( debris == null )
                            continue;
                        FallenSpirePerUnitBaseInfo debrisData = debris.CreateExternalBaseInfo<FallenSpirePerUnitBaseInfo>( "FallenSpirePerUnitBaseInfo" );
                        int debrisTime = BaseInfo.Difficulty.DebrisDuration + Context.RandomToUse.Next( 0, 60 );
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SpireDebris", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        if ( BaseInfo.SpireCities.Count == 1 )
                            debrisTime *= 2; //the first set of debris gives you extra time to get
                        debrisData.TimeUntilDebrisVanishes = World_AIW2.Instance.GameSecond + debrisTime;
                        Faction destinationFactionForDebris = GetAvailableFactionForDebris( AIFactionsForDebris, OtherFactionsForDebris, faction, BaseInfo, Context );
                        if ( destinationFactionForDebris == null )
                            debrisData.FactionIndexForDebris = -1;
                        else
                            debrisData.FactionIndexForDebris = destinationFactionForDebris.FactionIndex;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( debris );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "尖塔碎片正在 " + debris.GetPlanetName_Safe() + "上生成。你有 " + debrisTime +
                                " 秒时间取回它，否则他人将捷足先登。新尖塔城市建成后不久便会生成一些尖塔碎片。", ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                        }
                        BaseInfo.SpireDebris.AddToDisplayList( debris );
                    }
                }
                debugCode = 4000;
                foreach ( GameEntity_Squad debris in BaseInfo.SpireDebris.DisplaySquads() )
                {
                    debugCode = 4100;
                    FallenSpirePerUnitBaseInfo debrisData = debris.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
                    //if we are actively hacking this debris, ignore
                    if ( tracing )
                        tracingBuffer.Add("Processing " + debris.ToStringWithPlanet() + " TimeUntilDebrisVanishes " + (debrisData.TimeUntilDebrisVanishes - World_AIW2.Instance.GameSecond) ).Add("\n");
                    if ( debrisData.TimeUntilDebrisVanishes == -1 )
                        continue;

                    if ( World_AIW2.Instance.GameSecond >= debrisData.TimeUntilDebrisVanishes )
                    {
                        if ( tracing )
                            tracingBuffer.Add("\tTime to resolve this debris\n");

                        debugCode = 4200;
                        //Handle AI or minor factions getting spire debris
                        Faction factionToUse = World_AIW2.Instance.GetFactionByIndex( debrisData.FactionIndexForDebris );
                        if ( factionToUse == null )
                            factionToUse = GetAvailableFactionForDebris( AIFactionsForDebris, OtherFactionsForDebris, faction, BaseInfo, Context );
                        debugCode = 4210;
                        if ( factionToUse == null )
                        {
                            debugCode = 4220;
                            ArcenDebugging.ArcenDebugLogSingleLine( "Somehow there are no actual factions available for debris. This is a BUG (or you have won the game)\n", Verbosity.DoNotShow );
                            debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            continue;
                        }
                        if ( factionToUse.FactionIsDefeated ) //in the time between this debris having a faction selected and now, the faction has died. Do nothing
                        {
                            debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            continue;
                        }
                        if ( debris.AmIBeingHacked() ) //this won't be used anymore
                        {
                            //if a player was hacking this debris when it ran out of time, the other faction does not get it
                            debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            continue;
                        }

                        debugCode = 4230;
                        //bool multipleFactionsFlagged = false;

                        Planet debrisPlanet = debris.Planet;
                        ArcenCharacterBuffer workingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-workingBuffer", 10f );
                        if ( factionToUse.Type != FactionType.AI )
                        {
                            debugCode = 4300;
                            factionToUse.HasObtainedSpireDebris = true;
                            if ( faction.RandomImpact != TypeDifficulty.Unset &&
                                 !faction.HasBeenSeenByPlayer &&
                                 !GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" ) )
                                workingBuffer.Add( "一个" ).Add( "随机阵营", factionToUse.FactionCenterColor.ColorHexBrighter ).Add( "已获取" + debris.GetPlanetName_Safe() + "上的尖塔残骸。他们将利用此残骸建造使用尖塔技术的新舰船类型" );
                            else
                                workingBuffer.Add( factionToUse.GetDisplayName(), factionToUse.FactionCenterColor.ColorHexBrighter ).Add( "已获取" + debris.GetPlanetName_Safe() + "上的尖塔残骸。他们将利用此残骸建造使用尖塔技术的新舰船类型" );
                        }
                        else
                        {
                            debugCode = 4400;
                            factionToUse.HasObtainedSpireDebris = true;

                            workingBuffer.Add( "" ).Add( factionToUse.GetDisplayName(), factionToUse.FactionCenterColor.ColorHexBrighter ).Add( "已获取" + debris.GetPlanetName_Safe() + "上的尖塔残骸。他们将修复此残骸以建造一艘强大的舰船来对抗敌人。" );
                            //TODO: Do we want an AI-specific spire unit type? If so, we should use that here
                            debugCode = 4410;
                            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AIShipFromDebris" );
                            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( factionToUse );
                            PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( factionToUse );
                            debugCode = 4420;
                            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                                  factionToUse.CurrentGeneralMarkLevel,
                                                                                  pFaction.FleetUsedAtPlanet, 0,
                                                                                  king.WorldLocation, Context, "FallenSpire-AIShipFromDebris" );
                            debugCode = 4430;
                            if ( entity != null )
                                entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread

                        }
                        debugCode = 4500;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = debrisPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( workingBuffer.ToStringAndReturnToPool(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                        }
                        else
                            workingBuffer.ReturnToPool();
                        debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                }
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandleSpireDebris at debugCode " + debugCode + ". Exception: " + e, Verbosity.DoNotShow );
            }
        }
        public Planet GetDebrisSpawnPlanet( Faction faction, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            //reuses the relic spawn point interface, for convenience
            Planet planet = null;
            int retries = 6; //was 100, and that's likely to break the game in the late game.
            do
            {
                Int16 planetIdx = GetNextRelicSpawnPoint( faction, Context, BaseInfo, true );
                planet = World_AIW2.Instance.GetPlanetByIndex( planetIdx );
                //don't allow duplication
                foreach ( GameEntity_Squad debris in BaseInfo.SpireDebris.DisplaySquads() )
                {
                    if ( debris.Planet == planet )
                    {
                        planet = null;
                        break;
                    }
                }
            } while ( retries-- > 0 && planet == null );
            return planet;
        }
        public Int16 GetNextRelicSpawnPoint( Faction AttachedFaction, ArcenHostOnlySimContext Context, FallenSpireFactionBaseInfo BaseInfo,bool isDebris )
        {
            List<Planet> workingPlanetList = Planet.GetTemporaryPlanetList( "FallenSpire-GetNextRelicSpawnPoint-workingBuffer", 10f );
            if ( workingPlanetList == null ) //blocked for teardown/shutdown; bail
                return -1;

            //returns the planet index of where the relic will go. Also currently reused for the spire debris
            bool debug = false;
            Int16 minHopsFromHumanPlanet = -1;
            Int16 maxHopsFromHumanPlanet = -1;
            byte maxMarkLevel = 2;
            if ( isDebris )
            {
                //debris has slightly different rules
                minHopsFromHumanPlanet = 2;
                maxHopsFromHumanPlanet = 4;
                maxMarkLevel = 4;
            }
            else if ( BaseInfo.NumRelicsCaptured < 1 )
            {
                minHopsFromHumanPlanet = 1;
                maxHopsFromHumanPlanet = 2;
                maxMarkLevel = 2;
            }
            else if ( BaseInfo.NumRelicsCaptured < 3 )
            {
                minHopsFromHumanPlanet = 2;
                maxHopsFromHumanPlanet = 4;
                maxMarkLevel = 3;
            }
            else if ( BaseInfo.NumRelicsCaptured < 5 )
            {
                minHopsFromHumanPlanet = 4;
                maxHopsFromHumanPlanet = 5;
                maxMarkLevel = 5;
            }
            else
            {
                minHopsFromHumanPlanet = 6;
                maxMarkLevel = 7;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "minHopsFromHumanPlanet " + minHopsFromHumanPlanet + " max hops " + maxHopsFromHumanPlanet + " mark level " + maxMarkLevel, Verbosity.DoNotShow );
            int allowedRetries = 6; //was 100, and that's likely to break the game in the late game.
            int retries = 0;
            do
            {
                if ( retries > 0 )
                {
                    //the previous attempt was too restrictive
                    minHopsFromHumanPlanet--;
                    maxHopsFromHumanPlanet++;
                    maxMarkLevel++;
                }
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    if ( planet.IntelLevel == PlanetIntelLevel.Unexplored )
                        continue; //don't put anything on unexplored planets
                    if ( planet.GetControllingFactionType() == FactionType.Player )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because the planet is player owned", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( planet.MarkLevelForAIOnly.Ordinal > maxMarkLevel )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its mark level " + planet.MarkLevelForAIOnly.Ordinal + " > allowed mark level " + maxMarkLevel, Verbosity.DoNotShow );
                        continue;
                    }
                    bool adjacentKing = false;
                    if ( planet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Friendly ).HasKingUnitPresent ||
                         planet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).HasKingUnitPresent )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it is a king planet", Verbosity.DoNotShow );
                        continue;
                    }
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.GetDataByStanceForFaction( AttachedFaction, FactionStance.Friendly ).HasKingUnitPresent ||
                             neighbor.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).HasKingUnitPresent )
                            adjacentKing = true;
                    }
                    if ( adjacentKing == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to a king planet", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( minHopsFromHumanPlanet > 1 )
                    {
                        //this planet must not be too close to a player planet
                        //to make the bounds listed above inclusive minHops - 1 must be used
                        bool foundPlayerPlanetWithinMinHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( (Int16)(minHopsFromHumanPlanet - 1),
                            delegate ( Planet source, Planet destination )
                            {
                                var wh = source.GetWormholeTo(destination.Index);
                                if (wh.TypeData.SpecialWormholeLogic != null)
                                {
                                    if (wh.TypeData.SpecialWormholeLogic.WormholeTrafficFilter(wh, destination.GetWormholeTo(source), "Spire_RelicAndDebrisPlacement_MeasureHopsToHumanPlanet") == WormholeTraffic.Dissallowed)
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            },
                            (Planet.EvaluatorDelegate)null ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMinHops = true;
                        }
                        if ( foundPlayerPlanetWithinMinHops )
                        {
                            continue;
                        }
                    }
                    if ( maxHopsFromHumanPlanet > 0 )
                    {
                        //this planet can't be too far from a player planet
                        bool foundPlayerPlanetWithinMaxHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( maxHopsFromHumanPlanet,
                            delegate ( Planet source, Planet destination )
                            {
                                var wh = source.GetWormholeTo(destination.Index);
                                if (wh.TypeData.SpecialWormholeLogic != null)
                                {
                                    if (wh.TypeData.SpecialWormholeLogic.WormholeTrafficFilter(wh, destination.GetWormholeTo(source), "SpireRelicPlacement_MeasureHopsToHumanPlanet") == WormholeTraffic.Dissallowed)
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            },
                            (Planet.EvaluatorDelegate)null ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMaxHops = true;
                        }
                        if ( !foundPlayerPlanetWithinMaxHops )
                            continue;
                    }
                    workingPlanetList.Add( planet );
                }
                retries++;
            } while ( workingPlanetList.Count == 0 && retries < allowedRetries );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Choosing from " + workingPlanetList.Count + " planets to spawn relic", Verbosity.DoNotShow );
            if ( workingPlanetList.Count <= 0 )
            {
                Planet.ReleaseTemporaryPlanetList( workingPlanetList );
                return -1;
            }
            Int16 ret = workingPlanetList[Context.RandomToUse.Next( 0, workingPlanetList.Count )].Index;
            Planet.ReleaseTemporaryPlanetList( workingPlanetList );
            return ret;
        }

        public Faction GetAvailableFactionForDebris( List<Faction> AIFactionsForDebris, List<Faction> OtherFactionsForDebris, Faction faction, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[j];
                if ( otherFaction.Type == FactionType.Player )
                    continue;
                if ( !otherFaction.SpecialFactionData.CanUseSpireDebris )
                    continue;
                if ( otherFaction.FactionIsDefeated )
                    continue;
                if ( otherFaction.InvasionTime > World_AIW2.Instance.GameSecond )
                    continue; //faction hasn't invaded yet
                if ( otherFaction.Type == FactionType.AI )
                    AIFactionsForDebris.Add( otherFaction );
                else if ( !otherFaction.HasObtainedSpireDebris )
                    OtherFactionsForDebris.Add( otherFaction );
            }
            //Now I have the lists of eligible factions. Now remove all minor factions that are already on an existing piece of debris
            foreach ( GameEntity_Squad debris in BaseInfo.SpireDebris.DisplaySquads() )
            {
                FallenSpirePerUnitBaseInfo debrisData = debris.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
                Faction debrisFaction = World_AIW2.Instance.GetFactionByIndex( debrisData.FactionIndexForDebris );
                if ( debrisFaction == null )
                    continue;
                for ( int j = OtherFactionsForDebris.Count - 1; j >= 0; j-- )
                    if ( OtherFactionsForDebris[j] == debrisFaction )
                        OtherFactionsForDebris.RemoveAt( j );
            }

            //I've now removed all the minor factions currently in use.
            if ( OtherFactionsForDebris.Count > 0 && Context.RandomToUse.Next(0, 80) < 100 )
            {
                //80% chance of picking an available minor faction
                return OtherFactionsForDebris[Context.RandomToUse.Next(0, OtherFactionsForDebris.Count)];
            }
            if ( AIFactionsForDebris.Count > 0 )
                return AIFactionsForDebris[Context.RandomToUse.Next(0, AIFactionsForDebris.Count)];
            return null;
        }

        #region RecalculateSpireFleetsAndFlagships_MainThreadSimOnly
        public void RecalculateSpireFleetsAndFlagships_MainThreadSimOnly( Faction faction, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            //goal: three spire fleets, period, at most.  Regardless of how many players.
            //first fleet comes when you have a single city.
            //second on first city to mark 3, third on city to mark 4

            int intendedNumberOfFleets;
            switch ( BaseInfo.SpireCities.Count )
            {
                case 0:
                    return; //if we have no spire cities, then do nothing
                case 1:
                case 2:
                    intendedNumberOfFleets = 1;
                    break;
                case 3:
                case 4:
                    intendedNumberOfFleets = 2;
                    break;
                default:
                    intendedNumberOfFleets = 3;
                    break;
            }

            int countOfFleetsActuallyHere = 0;
            foreach ( Fleet cityFedFleet in World_AIW2.Instance.Fleets_CityFedMobile( null, FleetStatus.AnyStatus ) )
            {
                if ( cityFedFleet.FleetQualifier == "FallSpi" )
                {
                    countOfFleetsActuallyHere++;
                }
            }
            
            if ( intendedNumberOfFleets > countOfFleetsActuallyHere )
            {
                //if we have too-few fleets, let's create some then!  Only do one at a time, just in case.

                //We spawn this new flagship at the new city
                if ( BaseInfo.SortedSpireCities.Count <= 0 )
                    return; //no cities
                GameEntity_Squad cityToUse = BaseInfo.SortedSpireCities.GetDisplayList()[BaseInfo.SortedSpireCities.Count - 1].GetSquad();
                if ( cityToUse == null )
                    return;

                //here's our new flagship for our new fleet
                GameEntityTypeData fleetLeaderType = GameEntityTypeDataTable.Instance.GetRowByName(
                    BaseInfo.ExpertMode ? "SpireCruiserFlagship_Expert" : "SpireCruiserFlagship"
                );
                GameEntity_Squad newFleetLeader = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cityToUse.PlanetFaction, fleetLeaderType, 1,
                    null, 0, cityToUse.WorldLocation, Context, "FallenSpire-NewFleet" );

                newFleetLeader.FleetMembership.Fleet.NameRaw = "Spire Fleet '" + RandomSpireCityName(Context, cityToUse) + "'";
                newFleetLeader.FleetMembership.Fleet.FleetQualifier = "FallSpi";

                ArcenDebugging.ArcenDebugLogSingleLine( "Added new fleet: countOfFleetsActuallyHere: " + countOfFleetsActuallyHere + " intendedNumberOfFleets: " + intendedNumberOfFleets +
                    " " + newFleetLeader.FleetMembership.Fleet.Category, Verbosity.DoNotShow );
                newFleetLeader.OnClaim(); //run the "on claim" logic since we have just obtained a new flagship. This will trigger achievements
                //any cities that are not bolstering at all will now bolster this
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    if ( city.FleetMembership.Fleet.CityBolstersFleetID <= 0 )
                        city.FleetMembership.Fleet.CityBolstersFleetID = newFleetLeader.FleetMembership.Fleet.FleetID;
                }
                return; //catch up next time around, if we added one
            }
        }
        #endregion
        #region spireCityNames
        //https://ingesanagram.appspot.com/
        //All anagrams or partial anagrams of crystal names
        private static readonly string[] spireCityNames = new string[] {
            "ca Erl", "crt ezra", "az lucre", "nib sod", "bi Ian", "cir ie", "ctr en", "iq or", "iq rue", "qui re", "sire yeg", "eer gi", "et ey",
            "gi ret", "sri ye", "eg eyre", "ems ha", "et ha", "ma set", "st ta" };
        #endregion

        public string RandomSpireCityName( ArcenHostOnlySimContext Context, GameEntity_Squad city )
        {
            if ( World_AIW2.Instance.Setup.GetBoolBySetting("BoringSpireNames") && city != null )
            {
                return city.Planet.Name;
            }
            int num = Context.RandomToUse.Next( 0, 26 ); // Zero to 25
            char firsLet = (char)('a' + num);
            num = Context.RandomToUse.Next( 0, 26 ); // Zero to 25
            char lastLet = (char)('a' + num);
            return firsLet + spireCityNames[Context.RandomToUse.Next( 0, spireCityNames.Length )] + lastLet;
        }

        #region RecalculateSpireCityMobileFleetContents_MainThreadSimOnly
        public BolsteringManager bolsteringManger = new BolsteringManager();
        public void RecalculateSpireCityMobileFleetContents_MainThreadSimOnly( Faction faction, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                List<GameEntityTypeData> spireCityBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "SpireFeedsFleet" );

                int maxMarkLevelOfAnyCity = 0;

                debugStage = 200;

                #region First, Find The Max Mark Level Of Any City
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    debugStage = 300;
                    Fleet fleetForCity = city.GetFleetOrNull_Safe();
                    if ( fleetForCity == null )
                        continue;
                    debugStage = 400;
                    if ( fleetForCity.MaxMarkLevelOfAnyInFleet > maxMarkLevelOfAnyCity )
                        maxMarkLevelOfAnyCity = fleetForCity.MaxMarkLevelOfAnyInFleet;
                }
                #endregion

                debugStage = 1000;

                GameEntityTypeData desiredFlagshipData = null;
                int minMarkLevelOfAllFlagships = maxMarkLevelOfAnyCity;

                debugStage = 1100;

                #region Find The desiredFlagshipData
                if ( !BaseInfo.ExpertMode )
                {
                    debugStage = 1200;
                    if ( maxMarkLevelOfAnyCity >= 6 ) //any city mark 6 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireDreadnoughtFlagship" );
                    else if ( maxMarkLevelOfAnyCity >= 3 ) //any city mark 3 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireBattleshipFlagship" );
                    else
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireCruiserFlagship" );
                }
                else
                {
                    debugStage = 1300;
                    if ( maxMarkLevelOfAnyCity >= 6 ) //any city mark 6 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireDreadnoughtFlagship_Expert" );
                    else if ( maxMarkLevelOfAnyCity >= 3 ) //any city mark 3 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireBattleshipFlagship_Expert" );
                    else
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireCruiserFlagship_Expert" );
                }
                #endregion

                debugStage = 2000;

                debugStage = 3000;

                this.bolsteringManger.HandleBolstering( "SpireFeedsFleet", BaseInfo.SpireCities.GetDisplayList(), BaseInfo.SpirePlayerFleets.GetDisplayList(), Context );

                debugStage = 3100;

                //next loop over all the cities
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    debugStage = 3200;
                    Fleet fleetForCity = city.GetFleetOrNull_Safe();
                    if ( fleetForCity == null )
                        continue;

                    debugStage = 3300;

                    //what fleet is this city bolstering?  Might be null, that's okay
                    Fleet fleetForMobile = fleetForCity.GetFleetBolsteredByThisCity();

                    debugStage = 3400;
                    //if the city is crippled, buildings it contains still provide the mobile fleet stuff anyway
                    //sort of.  They won't reduce cap, but won't raise it either
                    bool isCityItselfDisabled = city.GetIsCrippled() || city.GetIsNonFunctional();

                    if ( isCityItselfDisabled )
                    {
                        continue;
                    }

                    debugStage = 3500;

                    int cruisersToHave = 0;
                    int battleshipsToHave = 0;

                    if ( fleetForCity.MaxMarkLevelOfAnyInFleet >= 6 ) //city mark 6 or higher
                    {
                        battleshipsToHave = (fleetForCity.MaxMarkLevelOfAnyInFleet - 5);
                        cruisersToHave = (fleetForCity.MaxMarkLevelOfAnyInFleet - 2);
                    }
                    else if ( fleetForCity.MaxMarkLevelOfAnyInFleet >= 3 ) //city mark 3 or higher
                        cruisersToHave = (fleetForCity.MaxMarkLevelOfAnyInFleet - 2);

                    debugStage = 3600;

                    string crusierTypeName = "SpireCruiser";
                    string battleshipTypeName = "SpireBattleship";
                    if ( BaseInfo.ExpertMode )
                    {
                        crusierTypeName = "SpireCruiser_Expert";
                        battleshipTypeName = "SpireBattleship_Expert";
                    }
                    debugStage = 3700;

                    if ( cruisersToHave > 0 ) //increase the cap only if the city is not disabled
                    {
                        debugStage = 3800;
                        FleetMembership cruiserMem = fleetForMobile.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( crusierTypeName ), fleetForCity.FleetID );
                        debugStage = 3900;
                        if ( cruiserMem.ExplicitBaseSquadCap < cruisersToHave )
                            cruiserMem.ExplicitBaseSquadCap = cruisersToHave;
                    }

                    if ( battleshipsToHave > 0 ) //increase the cap only if the city is not disabled
                    {
                        debugStage = 4100;
                        FleetMembership battleshipMem = fleetForMobile.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( battleshipTypeName ), fleetForCity.FleetID );
                        debugStage = 4200;
                        if ( battleshipMem.ExplicitBaseSquadCap < battleshipsToHave )
                            battleshipMem.ExplicitBaseSquadCap = battleshipsToHave;
                    }
                }

                debugStage = 5100;

                //now loop over all the mobile fleets for the spire
                foreach ( GameEntity_Squad mobileFlagship in BaseInfo.SpirePlayerFleets.DisplaySquads() )
                {
                    debugStage = 5200;
                    Fleet mobileFleet = mobileFlagship.GetFleetOrNull_Safe();
                    if ( mobileFleet == null )
                        continue;

                    debugStage = 5300;

                    int markLevelForFlagshipsInThisFleet = minMarkLevelOfAllFlagships;

                    if ( mobileFleet.AddedMarkLevelsForFleet_FromScience < markLevelForFlagshipsInThisFleet - 1 )
                        mobileFleet.AddedMarkLevelsForFleet_FromScience = (byte)(markLevelForFlagshipsInThisFleet - 1);

                    debugStage = 5400;

                    #region Find Flagship Types In This Fleet
                    //Backwards iterate because we remove memberships (DF's RemoveAndContinue semantics).
                    for ( int memIdx = mobileFleet.MemberGroupCount - 1; memIdx >= 0; memIdx-- )
                    {
                        FleetMembership mem = mobileFleet.GetMemberGroupAt( memIdx );
                        if ( mem == null )
                            continue;
                        debugStage = 5500;
                        //if this is a flagship type, make sure we don't have extras
                        if ( mem.TypeData.SpecialType == SpecialEntityType.MobileCustomCityFedFleetFlagship )
                        {
                            if ( mem != mobileFlagship.FleetMembership )
                            {
                                mem.DespawnAllContentsFromNoLongerBolstering( Context );
                                mobileFleet.RemoveMemGroupExplicit( mem ); //if it's not the correct type, then kill it
                            }
                        }
                    }
                    #endregion

                    debugStage = 5600;

                    if ( mobileFlagship != null )
                    {
                        debugStage = 5700;

                        //wrong type!
                        if ( mobileFlagship.TypeData != desiredFlagshipData )
                        {
                            debugStage = 5800;
                            GameEntity_Squad newFlagship = mobileFlagship.TransformInto( Context, desiredFlagshipData, 1, mobileFlagship.TypeData.KeepDamageAndDebuffsOnTransformation );
                            newFlagship.OnClaim(); //run the "on claim" logic since we have just obtained a new flagship. This will trigger achievements
                        }
                        else //right type!
                        {
                            debugStage = 5900;
                            mobileFlagship.FleetMembership.ExplicitBaseSquadCap = 1;
                            mobileFlagship.FleetMembership.SetEffectiveSquadCap( 1 );
                        }
                    }
                }

                debugStage = 7200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "RecalculateSpireCityMobileFleetContents_MainThreadSimOnly at debugStage " + debugStage + ", Exception: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion
        private readonly List<SafeSquadWrapper> imperialSpirePossibleTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "FallenSpireFactionDeepInfo-imperialSpirePossibleTargets" );
                                                                                                                                                                                            
        public void HandleImperialSpire( Faction faction, List<int> chokeStrengths, FallenSpireFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            GameEntity_Squad transciever = BaseInfo.Transceiver.Display.GetSquad();
            if ( transciever != null ) {
                if (transciever.SelfBuildingMetalRemaining > 0 ) {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_TranscieverStartedBuilding", string.Empty, faction, null, transciever.PlanetFaction.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                } else if ( !transciever.HasBeenRemovedFromSim || transciever.SecondsSpentAsRemains == 0) {
                    transciever.OnClaim(); //trigger the achievements for the transciever
                    if ( !BaseInfo.ImperialFleetActive && BaseInfo.TimeUntilImperialFleetArrives < 0 )
                    {
                        BaseInfo.TimeUntilImperialFleetArrives = BaseInfo.Difficulty.ImperialSpireWaitTime;
                        BaseInfo.TimeImperialFleetSummoned = World_AIW2.Instance.GameSecond;
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_ImperialFleetInbound", string.Empty, faction, null, transciever.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }
            }
            if ( BaseInfo.TimeUntilImperialFleetArrives == -1 && !BaseInfo.ImperialFleetActive )
                return;
            if ( World.Instance.ConclusionType != CampaignConclusionType.NotConcluded )
                return; //Don't do more imperial spire code if the game is over
            // Until the Imperial spire is active, keep counting down and give the AI a response.
            if ( !BaseInfo.ImperialFleetActive )
            {
                //spawn ai exo in response to imperial spire threat. If the game is over, don't do this anymore
                if ( (BaseInfo.TimeImperialFleetSummoned - World_AIW2.Instance.GameSecond) % BaseInfo.Difficulty.ImperialSpireAttackInterval == 0 )
                {
                    //every "interval" seconds, spawn an exo the size of the last main exo that was sent (first pass; may need its own balance later)
                    List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "FallenSpire-HandleImperialSpire1-workingTargets", 10f );
                    if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                        return;
                    FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
                    int strengthToSend = BaseInfo.exoData.StrengthRequiredForNextExo.IntValue;

                    if ( BaseInfo.Transceiver.Display.GetSquad() != null )
                        workingTargets.Add( BaseInfo.Transceiver.Display ); //also send some ships against the Transceiver, since that might not be on a player homeworld

                    int enemyStrengthOnStrongestChokePoint;
                    int enemyStrengthOnWeakestChokePoint;
                    GetStrongestPlanetEnRouteToHomeworld(World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), chokeStrengths, Context, PathCacheData
                        , out enemyStrengthOnStrongestChokePoint, out enemyStrengthOnWeakestChokePoint); 

                    if ( enemyStrengthOnWeakestChokePoint > ( strengthToSend * FInt.FromParts(2, 500) ).IntValue )
                    {
                        strengthToSend += (strengthToSend * FInt.FromParts(0, 050)).IntValue;
                    }
                    if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                        strengthToSend /= 15;
                    ExoOptions options = ExoOptions.CreateWithDefaults( workingTargets, strengthToSend, World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), faction );
                    options.newExoLeaderTag = "ExtragalacticWar"; //Always use Extragalactic war units for the transciever
                    if ( Context.RandomToUse.Next(0, 100 ) < 25 )
                    {
                        //sometimes send just heavy hitters
                        options.UnitBlocksToUse.Clear();
                        options.UnitBlocksToUse.Add(ExoUnitType.Guardians);
                        options.UnitBlocksToUse.Add(ExoUnitType.ExoLeaders);
                    }
                    options.newExoLeaderTag="ExtragalacticWar";

                    options.exoText = "AI 已检测到帝国尖塔！";

                    ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );

                    GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );
                }
                if ( (BaseInfo.TimeImperialFleetSummoned - World_AIW2.Instance.GameSecond) % BaseInfo.Difficulty.ImperialSpireSecondaryAttackInterval == 0 )
                {
                    //AI gets some bonus strikes against tasty player targets (maybe we can knock out some GCAs or economic command stations to force a brownout?)
                    int strengthToSend = (BaseInfo.exoData.StrengthRequiredForNextExo.IntValue * BaseInfo.Difficulty.ImperialSpireSecondaryMultiplier).IntValue;
                    if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                        strengthToSend /= 15;

                    bool omitHomeworlds = true;
                    ExoGalacticAttackManager.GetTastyPlayerTargets( imperialSpirePossibleTargets, omitHomeworlds );
                    if ( Context.RandomToUse.Next(0, 100 ) < 50 ) //sometimes go in really random
                        ArcenArrays.Randomize( imperialSpirePossibleTargets, Context.RandomToUse, 5);

                    int targetsToAttack = 3;
                    if ( strengthToSend > 50000 )
                        targetsToAttack = 5;
                    if ( targetsToAttack < imperialSpirePossibleTargets.Count )
                        imperialSpirePossibleTargets.RemoveRange(targetsToAttack, imperialSpirePossibleTargets.Count - targetsToAttack );

                    ExoOptions options = ExoOptions.CreateWithDefaults( imperialSpirePossibleTargets, strengthToSend, World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), faction );
                    options.newExoLeaderTag="ExtragalacticWar";
                    options.exoText = "";

                    ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                }
                BaseInfo.TimeUntilImperialFleetArrives--;

                if ( BaseInfo.TimeUntilImperialFleetArrives <= 0 )
                {
                    // The spire is here. Inform the player.
                    BaseInfo.ImperialFleetActive = true;

                    BaseInfo.TimeUntilImperialFleetArrives = -1;
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_ImperialFleetArrived", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                return;
            }
            //The Imperial Spire Fleet is here!
            //spawn imperial spire ships to attack
            if ( World_AIW2.Instance.GameSecond % 5 == 0 )
            {
                List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "FallenSpire-HandleImperialSpire2-workingTargets", 10f );
                if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                    return;

                FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
                if ( BaseInfo.Transceiver.Display.GetSquad() != null )
                    workingTargets.Add(BaseInfo.Transceiver.Display);
                for ( int i = 0; i < workingTargets.Count; i++ )
                {
                    GameEntity_Squad king = workingTargets[i].GetSquad();
                    if ( king == null )
                        continue;
                    PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireDreadnought" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire dreadnaught defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "FallenSpire-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireBattleship" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire battleship defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "FallenSpire-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireCruiser" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire cruiser defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "FallenSpire-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireDestroyer" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire destroyer defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "FallenSpire-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }

                }

                GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );
            }
        }
        private void GetStrongestPlanetEnRouteToHomeworld(Faction faction, List<int> chokeStrengths, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData, out int strongestChoke, out int weakestChoke )
        {
            //find this faction's king, then all the player kings
            List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "FallenSpire-GetStrongestPlanetEnRouteToHomeworld-workingTargets", 10f );
            if ( workingTargets == null ) //blocked for teardown/shutdown; bail
            {
                strongestChoke = 0;
                weakestChoke = 0;
                return;
            }

            FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing(faction);
            if ( king == null )
            {
                strongestChoke = 0;
                weakestChoke = 0;

                return; //the game seems to be over
            }
            chokeStrengths.Clear();

            for ( int i = 0; i < workingTargets.Count; i++ )
            {
                int strongestChokeForThisFaction = -1;
                PathBetweenPlanetsForFaction pathCacheForDangerCalculation = PathingHelper.FindPathFreshOrFromCache( faction, "FallenSpireGetStrongestPlanetEnRouteToHomeworld", 
                    king.Planet, workingTargets[i].Planet, PathingMode.Default, Context, PathCacheData );
                if ( pathCacheForDangerCalculation != null )
                {
                    for ( int j = 0; j < pathCacheForDangerCalculation.PathToReadOnly.Count; j++ )
                    {
                        Planet planet = pathCacheForDangerCalculation.PathToReadOnly[j];
                        int enemystrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;
                        int mystrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self].TotalStrength + planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Friendly].TotalStrength;
                        if ( enemystrength - mystrength > strongestChokeForThisFaction )
                            strongestChokeForThisFaction = enemystrength - mystrength;
                    }
                }
                chokeStrengths.Add(strongestChokeForThisFaction);
            }

            GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );

            if ( chokeStrengths.Count == 1 )
            {
                //single enemy case is easy
                strongestChoke = weakestChoke = chokeStrengths[0];
                return;
            }
            chokeStrengths.Sort(static delegate(int L, int R)
            {
                return L.CompareTo(R);
            } );
            strongestChoke = chokeStrengths[0];
            weakestChoke = chokeStrengths[chokeStrengths.Count - 1];
        }
        public void SpawnDragons( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                Faction kingOwner = king.GetFactionOrNull_Safe(); 
                if ( kingOwner.Type != FactionType.AI )
                    continue;
                debugCode = 900;
                AISentinelsFactionBaseInfo sentinelsBaseInfo = kingOwner.GetAISentinelsCoreData();
                Faction praetorian = sentinelsBaseInfo.SubFac_Praetorian;
                if ( praetorian == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no praetorian guard was found while spawning dragons for " + kingOwner.GetDisplayName(), Verbosity.DoNotShow );
                    continue;
                }
                debugCode = 1000;
                AITypeData aiType = sentinelsBaseInfo.SentinelInfo.AIType;
                int numDragons = 1;
                if ( aiType.InternalName == "PraetorHard" || aiType.InternalName == "PraetorBrutal" )
                {
                    if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                        numDragons++;
                }
                debugCode = 1100;
                for ( int j = 0; j < numDragons; j++ )
                {
                    PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( praetorian );
                    GameEntityTypeData dragonData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AIDragon" );
                    GameEntity_Squad dragon = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, dragonData,
                                                                                               king.CurrentMarkLevel,
                                                                                               pFaction.FleetUsedAtPlanet, 0,
                                                                                               king.WorldLocation, Context, "FallenSpire-AIDragon" );
                    if ( dragon != null )
                    {
                        dragon.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                        dragon.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                }
            }
            if ( debugCode > 0 )
            { }
        }
    }
}
