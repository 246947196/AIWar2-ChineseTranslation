
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
                        buffer.Add( "火力组 " ).Add( team.FireTeamID, "10ffdd" ).Add( " 为 " );
                        team.GetStatusForDisplay( buffer );
                        buffer.Add( "��" );
                        if ( team.Target != null && Config.ShowDebugInfo )
                            buffer.Add( "目标是 " + team.Target.ToStringWithPlanetAndOwner() ).Add( "\n" );
                        team.GetSpecificationForDisplay( buffer );
                        if ( team != null && team.History != null && team.History.Count > 0 && team.FireTeamID > 0 )
                        {
                            buffer.Add( "单位火力组历史：\n " );
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
                                .Add( "受到伤害后，" )
                                .AddMinutesAndSeconds( time )
                                .Add( " 内无法回复或修复。" )
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
                            buffer.Add( "将在战斗中" )
                                .AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" ).Add( " 后过期。" );
                        else
                            buffer.Add( "将在 " ).AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" )
                                .Add( " 后过期。" );
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
                                        .Add( "将在战斗中于 " )
                                        .AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" )
                                        .Add( " 后转化为 " )
                                        .Add( intoType.GetDisplayName(), "a1ffa1" )
                                        .Add( "。" );
                            }
                            else
                            {
                                buffer
                                    .Add( "此 " )
                                    .Add( intoType.GetDisplayName(), "a1ffa1" )
                                    .Add( " 正在跃迁进入，将在 " )
                                    .AddMinutesAndSeconds( Squad.SecondsTillTransformation, "ffa1a1" )
                                    .Add( " 后完全生成。" );
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
                                buffer.Add( "生产 " );
                                break;
                            case ResourceType.Energy:
                                buffer.Add( "产生 " );
                                break;
                            case ResourceType.Hacking:
                            case ResourceType.Science:
                                buffer.Add( "收集 " );
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
                                    buffer.Add( " 并储存 " ).Open(term, TermUse.Icon).AddNumber(Squad.DataForMark.MetalStorage, null, TextStyle.Empty).Close(term).Add("。");
                                }
                                break;
                            case ResourceType.Hacking:
                                buffer.Add( " 当位于有剩余黑客点的星球时。" );
                                break;
                            case ResourceType.Science:
                                buffer.Add( " 当位于有剩余科技的星球时。" );
                                break;
                            case ResourceType.FuelArgon:
                                buffer.Add( "（用于主力战斗舰船）。" );
                                break;
                            case ResourceType.FuelRadon:
                                buffer.Add( "（用于炮塔和力场）。" );
                                break;
                            case ResourceType.FuelXenon:
                                buffer.Add( "（用于军官和精英）。" );
                                break;
                        }
                    }
                    
                    if ( !handledMetalStorage )
                    {
                        if ( Squad.DataForMark.MetalStorage > 0 )
                        {
                            handledMetalStorage = true;
                            buffer.Add( "储存 " ).Open(TextTerm.Metal, TermUse.Icon).AddNumber(Squad.DataForMark.MetalStorage, null, TextStyle.Empty).Close(TextTerm.Metal).Add("。");
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
                                .Add("改变行星的")
                                .AddShipIconNameShort(mine_type).Add("的生产倍率：");
    
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
                            .Add( "提升").Add(res_type.Term(), TermUse.Icon).Add("的生产倍率：").Open(TextStyle.Number).AddMultiplier( res_boost ).Close(TextStyle.Number).Add( "（作用于本行星）。" )
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
                        
                        buffer.Add( "资源增幅器", TextStyle.Attr_Label).Add("： ");
                        
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
                        
                    buffer.Add(" 如果 ").AddTermRange(TermRange.Alloc(TextTerm.TimeOnPlanet, TermCmp.Greater, boost_delay));
                        
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
                                (x,y,b,z) => b.Add("星球非友方", TextStyle.Color_DoesNotApply.Color));
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
                    
                    buffer.Add( "再生器", Attr_Label).Add("： ");
                    
                    buffer.StartHull(true).AddNumberTruncated( avail );
                    if (avail < max)
                        buffer.Open(TextStyle.Fraction).Add( "/" ).AddNumberTruncated( max ).Close(TextStyle.Fraction);
                    buffer.EndHullWrapper(true);
                    
                    if (!Squad.IsFakeEntity)
                    {
                        buffer.Add(" 点用于重生垂死盟友。");
                    }
                    else
                    {
                        buffer.Add(" 点用于重生垂死盟友。");
                    }

                    if (Config.Detail > TooltipDetail.Medium)
                    {
                        buffer.Open(Attr_Line2);
                        var ratio = Squad.TypeData.RegeneratesDyingShipsAtThisHealthCostRatio.ToRounded(2).ToFloatNonSim();
                        buffer.InCase(TextCaps.FirstLetter).Add("比率", Attr_Label2).EndCase().Add("： ").Add("再生 1 船体值需消耗 ").AddFormat(ratio, "{0:#,##0.##}").Add("。");
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
                    .Add( "快速部署", Attr_Label)
                    .Add("：加成 ").AddNumber(()=>{ buffer.AddMultiplier(bonus, TextStyle.Empty); }, null, TextTerm.Speed, TermUse.Icon, null)
                    .Add(" 当 ").AddTermRange(TermRange.Alloc(TextTerm.TimeOnPlanet, TermCmp.Less, 5));
                
                if (Config.Detail >= TooltipDetail.Full)
                    buffer.Add("（加载时间计入）。");
                
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
                    buffer.Add( "复制器", Attr_Label).Add("： ");
                    if (cap == 0)
                    {
                        buffer.Add("建造更多 ").WriteSpawn(type_made).Add(" 在造成伤害时积累，上限为 ").WriteSpawn(type_for_cap).Add(" 在舰队中 ");
                    }
                    else
                    {
                        buffer
                            .Add("最多建造 ")
                            .AddNumber(cap, Text.Count, TextStyle.Color_Count, null)
                            .WriteSpawn(type_made)
                            .Add(" 在造成伤害时 ");
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
                                        c.Add("秒");
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
                                        c.AddColor("/秒",Color.gray.GetHexCode());//TextStyle.Fraction_Gray);
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
                        .Add( "AI 加速器", Attr_Label)
                        .Add("：本星球的 AI 增援增加 " )
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
                        .Add( "黑洞效应", Attr_Label)
                        .Add("：敌方舰船低于 " )
                        .AddNumber(Squad.TypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan, TextTerm.Engine_gX, TermUse.Icon)
                        .Add( " 无法离开此行星（除非残废）。" );
                    
                    buffer.EndStatement(Attr_Line);
                }
                else 
                if ( Squad.TypeData.AddsBlackHoleEffectForAllEntitiesPeriod )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "超级黑洞效应", Attr_Label).Add("：任何阵营或状态的舰船都无法离开此星球。" );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion 

                #region Aggro-Invisible
                debugstage = 425;
                if ( Squad.TypeData.CannotTargetOrAlertAIReinforcementSpots )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "隐形仇恨", Attr_Label);
                    if ( Config.Detail < TooltipDetail.Medium )
                        buffer.Add("  ");
                    else
                        buffer.Add("：当有守卫加载时无法攻击哨站或其他增援点。" );
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
                        buffer.Add( "巢穴", Attr_Label ).Add("： ");
                        Squad.TypeData.PeriodicSpawn_EntityTypeDrawingBag.Value.WriteToBuffer( buffer, Squad.TypeData, false );

                        var spawnFor = Squad.TypeData.Periodic_SpawnFactionForUnit;
                        var forFaction = relatedSquadFactionOrNull?.TryGetAISentinelsCoreData()?.GetAiSubFaction(spawnFor);
                        if (forFaction != null)
                        {
                            buffer.Add( " 归属 " ).AddFactionNameInItsColor( forFaction );
                        }
                        else
                        {
                            buffer.Add( " 归属 " ).Add(Extensions.ToString(spawnFor), TextStyle.Emphasis);
                        }
                    } 
                    else
                    {
                        if ( Squad.TypeData.PeriodicSpawn_CreatesWave && 
                             Squad.TypeData.PeriodicSpawn_CreatesExoStrike )
                        {
buffer.Add( "Exo/Raid Engine: Spawns waves and exo strikes" );
                    buffer.Add( "Exo-Raid Engine:", Attr_Label ).Add("：触发一波进攻和远征打击");
                        } 
                        else 
                        if ( Squad.TypeData.PeriodicSpawn_CreatesExoStrike )
                        {
                            buffer.Add( "Exo Engine:", Attr_Label ).Add("：触发远征打击");
                        } 
                        else
                        {
                            buffer.Add( "Raid Engine:", Attr_Label ).Add("：触发一波进攻");
                        }
                        
                        buffer
                            .Add( " <color=#ffdf72>" )
                            .Add( Squad.TypeData.PeriodicSpawn_WaveOrExoSizeMultiplier.ReadableString )
                            .Add( "x </color> 标准强度" );
                    }

                    buffer.Add( " <color=#ffdf72>每 " ).Add( Squad.TypeData.PeriodicSpawn_DelayBetweenSpawns ).Add( " 秒</color>" );
                    if ( Squad.TypeData.PeriodicSpawn_InitialDelay > 0 )
                        buffer.Add( " 在 <color=#ffdf72>" ).Add( Squad.TypeData.PeriodicSpawn_InitialDelay ).Add( " 秒</color> 后" );
                    else
                        buffer.Add( " 立即" );

                    buffer.Add( " 当" );
                    if ( Squad.TypeData.PeriodicSpawn_MinHostileStrengthToTrigger > 0 )
                        buffer.Add( " 至少 " ).WrapStrengthTruncated( Squad.TypeData.PeriodicSpawn_MinHostileStrengthToTrigger, Config.UseIcons, Config.UseText ).Add( " 敌方" );
                    if ( Squad.TypeData.PeriodicSpawn_OnlyTriggerAgainstPlayer )
                        buffer.Add( " 玩家" );
                    buffer.Add( " 存在时触发" );
                    
                    if ( Squad.TypeData.PeriodicSpawn_OnlyTriggerOnOccupation )
                        buffer.Add( " 占领星球时" );
                    else
                    {
                        buffer.Add( " 在此星球上" );
                        if ( Squad.TypeData.PeriodicSpawn_MaxHopsToTrigger > 0 )
                            buffer.Add( " 且" );
                    }
                    
                    if ( Squad.TypeData.PeriodicSpawn_MaxHopsToTrigger > 0 )
                    {
                        buffer.Add( " 在 <color=#ffdf72>" ).Add( Squad.TypeData.PeriodicSpawn_MaxHopsToTrigger ).Add( "</color> 跳内。" );
                    }
                    
                    if ( Squad.TypeData.PeriodicSpawn_NeverStopOnceTriggered )
                        buffer.Add( " 一旦首次生成，只要银河中仍有有效敌人就不会停止。" );
                    
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
                        buffer.Add( "求援", Attr_Label).Add("：所有守卫单位在 <color=#ffdf72>" ).Add(
                            Squad.TypeData.NumberOfHopsOutToFreeGuards ).Add( " 个虫洞跃迁</color>内将变为威胁（可能加入猎杀舰队），如果来袭敌军强度超过 <color=#ffdf72> " ).Add(
                            Squad.TypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "x</color> 此星球上的 AI 军力。" );
                    }
                    else
                    {
                        buffer.Add( "驱散守卫", Attr_Label).Add("：本星球上所有守卫单位将变为威胁（可能加入猎杀舰队），如果本地 AI 强度低于 <color=#ffdf72> " ).Add(
                            Squad.TypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "x</color> 此星球的敌方强度。" );
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
                    buffer.Add( "侦察", Attr_Label).Add("：监视 " ).Add( hopCount, "ffdf72" );
                    if ( hopCount > 1 )
                        buffer.Add( " 跳。" );
                    else
                        buffer.Add( " 跳。" );
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region Norris Effect
                debugstage = 460;
                if ( Squad.TypeData.PushesEnemyShields )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "诺里斯效应", Attr_Label).Add("：移动时驱散敌方球状力场发生器。" );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region DisallowKiting
                debugstage = 465;
                if ( Squad.TypeData.DisallowKiting && 
                     Config.Detail >= TooltipDetail.Full )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "绝不允许放风筝。" );
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
                        .Add("防御上限倍率", Attr_Label)
                        .StartColor("72ffbe")
                        .Add("：授予的防线为 ")
                        .AddMultiplier(Squad.TypeData.DefensiveStructureCap_Multiplier)
                        .Add(" 的基础值。")
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
                        buffer.Add( "在速度组 " ).Add( groupOnHost.SpeedGroupID );
                    else
                        buffer.Add( "在速度组" );
                    
                    buffer
                        .Add( "，组速 " )
                        .WrapSpeedMoreReadable( Squad.SpeedLimitFromGroupMove, false, false )
                        .Add( "，计算速度 " )
                        .WrapSpeedMoreReadable( Squad.SpeedLimitFromGroupMove, false, false )
                        .Add( "" );
                    
                    if ( groupOnHost != null && groupOnHost.OverrideSpeedLimit > 0 )
                        buffer.Add( "，覆盖速度 " ).WrapSpeedMoreReadable( groupOnHost.OverrideSpeedLimit, false, false ).Add( "" );
                    
                    buffer
                        .Add( "，相对于原速 " )
                        .WrapSpeedMoreReadable( Squad.DataForMark.Speed, false, false )
                        .Add( "。" );
                    
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

                    buffer.Add( "环绕 " );
                    if ( Squad.TypeData.OrbitsGravityWellCenterAtItsCurrentRadius )
                    {
                        buffer.AddColor("引力井", "#ffffff");
                    } 
                    else 
                    if ( Squad.TypeData.OrbitsParentAtRange > 0 )
                    {
                        buffer.AddColor("祖先", "#ffffff");
                    } 
                    else 
                    if ( Squad.TypeData.OrbitsFlagshipAtRange > 0 )
                    {
                        buffer.AddColor("旗舰", "#ffffff");
                    }
                    
                    buffer
                        .Add( " 以 " )
                        .Open(TextTerm.Speed, TermUse.Color)
                        .AddNumber( Squad.TypeData.DegreesToOrbitPerSecond, "°/s", TextStyle.Empty )
                        .Close(TextTerm.Speed);
                    
                    int range = 0;
                    range = Mathf.Max(range, Squad.TypeData.OrbitsParentAtRange);
                    range = Mathf.Max(range, Squad.TypeData.OrbitsFlagshipAtRange);
                    
                    if (range > 0)
                    {
                        buffer
                            .Add(" 于 ")
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
                        .Add( "电毒", Attr_Label ).Add("： ")
                        .Add("返还 ")
                        .Open(TextStyle.Number).AddPercent( etoxic_amt ).Close(TextStyle.Number)
                        .Add( " 的 ").Add(TextTerm.Damage, TermUse.Name).Add(" 以 ").Add(TextTerm.Damage_Exotic, TermUse.Name).Add("。");
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
                            .AddModuleTag(Squad.TypeData.IsForcefieldProvidedByAnyModule).Add( "球形护盾", Attr_Label )
                            
                            .StartSize("75%")
                            .Add("：投射其 ")
                            .AddNumber(Squad.GetMaxShieldPoints(), TextTerm.Shields, TermUse.Icon_Name)
                            .Add(" 作为 ")
                            .AddNumber(effectiveShieldRadius, TextTerm.Range, TermUse.Name).Add(" 力场。")
                            .EndSize();
                    }

                    
                    if ( Config.Detail >= TooltipDetail.Medium )
                    {
                        buffer.Open(TextStyle.Attr_Sub_Lines);
                        
                        bool blocking = !Squad.TypeData.OriginalXmlData.GetBool( "custom_forcefield_is_nonblocking", false, false );
                        if (!blocking)
                        {
                            buffer
                                .Open(Attr_Line2).Add( "薄型", Attr_Label2 )
                                .Add("：不阻挡移动，仅阻挡武器火力。").Close(Attr_Line2);
                        }
                        
                        if ( Squad.TypeData.MyForcefieldDoesNotShrink )
                        {
                            buffer
                                .Open(Attr_Line2).Add( "硬化", Attr_Label2 )
                                .Add("：护盾 ").Add(TextTerm.Range, TermUse.Name).Add(" 在受伤时不会降低。" ).Close(Attr_Line2);
                        }
                        else if ( Config.Detail >= TooltipDetail.Medium )
                        {
                            buffer
                                .Open(Attr_Line2).Add( "软性", Attr_Label2 )
                                .Add("：护盾 ").Add(TextTerm.Range, TermUse.Name).Add( " 在受伤时降低。" ).Close(Attr_Line2);
                        }
                        
                        if ( Squad.TypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                        {
                            buffer
                                .Open(Attr_Line2).Add( "增强", Attr_Label2 )
                                .Add("：受护盾保护的盟友的输出伤害不会降低。" ).Close(Attr_Line2);
                        }
                        else
                        {
                            buffer
                                .Open(Attr_Line2).Add( "减弱", Attr_Label2 )
                                .Add("：受护盾保护的盟友的输出伤害降低 ").AddNumber( "50%", TextTerm.Damage, TermUse.Icon ).Close(Attr_Line2);
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
                        .Add( "损耗者", Attr_Label)
                        .Add("：持续造成全行星范围 ")
                        .Add(TextTerm.Damage_Exotic, TermUse.Name)
                        .Add(" ")
                        .AddNumber(amount, "/s", TextStyle.Number);
                    
                    if ( max <= 0 )
                    { 
                        buffer.Add("（无上限）。");
                    }
                    else
                    {
                        buffer
                            .Add(" 上限为 ")
                            .AddNumber( max )
                            .Add("。");
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
                            .Add( "硬化", Attr_Label)
                            .Add("：单次承受伤害不超过 " )
                            .Add( val )
                            .Add( "% 最大船体生命值的伤害。" );
                    }
                    else
                    {
                        buffer
                            .Add( "硬化", Attr_Label)
                            .Add("：任何单次来源的伤害（爆炸、射击、损耗等）将降低至 " )
                            .Add( val )
                            .Add( "% 此单位最大船体生命值（若超过该值）。防止巨型火炮、离子炮、质量驱动器等。" );
                    }
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion
                
                #region CreatesCeasefireOnPlanet
                debugstage = 562;
                if ( Squad.TypeData.CreatesCeasefireOnPlanet )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "停火", Attr_Label).Add("：阻止本星球上所有单位开火。" );
                    buffer.EndStatement(Attr_Line);
                }
                #endregion

                #region BlocksCeasefireOnPlanet
                if ( Squad.TypeData.BlocksCeasefireOnPlanet )
                {
                    buffer.BeginStatement(Attr_Line);
                    buffer.Add( "停火阻挡者", Attr_Label).Add("：如果在本星球上，则无法停火。" );
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
                        .Add( "谐波", Attr_Label)
                        .Add("：加成 ")
                        .AddNumber( bonus, "+", TextTerm.Damage, TermUse.Icon )
                        .Add(" 每艘此类舰船（在本星球上），上限 ")
                        .AddNumber( cap, "+", TextTerm.Damage, TermUse.Icon )
                        .Add("。");
                    
                    buffer.EndStatement(Attr_Line);
                }
                #endregion


                #region HackingEffectMultiplier
                debugstage = 700;
                if ( Squad.TypeData.HackingEffectMultiplier != FInt.One )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    if ( Squad.TypeData.HackingEffectMultiplier < FInt.One )
                        buffer.Add( "黑客加成", Attr_Label);
                    else
                        buffer.Add( "黑客惩罚", Attr_Label);
                    buffer.Add( "：此单位进行的所有黑客入侵的响应时间倍率为 <color=#ffdf72>" ).AddNumberMoreReadable( Squad.TypeData.HackingEffectMultiplier ).Add( "x</color>。" );
                    
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
                        buffer.Add( "<color=#f2ae1c>舰队加成已禁用：</color> <color=#e0c266>由于银河设置（可能与您的战役类型有关），不允许使用舰队加成。</color>" );
                    }
                    else
                    {
                        int superchargeBonusLimiter = 0;
                        bool isSuperchargeLimited = false;
                        string fleetBonusPrefix = "<color=#7cf21c>舰队加成：</color> <color=#9ce066>";
                        if ( Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 && 
                             relatedMembershipOrNull != null && 
                             relatedMemFleetOrNull != null )
                        {
                            superchargeBonusLimiter = relatedMemFleetOrNull.GetCountOfShipLinesForSuperchargePurposes( null );
                            if ( superchargeBonusLimiter > Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                            {
                                isSuperchargeLimited = true;
                                fleetBonusPrefix = "<color=#f2ae1c>舰队加成关闭：</color> <color=#e0c266>";
                            } 
                            else
                            {
                                fleetBonusPrefix = "<color=#7cf21c>舰队加成开启：</color> <color=#9ce066>";
                            }
                        }

                        if ( Squad.TypeData.SuperchargesMinSpeedOfRestOfPlayerFleet )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "超充所有其他非旗舰舰队成员，使其速度至少与自身相同。</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesHullOfRestOfPlayerFleet > FInt.One )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "赋予 " ).AddFixedDecimal( Squad.TypeData.SuperchargesHullOfRestOfPlayerFleet.ToDouble(), 2 ).Add( "x 船体强度加成予所有舰队成员（包括旗舰）。</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesShieldsOfRestOfPlayerFleet > FInt.One )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "赋予 " ).AddFixedDecimal( Squad.TypeData.SuperchargesShieldsOfRestOfPlayerFleet.ToDouble(), 2 ).Add( "x 护盾强度加成予所有舰队成员（包括旗舰）。</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesAttackPowerOfRestOfPlayerFleet > FInt.One )
                        {
                            buffer.Add( fleetBonusPrefix ).Add( "赋予 " ).AddFixedDecimal( Squad.TypeData.SuperchargesAttackPowerOfRestOfPlayerFleet.ToDouble(), 2 ).Add( "x 攻击力加成予所有舰队成员（包括旗舰）。</color>  " );
                        }
                        
                        if ( Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                        {
                            if ( isSuperchargeLimited )
                            {
                                buffer.Add( "<color=#f2ae1c>舰队加成限制已超：</color> <color=#e0c266>舰队加成仅在拥有 " ).Add(
                                    Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                                    .Add( " 条或更少非旗舰、非精英舰船线时启用，但当前舰队中有 " ).Add( superchargeBonusLimiter ).Add( " 条。</color>" );
                            }
                            else 
                            if ( relatedMembershipOrNull == null || 
                                 Config.Detail >= TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#7cf21c>舰队加成限制：</color> <color=#9ce066>舰队加成仅在舰队拥有 " ).Add(
                                        Squad.TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess );

                                if ( relatedMembershipOrNull != null )
                                    buffer.Add( " 条或更少非旗舰、非精英舰船线时启用，当前有 " ).Add( superchargeBonusLimiter ).Add( " 条。</color>" );
                                else
                                    buffer.Add( " 条或更少非旗舰、非精英舰船线时启用。</color>" );
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
                    buffer.Add( "<color=#f2ae1c>舰队加成不适用：</color> <color=#e0c266>舰队加成对此特定单位无效。</color>" );
                    buffer.EndStatement(Attr_Line);
                }

                #endregion
                
                #region IncomingDamageModifiers
                debugstage = 900;
                if ( Squad.TypeData.IncomingDamageModifiers_FullList.Count > 0 )
                {
                    buffer.BeginStatement(Attr_Line);
                    
                    buffer.Add("防御修正", Attr_Label).Add("：对所受伤害的修正。");

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
                    
                    string label = is_currently ? "无敌" : "脆弱";
                    buffer.Add(label, TextStyle.Attr_Label).Add(": ");
                
                    if (!is_currently && !Squad.IsFakeEntity)
                        buffer.Add("如果...将免疫伤害");
                    else
                        buffer.Add("如果...赋予免疫能力");
                        
                    buffer
                        .Open(TextStyle.Number).AddFormat(num_req_for_invul, "{0}+ ").Close(TextStyle.Number)
                        .AddTypeGrantingInvul(Squad);
                        
                    if ( Squad.TypeData.InvulnerabilityRegion == ExternalInvulnerabilityRegion.ThisPlanet )
                        buffer.Add(" 在本").Add("星球", TextStyle.Emphasis);
                    else
                        buffer.Add(" 在全").Add("银河", TextStyle.Emphasis);
                    
                    buffer.Add(" 功能");
                    
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
                    buffer.Add( "死亡生成", Attr_Label).Add("： " );
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
                        buffer.Add( "再生", Attr_Label).Add("：随时间恢复至最大船体值 <color=#ffdf72>" ).Add( Squad.TypeData.SecondsToFullyRegenerateHull )
                        .Add( "</color> 秒（未受攻击时）。" ).EndStatement(Attr_Line);
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
                                .Add( "如果被摧毁（占领后）" )
                                .AddNumber( aip, prefix, TextTerm.AIP, TermUse.Icon );
                        }
                            // this mirrors the execution flow in HandleAIPIncrease
                            // .. which is really quite annoying, but ..
                            else 
                            if ( Squad.GetFactionTypeSafe() == FactionType.AI )
                            {
                                buffer
                                    .Add( "如果" )
                                    .Add( "玩家或盟友", TextStyle.PlayerType_Name )
                                    .Add( " 摧毁此 " )
                                    .AddNumber( aip, prefix, TextTerm.AIP, TermUse.Icon );
                            }
                            else
                            {
                                buffer
                                    .Add( "如果被摧毁 " )
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

                        buffer.Add( "如果" ).Add( "人类", TextStyle.PlayerType_Name ).Add( " 摧毁此，可获得 " );

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
                                .Add( "如果银河中无一剩余，" )
                                .AddNumber( Squad.TypeData.AIPOnDeathWhenNoneLeft, prefix, TextTerm.AIP, TermUse.Icon )
                                .Add( "。" );
                        }
                        else
                        {
                            var num_rem = BaseScenario.GetCountOfMatchingAIPOnDeathWhenNoneLeft( Squad.TypeData );
                            
                            buffer
                                .Add( "如果银河中最后" ).AddNumber(num_rem).Add("个")
                                .Add( "被摧毁，")
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
                            .Add( "如果" )
                            .Add( "玩家", TextStyle.PlayerType_Name)
                            .Add( " 为此舰船线路骇入 " )
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
                            .Add("如果")
                            .Add("玩家", TextStyle.PlayerType_Name)
                            .Add(" 建造此 ");
                            
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
                            .Add("如果")
                            .Add("玩家", TextStyle.PlayerType_Name)
                            .Add(" 占领此 ");
                            
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
                            buffer.Add( "可黑客", Attr_Label).Add("： " );
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
                            buffer.Add( " <color=#cdcdcd><size=70%>选择自</size></color> " );
                            
                            if (Squad.IsFakeEntity)
                            {
                                int numChoices = Squad.TypeData.GrantsStuffToBeAddedToPlayerFleets_FrigateOptions + 
                                                 Squad.TypeData.GrantsStuffToBeAddedToPlayerFleets_StrikecraftOptions;
                            if (numChoices == 0)
                                buffer.Add(" 可用");
                            else
                                buffer.Add("1 / ").Add(numChoices);
                            buffer.Add(" 舰船线路");
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
                            .Add( "祖先", Attr_Label).Add("： " );
                        
                        writer.WriteShip(parent, ShipExtraDetailFlags.HighestDetail);
                        
                        if ( Squad.TypeData.DiesIfParentDies )
                            buffer.Add( "（随祖先死亡）" );
                        
                        buffer.EndStatement(Attr_Line);
                    }
                } 
                else 
                if ( Squad.TypeData.DiesIfParentDies )
                {
                    buffer
                        .BeginStatement(Attr_Line)
                        .Add( "祖先", Attr_Label)
                        .Add( "：由祖先创建，随祖先死亡。" )
                        .EndStatement(Attr_Line);
                }
                #endregion
                
                // formerly 'last items' past here
                //#region last items
                //buffer.StartColor( "888888" );

                #region IsElite
                debugstage = 3010;
                if ( (Squad.TypeData.IsElite) && Config.Detail >= TooltipDetail.Medium )
                    buffer.BeginStatement(Attr_Line).Add( "精英：每支舰队只能加入一条精英舰船线" ).EndStatement(Attr_Line);
                #endregion
                
                #region ProvidesAIWarpEntryPoint
                debugstage = 3020;
                if ( (Squad.TypeData.ProvidesAIWarpEntryPoint || Squad.TypeData.IsWarpBeacon) && Config.Detail >= TooltipDetail.Medium )
                    buffer.BeginStatement(Attr_Line).Add( "允许 AI 舰船在此跃迁进入" ).EndStatement(Attr_Line);
                #endregion
                
                #region FleetMembershipStyle.Planetary
                debugstage = 3030;
                if ( Squad.TypeData.IsMobile && Squad.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary && Config.Detail >= TooltipDetail.Medium )
                    buffer.BeginStatement(Attr_Line).Add( "无法穿越虫洞" ).EndStatement(Attr_Line);
                #endregion
                
                #region AutomaticallyDiesWithCommandStation
                debugstage = 3030;
                if ( Squad.TypeData.AutomaticallyDiesWithCommandStation )
                    buffer.BeginStatement(Attr_Line).Add( "指挥站被摧毁时自毁" ).EndStatement(Attr_Line);
                #endregion
                
                #region DiesAfterLifetimeSec
                debugstage = 3040;
                if ( Squad.IsFakeEntity && Squad.TypeData.DiesAfterLifetimeSec > 0 )
                    buffer.BeginStatement(Attr_Line).Add("在...后过期 ").AddMinutesAndSeconds( Squad.TypeData.DiesAfterLifetimeSec).EndStatement(Attr_Line);
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
                        .Add("依赖型", Attr_Label)
                        .Add("： ");
                        
                    if (!Squad.IsFakeEntity)
                    {
                        var status = Squad.GetParentAndStatus(out _);
                        
                        string label = "STABLE";
                        if (status != GameEntity_Squad.ParentStatus.Alive_Local)
                            label = "DYING";
                  
                        buffer.AddVarReplace( TextVarMap.InParenthesis, null, (x,y,b,z)=>b.Add(label, TextStyle.Color_Inactive), null ).Add(" ");
                    }
                                       
                    buffer.Add(" 在...后死亡 ").Add("~", TextStyle.MinutesAndSeconds).AddMinutesAndSeconds(Mathf.RoundToInt(lifetime)).Add(" 无母舰在场。");
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
                        .Add("自我损耗", Attr_Label)
                        .Add("： ")
                        .Add("失去 ")
                    .AddNumber(per_sec, null, TextTerm.Hull, TermUse.Icon).Add(" /秒", TextStyle.Fraction_Gray)
                        .Add(" 在...后将其击杀 ")
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
                    buffer.BeginStatement(TextStyle.Newline_NoLabel).Add( "无法被修复。" ).EndStatement(TextStyle.Newline_NoLabel);
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
                        buffer.BeginStatement(Attr_Line).Add( "因其与现实的奇异交互，无法被力场保护" ).EndStatement(Attr_Line);
                    else
                        buffer.BeginStatement(Attr_Line).Add( "无法被力场保护" ).EndStatement(Attr_Line);
                }
                #endregion
                
                #region ImmuneToBonusDamage
                if ( Squad.TypeData.ImmuneToBonusDamage )
                {
                    buffer.BeginStatement(Attr_Line).Add( "免疫敌方武器系统加成伤害" ).EndStatement(Attr_Line);
                }
                #endregion
                
                #region CanPassThroughEnemyForcefields
                if ( Squad.TypeData.CanPassThroughEnemyForcefields )
                {
                    buffer.BeginStatement(Attr_Line);
                    if ( Config.Detail >= TooltipDetail.Full )
                        buffer.InCase(TextCaps.FirstLetter).Add( "力场谐波", Attr_Label).EndCase().Add("：可以匹配护盾谐波以穿透敌方力场，但其武器射击仍会撞击力场本身。" );
                    else
                        buffer.Add( "力场谐波", Attr_Label).Add("：穿透敌方力场。" );
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
                        buffer.Add( "由于与时空结构的奇异交互，此单位仅部分时间存在于常规位面，在此期间无敌且隐形 " );
                        
                        if ( Squad.TypeData.AlternativeStateOfMatter.CanTargetOtherUnitsInThisState )
                            buffer.Add( "在另一状态期间。" );
                        else
                            buffer.Add( "在另一状态期间——除了同样处于该状态的单位。" );
                        
                        buffer.NewLine();
                    }

                    buffer
                        .Add( "相移为 " ).Add( Squad.TypeData.AlternativeStateOfMatter.DisplayName )
                        .Add( " 每 " )
                        .Add( Squad.TypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds )
                        .Add( " 秒，再过 " )
                        .Add( returnTime )
                        .Add( " 秒后返回。" );
                    
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
                        .Add("：相位移动至 " ).Add( Squad.TypeData.StateOfMatterToBecomeOnWormholeExit.DisplayName )
                        .Add( " 每当穿越虫洞时" );
                    
                    if ( returnTime > 0 )
                        buffer.Add( "，并在 " ).Add( returnTime ).Add( " 秒后恢复正常。" );
                    else
                        buffer.Add( "，且未安排返回正常状态。" );
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
                        .Add( "AI 核心相位", Attr_Label)
                        .EndCase()
                        .Add("：相移为 " )
                        .Add( force_state_aihome.DisplayName )
                        .Add( " 当位于当前或前 AI 母星或 AI 堡垒世界上时（除非有敌人王者在场）。" );
                    
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
                            .Add( "免疫所有伤害，持续 " )
                            .AddMinutesAndSeconds( immune_dur - alive_sec )
                            .Add( " 更久。" );
                    }
                    else
                    {
                        buffer
                            .Add( "创建后免疫所有伤害，持续 " )
                            .AddMinutesAndSeconds( immune_dur )
                            .Add( "。" );
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
                        buffer.Add("伤害生成", Attr_Label);
                        buffer
                            .Add("：产生 ")
                            .Open(TextStyle.Color_Count)
                            .Add("~").Add(cap)
                            .Add(Text.Multiply)
                            .Close(TextStyle.Color_Count)
                            .Add(" ")
                            .WriteSpawn(type)
                            .Add(" 在其生命周期中，受到伤害时 ")
                            //.AddVarReplace(TextVarMap.InParenthesis, null, (a,b,c,d)=>{c.Add("1/").AddNumber(ehp_per, TextTerm.EHP, TermUse.Icon);}, null )
                            .Add("。");
                        
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
                        .Add( "当前有 <color=#ffdf72>" ).Add( Squad.NumberOfWeaponPoints )
                        .Add( "</color> 武器点数。" );
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
                        .Add( "已受到 ")
                        .Open(TextStyle.Color_Count).AddNumber(amount, null, TextStyle.Empty)/*.Add(Text.Multiply)*/.Close(TextStyle.Color_Count)
                        .Open(TextStyle.Fraction_Gray).Add("/").Add( deffect.Scale ).Close(TextStyle.Fraction_Gray)
                        .Add(" ")
                        .Add( deffect.DescriptionDamageName )
                        .Add( " 伤害" );
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
                        
                        buffer.Add( "不会死亡，但变为 " ).Add("残废", TextStyle.Brighter).Add("，生命值降至").AddNumber(1, TextTerm.Hull, TermUse.Icon);
                        
                        if ( Squad.TypeData.ForcedToBailOutOnCripple_Any )
                            buffer.Add( "。残废时跳伞至友方星球" );
                        else 
                        if ( Squad.TypeData.ForcedToBailOutOnCripple_DeepstrikeOnly )
                            buffer.Add( "。当在").Add("深入打击区域", TextStyle.Brighter).Add("残废时跳伞至友方星球" );

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
                                    buffer.Add( "，将损失 " ).AddNumber( hackingPointsLost, TextTerm.Hacking, TermUse.Icon );
                        
                        buffer.Add( "." );
                        
                        if ( relatedMemFleetOrNull != null &&
                             relatedMemFleetOrNull.TimesCrippled_UIOnly > 0 &&
                             Config.Detail >= TooltipDetail.Full )
                        {
                            buffer.Add( " 已残废 " ).AddNumber( relatedMemFleetOrNull.TimesCrippled_UIOnly ).Add( " 次。" );
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
                        buffer.Add( "由 " ).Add("玩家", TextStyle.PlayerType_Name).Add(" 控制时，死亡后留下可重建的残骸。" );
                        buffer.EndStatement(Attr_Line);
                    }
                    else 
                    if ( Squad.GetShouldDieToNeutral() )
                    {
                        buffer
                            .BeginStatement(Attr_Line)
                            .Add( "恢复为 ").Add("中立", TextStyle.PlayerType_Name).Add(" 在被摧毁时。" )
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
                    
                    buffer.Add( "<color=#ff5bf2>不追你" );
                    if ( Config.Detail >= TooltipDetail.Full )
                    {
                        buffer.Add( "：</color> 此舰船会在你接近时开火，否则忽略你。它正忙于猎杀 " );
                        buffer.Add( Squad.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( "。" );
                    } 
                    else 
                    if ( Config.Detail >= TooltipDetail.Medium )
                    {
                        buffer.Add( "：</color> 猎杀 " );
                        buffer.Add( Squad.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( "。" );
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
                        .Add( "此实体将在 " ).AddMinutesAndSeconds( Squad.DespawnsInXSeconds ).Add(" 后消失。")
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
                            .Add( "银河上限：" )
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
                .Add( "（" ).Add( Squad.TypeData.BaseGalaxyWideCapForPlayersConstructing )
                .Add( " + " ).Add( Squad.TypeData.AddedGalaxyWideCapForPlayersConstructingPerXPlanets )
                .Add( " 每 " ).Add( Squad.TypeData.GalaxyWideCapForPlayersConstructingXPlanetVariable )
                .Add( " 个玩家拥有的星球）" )
                .Add( World_AIW2.Instance.PlayerOwnedPlanets, "a1ffa1" )
                .Add(" 个玩家拥有的星球）" )
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
                        buffer.Add( "如果您占领此结构，AI 将对您生成远征打击（Exogalactic Strikeforces）。" );
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
                            .Add( "起始等级 " )
                            .AddColor( Squad.TypeData.StartingMarkLevel.Abbreviation, Squad.TypeData.StartingMarkLevel.ColorHex )
                            .Add("。");
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
                            buffer.Add( "科技", Attr_Label ).Add("： ");
                            
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
                    
                    buffer.Add("需要", Attr_Label).Add("： ");
                    
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
                        .Add(" 及 ")
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
                        .Add( "授予增援舰队" )
                        .Add("：")
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
                            .Add( "这是 " )
                            .Open( TextStyle.Number )
                            .Add( Squad.ShipCount )
                            .Add( " 艘舰船的堆叠")
                            .Close(TextStyle.Number)
                            .Add("。当前个体死亡时计数减少，下一个弹出。" );

                        if ( Config.Detail >= TooltipDetail.Full )
                        {
                            buffer
                                .Add( "此堆叠正常承受伤害，但射击次数为 " )
                                .Open( TextStyle.Number )
                                .Add( Squad.ShipCount ).Add( "x" )
                                .Close( TextStyle.Number )
                                .Add(" 正常数量。" );
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
                        buffer.Add("停下射击模式", Window_InGameSelectionInfo.color_StopToShoot);
                        buffer.Close(TextStyle.Newline_NoLabel);
                    }
                    
                    if ( Squad.SpeedLimitFromGroupMove > 0 )
                    {
                        buffer.Open(TextStyle.Newline_NoLabel);
                        buffer.StartColor( Window_InGameSelectionInfo.color_GroupMove ).Add("编队移动模式" ).Add("  速度 ").Add( Squad.SpeedLimitFromGroupMove ).EndColor();
                        buffer.Close(TextStyle.Newline_NoLabel);
                    }
                    */
                    if ( Squad.PreferredEntityTypeDataForTargeting != null )
                    {
                        buffer.Open(TextStyle.Newline_NoLabel);
                        buffer.StartColor( Window_InGameSelectionInfo.color_AttackMove ).Add("优先攻击目标： ").AddShipIconNameFull( Squad.PreferredEntityTypeDataForTargeting ).EndColor();
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
                            .Add( "保护 " )
                            .Add( Squad.AreaBoosting_CurrentCount.Display, TextStyle.Number )
                            .Add( " 个友军。" )
                            .EndStatement(TextStyle.GreenText);
                    }
                   
                    if ( Squad.AreaBoosters_ShotEvaluated.Count > 0 )
                    {
                        buffer
                            .BeginStatement( TextStyle.GreenText )
                            .Add( "受保护于：" );
                        
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

                            buffer.Add( "暂时无法" );
                            
                            if (repairable)
                            {
                                buffer.Add("被 ").Add("修复", TextStyle.Brighter);
                                if (regenable)
                                    buffer.Add(" 或 ");
                            }
                            
                            if (regenable)
                                buffer.Add("再生", TextStyle.Brighter);
                                
                            buffer.Add(" 再持续 ").AddMinutesAndSeconds(Squad.RepairImpossibleForSeconds);
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
                                    .Add( "此舰船通常可被运输，但当前无法装载入任何运输船，因为 " )
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
                            buffer.BeginStatement(TextStyle.Newline_NoLabel).Add( "无法被其他阵营捕获（" ).Add(Extensions.ToString(reasonCaptureImmune)).Add(")").EndStatement(TextStyle.Newline_NoLabel);
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
                            buffer.BeginStatement(TextStyle.WarnText).Add( "舰船已禁用：" );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CrippledInseadOfDead:
                                    WriteCrippledInfo( buffer, Squad );
                                    alreadyWroteCrippledInfo = true;
                                    break;
                                case ArcenRejectionReason.NonFunctionalWhenNotOnPlanetOwnedByMyFaction:
                                case ArcenRejectionReason.FactionDoesNotControlThisPlanet:
                                    buffer.Add( "阵营未控制此星球。" );
                                    break;
                                case ArcenRejectionReason.InUnexploredSpace:
                                    buffer.Add( "无法在未探索空间中运作。" );
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
                                    buffer.Add( "已下令停火。" );
                                    break;
                                case ArcenRejectionReason.EntityIsSelfBuilding:
                                    {
                                        float percent = (1f - ((float) Squad.SelfBuildingMetalRemaining / (float) Squad.GetMetalCost())) * 100;
                                        buffer.Add( "仍在建造中。" );
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
                                    buffer.Add( "阵营没有足够的能量。" );
                                    break;
                                case ArcenRejectionReason.NotEnoughCitySockets:
                                    buffer.Add( "不足 " ).Add( Squad.TypeData.NameForCitySockets_Plural ).Add( " 在此星球。" );
                                    break;
                                case ArcenRejectionReason.MetalIsZero:
                                    buffer.Add( "存储金属为零。" );
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
                            buffer.Add( "无法重建残骸：" );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CannotRebuild_IsNotremains:
                                    buffer.Add( "不是残骸！" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_YesEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "需等待 " )
                                        .AddMinutesAndSeconds( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuilding - Squad.SecondsSpentAsRemains )
                                        .Add( " 当有敌人时。" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_NoEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "只需再等 " )
                                        .AddMinutesAndSeconds( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuildingNoEnemies - Squad.SecondsSpentAsRemains )
                                        .Add( " 因无敌人存在。" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_CommandStationOnAIPlanet:
                                    buffer.Add( "指挥站无法在敌方星球重建。" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_WouldPutUsIntoBrownout:
                                    buffer.Add( "重建会导致能量不足。" );
                                    break;
                                default:
                                    buffer.Add( "ERROR_WRITE_NICE_TEXT: " ).Add( EnumNameCache.GetName( rejectionReason ) );
                                    break;
                            }
                        }
                        else
                        {
                            if ( Config.Detail < TooltipDetail.Full )
                                buffer.Add( "这只是该单位的破碎残骸。" );
                            else
                                buffer.Add( "这只是该单位的破碎残骸。残骸本身不执行任何操作，但可以被重建。" );
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
                        
                        buffer.Add( "能量不足：球形力场还需 " ).AddMinutesAndSeconds(sec).Add(" 才能恢复");
                        
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
                                .Add( "无法被占领，还需要 " ).AddMinutesAndSeconds( secondsUntilClaim )
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
                            .Add( "无法占领，因为能量短缺 ")
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
                        buffer.Add( "此单位正在黑客入侵。期间速度降低、解除隐形，且无法离开星球。" );
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

            buffer.Add( "残废：不会死亡，但需要被修复" );
            if (!relatedSquadOrNull.TypeData.ImmuneToRepairs)
            {
                buffer.Add(" ").AddVarReplace(TextVarMap.Parenthetical, 
                    (a,b,c,d)=>
                    { 
                        c.AddNumber(cost, TextTerm.Metal, TermUse.Icon);
                    },
                    null );
            }
            buffer.Add(" 恢复至满生命值才能重新运作" );

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
                    return buffer.Add( "AI 指挥站" );
                
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

