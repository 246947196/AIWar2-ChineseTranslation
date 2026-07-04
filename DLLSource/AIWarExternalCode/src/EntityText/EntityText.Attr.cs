
using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.AIW2.External
{
    #region EntityAttrText
    public struct EntityAttrText
    {
        public enum AttributeType
        {
            None = 0,
            
            UnsortedStuff,

            Length,
            First = 1,
            Last = Length-1,
        }
        
        public readonly AttributeType Type;
        public readonly GameEntity_Squad Squad;
        public readonly int SortOrder;
        public EntityText.Config Config;

        public EntityAttrText(AttributeType type, GameEntity_Squad squad, EntityText.Config config)
        {
            Type = type;
            Squad = squad;
            Config = config;
            SortOrder = (int)type;
        }

        public void Write(EntityTextWriter writer)
        {
            // todo: purge this?
            var buffer = writer.Buffer;
            FleetMembership relatedMembershipOrNull = Config.OptMembership;
            Fleet relatedMemFleetOrNull = Config.OptFleet;
            Faction localPlayerFactionOrNull = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            Faction relatedSquadFactionOrNull = Squad.GetFactionOrNull_Safe();
            Planet thisPlanetOrNull = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            PlanetFaction localPlayerPlanetFactionOrNull = thisPlanetOrNull?.GetPlanetFactionForFaction( localPlayerFactionOrNull );
            
            var orders = Squad.Orders;
            var order_first = orders.GetQueuedOrderAtIndex_OrNull( 0 );
            EntityOrder order_last = orders.GetLastQueuedOrder_OrNull();
                    
            var Attr_Line = TextStyle.Attr_Line;
            var Attr_Label = TextStyle.Attr_Label;
            var Attr_Line2 = TextStyle.Attr_Line2;
            var Attr_Label2 = TextStyle.Attr_Label2;
            var Attr_Pad = TextStyle.Attr_Pad;
            
            int debugstage = 0;
            try
            {
                #region DataExt_Mid
                {
                    debugstage = 30510;
                    buffer.BeginStatement(TextStyle.Attr_Line);
                    Squad.TypeData.ForAnyDataExtensions_AddToTooltip_MidSection_ForEntity( buffer, Squad, relatedMembershipOrNull, Squad.TypeData, Config.Detail );
                    buffer.EndStatement(TextStyle.Attr_Line);
                }
                #endregion

                #region Fireteam
                debugstage = 30530;
                if ( !Squad.IsFakeEntity && 
                     Squad.FireteamId > 0 &&
                     (Config.ShowFireteamHistory ||
                      (Squad.GetIsFriendlyToLocalFaction_Safe() && Config.Detail >= TooltipDetail.Full)))
                {
                    buffer.BeginStatement(TextStyle.Newline_NoLabel);
                    
                    debugstage = 30532;
                    Fireteam team = null;
                    ExternalFactionBaseInfo baseInfo = Squad.GetFactionBaseInfoOrNull_Safe();
                    if ( baseInfo != null )
                        team = (Fireteam) baseInfo.GetFireteamBaseById( Squad.FireteamId );

                    if ( team == null )
                        buffer.Add( "<color=#d57aff>Null fireteam despite fireteam ID " + Squad.FireteamId + "?</color>  " );
                    else
                    {
                        //team can be null if we are racing with the long range planning code that clears/rebuilds the team list
                        debugstage = 30533;
                        buffer.Add( "Fireteam " ).Add( team.FireTeamID, "10ffdd" ).Add( " is " );
                        team.GetStatusForDisplay( buffer );
                        buffer.Add( ". " );
                        if ( team.Target != null && Config.ShowDebugInfo )
                            buffer.Add( "Target is " + team.Target.ToStringWithPlanetAndOwner() ).Add( "\n" );
                        team.GetSpecificationForDisplay( buffer );
                        if ( team != null && team.History != null && team.History.Count > 0 && team.FireTeamID > 0 )
                        {
                            buffer.Add( "Unit Fireteam History:\n " );
                            for ( int i = team.History.Count - 1; i >= 0; i-- )
                            {
                                buffer.Add( "\t" + team.History[i].ToDisplayString( team ) + "\n" );
                            }
                        }
                    }
                    
                    buffer.EndStatement(TextStyle.Newline_NoLabel);
                }
                #endregion

                #region CrippledInsteadOfDying, RepairImpossibleForSeconds
                if ( Config.Detail > TooltipDetail.Medium )
                {
                    if ( Squad.TypeData.ShipClass.CanBeDamaged &&
                         Squad.IsFakeEntity ||
                         Squad.GetFactionTypeSafe() == FactionType.Player ||
                         Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                    {
                        int time = Squad.TypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15;
                        if (time <= 0)
                        { 
                            // don't bother
                            time = ExternalConstants.Instance.Balance_RepairImpossibleForSecondsAfterDamagedByEnemy;
                        }
                        else
                        {
                            buffer
                                .BeginStatement(TextStyle.Newline_NoLabel)
                                .Add( "Delay before regen or repair is " )
                                .AddMinutesAndSeconds( time )
                                .Add( " after taking damage." )
                                .EndStatement(TextStyle.Newline_NoLabel);
                        }
                    }
                }
                #endregion
                
                #region TransformInto
                if (Squad.SecondsTillTransformation > 0)
                {
                    buffer.BeginStatement(TextStyle.Newline_NoLabel);
                        
                    if ( Squad.TransformsIntoAfterTime == "$Dies" ||
                         Squad.TransformsIntoAfterTime == "$Dies_Paused" )
                    {
                        if ( Squad.TypeData.TransformationCountdownOnlyDuringCombat )
                            buffer.Add( "This will expire after " )
                                .AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" ).Add( " in combat." );
                        else
                            buffer.Add( "This will expire in " ).AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" )
                                .Add( "." );
                    }
                    else
                    {
                        var intoType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( Squad.TransformsIntoAfterTime );
                        if ( intoType == null )
                        {
                            buffer.Add( "Could not find a " + Squad.TransformsIntoAfterTime +
                                        " in XML. This is a BUG. Please report it. " );
                        }
                        else
                        {
                            if ( Squad.TypeData.TransformationCountdownOnlyDuringCombat )
                            {
                                buffer
                                    .Add( "This will transform into a " )
                                    .Add( intoType.GetDisplayName(), "a1ffa1" )
                                    .Add( " after " )
                                    .AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" )
                                    .Add( " in combat." );
                            }
                            else
                            {
                                buffer
                                    .Add( "This " )
                                    .Add( intoType.GetDisplayName(), "a1ffa1" )
                                    .Add( " is warping in and will be fully created in " )
                                    .AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" )
                                    .Add( "." );
                            }
                        }
                    }

                    buffer.EndStatement(TextStyle.Newline_NoLabel);
                }
                #endregion
                
                #region Assistance Items
                writer.WriteSupportMetalFlows( buffer );
                #endregion
                
                #region Resource Generation
                debugstage = 360;
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    bool handledMetalStorage = false;
                    FInt resourceProductionPreBonuses;
                    for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                    {
                        resourceProductionPreBonuses = Squad.DataForMark.GetResourceProductionBeforeAnyBonuses( resource );
                        if ( resourceProductionPreBonuses == FInt.Zero )
                            continue;

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                if ( !Config.ShowMetalOther )
                                    continue;
                                break;
                            case ResourceType.Energy:
                                if ( !Config.ShowEnergyProduction )
                                    continue;
                                break;
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                if ( !World_AIW2.Instance.IsFuelEnabled || !Config.ShowAnyFuel )
                                    continue;
                                break;
                        }

                        if ( Config.Detail < TooltipDetail.Full && 
                             (resource == ResourceType.Science || 
                              resource == ResourceType.Hacking) )
                        {
                            //science and hacking only shown in verbose mode
                            continue;  
                        }

                        FInt productionFinal = resourceProductionPreBonuses;
                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                {
                                    if ( !Squad.IsFakeEntity )
                                        productionFinal = Squad.GetFullyMultipliedMetalToProduce();
                                    else 
                                    if ( Squad.Planet != null )
                                        productionFinal = GameEntity_Squad.DoMultiplierOfMetalAtPlanet( Squad.TypeData, productionFinal, Squad.Planet );
                                    break;
                                }
                            case ResourceType.Energy:
                                {
                                    if ( !Squad.IsFakeEntity )
                                        productionFinal = Squad.GetFullyMultipliedEnergyToProduce();
                                    else 
                                    if ( Squad.Planet != null )
                                        productionFinal = GameEntity_Squad.DoMultiplierOfEnergyAtPlanet( Squad.TypeData, productionFinal, Squad.Planet );
                                    break;
                                }
                        }
                        
                        buffer.NewLineIfNeeded();
                        
                        switch ( resource )
                        {
                            case ResourceType.Metal:
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                buffer.Add( "Produces " );
                                break;
                            case ResourceType.Energy:
                                buffer.Add( "Generates " );
                                break;
                            case ResourceType.Hacking:
                            case ResourceType.Science:
                                buffer.Add( "Gathers " );
                                break;
                        }

                        string suffix = null;
                        if (resource == ResourceType.Metal || 
                            resource == ResourceType.Hacking || 
                            resource == ResourceType.Science)
                        {
                            suffix = "/s";
                        }
                            
                        var term = resource.Term();
                        
                        buffer
                            .Open(term, TermUse.Icon)
                            .AddNumber(productionFinal, suffix, TextStyle.Empty)
                            .Close(term);
                        
                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                if ( Squad.DataForMark.MetalStorage > 0 )
                                {
                                    handledMetalStorage = true;
                                    buffer.Add( " and stores " ).Open(term, TermUse.Icon).AddNumber(Squad.DataForMark.MetalStorage, null, TextStyle.Empty).Close(term).Add(".");
                                }
                                break;
                            case ResourceType.Hacking:
                                buffer.Add( " when on a planet with remaining hacking points." );
                                break;
                            case ResourceType.Science:
                                buffer.Add( " when on a planet with remaining science." );
                                break;
                            case ResourceType.FuelArgon:
                                buffer.Add( " (for main combat ships)." );
                                break;
                            case ResourceType.FuelRadon:
                                buffer.Add( " (for turrets and forcefields)." );
                                break;
                            case ResourceType.FuelXenon:
                                buffer.Add( " (for officers and elites)." );
                                break;
                        }
                    }
                    
                    if ( !handledMetalStorage )
                    {
                        if ( Squad.DataForMark.MetalStorage > 0 )
                        {
                            handledMetalStorage = true;
                            buffer.Add( "Stores " ).Open(TextTerm.Metal, TermUse.Icon).AddNumber(Squad.DataForMark.MetalStorage, null, TextStyle.Empty).Close(TextTerm.Metal).Add(".");
                        }
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Resource Multiplier - Distributed
                
                debugstage = 370;

                    if ( World_AIW2.Instance.PlayersAreInDistributedResourceGenerationMode )
                    {
                        var amt_metal = Squad.TypeData.MetalProductionMultiplierToAsteroids_DistributedModeOnly;
                        var amt_energy = Squad.TypeData.EnergyProductionMultiplierToAsteroids_DistributedModeOnly;
                        
                        bool showMetal = Config.ShowMetalOther && amt_metal > FInt.Zero && amt_metal != FInt.One;
                        bool showEnergy = Config.ShowEnergyProduction && amt_energy > FInt.Zero && amt_energy != FInt.One;
                        
                        if ( showMetal || showEnergy )
                        {
                            var mine_type = GameEntityTypeDataTable.Instance.GetRowByName("MineAndPowerplanet");
                            buffer
                                .Open(TextStyle.Newline_NoLabel)
                                .Add("Changes planetary ")
                                .AddShipIconNameShort(mine_type).Add(" production by ");
    
                            if (showMetal)
                                buffer.Open(TextTerm.Metal, TermUse.Icon).AddMultiplier( amt_metal, TextStyle.Empty ).Close(TextTerm.Metal).Add(" ");
                            if (showEnergy)
                                buffer.Open(TextTerm.Energy, TermUse.Icon).AddMultiplier( amt_energy, TextStyle.Empty ).Close(TextTerm.Energy).Add(" ");
                            
                            buffer.Close(TextStyle.Newline_NoLabel);
                        }
                    }
                    
                    #endregion

                #region Resource Multipliers
                {
                    for ( ResourceType res_type = ResourceType.None + 1; res_type < ResourceType.Length; res_type++ )
                    {
                        var res_boost = Squad.DataForMark.GetResourceProductionMultiplier( res_type );
                        if ( res_boost == FInt.Zero )
                            continue;
                        
                        switch ( res_type )
                        {
                            case ResourceType.Metal:
                                if ( !Config.ShowMetalOther )
                                    continue;
                                break;
                            case ResourceType.Energy:
                                if ( !Config.ShowEnergyProduction )
                                    continue;
                                break;
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                if ( !World_AIW2.Instance.IsFuelEnabled || !Config.ShowAnyFuel )
                                    continue;
                                break;
                        }

                        debugstage = 372;

                        
                        
                        buffer.Open(TextStyle.Newline_NoLabel)
                            .Add( "Boosts ").Add(res_type.Term(), TermUse.Icon).Add(" production by ").Open(TextStyle.Number).AddMultiplier( res_boost ).Close(TextStyle.Number).Add( " for its planet." )
                            .Close(TextStyle.Newline_NoLabel);
                    }
                }
                #endregion
                
                #region Resource Multipliers After Time Being Here And Not Crippled

                debugstage = 375;
                var boost_delay = Squad.TypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction;
                if ( boost_delay > 0 )
                {
                    var amt_boost_metal = Squad.TypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime;
                    var amt_boost_energy = Squad.TypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime;
                    var can_boost_metal = amt_boost_metal > FInt.One;
                    var can_boost_energy = amt_boost_energy > FInt.One;
                    
                    const int status_hidden = 0;
                    const int status_not_friend_planet = 1;
                    const int status_delay = 2;
                    const int status_active = 3;
                    
                    if ( can_boost_metal || can_boost_energy )
                    {
                        buffer.BeginStatement(TextStyle.Attr_Line);
                        
                        buffer.Add( "Resource-Booster", TextStyle.Attr_Label).Add(": ");
                        
                        int timeHasBeenHereAndNotCrippled = Squad.GetSecondsSinceEnteringThisPlanetOrLastCrippled();
                        
                        int show_status = status_hidden;
                        bool boost_metal_on_and_applied = false;
                        bool boost_energy_on_and_applied = false;
                        
                        if (!Squad.IsFakeEntity &&
                            (relatedSquadFactionOrNull.Type != FactionType.NaturalObject))
                        {
                            var planetController = Squad.Planet.GetControllingFaction();
                            if (!Squad.GetIsFriendlyTowardsSafe(planetController))
                            {
                                show_status = status_not_friend_planet;
                            }
                            else
                            if (timeHasBeenHereAndNotCrippled < boost_delay)
                            {
                                show_status = status_delay;
                            }
                            else
                            {
                                show_status = status_active;
                                
                                boost_metal_on_and_applied = !Squad.NonSim_PlanetaryMetalBoostFailedFromOthersBeingPresent;
                                boost_energy_on_and_applied = !Squad.NonSim_PlanetaryEnergyBoostFailedFromOthersBeingPresent;
                            }
                        }

                        string AsText(bool on_and_applied)
                        {
                            if (on_and_applied)
                                return "ON";
                            
                            return "NA";
                        }
                        
                        TextStyle AsStyle(bool on_and_applied)
                        {
                            if (on_and_applied)
                                return TextStyle.Color_Active;
                            
                            return TextStyle.Color_DoesNotApply;
                        }
                        
                        //if (show_status > status_hidden)
                        //{
                        //    buffer.AddVarReplace(TextVarMap.Bracket, (a,b,c,d)=>c.Add(AsText(status_hidden), AsStyle(status_hidden)));
                        //}
                        
                        if (can_boost_metal)
                        {
                            buffer
                                .Open(TextTerm.Metal, TermUse.Icon)
                                .Add("+").AddNumber(((amt_boost_metal-1)*100), null, TextStyle.Empty ).Add("%")
                                .Close(TextTerm.Metal);
                            
                            if (show_status == status_active)
                            {
                            buffer.AddVarReplaceParams(TextVarMap.InParenthesis, 
                                    null, 
                                    (a,b,c,d)=>c.Add(AsText(boost_metal_on_and_applied),AsStyle(boost_metal_on_and_applied)), 
                                    null);
                            }
                        }
                        
                        if (can_boost_energy)
                        {
                            if (can_boost_metal)
                                buffer.Add(" ");
                            
                            buffer
                                .Open(TextTerm.Energy, TermUse.Icon)
                                .Add("+").AddNumber(((amt_boost_energy-1)*100), null, TextStyle.Empty ).Add("%")
                                .Close(TextTerm.Energy);
                            
                            if (show_status == status_active)
                            {
                            buffer.AddVarReplaceParams(TextVarMap.InParenthesis,
                                    null, 
                                    (a,b,c,d)=>c.Add(AsText(boost_energy_on_and_applied), AsStyle(boost_energy_on_and_applied)), 
                                    null);
                            }
                        }
                        
                    buffer.Add(" if ").AddTermRange(TermRange.Alloc(TextTerm.TimeOnPlanet, TermCmp.Greater, boost_delay));
                        
                        if (show_status > status_hidden)
                        {
                            if (show_status == status_delay)
                            {
                                var rem = boost_delay-timeHasBeenHereAndNotCrippled;
                                if (rem > 0)
                                {
                                    buffer.Add(" ");
                                buffer.AddVarReplaceParams(
                                        TextVarMap.InParenthesis, 
                                    (x,y,b,z) => b.AddMinutesAndSeconds(rem, TextStyle.Color_DoesNotApply.Color));
                                    
                                    //buffer.Add(" ").AddMinutesAndSeconds(rem, TextStyle.Color_DoesNotApply.Color);
                                }
                            }
                            else
                            if (show_status == status_not_friend_planet)
                            {
                                buffer.Add(" ");
                                buffer.AddVarReplace(
                                    TextVarMap.InParenthesis, 
                                (x,y,b,z) => b.Add("Planet Not Friendly", TextStyle.Color_DoesNotApply.Color));
                                //buffer.Add(" Planet Not Friendly", TextStyle.Color_DoesNotApply);
                            }
                            /*
                            else
                            if (show_status == status_active)
                            {
                                buffer.Add(" ");
                                buffer.AddVarReplace(
                                    TextVarMap.Bracket, 
                                    (x,y,b,z)
                                        =>
                                        {
                                            b.Add("ON", TextStyle.Color_Active);
                                        } );
                            }
                            */
                        }
                        
                        buffer.Add(".");
                        
                        buffer.EndStatement(TextStyle.Attr_Line);
                    }
                }
                #endregion

                #region Regenerator
                debugstage = 390;
                if ( Squad.TypeData.RegeneratesDyingShipsAtThisHealthCostRatio > FInt.Zero )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    int avail = Squad.GetHullRegenPointsAvailable();
                    int max = Squad.GetHullRegenPointsMax();
                    
                    buffer.Add( "Regenerator", Attr_Label).Add(": ");
                    
                    buffer.StartHull(true).AddNumberTruncated( avail );
                    if (avail < max)
                        buffer.Open(TextStyle.Fraction).Add( "/" ).AddNumberTruncated( max ).Close(TextStyle.Fraction);
                    buffer.EndHullWrapper(true);
                    
                    if (!Squad.IsFakeEntity)
                    {
                        buffer.Add(" points available for respawning dying allies.");
                    }
                    else
                    {
                        buffer.Add(" points for respawning dying allies.");
                    }

                    if (Config.Detail > TooltipDetail.Medium)
                    {
                        buffer.Open(Attr_Line2);
                        var ratio = Squad.TypeData.RegeneratesDyingShipsAtThisHealthCostRatio.ToRounded(2).ToFloatNonSim();
                        buffer.InCase(TextCaps.FirstLetter).Add("Ratio", Attr_Label2).EndCase().Add(": ").Add("The cost to regen 1 hull is ").AddFormat(ratio, "{0:#,##0.##}").Add(".");
                        buffer.Close(Attr_Line2);
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region ShipClass, Normal Order (disabled, todo)
                //debugstage = 395;
                //if ( !Squad.TypeData.ShipClass.IsDefault && 
                //     !Squad.TypeData.ShipClass.WriteDescriptionSoonerInTooltip )
                //{
                //    var facType = relatedSquadFactionOrNull?.Type ?? FactionType.Player;
                //    Window_PrototypeInGameHoverEntityInfoUtils.WriteShipClass_Complete( buffer, Squad.TypeData.ShipClass, Config.Detail, facType, Squad.DataForMark.Speed );
                //}
                //#endregion

                // jcf: this shows up on a lot of stuff, and its mostly things that you wouldn't even expect to be capturable
                //      plus the term 'captured by other factions' is really ambiguous when you see it on a 'capturable' ..
                
                
                #endregion

                #region !CanBeFleetSupercharged
                /*
                debugstage = 400;
                if ( !Squad.TypeData.ShipClass.CanBeFleetSupercharged && 
                     !AIWar2GalaxySettingQuickAccess.DisableFleetWideBonuses )
                {
                    buffer.Add( "Cannot be supercharged.", TextStyle.Newline_NoLabel );
                }
                */
                #endregion

                #region Rapid Deployment
                if ( Squad.TypeData.SpeedMultiplierFirst5SecondsOnPlanet > FInt.One )
                {
                var bonus = Squad.TypeData.SpeedMultiplierFirst5SecondsOnPlanet;
                    buffer.BeginStatement(Attr_Line);
                buffer
                    .Add( "Rapid-Deployment", Attr_Label)
                    .Add(": Bonus ").AddNumber(()=>{ buffer.AddMultiplier(bonus, TextStyle.Empty); }, null, TextTerm.Speed, TermUse.Icon, null)
                    .Add(" while ").AddTermRange(TermRange.Alloc(TextTerm.TimeOnPlanet, TermCmp.Less, 5));
                
                if (Config.Detail >= TooltipDetail.Full)
                    buffer.Add(" (time loaded counts).");
                
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Von Neumann
                if ( Squad.TypeData.BuildPointsPerDamageDealt > FInt.Zero && 
                     Squad.TypeData.UnitToMakeWithBuildPoints_TypeData != null )
                {
                    var type_made = Squad.TypeData.UnitToMakeWithBuildPoints_TypeData;
                    var type_for_cap = Squad.TypeData.BaseShipLineForBuildPoints_TypeData;
                    var cost_per = type_made.MarkStatsFor( Squad.CurrentMarkLevel ).MetalCost;
                    
                    float min = 0.0f;
                    float max = 0.0f;
                    {
                        FInt _min, _max;
                        Squad.GetDps(out _min, out _max);
                        min = _min.ToFloat();
                        max = _max.ToFloat();
                    }
                    
                    var temp = Squad.TypeData.BuildPointsPerDamageDealt.ToFloat();
                    min = temp * min;
                    max = temp * max;

                    min /= cost_per;
                    max /= cost_per;
                    
                    bool show_seconds_per = false;
                    if ( min < 1 )
                    {
                        show_seconds_per = true;
                        min = 1 / min;
                        max = 1 / max;
                        min = (float)Math.Round(min, 0);
                        max = (float)Math.Round(max, 0);
                    }
                    else
                    {
                        min = (float)Math.Round(min, 1);
                        max = (float)Math.Round(max, 1);
                    }
                    
                    bool show_range = false;
                    var dif = Mathf.Abs(min-max);
                    if (dif > 0.1f)
                        show_range = true;
                    
                    int cap = 0;
                    if (Squad.IsFakeEntity)
                    {
                        cap = type_for_cap.LineCap(Squad.TypeData);
                    }
                    else
                    if ( Squad.GetFactionTypeSafe() == FactionType.Player && 
                         Squad.GetFleetOrNull_Safe() != null)
                    {
                        var fleet = Squad.FleetMembership?.Fleet;
                        var mem = fleet?.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates(type_for_cap);
                        if (mem != null)
                            cap = mem.EffectiveSquadCap;
                    }

                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Replicator", Attr_Label).Add(": ");
                    if (cap == 0)
                    {
                        buffer.Add("Builds more ").WriteSpawn(type_made).Add(" as it deals damage, up to the cap for ").WriteSpawn(type_for_cap).Add(" in the fleet ");
                    }
                    else
                    {
                        buffer
                            .Add("Builds up to ")
                            .AddNumber(cap, Text.Count, TextStyle.Color_Count, null)
                            .WriteSpawn(type_made)
                            .Add(" as it deals damage ");
                    }
                    
                    buffer.AddVarReplace(TextVarMap.InParenthesis, null, 
                            (a,b,c,d)=>
                                {
                                    c.Open(TextStyle.Number);
                                    c.StartSize("75%");
                                    
                                    if (show_seconds_per)
                                    {
                                        // (1= 5~15s)
                                        
                                        c.Add("1= ");
                                        
                                        c.Open(TextStyle.MinutesAndSeconds);
                                        c.Add(min);
                                        if (show_range)
                                            c.Add("~").Add(max);
                                        c.Add("s");
                                        c.Close(TextStyle.MinutesAndSeconds);
                                    }
                                    else
                                    {
                                        // (1~10/sec)
                                        // (1s= 1~10x)
                                        //c.Add("1s= 1~10x");
                                        
                                        c.Add(min);
                                        if (show_range)
                                            c.Add("~").Add(max);
                                        
                                        //c.Open(TextStyle.Fraction_Gray);
                                        c.AddColor("/sec",Color.gray.GetHexCode());//TextStyle.Fraction_Gray);
                                        //c.Close(TextStyle.Fraction_Gray);
                                    }
                                    
                                    c.EndSize();
                                    c.Close(TextStyle.Number);
                                }, 
                            null);
                    buffer.Add(".");
                        
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region AI Accelerator
                debugstage = 410;
                if ( Squad.TypeData.AIReinforcementMultiplier > FInt.One )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    buffer
                        .Add( "AI-Accelerator", Attr_Label)
                        .Add(": AI reinforcements at this planet are increased by " )
                        .AddMultiplier( Squad.TypeData.AIReinforcementMultiplier, TextStyle.Number );
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Black Hole Effect
                debugstage = 420;
                if ( Squad.TypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan > 0 )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    buffer
                        .Add( "Black-Hole Effect", Attr_Label)
                        .Add(": Enemy ships with less than " )
                        .AddNumber(Squad.TypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan, TextTerm.Engine_gX, TermUse.Icon)
                        .Add( " cannot leave the planet (unless crippled)." );
                    
                    buffer.EndStatement(Attr_Line);
                }
                else 
                if ( Squad.TypeData.AddsBlackHoleEffectForAllEntitiesPeriod )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Super Black-Hole Effect", Attr_Label).Add(": No ships can leave this planet, of any faction or status." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion 

                #region Aggro-Invisible
                debugstage = 425;
                if ( Squad.TypeData.CannotTargetOrAlertAIReinforcementSpots )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Aggro-Invisible", Attr_Label);
                    if ( Config.Detail < TooltipDetail.Medium )
                        buffer.Add("  ");
                    else
                        buffer.Add(": Cannot attack guard posts or other reinforcement points while they have loaded guards." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region PeriodicSpawn
                debugstage = 430;
                if ( Squad.TypeData.HasPeriodicSpawn )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    if ( Squad.TypeData.PeriodicallySpawnsUnits )
                    {
                        buffer.Add( "Lair", Attr_Label ).Add(": ");
                        Squad.TypeData.PeriodicSpawn_EntityTypeDrawingBag.Value.WriteToBuffer( buffer, Squad.TypeData, false );

                        var spawnFor = Squad.TypeData.Periodic_SpawnFactionForUnit;
                        var forFaction = relatedSquadFactionOrNull?.TryGetAISentinelsCoreData()?.GetAiSubFaction(spawnFor);
                        if (forFaction != null)
                        {
                            buffer.Add( " for " ).AddFactionNameInItsColor( forFaction );
                        }
                        else
                        {
                            buffer.Add( " for " ).Add(Extensions.ToString(spawnFor), TextStyle.Emphasis);
                        }
                    } 
                    else
                    {
                        if ( Squad.TypeData.PeriodicSpawn_CreatesWave && 
                             Squad.TypeData.PeriodicSpawn_CreatesExoStrike )
                        {
                            buffer.Add( "Exo/Raid Engine: Spawns waves and exo strikes" );
                            buffer.Add( "Exo-Raid Engine:", Attr_Label ).Add(": Triggers a Wave AND Exo-Strike");
                        } 
                        else 
                        if ( Squad.TypeData.PeriodicSpawn_CreatesExoStrike )
                        {
                            buffer.Add( "Exo Engine:", Attr_Label ).Add(": Triggers a Exo-Strike");
                        } 
                        else
                        {
                            buffer.Add( "Raid Engine:", Attr_Label ).Add(": Triggers a Wave");
                        }
                        
                        buffer
                            .Add( " of <color=#ffdf72>" )
                            .Add( Squad.TypeData.PeriodicSpawn_WaveOrExoSizeMultiplier.ReadableString )
                            .Add( "x </color> normal strength" );
                    }

                    buffer.Add( " <color=#ffdf72>every " ).Add( Squad.TypeData.PeriodicSpawn_DelayBetweenSpawns ).Add( " seconds</color>" );
                    if ( Squad.TypeData.PeriodicSpawn_InitialDelay > 0 )
                        buffer.Add( " after <color=#ffdf72>" ).Add( Squad.TypeData.PeriodicSpawn_InitialDelay ).Add( " seconds</color>" );
                    else
                        buffer.Add( " immmediately" );

                    buffer.Add( " when" );
                    if ( Squad.TypeData.PeriodicSpawn_MinHostileStrengthToTrigger > 0 )
                        buffer.Add( " at least " ).WrapStrengthTruncated( Squad.TypeData.PeriodicSpawn_MinHostileStrengthToTrigger, Config.UseIcons, Config.UseText ).Add( " in hostile" );
                    if ( Squad.TypeData.PeriodicSpawn_OnlyTriggerAgainstPlayer )
                        buffer.Add( " player" );
                    buffer.Add( " presence is detected" );
                    
                    if ( Squad.TypeData.PeriodicSpawn_OnlyTriggerOnOccupation )
                        buffer.Add( " occupying a planet" );
                    else
                    {
                        buffer.Add( " on this planet" );
                        if ( Squad.TypeData.PeriodicSpawn_MaxHopsToTrigger > 0 )
                            buffer.Add( " and" );
                    }
                    
                    if ( Squad.TypeData.PeriodicSpawn_MaxHopsToTrigger > 0 )
                    {
                        buffer.Add( " within <color=#ffdf72>" ).Add( Squad.TypeData.PeriodicSpawn_MaxHopsToTrigger ).Add( "</color> hops." );
                    }
                    
                    if ( Squad.TypeData.PeriodicSpawn_NeverStopOnceTriggered )
                        buffer.Add( " Once it spawned something for the first time it will not stop as long as valid enemies are in the galaxy." );
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Alarm Post
                debugstage = 440;
                if ( Squad.TypeData.FreesGuardsWhenLocalForceRatioIsWorseThan > FInt.Zero )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    debugstage = 450;
                    if ( Squad.TypeData.NumberOfHopsOutToFreeGuards > 0 )
                    {
                        buffer.Add( "Call for Aid", Attr_Label).Add(": All guarding units within <color=#ffdf72>" ).Add(
                            Squad.TypeData.NumberOfHopsOutToFreeGuards ).Add( " wormhole hops</color> from this planet will become threat (and possibly join the hunter fleet) if the attacking enemy strength is greater than <color=#ffdf72> " ).Add(
                            Squad.TypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "x</color> the AI forces on this planet." );
                    }
                    else
                    {
                        buffer.Add( "Disperse Guards", Attr_Label).Add(": All guarding units on this planet will become threat (and possibly join the hunter fleet) if the local AI strength is less than <color=#ffdf72> " ).Add(
                            Squad.TypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "x</color> that enemies on this planet." );
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region WatchPlanetsAtXHops
                debugstage = 451;
                if ( Squad.TypeData.WatchPlanetsAtXHops > 0 && 
                     ( Squad.IsFakeEntity || Squad.GetFactionTypeSafe() == FactionType.Player ) )
                {
                    debugstage = 455;
                    buffer.BeginStatement(Attr_Line);
                    
                    int hopCount = Squad.TypeData.WatchPlanetsAtXHops + (relatedMembershipOrNull == null ? 0 : relatedMembershipOrNull.Hacked_ExtraWatchPlanetsAtXHops);
                    buffer.Add( "Recon", Attr_Label).Add(": Watches all planets within " ).Add( hopCount, "ffdf72" );
                    if ( hopCount > 1 )
                        buffer.Add( " hops." );
                    else
                        buffer.Add( " hop." );
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Norris Effect
                debugstage = 460;
                if ( Squad.TypeData.PushesEnemyShields )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Norris-Effect", Attr_Label).Add(": Displaces enemy bubble-forcefield generators when moving into them." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region DisallowKiting
                debugstage = 465;
                if ( Squad.TypeData.DisallowKiting && 
                     Config.Detail >= TooltipDetail.Full )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Never allowed to kite." );
                    buffer.BeginStatement(Attr_Line);
                }
                #endregion

                #region Defense Cap Mult
                debugstage = 470;
                if (Squad.TypeData.DefensiveStructureCap_Multiplier != FInt.One &&
                    (Squad.TypeData.IsCommandStation || Squad.TypeData.IsBattlestation) &&
                    (Squad.IsFakeEntity || 
                     Squad.GetFactionTypeSafe() == FactionType.NaturalObject || 
                     Squad.GetFactionTypeSafe() == FactionType.Player))
                {
                    buffer
                        .BeginStatement(Attr_Line)
                        .Add("Defense-Cap Mult", Attr_Label)
                        .StartColor("72ffbe")
                        .Add(": Defensive lines granted to this are ")
                        .AddMultiplier(Squad.TypeData.DefensiveStructureCap_Multiplier)
                        .Add(" of the base value.")
                        .EndColor()
                        .EndStatement(Attr_Line);
                }
                #endregion

                #region SpeedLimitFromGroupMove
                if ( !Squad.IsFakeEntity && 
                     Squad.SpeedLimitFromGroupMove > 0 && 
                     Config.Detail == TooltipDetail.Full )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    SpeedGroup groupOnHost = Squad.GroupMoveSpeed_HostOnly;
                    if ( groupOnHost != null )
                        buffer.Add( "In Speed Group " ).Add( groupOnHost.SpeedGroupID );
                    else
                        buffer.Add( "In Speed Group" );
                    
                    buffer
                        .Add( " with " )
                        .WrapSpeedMoreReadable( Squad.SpeedLimitFromGroupMove, false, false )
                        .Add( " group speed and " )
                        .WrapSpeedMoreReadable( Squad.SpeedLimitFromGroupMove, false, false )
                        .Add( " calculated speed" );
                    
                    if ( groupOnHost != null && groupOnHost.OverrideSpeedLimit > 0 )
                        buffer.Add( " and " ).WrapSpeedMoreReadable( groupOnHost.OverrideSpeedLimit, false, false ).Add( " override speed" );
                    
                    buffer
                        .Add( " compared to " )
                        .WrapSpeedMoreReadable( Squad.DataForMark.Speed, false, false )
                        .Add( " original speed." );
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Orbiter
                debugstage = 480;
                if ( Squad.TypeData.OrbitsGravityWellCenterAtItsCurrentRadius ||
                     Squad.TypeData.OrbitsParentAtRange > 0 || 
                     Squad.TypeData.OrbitsFlagshipAtRange > 0 )
                {
                    buffer.BeginStatement(Attr_Line);

                    buffer.Add( "Orbits " );
                    if ( Squad.TypeData.OrbitsGravityWellCenterAtItsCurrentRadius )
                    {
                        buffer.Add( "the " ).AddColor("gravity well", "#ffffff");
                    } 
                    else 
                    if ( Squad.TypeData.OrbitsParentAtRange > 0 )
                    {
                        buffer.Add( "its " ).AddColor("ancestor", "#ffffff");
                    } 
                    else 
                    if ( Squad.TypeData.OrbitsFlagshipAtRange > 0 )
                    {
                        buffer.Add( "its " ).AddColor("flagship", "#ffffff");
                    }
                    
                    buffer
                        .Add( " at " )
                        .Open(TextTerm.Speed, TermUse.Color)
                        .AddNumber( Squad.TypeData.DegreesToOrbitPerSecond, "°/s", TextStyle.Empty )
                        .Close(TextTerm.Speed);
                    
                    int range = 0;
                    range = Mathf.Max(range, Squad.TypeData.OrbitsParentAtRange);
                    range = Mathf.Max(range, Squad.TypeData.OrbitsFlagshipAtRange);
                    
                    if (range > 0)
                    {
                        buffer
                            .Add(" and ")
                            .AddNumber( range, TextTerm.Range, TermUse.Name)
                            ;
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Electrotoxic
                var etoxic_amt = Squad.TypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack;
                if ( etoxic_amt > FInt.Zero )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer
                        .Add( "Electrotoxic", Attr_Label ).Add(": ")
                        .Add("Returns ")
                        .Open(TextStyle.Number).AddPercent( etoxic_amt ).Close(TextStyle.Number)
                        .Add( " of ").Add(TextTerm.Damage, TermUse.Name).Add(" received as ").Add(TextTerm.Damage_Exotic, TermUse.Name).Add(".");
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region Forcefield
                debugstage = 490;
                int effectiveShieldRadius = Squad.GetEffectiveMaxForcefieldRadius();
                if ( effectiveShieldRadius > 0 )
                {
                    buffer.BeginStatement(Attr_Line);
                    {
                        buffer
                            .AddModuleTag(Squad.TypeData.IsForcefieldProvidedByAnyModule).Add( "Bubble-Shield", Attr_Label )
                            
                            .StartSize("75%")
                            .Add(": Projects its ")
                            .AddNumber(Squad.GetMaxShieldPoints(), TextTerm.Shields, TermUse.Icon_Name)
                            .Add(" as a ")
                            .AddNumber(effectiveShieldRadius, TextTerm.Range, TermUse.Name).Add(" bubble.")
                            .EndSize();
                    }

                    
                    if ( Config.Detail >= TooltipDetail.Medium )
                    {
                        buffer.Open(TextStyle.Attr_Sub_Lines);
                        
                        bool blocking = !Squad.TypeData.OriginalXmlData.GetBool( "custom_forcefield_is_nonblocking", false, false );
                        if (!blocking)
                        {
                            buffer
                                .Open(Attr_Line2).Add( "Thin", Attr_Label2 )
                                .Add(": Does not block movement, only weapon fire.").Close(Attr_Line2);
                        }
                        
                        if ( Squad.TypeData.MyForcefieldDoesNotShrink )
                        {
                            buffer
                                .Open(Attr_Line2).Add( "Hardened", Attr_Label2 )
                                .Add(": Shield ").Add(TextTerm.Range, TermUse.Name).Add(" is not reduced when damaged." ).Close(Attr_Line2);
                        }
                        else if ( Config.Detail >= TooltipDetail.Medium )
                        {
                            buffer
                                .Open(Attr_Line2).Add( "Soft", Attr_Label2 )
                                .Add(": Shield ").Add(TextTerm.Range, TermUse.Name).Add( " is reduced when damaged." ).Close(Attr_Line2);
                        }
                        
                        if ( Squad.TypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                        {
                            buffer
                                .Open(Attr_Line2).Add( "Greater", Attr_Label2 )
                                .Add(": Outgoing damage of shielded allies is not reduced." ).Close(Attr_Line2);
                        }
                        else
                        {
                            buffer
                                .Open(Attr_Line2).Add( "Lesser", Attr_Label2 )
                                .Add(": Outgoing damage of shielded allies is reduced by ").AddNumber( "50%", TextTerm.Damage, TermUse.Icon ).Close(Attr_Line2);
                        }
                        
                        buffer.Close(TextStyle.Attr_Sub_Lines);
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Amplifier/Inhibitor
                debugstage = 500;
                buffer.BeginStatement(Attr_Line);
                writer.WriteAmplifierAndInhibitorData( buffer, Squad.DataForMark, Config.UseIcons, Config.UseText );
                buffer.EndStatement(Attr_Line);
                #endregion
                
                #region Unused
                /*
                debugstage = 530;
                for ( int i = 0; i < systems.Count; i++ )
                {
                    debugstage = 540;
                    var sys = systems[i];
                    if (sys.TypeData.Category == EntitySystemCategory.Weapon)
                        continue;
                    if (sys.CustomSystem != null)
                        continue;
                        
                    EntityText.WriteSystem(buffer, sys, Config);
                }
                */
                #endregion
                
                #region Deals Attrition
                debugstage = 555;
                if ( Squad.DataForMark != null && 
                     Squad.DataForMark.AttritionDamagePreFleetModifiers > 0 )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    debugstage = 55501;
                    int amount = Squad.DataForMark.AttritionDamagePreFleetModifiers;
                    if ( !Squad.IsFakeEntity )
                        amount = Squad.GetAttritionDamage();
                    
                    debugstage = 55502;
                    int max = Squad.DataForMark.MaxAttritionDamagePreFleetModifiers;
                    if ( !Squad.IsFakeEntity )
                        max = Squad.GetMaxAttritionDamageOrZeroForUnlimited();

                    buffer
                        .Add( "Attritioner", Attr_Label)
                        .Add(": Deals constant planet-wide ")
                        .Add(TextTerm.Damage_Exotic, TermUse.Name)
                        .Add(" of ")
                        .AddNumber(amount, "/s", TextStyle.Number);
                    
                    if ( max <= 0 )
                    { 
                        buffer.Add(" (uncapped).");
                    }
                    else
                    {
                        buffer
                            .Add(" capped at ")
                            .AddNumber( max )
                            .Add(".");
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region Hardened
                debugstage = 560;
                if ( Squad.TypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth > FInt.Zero &&
                    Squad.TypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth < FInt.One )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    int val = (Squad.TypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth * 100).GetNearestIntPreferringHigher();
                    
                    if ( Config.Detail < TooltipDetail.Full )
                    {
                        buffer
                            .Add( "Hardened:", Attr_Label)
                            .Add(": Never takes more than " )
                            .Add( val )
                            .Add( "% of max hull health in damage at one time." );
                    }
                    else
                    {
                        buffer
                            .Add( "Hardened:", Attr_Label)
                            .Add(": Any damage from a single source (explosion, shot, attrition, etc) will have its value reduced to " )
                            .Add( val )
                            .Add( "% of this unit's max hull health if it would be greater than that. Protects against very large guns, ion cannons, mass drivers, etc." );
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region CreatesCeasefireOnPlanet
                debugstage = 562;
                if ( Squad.TypeData.CreatesCeasefireOnPlanet )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Ceasefire:", Attr_Label).Add(" Prevents all units from firing weapons while it is on a planet with them." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region BlocksCeasefireOnPlanet
                if ( Squad.TypeData.BlocksCeasefireOnPlanet )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Ceasefire Blocker", Attr_Label).Add(": If this is on a planet, then no ceasefires will be possible." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region Harmonic
                debugstage = 564;
                if ( Squad.DataForMark.AmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 )
                {
                    var bonus = Squad.DataForMark.AmountAddedToDamagePerShipOfThisTypeOnPlanet;
                    var cap = Squad.TypeData.MaxAmountAddedToDamagePerShipOfThisTypeOnPlanet;
                    
                    buffer.BeginStatement(Attr_Line);
                    
                    buffer
                        .Add( "Harmonic", Attr_Label)
                        .Add(": Bonus ")
                        .AddNumber( bonus, "+", TextTerm.Damage, TermUse.Icon )
                        .Add(" per ship of this type on-planet, capped at ")
                        .AddNumber( cap, "+", TextTerm.Damage, TermUse.Icon )
                        .Add(".");
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion


                #region HackingEffectMultiplier
                debugstage = 700;
                if ( Squad.TypeData.HackingEffectMultiplier != FInt.One )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    if ( Squad.TypeData.HackingEffectMultiplier < FInt.One )
                        buffer.Add( "Hack Bonus", Attr_Label);
                    else
                        buffer.Add( "Hack Malus", Attr_Label);
                    buffer.Add( ": All hacks done by this unit have their response multiplied by <color=#ffdf72>" ).AddNumberMoreReadable( Squad.TypeData.HackingEffectMultiplier ).Add( "x</color>." );
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region FleetWideBonus
                if ( Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 ||
                     Squad.TypeData.SuperchargesMinSpeedOfRestOfPlayerFleet ||
                     Squad.TypeData.SuperchargesHullOfRestOfPlayerFleet > FInt.One ||
                     Squad.TypeData.SuperchargesShieldsOfRestOfPlayerFleet > FInt.One ||
                     Squad.TypeData.SuperchargesAttackPowerOfRestOfPlayerFleet > FInt.One ||
                     Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    if ( AIWar2GalaxySettingQuickAccess.DisableFleetWideBonuses )
                    {
                        buffer.Add( "<color=#f2ae1c>FLEET WIDE BONUS DISABLED:</color> <color=#e0c266>Because of galaxy options, potentially relating to your campaign type, fleet-wide bonues are not allowed.</color>" );
                    }
                    else
                    {
                        int superchargeBonusLimiter = 0;
                        bool isSuperchargeLimited = false;
                        string fleetBonusPrefix = "<color=#7cf21c>FLEET BONUS:</color> <color=#9ce066>";
                        if ( Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 && 
                             relatedMembershipOrNull != null && 
                             relatedMemFleetOrNull != null )
                        {
                            superchargeBonusLimiter = relatedMemFleetOrNull.GetCountOfShipLinesForSuperchargePurposes( null );
                            if ( superchargeBonusLimiter > Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                            {
                                isSuperchargeLimited = true;
                                fleetBonusPrefix = "<color=#f2ae1c>FLEET BONUS OFF:</color> <color=#e0c266>";
                            } 
                            else
                            {
                                fleetBonusPrefix = "<color=#7cf21c>FLEET BONUS ON:</color> <color=#9ce066>";
                            }
                        }

                        if ( Squad.TypeData.SuperchargesMinSpeedOfRestOfPlayerFleet )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "Supercharges all other non-flagship members of the fleet to be at least as fast as itself.</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesHullOfRestOfPlayerFleet > FInt.One )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "Grants a " ).AddFixedDecimal( Squad.TypeData.SuperchargesHullOfRestOfPlayerFleet.ToDouble(), 2 ).Add( "x bonus to the hull strength of all members of the fleet (including flagships).</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesShieldsOfRestOfPlayerFleet > FInt.One )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "Grants a " ).AddFixedDecimal( Squad.TypeData.SuperchargesShieldsOfRestOfPlayerFleet.ToDouble(), 2 ).Add( "x bonus to the shield strength of all members of the fleet (including flagships).</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesAttackPowerOfRestOfPlayerFleet > FInt.One )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "Grants a " ).AddFixedDecimal( Squad.TypeData.SuperchargesAttackPowerOfRestOfPlayerFleet.ToDouble(), 2 ).Add( "x bonus to the attack power of all members of the fleet (including flagships).</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                        {
                            if ( isSuperchargeLimited )
                            {
                                buffer.Add( "<color=#f2ae1c>FLEET BONUS LIMIT PASSED:</color> <color=#e0c266>Fleet-wide bonus only turns on in fleets with " ).Add(
                                    Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                                    .Add( " or fewer non-flagship, non-elite ship lines in them, but you have " ).Add( superchargeBonusLimiter ).Add( " in this fleet.</color>" );
                            }
                            else 
                            if ( relatedMembershipOrNull == null || 
                                 Config.Detail >= TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#7cf21c>FLEET BONUS LIMIT:</color> <color=#9ce066>Fleet-wide bonus only turns on in fleets with " ).Add(
                                        Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess );

                                if ( relatedMembershipOrNull != null )
                                    buffer.Add( " or fewer non-flagship, non-elite ship lines in them, and you only have " ).Add( superchargeBonusLimiter ).Add( " in this fleet.</color>" );
                                else
                                    buffer.Add( " or fewer non-flagship, non-elite ship lines in them.</color>" );
                            }
                        }
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                
                if ( !AIWar2GalaxySettingQuickAccess.DisableFleetWideBonuses &&
                     Squad.TypeData.CannotBeSuperchargedWhenInSuperchargedFleet && 
                     Config.Detail >= TooltipDetail.Full )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "<color=#f2ae1c>FLEET BONUSES DO NOT APPLY:</color> <color=#e0c266>Fleet-wide bonuses don't help this particular unit no matter what.</color>" );
                    buffer.EndStatement(Attr_Line);
                }

                #endregion
                
                #region IncomingDamageModifiers
                debugstage = 900;
                if ( Squad.TypeData.IncomingDamageModifiers_FullList.Count > 0 )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    buffer.Add("Defense-Modifiers", Attr_Label).Add(": Modifers to incoming damage.");

                    buffer.Open(TextStyle.Attr_Sub_Lines);
                    
                    for ( int k = 0; k < Squad.TypeData.IncomingDamageModifiers_FullList.Count; k++ )
                    {
                        //buffer.Open(TextStyle.Attr_Line2);
                        writer.Write( Squad.TypeData.IncomingDamageModifiers_FullList[k] );
                        //buffer.Close(TextStyle.Attr_Line2);
                    }
                    
                    buffer.Close(TextStyle.Attr_Sub_Lines);
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region ExternalInvulnerabilityUnitRequiredCount
                debugstage = 950;
                var num_req_for_invul = Squad.TypeData.ExternalInvulnerabilityUnitRequiredCount;
                var num_exist_for_invul = Squad.CountOfEntitiesProvidingExternalInvulnerability;
                if ( num_req_for_invul > 0 )
                {
                    bool is_currently = false;
                    if (Squad.IsFakeEntity || num_exist_for_invul >= num_req_for_invul)
                    {
                        is_currently = true;
                        if (Squad.TypeData.InvulnerabilityHideIf == ExternalInvulnerabilityHideIf.Vulnerable)
                            goto done_ExternalInvulnerabilityUnitRequiredCount;
                    }
                    
                    buffer.BeginStatement(Attr_Line);
                    
                    string label = is_currently ? "Invincible" : "Vulerable";
                    buffer.Add(label, TextStyle.Attr_Label).Add(": ");
                
                    if (!is_currently && !Squad.IsFakeEntity)
                        buffer.Add("Would be invulnerable if ");
                    else
                        buffer.Add("Granted invulerability if ");
                        
                    buffer
                        .Open(TextStyle.Number).AddFormat(num_req_for_invul, "{0}+ ").Close(TextStyle.Number)
                        .AddTypeGrantingInvul(Squad);
                        
                    if ( Squad.TypeData.InvulnerabilityRegion == ExternalInvulnerabilityRegion.ThisPlanet )
                        buffer.Add(" on this ").Add("Planet", TextStyle.Emphasis);
                    else
                        buffer.Add(" in the ").Add("Galaxy", TextStyle.Emphasis);
                    
                    buffer.Add(" function");
                    
                    if (!Squad.IsFakeEntity )
                        buffer.Add(" (").AddNumber(num_exist_for_invul).Add(")");
                    
                    buffer.Add(".");
                    buffer.EndStatement(Attr_Line);

                    done_ExternalInvulnerabilityUnitRequiredCount:
                    ;
                }
                #endregion

                #region SpawnOnDeath
                debugstage = 1001;
                if ( !EntityTypeDrawingBag.IsNullOrInvalid( Squad.TypeData.SpawnOnDeath_EntityTypeDrawingBag.Value ) )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "Death-Spawn", Attr_Label).Add(": " );
                    buffer.Write( Squad.TypeData.SpawnOnDeath_EntityTypeDrawingBag.Value );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region SecondsToFullyRegenerateHull
                if ( Squad.TypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                {
                    if ( !Squad.IsFakeEntity )
                    {
                        buffer.BeginStatement(Attr_Line);
                        buffer.Add( "Regeneration", Attr_Label).Add(": Regenerates to max hull over <color=#ffdf72>" ).Add( Squad.TypeData.SecondsToFullyRegenerateHull )
                        .Add( "</color> seconds if not under fire." ).EndStatement(Attr_Line);
                    }
                }
                #endregion

                buffer.BeginStatement(Attr_Line );
                {
                    #region AIPOnDeath

                    debugstage = 1010;
                    
                    if ( Squad.TypeData.AIPOnDeath != 0 )
                    {
                        var aip = Squad.TypeData.AIPOnDeath;
                        string prefix = aip > 0 ? "+" : null;
                    
                        bool ShouldWrite( GameEntity_Squad squad, Faction player, out string reason_na )
                        {
                            reason_na = null;
                            
                            debugstage = 1100;

                            if ( squad.IsFakeEntity )
                                return true;

                            Planet planet = squad.Planet;
                            if ( planet == null )
                                return true;

                            debugstage = 1101;
                            if ( squad.TypeData.AIPOnDeathOnlyWhenOwnedByAI &&
                                 squad.GetFactionTypeSafe() != FactionType.AI )
                            {
                                return false;
                            }

                            debugstage = 1103;
                            if ( localPlayerPlanetFactionOrNull == null )
                                return true;

                            debugstage = 1104;

                            if ( squad.TypeData.SpecialType == SpecialEntityType.AICommandStationReconquest &&
                                 localPlayerPlanetFactionOrNull.AIPLeftFromCommandStation == 0 )
                            {
                                debugstage = 1105;
                                reason_na = "already paid";
                                
                                return true;
                            }

                            if ( squad.TypeData.InternalName == "WarpGate" &&
                                 localPlayerPlanetFactionOrNull.AIPLeftFromWarpGate == 0 )
                            {
                                reason_na = "already paid";
                                
                                return true;
                            }

                            return true;
                        }
                        
                        string reason;
                        if (ShouldWrite(Squad, localPlayerFactionOrNull, out reason))
                        {
                            buffer.Open( TextStyle.Newline_NoLabel );
                            
                            if (Squad.GetFactionTypeSafe() == FactionType.NaturalObject &&
                                Squad.GetMetalToClaimRemaining() > 0)
                            {
                                buffer
                                    .Add( "If this is destroyed (after claimed) " )
                                    .AddNumber( aip, prefix, TextTerm.AIP, TermUse.Icon );
                            }
                            // this mirrors the execution flow in HandleAIPIncrease
                            // .. which is really quite annoying, but ..
                            else 
                            if ( Squad.GetFactionTypeSafe() == FactionType.AI )
                            {
                                buffer
                                    .Add( "If a " )
                                    .Add( "Player or Ally", TextStyle.PlayerType_Name )
                                    .Add( " destroys this " )
                                    .AddNumber( aip, prefix, TextTerm.AIP, TermUse.Icon );
                            }
                            else
                            {
                                buffer
                                    .Add( "If this is destroyed " )
                                    .AddNumber( aip, prefix, TextTerm.AIP, TermUse.Icon );
                            }
                            
                            if (reason != null)
                            {
                                buffer.Add(" ").AddVarReplace(TextVarMap.Parenthetical, (a,b,c,d)=>c.Add(reason, TextStyle.Color_Active), null);
                            }
                            
                            buffer.Add(".");
                            buffer.Close( TextStyle.Newline_NoLabel );
                        }
                    }

                    #endregion

                    #region #region DataExt_Gains
                    debugstage = 1015;
                    Squad.TypeData.ForAnyDataExtensions_AddToTooltip_GainsSection_ForEntity( buffer, Squad, relatedMembershipOrNull, Squad.TypeData, Config.Detail );
                    #endregion

                    #region ResourceOnDeath
                    debugstage = 1016;
                    bool any_humans = FactionUtilityMethods.Instance.AnyHumanPlayers();
                    bool for_enc = (Config.ExtraFlags & ShipExtraDetailFlags.Encyclopedia) > 0;
                    if ( (any_humans || for_enc) &&
                        Squad.TypeData.HasAnyResourceGrantOnDeath )
                    {
                        buffer.Open( TextStyle.Newline_NoLabel );

                        buffer.Add( "If a " ).Add( "Human", TextStyle.PlayerType_Name ).Add( " destroys this, they get " );

                        int count = 0;
                        if ( Squad.TypeData.MetalToGrantOnDeath > 0 )
                        {
                            if ( count > 0 )
                                buffer.Add( " " );
                            buffer.AddNumber( Squad.TypeData.MetalToGrantOnDeath, "+", TextTerm.Metal, TermUse.Icon );
                            count++;
                        }

                        if ( Squad.TypeData.ScienceToGrantOnDeath > 0 )
                        {
                            if ( count > 0 )
                                buffer.Add( " " );
                            buffer.AddNumber( Squad.TypeData.ScienceToGrantOnDeath, "+", TextTerm.Science, TermUse.Icon );
                            count++;
                        }

                        if ( Squad.TypeData.HackingToGrantOnDeath > 0 )
                        {
                            if ( count > 0 )
                                buffer.Add( " " );
                            buffer.AddNumber( Squad.TypeData.HackingToGrantOnDeath, "+", TextTerm.Hacking, TermUse.Icon );
                            count++;
                        }

                        buffer.Add( "." );
                        buffer.Close( TextStyle.Newline_NoLabel );
                    }
                    #endregion

                    #region AIPOnDeathWhenNoneLeft
                    debugstage = 1020;
                    if ( Squad.TypeData.AIPOnDeathWhenNoneLeft != 0 )
                    {
                        buffer.Open( TextStyle.Newline_NoLabel );

                        string prefix = Squad.TypeData.AIPOnDeathWhenNoneLeft > 0 ? "+" : null;
                        
                        if ( Squad.IsFakeEntity )
                        {
                            buffer
                                .Add( "If none remain in the " )
                                .Add("Galaxy ", TextStyle.Emphasis)
                                .AddNumber( Squad.TypeData.AIPOnDeathWhenNoneLeft, prefix, TextTerm.AIP, TermUse.Icon )
                                .Add( "." );
                        }
                        else
                        {
                            var num_rem = BaseScenario.GetCountOfMatchingAIPOnDeathWhenNoneLeft( Squad.TypeData );
                            
                            buffer
                                .Add( "If the last " ).AddNumber(num_rem).Add(" in the ")
                                .Add("Galaxy ", TextStyle.Emphasis)
                                .Add( "are destroyed ")
                                .AddNumber( Squad.TypeData.AIPOnDeathWhenNoneLeft, prefix, TextTerm.AIP, TermUse.Icon )
                                .Add( "." );
                        }
                        
                        buffer.Close( TextStyle.Newline_NoLabel );
                    }
                    #endregion

                    #region AIPWhenGrantedByHack
                    debugstage = 1030;
                    if ( Squad.TypeData.AIPWhenGrantedByHack > 0 &&
                         Squad.IsFakeEntity )
                    {
                        buffer.Open( TextStyle.Newline_NoLabel );
                        
                        string prefix = Squad.TypeData.AIPWhenGrantedByHack > 0 ? "+" : null;
                        
                        buffer
                            .Add( "If a " )
                            .Add( "Player", TextStyle.PlayerType_Name)
                            .Add( " hacks for this line " )
                            .AddNumber( Squad.TypeData.AIPWhenGrantedByHack, prefix, TextTerm.AIP, TermUse.Icon )
                            .Add( "." );
                        
                        buffer.Close( TextStyle.Newline_NoLabel );
                    }
                    #endregion

                    #region AIPToConstruct
                    debugstage = 2000;
                    if ( Squad.TypeData.AIPToConstruct != 0 &&
                         (Squad.IsFakeEntity || Squad.SelfBuildingMetalRemaining > 0))
                    {
                        debugstage = 2010;
                        buffer.Open( TextStyle.Newline_NoLabel );
                        
                        debugstage = 2020;
                        
                        buffer
                            .Add("If a ")
                            .Add("Player", TextStyle.PlayerType_Name)
                            .Add(" builds this ");
                            
                        string prefix = Squad.TypeData.AIPToConstruct > 0 ? "+" : null;
                        buffer.AddNumber( Squad.TypeData.AIPToConstruct, prefix, TextTerm.AIP, TermUse.Icon );

                        buffer.Add(".");
                        
                        buffer.Close( TextStyle.Newline_NoLabel );
                    }
                    #endregion

                    #region ClaimCost
                    debugstage = 2000;
                    if ( Squad.TypeData.AIPToClaim != 0 &&
                         (Squad.IsFakeEntity || Squad.GetMetalToClaimRemaining() > 0))
                    {
                        debugstage = 2010;
                        buffer.Open( TextStyle.Newline_NoLabel );
                        
                        debugstage = 2020;
                        
                        buffer
                            .Add("If a ")
                            .Add("Player", TextStyle.PlayerType_Name)
                            .Add(" claims this ");
                            
                        string prefix = Squad.TypeData.AIPToClaim > 0 ? "+" : null;
                        buffer.AddNumber( Squad.TypeData.AIPToClaim, prefix, TextTerm.AIP, TermUse.Icon );

                        buffer.Add(".");
                        
                        buffer.Close( TextStyle.Newline_NoLabel );
                    }
                    #endregion

                    ArcenExternalCodeHook.Invoke("EntityText_Append_PlayerCostsAndRewards", writer);
                }
                buffer.EndStatement( Attr_Line );

                #region Hackable

                debugstage = 386;
                if ( Squad.TypeData.GetIsEligibleForAnyHack() )
                {
                    var hackTypes = Squad.TypeData.GetListOfHacks();

                    bool hasShownGrants = false;
                    bool hasDoneHeader = false;
                    int counter = 0;
                    for (int i = 0; i < hackTypes.Count; i++)
                    {
                        var hack = hackTypes[i];
                        
                        if ( hack.GetShouldSkipHackInEntityTooltip( Squad ) )
                            continue;
                        
                        if (hack.IsAGrantShipStyleHack && 
                            hasShownGrants)
                        {
                            continue;
                        }
                        
                        if (!Squad.IsFakeEntity &&
                            hack.IsAGrantShipStyleHack &&
                            Squad.ShipGrantsList.Count == 0)
                        {
                            continue;
                        }
                        
                        if ( !hasDoneHeader )
                        {
                            hasDoneHeader = true;
                            buffer.BeginStatement(Attr_Line);
                            //buffer.Open(TextStyle.Attr_Line);
                            buffer.Add( "Hackable", Attr_Label).Add(": " );
                        }
                        else
                        if (counter > 0)
                        {
                            buffer.Add(", ");
                        }
                        
                        counter++;
                        
                        buffer.InCase(TextCaps.FirstLetter).Add( hack.GetShortDisplayName(), TextStyle.Hack_Name ).EndCase();
                        
                        try
                        {
                            bool handled = hack.Implementation.WriteAnySpecialDisplayCodeForHackedShipTooltip( Squad, localPlayerFactionOrNull, buffer, (BaseTooltipDetail) Config.Detail );
                            if (handled)
                                continue;
                        } 
                        catch ( Exception e )
                        {
                            LOG.Err("Error in WriteAnySpecialDisplayCodeForHackedShipTooltip for '{0}':\n{1}", hack.InternalName, e);
                            continue;
                        }
                        
                        if ( !hasShownGrants && 
                             hack.IsAGrantShipStyleHack )
                        {
                            Config.ActiveHackAgainstUs = hack;
                             
                            debugstage = 3856;
                            buffer.Add( " <color=#cdcdcd><size=70%>choose from</size></color> " );
                            
                            if (Squad.IsFakeEntity)
                            {
                                int numChoices = Squad.TypeData.GrantsStuffToBeAddedToPlayerFleets_FrigateOptions + 
                                                 Squad.TypeData.GrantsStuffToBeAddedToPlayerFleets_StrikecraftOptions;
                            if (numChoices == 0)
                                buffer.Add("available");
                            else
                                buffer.Add("1 of ").Add(numChoices);
                            buffer.Add(" ship lines");
                            }
                            else
                            {
                                for ( int j = 0; j < Squad.ShipGrantsList.Count; j++ )
                                {
                                    debugstage = 3857;
                                    var ship = Squad.ShipGrantsList[j];
                                    
                                    var tmp = new ShipForDisplay(ship, Config);
                                    tmp.Write(buffer, TextVarMap.Inline_Ship_Format);
                                    
                                    if ( j < (Squad.ShipGrantsList.Count-1) )
                                        buffer.Add( " " );
                                }
                            }
                            
                            buffer.Add( " " );
                        }
                        
                        if (hack.IsAGrantShipStyleHack)
                            hasShownGrants=true;
                    }
                    
                    if ( hasDoneHeader )
                    {
                        buffer.EndStatement(Attr_Line);
                        //buffer.Close(TextStyle.Attr_Line);
                    }
                }
                #endregion

                #region Ancestor
            debugstage = 3000;
                if ( !Squad.IsFakeEntity )
                {
                    GameEntity_Squad parent = Squad.ParentGameEntity.GetSquad();
                    if ( parent != null )
                    {
                        buffer
                            .BeginStatement(Attr_Line)
                            .Add( "Ancestor", Attr_Label).Add(": " );
                        
                        writer.WriteShip(parent, ShipExtraDetailFlags.HighestDetail);
                        
                        if ( Squad.TypeData.DiesIfParentDies )
                            buffer.Add( " (dies with ancestor)" );
                        
                        buffer.EndStatement(Attr_Line);
                    }
                } 
                else 
                if ( Squad.TypeData.DiesIfParentDies )
                {
                    buffer
                        .BeginStatement(Attr_Line)
                        .Add( "Ancestor", Attr_Label)
                        .Add( ": Created by an ancestor and dies if it does." )
                        .EndStatement(Attr_Line);
                }
                #endregion
                
                // formerly 'last items' past here
                //#region last items
                //buffer.StartColor( "888888" );

                #region IsElite
                debugstage = 3010;
                if ( (Squad.TypeData.IsElite) && Config.Detail >= TooltipDetail.Medium )
                    buffer.BeginStatement(Attr_Line).Add( "Elite: Only one elite ship line can be added to any fleet" ).EndStatement(Attr_Line);
                #endregion
                
                #region ProvidesAIWarpEntryPoint
                debugstage = 3020;
                if ( (Squad.TypeData.ProvidesAIWarpEntryPoint || Squad.TypeData.IsWarpBeacon) && Config.Detail >= TooltipDetail.Medium )
                    buffer.BeginStatement(Attr_Line).Add( "Allows AI ships to warp in here" ).EndStatement(Attr_Line);
                #endregion
                
                #region FleetMembershipStyle.Planetary
                debugstage = 3030;
                if ( Squad.TypeData.IsMobile && Squad.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary && Config.Detail >= TooltipDetail.Medium )
                    buffer.BeginStatement(Attr_Line).Add( "Cannot traverse wormholes" ).EndStatement(Attr_Line);
                #endregion
                
                #region AutomaticallyDiesWithCommandStation
                debugstage = 3030;
                if ( Squad.TypeData.AutomaticallyDiesWithCommandStation )
                    buffer.BeginStatement(Attr_Line).Add( "Self-destructs if command station is destroyed" ).EndStatement(Attr_Line);
                #endregion
                
                #region DiesAfterLifetimeSec
                debugstage = 3040;
                if ( Squad.IsFakeEntity && Squad.TypeData.DiesAfterLifetimeSec > 0 )
                    buffer.BeginStatement(Attr_Line).Add("Expires after ").AddMinutesAndSeconds( Squad.TypeData.DiesAfterLifetimeSec).EndStatement(Attr_Line);
                #endregion
                
                #region SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet
                if ( Squad.TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > FInt.Zero && 
                     !Squad.TypeData.AlwaysSelfAttritions /*&& 
                     Config.Detail >= TooltipDetail.Full*/ )
                {
                    var perc = (Squad.TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet / FInt.OneHundred).ToFloat();
                    var max = Squad.GetMaxHullPoints();
                    var per_sec = max * perc;
                    
                    var starting = max;
                    if (Squad.TypeData.StartsAtHull > 0)
                        starting = Squad.TypeData.StartsAtHull;
                    
                    var lifetime = starting / per_sec;
                    
                    buffer
                        .BeginStatement(Attr_Line)
                        .Add("Dependent", Attr_Label)
                        .Add(": ");
                        
                    if (!Squad.IsFakeEntity)
                    {
                        var status = Squad.GetParentAndStatus(out _);
                        
                        string label = "STABLE";
                        if (status != GameEntity_Squad.ParentStatus.Alive_Local)
                            label = "DYING";
                  
                        buffer.AddVarReplace( TextVarMap.InParenthesis, null, (x,y,b,z)=>b.Add(label, TextStyle.Color_Inactive), null ).Add(" ");
                    }
                                       
                    buffer.Add(" Dies after ").Add("~", TextStyle.MinutesAndSeconds).AddMinutesAndSeconds(Mathf.RoundToInt(lifetime)).Add(" without a parent ship present.");
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region AlwaysSelfAttritions
                debugstage = 3045;
                if ( Squad.TypeData.AlwaysSelfAttritions /*&& 
                     Config.Detail >= TooltipDetail.Full*/ )
                {
                    var perc = (Squad.TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet / FInt.OneHundred).ToFloat();
                    var max = Squad.GetMaxHullPoints();
                    var per_sec = max * perc;
                    
                    var starting = max;
                    if (Squad.TypeData.StartsAtHull > 0)
                        starting = Squad.TypeData.StartsAtHull;
                    
                    var lifetime = starting / per_sec;
                    
                    buffer
                        .BeginStatement(Attr_Line)
                        .Add("Self-Attrition", Attr_Label)
                        .Add(": ")
                        .Add("Loses ")
                    .AddNumber(per_sec, null, TextTerm.Hull, TermUse.Icon).Add(" /sec", TextStyle.Fraction_Gray)
                        .Add(" killing it after ")
                    .AddMinutesAndSeconds(lifetime)
                    //.AddNumber(lifetime, "sec", TextStyle.MinutesAndSeconds, "~")
                        .Add(".")
                        .EndStatement(Attr_Line);
                }
                #endregion
                
                #region unused
                debugstage = 3050;
                // This is already described in the ship class line...
                /*
                if ( !Squad.TypeData.ShipClass.CanBeDamaged )
                    buffer.Add( "Immune to all damage" ).EndStatement(Attr_Line);
                */
            #endregion

                #region ImmuneToRepairs
                debugstage = 3051;
                if ( Squad.TypeData.ImmuneToRepairs )
                {
                    buffer.BeginStatement(TextStyle.Newline_NoLabel).Add( "Cannot be repaired." ).EndStatement(TextStyle.Newline_NoLabel);
                }
                #endregion
                
                #region unused
                debugstage = 3052;
                // disabled this message, it is redundant on anything not player owned
                /*
                if ( Config.Detail >= TooltipDetail.Full && !Squad.TypeData.IsScrappingByPlayerDisallowed ) {
                    int percentageToReturnOfMetalAsInt = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ScrapRefundsOnFriendlyPlanets" );
                    FInt percentageToReturnOfMetalAsFIntMult = (FInt)percentageToReturnOfMetalAsInt / FInt.OneHundred;
                    if (percentageToReturnOfMetalAsInt > 0) {
                        int metalCostForScrapping = (Squad.DataForMark.MetalCost * Squad.TypeData.MetalCostMultiplierForScrapping * percentageToReturnOfMetalAsFIntMult).GetNearestIntPreferringHigher();
                        if ( Squad.TypeData.MetalCostMultiplierForScrapping == FInt.Zero ) {
                            buffer.Add( "Scrapping this unit gives no metal.  ");
                        } else {
                            buffer.Add( "Scrapping this unit on a friendly planet refunds ").AddMetal_MoreReadable(metalCostForScrapping, true).Add(".  ");
                        }
                    }
                }
                */
            #endregion

                #region ImmuneToProtectionByForcefields
                debugstage = 3053;
                if ( Squad.TypeData.ImmuneToProtectionByForcefields )
                {
                    if ( Config.Detail >= TooltipDetail.Full )
                        buffer.BeginStatement(Attr_Line).Add( "Cannot be protected by forcefields, due to its strange interaction with the fabric of reality" ).EndStatement(Attr_Line);
                    else
                        buffer.BeginStatement(Attr_Line).Add( "Cannot be protected by forcefields" ).EndStatement(Attr_Line);
                }
                #endregion
                
                #region ImmuneToBonusDamage
                if ( Squad.TypeData.ImmuneToBonusDamage )
                {
                    buffer.BeginStatement(Attr_Line).Add( "Immune to enemy weapon system bonus damage" ).EndStatement(Attr_Line);
                }
                #endregion
                
                #region CanPassThroughEnemyForcefields
                if ( Squad.TypeData.CanPassThroughEnemyForcefields )
                {
                    buffer.BeginStatement(Attr_Line);
                    if ( Config.Detail >= TooltipDetail.Full )
                        buffer.InCase(TextCaps.FirstLetter).Add( "Forcefield Harmonics", Attr_Label).EndCase().Add(": Can match shield harmonics to pass through enemy forcefields, although any weapons fire it does will still impact on the forcefield itself." );
                    else
                        buffer.Add( "Forcefield Harmonics", Attr_Label).Add(": Pass through enemy forcefields." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region CurrentStateOfMatter
                debugstage = 30531100;
                if ( !Squad.IsFakeEntity && 
                     Squad.CurrentStateOfMatter.ShouldShowDescriptionInTooltips )
                {
                    buffer.BeginStatement(TextStyle.Newline_NoLabel);
                        
                    var state_matter_style = TextStyle.Get("State_Of_Matter");
                    buffer
                        //.Open(TextStyle.Newline_NoLabel)
                        .Open(state_matter_style)
                        .Add( Squad.CurrentStateOfMatter.DisplayName )
                        .Add( " State Of Matter" )
                        ;
                    
                    if ( Config.Detail < TooltipDetail.Medium)
                    {
                        buffer.Close(state_matter_style);
                    }
                    else
                    {
                        buffer
                            .Add( ":" )
                            .Close(state_matter_style)
                            .Add(" ");
                        
                        if (Config.Detail == TooltipDetail.Medium)
                            buffer.Add( Squad.CurrentStateOfMatter.DescriptionShort );
                        else
                            buffer.Add( Squad.CurrentStateOfMatter.DescriptionFull );
                    }
                    
                    //buffer.Close(TextStyle.Newline_NoLabel);
                    buffer.EndStatement(TextStyle.Newline_NoLabel);
                }
                #endregion

                #region AlternativeStateOfMatter
                debugstage = 30531200;
                if ( Squad.TypeData.AlternativeStateOfMatter != null && 
                     Squad.TypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds > 0 )
                {
                    int returnTime = Squad.TypeData.PhasesBackToDefaultStateOfMatterAfterSeconds;
                    if ( returnTime <= 0 )
                        returnTime = Squad.TypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds;
                    
                    buffer.BeginStatement(Attr_Line);
                    buffer.Open(TextStyle.Newline_NoLabel);
                    
                    if ( Config.Detail >= TooltipDetail.Full )
                    {
                        buffer.Add( "Strange interactions with the very fabric of spacetime cause this to exist in the normal plane of existence only part of the time, making it invincible and invisible " );
                        
                        if ( Squad.TypeData.AlternativeStateOfMatter.CanTargetOtherUnitsInThisState )
                            buffer.Add( "while it is in the other state." );
                        else
                            buffer.Add( "in the other state EXCEPT to other units in the same state." );
                        
                        buffer.NewLine();
                    }

                    buffer
                        .Add( "Phases to " ).Add( Squad.TypeData.AlternativeStateOfMatter.DisplayName )
                        .Add( " every " )
                        .Add( Squad.TypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds )
                        .Add( " seconds, and back " )
                        .Add( returnTime )
                        .Add( " seconds after that." );
                    
                    buffer.Close(TextStyle.Newline_NoLabel);
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region StateOfMatterToBecomeOnWormholeExit
                debugstage = 30531300;
                if ( Squad.TypeData.StateOfMatterToBecomeOnWormholeExit != null )
                {
                    buffer.BeginStatement(Attr_Line);
                    int returnTime = Squad.TypeData.ReturnsToDefaultStateOfMatterAfterSecondsFromWormholeExit;
                    buffer
                        .InCase(TextCaps.FirstLetter).Add( "Phase-Warp", Attr_Label).EndCase()
                        .Add(": Phases to " ).Add( Squad.TypeData.StateOfMatterToBecomeOnWormholeExit.DisplayName )
                        .Add( " whenever it passes through a wormhole" );
                    
                    if ( returnTime > 0 )
                        buffer.Add( ", and then returns to normal " ).Add( returnTime ).Add( " seconds after that." );
                    else
                        buffer.Add( ", and does not schedule a return to normal." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region AI Core-Phase
                debugstage = 30531400;
                var force_state_aihome = Squad.TypeData.ForcedToBeThisStateOfMatterWhenOnFormerOrCurrentAIHomeworldOrBastionWorldsUnlessEnemyKingPresent;
                if ( force_state_aihome != null && 
                     force_state_aihome.ShouldShowDescriptionInTooltips)
                {
                    buffer.BeginStatement(Attr_Line);
                        
                    buffer
                        .InCase(TextCaps.FirstLetter)
                        .Add( "AI Core-Phase", Attr_Label)
                        .EndCase()
                        .Add(": Phases to " )
                        .Add( force_state_aihome.DisplayName )
                        .Add( " whenever it is on a current or former AI Homeworld or AI Bastion World (unless it is in the presence of an enemy king)." );
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region ImmuneToAllDamageForSecondsAfterCreation
                debugstage = 3060;
                var immune_dur = Squad.TypeData.ImmuneToAllDamageForSecondsAfterCreation;
                var alive_sec = Squad.GetSecondsSinceCreation();
                if ( immune_dur > 0 && 
                     ((immune_dur > alive_sec) || Squad.IsFakeEntity))
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    if ( !Squad.IsFakeEntity )
                    {
                        buffer
                            .Add( "Immune to all damage for " )
                            .AddMinutesAndSeconds( immune_dur - alive_sec )
                            .Add( " more." );
                    }
                    else
                    {
                        buffer
                            .Add( "Immune to all damage for " )
                            .AddMinutesAndSeconds( immune_dur )
                            .Add( " after creation." );
                    }
                }
                #endregion

                #region UnitToMakeWithBuildPointsFromDamageTaken
                debugstage = 3061;
                {
                    var type = Squad.TypeData.UnitToMakeWithBuildPointsFromDamageTaken;
                    var rate = Squad.TypeData.BuildPointsPerDamageTaken;
                    if (type != null && rate > 0)
                    {
                        var cost = Squad.GetCostToBuildHydra();
                        var cur = Squad.BuildPoints;
                        
                        int ehp = 0;
                        if (Squad.TypeData.StartsAtShields > 0)
                            ehp += Squad.TypeData.StartsAtShields;
                        else
                            ehp += Squad.GetMaxShieldPoints();
                        
                        if (Squad.TypeData.StartsAtHull > 0)
                            ehp += Squad.TypeData.StartsAtHull;
                        else
                            ehp += Squad.GetMaxHullPoints();
                        
                        var ehp_per = cost / rate;
                        var cap = ehp / ehp_per;
                        
                        buffer.BeginStatement(Attr_Line);
                        
                        //var cap = 
                        buffer.Add("Damage-Spawn", Attr_Label);
                        buffer
                            .Add(": Produces ")
                            .Open(TextStyle.Color_Count)
                            .Add("~").Add(cap)
                            .Add(Text.Multiply)
                            .Close(TextStyle.Color_Count)
                            .Add(" ")
                            .WriteSpawn(type)
                            .Add(" over its lifetime, as it takes damage ")
                            //.AddVarReplace(TextVarMap.InParenthesis, null, (a,b,c,d)=>{c.Add("1/").AddNumber(ehp_per, TextTerm.EHP, TermUse.Icon);}, null )
                            .Add(".");
                        
                        buffer.EndStatement(Attr_Line);
                    }
                }
                #endregion

                #region NumberOfWeaponPoints
                debugstage = 3062;
                // Puffin Note Neinzul Fireflies
                if ( !Squad.IsFakeEntity && 
                     Squad.NumberOfWeaponPoints > FInt.Zero )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer
                        .Add( "Currently has <color=#ffdf72>" ).Add( Squad.NumberOfWeaponPoints )
                        .Add( "</color> weapon points." );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region DeathEffect Points Received
                // For Death Effects. Lists each one a unit has been hit by, the current amount, and the amount required.
                // pair.Value = current amount, pair.Key.Scale = amount required (scale being from the XML for each death effect).
                debugstage = 3063;
                if ( !Squad.IsFakeEntity && 
                     Squad.DeathEffectCausingDamageReceivedToEntity.Count > 0 )
                {
                buffer.BeginStatement(Attr_Line);
                    foreach ( var pair in Squad.DeathEffectCausingDamageReceivedToEntity )
                    {
                    var deffect = pair.Key;
                    var amount = pair.Value;
                    if (deffect == null || amount == 0)
                        continue;
                    
                    buffer
                        .NewLineIfNeeded()
                        .Add( "Has ")
                        .Open(TextStyle.Color_Count).AddNumber(amount, null, TextStyle.Empty)/*.Add(Text.Multiply)*/.Close(TextStyle.Color_Count)
                        .Open(TextStyle.Fraction_Gray).Add("/").Add( deffect.Scale ).Close(TextStyle.Fraction_Gray)
                        .Add(" ")
                        .Add( deffect.DescriptionDamageName )
                        .Add( " damage taken" );
                    var fac = World_AIW2.Instance.GetFactionByIndex(Squad.DeathEffectDamageLastDoneByFactionIndex[deffect]);
                    if (fac != null)
                        buffer.Open(TextStyle.Color_Gray).Add(" (").Add(fac.GetDisplayName()).Add(")").Close(TextStyle.Color_Gray);
                    buffer.Add(".");
                }
                buffer.EndStatement(Attr_Line);
            }
            #endregion

                #region Fuel Cost
                debugstage = 3070;
                if ( World_AIW2.Instance.IsFuelEnabled && !Squad.IsFakeEntity )
                {
                    if ( Squad.GetIsOutguardUnit() )
                    {
                        if ( AIWar2GalaxySettingQuickAccess.OutguardAlsoCostXenon )
                        {
                            FInt worstXenonRatio = FInt.One;
                            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                            {
                                if ( worstXenonRatio > player.FuelXenonOveruseRatio )
                                    worstXenonRatio = player.FuelXenonOveruseRatio;
                            }
                            if ( worstXenonRatio < FInt.One )
                                buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by the worst player Xenon ratio, which is " )
                                    .AddFixedDecimal( worstXenonRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                        }
                    } 
                    else
                    {
                        switch ( Squad.TypeData.FuelUseType )
                        {
                            case ResourceType.FuelArgon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelArgonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by your Argon ratio, which is " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelArgonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                                }
                                break;
                            case ResourceType.FuelRadon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelRadonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by your Radon ratio, which is " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelRadonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                                }
                                break;
                            case ResourceType.FuelXenon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelXenonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by your Xenon ratio, which is " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelXenonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                                }
                                break;
                        }
                    }
                }
                #endregion

                #region IsCrippledInsteadOfDying, DiesToRemains, RevertsToNeutralOnDeathIfPermadeathSettingIsFalse
                if ( Config.Detail >= TooltipDetail.Full )
                {
                    if ( Squad.TypeData.IsCrippledInsteadOfDying && 
                         (Squad.IsFakeEntity || 
                          Squad.GetFactionTypeSafe() == FactionType.Player ||
                          Squad.GetFactionTypeSafe() == FactionType.NaturalObject) )
                    {
                        buffer.BeginStatement(Attr_Line);
                        
                        buffer.Add( "Cannot die, but becomes " ).Add("crippled", TextStyle.Brighter).Add(" at ").AddNumber(1, TextTerm.Hull, TermUse.Icon);
                        
                        if ( Squad.TypeData.ForcedToBailOutOnCripple_Any )
                            buffer.Add( ". When crippled bails-out to a friendly planet" );
                        else 
                        if ( Squad.TypeData.ForcedToBailOutOnCripple_DeepstrikeOnly )
                            buffer.Add( ". When crippled in " ).Add("deepstrike territory", TextStyle.Brighter).Add(" bails-out to a friendly planet" );

                        FInt extraCost = Squad.TypeData.GetExtraCostWhileCrippled();
                        int hackingPointsLost = Squad.TypeData.GetHackingPointsLostWhenCrippled();
                        
                        /*
                        buffer
                            .StartColor( "999999" )
                            .Add( "If this unit becomes crippled, repairs to it will cost " )
                            .AddFixedDecimal( extraCost.ToFloatNonSim(), 2 )
                            .Add( "x more until it is no longer crippled (when it reaches full health)" ).EndStatement(EndStatementStyle.EOL);
                        */
                        
                        if ( hackingPointsLost > 0 )
                            buffer.Add( ", and you will lose " ).AddNumber( hackingPointsLost, TextTerm.Hacking, TermUse.Icon );
                        
                        buffer.Add( "." );
                        
                        if ( relatedMemFleetOrNull != null &&
                             relatedMemFleetOrNull.TimesCrippled_UIOnly > 0 &&
                             Config.Detail >= TooltipDetail.Full )
                        {
                            buffer.Add( " Has been crippled " ).AddNumber( relatedMemFleetOrNull.TimesCrippled_UIOnly ).Add( " time(s)." );
                        }

                        buffer.EndStatement(Attr_Line);
                    }
                    else 
                    if ( Squad.TypeData.DiesToRemains && 
                         (Squad.IsFakeEntity || 
                          Squad.GetFactionTypeSafe() == FactionType.Player ||
                          Squad.GetFleetFactionType_Safe() == FactionType.NaturalObject))
                    {
                        buffer.BeginStatement(Attr_Line);
                        buffer.Add( "When controlled by a " ).Add("Player", TextStyle.PlayerType_Name).Add(" dies to remains that can be rebuilt." );
                        buffer.EndStatement(Attr_Line);
                    }
                    else 
                    if ( Squad.GetShouldDieToNeutral() )
                    {
                        buffer
                            .BeginStatement(Attr_Line)
                            .Add( "Reverts to ").Add("Neutral", TextStyle.PlayerType_Name).Add(" when destroyed." )
                            .EndStatement(Attr_Line);
                    }
                }
                #endregion
                
                debugstage = 30710;

                #region IsAfterANonHumanTeam_NonSim
                debugstage = 30712;
                if ( !Squad.IsFakeEntity && 
                     Squad.IsAfterANonHumanTeam_NonSim )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    buffer.Add( "<color=#ff5bf2>NOT AFTER YOU" );
                    if ( Config.Detail >= TooltipDetail.Full )
                    {
                        buffer.Add( ":</color> This ship will fire on you if you fly up to it, but otherwise will ignore you. It is quite occupied hunting " );
                        buffer.Add( Squad.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( "." );
                    } 
                    else 
                    if ( Config.Detail >= TooltipDetail.Medium )
                    {
                        buffer.Add( ":</color> Hunting " );
                        buffer.Add( Squad.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( "." );
                    } 
                    else
                    {
                        buffer.Add( "</color>" );
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region DespawnsInXSeconds
                debugstage = 30712;
                if ( !Squad.IsFakeEntity && 
                     Squad.DespawnsInXSeconds > 0 )
                {
                    buffer
                        .BeginStatement(TextStyle.Newline_NoLabel)
                        .StartColor("7486d1")
                        .Add( "This entity will despawn in " ).AddMinutesAndSeconds( Squad.DespawnsInXSeconds ).Add(".")
                        .EndColor()
                        .EndStatement(TextStyle.Newline_NoLabel);
                }
                #endregion

                debugstage = 3080;

                #region Appender
                
                debugstage = 5000;
                if ( Squad.TypeData.DescriptionAppender != null && 
                     !Squad.IsFakeEntity )
                {
                    buffer.BeginStatement(TextStyle.Attr_Line);
                    Squad.TypeData.DescriptionAppender.AddToDescriptionBuffer( Squad, Squad.TypeData, buffer );
                    buffer.EndStatement(TextStyle.Attr_Line);
                }

                #endregion

                #region Galaxy-Wide Cap
                
                debugstage = 5400;

                if ( relatedSquadFactionOrNull?.Type == FactionType.Player )
                {
                    var effectiveGalaxyCap = Squad.TypeData.CalculateEffectiveGalaxyWideCapForPlayersConstructing(relatedSquadFactionOrNull);
                    if (effectiveGalaxyCap > 0)
                    {
                        int countOfExisting = 0;
                        var thisSquad = this.Squad;
                        foreach ( GameEntity_Squad e in relatedSquadFactionOrNull.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                        {
                            if ( e.TypeData.GalaxyWideCapMatchString == thisSquad.TypeData.GalaxyWideCapMatchString )
                                countOfExisting++;
                        }

                        buffer
                            .BeginStatement(TextStyle.Newline_NoLabel)
                            .Add( "Galaxy-Wide Cap: " )
                            .AddNumber( countOfExisting )
                            .Open(TextStyle.Fraction_Gray)
                            .Add( "/" ).Add( effectiveGalaxyCap )
                            .Close(TextStyle.Fraction_Gray);

                        if ( Config.Detail >= TooltipDetail.Full )
                        {
                            if ( Squad.TypeData.GalaxyWideCapForPlayersConstructingXPlanetVariable > 0 &&
                                 Squad.TypeData.AddedGalaxyWideCapForPlayersConstructingPerXPlanets > 0 )
                            {
                                buffer
                                    .Add( "<size=80%>" )
                                    .Add( " (" ).Add( Squad.TypeData.BaseGalaxyWideCapForPlayersConstructing )
                                    .Add( " + " ).Add( Squad.TypeData.AddedGalaxyWideCapForPlayersConstructingPerXPlanets )
                                    .Add( " per " ).Add( Squad.TypeData.GalaxyWideCapForPlayersConstructingXPlanetVariable )
                                    .Add( " player-owned planets. ")
                                    .Add( World_AIW2.Instance.PlayerOwnedPlanets, "a1ffa1" )
                                    .Add(" Player planets Owned)" )
                                    .Add( "</size>" );
                            }
                        }
                        
                        buffer.EndStatement(TextStyle.Newline_NoLabel);
                    }
                }

                #endregion
                
                #region Planned (metal) Flows
                // im showing this inline with the supported flow list
                /*
                if ( Config.Detail >= TooltipDetail.Full && 
                     Squad.SquadPlannedFlows.Count > 0 )
                {
                    var flows = Squad.SquadPlannedFlows.GetDisplayList();
                    
                    buffer.Add( "<color=#6bffec>Metal Flows: " ).Add( flows.Count );
                    
                    for ( int flowIndex = 0; flowIndex < flows.Count; flowIndex++ )
                    {
                        if ( flowIndex > 0 )
                            buffer.Add( ", " );
                        else
                            buffer.Add( ": " );
                        
                        PlannedMetalFlow plannedFlow = flows[flowIndex];
                        if ( plannedFlow.FromEntity == null )
                        {
                            buffer.Add( "[null flow]" );
                            continue;
                        }
                        
                        if ( !plannedFlow.IsInRangeAtTheMoment )
                            buffer.Add( "(Out of Range) " );
                        
                        switch ( plannedFlow.Purpose )
                        {
                            case MetalFlowPurpose.AssistSelfConstruction:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Help Build", true );
                                break;
                            case MetalFlowPurpose.AssistFactoryConstruction:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Assist Factory", true );
                                break;
                            case MetalFlowPurpose.ClaimingNeutrals:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Claim", true );
                                break;
                            case MetalFlowPurpose.RebuildingRemains:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Rebuild", true );
                                break;
                            case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Rep Eng", true );
                                break;
                            case MetalFlowPurpose.RepairingHullsOfFriendlies:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Rep Hull", true );
                                break;
                            case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Rep Shld", true );
                                break;
                            case MetalFlowPurpose.BuildingDronesInternally:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Build Drones", true );
                                break;
                            case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Factory Work", true );
                                break;
                            case MetalFlowPurpose.SelfConstruction:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, "Self Build", true );
                                break;
                            default:
                                WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, Squad, plannedFlow.Purpose.ToString(), true );
                                break;
                        }
                    }
                    
                    buffer.Add( "</color>  " );
                }
                */
                #endregion
                
                #region Error Checks For Strange Things
                debugstage = 87396000;
                if ( Config.ShowDebugInfo && !Squad.IsFakeEntity )
                {
                    buffer.BeginStatement(TextStyle.Newline_NoLabel);
                    
                    if ( relatedMembershipOrNull != null && !relatedMembershipOrNull.DoesShipLineContainThisExactShip( Squad ) )
                        buffer.Add( "<color=#ff731e>ERROR!  The fleet membership for this ship does not actually contain it!</color>  " );
                    if ( Squad.ToBeRemovedAtEndOfThisFrame )
                        buffer.Add( "<color=#ff731e>ERROR!  ToBeRemovedAtEndOfThisFrame = true!</color>  " );
                    if ( Squad.HasBeenRemovedFromSim )
                        buffer.Add( "<color=#ff731e>ERROR!  HasBeenRemovedFromSim = true!</color>  " );
                    if ( Squad.IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject )
                        buffer.Add( "<color=#ff731e>ERROR!  IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject = true!</color>  " );
                    if ( Squad.HasDoneOnDeathSinceLastClaimed )
                        buffer.Add( "<color=#ff731e>ERROR!  HasDoneOnDeathSinceLastClaimed = true!</color>  " );
                    if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                    {
                        if ( Squad.InstancedRenderer == null )
                            buffer.Add( "<color=#ff731e>ERROR!  InstancedRenderer is null!</color>  " );
                        if ( Squad.Planet != Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() )
                            buffer.Add( "<color=#ff731e>ERROR!  Planet of this ship is " ).Add( Squad.Planet == null ? "null" : Squad.GetPlanetName_Safe() ).Add( " instead of planet being viewed.</color>  " );
                    }
                    
                    buffer.EndStatement(TextStyle.Newline_NoLabel);
                }
                #endregion
                
                #region Codehooks
                debugstage = 87398000;
                {
                    var codeHook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "GlobalEntityDescriptionAppender" );
                    if (codeHook != null)
                    {
                        buffer.BeginStatement(Attr_Line);
                        codeHook.HandleAllSubscribedHooks( buffer, Squad, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                        buffer.EndStatement(Attr_Line);
                    }
                }
                #endregion
                
                #region Exo stuff
                debugstage = 87399000;
                if ( Squad.TypeData.ExoGenerationDifficulty > 0 )
                {
                    AIDifficulty highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
                    if ( highestDifficulty.Difficulty >= Squad.TypeData.ExoGenerationDifficulty )
                    {
                        buffer.Add( "The AI will generate Exostrikes (Exogalactic Strikeforces) against you if you capture this structure. " );
                    }
                }
                #endregion
                
                #region TechUpgradesThatBenefitMe
                debugstage = 1500;
                if ( Config.Detail >= TooltipDetail.Medium || 
                     Squad.IsFakeEntity )
                {
                    debugstage = 1501;
                    var starts_at = Squad.TypeData.StartingMarkLevel;
                    if ( starts_at.Ordinal > 1 )
                    {
                        debugstage = 1503;
                        buffer.BeginStatement(Attr_Line);
                        buffer
                            .Add( "Starts at " )
                            .AddColor( Squad.TypeData.StartingMarkLevel.Abbreviation, Squad.TypeData.StartingMarkLevel.ColorHex )
                            .Add(".");
                        buffer.EndStatement(Attr_Line);
                        
                        debugstage = 1504;
                        //buffer.EndStatement(Attr_Line);
                    }
                    
                    debugstage = 1505;
                    Faction factionToUse = Squad.GetFactionOrNull_Safe();
                    //if (factionToUse.Type == FactionType.NaturalObject)
                        //factionToUse = Config.LocalPlayerFaction;
                        
                    debugstage = 1506;
                    if (factionToUse == null ||
                        factionToUse.Type == FactionType.NaturalObject ||
                        factionToUse.Type == FactionType.Player ||
                        factionToUse.SpecialFactionData.DoesPlayerStyleShipUpgrades)
                    {
                        debugstage = 1507;
                        var techs = Squad.TypeData.TechUpgradesThatBenefitMe;
                        debugstage = 1508;
                        if (techs.Count > 0)
                        {
                            debugstage = 1509;
                        var show_lines_affected = Config.From == FromSidebarType.Sidebar_MultipleUnits || 
                                                  Config.ExtraFlags.HasFlag(ShipExtraDetailFlags.ShowTechLineCount);
                            
                            buffer.BeginStatement(Attr_Line);
                            buffer.Add( "Techs", Attr_Label ).Add(": ");
                            
                            debugstage = 1510;
                            for ( int i = 0; i < techs.Count; i++ )
                            {
                                if ( i > 0 )
                                    buffer.Add( ", " );
                                
                                var upgrade = techs[i];
                                debugstage = 1511;
                                writer.WriteTech(buffer, upgrade, factionToUse, false, show_lines_affected, ref debugstage);
                            }
                            
                            debugstage = 1515;
                            buffer.EndStatement(Attr_Line);
                        }
                    }
                    
                    //buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region AIReinforcementPointReasonCodesForDebugging
                debugstage = 87403000;
                if ( Squad.AIReinforcementPointReasonCodesForDebugging != null && Squad.AIReinforcementPointReasonCodesForDebugging.Count > 0 )
                {
                    if ( Config.Detail >= TooltipDetail.Medium )
                    {
                        buffer.Add( "Reinforcement Debug Reason Codes: " );
                        bool isFirst = true;
                        RefPair<string, int> content;
                        for ( int i = 0; i < Squad.AIReinforcementPointReasonCodesForDebugging.Count; i++ )
                        {
                            content = Squad.AIReinforcementPointReasonCodesForDebugging[i];
                            if ( content.RightItem > 0 )
                            {
                                if ( isFirst )
                                    isFirst = false;
                                else
                                    buffer.Add( ", " );
                                buffer.Add( content.RightItem ).Add( "x " ).StartColor( QuickColors.NewValue ).Add( content.LeftItem ).Add( "</color>" );
                            }
                        }
                        buffer.Add( "" ).EndStatement(EndStatementStyle.EOL);
                    }
                }
                #endregion

                #region CenterpieceOfFleet
                {
                    writer.WriteFleet(Squad);
                }
                #endregion
                
                #region CitySockets/CitySocketCost
                debugstage = 6470;
                if ( Squad.TypeData.CitySocketCost > 0 || 
                     Squad.TypeData.MinimumRequiredCityLevelForConstruction > 0)
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    buffer.Add("Requires", Attr_Label).Add(": ");
                    
                    buffer.Add(Squad.CityName_FromSocketName(), TextStyle.Brighter);
                        
                    var mark = Squad.TypeData.MinimumRequiredCityLevelForConstruction;
                    if (mark > 0)
                    {
                        var row = Balance_MarkLevelTable.Instance.RowsByOrdinal[mark];
                        buffer.Space("0.25em").StartColor(row.ColorHex).Add(row.MapDisplay).Add("+").EndColor();
                    }
                    
                    if (relatedMemFleetOrNull != null && 
                        Config.PanelMode == Window_InGameHoverEntityInfo.Mode.Build)
                    {
                        var citymark = relatedMemFleetOrNull.Centerpiece.CurrentMarkLevel;
                        if (citymark < mark)
                            buffer.Open(TextStyle.Req_Unmet).AddVarReplace(TextVarMap.Build_Req_Status, "MISSING").Close(TextStyle.Req_Unmet);
                        //else
                            //buffer.Open(TextStyle.Req_Met).AddVarReplace(TextVarMap.Build_Req_Status, "MET").Close(TextStyle.Req_Met);
                    }
                    
                    var cost = Squad.TypeData.CitySocketCost;
                    buffer
                        .Add(" and ")
                        .Open(TextStyle.Color_Count).Add( cost ).Add(Text.Multiply).Close(TextStyle.Color_Count)
                        .Add(Squad.SocketName(cost), TextStyle.Brighter);
                        ;
                    
                    if (relatedMemFleetOrNull != null && 
                        Config.PanelMode == Window_InGameHoverEntityInfo.Mode.Build)
                    {
                        var avail = relatedMemFleetOrNull.CalculateRemainingCitySockets();
                        if (avail <= 0)
                        {
                            buffer.Open(TextStyle.Req_Unmet).AddVarReplace(TextVarMap.Build_Req_Status, "MISSING").Close(TextStyle.Req_Unmet);
                        }
                        else
                        if (avail < cost)
                            buffer.Open(TextStyle.Req_Unmet).AddVarReplace(TextVarMap.Build_Req_Status, "MISSING").Close(TextStyle.Req_Unmet);
                        //{
                        //    buffer.Open(TextStyle.Req_Unmet).AddVarReplace(TextVarMap.Build_Req_Status, (a,b,c,d)=>c.Add("UNMET:").Add(avail)).Close(TextStyle.Req_Unmet);
                        //}
                        else
                        {
                            //uffer.Open(TextStyle.Req_Met).AddVarReplace(TextVarMap.Build_Req_Status, (a,b,c,d)=>c.Add("MET:").Add(avail)).Close(TextStyle.Req_Met);
                        }
                    }
                    
                    buffer.Add(".");
                    
                    buffer.EndStatement(Attr_Line);
                }

                debugstage = 6472;
                
                //only tell about added points if they come from not-the-hub
                // jcf: they always come from the hub?
                /*
                if ( Squad.DataForMark.CitySockets > 0 && Squad.TypeData.NameForCityCenter)
                {
                    buffer
                        .BeginStatement(Attr_Line)
                        .Add( Squad.TypeData.NameForCitySockets_Plural )
                        .Add( " Provided: " )
                        .AddNumber( Squad.DataForMark.CitySockets )
                        .EndStatement(Attr_Line);
                }
                */
                
                #endregion

                #region ShipTypeNameToGrantMoreOfInCustomFleet
                debugstage = 6473;
                if ( Squad.TypeData != null &&
                     Squad.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet != string.Empty )
                {
                    debugstage = 6474;
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName( Squad.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet );
                    
                    buffer.BeginStatement(TextStyle.Newline_NoLabel);
                    
                    buffer
                        .Add( "Grants-Bolstered Fleet" )
                        .Add(": ")
                        .AddShipIconNameFull(typeData)
                        .Open(TextStyle.Color_Count)
                        .Add(Text.Multiply)
                        .Add(Squad.TypeData.ShipTypeCountToGrantMoreOfInCustomFleet)
                        .Close(TextStyle.Color_Count);
                    
                    if ( Config.Detail > TooltipDetail.Medium || 
                         Config.ExtraFlags.HasFlag(ShipExtraDetailFlags.AnyGrantHackInfo) )
                    {
                        buffer.NewLine(2);
                        
                        byte markLevelToUse = localPlayerFactionOrNull?.GetGlobalMarkLevelForShipLine( typeData ) ?? 1;
                        EntityText.GetTooltip( buffer, null, null,
                             typeData, Squad.TypeData.ShipTypeCountToGrantMoreOfInCustomFleet, null, markLevelToUse, FromSidebarType.NonSidebar_MultipleUnits, 
                             ShipExtraDetailFlags.ShownInsideAnother, 1.0f, false );
                    }
                
                    buffer.EndStatement(TextStyle.Newline_NoLabel);
                }
                debugstage = 6476;
                #endregion
                
                #region Stack-of-Ships
                if ( !Squad.IsFakeEntity )
                {
                    debugstage = 5450;
                    if ( Squad.ShipCount > 1 && 
                         Config.Detail >= TooltipDetail.Medium )
                    {
                        buffer.BeginStatement( TextStyle.DarkText );
                            
                        buffer
                            .Add( "This is a stack of " )
                            .Open( TextStyle.Number )
                            .Add( Squad.ShipCount )
                            .Add( " ships")
                            .Close(TextStyle.Number)
                            .Add(".  When the current one dies, the count will go down and the next one pops out." );

                        if ( Config.Detail >= TooltipDetail.Full )
                        {
                            buffer
                                .Add( " This stack takes damage like normal, but shoots " )
                                .Open( TextStyle.Number )
                                .Add( Squad.ShipCount ).Add( "x" )
                                .Close( TextStyle.Number )
                                .Add(" the normal amount of shots." );
                        }
                        
                        buffer.EndStatement(TextStyle.DarkText);
                    }
                }
                #endregion

                debugstage = 5500;

                #region Stationary Flagship Mode
                /*
                if ( !Squad.IsFakeEntity && 
                     Config.Detail > TooltipDetail.SuperShort &&
                     Squad.GetFleetCenterpieceOrNull_Safe() == Squad &&
                     Squad.FleetMembership.Fleet.IsFleetFlagshipStationaryStatusOn )
                {
                    debugstage = 5502;
                    buffer.BeginStatement(Attr_Line).Add( "Stationary Flagship Mode", "ee3198" );
                    if ( Config.Detail > TooltipDetail.Medium )
                    {
                        debugstage = 5503;
                        buffer.Add( "Hold " ).Add( InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldToGiveOrdersToStationaryFlagships" ).GetHumanReadableKeyCombo() )
                            .Add( " to have this flagship listen to orders.  Otherwise, the rest of the fleet listens, the flagship holds onto the orders, and any new ships emerging from the flagship inherit those orders" ).EndStatement(EndStatementStyle.EOL);
                        if ( Config.Detail >= TooltipDetail.Full )
                        {
                            buffer.Add( "You can find out more about this, and change its mode, on the Fleets sidebar tab.  Find this flagship's fleet and click it" ).EndStatement(EndStatementStyle.EOL);
                        }
                    }
                    buffer.EndColor().EndStatement(Attr_Line);
                }
                */
                #endregion

                #region Prefers To Target
                debugstage = 5504;
                if ( !Squad.IsFakeEntity && 
                     orders != null && 
                     Squad.GetFactionTypeSafe() == FactionType.Player && 
                     Config.Detail > TooltipDetail.SuperShort )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    /*
                    debugstage = 5505;
                    if (Squad.GetFleetCenterpieceOrNull_Safe() == Squad &&
                        Squad.FleetMembership.Fleet.IsFleetFlagshipStationaryStatusOn)
                    {
                        buffer.Open(TextStyle.Newline_NoLabel);
                        buffer.Add( "Stationary Flagship Mode", "ee3198" );
                        buffer.Close(TextStyle.Newline_NoLabel);
                    }
                    
                    if ( Squad.StopToShootAnySeenTargets )
                    {
                        buffer.Open(TextStyle.Newline_NoLabel);
                        buffer.Add("Stop-To-Shoot Mode", Window_InGameSelectionInfo.color_StopToShoot);
                        buffer.Close(TextStyle.Newline_NoLabel);
                    }
                    
                    if ( Squad.SpeedLimitFromGroupMove > 0 )
                    {
                        buffer.Open(TextStyle.Newline_NoLabel);
                        buffer.StartColor( Window_InGameSelectionInfo.color_GroupMove ).Add("Group Move Mode" ).Add("  Speed ").Add( Squad.SpeedLimitFromGroupMove ).EndColor();
                        buffer.Close(TextStyle.Newline_NoLabel);
                    }
                    */
                    if ( Squad.PreferredEntityTypeDataForTargeting != null )
                    {
                        buffer.Open(TextStyle.Newline_NoLabel);
                        buffer.StartColor( Window_InGameSelectionInfo.color_AttackMove ).Add("Prefers To Target: ").AddShipIconNameFull( Squad.PreferredEntityTypeDataForTargeting ).EndColor();
                        buffer.Close(TextStyle.Newline_NoLabel);
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region Write Decollision Move Logic
                debugstage = 5510;
                if ( Config.ShowDebugInfo && 
                     !Squad.IsFakeEntity && 
                     Squad.DecollisionMoveTarget != ArcenPoint.ZeroZeroPoint && 
                     !Config.ForMultipleShips && 
                     Config.Detail >= TooltipDetail.Medium )
                {
                    /*
                    buffer.StartColor( QuickColors.HeaderDull ).Add( "Decollision Detour To: " );
                    if ( Squad.DecollisionMoveTarget == ArcenPoint.OutOfRange )
                        buffer.Add( "BUG!  OutOfRange!" );
                    else
                    {
                        ArcenPoint targetPoint = Squad.DecollisionMoveTarget;
                        targetPoint -= Engine_AIW2.Instance.CombatCenter;
                        buffer.Add( targetPoint.X ).Add( "," ).Add( targetPoint.Y );
                    }
                    buffer.EndColor();
                    buffer.Add( "  " );
                    */
                }
                #endregion

                #region Incoming Damage/Shots
                debugstage = 5520;
                if ( !Squad.IsFakeEntity && Config.ShowDebugInfo)
                {
                    int estimatedRemainingDurability = Squad.EstimateRemainingDurabilityAfterAllShots( 0 );
                    int originalDurability = Squad.GetAbsoluteDurabilityOfMyselfAndStack();
                    if ( estimatedRemainingDurability < originalDurability )
                    {
                        int damage = (originalDurability - estimatedRemainingDurability);
                        
                        buffer
                            .BeginStatement(TextStyle.WarnText)
                            .Add( "Incoming Damage Expected: " )
                            .AddNumber( damage );
                        
                        if ( estimatedRemainingDurability <= 0 )
                            buffer.Add( " (Death Is Expected)" );
                        
                        buffer
                            .Add(".")
                            .EndStatement(TextStyle.WarnText);
                    } 
                    else
                    {
                        int incomingShotCount = Squad.GetIncomingShotCount();
                        if ( incomingShotCount > 0 )
                        {
                            buffer
                                .BeginStatement(TextStyle.WarnText)
                                .Add( "Incoming Shots: " )
                                .AddNumber( incomingShotCount )
                                .Add( " (But Their Calculated Damage Is Zero)?" )
                                .EndStatement(TextStyle.WarnText);
                        }
                    }
                }
                #endregion
                
                #region AIReinforcementPointReasonCodesForDebugging
                if ( Squad.AIReinforcementPointReasonCodesForDebugging != null && Squad.AIReinforcementPointReasonCodesForDebugging.Count > 0 )
                {
                    if ( Config.Detail >= TooltipDetail.Medium )
                    {
                        buffer.Add( "Reinforcement Debug Reason Codes: " );
                        bool isFirst = true;
                        RefPair<string, int> content;
                        for ( int i = 0; i < Squad.AIReinforcementPointReasonCodesForDebugging.Count; i++ )
                        {
                            content = Squad.AIReinforcementPointReasonCodesForDebugging[i];
                            if ( content.RightItem > 0 )
                            {
                                if ( isFirst )
                                    isFirst = false;
                                else
                                    buffer.Add( ", " );
                                buffer.Add( content.RightItem ).Add( "x " ).StartColor( QuickColors.NewValue ).Add( content.LeftItem ).Add( "</color>" );
                            }
                        }
                        buffer.Add( "" ).EndStatement(EndStatementStyle.EOL);
                    }
                }
                #endregion

                debugstage = 6000;
                
                // this block only appears if a specific/singular ship
                if ( !Squad.IsFakeEntity && 
                     !Config.ForMultipleShips )
                {
                    #region AreaBoosters
                    debugstage = 6030;
                    if ( Squad.AreaBoosting_CurrentCount.Display > 0 )
                    {
                        buffer
                            .BeginStatement( TextStyle.GreenText )
                            .Add( "Protecting " )
                            .Add( Squad.AreaBoosting_CurrentCount.Display, TextStyle.Number )
                            .Add( " friendlies." )
                            .EndStatement(TextStyle.GreenText);
                    }
                   
                    if ( Squad.AreaBoosters_ShotEvaluated.Count > 0 )
                    {
                        buffer
                            .BeginStatement( TextStyle.GreenText )
                            .Add( "Protected by: " );
                        
                        writer.WriteShips(Squad.AreaBoosters_ShotEvaluated.GetDisplayList());
                        
                        buffer
                            .Add( "." )
                            .EndStatement(TextStyle.GreenText);
                    }
                    #endregion

                    #region RepairImpossibleForSeconds
                    if ( Squad.RepairImpossibleForSeconds > 0 )
                    {
                    // don't bother saying cannot be repaired for ships that are never repairable
                    bool repairable = !Squad.TypeData.ImmuneToRepairs;
                    
                    // don't bother saying cannot be repaired for ships that aren't friendly to a player
                    // after all players are the ones that HAVE a repair mechanic
                    if (localPlayerFactionOrNull == null || !Squad.GetIsFriendlyTowardsSafe(localPlayerFactionOrNull))
                        repairable = false;
                    
                        bool regenable = Squad.TypeData.SecondsToFullyRegenerateHull > FInt.Zero;
                        
                        if (repairable || regenable)
                        {
                            buffer.BeginStatement(TextStyle.WarnText);

                            buffer.Add( "Can not " );
                            
                            if (repairable)
                            {
                                buffer.Add("be ").Add("Repaired", TextStyle.Brighter);
                                if (regenable)
                                    buffer.Add(" or ");
                            }
                            
                            if (regenable)
                                buffer.Add("Regenerate", TextStyle.Brighter);
                                
                            buffer.Add(" for another ").AddMinutesAndSeconds(Squad.RepairImpossibleForSeconds);
                            buffer.EndStatement(TextStyle.WarnText);
                        }
                    }
                    #endregion
                        
                    #region CannotTransportReason
                    debugstage = 6050;
                    if ( Config.Detail >= TooltipDetail.Full && 
                         Squad.PlanetFaction != null &&
                         Squad.PlanetFaction.GetIsLocalFaction() && 
                         !Squad.TypeData.IsFleetLeader &&
                         Squad.TypeData.FleetMembershipStyle != FleetMembershipStyle.Planetary )
                    {
                        CannotTransportReason noTransportBase = Squad.TypeData.GetCanBeTransported();
                        if ( noTransportBase != CannotTransportReason.TranportingIsFine )
                        {
                            //Don't bother telling me about that, actually.
                            //buffer.StartColor( QuickColors.HeaderDull ).Add( "This ship can never be transported" ).EndStatement(EndStatementStyle.EOL).EndColor();
                        } 
                        else
                        {
                            CannotTransportReason noTransport = Squad.GetCanBeTransportedRightNow();
                            if ( noTransport != CannotTransportReason.TranportingIsFine )
                            {
                                buffer
                                    .BeginStatement(TextStyle.WarnText)
                                    .Add( "This ship normally can be transported, but cannot be loaded into any transport right now because " )
                                .Add( Extensions.ToString(noTransport) )
                                .EndStatement(TextStyle.WarnText);
                        }
                    }
                }
                #endregion
                
                    #region ImmuneToCapture
                    // jcf: this shows up on a lot of stuff, and its mostly things that you wouldn't even expect to be capturable
                    //      plus the term 'captured by other factions' is really ambiguous when you see it on a 'capturable' ..
                    if (Config.ShowDebugInfo)
                    {
                        var reasonCaptureImmune = Squad.GetImmuneToCaptureReason();
                        if (reasonCaptureImmune != GameEntity_Squad.CaptureImmuneReason.None)
                        {
                            buffer.BeginStatement(TextStyle.Newline_NoLabel).Add( "Cannot be captured by other factions (" ).Add(Extensions.ToString(reasonCaptureImmune)).Add(")").EndStatement(TextStyle.Newline_NoLabel);
                        }
                    }
                    #endregion

                    #region Ship-Disabled Reason
                    debugstage = 8000;
                    ArcenRejectionReason rejectionReason = Squad.ComputeDisabledReason( ArcenRejectionReason.Unknown );
                    bool alreadyWroteCrippledInfo = false;
                    if ( rejectionReason != ArcenRejectionReason.Unknown )
                    {
                        bool skipBecauseWrittenElsewhere = false;
                        switch ( rejectionReason )
                        {
                            case ArcenRejectionReason.EntityIsParalyzed:
                            case ArcenRejectionReason.EntityIsUnrebuiltRemains:
                            case ArcenRejectionReason.EntityHasNotYetBeenFullyClaimed:
                                skipBecauseWrittenElsewhere = true;
                                break;
                        }
                        if ( !skipBecauseWrittenElsewhere )
                        {
                            //HandleNewline( buffer, ref haveDoneNewLine );
                            buffer.BeginStatement(TextStyle.WarnText).Add( "Ship Disabled: " );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CrippledInseadOfDead:
                                    WriteCrippledInfo( buffer, Squad );
                                    alreadyWroteCrippledInfo = true;
                                    break;
                                case ArcenRejectionReason.NonFunctionalWhenNotOnPlanetOwnedByMyFaction:
                                case ArcenRejectionReason.FactionDoesNotControlThisPlanet:
                                    buffer.Add( "Faction not in control of this planet." );
                                    break;
                                case ArcenRejectionReason.InUnexploredSpace:
                                    buffer.Add( "Cannot function while in Unexplored Space." );
                                    break;
                                //case ArcenRejectionReason.EntityConsideredOutOfSupplyOfParentFleetCenterpiece:
                                //    buffer.Add( "No supply - not on same or adjacent parent to the fleet centerpiece." );
                                //    break;
                                /*
                                case ArcenRejectionReason.EntityHasNotYetBeenFullyClaimed:
                                    buffer.Add( "Not yet fully claimed" );
                                    if ( Squad.IsInHoldFireMode )
                                        buffer.Add( " (paused)");
                                    buffer.Add(".");
                                    break;
                                */
                                case ArcenRejectionReason.EntityIsInHoldFireMode:
                                    buffer.Add( "Ordered to Stand Down." );
                                    break;
                                case ArcenRejectionReason.EntityIsSelfBuilding:
                                    {
                                        float percent = (1f - ((float) Squad.SelfBuildingMetalRemaining / (float) Squad.GetMetalCost())) * 100;
                                        buffer.Add( "Still under construction." );
                                        //buffer.AddPercentageInColor( percent, true, false );
                                        //buffer.Add( " " ).AddFixedDecimalThousands( Squad.SelfBuildingMetalRemaining.ToFloatNonSim(), 1 ).Add( " metal left" );
                                        //buffer.Add( "%)" );
                                        //if ( Squad.GetFactionTypeSafe() == FactionType.Player )
                                        //{
                                        //    if ( Squad.TypeData.EnergyUsage > 0 && 
                                        //         relatedSquadFactionOrNull?.NetEnergy < 0 &&
                                        //         Squad.GetEnergyUsage() > 0 )
                                        //    {
                                        //        buffer.StartColor( ColorMath.LightRed ).Add( " ENERGY SHORTAGE" ).EndColor();
                                        //    }
                                        //}
                                        //buffer.Add( "." );
                                    }
                                    break;
                                //case ArcenRejectionReason.EntityIsUnrebuiltRemains:
                                //    buffer.Add( "Was destroyed and not yet rebuilt." );
                                //    break;
                                case ArcenRejectionReason.FactionDoesNotHaveEnoughEnergy:
                                    buffer.Add( "Faction does not have enough energy." );
                                    break;
                                case ArcenRejectionReason.NotEnoughCitySockets:
                                    buffer.Add( "Not enough " ).Add( Squad.TypeData.NameForCitySockets_Plural ).Add( " at this planet." );
                                    break;
                                case ArcenRejectionReason.MetalIsZero:
                                    buffer.Add( "Stored metal is zero." );
                                    break;
                                default:
                                    buffer.Add( "UNHANDLED_REASON '" ).Add( Extensions.ToString(rejectionReason) ).Add("'");
                                    break;
                            }

                            buffer.EndStatement(TextStyle.WarnText);
                        }
                    }
                    #endregion

                    #region Crippled
                    if ( !alreadyWroteCrippledInfo && 
                         Squad.GetIsCrippled() )
                    {
                        WriteCrippledInfo( buffer, Squad );
                    }
                    #endregion
                    
                    #region Remains, CannotRebuildRemainsReason
                        debugstage = 8050;
                        if ( !Squad.IsFakeEntity && 
                             Squad.SecondsSpentAsRemains >= 0 )
                    {
                        buffer.BeginStatement(TextStyle.WarnText);
                        
                        rejectionReason = Squad.GetCannotRebuildRemainsReason();
                        if ( rejectionReason != ArcenRejectionReason.Unknown )
                        {
                            buffer.Add( "Cannot Rebuild Remains: " );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CannotRebuild_IsNotremains:
                                    buffer.Add( "Is not remains!" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_YesEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "Must wait another " )
                                        .AddMinutesAndSeconds( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuilding - Squad.SecondsSpentAsRemains )
                                        .Add( " while enemies are here." );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_NoEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "Must only wait another " )
                                        .AddMinutesAndSeconds( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuildingNoEnemies - Squad.SecondsSpentAsRemains )
                                        .Add( " since no enemies present." );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_CommandStationOnAIPlanet:
                                    buffer.Add( "Command Stations cannot be rebuilt on enemy planets." );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_WouldPutUsIntoBrownout:
                                    buffer.Add( "Rebuilding would put you into negative energy." );
                                    break;
                                default:
                                    buffer.Add( "ERROR_WRITE_NICE_TEXT: " ).Add( EnumNameCache.GetName( rejectionReason ) );
                                    break;
                            }
                        }
                        else
                        {
                            if ( Config.Detail < TooltipDetail.Full )
                                buffer.Add( "This is only the broken remains of the unit." );
                            else
                                buffer.Add( "This is only the broken remains of the unit. Remains do nothing directly but can be rebuilt." );
                        }
                        
                        buffer.EndStatement(TextStyle.WarnText);
                    }
                    #endregion

                    #region BubbleShield Brownout
                    debugstage = 8100;
                    Faction facOrNull = Squad.GetFactionOrNull_Safe();
                    if ( Squad.GetHasBubbleForcefieldRightNow() && 
                         Squad.GetFactionTypeSafe() == FactionType.Player &&
                         facOrNull != null && 
                         facOrNull.SecondsSinceBrownout >= 0 )
                    {
                        var sec = ExternalConstants.Instance.SecondsToWaitBeforeBrownoutEnds - facOrNull.SecondsSinceBrownout;
                        
                        buffer.BeginStatement(TextStyle.WarnText);
                        
                        buffer.Add( "Brownout: Bubble forcefields down for another " ).AddMinutesAndSeconds(sec);
                        
                        buffer.EndStatement(TextStyle.WarnText);
                    }
                    #endregion

                    #region Cannot Be Claimed
                    debugstage = 8200;
                    if ( !Squad.IsFakeEntity && Squad.GetMetalToClaimRemaining() > 0 )
                    {
                        int secondsUntilClaim = localPlayerPlanetFactionOrNull == null ? 0 : Squad.GetRemainingSecondsBeforeCanClaim( localPlayerPlanetFactionOrNull );
                        if ( secondsUntilClaim > 0 )
                        {
                            buffer
                                .BeginStatement(TextStyle.WarnText)
                                .Add( "Cannot be claimed for another " ).AddMinutesAndSeconds( secondsUntilClaim )
                                .EndStatement(TextStyle.WarnText);
                        }
                    }

                    debugstage = 8300;
                    int energy_missing = Squad.TypeData.EnergyUsage - localPlayerFactionOrNull.NetEnergy;
                    if ( Squad.HasNotYetBeenFullyClaimed && 
                         localPlayerFactionOrNull != null &&
                         Squad.TypeData.EnergyUsage > 0 && 
                         energy_missing > 0 )
                    {
                        buffer
                            .BeginStatement(TextStyle.WarnText)
                            .Add( "Cannot be claimed because you are short ")
                            .AddNumber(energy_missing, TextTerm.Energy, TermUse.Icon_Name)
                            .EndStatement(TextStyle.WarnText);
                }
                #endregion
                
                    #region ActiveHack
                    debugstage = 87394000;
                    if ( Squad.ActiveHack != null )
                    {
                        buffer.BeginStatement(TextStyle.WarnText);
                        debugstage = 87394010;
                        buffer.Add( "This unit is currently hacking. While doing so it is slowed, decloaked, and cannot leave the planet." );
                        buffer.EndStatement(TextStyle.WarnText);
                    }
                    #endregion
                }

                debugstage = 9005;
            }
            catch (System.Threading.ThreadAbortException) { }
            catch (Exception e)
            {
                LOG.Err("error at debugstage {0}\n{1}", debugstage, e);
            }
            finally
            {
            }
        }

        #region Private Helpers
        
        private static void WriteCrippledInfo( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull )
        {
            buffer.BeginStatement( TextStyle.WarnText );
            
            int cost = (int)(relatedSquadOrNull.TypeData.GetExtraCostWhileCrippled().ToFloat() * relatedSquadOrNull.GetMetalCost());

            buffer.Add( "Crippled: Will not die, but needs to be repaired" );
            if (!relatedSquadOrNull.TypeData.ImmuneToRepairs)
            {
                buffer.Add(" ").AddVarReplace(TextVarMap.Parenthetical, 
                    (a,b,c,d)=>
                    { 
                        c.AddNumber(cost, TextTerm.Metal, TermUse.Icon);
                    },
                    null );
            }
            buffer.Add(" to full health to function again" );

            buffer.EndStatement( TextStyle.WarnText );
        }

        private static bool WritePlannedMetalFlowBriefTargetInfo( ArcenCharacterBufferBase buffer, PlannedMetalFlow flow, string Prefix, bool WriteFull )
        {
            if ( flow.FromEntity == null )
                return false;
            GameEntity_Squad squad = flow.SquadRecipient;
            if ( squad == null )
                return false;

            buffer.StartColor( "70ff59" ); //bright green
            buffer.Add( " (" );
            if ( Prefix != null && Prefix.Length > 0 )
                buffer.Add( Prefix ).Add( ": " );
            buffer.StartColor( squad.GetFactionCenterColorHexBrighter_Safe() );
            buffer.Add( WriteFull ? squad.TypeData.DisplayName : squad.FleetMembership?.GetDisplayNameForSidebar() ).EndColor();
            if ( WriteFull && squad.CurrentMarkLevel > 0 )
                buffer.Add( " " ).StartColor( squad.DataForMark.MarkLevel.ColorHex ).Add( squad.DataForMark.MarkLevel.Abbreviation ).EndColor();
            buffer.Add( ")" ).EndColor();
            return true;
        }

        private static bool WritePlannedMetalFlowFullTargetInfo( ArcenCharacterBufferBase buffer, PlannedMetalFlow flow, GameEntity_Squad entity, string Prefix, bool WriteFull )
        {
            if ( flow.FromEntity == null )
            {
                buffer.Add( "[null flow]" );
                return false;
            }
            GameEntity_Squad squad = flow.SquadRecipient;
            if ( Prefix != null && Prefix.Length > 0 )
                buffer.Add( Prefix ).Add( ": " );

            bool addedRecipient = false;
            if ( squad != null )
            {
                buffer.StartColor( squad.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( WriteFull ? squad.TypeData.DisplayName : squad.FleetMembership?.GetDisplayNameForSidebar() ).EndColor();
                if ( WriteFull && squad.CurrentMarkLevel > 0 )
                {
                    buffer.Add( " " ).StartColor( squad.DataForMark.MarkLevel.ColorHex ).Add( squad.DataForMark.MarkLevel.Abbreviation ).EndColor();
                }
                if ( WriteFull && GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) )
                {
                    buffer.Add( " (dist " ).Add( flow.FromEntity.GetDistanceTo_ExpensiveAccurate( squad, RadiusCheck.SubtractRadiiFromDistance, true ) ).Add( ")" );
                }
                addedRecipient = true;
            }
            switch ( flow.Purpose )
            {
                case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                    {
                        if ( entity == null )
                            break;
                        PlanetFaction pFaction = entity.PlanetFaction;
                        if ( pFaction == null )
                            break;
                        DoubleBufferedList<FleetMembership> factoryTargets = pFaction.FactoryBoostsBySpecialFactoryType.GetListForOrNull( entity.TypeData.SpecialFactoryType );
                        if ( factoryTargets == null || factoryTargets.Count == 0 )
                            break;
                        List<FleetMembership> factoryTargetsFinal = factoryTargets.GetDisplayList();
                        if ( factoryTargetsFinal == null || factoryTargetsFinal.Count == 0 )
                            break;

                        for ( int i = 0; i < factoryTargetsFinal.Count; i++ )
                        {
                            FleetMembership fMem = factoryTargetsFinal[i];
                            if ( i > 0 )
                                buffer.Add( ", " );
                            buffer.StartColor( fMem.Fleet.Faction.FactionCenterColor.ColorHexBrighter );
                            buffer.Add( WriteFull ? fMem.TypeData.DisplayName : fMem.GetDisplayNameForSidebar() ).EndColor();
                            addedRecipient = true;
                        }
                    }
                    break;
            }
            if ( !addedRecipient )
                buffer.Add( "[no recipients]" );
            return true;
        }

        
        
        private static Color GetProportionalStrengthColor( float ratio )
        {
            //returns a color that indicates how much of the total available strength this unit has
            Color lowHealthFleet = ColorMath.FromRGB( 246, 50, 50 );
            Color highHealthFleet = ColorMath.FromRGB( 75, 244, 170 );
            return Color.Lerp( lowHealthFleet, highHealthFleet, ratio );
        }
        
        #endregion
    }
    #endregion
    
    #region EntityAttributeCollection
    public class EntityAttributeCollection : List<EntityAttrText>, IRapidAntiLeakPoolable<EntityAttributeCollection>
    {
        public GameEntity_Squad ForEntity;
        
        private EntityAttributeCollection( int capacity, string locationNameForTracing, bool shouldBeLoggedCentrally, bool shouldCheckIfInvalidToBeInCollection, int capacityIncreasesByOrDoublesEachTimeIfZero ) 
            : base(capacity, locationNameForTracing, shouldBeLoggedCentrally, shouldCheckIfInvalidToBeInCollection, capacityIncreasesByOrDoublesEachTimeIfZero)
        {
        }

        private EntityAttributeCollection()
            : base(6, "unknown", false, false, 3)
        {
        }
        
        public EntityAttributeCollection(GameEntity_Squad e)
            : base(6, "unknown", false, false, 3)
        {
            ForEntity = e;
        }

        #region IRapidAntiLeakPoolable
        private bool IsInRapidPool = false;
        private string RapidPoolName = string.Empty;
        private float RapidPoolExpirationTime = 0;

        bool IRapidAntiLeakPoolable<EntityAttributeCollection>.GetInRapidAntiLeakPoolStatus()
        {
            return this.IsInRapidPool;
        }

        void IRapidAntiLeakPoolable<EntityAttributeCollection>.SetInRapidAntiLeakPoolStatus( bool InPool )
        {
            this.IsInRapidPool = InPool;
            this.RapidPoolExpirationTime = 0;
        }

        void IRapidAntiLeakPoolable<EntityAttributeCollection>.SetNameAndTimeAfterWhichToDeclareLeak( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            this.RapidPoolName = Name;
            this.RapidPoolExpirationTime = ArcenTime.TimeSinceStartF + SecondsAfterWhichToDeclareLeak;
        }

        void IRapidAntiLeakPoolable<EntityAttributeCollection>.RaiseErrorIfExpiredTimeForLeak()
        {
            if ( this.IsInRapidPool || this.RapidPoolExpirationTime <= 0 )
                return; //nothing to worry about here

            if ( this.RapidPoolExpirationTime > ArcenTime.TimeSinceStartF )
                return; //has not expired yet

            this.RapidPoolExpirationTime = 0; //prevent repeat error spam from one leak

            ArcenDebugging.ArcenDebugLogSingleLine( "WARNING ONLY UNLESS HUGE REPEATS: RapidAntiLeakPoolable expiration for EntityAttributeCollection '" + this.RapidPoolName + "' of type " +
            this.GetType(), Verbosity.DoNotShow );
        }

        void IRapidAntiLeakPoolable<EntityAttributeCollection>.DoCleanupWhenComingOutOfRapidAntiLeakPool()
        {
            this.Clear();
        }
        #endregion

        #region Temporary Lists
        private static RapidAntiLeakPool<EntityAttributeCollection> InnerPool = RapidAntiLeakPool<EntityAttributeCollection>.Create_WillNeverBeGCed(
            "InnerPoolFor_EntityAttributeCollection", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return new EntityAttributeCollection( 6, "TempEntityAttributeCollection", false, false, 3 ); } );

        public static EntityAttributeCollection GetTemporary( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            var result = InnerPool.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( result == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            result.Clear();
            return result;
        }

        public static void ReleaseTemporary( EntityAttributeCollection item )
        {
            InnerPool.ReturnToPool( item );
        }
        #endregion
    }
    #endregion
    
    #region Extensions, Static Helpers
    public static partial class EntityText
    {
        public static void EnumerateAttributes( GameEntity_Squad e, EntityAttributeCollection attributes, EntityText.Config cfg )
        {
            for (int i = (int)EntityAttrText.AttributeType.First; i < (int)EntityAttrText.AttributeType.Length; i++)
            {
                var type = (EntityAttrText.AttributeType)i;
                var attr = new EntityAttrText(type, e, cfg);
                attributes.Add(attr);
            }
                 
            // SortOrder:
            // ... currently the enum value

            attributes.StableSort(
                static (a, b)=>
                    {
                        return a.SortOrder.CompareTo(b.SortOrder);
                    } );
        }
    }
    
    public static partial class Extensions
    {
        public static ArcenCharacterBufferBase AddTypeGrantingInvul( this ArcenCharacterBufferBase buffer, GameEntity_Squad squad )
        {
            var special_type = squad.TypeData.ExternalInvulnerabilityUnitType;
            var tag = squad.TypeData.ExternalInvulnerabilityUnitTag;
            
            if ( special_type != SpecialEntityType.None )
            {
                return buffer.Add( Extensions.ToString(special_type) );
            }
            
            if ( !string.IsNullOrWhiteSpace(tag) )
            {
                if ( tag == "AICommandStationOriginal" )
                    return buffer.Add( "AI Command Station" );
                
                for (int i = 0; i < tag.Length; i++)
                {
                    var c = tag[i];
                    if (!char.IsLetterOrDigit(c))
                    {
                        if (i > 0 && tag[i-1] != ' ')
                        {
                            buffer.Add(" ");
                        }
                        
                        continue;
                    }
                    
                    if (char.IsUpper(c))
                    {
                        if (i > 0)
                        {
                            buffer.Add(" ");
                        }
                    }
                    
                    buffer.Add(c);
                }
                
                buffer.Add("(s)");
            }
            
            return buffer;
        }
    }
    #endregion
}

