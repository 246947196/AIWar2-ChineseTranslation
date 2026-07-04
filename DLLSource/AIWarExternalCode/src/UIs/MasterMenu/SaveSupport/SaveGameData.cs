//#define POOLED
using System;
using System.IO;
using System.Linq;
using Arcen.AIW2.Core;
using Arcen.Universal;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public enum SaveMetaField
    {
        Null = -1,
        
        MapType,
        Seed,
        GameTime,
        CampaignName,
        MasterAIType,
        Difficulty,
        TimesLoaded,
        PlayerString,
        DlcInUse,
        ModInUse,
        Author,
        Ironman,
        IsQuickstart,
        PlayerType,
        Community,
        IsScenario,
        
        First = MapType,
        Last = IsScenario,
    }
        
    public static partial class SaveMetaField_Ext
    {
        public static string MetaField_Prefix(this SaveMetaField field)
        {
            switch (field)
            {
                case SaveMetaField.MapType:
                    return "#map:";
                case SaveMetaField.Seed:
                    return "#seed:";
                case SaveMetaField.GameTime:
                    return "#game_time:";
                case SaveMetaField.CampaignName:
                    return "#campaign:";
                case SaveMetaField.MasterAIType:
                    return "#hardest_ai:";
                case SaveMetaField.Difficulty:
                    return "#difficulty:";
                case SaveMetaField.TimesLoaded:
                    return "#times_loaded:";
                case SaveMetaField.PlayerString:
                    return "#players:";
                case SaveMetaField.DlcInUse:
                    return "#dlc_used:";
                case SaveMetaField.ModInUse:
                    return "#mods_used:";
                case SaveMetaField.Author:
                    return "#author:";
                case SaveMetaField.Ironman:
                    return "#ironman:";
                case SaveMetaField.IsQuickstart:
                    return "#isquickstart:";
                case SaveMetaField.PlayerType:
                    return "#player_type:";
                case SaveMetaField.Community:
                    return "#community:";
                case SaveMetaField.IsScenario:
                    return "#isscenario:";
                default:
                    return string.Format("Unhandled:{0}", Extensions.ToString(field));
            }
        }
        
        public static SaveMetaField MetaField_FromPrefix(string prefix)
        {
            for (var field = SaveMetaField.First; field <= SaveMetaField.Last; field++)
            {
                if (field.MetaField_Prefix() == prefix)
                    return field;
            }
            
            return SaveMetaField.Null;
        }
        
        public static string MetaField_Label(this SaveMetaField field)
        {
            switch (field)
            {
                case SaveMetaField.MapType:
                    return "Map";
                case SaveMetaField.Seed:
                    return "Seed";
                case SaveMetaField.GameTime:
                    return "Game Time";
                case SaveMetaField.CampaignName:
                    return "Campaign";
                case SaveMetaField.MasterAIType:
                    return "Hardest AI";
                case SaveMetaField.Difficulty:
                    return "Difficulty";
                case SaveMetaField.TimesLoaded:
                    return "Times Loaded";
                case SaveMetaField.PlayerString:
                    return "Players";
                case SaveMetaField.DlcInUse:
                    return "Dlc Used";
                case SaveMetaField.ModInUse:
                    return "Mods Used";
                case SaveMetaField.Author:
                    return "Author";
                case SaveMetaField.Ironman:
                    return "Ironman";
                case SaveMetaField.IsQuickstart:
                    return "";
                case SaveMetaField.PlayerType:
                    return "Player Type";
                case SaveMetaField.Community:
                    return "Community";
                case SaveMetaField.IsScenario:
                    return "Scenario";
                default:
                    return string.Format("Unhandled:{0}", Extensions.ToString(field));
            }
        }
    }

    /* We store the information about any given saved game
       in one of these structures. The metadata is written separately to disk in an accompanying file. */
    public sealed class SaveGameData : ArcenExternalSource, IProtectedListable, IOption
#if POOLED
        , IConcurrentPoolable<SaveGameData>
#endif
    {
        public int seed; //If we insist that the user
                         //really provide a campaign name,
                         //the seed isn't really necessary
        public int secondsSinceGameStart;
        public string mapType;
        public string mapTypeShort;
        public string campaignName;
        public string saveName;
        public string saveFullFilename;
        public string masterAIType;
        public string difficulty;
        public int numTimesLoaded;
        public Int64 filesizeInBytes;
        public string PlayersString = string.Empty;
        public string author = string.Empty;
        public DateTime lastModified;
        public bool ironman;
        public bool isquickstart;
        public bool isscenario;
        public string playerType;
        public bool community;

        public string TooltipData;
        
        public bool metaExists;
        public bool tooltipExists;
        public int sortOrder;
        
        private string _sortByStr;
        public string SortByStr
        {
            get
            {
                if (_sortByStr == null)
                    _sortByStr = saveName.ToLower().PadRight(30);
                return _sortByStr;
            }
        }
        
        /// <summary>
        /// This list is filled in by parsing the actual .save file for its requirements.
        /// This is separate from the SaveGameData.RowFromExpansion and SaveGameData.RowFromXmlMod
        /// which only describe the location on disk (ie. under Expansions/SomeDLC or XmlMods/SomeMod).
        /// </summary>
        public List<Expansion> DlcInUse = List<Expansion>.Create_WillNeverBeGCed(5, "SaveGameData.DlcInUse");
        public List<XmlMod> ModInUse = List<XmlMod>.Create_WillNeverBeGCed(5, "SaveGameData.ModInUse");
        public List<string> MetaLines = List<string>.Create_WillNeverBeGCed(10, "SaveGameData.MetaLines");
        
#if !POOLED
        private SaveGameData()
            : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
        }
#endif
        /// <summary>
        /// Create a SaveGameData with metadata initialized from the current World_AIW2.Instance.Setup
        /// </summary>
        public static SaveGameData Create_ForWorld( World_AIW2 world, string saveName )
        {
            string campaignName = world.Universal_World.CampaignName;
            if (string.IsNullOrEmpty(campaignName))
                LOG.Msg("Warning: Created SaveGameData '{0}' has no CampaignName set because neither does the current World.", saveName);

            string saveDirectoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/";
            if ( campaignName.Length > 0 )
                saveDirectoryPath += campaignName + "/";

            //if ( !Directory.Exists( saveDirectoryPath ) )
                //Directory.CreateDirectory( saveDirectoryPath );

            string saveFileName = saveDirectoryPath + saveName;

            var seed = World_AIW2.Instance.Setup?.MapConfig?.Seed ?? 0;
            
            SaveGameData data = SaveGameData.Create( 
                saveName, saveFileName, seed, World_AIW2.Instance.GameSecond, campaignName, DateTime.Now, 0, 
                null, null, null, null );
            
            data.setEverything(World_AIW2.Instance.Universal_World);
            //data.setShortMapType( World_AIW2.Instance.Setup.MapConfig.MapType.InternalName );
            //data.setPlayerNames();
            //data.setDlcModInUse();
            //data.setIronman();
            //data.setAuthor();
            //data.setStuff();
            
            return data;
        }

        /// <summary>
        /// Create a SaveGameData with metadata specified as individual arguments.
        /// </summary>
        public static SaveGameData Create( string saveName, string saveFullFilename, int seed, int secondsSinceGameStart, string campaignName, DateTime dt, Int64 FilesizeOnDisk, 
                                           string masterAIType, string difficulty, Expansion FromExpansion, XmlMod FromMod )
        {
#if POOLED
            var save = Pool.GetFromPoolOrCreate();
#else
            var save = new SaveGameData();
#endif

            save.RowFromExpansion = FromExpansion;
            save.RowFromXmlMod = FromMod;
            //save.debug = false;
            save.mapType = string.Empty; //to set the map and mapTypeShort, we call setFullMapType() or setShortMapType()
            save.mapTypeShort = string.Empty;
            save.saveName = saveName;
            save.saveFullFilename = saveFullFilename;
            save.seed = seed;
            save.secondsSinceGameStart = secondsSinceGameStart;
            save.campaignName = campaignName;
            save.lastModified = dt;
            save.masterAIType = masterAIType;
            save.difficulty = difficulty;
            save.filesizeInBytes = FilesizeOnDisk;
            
            return save;
        }

        public static SaveGameData Load( string saveFullFilename )
        {
#if POOLED
            var save = Pool.GetFromPoolOrCreate();
#else
            var save = new SaveGameData();
#endif
            save.saveFullFilename = saveFullFilename;
            save.saveName = Path.GetFileNameWithoutExtension(saveFullFilename);
            save.campaignName = Path.GetDirectoryName(saveFullFilename);
            save.LoadMetaData();
            
            return save;
        }

#if false
        /// <summary>
        /// Create a SaveGameData with metadata specified as text lines.
        /// </summary>
        public static SaveGameData Create( string SaveName, string saveFullFilename, string CampaignName, Sys.IEnumerable<string> SavegameMetaData, DateTime dt, Int64 FilesizeOnDisk,
            Expansion FromExpansion, XmlMod FromMod )
        {
#if POOLED
            var save = Pool.GetFromPoolOrCreate();
#else
            var save = new SaveGameData();
#endif
            save.RowFromExpansion = FromExpansion;
            save.RowFromXmlMod = FromMod;
            //save.debug = false;
            save.mapType = "Unknown";
            save.mapTypeShort = "UK";
            save.seed = -1;
            save.secondsSinceGameStart = -1;
            save.campaignName = CampaignName;
            save.masterAIType = string.Empty;
            save.difficulty = string.Empty;
            save.lastModified = dt;
            save.saveName = SaveName;
            save.saveFullFilename = saveFullFilename;
            save.filesizeInBytes = FilesizeOnDisk;

            save.LoadMetaData( CampaignName, SavegameMetaData );
            
            return save;
        }
#endif
        
        /// <summary>
        /// Create a SaveGameData with none of the metadata.
        /// </summary>
        public static SaveGameData Create( string SaveName, string saveFullFilename, string CampaignName, DateTime dt, Int64 FilesizeOnDisk,
            Expansion FromExpansion, XmlMod FromMod )
        {
#if POOLED
            var save = Pool.GetFromPoolOrCreate();
#else
            var save = new SaveGameData();
#endif
            save.RowFromExpansion = FromExpansion;
            save.RowFromXmlMod = FromMod;
            //save.debug = false;
            save.mapType = "Unknown";
            save.mapTypeShort = "UK";
            save.seed = -1;
            save.secondsSinceGameStart = -1;
            save.campaignName = CampaignName;
            save.masterAIType = string.Empty;
            save.difficulty = string.Empty;
            save.lastModified = dt;
            save.saveName = SaveName;
            save.saveFullFilename = saveFullFilename;
            save.filesizeInBytes = FilesizeOnDisk;
            
            return save;
        }

        public void SaveMetaData(bool debug=false)
        {
            this.MetaLines.Clear();
            for (var f = SaveMetaField.First; f <= SaveMetaField.Last; f++)
                this.MetaLines.Add(Get_MetaField_AsString(f));

            var path = Path.ChangeExtension(this.saveFullFilename, Engine_Universal.SaveMetadataExtension);
            
            if (SaveLoadMethods.Debug || debug)
            {
                LOG.Msg("SaveMetaData for '{0}' called (path='{1}').\nLines to write:\n{2}", 
                        this, path, string.Join("\n",this.MetaLines));
            }
            
            File.WriteAllLines(path, this.MetaLines);
        }
        
        public bool LoadMetaData()
        {
            string metaFullFilename = Path.ChangeExtension(this.saveFullFilename, Engine_Universal.SaveMetadataExtension );
            
            if ( !File.Exists( metaFullFilename ) )
                return false;

            string[] lines = null;
            try
            {
                lines = File.ReadAllLines( metaFullFilename );
            }
            catch
            {
            }

            if ( lines == null )
                return false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (line.StartsWith("#"))
                    this.Set_MetaField_FromString(line);
                else
                    this.Set_MetaField_FromString( SaveMetaField.First + i, line );
            }
            
            return true;
        }

        private string Get_MetaField_AsString( SaveMetaField field )
        {
            string prefix = field.MetaField_Prefix();
            
            switch ( field )
            {
                case SaveMetaField.MapType:
                    return prefix + this.mapTypeShort ?? string.Empty;
                
                case SaveMetaField.Seed:
                    return prefix + this.seed.ToString();
                
                case SaveMetaField.GameTime:
                    return prefix + this.secondsSinceGameStart.ToString();
                    
                case SaveMetaField.CampaignName:
                    return prefix + this.campaignName ?? string.Empty;
                    
                case SaveMetaField.MasterAIType:
                    return prefix + this.masterAIType ?? string.Empty;
                
                case SaveMetaField.Difficulty:
                    return prefix + this.difficulty ?? string.Empty;
                
                case SaveMetaField.TimesLoaded:
                    return prefix + this.numTimesLoaded.ToString();
                
                case SaveMetaField.PlayerString:
                    return prefix + this.PlayersString;
                
                case SaveMetaField.DlcInUse:
                    if (this.DlcInUse.Count == 0)
                        return prefix + string.Empty;
                    else
                        return prefix + string.Join(",", this.DlcInUse.Select(i=>i.InternalName));
                
                case SaveMetaField.ModInUse:
                    if (this.ModInUse.Count == 0)
                        return prefix + string.Empty;
                    else
                        return prefix + string.Join(",", this.ModInUse.Select(i=>i.InternalName));
                
                case SaveMetaField.Author:
                    return prefix + this.author ?? string.Empty;
                
                case SaveMetaField.PlayerType:
                    return prefix + this.playerType;
                
                case SaveMetaField.Community:
                    return prefix + this.community;
                
                case SaveMetaField.IsScenario:
                    return prefix + this.isscenario;
                
                case SaveMetaField.Ironman:
                    return prefix + this.ironman;
                
                case SaveMetaField.IsQuickstart:
                    return prefix + this.isquickstart;
                
                default:
                    return string.Empty;
            }
        }
        
        /// <summary>
        /// Set the field value on this SaveGameData.
        /// The argument should be a string beginning with a proper syntax SaveMetaField prefix followed by a string value to parse.
        /// This method will error (but not throw exceptions) for invalid arguments.
        /// </summary>
        private void Set_MetaField_FromString( string line )
        {
            if (!line.StartsWith("#"))
            {
                LOG.Err("Invalid line \"{0}\" should start with a proper prefix (#fieldname:fieldvalue).", line);
                return;
            }
            
            int idx = line.IndexOf(':');
            if (idx < 0 || idx >= line.Length)
            {
                LOG.Err("Invalid line \"{0}\" should start with a proper prefix (#fieldname:fieldvalue).", line);
                return;
            }

            var prefix = line.Substring(0, idx+1);
            var field = SaveMetaField_Ext.MetaField_FromPrefix(prefix);
            if (field == SaveMetaField.Null)
            {
                LOG.Err("Invalid line \"{0}\" (syntax: \"#fieldname:fieldvalue\") unrecognized SaveMetaField field name \"{1}\".", line, prefix.Substring(1,prefix.Length-2));
                return;
            }
            
            line = line.Remove(0, prefix.Length);
            Set_MetaField_FromString(field, line);
        }
        
        /// <summary>
        /// Set this SaveGameData's value for 'field' by parsing 'val'.
        /// </summary>
        private void Set_MetaField_FromString( SaveMetaField field, string val )
        {
            switch ( field )
            {
                case SaveMetaField.MapType:
                    this.setMapType( val );
                    break;
                
                case SaveMetaField.Seed:
                    if ( !Int32.TryParse( val, out this.seed ) )
                        this.seed = 0;
                    break;
                
                case SaveMetaField.GameTime:
                    if ( !Int32.TryParse( val, out this.secondsSinceGameStart ) )
                        this.secondsSinceGameStart = -1;
                    break;
                
                case SaveMetaField.CampaignName:
                    this.campaignName = val;
                    break;
                
                case SaveMetaField.MasterAIType:
                    this.masterAIType = val;
                    break;
                
                case SaveMetaField.Difficulty:
                    this.difficulty = val;
                    break;
                
                case SaveMetaField.TimesLoaded:
                    if ( !Int32.TryParse( val, out this.numTimesLoaded ) )
                        this.numTimesLoaded = 0;
                    break;
                
                case SaveMetaField.PlayerString:
                    this.PlayersString = val;
                    break;
                
                case SaveMetaField.DlcInUse:
                    this.DlcInUse.Clear();
                    if (!string.IsNullOrEmpty(val))
                    {
                        var dlcs = val.Split(',');
                        foreach (var n in dlcs)
                        {
                            var row = ExpansionTable.Instance.GetRowByNameOrNullIfNotFound(n);
                            if (row == null)
                                LOG.Msg("Warning: SaveGameData \"{0}\" had dlc \"{1}\" in use, which is unrecognized.", this.saveName.OrNull(), n.OrNull());
                            else
                                this.DlcInUse.Add(row);
                        }
                    }
                    break;
                
                case SaveMetaField.ModInUse:
                    this.ModInUse.Clear();
                    if (!string.IsNullOrEmpty(val))
                    {
                        var mods = val.Split(',');
                        foreach (var n in mods)
                        {
                            var row = XmlModTable.Instance.GetRowByNameOrNullIfNotFound(n);
                            if (row == null)
                                LOG.Msg("Warning: SaveGameData \"{0}\" had mod \"{1}\" in use, which is unrecognized.", this.saveName.OrNull(), n.OrNull());
                            else
                                this.ModInUse.Add(row);
                        }
                    }
                    break;
                
                case SaveMetaField.Author:
                    this.author = val ?? string.Empty;
                    break;
                
                case SaveMetaField.PlayerType:
                    this.playerType = val ?? string.Empty;
                    break;
                
                case SaveMetaField.Community:
                    if (!bool.TryParse(val, out this.community))
                        this.community = false;
                    break;
                
                case SaveMetaField.IsScenario:
                    if (!bool.TryParse(val, out this.isscenario))
                        this.isscenario = false;
                    break;
                
                case SaveMetaField.Ironman:
                    if (!bool.TryParse(val, out this.ironman))
                        this.ironman = false;
                    break;
                
                case SaveMetaField.IsQuickstart:
                    if (!bool.TryParse(val, out this.isquickstart))
                        this.isquickstart = false;
                    break;
                
                default:
                    Verbosity verb = SaveLoadMethods.Debug ? Verbosity.ShowAsError : Verbosity.DoNotShow;
                    LOG.Log( verb, "Unhandled SaveMetaField value passed in \"{0}\"", Extensions.ToString(field));
                    break;
            }
        }
        
        /// <summary>
        /// TODO: Right now we call setEverything(world) and do not have the ability
        /// to set them individually.
        /// 
        /// Set this SaveGameData's value for 'field' based on the passed world.
        /// </summary>
        /*
        private void Set_MetaField_FromWorld( SaveMetaField field, World_AIW2 world )
        {
            switch ( field )
            {
                case SaveMetaField.MapType:
                    this.setMapType( line );
                    break;
                
                case SaveMetaField.Seed:
                    if ( !Int32.TryParse( line, out this.seed ) )
                        this.seed = 0;
                    break;
                
                case SaveMetaField.GameTime:
                    if ( !Int32.TryParse( line, out this.secondsSinceGameStart ) )
                        this.secondsSinceGameStart = -1;
                    break;
                
                case SaveMetaField.CampaignName:
                    this.campaignName = line;
                    break;
                
                case SaveMetaField.MasterAIType:
                    this.masterAIType = line;
                    break;
                
                case SaveMetaField.Difficulty:
                    this.difficulty = line;
                    break;
                
                case SaveMetaField.TimesLoaded:
                    if ( !Int32.TryParse( line, out this.numTimesLoaded ) )
                        this.numTimesLoaded = 0;
                    break;
                
                case SaveMetaField.PlayerString:
                    this.PlayersString = line;
                    break;
                
                case SaveMetaField.DlcInUse:
                    if (!string.IsNullOrEmpty(line))
                    {
                        var dlcs = line.Split(',');
                        foreach (var n in dlcs)
                        {
                            var row = ExpansionTable.Instance.GetRowByNameOrNullIfNotFound(n);
                            if (row == null)
                                LOG.Msg("Warning: SaveGameData '{0}' had '{1}' dlc in use, which is unrecognized.", this.saveName.OrNull(), n.OrNull());
                            else
                                this.DlcInUse.Add(row);
                        }
                    }
                    break;
                
                case SaveMetaField.ModInUse:
                    if (!string.IsNullOrEmpty(line))
                    {
                        var mods = line.Split(',');
                        foreach (var n in mods)
                        {
                            var row = XmlModTable.Instance.GetRowByNameOrNullIfNotFound(n);
                            if (row == null)
                                LOG.Msg("Warning: SaveGameData '{0}' had '{1}' mod in use, which is unrecognized.", this.saveName.OrNull(), n.OrNull());
                            else
                                this.ModInUse.Add(row);
                        }
                    }
                    break;
                
                case SaveMetaField.Author:
                    this.author = line ?? string.Empty;
                    break;
                
                case SaveMetaField.PlayerType:
                    this.playerType = line ?? string.Empty;
                    break;
                
                case SaveMetaField.Community:
                    if (!bool.TryParse(line, out this.community))
                        this.community = false;
                    break;
                
                case SaveMetaField.Ironman:
                    if (!bool.TryParse(line, out this.ironman))
                        this.ironman = false;
                    break;
                
                case SaveMetaField.IsQuickstart:
                    if (!bool.TryParse(line, out this.isquickstart))
                        this.isquickstart = false;
                    break;
                
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: too many tokens in ParseOneMetaDataLine; next was " + line + " (index " + (int)field + ")", Verbosity.DoNotShow );
                    break;
            }
        }
        */
        
        /*
        public void FillMetadataList( List<string> ListToFill )
        {
            ListToFill.Clear();
            
            for (var f = SaveMetaField.First; f <= SaveMetaField.Last; f++)
            {
                ListToFill.Add(Get_MetaField_AsString(f));
            }
        }
        */
        
        #region setMapType
        public void setMapType( string value=null)
        {
            if (string.IsNullOrEmpty(value))
            {
                var map = World_AIW2.Instance.Setup.MapConfig.MapType;
                this.mapType = map.GetDisplayName();
                this.mapTypeShort = map.GetShortDisplayName();
            }
            else
            {
                this.mapType = value;
                this.mapTypeShort = value;
            }
        }
        #endregion
        #region setPlayerNames
        public void setPlayerNames()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
            {
                PlayerAccount acc = World.Instance.AllPlayerAccounts[i];
                if ( builder.Length > 0 )
                    builder.Append( ", " );
                builder.Append( "<color=#" ).Append( acc.GetFactionCenterColor().ColorHexBrighter ).Append( ">" ).Append( acc.Username ).Append( "</color>" );
            }
            this.PlayersString = builder.ToString();
        }
        #endregion
        #region setDlcModInUse
        public void setDlcModInUse(World world)
        {
            this.DlcInUse.Clear();
            for (int i = 0; i < world.ExpansionsInUse.Count; i++)
            {
                var exp = world.ExpansionsInUse[i];
                this.DlcInUse.Add(exp);
            }
            
            this.ModInUse.Clear();
            for (int i = 0; i < world.XmlModsInUse.Count; i++)
            {
                var mod = world.XmlModsInUse[i];
                this.ModInUse.Add(mod);
            }
            
            //LOG.Msg("setDlcModInUse (world #{0:x}) found {1} dlc {2} mod",
            //        world.GetHashCode(), this.DlcInUse.Count, this.ModInUse.Count);
        }
        #endregion
        #region setAuthor
        public void setAuthor()
        {
            this.author = PlayerAccount.Local.GetNameInColor();
        }
        #endregion
        #region setIronman
        /// <summary>
        /// Sets the value of SaveGameData.ironman based on the current world.
        /// </summary>
        public void setIronman(World_AIW2 world)
        {
            this.ironman = world.Setup.GetBoolBySetting("IronmanMode");
        }
        #endregion
        #region setStuff
        public void setStuff(World world)
        {
            int maxAiIntensity = 0;
            Faction mostIntenseAi = null;
            int maxAiOrAllyIntensity = 0;
            int numAiOrAlly = 0;
            int numExtraHighImpactHostiles = 0;

            //var setup = World.Instance;
            var ai2_world = world.GameSpecificSubObject as World_AIW2;
            var factions = ai2_world.Factions;
            var configs = ai2_world.Setup.FactionConfigurations;
            int numFactions = factions.Count;
            
            //foreach (var fac in World_AIW2.Instance.Factions)
            if (SaveLoadMethods.Debug) LOG.Msg("setStuff; {0} configs {1} factions", configs.Count, factions.Count);
            
            var playerTypeBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate("SaveGameData.setStuff.playerTypeBuffer");
            
            bool isfallenspire = false;
            for (int i = 0; i < numFactions; i++)
            {
                var fac = factions[i];
                //var fac = Faction.Create(con);
                //fac.DoImmediatelyAfterAllFactionsExist();
                if (SaveLoadMethods.Debug) LOG.Msg("faction[{0}] is {1}", i+1, fac.GetDisplayName());
                
                //LOG.Msg("con={0}, fac={1}", con.GetDisplayNameWithoutPlayerNames(), fac.GetDisplayName());
                
                if (fac.Type == FactionType.Player && !isfallenspire)
                {
                    //fac._factionCenterColor = fac.SpecialFactionData
                    //var bi_root = fac.BaseInfo as ExternalFactionBaseInfoRoot;
                    var p_type = fac.PlayerTypeDataOrNull_ModeratelyExpensive;
                    //var p_specialFacData = fac.SpecialFactionData;
                    
                    //TeamColorDefinition teamColor = null;
                    //TeamColorDefinition centerColor = null;
                    TeamColorDefinition teamColor = p_type.DefaultCenterColor;
                    //var tempTrimColor = fac._factionTrimColor;
                    if (teamColor == null)
                    {
                        teamColor = TeamColorDefinitionTable.Instance.GetRowByName("cE1EBEE");
                        //teamColor = p_specialFacData.DefaultFactionCenterColor;
                    }
                    //else
                        //fac._factionCenterColor = p_specialFacData.DefaultFactionCenterColor;
                    
                    //if (p_type.DefaultTrimColor != null)
                    //    fac._factionTrimColor = p_type.DefaultTrimColor;
                    //else
                    //    fac._factionTrimColor = p_specialFacData.DefaultFactionTrimColor;
                    
                    if (!playerTypeBuffer.GetIsEmpty())
                        playerTypeBuffer.Add(", ", TextStyle.Color_Gray);
                    //bi_root.WriteFactionIcon(playerTypeBuffer);
                    //playerTypeBuffer.Builder.Remove(0, 7);
                    playerTypeBuffer.AddColor(p_type.ShortName, teamColor.ColorHexBrighter);
                    
                    //fac._factionCenterColor = tempCenterColor;
                    //fac._factionTrimColor = tempTrimColor;
                    
                    continue;
                }
                
                if (fac.SpecialFactionData.InternalName == "FallenSpire")
                {
                    isfallenspire = true;
                    playerTypeBuffer.Clear();
                    playerTypeBuffer.AddColor("Fallen Spire", fac.SpecialFactionData.DefaultFactionCenterColor.ColorHexBrighter);
                }
                
                if (fac.GetIsHostileToAnyPlayerFaction() == false)
                    continue;
                
                if (fac.Type == FactionType.SpecialFaction)
                {
                    if (fac.SpecialFactionData.Impact < TypeDifficulty.Hard)
                        continue;
                }
                
                int intensity = fac.BaseInfo?.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()??-1;
                bool isAiOrAlly = fac.GetIsAiOrAlly();
                
                if (fac.Type == FactionType.AI && 
                    intensity > maxAiIntensity)
                {
                    mostIntenseAi = fac;
                    maxAiIntensity = intensity;
                }
                
                if (isAiOrAlly && 
                    intensity > maxAiOrAllyIntensity)
                {
                    maxAiOrAllyIntensity = intensity;
                }
                    
                if (isAiOrAlly)
                {
                    numAiOrAlly++;
                }
                else
                {
                    numExtraHighImpactHostiles++;
                }
            }
            
            this.playerType = playerTypeBuffer.ToStringAndReturnToPool();
            if (SaveLoadMethods.Debug) LOG.Msg("setting SaveGameData.playerType to '{0}'.", this.playerType.OrNull());
            
            string masterAi = "";
            if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ))
                masterAi = "Secret";
            else if ( mostIntenseAi != null )
            {
                var aidata = mostIntenseAi.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( aidata != null )
                {
                    if ( aidata.WasRandomAIType )
                        masterAi = "Random";
                    else if ( aidata.AdaptiveAIDifficulty != TypeDifficulty.Unset )
                        masterAi = "Adaptive";
                    else
                        masterAi = aidata.AIType.GetDisplayName();
                }
            }
            
            this.masterAIType = masterAi;
            if (SaveLoadMethods.Debug) LOG.Msg("setting SaveGameData.masterAIType to '{0}'.", this.masterAIType.OrNull());
            
            this.difficulty = "";
            if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ))
                this.difficulty = "Secret";
            else 
            if ( maxAiOrAllyIntensity > 0 )
            {
                var dtype = TypeDifficulty.Unset;
                
                if (maxAiOrAllyIntensity <= 6)
                    dtype = TypeDifficulty.Easier;
                else if (maxAiOrAllyIntensity == 7)
                    dtype = TypeDifficulty.Moderate;
                else if (maxAiOrAllyIntensity == 8)
                    dtype = TypeDifficulty.Hard;
                else if (maxAiOrAllyIntensity == 9)
                    dtype = TypeDifficulty.Brutal;
                else if (maxAiOrAllyIntensity == 10)
                    dtype = TypeDifficulty.SuperCat;
                else
                    dtype = TypeDifficulty.Unset;
                
                if (dtype != TypeDifficulty.Unset)
                {
                    var buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("SaveGameData.setDifficulty.buffer");
                    
                    //buffer
                    //    .StartColor(dtype.GetHexColor())
                    //    .Add(maxAiIntensity).Add("-").Add(Extensions.ToString(dtype));
                    
                    buffer.StartColor(dtype.GetHexColor());
                        
                    buffer.Add(Extensions.ToString(dtype));

                    // the count of pluses based on factions
                    int numPlus = numExtraHighImpactHostiles;
                    if (numAiOrAlly > 2)
                        numPlus += numAiOrAlly-2;

                    // lots of pluses, show (+X) so the text fits
                    if (numPlus > 4)
                    {
                        buffer.Add("+").Add(numPlus).Add("");
                    }
                    // show individual +'s
                    else
                    {
                        for (int i = 0; i < numPlus; i++)
                            buffer.Add("+");
                    }
                    
                    buffer.EndColor();
                    
                    this.difficulty = buffer.ToStringAndReturnToPool();
                }
            }
            
            if (SaveLoadMethods.Debug) LOG.Msg("setting SaveGameData.difficulty to '{0}'.", this.difficulty.OrNull());
        }
        #endregion
        #region setEverything
        public void setEverything(World world)
        {
            setMapType();
            setPlayerNames();
            setDlcModInUse(world);
            setAuthor();
            setIronman(world.GameSpecificSubObject as World_AIW2);
            setStuff(world);
        }
        #endregion
        
        public override String ToString()
        {
            return string.Format("{0}/{1}.save", campaignName, saveName);
        }
        
#if POOLED
        private static ReferenceTracker RefTracker;
        private SaveGameData() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "SaveGameDatas" );
            RefTracker.IncrementObjectCount();
            this.SetToDefaults();
        }

        private static ConcurrentPool<SaveGameData> Pool = new ConcurrentPool<SaveGameData>( "SaveGameDatas", 30000,
            KeepTrackOfPooledItems.No, PoolBehaviorDuringShutdown.AllowAllThreads, delegate { return new SaveGameData(); } );

        public void DoAnyBelatedCleanupWhenComingOutOfPool()
        {

        }

        public void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.SetToDefaults();
        }

        public bool GetInPoolStatus()
        {
            return this.isInPool;
        }

        private bool isInPool = false;
        public void SetInPoolStatus( bool IsInPool )
        {
            this.isInPool = IsInPool;
        }
#endif
        public void ReturnToPool()
        {
#if POOLED
            Pool.ReturnToPool( this );
#endif
        }
        
        public void DoBeforeRemoveOrClear()
        {
            this.ReturnToPool();
        }
        
        public void SetToDefaults()
        {
            this.seed = 0;
            this.secondsSinceGameStart = 0;
            this.mapType = string.Empty;
            this.mapTypeShort = string.Empty;
            this.campaignName = string.Empty;
            this.saveName = string.Empty;
            this.saveFullFilename = string.Empty;
            this.masterAIType = string.Empty;
            this.difficulty = string.Empty;
            this.numTimesLoaded = 0;
            this.filesizeInBytes = 0;
            this.PlayersString = string.Empty;
            this.lastModified = SaveLoadMethods.NullDate;
            //this.debug = false;
            this.ironman = false;
            this.playerType = string.Empty;
            this.TooltipData = string.Empty;

            this.author = string.Empty;
            DlcInUse.Clear();
            ModInUse.Clear();
            _sortByStr = null;
            
            this.metaExists = false;
            this.tooltipExists = false;
            this.sortOrder = 0;
        }

        string IOption.GetInternalName()
        {
            return this.saveName;
        }

        string IOption.GetDisplayName()
        {
            return this.saveName;
        }

        string IOption.GetShortDisplayName()
        {
            return this.saveName;
        }

        void IOption.AddDescription( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add( this.TooltipData );
        }
        
        public void WriteTooltip( ArcenCharacterBufferBase buffer )
        {
            /*
            buffer.Open(TextStyle.Save_Tooltip);
            buffer.Add( this.saveName, TextStyle.Color_Quickstart_Selected );
            buffer.Open(TextStyle.Save_Tooltip_Meta_Lines);
            
            foreach (var field in EnumerateTooltipFields())
            {
                var label = SaveGameData.MetaField_Label(field);
                var val = this.MetaField_ToString(field);
                
                if (!string.IsNullOrEmpty(val))
                {
                    buffer.Open(TextStyle.Save_Tooltip_Meta_Lines);
                    
                    if (!string.IsNullOrEmpty(label))
                        buffer.Add(label+": ", TextStyle.Save_Tooltip_Meta_Label);
                    
                    buffer.Add(val);
                    
                    buffer.Close(TextStyle.Save_Tooltip_Meta_Lines);
                }
            }
            
            buffer.Close(TextStyle.Save_Tooltip_Meta_Lines);
            buffer.Close(TextStyle.Save_Tooltip);
            */

            string Tabs = "\t";
            if (!this.isquickstart)
            {
                //if (this.isquickstart)
                    //buffer.Add( "Quickstart: ").Add(Tabs).Add(this.saveName, ColorMath.LightYellow ).NewLine(2);
                //else
                    buffer.Add(this.saveName, ColorMath.LightYellow ).NewLine(2);
                
                //buffer.Add( this.campaignName )
                //const string Tabs = "  ";
                
                buffer.Add( "Last Modified: "+Tabs );
                buffer.Add( this.lastModified.ToShortDateString() );
                buffer.Add( "  " );
                buffer.Add( this.lastModified.ToShortTimeString() );
                
                buffer.Add( "\nGame Time: "+Tabs );
                buffer.AddHoursAndMinutes( this.secondsSinceGameStart );
            }
            
            if (!string.IsNullOrEmpty(this.difficulty) && this.difficulty != "nullDiff")
            {
                buffer.Add( "\nDifficulty: "+Tabs+Tabs );
                buffer.Add( this.difficulty );
            }
            
            if (!string.IsNullOrEmpty(this.mapType))
            {
                buffer.Add( "\nMap Type: "+Tabs );
                buffer.Add( this.mapType );
            }
            
            if (!this.isquickstart)
            {
                buffer.Add( "\nMap Seed: "+Tabs );
                buffer.Add( this.seed );
            }
            
            if (!string.IsNullOrEmpty(this.masterAIType))
            {
                buffer.Add( "\nAI Type: "+Tabs+Tabs );
                buffer.Add( this.masterAIType );
            }
            
            if (!string.IsNullOrEmpty(this.playerType))
            {
                buffer.Add( "\nPlayer Type: "+Tabs );
                buffer.Add( this.playerType );
            }
            
            if (!this.isquickstart)
            {
                if (!string.IsNullOrEmpty(this.PlayersString))
                {
                    buffer.Add( "\nPlayers: "+Tabs+Tabs );
                    buffer.Add( this.PlayersString );
                }

                buffer.Add( "\nTimes Loaded: "+Tabs );
                buffer.Add( this.numTimesLoaded );
                //buffer.Add( "\nFile Size: " );
                //buffer.AddBytesWithFormat( this.filesizeInBytes );
            
                if (this.DlcInUse.Count > 0)
                {
                    buffer.Add( "\nDLC Used: "+Tabs );
                    int counter = 0;
                    foreach (var dlc in this.DlcInUse)
                    {
                        if (counter > 0)
                            buffer.Add(" ");
                        buffer.AddColor(dlc.Abbreviation, dlc.ColorForDisplay);
                        counter++;
                    }
                }
                
                if (this.ModInUse.Count > 0)
                {
                    buffer.Add( "\nMods Used: "+Tabs );
                    int counter = 0;
                    foreach (var mod in this.ModInUse)
                    {
                        if (counter > 0)
                            buffer.Add(" ");
                        buffer.AddColor(mod.Abbreviation, mod.ColorForDisplay);
                        counter++;
                    }
                }
            }
            
            if (this.ironman)
                buffer.Add( "\nIronman");
            //if (this.isquickstart)
                //buffer.Add( "\nThis is a Quickstart");
        }
    }
    
    public static class ArcenCharacterBuffer_Ext
    {
        public static ArcenCharacterBufferBase WriteTooltip( this ArcenCharacterBufferBase buffer, SaveGameData savedata )
        {
            savedata.WriteTooltip(buffer);
            return buffer;
        }
    }
}
