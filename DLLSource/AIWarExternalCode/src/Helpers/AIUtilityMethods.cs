using Arcen.AIW2.Core;
using System;
using Arcen.Universal;
using System.Text;

namespace Arcen.AIW2.External
{
    public static class AIUtilityMethods
    {
        public static FInt GetGuardingUnitAICostPurchaseCap( Faction faction, ReinforcementType reinforcementType, 
            Planet planet, bool IsForInitialSeeding, bool WriteDetailsToLog,
            //this is optional so that if we already have it, we can just pass it in and not have to keep getting it
            AISentinelsCoreData factionExternal = null )
        {
            FInt result;
            if ( factionExternal == null )
                factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;

            StringBuilder builder = (WriteDetailsToLog ? new StringBuilder() : null);
            if ( WriteDetailsToLog )
                builder.Append( "Purchase Cap For Planet: " ).Append( planet.Name ).Append( " for faction " )
                    .Append( faction.FactionIndex ).Append( " type: " ).AppendLine( reinforcementType.ToString() );

            //Using null for faction, because any AI that reinforces to a planet gets to use the combined might of all the guard posts there
            //from any AIs.  Even in civil war mode, fine, they still work this way.
            //Reinforcement points are comand stations, guard posts, and a few other things like special forces ninja hideouts.
            int numReinforcementPoints = planet.GetNumberIn( EntityRollupType.ReinforcementLocations, null, false, false );
            if ( numReinforcementPoints <= 0 )
            {
                if ( WriteDetailsToLog )
                {
                    builder.AppendLine( "No reinforcement points, so no budget." );
                    ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
                }
                return FInt.Zero; //if there's nowhere to reinforce, we also have no budget
            }

            if ( numReinforcementPoints > planet.MaxReinforcementPlacesEverSeenHere )
                numReinforcementPoints = planet.MaxReinforcementPlacesEverSeenHere;
            bool totalsCanGoUpOrDownFromPlanetType_GeneralQuantities = true;
            bool totalsCanGoUpOrDownFromMarkLevel = true;

            try
            {
                if ( World_AIW2.Instance.GameSecond > 5 )
                {
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int alliedStrength = myFactionData[FactionStance.Friendly].TotalStrength;
                    if ( hostileStrength > alliedStrength + alliedStrength )
                    {
                        if ( WriteDetailsToLog )
                        {
                            builder.Append( "hostileStrength: " ).Append( hostileStrength ).Append( " alliedStrength: " ).AppendLine( alliedStrength.ToString() );
                            builder.AppendLine( "Hostile strength here is at least twice my strength plus allies, so no budget." );
                            ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
                        }
                        return FInt.Zero;
                    }
                }
            }
            catch { } //early on in the game, or during mapgen, this might be all fouled up.

            //first up, each AI has a cost, in AI cost units, for each of the four categories of unit.
            //some AIs do a strange thing like put guardians into the strikecraft category (Royal does this), and in those cases this can be modified per AI.
            //note that these are no longer per-planet amounts like they once were, but instead are per-reinforcement-point.
            //this is because we want neutering to actually matter.
            //A reinforcement point is a command station, a guard post, or a few things like special forces ninja hideouts.
            switch ( reinforcementType )
            {
                case ReinforcementType.Guardian:
                    result = factionExternal.AIType.InitialAIDefensesGuardiansBudgetPerMark3Planet;
                    totalsCanGoUpOrDownFromPlanetType_GeneralQuantities = false;
                    if ( WriteDetailsToLog )
                        builder.Append( "InitialAIDefensesGuardiansBudgetPerMark3Planet: " ).AppendLine( result.ToFloatNonSim().ToString() );
                    break;
                case ReinforcementType.Turret:
                    result = factionExternal.AIType.InitialAIDefensesTurretBudgetPerMark3Planet;
                    if ( WriteDetailsToLog )
                        builder.Append( "InitialAIDefensesTurretBudgetPerMark3Planet: " ).AppendLine( result.ToFloatNonSim().ToString() );
                    totalsCanGoUpOrDownFromPlanetType_GeneralQuantities = false;
                    totalsCanGoUpOrDownFromMarkLevel = false;
                    if ( planet.PlanetAITurretSeedingCap == PlanetAITurretAnyNonTurretDefensesCap.NoneAllowed )
                    {
                        if ( WriteDetailsToLog )
                        {
                            builder.AppendLine( "PlanetAITurretAnyNonTurretDefensesCap.NoneAllowed, so no budget." );
                            ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
                        }
                        return FInt.Zero;
                    }
                    break;
                case ReinforcementType.NonTurretDefense:
                    result = factionExternal.AIType.InitialAIDefensesNonTurretDefenseBudgetPerMark3Planet;
                    if ( WriteDetailsToLog )
                        builder.Append( "InitialAIDefensesNonTurretDefenseBudgetPerMark3Planet: " ).AppendLine( result.ToFloatNonSim().ToString() );
                    totalsCanGoUpOrDownFromPlanetType_GeneralQuantities = false;
                    totalsCanGoUpOrDownFromMarkLevel = false;
                    if ( planet.PlanetAITurretSeedingCap == PlanetAITurretAnyNonTurretDefensesCap.NoneAllowed )
                    {
                        if ( WriteDetailsToLog )
                        {
                            builder.AppendLine( "PlanetAITurretAnyNonTurretDefensesCap.NoneAllowed, so no budget." );
                            ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
                        }
                        return FInt.Zero;
                    }
                    break;
                case ReinforcementType.Strikecraft:
                    result = factionExternal.AIType.InitialAIDefensesStrikecraftBudgetPerMark3Planet;
                    if ( WriteDetailsToLog )
                        builder.Append( "InitialAIDefensesStrikecraftBudgetPerMark3Planet: " ).AppendLine( result.ToFloatNonSim().ToString() );
                    break;
                default:
                    {
                        if ( WriteDetailsToLog )
                        {
                            builder.AppendLine( reinforcementType + " has no case in switch statement, so no budget." );
                            ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
                        }
                        return FInt.Zero;
                    }
            }
            //this is giving more or less based on the AI difficulty, and actually is pretty substantial now.
            //It's a multiplier in the ballpark of 2x for diff 10, and some small decimal for difficulty 1.
            //Difficulty 7 should ALWAYS be 1.0, since that's the main thing we balance around.
            //This is defensive_cap_multiplier
            if ( IsForInitialSeeding )
            {
                result *= factionExternal.AIDifficulty.DefensiveCapMultiplier_Initial;

                if ( WriteDetailsToLog )
                    builder.Append( "DefensiveCapMultiplier_Initial: " ).Append( factionExternal.AIDifficulty.DefensiveCapMultiplier_Initial.ToFloatNonSim().ToString() )
                        .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
            }
            else
            {
                result *= factionExternal.AIDifficulty.DefensiveCapMultiplier_Ongoing;

                if ( WriteDetailsToLog )
                    builder.Append( "DefensiveCapMultiplier_Ongoing: " ).Append( factionExternal.AIDifficulty.DefensiveCapMultiplier_Ongoing.ToFloatNonSim().ToString() )
                        .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );

            }

            if ( totalsCanGoUpOrDownFromMarkLevel )
            {
                //Okay, so planets get a mark-level boost to these, too.
                //ai_planet_defense_multiplier is what you're looking for on the mark level.
                //This is now something that should be lower than 1x for the first two difficulties,
                //then barely cross the threshold of 1x for difficulty 3, and goes no higher than 4x
                //for difficulty 7.  The strength values are multiplied like crazy as it is since cost
                //does not increase for the AI as mark levels go up.  So we don't want to have 
                //an exponent on our hands, which is what we get if mark 2 is a 2x multiplier, etc.
                result *= planet.MarkLevelForAIOnly.AIPlanetDefenseMultiplier;

                if ( WriteDetailsToLog )
                    builder.Append( "totalsCanGoUpOrDownFromMarkLevel and AIPlanetDefenseMultiplier: " ).Append( planet.MarkLevelForAIOnly.AIPlanetDefenseMultiplier.ToFloatNonSim().ToString() )
                        .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
            }

            ///The "general quantities" stuff is probably just strikecraft, but could later be something else
            ///These are things that are a bit more sensitive to numbers shifting up and down.
            if ( totalsCanGoUpOrDownFromPlanetType_GeneralQuantities )
            {
                switch ( planet.PopulationType )
                {
                    case PlanetPopulationType.AIHomeworld:
                        //if ( IsForInitialSeeding )
                        //{
                        //    //AI homeworlds are beasts that get twice what anything else does.  Yow.
                        //    //But this ONLY applies on the initial world seeding, not the rest of the time.
                        //    //We don't want this logic long-term, 
                        //    result *= 2;
                        //    if ( WriteDetailsToLog )
                        //        builder.Append( "AIHomeworld IsForInitialSeeding multiplier of: " ).Append( 2 )
                        //            .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                        //}
                        break;
                    case PlanetPopulationType.AIBastionWorld:
                        break;
                    case PlanetPopulationType.HumanHomeworld:
                        break;
                }
            }

            FInt numberOfTenMinuteIncrements = (FInt)World_AIW2.Instance.GameSecond / (FInt)600;

            //these PopulationType-based items need to happen ALL the time.
            {
                switch ( planet.PopulationType )
                {
                    case PlanetPopulationType.AIHomeworld:
                        FInt numberOfTenMinuteIncrementsBeforeFullyDefensible = (FInt)factionExternal.AIDifficulty.NumberOfTenMinuteIncrementsBeforeHomeworldIsFullyDefensible;
                        //AI homeworlds have a constrained cap based on time when not on the initial seeding
                        if ( numberOfTenMinuteIncrements < numberOfTenMinuteIncrementsBeforeFullyDefensible && !IsForInitialSeeding )
                        {
                            //first look at the total percentage into the first 8 hours
                            FInt percentage = numberOfTenMinuteIncrements / numberOfTenMinuteIncrementsBeforeFullyDefensible;
                            //make the percentage now be half that
                            percentage /= 2;
                            //now add in that other 50%, keep it at that at least
                            percentage += FInt.FromParts( 0, 500 );
                            //limit the homeworld down some
                            if ( percentage < FInt.One )
                                result *= percentage;

                            if ( WriteDetailsToLog )
                                builder.Append( "AIHomeworld numberOfTenMinuteIncrements: " ).Append( numberOfTenMinuteIncrements.ToFloatNonSim().ToString() )
                                    .Append( " and numberOfTenMinuteIncrementsBeforeFullyDefensible: " ).Append( numberOfTenMinuteIncrementsBeforeFullyDefensible.ToFloatNonSim().ToString() )
                                    .Append( " and percentage: " ).Append( percentage.ToFloatNonSim().ToString() )
                                    .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                        }
                        break;
                    case PlanetPopulationType.AIBastionWorld:
                        //stay the same for now.
                        break;
                    case PlanetPopulationType.HumanHomeworld:
                        result *= FInt.FromParts( 0, 667 ); //things that were once a human homeworlds are forever hobbled at 2/3rds normal strength.  Only relevant in multiplayer, really.
                        if ( WriteDetailsToLog )
                            builder.Append( "HumanHomeworld multiplier of: " ).Append( "0.667" )
                                .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                        break;
                }
            }

            if ( planet.SentinelsAlertLevel != null )
            {
                result *= planet.SentinelsAlertLevel.ReinforcementCapMultiplier;
                if ( WriteDetailsToLog )
                    builder.Append( "SentinelsAlertLevel.ReinforcementCapMultiplier: " ).Append( planet.SentinelsAlertLevel.ReinforcementCapMultiplier.ToFloatNonSim().ToString() )
                        .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
            }

            //stuff that is super close to the player homeworld gets an additional penalty,
            //though there may be little point to this since the mark 1 and 2 planets are inherently
            //only the things ever that close.  I guess on the off chance there is a mark 4 planet really nearby,
            //which is something we might entertain at some point (the original game had it), we'd have it be very weak.
            switch ( planet.OriginalHopsToHumanHomeworld )
            {
                case 1:
                    result *= FInt.FromParts( 0, 500 );
                    if ( WriteDetailsToLog )
                        builder.Append( "OriginalHopsToHumanHomeworld=1 multiplier of: " ).Append( "0.5" )
                            .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                    break;
                case 2:
                    result *= FInt.FromParts( 0, 750 );
                    if ( WriteDetailsToLog )
                        builder.Append( "OriginalHopsToHumanHomeworld=2 multiplier of: " ).Append( "0.75" )
                            .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                    break;
            }

            //the result is multiplied by the percentage of reinforcement points remaining compared to the max seen here.
            //neutering a planet thus REALLY matters, in the sense of taking away huge amounts
            //of capacity that the AI can actually use to defend itself there.
            //This applies to everything, not just strikecraft anymore.
            if ( numReinforcementPoints < planet.MaxReinforcementPlacesEverSeenHere )
            {
                if ( numReinforcementPoints <= 0 )
                {
                    if ( WriteDetailsToLog )
                    {
                        builder.AppendLine( "numReinforcementPoints <= 0, so no budget." );
                        ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
                    }
                    return FInt.Zero;
                }
                if ( numReinforcementPoints == 1 )
                {
                    result *= FInt.FromParts( 0, 050 );
                    if ( WriteDetailsToLog )
                        builder.Append( "1 numReinforcementPoints multiplier of: " ).Append( "0.05" )
                            .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                }
                else
                {
                    FInt reinforcementPercentage = (FInt)numReinforcementPoints / (FInt)planet.MaxReinforcementPlacesEverSeenHere;
                    if ( reinforcementPercentage < FInt.One )
                    {
                        //multiply it by itself.  So 0.8 becomes 0.64, it's an exponential falloff.
                        reinforcementPercentage *= reinforcementPercentage;
                        if ( reinforcementPercentage < FInt.FromParts( 0, 250 ) )
                            reinforcementPercentage = FInt.FromParts( 0, 050 ); //but don't let it go below 0.05, or 5%, as the multiplier
                        result *= reinforcementPercentage;

                        if ( WriteDetailsToLog )
                            builder.Append( numReinforcementPoints + " numReinforcementPoints multiplier of: " ).Append( reinforcementPercentage.ToFloatNonSim().ToString() )
                                .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                    }
                }
            }

            /* Handle allowed reinforcement increases based on AIP 
             This is defensive_cap_increase_per_aip, and is EXTREMELY small.
             Otherwise it can get rather punitive.
             It's a multiplier to everything that came before.
             */
            
            FInt AIP = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective - 100;
            if ( AIP > 0 )
            {
                FInt bonusMultiplierPerAIP = factionExternal.AIDifficulty.DefensiveCapIncreasePerAIP;
                FInt amountToAdd = ( result / 10 ) * AIP * bonusMultiplierPerAIP;
                result += amountToAdd;

                if ( WriteDetailsToLog )
                    builder.Append( "AIP: " ).Append( AIP.ToFloatNonSim().ToString() )
                        .Append( " and bonusMultiplierPerAIP: " ).Append( bonusMultiplierPerAIP.ToFloatNonSim().ToString() )
                        .Append( " and added: " ).Append( amountToAdd.ToFloatNonSim().ToString() )
                        .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
            }

            /* This is defensive_cap_increase_per_ten_minutes, and it's incredibly small.
             * It gives some time pressure in the sense of giving the AI a tiny bit more percentage cap
             * every ten minutes, but it's absolutely tiny.
             * On difficuly 10, after 10 hours, that's 60 10-minute increments, or 0.008*10, 
             * aka an 48% increase in power compared to what it was at game start.  This is TAME for that level of difficulty, but creeps up very slowly.
             * On difficulty 7, that's 0.002*60, aka a 12%.  That's so incredibly tame.
             * */
            {
                FInt bonusMultiplierPerTenMinutes = factionExternal.AIDifficulty.DefensiveCapIncreasePerTenMinutes;
                FInt amountToAdd = ( result / 10 ) * numberOfTenMinuteIncrements * bonusMultiplierPerTenMinutes;
                result += amountToAdd;

                if ( WriteDetailsToLog )
                    builder.Append( "bonusMultiplierPerTenMinutes: " ).Append( bonusMultiplierPerTenMinutes.ToFloatNonSim().ToString() )
                        .Append( " and numberOfTenMinuteIncrements: " ).Append( numberOfTenMinuteIncrements.ToFloatNonSim().ToString() )
                        .Append( " and added: " ).Append( amountToAdd.ToFloatNonSim().ToString() )
                        .Append( " so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
            }

            switch ( reinforcementType )
            {
                case ReinforcementType.Turret:
                case ReinforcementType.NonTurretDefense:
                    switch ( planet.PlanetAITurretSeedingCap )
                    {
                        case PlanetAITurretAnyNonTurretDefensesCap.NoneAllowed:
                            {
                                if ( WriteDetailsToLog )
                                {
                                    builder.AppendLine( "PlanetAITurretAnyNonTurretDefensesCap.NoneAllowed, so no budget (hey, late!)" );
                                    ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
                                }
                                return FInt.Zero;
                            }
                        case PlanetAITurretAnyNonTurretDefensesCap.HalfCap:
                            result /= 2;
                            if ( WriteDetailsToLog )
                                builder.Append( "PlanetAITurretAnyNonTurretDefensesCap.HalfCap so running result: " ).AppendLine( result.ToFloatNonSim().ToString() );
                            break;
                        case PlanetAITurretAnyNonTurretDefensesCap.FullCap:
                            break; //leave it alone
                    }
                    break;
            }
            if ( WriteDetailsToLog )
            {
                builder.AppendLine( "ending result: " + result.ToFloatNonSim().ToString() );
                ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
            }
            return result;
        }

        public static FInt GetAICostPurchaseCapForBudgetType( Planet planet, Faction faction, ReinforcementType reinforcementType, bool IsForInitialSeeding, bool WriteDetailsToLog,
            //this is optional so that if we already have it, we can just pass it in and not have to keep getting it
            AISentinelsCoreData factionExternal = null )
        {
            return GetGuardingUnitAICostPurchaseCap( faction, reinforcementType, planet, IsForInitialSeeding, WriteDetailsToLog, factionExternal );
        }

        public static int GetAIToPurchaseCostPresentForBudgetType( Planet planet, Faction faction, ReinforcementType reinforcementType )
        {
            int totalAIPurchaseCost = 0;
            int debugStage = 0;
            try
            {
                debugStage = 10;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                debugStage = 20;
                switch ( reinforcementType )
                {
                    case ReinforcementType.Guardian:
                        debugStage = 1000;
                        //normal stuff wandering around
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads( EntityRollupType.ForReinforcementType_Guardian ) )
                        {
                            debugStage = 1100;
                            totalAIPurchaseCost += squad.TypeData.CostForAIToPurchase * (squad.ExtraStackedSquadsInThis + 1);
                        }
                        debugStage = 1400;
                        //stuff inside reinforcement points
                        foreach ( GameEntity_Squad reinforcementPoint in pFaction.Entities.Squads( EntityRollupType.ReinforcementLocations ) )
                        {
                            debugStage = 1500;
                            if ( reinforcementPoint.AIReinforcementPointContents != null )
                            {
                                debugStage = 1600;
                                RefPair<GameEntityTypeData, int> pair;
                                for ( int i = 0; i < reinforcementPoint.AIReinforcementPointContents.Count; i++ )
                                {
                                    debugStage = 1700;
                                    pair = reinforcementPoint.AIReinforcementPointContents[i];
                                    debugStage = 1800;
                                    if ( pair.LeftItem.IsGuardian )
                                    {
                                        debugStage = 1900;
                                        totalAIPurchaseCost += (pair.LeftItem.CostForAIToPurchase * pair.RightItem);
                                    }
                                }
                            }
                        }
                        debugStage = 2100;
                        break;
                    case ReinforcementType.Strikecraft:
                        debugStage = 4100;
                        //normal stuff
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads( EntityRollupType.ForReinforcementType_Strikecraft ) )
                        {
                            debugStage = 4200;
                            totalAIPurchaseCost += squad.TypeData.CostForAIToPurchase * (squad.ExtraStackedSquadsInThis + 1);
                        }

                        debugStage = 4300;
                        //stuff inside reinforcement points
                        foreach ( GameEntity_Squad reinforcementPoint in pFaction.Entities.Squads( EntityRollupType.ReinforcementLocations ) )
                        {
                            debugStage = 4400;
                            if ( reinforcementPoint.AIReinforcementPointContents != null )
                            {
                                debugStage = 4500;
                                RefPair<GameEntityTypeData, int> pair;
                                for ( int i = 0; i < reinforcementPoint.AIReinforcementPointContents.Count; i++ )
                                {
                                    debugStage = 4600;
                                    pair = reinforcementPoint.AIReinforcementPointContents[i];
                                    debugStage = 4700;
                                    if ( pair.LeftItem.IsStrikecraft || pair.LeftItem.SpecialType == SpecialEntityType.Frigate )
                                    {
                                        debugStage = 4800;
                                        totalAIPurchaseCost += (pair.LeftItem.CostForAIToPurchase * pair.RightItem);
                                    }
                                }
                            }
                        }
                        debugStage = 6100;
                        break;
                    case ReinforcementType.Turret:
                        debugStage = 7100;
                        //normal stuff; these could not possibly be inside a reinforcement point
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads( EntityRollupType.ForReinforcementType_Turret ) )
                        {
                            debugStage = 7200;
                            totalAIPurchaseCost += squad.TypeData.CostForAIToPurchase * (squad.ExtraStackedSquadsInThis + 1);
                        }
                        debugStage = 8100;
                        break;
                    case ReinforcementType.NonTurretDefense:
                        debugStage = 9100;
                        //normal stuff; these could not possibly be inside a reinforcement point
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads( EntityRollupType.ForReinforcementType_NonTurretDefense ) )
                        {
                            debugStage = 9200;
                            totalAIPurchaseCost += squad.TypeData.CostForAIToPurchase * (squad.ExtraStackedSquadsInThis + 1);
                        }
                        debugStage = 9300;
                        break;
                    default:
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in GetAIToPurchaseCostPresentForBudgetType, debugStage " + debugStage + ".  Exception: " + e, Verbosity.ShowAsError );
            }
            return totalAIPurchaseCost;
        }
    }
}
