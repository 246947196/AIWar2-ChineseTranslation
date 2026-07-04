using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public abstract class ExternalFactionBaseInfoRoot : ExternalFactionBaseInfo
    {
        #region DoAnyInitializationImmediatelyAfterFactionAssigned
        public sealed override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            //TEACHING_MOMENT: This is required for mapgen!  This method call is what make it so that anything you do in
            //DoFactionGeneralAggregationsPausedOrUnpaused() and DoRefreshFromSettings() will be present during seeding.

            NextFactionSettingsRefreshTime = 0; //ensure that the refresh happens
            DoGeneralAggregationsPausedOrUnpaused();

            DoAnySubInitializationImmediatelyAfterFactionAssigned();
        }
        protected virtual void DoAnySubInitializationImmediatelyAfterFactionAssigned() { }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        public sealed override void DoGeneralAggregationsPausedOrUnpaused()
        {
            DoFactionGeneralAggregationsPausedOrUnpaused();
            DoRefreshFromSettings_Outer();
        }
        protected virtual void DoFactionGeneralAggregationsPausedOrUnpaused()
        { }
        #endregion

        #region DoRefreshFromFactionSettings
        private float NextFactionSettingsRefreshTime = 0;
        private void DoRefreshFromSettings_Outer()
        {
            if ( ArcenTime.TimeSinceStartF <= NextFactionSettingsRefreshTime )
                return; //if the difficuly is set and we've checked recently, don't check again
            NextFactionSettingsRefreshTime = ArcenTime.TimeSinceStartF + Engine_Universal.PermanentQualityRandom.NextFloat( 0.4f, 1.9f ); //randomize to keep load down

            AttachedFaction.IsVassal = this.AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "Vassal", false );

            DoRefreshFromFactionSettings();
        }

        protected override void DoRefreshFromFactionSettings()
        {
        }

        #endregion

        #region GetRaidDesirability
        public override int GetRaidDesirability( Planet planet )
        {
            if ( this.AttachedFaction.Type != FactionType.AI )
                return 0;
            return this.AttachedFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType.Implementation.GetRaidDesirability( this.AttachedFaction, planet );
        }
        #endregion

        #region GetRaidTraversalDifficulty
        public override int GetRaidTraversalDifficulty( Planet planet )
        {
            if ( this.AttachedFaction.Type != FactionType.AI )
                return 0;
            return this.AttachedFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType.Implementation.GetRaidTraversalDifficulty( this.AttachedFaction, planet );
        }
        #endregion

        #region GetFireteamById
        public virtual Fireteam GetFireteamById( int id )
        {
            return null;
        }

        public sealed override FireteamBase GetFireteamBaseById( int id )
        {
            return this.GetFireteamById( id );
        }
        #endregion

        public virtual PlanetPathfinder GetNormalPathfinderThatMustBeReleased()
        {
            return PlanetPathfinderBasic.Pool.GetFromPoolOrCreate( "ExternalFactionBaseInfoRoot-PlanetPathfinderBasic", 300f );
        }

        public virtual PlanetPathfinder GetConservativePathfinderThatMustBeReleasedOrNull()
        {
            return PlanetPathfinderConservative.Pool.GetFromPoolOrCreate( "ExternalFactionBaseInfoRoot-PlanetPathfinderConservative", 300f );
        }

        public sealed override void WriteFactionSlotText( ArcenCharacterBufferBase buffer )
        {
            var facinfo = this;
            var fac = AttachedFaction;
            var cfg = fac.Config;
            var facdata = fac.SpecialFactionData;

            if ( facdata == null )
                return;
            if ( fac == null )
                return;
            if ( cfg == null )
                return;
            if ( facdata == null )
                return;

            if ( World_AIW2.Instance.InSetupPhase )
                cfg = World_AIW2.Instance.Setup.GetConfigurationForFaction( fac.FactionIndex );
                
            int debugCode = 0;
            try
            {
                debugCode = 100;

                // Choose color for text
                var line1Color = new Color(0.9f, 0.9f, 0.9f);
                var line2Color = new Color(0.6f, 0.6f, 0.6f);
                if ( Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby == cfg )
                {
                    line1Color = Color.white;
                    line2Color = Color.white;
                }
                string line1ColorHex = line1Color.GetHexCode();
                string line2ColorHex = line2Color.GetHexCode();

                // faction icon
                {
                    debugCode = 1200;

                    WriteFactionIcon( buffer );
                }

                buffer.Add( "<indent=28px>" );

                // line 1
                // faction name
                {
                    buffer.StartColor( line1ColorHex );

                    debugCode = 200;

                    WriteFactionLabel( buffer );

                    buffer.EndColor();

                    buffer.NewLine();
                }

                // line 2
                // faction status
                {
                    debugCode = 1300;

                    buffer.StartColor( line2ColorHex );

                    buffer.Add( "<size=10>" );
                    buffer.Add( " " );

                    debugCode = 1400;
                    facinfo.WriteFactionSlotStatus( buffer );

                    buffer.Add( "</size>"  );

                    buffer.EndColor();
                }

                buffer.Add( "</indent>" );

                // mod/expansion abbrev
                {
                    buffer.Add( "<line-height=0>" );
                    buffer.NewLine();
                    buffer.Add( "<align=\"right\">" ).Add( "<margin-right=2><sub>" );

                    var mod = fac.SpecialFactionData.RowFromXmlMod;
                    if ( mod != null )
                        buffer.Add( mod.Abbreviation, mod.ColorForDisplay );

                    var dlc = fac.SpecialFactionData.RowFromExpansion;
                    if ( dlc != null )
                        buffer.Add( dlc.Abbreviation, dlc.ColorForDisplay );

                    buffer.Add( "</align></line-height></sub>" );
                }

                debugCode = 1700;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogNoDateOrAnything(
                    string.Format( "Hit exception in WriteFactionSlotText for {0} at debugCode {1}\n{2}", fac.SpecialFactionData.InternalName, debugCode, e ), DebugLogDestination.ArcenDebugLog, Verbosity.ShowAsError );
            }
        }

        public virtual void WriteFactionIcon( ArcenCharacterBufferBase buffer )
        {
            var fac = AttachedFaction;
            var data = fac.SpecialFactionData;
            if (data != null)
            {
                buffer.Add( "<pos=3><size=8px><voffset=6px>" );
                buffer.AddSprite( data.TexEmbedSprite_Icon, data.TexEmbedSprite_IconBorder,
                                  data.TexEmbedSprite_IconOverlay,
                                  fac.FactionCenterColor.ColorHex, fac.FactionTrimColor.ColorHex,
                                  data.TexEmbedSprite_IconOverlay_HexColor );
                buffer.Add( "</pos></size></voffset>" );
            }
        }
        
        public void WriteFactionLabel( ArcenCharacterBufferBase buffer )
        {
            var fac = AttachedFaction;
            if ( fac == null )
                return;
            
            var facdata = fac.SpecialFactionData;
            if ( facdata == null )
                return;
            
            var facinfo = this;
            var playerdata = facinfo.PlayerType;

            int debugStage = 0;
            try
            {
                bool log = false;

                int numtype = 0;
                int mynum = 0;
                var factions = World_AIW2.Instance.Factions;
                for ( int i = 0; i < factions.Count; i++ )
                {
                    var other = factions[i];
                    var other_fdata = other.SpecialFactionData;
                    var other_playerdata = other.BaseInfo?.PlayerType;

                    if ( !other_fdata.InternalName.Equals( facdata.InternalName, StringComparison.InvariantCultureIgnoreCase ) )
                    {
                        if ( log )
                            ArcenDebugging.ArcenDebugLogNoDateOrAnything( string.Format( "faction {0} doesn't match my {1}", other.SpecialFactionData.InternalName, facdata.InternalName ), DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow );
                        continue;
                    }

                    if (playerdata != null && playerdata != other_playerdata)
                    {
                        if ( log )
                            ArcenDebugging.ArcenDebugLogNoDateOrAnything( string.Format( "faction {0}s player data {1} doesn't match my {1}", other.SpecialFactionData.InternalName, other_playerdata?.InternalName??"null", playerdata?.InternalName??"null"), DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow );
                        continue;
                    }

                    if ( other == fac )
                    {
                        if ( log ) LOG.Msg( "i am faction [{0}] #{1}", i, numtype );
                        
                        numtype++;
                        mynum = numtype;
                        
                        continue;
                    }

                    bool hidden( SpecialFactionData arg )
                    {
                        if ( arg.ShouldNotBeShown )
                            return true;
                        if ( arg.ShouldNotBeShownUnlessZombiesEnabled )
                            return true;

                        if ( Window_FactionsWindow.Instance.IsOpen )
                        {
                            if ( arg.ShouldNotBeShownInGameLobby )
                                return true;
                            if ( arg.ShouldNotBeShownInGameLobbyAddFactionsList )
                                return true;
                            if ( arg.ShouldNotBeShownEvenAsSeparateLineItemsInGameFactionsMenu )
                                return true;
                        }

                        return false;
                    }
                    
                    bool notshown = hidden( other_fdata );
                    
                    if ( log ) LOG.Msg( "faction {0} is {1}", other_fdata.InternalName, notshown ? "hidden" : "shown" );

                    if ( notshown )
                        continue;

                    numtype++;
                }

                if ( log ) LOG.Msg( "{0} factions and {1} my type", factions.Count, numtype );

                if (playerdata != null)
                {
                    buffer.Add( playerdata.GetDisplayName() );
                }
                else
                {
                    buffer.Add( facdata.GetDisplayName() );
                }

                if ( numtype > 1 )
                {
                    buffer.Add( " " ).Add( mynum );
                }

                debugStage = 1000;
            }
            catch ( Exception e )
            {
                e.Data.Add( "debugStage", debugStage );
                throw e;
            }

        }

	    #region WriteFactionSlotStatus
	    public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
	    {
	        var fac = AttachedFaction;
	        var fcon = fac.Config;
        
	        string value = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "Intensity", false );
	        if ( !string.IsNullOrWhiteSpace(value) )
	            buffer.Add( "Intensity " ).Add( value ).Add( "    " );

            if ( !string.IsNullOrWhiteSpace(this.Allegiance) )
            {
	            if ( this.Allegiance == "Friendly To Players" )
	                buffer.Add( "Friendly", "a1ffa1" );
	            else if ( this.Allegiance == "Allied To AI" )
	                buffer.Add( "AI Allied", "ffa1a1" );
	            else if ( this.Allegiance == "Minor Faction Team Red" )
	                buffer.Add( "Red Team", "ff3800" );
	            else if ( this.Allegiance == "Minor Faction Team Blue" )
	                buffer.Add( "Blue Team", "1A2DFF" );
	            else if ( this.Allegiance == "Minor Faction Team Green" )
	                buffer.Add( "Green Team", "3fff00" );
	            else if ( this.Allegiance == "Dark Alliance" )
	                buffer.Add( "Dark Alliance", "666666" );
                else if ( this.Allegiance == "Hostile To All")
                    buffer.Add( "Hostile", "ff4a32" );
	            else
	                buffer.Add( this.Allegiance, "dddddd" );
            }
	    }
	    #endregion

	    public override void WriteControllingAccount( ArcenCharacterBufferBase buffer )
	    {
	        PlayerAccount account = AttachedFaction.Config.GetAccountOfFirstControllingPlayerOrNull();

	        buffer
	            .Add( "<line-height=0>"  )
	            .NewLine()
	            .Add( "<align=right>" )
	            .Add( "<margin-right=5>" )
	            .Add( "<color=#dddddd>"  )
	            ;
	        var count = AttachedFaction.Config.CountOfPlayersFactionControllingFaction;
	        if (count > 1)
	            buffer.Add( "(" ).Add( count ).Add( ") controllers" );
	        else if ( count == 1)
	        {
                buffer.Add( account.Username );
	        }
	        else
	        {
	            //buffer.Add( "nobody" );
	        }

	        buffer
	            .Add( "</line-height>" )
	            .Add("</margin>"  )
	            .Add( "</align>" )
	            .Add( "</color>"  )
	            ;
	    }

        public override void WriteAddedHackingHeaderInfo( ArcenCharacterBufferBase buffer )
        {
        }
        
        public override void WriteAddedScienceHeaderInfo( ArcenCharacterBufferBase buffer )
        {
        }

        public override FInt GetRatioOfInferiorityAtWhichAllGuardPostsDeploy()
        {
            return FInt.FromParts(2,500);
        }

        public override void WriteFactionTooltipForSidebarInLobby( ArcenCharacterBufferBase buffer )
        {
            var fac = AttachedFaction;
            var config = fac.Config;

            buffer.Add( fac.GetDisplayNameWithoutPlayerNames() );
            buffer.Add( "\n" ).Add( fac.SpecialFactionData.Description );

            if ( fac.SpecialFactionData.Type == FactionType.Player )
            {
                PlayerTypeData playerType = fac.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType == null )
                    return;

                    bool hasLoggedAnyError = false;
                    List<ConfigurationForFaction> factionConfigs = World_AIW2.Instance.Setup.FactionConfigurations;

                    if ( playerType.MustBeSolePlayerFaction )
                    {
                        int countOfOtherPlayers = 0;
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction cfg = factionConfigs[j];
                            if ( cfg == config || cfg.SpecialFactionData.Type != FactionType.Player )
                                continue;
                            countOfOtherPlayers++;
                        }

                        if ( countOfOtherPlayers > 0 )
                        {
                            if ( !hasLoggedAnyError )
                            {
                                buffer.Add( "\n\n<color=#ff6e52>" );
                                hasLoggedAnyError = true;
                            }
                            else
                                buffer.Add( "\n" );
                            buffer.Add( playerType.DisplayName )
                                .Add( " cannot be used with other players present, but at the moment there are " ).Add( countOfOtherPlayers ).Add( " others.  " );
                            buffer.Add( "\n<size=80%>If you want to have a spectator in multiplayer, then just have the player(s) in question just remove themself from being attached to any player factions, and they will become spectators that share the view of the human players.  The player-type form of spectator is omniscient, and meant for watching factions duke it out without any player involvement.</size>" );

                        }
                    }

                    if ( !playerType.CanLoseGame && !playerType.PreventsGameWinOrLoss )
                    {
                        bool foundOtherPlayerThatCanLose = false;
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction cfg = factionConfigs[j];
                            if ( cfg == config || cfg.SpecialFactionData.Type != FactionType.Player )
                                continue;
                            PlayerTypeData otherPlayerType = cfg.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( otherPlayerType.CanLoseGame )
                            {
                                foundOtherPlayerThatCanLose = true;
                                break;
                            }
                        }
                        if ( !foundOtherPlayerThatCanLose )
                        {
                            if ( !hasLoggedAnyError )
                            {
                                buffer.Add( "\n\n<color=#ff6e52>" );
                                hasLoggedAnyError = true;
                            }
                            else
                                buffer.Add( "\n" );
                            buffer.Add( playerType.DisplayName ).Add( " Cannot Be Used Without Other Players" );
                            buffer.Add( "\n<size=80%>This player type is unable to lose the game, so it needs to accompany another player type that can.</size>" );
                        }
                    }

                    if ( playerType.RequiredVassalCount > 0 )
                    {
                        int foundVassalCount = 0;
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction cfg = factionConfigs[j];
                            if ( cfg == config || cfg.SpecialFactionData.Type == FactionType.Player )
                                continue;
                            if ( cfg.GetBoolValueForCustomFieldOrDefaultValue( "Vassal", false ) )
                                foundVassalCount++;
                        }
                        if ( foundVassalCount < playerType.RequiredVassalCount )
                        {
                            if ( !hasLoggedAnyError )
                            {
                                buffer.Add( "\n\n<color=#ff6e52>" );
                                hasLoggedAnyError = true;
                            }
                            else
                                buffer.Add( "\n" );
                            buffer.Add( playerType.DisplayName ).Add( " Requires " ).Add( playerType.RequiredVassalCount - foundVassalCount ).Add( " More Vassals" );
                            buffer.Add( "\n<size=80%>This faction requires " ).Add( playerType.RequiredVassalCount )
                                .Add( " vassals.  Vassals are friendly factions that are subordinate to you: you can't control them directly, but you can give them targets and goals, and sometimes broad orders.\n\nCurrently Available Vassal Choices:" );

                            foreach ( SpecialFactionData facType in SpecialFactionDataTable.Instance.Rows )
                            {
                                if ( facType.CanBeAVassal )
                                    buffer.Add( "\n" ).Add( facType.DisplayName );
                            }

                            buffer.Add( "</size>" );
                        }
                    }

                    if ( hasLoggedAnyError )
                        buffer.Add( "</color>" );
                }
            }

        protected override void Cleanup()
        {
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            return 0;
        }
        
        public override string ToString()
        {
            return this.TracingName;
        }
        
        public override void WriteFactionIdentityString(ArcenCharacterBufferBase buffer)
        {
            var fac = this.AttachedFaction;
            if ( fac.SpecialFactionData != null )
                buffer.Add(fac.GetDisplayNameWithoutPlayerNames()).Add(" index ").Add(fac.FactionIndex);
            else
                buffer.Add(Extensions.ToString(fac.Type)).Add(" index ").Add(fac.FactionIndex);
        }
    }
}
