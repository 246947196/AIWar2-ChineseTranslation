using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    //TODO: check the xml is working
    //Investigate setting Log message instead of just PlaySound
    //Do patrol behaviour    
    public sealed class HumanResistanceFighterFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public HumanResistanceFighterFactionBaseInfo BaseInfo;
        public static HumanResistanceFighterFactionDeepInfo Instance;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            Instance = this;
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<HumanResistanceFighterFactionBaseInfo>();
        }
        
        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            playWarpOutSoundVictory = false;
            playWarpOutSoundDefeat = false;
            afterBattleBehaviour = "Unset";
            eligiblePlanets.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;
        
        private bool playWarpOutSoundVictory = false; //some ships have warped out this Second (I'm not sure if I can call Presentation from LongRangePlanning, so set this in LongRangePlanning and then play the sound effect in PerSecond
        private bool playWarpOutSoundDefeat = false; //some ships have warped out this Second (I'm not sure if I can call Presentation from LongRangePlanning, so set this in LongRangePlanning and then play the sound effect in PerSecond

        public string afterBattleBehaviour = "Unset"; //After ships spawn and the battle is over, the HRF will either
                                                      //remain to patrol your planets or will warp back out and join another battle later

        //this is reset every LongRangePlanning step, so it doesn't need to by Serialized
        private readonly List<Planet> eligiblePlanets = List<Planet>.Create_WillNeverBeGCed( 500, "HumanResistanceFighterFactionDeepInfo-eligiblePlanets" ); 

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
               StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, "HRFShipGranter", SeedingType.HardcodedCount, 1,
                       MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3, 8, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            /* There are a few distinct pieces here.
               First, we figure out whether there are any planets that the HRF should send troops to.

               Second, handle the actual battle.

               Third, handle the post-battle, where the ships will either warp out or start patrolling.
               For the warp out, ships will fly to the edge of the gravity well then do the Spawn effect, but it will get bigger for a second and then vanish.*/
            /* Stage 1: check for eligible planets */

            bool stageOneDebug = BaseInfo.LogStage1;
            this.eligiblePlanets.Clear();
            FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( this.AttachedFaction, Context, 5f );
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction controllingFaction = planet.GetPlanetFactionForFaction( planet.GetControllingFaction() );
                int hostileMobileStrength = planet.GetPlanetFactionForFaction( this.AttachedFaction ).DataByStance[FactionStance.Hostile].MobileStrength;
                int hostileTotalStrength = planet.GetPlanetFactionForFaction( this.AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                int friendlyTotalStrength = planet.GetPlanetFactionForFaction( this.AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength;
                int friendlyPlusHRF = (friendlyTotalStrength + this.BaseInfo.Budget * this.BaseInfo.Braveness_Constant).IntValue;

                if ( this.BaseInfo.Budget < this.BaseInfo.MinBudgetToHelp )
                    break; //if we don't have enough budget, do nothing

                //Check for ineligibility.
                if ( this.BaseInfo.IneligiblePlanets.ContainsKey( planet ) )
                {
                    if ( stageOneDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " is ineligible until " + this.BaseInfo.IneligiblePlanets[planet] + ". It is now " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                    continue; //skip planets we have recently sent forces to
                }
                // avoid divide by zeros
                if ( hostileTotalStrength <= 0 || hostileTotalStrength <= 0 || friendlyTotalStrength <= 0 || friendlyPlusHRF <= 0 )
                { 
                    continue;
                }
                
                if ( hostileMobileStrength < this.BaseInfo.EnemyMinStrength)
                {
                    continue;
                }
                if ( friendlyTotalStrength < this.BaseInfo.FriendlyMinStrength)
                {
                    continue;
                }

                if ( ( friendlyTotalStrength / hostileTotalStrength > this.BaseInfo.OverkillRatio ) ||
                     ( hostileTotalStrength / (friendlyTotalStrength + (this.BaseInfo.Budget/2) )  > this.BaseInfo.OverkillRatio ) ) //converting budget to strength is perilous, so this is an approximation at best
                {
                    if ( stageOneDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "friendly " + friendlyTotalStrength + " hostile " + hostileMobileStrength + " maxRatio " + this.BaseInfo.OverkillRatio, Verbosity.DoNotShow );
                    continue; //don't help a fight that's already one sided on either side
                }
                FInt ratio = (FInt)friendlyTotalStrength / (FInt)hostileMobileStrength;
                if ( stageOneDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "budget " + this.BaseInfo.Budget + " friendlyTotalstrength " + friendlyTotalStrength + " friendlyPlusHRF " + friendlyPlusHRF + " ratio: " + ratio, Verbosity.DoNotShow );
                if ( controllingFaction.Faction.GetIsFriendlyTowards( this.AttachedFaction ) )
                {
                    if ( ratio > this.BaseInfo.RatioForFriendlyPlanet )
                    {
                        if ( stageOneDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Name + " is eligible, friendly planet. Ratio " + ratio, Verbosity.DoNotShow );
                        this.eligiblePlanets.Add( planet );
                    }
                    //friendly planet
                    //add this to Eligible Planets
                }
                else if ( controllingFaction.Faction.GetIsHostileTowards( this.AttachedFaction ) )
                {
                    if ( FactionUtilityMethods.Instance.DoesPlanetHaveAIEye ( planet ) )
                    {
                        if ( stageOneDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Name + " has an eye; don't attack", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( ratio > this.BaseInfo.RatioForEnemyPlanet )
                    {
                        if ( stageOneDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Name + " is eligible, enemy planet. Ratio " + ratio, Verbosity.DoNotShow );

                        this.eligiblePlanets.Add( planet );
                    }
                    //hostile planet
                }
                else
                {
                    //neutral planet
                    if ( ratio > this.BaseInfo.RatioForNeutralPlanet )
                    {
                        if ( stageOneDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Name + " is eligible, neutral planet. Ratio " + ratio, Verbosity.DoNotShow );

                        this.eligiblePlanets.Add( planet );
                    }
                }
            }
            if(stageOneDebug)
                ArcenDebugging.ArcenDebugLogSingleLine("There are " + this.eligiblePlanets.Count + " eligible planets in longrangeplanning", Verbosity.DoNotShow );
            /* Stage 2: handle spawned ships */
            bool stageTwoDebug = BaseInfo.LogStage2;
            AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                if ( entity.TypeData.GetHasTag("HRFShipGranter") )
                    continue;
                Planet currentPlanet = entity.Planet;
                if(stageTwoDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Entity " + entity.PrimaryKeyID + " on planet " + currentPlanet.Name + " friendly strength " + currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength + " enemy strength " + currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].MobileStrength + " post battle behaviour " + this.BaseInfo.PostBattleBehaviour, Verbosity.DoNotShow );
                if(currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength == 0)
                {
                    //If all your allies are dead, run away
                    if(stageTwoDebug)
                        ArcenDebugging.ArcenDebugLogSingleLine("Entity warping out (no allies)", Verbosity.DoNotShow );
                    this.BaseInfo.Budget += entity.GetStrengthOfStack();

                    //ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    //Ideally we would figure out the direction "Away from the center"
                    //Practically we're just going to pick a random angle for the moment
                    ArcenPoint WarpOutDestination = entity.WorldLocation.GetPointAtAngleAndDistance(angle, entity.Planet.GravWellSize.DistanceScale_GravwellRadius);
                    entity.despawnVis = DespawnVisualization.WarpOut;
                    entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                     //Proposed: on Spawn/Despawn in the Vis code, check for a DespawnAnimation or a SpawnAnimation
                     //having been set on the entity. Note that I'll need to tell SpawnEntity what information to add.
                     //Perhaps isntead of setting the DespawnAnimation on the GameEntity it gets set on the GameEntityTypeData?
                     //Or I'll need to pass in a boolean to SpawnEntity()

                     // entity.DoFancyDespawnAnimation();
                     this.playWarpOutSoundDefeat = true;
                }
                if ( currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].MobileStrength == 0)
                {
                    //Handle the case where all the enemy fleet is gone
                    if(this.BaseInfo.PostBattleBehaviour == "HitAndRun")
                    {
                        if(stageTwoDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Entity warping out (No more enemy fleet)", Verbosity.DoNotShow );
                        this.BaseInfo.Budget += entity.GetStrengthOfStack();
                        ArcenPoint WarpOutDestination = entity.WorldLocation.GetPointAtAngleAndDistance(angle, entity.Planet.GravWellSize.DistanceScale_GravwellRadius);
                        entity.despawnVis = DespawnVisualization.WarpOut;
  //                      entity.DoFancyDespawnAnimation();
//                        entity.VisualLinkObject.DetachVisObjectFromSim();
                        entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                        if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                            this.playWarpOutSoundVictory = true;
                    }
                    else
                    {
                        //just patrol this planet (possibly also respond to adjacent endangered planets?). Note that this is not implemented,
                        //and can be a TODO for future work if we want it
                    }
                }
                if ( currentPlanet.GetPlanetFactionForFaction( this.AttachedFaction ).DataByStance[FactionStance.Hostile].MobileStrength > 0 )
                {
                    //there are enemies! Fight them!
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //definitely breaks in multiplayer, because nonsim thread
                }

            }
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {

        }
        
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            //Context.RandomToUse.ReinitializeWithSeed(World_AIW2.Instance.CurrentGalaxy.RandomSeedBase.HashCombine(World_AIW2.Instance.GameSecond).HashCombine(this.AttachedFaction.FactionIndex));
            
            int debugstage = 0;
            try
            {
                bool stageThreeDebug = BaseInfo.LogStage3;
                
                debugstage = 100;
                updateBudget(AttachedFaction);
                
                debugstage = 200;
                if(this.playWarpOutSoundVictory)
                {
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                        World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#"+AttachedFaction.FactionCenterColor.ColorHexBrighter +">Human Resistance Fighters</color> warping out", 
                            ChatType.LogToCentralChat, "ArkChiefOfStaff_VictoryWarpOut", null );
                    if (stageThreeDebug)
                        ArcenDebugging.ArcenDebugLogSingleLine("HRF has just warped out! (victory)", Verbosity.DoNotShow );
    //                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SFXItemType_NonPositional.HumanResistanceFightersWarpOutVictory );
                    this.playWarpOutSoundVictory = false;
                }
                
                debugstage = 300;
                if(this.playWarpOutSoundDefeat)
                {
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                        World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#"+AttachedFaction.FactionCenterColor.ColorHexBrighter +">Human Resistance Fighters</color> warping out", 
                            ChatType.LogToCentralChat, "ArkChiefOfStaff_DefeatWarpOut", null );
                    if (stageThreeDebug)
                        ArcenDebugging.ArcenDebugLogSingleLine("HRF has just warped out! (defeat)", Verbosity.DoNotShow );
    //                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SFXItemType_NonPositional.HumanResistanceFightersWarpOutDefeat );
                    this.playWarpOutSoundDefeat = false;
                }
                
                debugstage = 400;
                
                //iterate over ineligiblePlanets and remove any from the list that are now eligible
                int pairCount = this.BaseInfo.IneligiblePlanets.Count;
                foreach ( KeyValuePair<Planet, int> pair in this.BaseInfo.IneligiblePlanets )
                {
                    if(pair.Value < World_AIW2.Instance.GameSecond)
                    {
                        if(stageThreeDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Removing planet " + pair.Key + " from the Ineligible list. Ineligible until " + pair.Value + " < " + World_AIW2.Instance.GameSecond + " (current second)", Verbosity.DoNotShow );
                        this.BaseInfo.IneligiblePlanets.Remove(pair.Key);
                    }
                }

                debugstage = 500;
                
                Planet targetPlanet = null;
                if(this.eligiblePlanets.Count > 0)
                {
                    //Pick the best eligible planet, then decide whether to help it.
                    //If we do help it, spawn a bunch of ships on the planet
                    //Whether we help or not, add it to the IneligiblePlanets list
                    targetPlanet = this.eligiblePlanets[Context.RandomToUse.Next( 0, this.eligiblePlanets.Count )];
                    if(targetPlanet == null)
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("HRF: target planet is null for no obvious reason. This is a confusing bug", Verbosity.DoNotShow );
                        return;
                    }
                    this.BaseInfo.IneligiblePlanets[targetPlanet] = World_AIW2.Instance.GameSecond + this.BaseInfo.IneligibleSeconds;
                    if(stageThreeDebug)
                        ArcenDebugging.ArcenDebugLogSingleLine("planet " + targetPlanet.Name + " is now ineligible until " + (World_AIW2.Instance.GameSecond + this.BaseInfo.IneligibleSeconds) + " aka " + this.BaseInfo.IneligibleSeconds + " seconds" , Verbosity.DoNotShow );
                    int randomNum = Context.RandomToUse.Next( 0, 100);
                    if( randomNum > this.BaseInfo.GetChanceOfHelping() )
                    {
                        //The HRF only helps sometimes
                        if(stageThreeDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine(targetPlanet.Name + ": not helping; " + randomNum + " > " + this.BaseInfo.GetChanceOfHelping() , Verbosity.DoNotShow );
                        this.BaseInfo.IneligiblePlanets[targetPlanet] = World_AIW2.Instance.GameSecond + this.BaseInfo.IneligibleSeconds;
                        this.eligiblePlanets.Remove(targetPlanet);
                        targetPlanet = null;
                    }
                    else if(stageThreeDebug)
                        ArcenDebugging.ArcenDebugLogSingleLine(targetPlanet.Name + ": helping; " + randomNum + " < " + this.BaseInfo.GetChanceOfHelping() , Verbosity.DoNotShow );
                    this.eligiblePlanets.Remove(targetPlanet);
                }

                debugstage = 600;
                
                void SpawnOnTarget()
                {
                    var bag = ObjectBag.GetTemporary("Hrf.tospawn", 5.0f);
                    if ( bag == null ) //blocked for teardown/shutdown; bail
                        return;
                    var spawnTypes = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "HRFSpawn" );
                    var spawnDict = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict("Hrf.spawnDict", 5.0f);
                    if ( spawnDict == null ) //blocked for teardown/shutdown; bail
                    {
                        ObjectBag.ReleaseTemporary(bag);
                        return;
                    }
                    var optSpawnedShips = GameEntity_Squad.GetTemporaryUnsafeSquadList("Hrf.optSpawnedShips", 5.0f);
                    if ( optSpawnedShips == null ) //blocked for teardown/shutdown; bail
                    {
                        ObjectBag.ReleaseTemporary(bag);
                        GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict(spawnDict);
                        return;
                    }
                    try
                    {
                        //Spend the budget spawning ships
                        //Also find a point on the gravity well
                        AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                        ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                        float warpInMultiplier = 0.9f;
                        ArcenPoint spawnLocation = center.GetPointAtAngleAndDistance(angle, (int)(targetPlanet.GravWellSize.DistanceScale_GravwellRadius * warpInMultiplier));
                        ArcenPoint WarpInStart = center.GetPointAtAngleAndDistance(angle, targetPlanet.GravWellSize.DistanceScale_GravwellRadius);
                        PlanetFaction controllingFaction = targetPlanet.GetPlanetFactionForFaction( targetPlanet.GetControllingFaction() );

        //                World_AIW2.Instance.QueueChatMessageOrCommand( "Human Resistance Fighters arriving to reinforce " + targetPlanet.Name  , Context );
                        if ( targetPlanet.GetDoHumansHaveVision() && ArcenNetworkAuthority.GetIsHostMode() ) //only have the host notify everyone else, or you get duplicates
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = targetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#"+AttachedFaction.FactionCenterColor.ColorHexBrighter +">Human Resistance Fighters</color> arriving to reinforce " + 
                                targetPlanet.Name, ChatType.LogToCentralChat, "ArkChiefOfStaff_VictoryWarpIn", chatHandlerOrNull );
                        }

                        if(stageThreeDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("HRF with budget " + this.BaseInfo.Budget + " spawning on planet " + targetPlanet.Name + "; it will remain ineligible until " + this.BaseInfo.IneligiblePlanets[targetPlanet], Verbosity.DoNotShow );
                        
                        debugstage = 610;
                        
                        int percentLow = 50;
                        int percentMed = 30;
                        if (this.BaseInfo.Intensity < 5)
                        {
                            percentLow = 60;
                            percentMed = 20;
                        }
                        int percentHigh = 100 - (percentLow + percentMed);
                        
                        if (spawnTypes.Count == 0)
                        {
                            if(stageThreeDebug)
                                LOG.Msg("No ships are tagged with HRFSpawn ..");
                            return;
                        }
                        
                        int average_ship_cost = 0;
                        foreach (var type in spawnTypes)
                        {
                            int weight = 0;
                            if (type.GetHasTag("LowHRFSpawn"))
                                weight = percentLow;
                            else 
                            if (type.GetHasTag("MediumHRFSpawn"))
                                weight = percentMed;
                            else 
                            if (type.GetHasTag("HighHRFSpawn"))
                                weight = percentLow;
                            
                            if (type.CostForAIToPurchase <= 0)
                            {
                                LOG.Msg("{0} has no AICost so is skipped.", type.OrNull());
                                continue;
                            }
                            
                            average_ship_cost += type.CostForAIToPurchase;
                            bag.AddItem(type, weight);
                        }
                        
                        if (average_ship_cost == 0)
                        {
                            if (stageThreeDebug)
                                LOG.Msg("HRFSpawn ships have no cost specified.");
                            return;
                        }
                        
                        average_ship_cost /= bag.InternalOnly_totalCount;
                        
                        int budget = this.BaseInfo.Budget.ToInt();
                        int approx_num_to_spawn = budget / average_ship_cost;
                        
                        int min = 1;
                        int max = 5;
                        int num100 = approx_num_to_spawn / 100;
                        if (num100 > 0)
                        {
                            min *= num100;
                            max *= num100;
                        }
                        
                        while (true)
                        {
                            var next = bag.PickRandomItemAndReplace(Context.RandomToUse) as GameEntityTypeData;
                            if (next == null)
                                break;

                            var num = Context.RandomToUse.NextInclus(min, max);
                            var cost = next.CostForAIToPurchase * num;
                            var over = (budget - cost) * -1;
                            if (over > 0 && num > 1)
                            {
                                num = 1;
                                cost = next.CostForAIToPurchase;
                            }
                            
                            if (num > 0)
                                spawnDict[next] += num;

                            budget -= num * next.CostForAIToPurchase;
                            
                            if (budget <= 0)
                                break;
                        }

                        this.BaseInfo.Budget = budget.ToFInt();

                        debugstage = 3900;

                        ShipSpawning.Spawn(
                            Context, 
                            onPlanet:targetPlanet, 
                            forFaction:this.AttachedFaction, 
                            inFleet:null, 
                            typeCounts:spawnDict, 
                            atLocation:spawnLocation, 
                            debugtext:"HRF-Units", 
                            optSpawnedShips:optSpawnedShips);

                        var planetfac = targetPlanet.GetPlanetFactionForFaction(this.AttachedFaction);
                        foreach (var e in optSpawnedShips)
                        {
                            e.SetCurrentMarkLevel(e.TypeData.MarkFor(planetfac));
                            e.spawnVis = SpawnVisualization.WarpIn;
                            e.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                        }
                    }
                    finally
                    {
                        ObjectBag.ReleaseTemporary(bag);
                        GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict(spawnDict);
                        GameEntity_Squad.ReleaseTemporaryUnsafeSquadList(optSpawnedShips);
                    }
                }
                
                if (targetPlanet != null)
                    SpawnOnTarget();
                
                debugstage = 700;
                
                //we need to check for our allegiances because things like the Dyson or
                //Nanocaust can change allegiance
                AllegianceHelper.AllyThisFactionToHumans(AttachedFaction);
            }
            catch (Exception e)
            {
                LOG.Err("Exception in {0}() at debugstage={1}.\n{2}", this.TypeNameAndMethod(), debugstage, e);
            }
        }

        private void updateBudget(Faction faction)
        {
            bool budgetDebug = BaseInfo.LogBudget;
            
            FInt budgetPerSecond = this.BaseInfo.BudgetPerSecond;
            if( World_AIW2.Instance.GameSecond == 1)
                this.BaseInfo.Budget =  this.BaseInfo.StartingBudget; //the starting budget is mostly for testing

            if(this.BaseInfo.Budget < FInt.Zero)
                this.BaseInfo.Budget = FInt.Zero; //it's possible to overspend if the last item purchased is pricey
            FInt multiplier =  this.BaseInfo.getBudgetMutliplier();
            this.BaseInfo.Budget += budgetPerSecond * multiplier;
            if(budgetDebug)
                ArcenDebugging.ArcenDebugLogSingleLine("budgetPerSecond " + budgetPerSecond + " multiplier " + multiplier + " new budget total " + BaseInfo.Budget , Verbosity.DoNotShow );

            int numHoursIntoGame = (World_AIW2.Instance.GameSecond / 3600) + 1;
            //the budget for the HRF BaseMaxBudget * number of hours into the game * MAX(intensity multiplier, 1)
            FInt MaxBudget = this.BaseInfo.MaxBudget * numHoursIntoGame * multiplier;
            if(this.BaseInfo.Budget > MaxBudget)
                this.BaseInfo.Budget = MaxBudget;
        }
    }
}
