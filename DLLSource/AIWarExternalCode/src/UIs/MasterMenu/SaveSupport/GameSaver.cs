using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.IO;

namespace Arcen.AIW2.External
{
    public class GameSaver : IGameSaver
    {
        public void SaveEverythingRightNow( string SaveName )
        {
            SaveLoadMethods.SaveWorldToDisk(SaveName);
        }

        public void DoSave( string SaveName, bool IsAutomaticSave, bool IsSaveAndQuitSave )
        {
            if ( (ArcenTime.TimeSinceStartF - World.Instance.lastSaveTime) < 2 )
                return;
            World.Instance.lastSaveTime = ArcenTime.TimeSinceStartF;

            if ( ArcenStrings.IsEmpty( SaveName ) )
            {
                ArcenDebugging.ArcenDebugLog( "Could not save, because the save name is empty!", Verbosity.ShowAsError );
                return;
            }

            string campaignName = ArcenStrings.MakeValidFilename( World.Instance.CampaignName, false );
            campaignName = SaveGameData.EncodeForCondensedFormat( campaignName );
            campaignName = campaignName.ConvertToCondensedFormat();

            if ( ArcenStrings.IsEmpty( campaignName ) )
            {
                ArcenDebugging.ArcenDebugLog( "Could not save, because the campaign name is empty!", Verbosity.ShowAsError );
                return;
            }

            World.Instance.CampaignName = campaignName;

            SaveName = ArcenStrings.MakeValidFilename( SaveName, false );
            SaveName = SaveGameData.EncodeForCondensedFormat( SaveName );
            SaveName = SaveName.ConvertToCondensedFormat();

            if ( Engine_Universal.RunStatus != RunStatus.GameStart )
            {
                string saveDirectoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + campaignName + "/";

                string fullSaveFilename = saveDirectoryPath + SaveName + Engine_Universal.SaveMainExtension;

                if ( File.Exists( fullSaveFilename ) && !IsAutomaticSave && !IsSaveAndQuitSave )
                {
                    //ConfirmPopup.Instance.Show( Language.Current.GetValue( "SaveOverwrite_Header" ),
                    //    string.Format( Language.Current.GetValue( "SaveOverwrite_Text" ), fileName ),
                    //    delegate
                    //    {
                    FinishSaving( SaveName, campaignName, IsAutomaticSave );
                    //},
                    //null );
                }
                else
                    FinishSaving( SaveName, campaignName, IsAutomaticSave );
            }
        }

        private void FinishSaving( string SaveName, string CampaignName, bool IsAutomaticSave )
        {
            //this.ShouldBeDrawnNextGUIFrame = false;
            SaveEverythingRightNow( SaveName );
            if ( (GameSettings.Current.LastSavegameFile != SaveName || GameSettings.Current.LastSavegameCampaign != CampaignName) &&
                !IsAutomaticSave )
            {
                GameSettings.Current.LastSavegameFile = SaveName;
                GameSettings.Current.LastSavegameCampaign = CampaignName;
                GameSettings.SaveToDisk();
            }
        }
    }
}
