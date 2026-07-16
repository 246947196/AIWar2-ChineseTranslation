
using System;
using Arcen.Universal;
using Arcen.AIW2.Core;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public struct EntityStatBlock
    {
        public readonly GameEntity_Squad Squad;
        //public readonly EntityText.SquadInfo Info;
        public readonly EntityText.Config Config;

        public EntityStatBlock( GameEntity_Squad squad, EntityText.Config config )
        {
            Squad = squad;
            Config = config;
        }

        public void Write( ArcenCharacterBufferBase buffer, ref CharacterPosInfo cpi )
        {
            if ( Squad.TypeData.HideStatBlock )
            {
                buffer.NewLineIfNeeded();
                buffer.NewLine();
                return;
            }

            var detail = Config.Detail;
            //var useIcons = Config.UseIcons;
            //var useText = Config.UseText;
            var panelMode = Config.PanelMode;
            var localPlayerFaction = Config.LocalPlayerFaction;
            var facType = Squad.GetFactionTypeSafe();
            var entityCanBeClaimed = Squad.HasNotYetBeenFullyClaimed && (facType == FactionType.NaturalObject || facType == FactionType.Player);

            var block_style = TextStyle.Stat_Block;
            var padding_style = TextStyle.Stat_Pad;
            var line_style = TextStyle.Stat_Line;

            int debugstage = 0;
            try
            {
                buffer.Open( block_style );
                {
                    #if false
                    #region SecondRow: Useful Stats

                    buffer.Open( line_style );
                    {
                        cpi.ResetPos( Config.CpiBaseOffset );
                        buffer.ToPos( cpi );

                        #region Hull
                        {
                            int cur = Squad.GetCurrentHullPoints();
                            int max = Squad.GetMaxHullPoints();
                            int perc = Squad.GetHullPercent();

                            if ( max == 0 )
                            {
                                //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                            }
                            else
                            {
                                buffer.Open( TextTerm.Hull, TermUse.Icon );

                                if ( Squad.IsFakeEntity )
                                {
                                    buffer.AddNumber( max );
                                }
                                else if ( Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                                {
                                    buffer.AddNumber( max );
                                }
                                else
                                {
                                    buffer.AddNumber( cur );

                                    if ( Config.Detail > TooltipDetail.SuperShort )
                                    {
                                        buffer.AddSize_Small();
                                        buffer.Add( " / " );
                                        buffer.AddNumber( max );
                                        buffer.EndSize();
                                    }

                                    buffer.Add( " " );
                                    buffer.AddSize_Tiny().AddPercentageInColor( perc.ToFInt(), true, false ).EndSize();
                                }

                                buffer.Close( TextTerm.Hull );
                            }
                        }
                        #endregion

                        debugstage = 6000;

                        cpi.AddStep_HundredTwentyFivePercent();
                        //if ( Config.UseText )
                        //cpi.AddStep_Quarter();
                        buffer.ToPos( cpi );

                        #region Shield
                        {
                            int cur = Squad.GetCurrentShieldPoints();
                            int max = Squad.GetMaxShieldPoints();
                            int perc = Squad.GetShieldPercent();

                            if ( max == 0 )
                            {
                                //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                            }
                            else
                            {
                                buffer.Open( TextTerm.Shields, TermUse.Icon );

                                if ( Squad.IsFakeEntity )
                                {
                                    buffer.AddNumber( max );
                                }
                                else if ( Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                                {
                                    buffer.AddNumber( max );
                                }
                                else
                                {
                                    buffer.AddNumber( cur );

                                    if ( Config.Detail > TooltipDetail.SuperShort )
                                    {
                                        buffer.AddSize_Small();
                                        buffer.Add( " / " );
                                        buffer.AddNumber( max );
                                        buffer.EndSize();
                                    }

                                    buffer.Add( " " );
                                    buffer.AddSize_Tiny().AddPercentageInColor( perc.ToFInt(), true, false ).EndSize();
                                }

                                buffer.Close( TextTerm.Shields );
                            }
                        }
                        #endregion

                        cpi.AddStep_HundredTwentyFivePercent();
                        //if ( Config.UseText )
                        //cpi.AddStep_FourtyPercent();
                        if ( detail >= TooltipDetail.Medium && !(panelMode == Window_InGameHoverEntityInfo.Mode.Build) )
                            cpi.AddStep_Half();
                        buffer.ToPos( cpi );

                        #region Metal
                        debugstage = 8000;
                        if ( Config.ShowMetalCost )
                        {
                            var val = Squad.GetMetalCost();

                            if ( val == 0 )
                            {
                                //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                            }
                            else
                            {
                                buffer.Open( TextTerm.Metal, TermUse.Icon );
                                buffer.AddNumber( val );
                                buffer.Close( TextTerm.Metal );
                            }
                        }
                        #endregion

                        cpi.AddStep_HundredTwentyFivePercent();
                        //cpi.AddStep_EightyPercent();
                        //if ( Config.UseText )
                        //cpi.AddStep_FourtyPercent();
                        buffer.ToPos( cpi );

                        #region Energy
                        debugstage = 8000;
                        if ( Config.ShowEnergyConsumption )
                        {
                            var val = Squad.GetEnergyUsage();

                            if ( val == 0 )
                            {
                                //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                            }
                            else
                            {
                                buffer.Open( TextTerm.Energy, TermUse.Icon );
                                buffer.AddNumber( val );
                                buffer.Close( TextTerm.Energy );
                            }
                        }
                        #endregion

                        #region Fuel
#if false
                        debugstage = 8500;
                        cpi.AddStep_EightyPercent();
                        //if ( Config.UseText )
                            //cpi.AddStep_Half();
                        buffer.ToPos( cpi );

                        
                        if ( World_AIW2.Instance.IsFuelEnabled )
                        {
                            var val = Squad.TypeData.FuelUse;

                            if ( val == 0)
                            {
                                //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                            }
                            else
                            {
                                var term = Squad.TypeData.FuelUseType.Term();
                                buffer.Open(term, TermUse.Icon);
                                buffer.AddNumber(val);
                                buffer.Close(term);
                            }
                        }
#endif
                        #endregion
                    }
                    buffer.Close( line_style );

                    #endregion

                    #region ThirdRow: Strength, APICost, Speed, +4 Stats

                    debugstage = 9000;

                    buffer.Open( line_style );
                    {
                        cpi.ResetPos( Config.CpiBaseOffset );
                        buffer.ToPos( cpi );

                        #region AIPToClaim
                        if ( Squad.TypeData.AIPToClaim != FInt.Zero &&
                             entityCanBeClaimed &&
                             (Squad.HasNotYetBeenFullyClaimed || Squad.IsFakeEntity) /*&&
                     panelMode == Window_InGameHoverEntityInfo.Mode.Build*/ )
                        {
                            if ( Config.UseText )
                                buffer.Add( "占领 AIP： " );

                            if ( Squad.TypeData.AIPToClaim > FInt.Zero )
                                buffer.WrapAIPMoreReadable( Squad.TypeData.AIPToClaim, Config.UseIcons, false );
                            else
                                buffer.WrapAIPReductionMoreReadable( Squad.TypeData.AIPToClaim, Config.UseIcons, false );
                        }
                        #endregion

                        #region AIPWhenGrantedByHack
                        if ( Squad.TypeData.AIPWhenGrantedByHack != FInt.Zero &&
                             Squad.IsFakeEntity /*&&
                     panelMode == Window_InGameHoverEntityInfo.Mode.Build*/ )
                        {
                            if ( Config.UseText )
                                buffer.Add( "入侵 AIP： " );

                            if ( Squad.TypeData.AIPWhenGrantedByHack > FInt.Zero )
                                buffer.WrapAIPMoreReadable( Squad.TypeData.AIPWhenGrantedByHack, Config.UseIcons, false );
                            else
                                buffer.WrapAIPReductionMoreReadable( Squad.TypeData.AIPWhenGrantedByHack, Config.UseIcons, false );
                        }
                        #endregion

                        #region Strength
                        {
                            int strengthCurr = Squad.GetStrengthPerSquad( false );
                            int strengthTotal = 0;
                            string strengthMode = null;
                            string strengthModeColorHex = null;
                            if ( !Config.InPopup )
                            {
                                if ( Config.OptShipCount > 1 )
                                {
                                    strengthTotal = strengthCurr;
                                    strengthCurr *= Config.OptShipCount;
                                    strengthMode = "1x";
                                    strengthModeColorHex = "ffffff";
                                }
                                else
                                if ( Squad.ShipCount > 1 )
                                {
                                    strengthTotal = strengthCurr;
                                    strengthCurr *= Squad.ShipCount;
                                    strengthMode = "1x";
                                    strengthModeColorHex = "ffffff";
                                }
                                else
                                {
                                    strengthTotal = Squad.GetStrengthOfContentsIfAny();
                                    if ( strengthTotal > 0 )
                                    {
                                        strengthMode = "+T";
                                        strengthModeColorHex = ColorMath.IceBlue.GetHexCode();
                                    }
                                }
                            }

                            debugstage = 11000;
                            if ( Config.UseText )
                                buffer.Add( "强度： " );

                            buffer
                                .Open( TextTerm.Strength, TermUse.Icon )
                                .AddNumber( FInt.Create( strengthCurr, false ) );

                            if ( strengthMode != null )
                            {
                                buffer
                                    .Add( " " )
                                    .AddSize_Tiny()
                                    .Add( "(" )
                                    .AddNumber( FInt.Create( strengthTotal, false ) )
                                    .Add( " " )
                                    .Add( strengthMode )
                                    .Add( ")" )
                                    .EndSize();
                            }

                            buffer.Close( TextTerm.Strength );
                        }
                        #endregion

                        cpi.AddStep_HundredFiftyPercent();
                        //if ( Config.UseText )
                        //cpi.AddStep_Quarter();
                        buffer.ToPos( cpi );

                        #region Speed
                        debugstage = 12000;
                        {
                            int cur = Squad.CalculateSpeed( false );
                            int max = Squad.DataForMark.Speed;

                            if ( max > 0 )
                            {
                                buffer.Open( TextTerm.Speed, TermUse.Icon );

                                if ( Squad.IsFakeEntity ||
                                    Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                                {
                                    buffer.AddNumber( max );
                                }
                                else
                                {
                                    buffer.AddNumber( cur );

                                    if ( Config.Detail > TooltipDetail.SuperShort )
                                    {
                                        buffer.AddSize_Small();
                                        buffer.Add( " / " );
                                        buffer.AddNumber( max );
                                        buffer.EndSize();
                                    }

                                    int perc = perc = (int)(((float)cur / (float)max) * 100);

                                    buffer.Add( " " );
                                    buffer.AddSize_Tiny().AddPercentageInColor( perc.ToFInt(), true, false ).EndSize();
                                }

                                buffer.Close( TextTerm.Speed );
                            }
                            else
                            if ( Squad.TypeData.DegreesToOrbitPerSecond != FInt.Zero )
                            {
                                buffer
                                    .Open( TextTerm.Speed, TermUse.Icon )
                                    .AddFIntTruncated( Squad.TypeData.DegreesToOrbitPerSecond )
                                    .Add( "°/秒" )
                                    .Close( TextTerm.Speed );
                            }
                        }
                        #endregion

                        debugstage = 13000;

                        cpi.AddStep_HundredTwentyFivePercent();
                        //if ( Config.UseText )
                        //cpi.AddStep_FourtyPercent();
                        //if ( detail >= TooltipDetail.Medium && !(Config.InPopup || panelMode == Window_InGameHoverEntityInfo.Mode.Build) )
                        //cpi.AddStep_Half();
                        buffer.ToPos( cpi );

                        buffer.AddSize_Small();
                        {
                            #region Engine
                            {

                            }
                            if ( Config.UseText )
                            {
                                buffer.Add( "引擎： " );
                            }
                            buffer.StartEngineWrapper( Config.UseIcons );
                            if ( Squad.TypeData.Engine_gx > 0 )
                            {
                                buffer.Add( Squad.TypeData.Engine_gx ).Add( " gX" );
                            }
                            else
                            {
                                buffer.Add( " -" );
                            }
                            buffer.EndEngineWrapper( false );
                            #endregion

                            cpi.AddStep_EightyPercent();
                            //if ( Config.UseText )
                            //cpi.AddStep_FourtyPercent();
                            buffer.ToPos( cpi );

                            #region Mass
                            if ( Config.UseText )
                            {
                                buffer.Add( "质量： " );
                            }
                            buffer.StartMassWrapper( Config.UseIcons ).Add( Squad.TypeData.Mass_tX ).Add( " tX" ).EndMassWrapper( false );
                            #endregion

                            cpi.AddStepFraction( 0.62f );
                            //if ( Config.UseText )
                            //cpi.AddStep_TwentyPercent();
                            buffer.ToPos( cpi );

                            #region Albedo
                            if ( Config.UseText )
                            {
                                buffer.Add( "反照率： " );
                            }
                            buffer.WrapAlbedo( Squad.DataForMark.Albedo, Config.UseIcons, false );
                            #endregion

                            cpi.AddStep_SixtyPercent();
                            //if ( Config.UseText )
                            //cpi.AddStepFraction( 0.23f );
                            buffer.ToPos( cpi );

                            #region Armor
                            if ( Config.UseText )
                            {
                                buffer.Add( "装甲： " );
                            }
                            buffer.StartArmorWrapper( Config.UseIcons ).Add( Squad.TypeData.Armor_mm ).Add( " mm" ).EndArmorWrapper( false );
                            #endregion
                        }
                        buffer.EndSize();
                    }
                    buffer.Close( line_style );

                    #endregion
#endif
                }
                buffer.Close( block_style );
                
                buffer.Pad( padding_style );
            }
            catch ( Exception e )
            {
                LOG.Err( "error at debugstage {0}\n{1}", debugstage, e );
            }
        }

    }
}
