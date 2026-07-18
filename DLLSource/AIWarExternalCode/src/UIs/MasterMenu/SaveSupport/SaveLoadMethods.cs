using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Linq;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using Sys=System.Collections.Generic;
using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public struct TooltipFileData
    {
        public string DisplayName;
        public string Tooltip;
        public int SortOrder;
        public bool IsCommunity;
    }
    
    public enum SaveSortMode
    {
        Name = 0,
        GameTime = 1,
        Date = 2,
    }

    public static class SaveLoadMethods
    {
        public const string IronmanPrefix = "Ironman Save at ";
        
        /*
        These are shared by everyone. But mostly the Save/Load/Quickstart windows.
        */
        public static ProtectedList<CampaignOrQuickstartGroup> SortedCampaignNames = ProtectedList<CampaignOrQuickstartGroup>.Create_WillNeverBeGCed( 60, "SaveLoadMethods-SortedCampaignNames" );
        public static ProtectedList<CampaignOrQuickstartGroup> SortedQuickstartGroups = ProtectedList<CampaignOrQuickstartGroup>.Create_WillNeverBeGCed( 60, "SaveLoadMethods-SortedQuickstartGroups" );

        public static readonly DateTime NullDate = new DateTime( 1970, 1, 1 );
        
        private static bool? _debug;
        public static bool Debug
        {
            get
            {
                //if (_debug == null)
                    _debug = GameSettings.Current.GetBoolBySetting("SaveGameData_Debug", false);
                return _debug.Value;
            }
        }
        
        /// <summary>
        /// Return object used for writing debug logging related to save/load methods.
        /// If debug logging is not enabled, returns null.
        /// </summary>
        private static Log Log
        {
            get
            {
                if (!Debug)
                    return null;
                
                return Log.Yes;
            }
        }
        
        public static Verbosity ErrorIfDebug => Debug ? Verbosity.ShowAsError : Verbosity.DoNotShow;
        
        public static int ResaveNextCount = 0;
        
        private static bool? _saveMissingSaveMeta;
        public static bool SaveMissingSaveMeta
        {
            get
            {
                //if (_saveMissingSaveMeta == null)
                    _saveMissingSaveMeta = GameSettings.Current.GetBoolBySetting("SaveMissingSaveMeta", false);
                return _saveMissingSaveMeta.Value;
            }
        }
        
        private static bool? _resaveAllSaveMeta;
        public static bool ResaveAllSaveMeta
        {
            get
            {
                if (ResaveNextCount > 0)
                    return true;
                
                //if (_resaveAllSaveMeta == null)
                    _resaveAllSaveMeta = GameSettings.Current.GetBoolBySetting("ResaveAllSaveMeta", false);
                return _resaveAllSaveMeta.Value;
            }
        }
        
        public static Dictionary<string,bool> DeletedManifest = Dictionary<string,bool>.Create_WillNeverBeGCed(20, "SaveLoadMethods.DeletedManifest");
        public static bool IsDeleted(string file_or_dir_name)
        {
            var key = Path.ChangeExtension(file_or_dir_name.ToLower().Replace("\\", "/"),"");
            bool res = DeletedManifest[key];
            if (Debug) LOG.Msg("IsDeleted returning {0} for '{1}'", res.ToString().ToUpper(), key);
            return res;
        }
        
        private static void LoadManifest(string directoryPath)
        {
            if (!directoryPath.EndsWith("/"))
                directoryPath += "/";
            
            var manifest = directoryPath + "deleted.txt";
            bool exists = File.Exists(manifest);
            if (Debug) LOG.Msg("Checking for manifest ({0}) path='{1}'", exists, manifest);
            
            if (exists)
            {
                var lines = File.ReadAllLines(manifest);
                foreach (var ln in lines)
                {
                    if (string.IsNullOrEmpty(ln))
                        continue;
                    
                    var key = Path.ChangeExtension((directoryPath + ln).ToLower().Replace("\\", "/"),"");
                    DeletedManifest[key] = true;
                    
                    if (Debug) LOG.Msg("Added '{0}' to DeletedManifest.", key);
                }
            }
        }

        public static void PopulateQuickstartGroups()
        {
            SortedQuickstartGroups.Clear( true );
            DeletedManifest.Clear();
            
            string directoryPath = Engine_Universal.CurrentGameDataDirectory + "QuickStarts2/";
            ParseQuickstartFolders( directoryPath, null, null );

            #region Local Player Quickstarts
            directoryPath = Engine_Universal.CurrentPlayerDataDirectory + "QuickStarts/";
            if (Directory.Exists(directoryPath))
            {
                var info = new DirectoryInfo(directoryPath);
                var files = info.EnumerateFiles("*.save");
                bool hascontent = files.Any();
                    
                if (!hascontent)
                {
                    if (Debug) LOG.Msg("Skip quickstart folder '{0}' because contains no actual saves.", directoryPath);
                }
                else
                {
                    CampaignOrQuickstartGroup group;
                    if (!SaveLoadMethods.GetQuickstartGroup("Mine", out group))
                    {
                        group = CampaignOrQuickstartGroup.Create( "Mine", SaveType.Quickstart );
                        group.SortOrder = int.MaxValue;
                        group.Tooltip = "Quickstarts you create yourself show up here.\nThey are located on disk at [aiw2_root]/PlayerData/QuickStarts";
                        
                        if (Debug) LOG.Msg("Added quickstart group '{0}'.", group.DisplayName);
                        
                        SortedQuickstartGroups.Add( group );
                    }
                    
                    if (Debug) LOG.Msg("Added directory '{0}' to quickstart group '{1}'.", directoryPath, group.DisplayName);
                    
                    // add this directory to the group, as it is now a location which can contain items to show in it
                    group.Directories.Add( CampaignOrQuickstart_SubFolder.Create( directoryPath, null, null, true ) );
                }
            }
            #endregion

            #region Expansion Quickstarts
            if ( !Engine_Universal.IsInTotalConversionMode )
            {
                for ( int j = 0; j < ExpansionTable.Instance.Rows.Count; j++ )
                {
                    Expansion exp = ExpansionTable.Instance.Rows[j];
                    if ( !exp.IsInstalledAtAll || exp.IsHidden )
                        continue;

                    directoryPath = exp.FullDirectoryPath + "QuickStarts2/";
                    
                    if ( Directory.Exists( directoryPath ) ) 
                        ParseQuickstartFolders( directoryPath, exp, null );
                }
            }
            #endregion
            
            #region XmlMod Quickstarts
            for ( int j = 0; j < XmlModTable.Instance.Rows.Count; j++ )
            {
                XmlMod mod = XmlModTable.Instance.Rows[j];
                if ( mod.IsOff() || mod.IsHidden )
                    continue;

                directoryPath = mod.FullDirectoryPath + "QuickStarts2/";

                if ( Directory.Exists( directoryPath ) ) 
                    ParseQuickstartFolders( directoryPath, null, mod );
            }
            #endregion
            
            // sort alphanumerically by name for quickstarts
            SortedQuickstartGroups.Sort(SaveLoadMethods.CompareQuickstartGroups);
        }

        public static bool GetCampaign( string name, bool createIfMissing, out CampaignOrQuickstartGroup campaign )
        {
            campaign = null;
            string decodedName = SaveGameData.DecodeCondensedSaveName( name );
            
            foreach (var c in SortedCampaignNames)
            {
                if ( c.DisplayName == name || c.DisplayName == decodedName )
                {
                    campaign = c;
                    return true;
                }
            }

            if (!createIfMissing)
                return false;
            
            campaign = CampaignOrQuickstartGroup.Create( name, SaveType.Campaign );
            campaign.Directories.Add( CampaignOrQuickstart_SubFolder.Create( Engine_Universal.CurrentPlayerDataDirectory + "Save/" + name + "/" ) );
            SortedCampaignNames.Add(campaign);

            return true;
        }
        
        public static bool GetQuickstartGroup( string name, out CampaignOrQuickstartGroup group )
        {
            foreach (var itr in SortedQuickstartGroups)
            {
                if (itr.DisplayName == name)
                {
                    group = itr;
                    return true;
                }
            }

            group = null;
            
            return false;
        }

        private static void ParseQuickstartFolders( string directoryPath, Expansion ForExpansion, XmlMod ForMod )
        {
            LoadManifest(directoryPath);
            
            var directories = Directory.EnumerateDirectories( directoryPath );
            
            //Debug.Log( directories.Length + " GetDir: " + directoryPath );
            
            foreach ( string directory in directories )
            {
                if (IsDeleted(directory))
                {
                    if (Debug) LOG.Msg("Skip quickstart folder '{0}' because listed in DeletedManifest.", directory);
                    continue;
                }
                
                TooltipFileData data;
                
                // todo: if needed we can support _folder.tooltip files for TC
                //       however they dont currently have one and still need to be listed
                if ( Engine_Universal.IsInTotalConversionMode )
                {
                    data.DisplayName = Path.GetFileNameWithoutExtension(directory);
                    data.Tooltip = " ";
                    data.SortOrder = 0;
                    data.IsCommunity = false;
                }
                else
                {
                    var tippath = directory + "/_folder.tooltip";
                    var tipinfo = new FileInfo(tippath);
                    if (!tipinfo.Exists || tipinfo.Length == 0)
                    {
                        if (Debug) LOG.Msg("Skip quickstart folder '{0}' because no _folder.tooltip file.", directory);
                        continue;
                    }

                    if (!SaveLoadMethods.LoadTooltipFromDisk(tippath, out data))
                    {
                        if (Debug) LOG.Msg("Skip quickstart folder '{0}' because _folder.tooltip has a blank value for the tooltip.", directory);
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(data.DisplayName))
                        data.DisplayName = Path.GetFileNameWithoutExtension(directory);
                }
                
                LoadManifest(directory);

                var info = new DirectoryInfo(directory);
                var files = info.EnumerateFiles("*.save");
                bool hascontent = files.Any(
                    (f)=>
                    { 
                        if (IsDeleted(f.FullName))
                        {
                            //if (Debug) LOG.Msg("Skip quickstart file '{0}' because listed in DeletedManifest.", f.FullName);
                            return false;
                        }
                        
                        return true;
                    } );
                
                if (!hascontent)
                {
                    if (Debug) LOG.Msg("Skip quickstart folder '{0}' because contains no actual saves.", directory);
                    continue;
                }
                
                CampaignOrQuickstartGroup group;
                if (!SaveLoadMethods.GetQuickstartGroup(data.DisplayName, out group))
                {
                    group = CampaignOrQuickstartGroup.Create( data.DisplayName, SaveType.Quickstart, ForExpansion, ForMod );
                    group.SortOrder = data.SortOrder;
                    group.Tooltip = data.Tooltip;
                    
                    if (Debug) LOG.Msg("Added quickstart group '{0}'.", group.DisplayName);
                    
                    SortedQuickstartGroups.Add( group );
                }

                //Debug.Log( "add match: " + directory );
                
                if (Debug) LOG.Msg("Added directory '{0}' to quickstart group '{1}'.", directory, group.DisplayName);
                
                // add this directory to the group, as it is now a location which can contain items to show in it
                group.Directories.Add( CampaignOrQuickstart_SubFolder.Create( directory, ForExpansion, ForMod, data.IsCommunity ) );
            }
        }
        
        /// <summary>
        /// This is the campaign folder equivalent to PopulateQuickstartGroups
        /// but actually there is really just one campaign root: PlayerData/Save/
        /// 
        /// This will clear and then re-populate SaveLoadMethods.SortedCampaignNames
        /// That is, the subfolders of PlayerData/Save/
        ///
        /// Note this is only showing that a folder exists and not anything about
        /// its content.
        ///
        /// As in, it allocates the CampaignOrQuickstartGroup for all
        /// folders even empty ones.
        /// 
        /// And CampaignOrQuickstartGroup.SortedSavesInFolder is not yet populated.
        /// </summary>
        public static void PopulateCampaignFolders()
        {
            MoveLooseSavesToOldSavesFolder();

            SortedCampaignNames.Clear(true);
            
            string directoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/";
            ParseCampaignFolders( directoryPath );
             
            // This sort mode is for the saves/quickstarts within a group
            // not the list of groups. At this time, it is always sorted by most recently
            // accessed (which we intuit by the most recent file write time).
            //var sortType = (SaveSortMode)GameSettings_AIW2.Current.SaveLoadSortType;
            
            SortedCampaignNames.Sort(CompareCampaigns);
        }
        
        private static void ParseCampaignFolders( string directoryPath )
        {
            var directories = Directory.EnumerateDirectories( directoryPath );
            
            //Debug.Log( directories.Length + " GetDir: " + directoryPath );
            foreach ( string directory in directories )
            {
                // The name of the directory is the "campaign name" we show in the list of campaigns
                // and what all saves under that directory are associated with.
                string campaignName = Path.GetFileNameWithoutExtension( directory );
                
                // Skip this specially named folder which is for the "last lobby settings".
                if ( campaignName.Equals( "_Internal", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    if (Debug) LOG.Msg("Skipping campaign folder '{0}' because always skipped.", directory);
                    continue; 
                }
                
                DateTime mostRecentTime = NullDate;
                
                // Campaign folders only appear if there are actual .save files under them.
                // Also we want to know the most recent time a file under this campaign folder
                // has been modified, for ordering of the campaigns.
                var info = new DirectoryInfo(directory);
                //bool saves = false;
                foreach (var fi in info.EnumerateFiles("*.save"))
                {
                    //if (fi.LastWriteTime > mostRecentTime)
                        //mostRecentTime = fi.LastWriteTime;
                    mostRecentTime = info.LastWriteTime;
                    //saves = true;
                    break;
                }
                    
                if (mostRecentTime == NullDate)
                {
                    if (Debug) LOG.Msg("Skipping campaign folder '{0}' because contains no .save(s).", directory);
                    continue;
                }

                //Debug.Log( "campaignName: "+ campaignName );
                
                CampaignOrQuickstartGroup group;
                SaveLoadMethods.GetCampaign(campaignName, true, out group);
                group.DisplayName = SaveGameData.DecodeCondensedSaveName( campaignName );
                
                if (group.TimeOfLastSave < mostRecentTime)
                    group.TimeOfLastSave = mostRecentTime;
            }
        }
        
        #region GetMostRecentSaveModifiedDate
        public static DateTime GetMostRecentSaveModifiedDate( CampaignOrQuickstartGroup CampaignName )
        {
            if ( CampaignName == null )
            {
                UnityEngine.Debug.LogError( "Null CampaignName!" );
                return NullDate;
            }

            DateTime newestSave = NullDate;
            for ( int j = 0; j < CampaignName.Directories.Count; j++ )
            {
                CampaignOrQuickstart_SubFolder savesDirectoryPath = CampaignName.Directories[j];
                if ( !Directory.Exists( savesDirectoryPath.Directory ) )
                    return NullDate; //such as the first time you save a new campaign.

                var files = Directory.EnumerateFiles( savesDirectoryPath.Directory, "*" + Engine_Universal.SaveMainExtension );
                if ( files == null )
                    return NullDate;

                foreach ( string fileName in files )
                {
                    DateTime lastMod = File.GetLastWriteTime( fileName );
                    if ( lastMod > newestSave )
                        newestSave = lastMod;
                }
            }
            return newestSave;
        }
        #endregion
        
        #region GetIsCampaignIronman
        public static bool GetIsCampaignIronman( CampaignOrQuickstartGroup CampaignName )
        {
            if ( CampaignName == null )
            {
                UnityEngine.Debug.LogError( "Null CampaignName!" );
                return false;
            }

            DateTime newestSave = NullDate;
            for ( int j = 0; j < CampaignName.Directories.Count; j++ )
            {
                CampaignOrQuickstart_SubFolder savesDirectoryPath = CampaignName.Directories[j];
                if ( !Directory.Exists( savesDirectoryPath.Directory ) )
                    return false;

                var files = Directory.EnumerateFiles( savesDirectoryPath.Directory, "*" + Engine_Universal.SaveMainExtension );
                if ( files == null )
                    return false;

                foreach ( string fileName in files )
                {
                    if ( fileName.Contains( SaveLoadMethods.IronmanPrefix ) )
                        return true;
                }
            }
            return false;
        }
        #endregion

        /// <summary>
        /// This method clears and repopulates the passed list of saves in the passed FolderGroup.
        /// ie. CampaignOrQuickstartGroup.SortedSavesInFolder
        /// </summary>
        public static void PopulateSavesInGroup( CampaignOrQuickstartGroup FolderGroup )
        {
            FolderGroup.SortedSavesInFolder.Clear(true);

            Comparison<SaveGameData> compare;
            if (FolderGroup.Type == SaveType.Quickstart)
                compare = SaveLoadMethods.CompareQuickstarts;
            else
            {
                var mode = (SaveSortMode)GameSettings_AIW2.Current.SaveLoadSortType;
                if (mode == SaveSortMode.GameTime)
                    compare = SaveLoadMethods.CompareSaves_GameTime;
                else if (mode == SaveSortMode.Date)
                    compare = SaveLoadMethods.CompareSaves_Date;
                else //if (mode == SaveSortMode.Name)
                    compare = SaveLoadMethods.CompareSaves_Name;
            }
            
            for ( int j = 0; j < FolderGroup.Directories.Count; j++ )
            {
                var folder = FolderGroup.Directories[j];
                var dir = folder.Directory;
                
                // such as the first time you save a new campaign.
                if ( !Directory.Exists( dir ) )
                    continue;

                var filelist = Directory.EnumerateFiles( dir, "*" + Engine_Universal.SaveMainExtension );
                if ( filelist == null ) 
                    continue;

                foreach ( var filename in filelist )
                {
                    if (FolderGroup.Type == SaveType.Quickstart)
                    {
                        if (IsDeleted(filename))
                        {
                            if (Debug) LOG.Msg("Skip file '{0}' because listed in DeletedManifest.", filename);
                            continue;
                        }
                    }
                    
                    var saveGame = ParseSaveInGroup( filename, FolderGroup );
                    if (saveGame == null)
                    {
                        // skipped or failed, already logged within the call
                        // if quickstart without a .tooltip file or one that is blank then this is normal
                    }
                    else
                    {
                        FolderGroup.SortedSavesInFolder.BinaryInsert(saveGame, compare);
                    }
                }
            }
        }

        #region Compare Methods

        private static int CompareQuickstartGroups( CampaignOrQuickstartGroup a, CampaignOrQuickstartGroup b )
        {
            int val;
            
            val = a.SortOrder.CompareTo(b.SortOrder);
            if (val != 0) return val;
            
            val = a.SortByStr.CompareTo(b.SortByStr);
            if (val != 0) return val;
            
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }

        private static int CompareQuickstarts( SaveGameData a, SaveGameData b )
        {
            int val;
            
            val = a.sortOrder.CompareTo(b.sortOrder);
            if (val != 0) return val;
            
            val = a.SortByStr.CompareTo(b.SortByStr);
            if (val != 0) return val;
            
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }
        
        private static int CompareCampaigns( CampaignOrQuickstartGroup a, CampaignOrQuickstartGroup b )
        {
            int val;
            
            val = b.TimeOfLastSave.CompareTo(a.TimeOfLastSave);
            if (val != 0) return val;
            
            val = a.SortByStr.CompareTo(b.SortByStr);
            if (val != 0) return val;
            
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }
        
        private static int CompareSaves_GameTime( SaveGameData a, SaveGameData b )
        {
            int val = ( b.secondsSinceGameStart.CompareTo( a.secondsSinceGameStart ) );
            if ( val != 0 ) return val;
            
            val = b.lastModified.CompareTo( a.lastModified );
            if ( val != 0 ) return val;
            
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }

        private static int CompareSaves_Date( SaveGameData a, SaveGameData b )
        {
            int val = b.lastModified.CompareTo( a.lastModified );
            if ( val != 0 ) return val;
            
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }
        
        private static int CompareSaves_Name( SaveGameData a, SaveGameData b )
        {
            int val = a.SortByStr.CompareTo(b.SortByStr);
            if (val != 0) return val;
            
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }

        #endregion
        
        private static SaveGameData ParseSaveInGroup( string FullFilename, CampaignOrQuickstartGroup Group )
        {
            FileInfo info = new FileInfo( FullFilename );
            Int64 filesizeInBytes = info.Length;
            DateTime dt = info.LastWriteTime;

            if (Debug) LOG.Msg("ParseSaveInGroup {0} Type={1}", Group.DisplayName, Group.Type);
            
            string campaignName = Group.DisplayName;
            
            SaveGameData saveGame = SaveGameData.Load( FullFilename );
            saveGame.campaignName = campaignName;
            saveGame.filesizeInBytes = filesizeInBytes;
            saveGame.lastModified = dt;
            
            saveGame.metaExists = SaveLoadMethods.LoadOrCreateMetaFile( Group, saveGame );
            
            TooltipFileData tipdata;
            saveGame.tooltipExists = SaveLoadMethods.LoadTooltipFromDisk( FullFilename, out tipdata );
            
            if (saveGame.tooltipExists)
            {
                if (!string.IsNullOrEmpty(tipdata.DisplayName))
                    saveGame.saveName = tipdata.DisplayName;
                
                saveGame.sortOrder = tipdata.SortOrder;
                
                if (!string.IsNullOrEmpty(tipdata.Tooltip))
                    saveGame.TooltipData = tipdata.Tooltip;
            }
            
            if (Group.Type == SaveType.Quickstart)
            {
                saveGame.isquickstart = true;
                
                if (saveGame.filesizeInBytes > (20 * 1024))
                    saveGame.isscenario = true;
                
                /*
                if (!saveGame.tooltipExists || 
                    string.IsNullOrEmpty(tipdata.Tooltip))
                {
                    if (Debug) LOG.Msg("Quickstart {0}/{1} has null or empty .tooltip file and will not be shown.", campaignName, FullFilename);
                    saveGame.ReturnToPool();
                    return null;
                }
                */
                
                if (saveGame.DlcInUse.Any(itr=>!itr.IsInstalledAtAll))
                {
                    if (Debug) LOG.Msg("Quickstart {0}/{1} is using an uninstalled expansion and will not be shown.", campaignName, FullFilename);
                    saveGame.ReturnToPool();
                    return null;
                }
            }
            
            if ( Debug ) LOG.Msg( "Parsing {3} {0} -> {1} so added to {4} {2}.", FullFilename, saveGame.saveName, saveGame.campaignName, saveGame.isquickstart ? "Quickstart" : "Save", Group.Type );

            return saveGame;
        }

        private static void MoveLooseSavesToOldSavesFolder()
        {
            string directoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/";
            
            var files = Directory.EnumerateFiles( directoryPath, "*" + Engine_Universal.SaveMainExtension );
            if ( files == null )
                return;
            
            string directoryPathForOldSaves = Engine_Universal.CurrentPlayerDataDirectory + "Save/Old Saves/";
            if ( !Directory.Exists( directoryPathForOldSaves ) )
                Directory.CreateDirectory( directoryPathForOldSaves );
            
            foreach ( var src in files )
            {
                try
                {
                    var dst = directoryPathForOldSaves + Path.GetFileNameWithoutExtension(src) + Engine_Universal.SaveMainExtension;
                    File.Move( src, dst );
                }
                catch { }
            }
        }

        /// <summary>
        /// Returns the text read from any .tooltip file with the same name as the passed .save (if it exists)
        /// You may actually pass in a file path with any extension, the part used is the filename.
        /// If no such file exists or it is zero length, then false is returned and the value of Data will be null.
        /// </summary>
        public static bool LoadTooltipFromDisk( string FullSaveName, out TooltipFileData Data )
        {
            Data = default;
            
            var tippath = Path.ChangeExtension(FullSaveName, Engine_Universal.SaveTooltipExtension);
            var tipinfo = new FileInfo(tippath);
            if (!tipinfo.Exists || tipinfo.Length == 0)
                return false;
            
            var data = new TooltipFileData()
            {
                DisplayName = null,
                Tooltip = null,
                SortOrder = 0,
                IsCommunity = false,
            };
            
            var buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("SaveLoadMethods.LoadTooltipFromDisk");
            
            try
            {
                var lines = File.ReadAllLines(tippath);
                for (int i = 0; i < lines.Length; i++)
                {
                    var ln = lines[i];
                    if (ln.StartsWith("#showas:"))
                    {
                        data.DisplayName = ln.Substring(8).Trim();
                        if (string.IsNullOrEmpty(data.DisplayName))
                        {
                            data.DisplayName = null;
                            LOG.Log(ErrorIfDebug, "Error parsing file \"{0}\" line #{1} \"{2}\"; value \"{3}\" was expected to be a non-empty string.", tippath, i+1, ln, data.DisplayName.OrNull() );
                        }
                    }
                    else if (ln.StartsWith("#sortorder:"))
                    {
                        var str = ln.Substring(11).Trim();
                        if (!int.TryParse(str, out data.SortOrder))
                        {
                            data.SortOrder = 0;
                            LOG.Log(ErrorIfDebug, "Error parsing file \"{0}\" line #{1} \"{2}\"; value \"{3}\" was expected to be Integer.", tippath, i+1, ln, str );
                        }
                    }
                    else if (ln.StartsWith("#community:"))
                    {
                        var str = ln.Substring(11).Trim();
                        if (!bool.TryParse(str, out data.IsCommunity))
                        {
                            data.IsCommunity = false;
                            LOG.Log(ErrorIfDebug, "Error parsing file \"{0}\" line #{1} \"{2}\"; value \"{3}\" was expected to be Boolean.", tippath, i+1, ln, str );
                        }
                        else
                        {
                            //LOG.Msg("File \"{0}\" line #{1} \"{2}\"; value \"{3}\" has been set for COMMUNITY.", tippath, i+1, ln, str );
                        }
                    }
                    else
                    {
                        // this is optional, so remove it if its there
                        ln = ln.Replace("#tooltip:", "");
                        buffer.Add(ln);
                        buffer.NewLine();
                    }
                }
                
                data.Tooltip = buffer.ToString().Trim();
                
                Data = data;
                
                return true;
            }
            catch (Exception e)
            {
                LOG.Err("Error in SaveLoadMethods.LoadTooltipFromDisk:\n{0}", e);
            }
            finally
            {
                buffer?.ReturnToPool();
            }
            
            return false;
        }

        public static bool SaveWorldToDisk_AsQuickStart( string qsname=null, string qsfolder=null, bool as_scenario=false, Log log=null )
        {
            int debugstage = 0;
            try
            {
                if (string.IsNullOrEmpty(qsname))
                {
                    qsname = World.Instance.CampaignName;
                    if (string.IsNullOrEmpty(qsname))
                    {
                        var date = DateTime.Now;
                        qsname = string.Format("newquickstart__{0}_{1}_{2}__{3}_{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute);
                    }
                }
                
                bool community = false;
                string saveDirectoryPath;
                if (string.IsNullOrEmpty(qsfolder))
                {
                    community = true;
                    saveDirectoryPath = Engine_Universal.CurrentPlayerDataDirectory + "QuickStarts/";
                }
                else
                    saveDirectoryPath = Engine_Universal.CurrentGameDataDirectory + "QuickStarts2/" + qsfolder + "/";
                
                if ( !Directory.Exists( saveDirectoryPath ) )
                    Directory.CreateDirectory( saveDirectoryPath );

                debugstage = 101;
                var output_buffer = new ArcenSerializationBuffer( ArcenSerializationBuffer.COMMON_SIZE_2_MB, ArcenSerializationBuffer.COMMON_SIZE_500_KB );

                debugstage = 102;
                if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteSerializationLogs" ) )
                    output_buffer.StartLogging( "WorldSerialization_Quickstart" );

                try
                {
                    debugstage = 200;
                    World.Instance.SerializeTo( SerMetaData.Stripped, output_buffer, 
                                                (as_scenario ? SpecialSerializationType.None : SpecialSerializationType.QuickStart), 
                                                SerializationCommandType.NormalFullType_Disk );
                }
                finally
                {
                    output_buffer.EndLogging();
                }

                debugstage = 300;
                var fullSaveName = saveDirectoryPath + qsname + Engine_Universal.SaveMainExtension;

                debugstage = 301;
                if ( File.Exists( fullSaveName ) )
                {
                    debugstage = 302;
                    File.Delete( fullSaveName );
                }
                
                debugstage = 303;
                output_buffer.WriteToStream( fullSaveName );

                debugstage = 304;
                var save = SaveGameData.Create_ForWorld(World_AIW2.Instance, qsname);
                save.saveFullFilename = fullSaveName;
                save.community = community;
                save.isquickstart = true;
                save.isscenario = as_scenario;
                save.PlayersString = null;
                
                debugstage = 306;
                save.SaveMetaData(true);
                
                //LOG.Msg("author={0}\nlocal prof={1}", save.author.OrNull(), PlayerProfile.Local.DisplayName);
                /*
                debugstage = 400;
                var fullTooltipName = saveDirectoryPath + qsname + Engine_Universal.SaveTooltipExtension;
                
                debugstage = 401;
                Engine_Universal.WriteReplacementTextToFile( fullTooltipName, qstooltip );
                */
                    
                debugstage = 402;
                log?.AppendFormat("Saved quickstart: {0}\n", fullSaveName);
                
                try
                {
                    Process.Start( Path.GetDirectoryName(fullSaveName) );
                }
                catch (Exception e)
                {
                    LOG.Msg("Error trying to open OS location of the just saved quickstart.\n{0}", e);
                }
                
                return true;
            }
            catch (Exception e)
            {
                LOG.Err("Exception in SaveWorldToDisk_AsQuickStart() at debugstage {0}; qsname={1}, qsfolder={2}; exception:\n{4}", 
                        debugstage, qsname, qsfolder, e);
                
                log?.AppendFormat("Error saving quickstart.\n");
                
                return false;
            }
        }
        
        public static void SaveWorldToDisk( string SaveName )
        {
            var aiw2_world = World_AIW2.Instance;
            var world = aiw2_world.Universal_World;
            
            if ( ArcenNetworkAuthority.GetIsClientMode() )
                return;
            
            if ( (ArcenTime.TimeSinceStartF - world.lastSaveTime) < 2 )
                return;

            // 编码使用局部变量，绝不写回 world.CampaignName：
            // 运行期 CampaignName 应为解码态（中文），一旦写回编码串，
            // 后续再次保存会对其再次 EncodeForCondensedFormat 产生二次编码（~~uXXXX）。
            string encodedCampaignName = ArcenStrings.MakeValidFilename( world.CampaignName, false );
            encodedCampaignName = SaveGameData.EncodeForCondensedFormat( encodedCampaignName );
            encodedCampaignName = encodedCampaignName.ConvertToCondensedFormat();

            SaveName = ArcenStrings.MakeValidFilename( SaveName, false );
            SaveName = SaveGameData.EncodeForCondensedFormat( SaveName );
            SaveName = SaveName.ConvertToCondensedFormat();

            world.lastSaveTime = ArcenTime.TimeSinceStartF;
            world.LastTimeStateWasSavedOrSomethingLikeThat = ArcenTime.TimeSinceStartF;

            //ArcenDebugging.ArcenDebugLog( "calling SaveWorldToDisk" );
            
            string saveDirectoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/";
            if ( encodedCampaignName.Length > 0 )
                saveDirectoryPath += encodedCampaignName + "/";

            if ( !Directory.Exists( saveDirectoryPath ) )
                Directory.CreateDirectory( saveDirectoryPath );

            var outputBuffer = new ArcenSerializationBuffer( ArcenSerializationBuffer.COMMON_SIZE_2_MB, ArcenSerializationBuffer.COMMON_SIZE_500_KB );
            
            if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteSerializationLogs" ) )
                outputBuffer.StartLogging( "WorldSerialization" );
            
            try
            {
                world.SerializeTo( SerMetaData.Stripped, outputBuffer, SpecialSerializationType.None, SerializationCommandType.NormalFullType_Disk );
            }
            catch ( Exception )
            {
                throw;
            }
            finally
            {
                outputBuffer.EndLogging();
            }

            string saveFilename = saveDirectoryPath + SaveName + Engine_Universal.SaveMainExtension;

            bool fileExisted = false;
            string backupFilename = string.Empty;
            if ( File.Exists( saveFilename ) )
            {
                fileExisted = true;
                backupFilename = saveDirectoryPath + SaveName + ".bak";
                try
                {
                    File.Copy( saveFilename, backupFilename, true );
                }
                catch ( Exception e )
                {
                    backupFilename = string.Empty;
                    ArcenDebugging.ArcenDebugLog( e );
                    ArcenDebugging.ArcenDebugLog( "Could not save your game!  We tried to create a backup but your operating system told us access was denied.  This may be a temporary condition that will resolve itself in a few minutes, but we did not want to proceed with the actual save operation as that involves deleting the existing save file, which could leave you with no save if the operating system continued to forbid us write access." +
                        "\n\nYou can save to a filename that doesn't already exist to avoid the need for creating a backup.", Verbosity.ShowAsError );
                    return;
                }
                File.Delete( saveFilename );
            }
            try
            {
                //if ( GC.GetTotalMemory( false ) > 400 )
                //    GC.Collect();
                outputBuffer.WriteToStream( saveFilename );
            }
            catch ( Exception e )
            {
                bool backupRecoverySuccessful = !fileExisted;
                if ( fileExisted && !ArcenStrings.IsEmpty( backupFilename ) )
                {
                    try
                    {
                        File.Copy( backupFilename, saveFilename );
                        backupRecoverySuccessful = true;
                    }
                    catch { }
                }

                ArcenDebugging.ArcenDebugLog( e );
                ArcenDebugging.ArcenDebugLog( backupRecoverySuccessful ? "Could not save your game!  Your operating system told us access was denied.  This may be a temporary condition that will resolve itself in a few minutes." :
                    "Could not save your game!  Your operating system told us access was denied.  YOUR EXISTING SAVE FILE WAS DELETED to clear the way for the save operation, and the backup we created beforehand also could not be recovered." +
                    "\n\nYou can try to save again, possibly using a different filename, to avoid losing progress.", Verbosity.ShowAsError );
            }

            // now save the metadata file
            try
            {
                var save = SaveGameData.Create_ForWorld(aiw2_world, SaveName);
                save.SaveMetaData();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( e );
            }
        }

        /// <summary>
        /// Returns the lines of text from the .savemet file with the same name as the passed .save
        /// If one does not exist, it will be created from the .save
        /// </summary>
        public static bool LoadOrCreateMetaFile( CampaignOrQuickstartGroup Group, SaveGameData Save )
        {
            //string FullSaveName, bool IsQuickStart, bool Overwrite, List<string> OutSaveMeta
                
            Log log = null;
            if (Debug)
                log = Log.Yes;
            
            SaveGameData temp = SaveGameData.Create(null, null, null, DateTime.Now, 0, null, null);
            
            int debugstage = 0;
            try
            {
                log?.Msg("LoadOrCreateMetaFile for '{0}'", Save.saveName);
                
                debugstage = 10;
                //string metadataFilename = Save.saveFullFilename.Replace( Engine_Universal.SaveMainExtension, Engine_Universal.SaveMetadataExtension );
                bool exists = false;
                bool resaveAll = ResaveAllSaveMeta;
                bool resaveMissing = SaveMissingSaveMeta;
                bool resave = resaveAll || resaveMissing;
                
                //int tries = 0;
                //retry:
                if (resave && !World_AIW2.Instance.IsOutsideOfNormalGameplay)
                {
                    //System.Threading.Thread.Sleep(1);
                    //tries++;
                    //if (tries < 100)
                        //goto retry;
                    
                    if (log != null)
                    {
                        log.AppendFormat("Disabled resave because we are in gameplay.\n\tInSetupPhase={0} IsMapCurrentlyGenerating={1} RunStatus={2}\n", 
                                         World_AIW2.Instance.InSetupPhase, Mapgen.IsMapCurrentlyGenerating, Engine_Universal.RunStatus);
                        log.Flush();
                    }

                    resave = false;
                }

                debugstage = 20;
                if (Save.LoadMetaData())
                {
                    exists = true;

                    log?.Msg(".savemet exists, loaded values are:\n{0}", ObjToStr.Format(Save, ObjToStr.Style.TypeAndMembersMultiLine));
                }
                else
                {
                    log?.Msg(".savemet does not exist, default values are:\n{0}", ObjToStr.Format(Save, ObjToStr.Style.TypeAndMembersMultiLine));
                }
                
                //var prev = SaveGameData.Create(null, FullSaveName, null, DateTime.Now, 0, );
                
                if ( exists && 
                     resaveAll )
                {
                    log?.Msg("File {0} has .savemet (deleting).", Save.saveName);   
                    File.Delete(Path.ChangeExtension(Save.saveFullFilename, Engine_Universal.SaveMetadataExtension));
                    exists = false;
                    
                    if (ResaveNextCount > 0)
                        ResaveNextCount--;
                }
                
                if ( exists )
                {
                    if (Save.campaignName != Group.DisplayName)
                    {
                        log?.Msg("Campaign name loaded was '{0}' but corrected to '{1}'.", Save.campaignName.OrNull(), Group.DisplayName.OrNull());
                        Save.campaignName = Group.DisplayName;
                    }
                    
                    log?.Msg("File {0} has .savemet", Save.saveName);
                    return true;
                }
                
                if (resave)
                {
                    LOG.Msg("File {0} has no .savemet", Save.saveName);

                    debugstage = 30;
                    if (!File.Exists(Save.saveFullFilename))
                    {
                        LOG.Msg("File {0} missing .save too, so nothing to do.", Save.saveName);
                        return false;
                    }
                    
                    debugstage = 30;
                    LOG.Msg("Attempting to load world from {0}.", Save.saveName);
                    
                    debugstage = 30;
                    
                    var flags = LoadFlag.OnlyMetaData | LoadFlag.HideErrors | LoadFlag.IgnoreMissingDlcMod /*| LoadFlag.LoadAsTemplate*/;
                    if (Group.Type == SaveType.Quickstart)
                        flags |= LoadFlag.Quickstart;
                    
                    bool isfilequickstart = Save.isquickstart && Save.filesizeInBytes < (20 * 1024);
                    
                    World world = null;
                    bool done = false;
                    bool failed = false;
                    Action<bool> PostAction =
                        (isloaded) =>
                        {
                            if ( isloaded )
                            {
                                Action a2 = ()=>
                                {
                                    World_AIW2.Instance.Setup.ChangedSinceLastMapGenCall = false;
                                    World.Instance.CampaignName = SaveGameData.DecodeCondensedSaveName( Group.DisplayName );
                                    
                                    log?.Msg("In Post Action #2: world={0:x} campaign={5} numDlc={1} numMods={2} mapType={3} ironman={4}", 
                                            world.GetHashCode(), 
                                            world.ExpansionsInUse.Count, world.XmlModsInUse.Count, 
                                            World_AIW2.Instance.Setup.MapConfig.MapType.InternalName, 
                                            World_AIW2.Instance.Setup.GetBoolBySetting("IronmanMode"),
                                            world.CampaignName.OrNull());
                                    
                                    done = true;
                                };

                                {
                                    World_AIW2.Instance.IsFromQuickLoad = true;
                                    World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true;
                                    //Engine_AIW2.LastWorldSource1 = StartWorldSource1.LoadingMetaData;
                                    //Engine_AIW2.LastWorldSource2 = StartWorldSource2.LoadingQuickStart;
                                    //Engine_AIW2.LastWorldFlags = flags;

                                    var uworld = World_AIW2.Instance.Universal_World;
                                    var iworld = World.Instance;
                                    world = uworld;
                                    
                                    log?.Msg("In Post Action #1: world={0:x} campaign={5} numDlc={1} numMods={2} mapType={3} ironman={4}", 
                                             world.GetHashCode(), 
                                             world.ExpansionsInUse.Count, world.XmlModsInUse.Count, 
                                             World_AIW2.Instance.Setup.MapConfig.MapType.InternalName, 
                                             World_AIW2.Instance.Setup.GetBoolBySetting("IronmanMode"),
                                             world.CampaignName.OrNull());
                                    
                                    temp.setEverything(world);

                                    Engine_Universal.LoadGameNoCampaignNameSet_NeverCallDirectly_P2( false );

                                    Mapgen.GenerateMap(null, a2);
                                }
                            }
                            else
                            { 
                                failed = true;
                            }
                        };
                
                    world = World.Instance;

                    log?.Msg("Before Load : world={0:x} campaign={5} numDlc={1} numMods={2} mapType={3} ironman={4}", 
                            world.GetHashCode(), 
                            world.ExpansionsInUse.Count, world.XmlModsInUse.Count, 
                            World_AIW2.Instance.Setup.MapConfig.MapType.InternalName, 
                            World_AIW2.Instance.Setup.GetBoolBySetting("IronmanMode"),
                            world.CampaignName.OrNull());
                    
                    var s2 = isfilequickstart ? StartWorldSource2.LoadingQuickStart : StartWorldSource2.LoadingSaveGame;
                    Engine_AIW2.LoadGameNoCampaignNameSet( Save.saveFullFilename, StartWorldSource1.LoadingMetaData, s2, true, PostAction );

                    while (!done && !failed)
                        System.Threading.Thread.Sleep(1);
                    
                    if (failed)
                    {
                        LOG.Msg("World failed to load for {0}.", Save.saveName);
                    }
                    else
                    {
                        log?.Msg("World #{0:x} appears to have been loaded from {1}.", world.GetHashCode(), Save.saveName);
                        
                        debugstage = 80;

                        if (Group.Type == SaveType.Quickstart)
                        {
                            var author = Save.author;
                            var mapType = Save.mapType;
                            
                            Save.setEverything(world);
                            Save.author = author;
                            Save.PlayersString = null;
                            Save.campaignName = Group.DisplayName;
                            //Save.setMapType(mapType);
                            Save.isquickstart = true;
                            Save.isscenario = !isfilequickstart;
                            Save.community = Group.GetSubfolderContainingSave(Save.saveFullFilename)?.IsCommunity ?? false;
                            
                            log?.Msg("SaveGameData before save:\n{0}", ObjToStr.Format(Save, ObjToStr.Style.TypeAndMembersMultiLine));
                            
                            Save.DlcInUse.Clear();
                            Save.DlcInUse.AddRange(temp.DlcInUse);
                            Save.ModInUse.Clear();
                            Save.ModInUse.AddRange(temp.ModInUse);
                        }
                        else
                        {
                            Save.setEverything(world);
                            Save.campaignName = Group.DisplayName;
                            Save.isquickstart = false;
                            Save.isscenario = false;
                        }
                        
                        Save.SaveMetaData();

                        debugstage = 110;
                        LOG.Msg("Created missing .savemet for '{0}'.", Save.saveName);
                    }

                    ArcenThreading.DoOnWorldClear();
                    World_AIW2.Instance.ResetForQuittingToMainMenu(); //extra clearing of stuff!
                    World_AIW2.Instance.ResetForNewMapGeneration( null );
                    Engine_Universal.ClearAllTraceOfExistingGame();
                    World.ClearAllState( ClearStateType.YesClearPlayerAccounts );
                    Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.ModalMenu );
                    Engine_AIW2.Instance.ClearAllGalaxyIcons();
                
                    return !failed;
                }
                
                return false;
            }
            catch (Exception e)
            {
                LOG.Err("Error at debugstage {0}:\n{1}", debugstage, e);
                return false;
            }
        }

        public static bool RestartCampaignData( bool IsForLobby, string CampaignNameToUse, bool IsScenario )
        {
            Log?.Msg("{0} called. IsForLobby={1} CampaignNameToUse={2} IsScenario={3}", 
                         LOG.MethodName(), IsForLobby, CampaignNameToUse, IsScenario);
            
            if ( !IsForLobby && string.IsNullOrEmpty(CampaignNameToUse) )
            {
                LOG.Err( "Could not restart campaign data, because the campaign name is empty!");
                return false;
            }
            
            if ( IsForLobby )
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;

            //ArcenDebugging.ArcenDebugLogSingleLine( "Factions in long-term: " + World_AIW2.Instance.SetupStoredLongTerm.FactionConfigurations.Count + "\n" +
            //    "Factions in lobby: " + World_AIW2.Instance.SetupWorkingForLobbyOnly.FactionConfigurations.Count + "\n" +
            //    "Factions in world: " + World_AIW2.Instance.Factions.Count + "\n", Verbosity.DoNotShow );

            World_AIW2.Instance.IsFromQuickLoad = true;
            World_AIW2.Instance.Setup.ShouldSeedDetailsYet = !IsForLobby;
            
            void PostGeneration()
            {
                Log?.Msg("PostGeneration() called. CampaignNameToUse={0}", CampaignNameToUse.OrNull());
                
                World_AIW2.Instance.Setup.ChangedSinceLastMapGenCall = false;
                
                if (!string.IsNullOrEmpty(CampaignNameToUse))
                    World.Instance.CampaignName = CampaignNameToUse;

                if ( !IsForLobby && !ArcenNetworkAuthority.IsClient && !GameSettings.Current.GetBoolBySetting("StartPaused"))
                    World.Instance.IsPaused = false;
            };
            
            if (IsScenario)
            {
                World_AIW2.Instance.DoFinalBitsAfterMapGenerationOrFixingSavegame( Galaxy.Current, false, true, false );
                PostGeneration();
            }
            else
            {
                Mapgen.GenerateMap( null, PostGeneration );
            }
            
            return true;
        }
        
        #region TakeIronmanSave
        public static void TakeIronmanSave( bool quitAfterwards, bool quitToOSAfterwards )
        {
            string campaignName = World.Instance.CampaignName;
            if ( ArcenStrings.IsEmpty( campaignName ) )
            {
                ArcenDebugging.ArcenDebugLog( "Could not ironman-save, because the campaign name is empty!", Verbosity.ShowAsError );
                return;
            }

            int debugCode = 0;
            try
            {
                debugCode = 100;
                
                string saveGameName = SaveLoadMethods.IronmanPrefix + Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ); //generate a new save name so it won't overwrite
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SaveGame], GameCommandSource.AnythingElse );
                
                debugCode = 200;
                
                command.RelatedString = saveGameName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, false );
                GameSettings.Current.LastSavegameFile = saveGameName;
                GameSettings.Current.LastSavegameCampaign = campaignName;
                GameSettings.SaveToDisk();
                
                debugCode = 300;
                
                //Now delete all saves except the one just made
                GameCommandType type = BaseGameCommand.CommandsByCode[BaseGameCommand.Code.DeleteAllSaveGames];
                if ( type == null )
                    throw new Exception( "Could not find game command for code " + BaseGameCommand.Code.DeleteAllSaveGames + " (" + (int)BaseGameCommand.Code.DeleteAllSaveGames + "); this array is of length " + BaseGameCommand.CommandsByCode.Size() );
                
                command = GameCommand.Create( type, GameCommandSource.AnythingElse );
                
                debugCode = 400;
                
                command.RelatedString = saveGameName; //save this one
                command.RelatedString2 = campaignName;
                command.RelatedString3 = "Delete_Ironman_Only";
                command.RelatedBool = quitAfterwards;
                
                debugCode = 500;
                
                if ( quitToOSAfterwards )
                    command.RelatedBools.Add( true );
                
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, false );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during TakeIronmanSave debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

#region unused
#if false
        public static void ResaveAllQuickstartMeta()
        {
            SaveLoadMethods.ResaveNextCount = 10;
            return;
            
            var task = new Task(
                ()=>
                    {
                        LOG.Msg("Task ResaveAllQuickstartMeta starting.");
                        foreach (var itr in resaveNextQuickstartMeta())
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                        LOG.Msg("Task ResaveAllQuickstartMeta done.");
                    });
            task.Start();
        }
        
        private static Sys.IEnumerable<int> resaveNextQuickstartMeta()
        {
            var lines = ArcenStrings.GetTemporaryStringList("SaveLoadMethods.ResaveAllQuickstartMeta", 5);
            
            int numToDo = 999;
            int numFails = 0;
            bool gonext = false;
            bool stop = false;
            bool retry = false;
                    
            PopulateQuickstartGroups();
            
            foreach (var group in SortedQuickstartGroups)
            {
                PopulateSavesInGroup( group );
                
                foreach (var qs in group.SortedSavesInFolder)
                {
                    again:
                    
                    Engine_AIW2.Instance.QuitGameAndGoBackToMainMenu();
                    
                    //System.Threading.Thread.Sleep(10);
                    
                    //EndpointFunctions.SetDefaultsForLobby();
                    
                    LOG.Msg("Processing qs {0}.", qs.saveName);
                    
                    LoadMetaFile(qs.saveFullFilename, lines);
                    qs.LoadMetaData(qs.campaignName, lines);

                    gonext = false;
                    stop = false;
                    retry = false;
                    
                    if (!string.IsNullOrEmpty(Engine_Universal.LastErrorText))
                    {
                        LOG.Msg("Error popup showing trying to load save {0}.", qs.saveName);
                        stop = true;
                                
                        //System.Threading.Thread.Sleep(20);
                        //Window_ErrorReportMenu.Instance.Close();
                        //retry = true;
                    }
                    
                    Action PostAction2 = ()=>
                        {
                            if (!string.IsNullOrEmpty(Engine_Universal.LastErrorText))
                            {
                                System.Threading.Thread.Sleep(20);
                                Window_ErrorReportMenu.Instance.Close();
                                retry = true;
                                return;
                            }
                            
                            LOG.Msg("Resaving .savemet for {0}.", qs.saveName);

                            World_AIW2.Instance.Setup.ChangedSinceLastMapGenCall = false;
                            
                            if (!string.IsNullOrEmpty(qs.saveName))
                                World.Instance.CampaignName = qs.saveName;
                            
                            //foreach (var fac in World_AIW2.Instance.Factions)
                            //{
                            //    LOG.Msg("fac:{0}", fac.GetDisplayName());
                            //}
                            
                            var author = qs.author;
                            qs.setEverything(World.Instance);
                            qs.author = author;
                            qs.PlayersString = "";
                            
                            lines.Clear();
                            qs.FillMetadataList(lines);
                    
                            var metafile = Path.ChangeExtension(qs.saveFullFilename, Engine_Universal.SaveMetadataExtension);
                            File.WriteAllLines(metafile, lines);
                            
                            gonext = true;
                            //stop = true;
                        };
                            
                    Action<bool> PostAction = (success)=>
                        {
                            if (!success)
                            {
                                LOG.Msg("Failed loading save {0}.", qs.saveName);
                                stop = true;
                                
                                return;
                            }
                            
                            if (!string.IsNullOrEmpty(Engine_Universal.LastErrorText))
                            {
                                LOG.Msg("Error popup showing trying to load save {0}.", qs.saveName);
                                stop = true;
                                //System.Threading.Thread.Sleep(20);
                                //Window_ErrorReportMenu.Instance.Close();
                                //retry = true;
                                
                                return;
                            }
                            
                            LOG.Msg("Loaded save {0}.", qs.saveName);
     
                            //System.Threading.Thread.Sleep(10);
                            
                            //World_AIW2.Instance.IsFromQuickLoad = true;
                            //World_AIW2.Instance.Setup.ShouldSeedDetailsYet = false;
                            //Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                            //Mapgen.GenerateMap( null, PostAction2 );
                            
                            
                        };

            //ArcenDebugging.ArcenDebugLogSingleLine( "Factions in long-term: " + World_AIW2.Instance.SetupStoredLongTerm.FactionConfigurations.Count + "\n" +
            //    "Factions in lobby: " + World_AIW2.Instance.SetupWorkingForLobbyOnly.FactionConfigurations.Count + "\n" +
            //    "Factions in world: " + World_AIW2.Instance.Factions.Count + "\n", Verbosity.DoNotShow );
            
                    if (!stop)
                    {
                        try
                        {
                            Engine_AIW2.LoadGameNoCampaignNameSet(qs.saveFullFilename, StartWorldSource1.AnythingElse, StartWorldSource2.LoadingSaveMeta, true, PostAction);
                        }
                        catch (Exception e)
                        {
                            LOG.Msg("Exception loading save {0}.\n{1}", qs.saveName, e);
                            stop = true;
                        }
                    }
                    
                    while (!gonext && !stop && !retry)
                    {
                        System.Threading.Thread.Sleep(20);
                        yield return 1;
                    }
                    
                    if (retry)
                    {
                        numFails++;
                        
                        if (numFails > 10)
                        {
                            LOG.Msg("Reached max fails.");
                            yield break;
                        }
                        
                        goto again;
                    }
                    
                    numToDo--;
                    
                    if (stop || numToDo <= 0)
                    {
                        if (stop)
                            LOG.Msg("Stop specified.");
                        else
                            LOG.Msg("Done processing requested amount.");
                        
                        yield break;
                    }
                    
                    
                }
            }
            
            //                            //var flags = LoadFlag.OnlyMetaData | LoadFlag.LoadAsTemplate | LoadFlag.HideErrors | LoadFlag.IgnoreMissingDlcMod;
            //        //flags |= LoadFlag.Quickstart;
            //        //if (!Engine_Universal.LoadGameNoCampaignNameSet_NeverCallDirectly_P1(qs.saveFullFilename, flags))
            //        //{
            //        //    if (_debug) LOG.Msg("Attempt to load world from {0} failed.", qs.saveFullFilename);
            //        //    continue;
            //        //}
            //    }
            //}
        }
#endif
#endregion
    }
}
