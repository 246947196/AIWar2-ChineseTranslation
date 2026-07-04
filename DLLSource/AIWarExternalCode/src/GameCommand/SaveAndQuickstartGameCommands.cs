using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_SaveGame : BaseGameCommand
    {
        private static readonly List<string> metaDataList = List<string>.Create_WillNeverBeGCed( 20, "GameCommand_SaveGame-metaDataList" );

        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            string saveName = command.RelatedString;
            
            SaveLoadMethods.SaveWorldToDisk(saveName);
            
            if ( command.RelatedBool )
            {
                var closeAIW2 = command.RelatedMagnitude > 0;
                
                if (closeAIW2)
                {
                    Engine_Universal.IsShuttingDown = true;
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();
                    Engine_Universal.ForceClose( false );
                }
                else
                {
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();    
                    ArcenThreading.InitiateThreadingBlockForShutdown( NextThreadingBlockPhase.QuitToMainMenu );
                }
            }
        }
    }

    public class GameCommand_DeleteSaveGame : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            string campaignName = World.Instance.CampaignName;
            if ( campaignName == null || campaignName.Length == 0 )
            {
                ArcenDebugging.ArcenDebugLog( "Could not delete savegame, because the campaign name is empty!", Verbosity.ShowAsError );
                return;
            }

            string fullSaveName = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + campaignName +
                "/" + command.RelatedString + Engine_Universal.SaveMainExtension;

            World.Instance.DeleteSaveGame( fullSaveName, false, false );
        }
    }

    public class GameCommand_DeleteAllSaveGames : BaseGameCommand
    {
        private static readonly List<SaveGameData> SortedSavegamesInCampaign = List<SaveGameData>.Create_WillNeverBeGCed( 200, "GameCommand_DeleteAllSaveGames-SortedSavegamesInCampaign" );
        //this is used for Ironman mode
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            //we are deleting all the save games except the save we just made
            string campaignName = command.RelatedString2;
            if ( campaignName == null || campaignName.Length == 0 )
            {
                ArcenDebugging.ArcenDebugLog( "Could not delete all savegames in the campaign, because the campaign name is empty!", Verbosity.ShowAsError );
                return;
            }

            string saveToPreserve = command.RelatedString; //don't delete the most recent save
            bool deleteIronmanOnly = false;
            if ( command.RelatedString3 == "Delete_Ironman_Only" )
                deleteIronmanOnly = true;
            
            int debugCode = 100;
            
            //this isn't the most efficient
            try
            {
                debugCode = 200;
                SortedSavegamesInCampaign.Clear();
                CampaignOrQuickstartGroup CurrentCampaignName = CampaignOrQuickstartGroup.Create( campaignName, SaveType.Campaign );
                CurrentCampaignName.Directories.Add( CampaignOrQuickstart_SubFolder.Create( Engine_Universal.CurrentPlayerDataDirectory + "Save/" + campaignName, null, null ) );
                
                debugCode = 400;
                SaveLoadMethods.PopulateSavesInGroup( CurrentCampaignName );
                
                debugCode = 500;
                for ( int i = 0; i < SortedSavegamesInCampaign.Count; i++ )
                {
                    debugCode = 600;
                    string saveName = SortedSavegamesInCampaign[i].saveName;
                    if ( !String.IsNullOrEmpty( saveToPreserve ) &&
                         saveName.Contains( saveToPreserve ) )
                    {
                        continue;
                    }
                    
                    if ( deleteIronmanOnly && !saveName.Contains( SaveLoadMethods.IronmanPrefix ) )
                        continue; //we are only deleting ironman saves
                    
                    string fullSaveName = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + campaignName +
                        "/" + saveName + Engine_Universal.SaveMainExtension;
                    
                    debugCode = 700;
                    World.Instance.DeleteSaveGame( fullSaveName, false, false );
                }
                
                debugCode = 800;

                //clean up and avoid memory leaking
                CurrentCampaignName.ReturnToPool();

                if ( command.RelatedBools.Count > 0 ) //exit the entire game
                {
                    Engine_Universal.IsShuttingDown = true;
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();
                    Engine_Universal.ForceClose( false );
                }
                else 
                if ( command.RelatedBool )
                {
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();
                    //quitting to the main menu
                    ArcenThreading.InitiateThreadingBlockForShutdown( NextThreadingBlockPhase.QuitToMainMenu );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in DeleteAllSaveGames debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
}
