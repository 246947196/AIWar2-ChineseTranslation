using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AutosaveHandlerDeepInfo : ExternalWorldDeepInfo
    {
        //serialized
        public int TimeForLastAutosave;
        public readonly List<string> savedAutosaves = List<string>.Create_WillNeverBeGCed( 50, "AutosaveHandlerDeepInfo-savedAutosaves" );

        //nonserialized
        public int NumAutosavesToTrack;

        public static AutosaveHandlerDeepInfo Instance;
        public AutosaveHandlerDeepInfo()
        {
            Instance = this;
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            this.TimeForLastAutosave = 0;
            this.NumAutosavesToTrack = 0;
            this.savedAutosaves.Clear();
        }

        public override string GetIdentifierForErrorMessages()
        {
            return "AutosaveHandlerDeepInfo";
        }

        public override bool GetShouldIBeInUse()
        {
            return true; //always in use!
        }

        #region SerializeTo
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TimeForLastAutosave );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.savedAutosaves.Count );
            for ( int i = 0; i < this.savedAutosaves.Count; i++ )
            {
                Buffer.AddString_Condensed( MetaData, this.savedAutosaves[i] );
            }
        }
        #endregion

        #region DeserializeIntoSelf
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) 
        {
            this.TimeForLastAutosave = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            int numSavesTracked = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.savedAutosaves.Clear();
            for ( int i = 0; i < numSavesTracked; i++ )
            {
                this.savedAutosaves.Add( Buffer.ReadString_Condensed( MetaData ) );
            }
        }
        #endregion

        protected override void DoPerSimStepLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            //nothing to do!
        }

        protected override void DoPerSecondLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            //nothing to do!
        }

        #region UpdateAutosave_HostOnly
        public void UpdateAutosave_HostOnly()
        {
            bool debug = false;
            if ( ArcenNetworkAuthority.GetIsClientMode() || ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //no saving on the clients
            
            if ( Engine_AIW2.Instance.IsTestChamber )
                return;

            string campaignName = World.Instance.CampaignName;
            if ( string.IsNullOrEmpty(campaignName) )
            {
                ArcenDebugging.ArcenDebugLog( "Could not run an autosave, because the campaign name is empty!", Verbosity.ShowAsError );
                return;
            }

            this.NumAutosavesToTrack = GameSettings.Current.GetIntBySetting( "AutosavesToTrack" );
            int autosaveInterval = GameSettings.Current.GetIntBySetting( "AutosaveInterval" );
            bool isIronMan = World_AIW2.Instance.CalculateIsIronmanMode();
            if ( isIronMan )
            {
                this.NumAutosavesToTrack = 1;
                autosaveInterval = 5; //take ironman saves every few minutes. Note that exiting the game will cause it to take an ironman save anyway, so doesn't need to be super often
            }
            if ( this.NumAutosavesToTrack <= 0 || autosaveInterval <= 0 )
                return;
            int autosaveIntervalUnit = 60; //once per minute
            int secondsForNextAutosave = this.TimeForLastAutosave + autosaveInterval * autosaveIntervalUnit;
            if ( secondsForNextAutosave <= World_AIW2.Instance.GameSecond ||
                 (this.TimeForLastAutosave <= 0 && isIronMan) ) //save immediately for ironman
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Time to take an autosave at " + World_AIW2.Instance.GameSecond + " and are taking autosaves every " + autosaveInterval * 60 + " seconds. We are tracking " + this.NumAutosavesToTrack + " autosaves, and currently have " + this.savedAutosaves.Count + " and are about to add 1 more", Verbosity.DoNotShow );
                this.TimeForLastAutosave = World_AIW2.Instance.GameSecond;
                string saveGameName = "Autosave at " + Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ); //generate a new save name so it won't overwrite
                if ( isIronMan )
                {
                    SaveLoadMethods.TakeIronmanSave( false, false );
                    return;
                }

                // command that actually does the save
                {
                    var command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SaveGame], GameCommandSource.AnythingElse );
                    command.RelatedString = saveGameName;
                    
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), command, false );
                }

                if ( GameSettings.Current.LastSavegameFile != saveGameName || 
                     GameSettings.Current.LastSavegameCampaign != campaignName )
                {
                    GameSettings.Current.LastSavegameFile = saveGameName;
                    GameSettings.Current.LastSavegameCampaign = campaignName;
                    GameSettings.SaveToDisk();
                }
                
                this.savedAutosaves.Add( saveGameName );
                
                while ( this.savedAutosaves.Count > this.NumAutosavesToTrack )
                {
                    string saveToDelete = this.savedAutosaves[0];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "We have " + this.savedAutosaves.Count + " and are only tracking " + this.NumAutosavesToTrack + " deleting autosave " + saveToDelete, Verbosity.DoNotShow );
                    
                    var command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.DeleteSaveGame], GameCommandSource.AnythingElse );
                    command.RelatedString = saveToDelete;
                    command.RelatedString2 = campaignName;
                    
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), command, false );
                    
                    this.savedAutosaves.RemoveAt( 0 );
                }
            }
        }
        #endregion
    }
}
