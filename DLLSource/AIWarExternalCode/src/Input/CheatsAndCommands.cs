using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    
    public static class CheatsAndCommands
    {
        public class CmdReturnValue
        {
            public bool Handled;
        }

        /// <summary>
        /// Do NOT call this directly!  This should be coming from a GameCommand, and thus happening for everyone at once.
        /// </summary>
        /// <param name="CameFrom"></param>
        /// <param name="RawText"></param>
        public static void ProcessCheatOrCommandText( string CameFrom, string RawText, Planet OnPlanet, Faction PlayerFactionIssuingCommand, 
            PlayerAccount PlayerAccountIssuingCommand, ArcenClientOrHostSimContextCore Context )
        {
            int debugStage = 0;
            bool isCheat = true;
            bool isIronman = World_AIW2.Instance.CalculateIsIronmanMode();
            var hostCtx = Context.GetHostOnlyContext();
            try
            {
                debugStage = 100;
                string textToUse = RawText.Replace( "cmd:", string.Empty ).Replace( "\t", string.Empty ).Replace( "\n", string.Empty ).Replace( "\r", string.Empty );
                debugStage = 200;
                string[] commandParts = textToUse.Split( ',' );

                debugStage = 1000;
                string command = commandParts[0].ToLower().Replace( " ", string.Empty );
                debugStage = 1100;
                for ( int i = 1; i < commandParts.Length; i++ )
                    commandParts[i] = commandParts[i].Trim(); //get rid of starting and ending whitespace, but not inner whitespace
                debugStage = 1200;
                switch ( command )
                {
                    case "stacking":
                    case "restack":
                        {
                            var data = Arcen.AIW2.Core.Stacking.Data.Get( OnPlanet );
                            data.Log();
                            if (command == "restack")
                                data.Restack();
                            Arcen.AIW2.Core.Stacking.Data.Return( data );
                            break;
                        }
                    case "split":
                        {
                            if (hostCtx == null)
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "无法在客户端使用此命令。", false, false, null);
                            else
                            {
                                foreach (var e in OnPlanet.Squads())
                                {
                                    while (e.ShipCount > 1)
                                    {
                                        if (e.SplitStack_ReturnNewSquadOrNullIfNoneSplit(hostCtx, 1, "Cmd:Split") == null)
                                            break;
                                    }
                                }
                            }
                            break;
                        }
                    case "killwarden":
                        foreach (var ai in World_AIW2.Instance.AIFactions)
                        {
                            var warden = ai.GetAISentinelsCoreData().SubFac_Warden;
                            foreach ( GameEntity_Squad e in warden.Squads() )
                            {
                                if (!e.IsMobileOrCountsAsMobileDueToOrbitingSomethingMobile())
                                    continue;
                                e.Despawn(Context, false, InstancedRendererDeactivationReason.PlayerIsScrappingMe);
                                continue; // was RemoveAndContinue (no-op)
                            }
                        }
                        break;
                    case "spawn":
                    case "spawnh":
                    case "spawnhostile":
                    {
                        isCheat = true;
                        string message = null;
                        bool success = false;
                        Exception ex = null;
                        
                        void doSpawnCmd(bool hostile)
                        {
                            string entityName = null;
                            string factionName = null;
                            int entityCount = 1;
                            if ( commandParts.Length > 1  )
                            {
                                entityName = commandParts[1];
                            }
                            if ( commandParts.Length > 2  )
                            {
                                entityCount = Convert.ToInt32( commandParts[2] );
                            }
                            if ( commandParts.Length > 3 )
                                factionName = commandParts[3];

                            var row = GameEntityTypeDataTable.Instance.GetRowByName(entityName);
                            if (row == null)
                            {
                                message = string.Format("Found no entity type '{0}'", entityName);
                                return;
                            }

                            Faction forFaction = null;
                            if (!string.IsNullOrEmpty(factionName))
                            {
                                forFaction = World_AIW2.Instance.GetFirstFactionWithName(factionName);
                                if (forFaction == null)
                                {
                                    message = string.Format("Expected the internal name of a faction for argument 3, if given. Found no faction of type '{0}'.", factionName);
                                    return;
                                }
                            }
                            else if (hostile)
                            {
                                forFaction = World_AIW2.Instance.GetFirstFactionWithType(FactionType.AI);
                                if (forFaction == null)
                                {
                                    message = "Found no ai faction to spawn for.";
                                    return;
                                }
                            }
                            else
                            {
                                forFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                                if (forFaction == null)
                                {
                                    message = "Found no player faction to spawn for.";
                                    return;
                                }
                            }

                            var planetFaction = OnPlanet.GetPlanetFactionForFaction(forFaction);
                            if (planetFaction == null)
                            {
                                message = string.Format("Found no planet faction on '{0}' for '{1}' to spawn for.", OnPlanet.OrNull(), forFaction.OrNull());
                                return;
                            }
                            
                            var placementLocation = OnPlanet.GetSafePlacementPoint_AroundZone(Context.GetHostOnlyContext(), row, PlanetSeedingZone.InnerSystem);
                            var e = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(planetFaction, row, 1, planetFaction.FleetUsedAtPlanet, 0, placementLocation, Context.GetHostOnlyContext(), "cmd:spawn");
                            if (e == null)
                            {
                                message = "Create entity call returned null.";
                                return;
                            }
                            
                            e.SetShipCount(entityCount);
                            success = true;
                        }
                        
                        try
                        {
                            doSpawnCmd(command == "spawnhostile" || command == "spawnh");
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                        
                        WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, message, isCheat, success, ex );
                        break;
                    }
                    case "alljournals":
                        #region alljournals
                        {
                        foreach (var row in JournalEntryTable.Instance.Rows)
                            World.Instance.GameSpecificSubObject.QueueLogJournalEntryToSidebar(row.InternalName, row.OptionalGroupID, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        }
                        #endregion
                        break;
                    case "noplans":
                        #region noplans
                        {
                            debugStage = 1500;
                            int val = 0;
                            try
                            {
                                val = Convert.ToInt32( commandParts[1] );
                            }
                            catch
                            {
                                val = 1;
                            }
                            isCheat = val > 0;
                            debugStage = 1600;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.AI )
                                    continue;

                                var data = fac.TryGetAISentinelsCoreData();
                                if (data == null)
                                    continue;

                                var v = val > 0 ? true : false;
                                data.HunterInfo.DisableLongRangePlanning = v;
                                data.PraetorianInfo.DisableLongRangePlanning = v;
                                data.WardenInfo.DisableLongRangePlanning = v;

                            }
                            debugStage = 1700;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, string.Format("AI LRP is {0}", val > 0 ? "DISABLE" : "ENABLED"), isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "forscience":
                    case "science":
                        #region forscience
                        {
                            debugStage = 2000;
                            int amount = 0;
                            try
                            {
                                amount = Convert.ToInt32( commandParts[1] );
                            }
                            catch
                            {
                                amount = 10000;
                            }
                            if ( isIronman && amount > 0 )
                                amount = 0;
                            isCheat = amount > 0;
                            debugStage = 3000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue; ;
                                fac.StoredScience += amount;
                                if ( fac.StoredScience < 0 )
                                    fac.StoredScience = FInt.Zero;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                (amount > 0 ? "All Players Granted " + amount.ToString( "#,##0" ) + " Science" :
                                "All Players Lost " + (-amount).ToString( "#,##0" ) + " Science"),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "essence":
                    case "resourceone":
                        #region Essence
                        {
                            debugStage = 2000;
                            int amount = 1000;
                            try
                            {
                                amount = Convert.ToInt32( commandParts[1] );
                            }
                            catch { amount = 10000000; }
                            if ( isIronman && amount > 0 )
                                amount = 0;
                            isCheat = amount > 0;
                            debugStage = 3000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue; ;
                                fac.StoredFactionResourceOne += amount;
                                if ( fac.StoredFactionResourceOne < 0 )
                                    fac.StoredFactionResourceOne = FInt.Zero;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                (amount > 0 ? "All Players Granted " + amount.ToString( "#,##0" ) + " Essence" :
                                "All Players Lost " + (-amount).ToString( "#,##0" ) + " Essence"),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "resourcetwo":
                        #region resourceTwo
                        {
                            debugStage = 2000;
                            int amount = 1000;
                            try
                            {
                                amount = Convert.ToInt32( commandParts[1] );
                            }
                            catch { amount = 10000000; }
                            if ( isIronman && amount > 0 )
                                amount = 0;
                            isCheat = amount > 0;
                            debugStage = 3000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue; ;
                                fac.StoredFactionResourceTwo += amount;
                                if ( fac.StoredFactionResourceTwo < 0 )
                                    fac.StoredFactionResourceTwo = FInt.Zero;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                (amount > 0 ? "All Players Granted " + amount.ToString( "#,##0" ) + " resourceTwo" :
                                "All Players Lost " + (-amount).ToString( "#,##0" ) + " resourceTwo"),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "resourcethree":
                        #region resourceThree
                        {
                            debugStage = 2000;
                            int amount = 1000;
                            try
                            {
                                amount = Convert.ToInt32( commandParts[1] );
                            }
                            catch { amount = 10000000; }
                            if ( isIronman && amount > 0 )
                                amount = 0;
                            isCheat = amount > 0;
                            debugStage = 3000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue; ;
                                fac.StoredFactionResourceThree += amount;
                                if ( fac.StoredFactionResourceThree < 0 )
                                    fac.StoredFactionResourceThree = FInt.Zero;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                (amount > 0 ? "All Players Granted " + amount.ToString( "#,##0" ) + " resourceThree" :
                                "All Players Lost " + (-amount).ToString( "#,##0" ) + " resourceThree"),
                                isCheat, true, null );
                        }
                        #endregion
                        break;

                    case "riches":
                    case "metal":
                        #region riches
                        {
                            debugStage = 2000;
                            int amount = 10000000;
                            try
                            {
                                amount = Convert.ToInt32( commandParts[1] );
                            }
                            catch { amount = 10000000; }
                            if ( isIronman && amount > 0 )
                                amount = 0;
                            isCheat = amount > 0;
                            debugStage = 3000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue; ;
                                fac.StoredMetal += amount;
                                if ( fac.StoredMetal < 0 )
                                    fac.StoredMetal = FInt.Zero;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                (amount > 0 ? "All Players Granted " + amount.ToString( "#,##0" ) + " Metal" :
                                "All Players Lost " + (-amount).ToString( "#,##0" ) + " Metal"),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "iheartenergon":
                    case "energy":
                        #region iheartenergon
                        {
                            debugStage = 2000;
                            if ( isIronman )
                                break;
                            isCheat = true;
                            debugStage = 3000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue;
                                fac.Debug_SpawnZenithPowerGenerator = true;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "All Players Granted A Free Zenith Power Generator",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "idon'tevenseethecode":
                    case "idontevenseethecode":
                    case "hacking":
                        #region idon'tevenseethecode
                        {
                            debugStage = 2000;
                            int amount = 400;
                            if ( commandParts.Length > 1  )
                                amount = Convert.ToInt32( commandParts[1] );

                            if ( isIronman && amount > 0 )
                                amount = 0;
                            isCheat = amount > 0;
                            debugStage = 3000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue; ;
                                fac.StoredHacking += amount;
                                if ( fac.StoredHacking < 0 )
                                    fac.StoredHacking = FInt.Zero;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                (amount > 0 ? "All Players Granted " + amount.ToString( "#,##0" ) + " Hacking Points" :
                                "All Players Lost " + (-amount).ToString( "#,##0" ) + " Hacking Points"),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "angermanagement":
                    case "aip":
                        #region angermanagement
                        {
                            debugStage = 2000;
                            int amount = Convert.ToInt32( commandParts[1] );
                            if ( isIronman && amount < 0 )
                                amount = 0;

                            isCheat = amount < 0;
                            debugStage = 3000;
                            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                            if ( localFaction != null )
                                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)amount, AIPChangeReason.Debug, null, localFaction.FactionIndex, -1, -1 );
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                (amount > 0 ? "AI Progress Increased By " + amount.ToString( "#,##0" ) :
                                "AI Progress Reduced By " + (-amount).ToString( "#,##0" )),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "icanseemyhouse":
                        #region icanseemyhouse
                        {
                            isCheat = false; //lifestyle choice!
                            debugStage = 2000;
                            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                            {
                                planet.IntelLevel = PlanetIntelLevel.ExploredByNaturalMeans;
                                planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Entire Galaxy Explored",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "i'mwatchingyourhouse":
                        #region i'mwatchingyourhouse
                        {
                            isCheat = false; //lifestyle choice!
                            debugStage = 2000;
                            World_AIW2.Instance.Debug_JustShowEverything = true;

                            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                            {
                                planet.IntelLevel = PlanetIntelLevel.ExploredByNaturalMeans;
                                planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Entire Galaxy Now Watched",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "concede":
                        #region concede
                        {
                            isCheat = false; //lifestyle choice!
                            debugStage = 2000;
                            //if ( World_AIW2.Instance.GetHasAnythingPreventingGameFromConcluding() )
                            //    break; //go ahead and allow this one...
                            World_AIW2.Instance.DoConclusionOfGame( CampaignConclusionType.Lost );
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Conceded The Game",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "tableflip":
                    case "wingame":
                        #region tableflip
                        {
                            isCheat = true;
                            debugStage = 2000;
                            //if ( World_AIW2.Instance.GetHasAnythingPreventingGameFromConcluding() )
                            //    break; //go ahead and allow this one...
                            World.Instance.HaveDoneAnyCheatingThatBlocksAchievemets = true;
                            World_AIW2.Instance.DoConclusionOfGame( CampaignConclusionType.Won );
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Flipped The Table.  Here's Your Fake Victory",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "braindeath":
                    case "braindead":
                        #region braindeath
                        {
                            if ( World_AIW2.Instance.IsBrainDeathCurrentlyEnabled )
                            {
                                isCheat = false;
                                debugStage = 2000;
                                World_AIW2.Instance.IsBrainDeathCurrentlyEnabled = false;
                                debugStage = 4000;
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    "Brain Death Halted (factions can think again)",
                                    isCheat, true, null );
                            }
                            else
                            {
                                isCheat = true;
                                debugStage = 6000;
                                World_AIW2.Instance.IsBrainDeathCurrentlyEnabled = true;
                                debugStage = 7000;
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    "Brain Death Started (no factions can think anymore)",
                                    isCheat, true, null );
                            }
                        }
                        #endregion
                        break;
                    case "logfastblast":
                        #region logfastblast
                        {
                            isCheat = false;
                            if ( World_AIW2.Instance.FastBlastLogger.IsActive )
                            {
                                debugStage = 2000;
                                World_AIW2.Instance.FastBlastLogger.EndLog();
                                debugStage = 4000;
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    "Fast Blast Logging Disabled",
                                    isCheat, true, null );
                            }
                            else
                            {
                                debugStage = 6000;
                                World_AIW2.Instance.FastBlastLogger.StartNewLog();
                                debugStage = 7000;
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    "Fast Blast Logging Enabled to PlayerData/FastBlastLog/[datetime].txt",
                                    isCheat, true, null );
                            }
                        }
                        #endregion
                        break;
                    case "dumpallshipinfo":
                    case "dumpships":
                        #region dumpallshipinfo
                        {
                            isCheat = false;
                            debugStage = 2000;
                            DumpAllShipInfo();
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Dumping All Ship Info to PlayerData/FullShipStatsLog/[datetime].txt",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "multiplystrikecraft":
                    case "multiplyfrigates":
                    case "multiplydrones":
                    case "multiplyturrets":
                        #region multiplystrikecraft
                        {
                            debugStage = 2000;
                            FInt amount = FInt.CreateFromDoubleNonSim( float.Parse( commandParts[1] ) );
                            if ( isIronman && amount > FInt.Zero )
                                amount = FInt.Zero;

                            isCheat = amount > 0;
                            debugStage = 3000;
                            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                            {
                                switch ( fleet.Category )
                                {
                                    case FleetCategory.NPC:
                                    case FleetCategory.PlayerLoose:
                                        continue;
                                }
                                if ( fleet.Faction == null || fleet.Faction.Type == FactionType.NaturalObject || fleet.Faction.Type == FactionType.Player )
                                {
                                    //only if these types of factions should we do the stuff
                                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        switch ( command )
                                        {
                                            case "multiplystrikecraft":
                                                if ( mem.TypeData.IsDrone )
                                                    continue;
                                                if ( !mem.TypeData.IsStrikecraft )
                                                    continue;
                                                break;
                                            case "multiplyfrigates":
                                                if ( mem.TypeData.IsDrone )
                                                    continue;
                                                if ( mem.TypeData.SpecialType != SpecialEntityType.Frigate )
                                                    continue;
                                                break;
                                            case "multiplydrones":
                                                if ( !mem.TypeData.IsDrone )
                                                    continue;
                                                break;
                                            case "multiplyturrets":
                                                if ( mem.TypeData.IsDrone )
                                                    continue;
                                                if ( !mem.TypeData.IsTurret )
                                                    continue;
                                                break;
                                        }

                                        mem.ExplicitBaseSquadCap = (mem.ExplicitBaseSquadCap * amount).GetNearestIntPreferringHigher();

                                    }
                                }
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "All Player And Capturable Fleets Now Have " + amount.ToFloatNonSim().ToString( "#,##0.###" ) + "x As Many Strikecraft",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "alterai":
                        #region alterai
                        {
                            debugStage = 2000;
                            int aiIndex = Convert.ToInt32( commandParts[1] ) - 1; //make it 0-indexed while players enter 1-indexed values
                            debugStage = 2100;
                            byte difficultyChangeAmount = Convert.ToByte(  commandParts[2] );
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat, because achievements
                            debugStage = 3000;
                            if ( aiIndex < 0 || aiIndex >= World_AIW2.Instance.AIFactions.Count )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    "Alter AI failed because there is no AI with index " + (aiIndex + 1),
                                    isCheat, false, null );
                                break;
                            }
                            debugStage = 3100;
                            Faction aiFaction = World_AIW2.Instance.AIFactions[aiIndex];
                            debugStage = 3200;
                            AISentinelsCoreData factionExternal = aiFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                            debugStage = 3200;
                            byte oldAIDifficulty = factionExternal.AIDifficulty.Difficulty;
                            byte newFinalAIDifficulty = (byte)(oldAIDifficulty + difficultyChangeAmount);
                            if ( newFinalAIDifficulty > 10 )
                                newFinalAIDifficulty = 10;
                            if ( newFinalAIDifficulty < 1 )
                                newFinalAIDifficulty = 1;

                            factionExternal.AIDifficulty = AIDifficultyTable.Instance.GetRowByOrdinal( newFinalAIDifficulty );

                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "AI #" + (aiIndex + 1) + " Difficulty Changed From " + oldAIDifficulty + " to " + newFinalAIDifficulty,
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "stopsim":
                        #region stopsim
                        {
                            isCheat = false; //lifestyle choice!
                            debugStage = 2000;
                            if ( PlayerFactionIssuingCommand != null && PlayerFactionIssuingCommand == World_AIW2.Instance.GetLocalPlayerFactionOrNull() )
                            {
                                World_AIW2.Instance.IsSimulationArtificiallyStopped = !World_AIW2.Instance.IsSimulationArtificiallyStopped;
                                debugStage = 4000;
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    World_AIW2.Instance.IsSimulationArtificiallyStopped ? "Halted The Sim" : "Resumed The Sim",
                                    isCheat, true, null );
                            }
                        }
                        #endregion
                        break;
                    case "resync":
                        #region resync
                        {
                            isCheat = false; //lifestyle choice!
                            debugStage = 2000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Forced full MP Resync",
                                isCheat, true, null );

                            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Host )
                            {
                                //only actually do this on the host!
                                foreach ( ArcenNetworkClientConnection client in ArcenNetworkAuthority.ClientConnections )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Send client index " + client.ConnectionIndex + " the world data.", Verbosity.DoNotShow );
                                    //now send the world
                                    string errorText;
                                    if ( !ArcenNetworkAuthority.SendTheWorldFromTheHostToClients( (int)client.ConnectionIndex, out errorText ) )
                                        ArcenDebugging.ArcenDebugLog( "MP Error: " + errorText, Verbosity.ShowAsError );
                                }
                            }
                        }
                        #endregion
                        break;
                    case "aibudget":
                        #region aibudget
                        {
                            debugStage = 2000;
                            int aiIndex = Convert.ToInt32( commandParts[1] ) - 1; //make it 0-indexed while players enter 1-indexed values
                            debugStage = 2050;
                            AIBudgetType budgetType = (AIBudgetType)Enum.Parse( typeof( AIBudgetType ), commandParts[2] );
                            debugStage = 2100;
                            int budgetChangeAmount = Convert.ToInt32( commandParts[3] );
                            debugStage = 2200;
                            bool spendNow = commandParts.Length > 4 && commandParts[4].ToLower() == "now";
                            if ( isIronman && (budgetChangeAmount < 0 || spendNow) )
                                budgetChangeAmount = 0;

                            isCheat = budgetChangeAmount < 0; //only a cheat if you are reducing the budget
                            debugStage = 3000;
                            if ( aiIndex < 0 || aiIndex >= World_AIW2.Instance.AIFactions.Count )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    "AI Budget Adjustment failed because there is no AI with index " + (aiIndex + 1),
                                    isCheat, false, null );
                                break;
                            }
                            if ( budgetType <= AIBudgetType.None || budgetType >= AIBudgetType.Length )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                    "AI Budget Adjustment failed because could not interpret the budget with name '" + commandParts[2] + "'",
                                    isCheat, false, null );
                                break;
                            }
                            debugStage = 3100;
                            Faction aiFaction = World_AIW2.Instance.AIFactions[aiIndex];
                            debugStage = 3200;
                            AISentinelsCoreData factionExternal = aiFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                            debugStage = 3200;
                            if ( factionExternal == null || factionExternal.AIType == null || factionExternal.AIType.BudgetItems == null )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                       "AI Budget Adjustment failed because budget items or AI type was null",
                                       isCheat, false, null );
                                break;
                            }
                            AIBudgetItem item = factionExternal.AIType.BudgetItems[budgetType];
                            if ( item == null )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                          "AI Budget Adjustment failed because AIBudgetItem is null",
                                          isCheat, false, null );
                                break;
                            }
                            FInt oldBudget = factionExternal.StoredAIPurchaseCostByBudget[budgetType];
                            FInt newBudget = oldBudget + budgetChangeAmount;
                            if ( newBudget < FInt.Zero )
                                newBudget = FInt.Zero;
                            factionExternal.StoredAIPurchaseCostByBudget[budgetType] = newBudget;
                            if (spendNow) {
                                int nextEventTime = World_AIW2.Instance.GameSecond;
                                // This is treated as uninitialized, if it is less than 10.
                                if (nextEventTime < 10) {
                                    nextEventTime = 10;
                                }
                                factionExternal.NextEventTime[budgetType] = nextEventTime;
                            }

                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "AI #" + (aiIndex + 1) + " " + budgetType + " budget changed From " + oldBudget.IntValue + " to " + newBudget.IntValue +
                                (spendNow ? " (spending now)" : ""),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "scrubfoes":
                        #region scrubfoes
                        {
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat!
                            debugStage = 2000;
                            int countDone = 0;
                            int countSkippedByTarget = 0;
                            int countSkippedByAllegiance = 0;
                            foreach ( GameEntity_Squad entity in OnPlanet.Squads() )
                            {
                                if ( entity.GameSecondCreated > World_AIW2.Instance.GameSecond - 1 ) //don't scrub anything from the last second, or we get an infinite loop if things spawn on death
                                    continue;

                                if ( entity.GetIsHostileTowards_Safe( PlayerFactionIssuingCommand ) )
                                {
                                    if ( entity.TypeData.TargetTypeForPlayer != PlayerTargetType.NeverTarget )
                                    {
                                        if ( entity.ExtraStackedSquadsInThis > 0 )
                                        {
                                            countDone += entity.ExtraStackedSquadsInThis;
                                            entity.AddOrSetExtraStackedSquadsInThis( 0, true );
                                        }
                                        entity.Die( Context, true );
                                        countDone++;
                                    }
                                    else
                                        countSkippedByTarget++;
                                }
                                else
                                    countSkippedByTarget++;
                            }
                            debugStage = 4000;
                            string endMessage = "All enemy units of faction " + PlayerFactionIssuingCommand.GetDisplayName() + " killed on planet " + OnPlanet.Name + " (there were " + countDone + ").  ";
                            if ( countSkippedByTarget > 0 )
                                endMessage += (countSkippedByTarget + " units were skipped because they were of 'never target' status.  ");
                            if ( countSkippedByAllegiance > 0 )
                                endMessage += (countSkippedByAllegiance + " were skipped because they are not hostile to the invoking faction.  ");

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, endMessage,
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "scanforlife":
                        #region scanforlife
                        {
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat!
                            debugStage = 2000;
                            FactionUtilityMethods.Instance.TachyonBlastPlanet( OnPlanet, PlayerFactionIssuingCommand, Context, false );
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "All enemy units of faction " + PlayerFactionIssuingCommand.GetDisplayName() + " decloaked on planet " + OnPlanet.Name,
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "hurryup":
                        #region hurryup
                        {
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat!
                            debugStage = 2000;
                            int countDone = 0;
                            foreach ( GameEntity_Squad entity in OnPlanet.Squads() )
                            {
                                if ( entity.SelfBuildingMetalRemaining > 0 )
                                {
                                    entity.SelfBuildingMetalRemaining = FInt.One;
                                    countDone++;
                                }
                            }
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "All construction projects on planet " + OnPlanet.Name + " (there were " + countDone + ") have been instantly completed.",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "forgetthepast":
                        #region forgetthepast
                        {
                            isCheat = false; //never a cheat!
                            debugStage = 2000;
                            if ( PlayerFactionIssuingCommand == World_AIW2.Instance.GetLocalPlayerFactionOrNull() )
                            {
                                //let's you get them again.  This is not a cheat at all
                                GameSettings.Current.CompletedAchievementsOffline.Clear();
                                GameSettings.Current.CompletedAchievementsGOG.Clear();
                                GameSettings.Current.CompletedAchievementsSteam.Clear();
                                World.Instance.AchievementsWonThisCampaign.Clear();
                            }
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, PlayerFactionIssuingCommand.GetDisplayName() + " has wiped out all their past achievements, locally at least, so that they can get them again.",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "dumpdatatables":
                        #region dumpdatatables
                        {
                            isCheat = false; //never a cheat!
                            debugStage = 2000;
                            try
                            {
                                Engine_AIW2.Instance.DumpAllDataTablesToDiskNoYield();

                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, PlayerFactionIssuingCommand.GetDisplayName() + " has requested that every computer in this game log details of their setup to the DataTableExports subfolder in the PlayerData folder on each machine.",
                                    isCheat, true, null );
                            }
                            catch ( Exception e )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, PlayerFactionIssuingCommand.GetDisplayName() + " has requested that every computer in this game log details of their setup to the DataTableExports subfolder in the PlayerData folder on each machine.",
                                    isCheat, false, e );

                            }
                            debugStage = 4000;
                        }
                        #endregion
                        break;
                    case "dumpexternaldata":
                        #region dumpexternaldata
                        {
                            isCheat = false; //never a cheat!
                            debugStage = 2000;
                            try
                            {
                                Engine_AIW2.Instance.DumpAllExternalDataToDisk();

                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, PlayerFactionIssuingCommand.GetDisplayName() + " has requested that every computer in this game log details of their 'external data' to the ExternalDataExports subfolder in the PlayerData folder on each machine.",
                                    isCheat, true, null );
                            }
                            catch ( Exception e )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, PlayerFactionIssuingCommand.GetDisplayName() + " has requested that every computer in this game log details of their 'external data' to the ExternalDataExports subfolder in the PlayerData folder on each machine.",
                                    isCheat, false, e );

                            }
                            debugStage = 4000;
                        }
                        #endregion
                        break;
                    case "resetstats":
                        ArcenSettingTable.Instance.ResetAccessStats();
                        AIWar2GalaxySettingTable.Instance.ResetAccessStats();
                        break;
                    case "dumpstats":
                        ArcenSettingTable.Instance.DumpAccessStatsToCSV();
                        AIWar2GalaxySettingTable.Instance.DumpAccessStatsToCSV();
                        break;
                    case "instainvasion":
                    case "wormholeinvasion":
                        #region instainvasion
                        isCheat = false; //not a cheat, makes the game harder
                        int strength = 20 * 1000;
                        if ( commandParts.Length >= 2 )
                            strength = Convert.ToInt32( commandParts[1] ) * 1000;
                        Faction wormholeInvasionFaction = FactionUtilityMethods.Instance.GetWormholeInvasionFaction();
                        WormholeInvasionFactionBaseInfo wormholeBase = wormholeInvasionFaction.GetExternalBaseInfoAs<WormholeInvasionFactionBaseInfo>();
                        wormholeBase.DebugSpawnInvasion = true;
                        wormholeBase.DebugInvasionStrength = strength;
                        WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Triggered wormhole invasion.", isCheat, true, null );
                        #endregion
                        break;
                    case "instawave":
                    case "instawaves":
                        #region instawaves
                        {
                            isCheat = false; //never a cheat, makes things harder!
                            debugStage = 2000;
                            int countWavesGenerated = 0;
                            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                            {
                                Faction faction = World_AIW2.Instance.AIFactions[i];
                                AISentinelsFactionBaseInfo factionExternal = faction.TryGetAISentinelsCoreData();
                                factionExternal.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] += factionExternal.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
                                faction.Debug_ImmediatelyLaunchAllWaves = true;
                                countWavesGenerated++;
                                if ( ArcenNetworkAuthority.GetIsHostMode() )
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#" + faction.FactionCenterColor.ColorHexBrighter + ">" + faction.GetDisplayName() + " #" + (i + 1) + "</color>: given " + factionExternal.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ).ToString( "#,##0" ) +
                                        " to their wave budget, for a current total of " + factionExternal.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave].ToFloatNonSim().ToString( "#,##0" ) + ".", ChatType.LogToCentralChat, string.Empty, null );
                            }
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, countWavesGenerated + " waves generated in total.",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "instacpa":
                        #region instacpa
                        {
                            isCheat = false; //never a cheat, makes things harder!
                            debugStage = 2000;
                            Faction faction = World_AIW2.GetRandomAIFaction( Context );

                            AISentinelsFactionBaseInfo factionExternal = faction.TryGetAISentinelsCoreData();
                            int amountAdded = factionExternal.GetSpecificBudgetThreshold( AIBudgetType.CPA, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ) / 2;
                            factionExternal.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.CPA] += amountAdded;
                            faction.Debug_ImmediatelyLaunchCPA = true;
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#" + faction.FactionCenterColor.ColorHexBrighter + ">" + faction.GetDisplayName() + "</color>: given " + amountAdded +
                                        " to their CPA budget, for a current total of " + factionExternal.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.CPA].ToFloatNonSim().ToString( "#,##0" ) + ". Then launching a CPA immediately.", ChatType.LogToCentralChat, string.Empty, null );
                        }
                        #endregion
                        break;
                    case "instaexo":
                        #region instaexo
                        {
                            isCheat = false; //never a cheat, makes things harder!
                            debugStage = 2010;
                            int exoStrength;
                            try {
                                exoStrength = Convert.ToInt32( commandParts[1] );
                            } catch {
                                exoStrength = 100;
                            }
                            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );

                            GameEntity_Squad king = PlayerFactionIssuingCommand.GetFactionKing();
                            if (king == null) {
                                break;
                            }
                            ExoOptions options = ExoOptions.CreateWithDefaults(king, exoStrength * 1000, aiFaction, aiFaction);
                            ExoGalacticDeepLinkRoot.Instance.SendExoGalacticAttack( options, Context.GetHostOnlyContext() );

                            if ( ArcenNetworkAuthority.GetIsHostMode() ) {
                                World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#" + aiFaction.FactionCenterColor.ColorHexBrighter + ">" + aiFaction.GetDisplayName() + "</color>: Triggered exo wave of strength " + exoStrength + "." ,
                                        ChatType.LogToCentralChat, string.Empty, null );
                            }
                        }
                        #endregion
                        break;
                    case "instaexow":
                        #region instaexow
                        {
                            isCheat = false; //never a cheat, makes things harder!
                            debugStage = 2010;
    
                            Faction faction = World_AIW2.GetRandomAIFaction( Context );

                            AISentinelsFactionBaseInfo factionExternal = faction.TryGetAISentinelsCoreData();
                            
                            var budget = factionExternal.SentinelInfo.ExtragalacticBudgets.FirstOrDefault(b => b.Target != null && string.Equals(b.Target.AgainstFactionAllegiance, "对玩家友好", StringComparison.InvariantCultureIgnoreCase));
                            if (budget == null)
                                return;
                            budget.Budget = FInt.FromParts(budget.NextExtragalacticUnitToBuy.CostForAIToPurchase, 0);

                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#" + faction.FactionCenterColor.ColorHexBrighter + ">" + faction.GetDisplayName() + "</color>: budget set to " + budget.Budget +
                                        " so they can buy a " + budget.NextExtragalacticUnitToBuy.DisplayName, ChatType.LogToCentralChat, string.Empty, null );
                        }
                        #endregion
                        break;
                    case "instausurp":
                    case "instaconquest":
                        #region instausurp
                        {
                            isCheat = false; //never a cheat, makes things harder!
                            debugStage = 2000;
                            int countWavesGenerated = 0;
                            int amt;
                            try {
                                amt = Convert.ToInt32( commandParts[1] );
                            } catch {
                                amt = 100;
                            }
                            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                            {
                                Faction faction = World_AIW2.Instance.AIFactions[i];
                                AISentinelsFactionBaseInfo factionExternal = faction.TryGetAISentinelsCoreData();
                                factionExternal.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Reconquest] += amt;
                                faction.Debug_ImmediatelyLaunchAllWaves = true;
                                countWavesGenerated++;
                                if ( ArcenNetworkAuthority.GetIsHostMode() )
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#" + faction.FactionCenterColor.ColorHexBrighter + ">" + faction.GetDisplayName() + " #" + (i + 1) + "</color>: given " + amt.ToString( "#,##0" ) +
                                        " to their Reconquest budget, for a current total of " + factionExternal.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Reconquest].ToFloatNonSim().ToString( "#,##0" ) + ".", ChatType.LogToCentralChat, string.Empty, null );
                            }
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, countWavesGenerated + " waves generated in total.",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                     case "templarwave":
                        #region templarwave
                        {
                            isCheat = false; //never a cheat, makes things harder!
                            debugStage = 2000;
                            Faction templarFaction = FactionUtilityMethods.Instance.GetTemplarFaction();
                            if ( templarFaction != null )
                            {
                                TemplarFactionBaseInfo baseInfo = templarFaction.GetExternalBaseInfoAs<TemplarFactionBaseInfo>();
                                baseInfo.SpawnTemplarWaveNextSecond = true;
                            }
                        }
                        #endregion
                        break;

                    case "playerpotluck":
                    case "pp":
                        #region playerpotluck
                        {
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            foreach ( Faction fac in World_AIW2.Instance.Factions )
                            {
                                if ( fac.Type != FactionType.Player )
                                    continue;
                                fac.Debug_SpawnShips = true;
                            }
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Grab bag of new ships spawning for every human player.",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "mypotluck":
                    case "mp":
                        #region mypotluck
                        {
                            if ( isIronman )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            PlayerPotluck( PlayerFactionIssuingCommand, OnPlanet, Engine_AIW2.Instance.CombatCenter, "TransportFlagship_Starter", "Potluck ", Context.GetHostOnlyContext(), true );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Grab bag of new ships spawning for player " +
                                PlayerFactionIssuingCommand.GetDisplayName() + " on " + OnPlanet.Name + " .",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "ohmu":
                        #region ohmu
                        {
                            if ( isIronman )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            PlayerPotluck( PlayerFactionIssuingCommand, OnPlanet, Engine_AIW2.Instance.CombatCenter, "InvincibleOhmu", "Ohmu ", Context.GetHostOnlyContext(), false );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Invincible Ohmu fleet spawning for player " +
                                PlayerFactionIssuingCommand.GetDisplayName() + " on " + OnPlanet.Name + " .",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "chainlight":
                        #region chainlight
                        {
                            if ( isIronman )
                                break;
                            if ( !ExpansionTable.Instance.GetRowByName( "2_Zenith_Onslaught" ).IsInstalledAndEnabled )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            PlayerPotluck( PlayerFactionIssuingCommand, OnPlanet, Engine_AIW2.Instance.CombatCenter, "InvincibleOhmuChainLightning", "Ohmu ", Context.GetHostOnlyContext(), false );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Invincible Ohmu Chain Lightning fleet spawning for player " +
                                PlayerFactionIssuingCommand.GetDisplayName() + " on " + OnPlanet.Name + " .",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "zeuscc":
                        #region zeuscc
                        {
                            if ( isIronman )
                                break;
                            if ( !ExpansionTable.Instance.GetRowByName( "2_Zenith_Onslaught" ).IsInstalledAndEnabled )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            PlayerShipSpawn( PlayerFactionIssuingCommand, OnPlanet, Engine_AIW2.Instance.CombatCenter, "TransportFlagship_Agile",
                                "Zeus CC ", Context.GetHostOnlyContext(), "AmbushCruiser", "TempestCruiser", "DisruptiveCruiser", "FusionCruiser", "GeneralistCruiser",
                                "MeleeCruiser", "RaidCruiser", "SplashCruiser", "SubterfugeCruiser", "TechnologistCruiser", "ZASpartacusCruiser", "DZGungnirCruiser" );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Zeus Almighty's Cruiser fleet spawning for player " +
                                PlayerFactionIssuingCommand.GetDisplayName() + " on " + OnPlanet.Name + " .",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "warhead":
                    case "warheads":
                        #region warheads
                        {
                            if ( isIronman )
                                break;
                            if ( !ExpansionTable.Instance.GetRowByName( "1_The_Spire_Rises" ).IsInstalledAndEnabled )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            PlayerShipSpawnSpecific( PlayerFactionIssuingCommand, OnPlanet, Engine_AIW2.Instance.CombatCenter, "TransportFlagship_Agile",
                                "Puffin War ", Context.GetHostOnlyContext(), 2, new string[] { "LightningWarhead", "StormWarhead", "EMPWarhead", "NuclearWarhead" }, 
                                1, new string[] { "TheLastResort", "TheTimeStopper", "TheWormholeJammer" } );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "An Angry Puffin's Warhead fleet spawning for player " +
                                PlayerFactionIssuingCommand.GetDisplayName() + " on " + OnPlanet.Name + " .",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "zeusds":
                        #region zeusds
                        {
                            if ( isIronman )
                                break;
                            if ( !ExpansionTable.Instance.GetRowByName( "3_The_Neinzul_Abyss" ).IsInstalledAndEnabled )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            PlayerShipSpawnSpecific( PlayerFactionIssuingCommand, OnPlanet, Engine_AIW2.Instance.CombatCenter, "TransportFlagship_Agile",
                                "Zeus DS ", Context.GetHostOnlyContext(), 2, new string[] { "PredatorDestroyer", "HellfireDestroyer", "BehemothDestroyer", "WranglerDestroyer", "SirenDestroyer",
                                "ChainsawDestroyer", "NuclearDestroyer", "AssassinDestroyer", "TornadoDestroyer" }, 0, null );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Zeus Almighty's Destroyer fleet spawning for player " +
                                PlayerFactionIssuingCommand.GetDisplayName() + " on " + OnPlanet.Name + " .",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "grantalloutguard":
                    case "alloutguard":
                    case "outguard":
                        #region grantalloutguard
                        {
                            if ( isIronman )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            OutguardFactionBaseInfo.Instance.CheatHackAllBeacons_HostOnly();

                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "All outguard are now available for use!",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "grantalltech":
                        #region grantalltech
                        {
                            if ( isIronman )
                                break;
                            isCheat = true; //always a cheat
                            debugStage = 2000;

                            GrantAllTech( PlayerFactionIssuingCommand, Context.GetHostOnlyContext() );

                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "All tech have now been upgraded!",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "healme":
                        #region healme
                        {
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            PlanetFaction pFaction = OnPlanet.Factions[PlayerFactionIssuingCommand.FactionIndex];
                            debugStage = 3000;
                            foreach ( GameEntity_Squad squad in pFaction.Entities.Squads() )
                            {
                                squad.CloakingPointsLost = 0;
                                squad.HullPointsLost = 0;
                                squad.ShieldPointsLost = 0;
                            }
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Fully healed all shields, hulls, and cloaking points of all ships of " +
                                PlayerFactionIssuingCommand.GetDisplayName() + " on " + OnPlanet.Name + " .",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "luckyrelic":
                        #region luckyrelic
                        {
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            Faction fallenSpireFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
                            if ( fallenSpireFaction == null )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Fallen Spire isn't on, sorry!",
                                    isCheat, false, null );
                                break;
                            }

                            FallenSpireFactionBaseInfo spireData = fallenSpireFaction.TryGetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();

                            if ( spireData == null )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Fallen Spire data isnt' set up, sorry!",
                                       isCheat, false, null );
                                break;
                            }
                            FallenSpireFactionBaseInfo.Instance.CreateRelic( OnPlanet, fallenSpireFaction, PlayerFactionIssuingCommand, Context.GetHostOnlyContext(), FInt.One, false, Engine_AIW2.Instance.CombatCenter, true );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Fallen Spire Relic spawned for player " + PlayerFactionIssuingCommand.GetDisplayName() + " on planet " + OnPlanet.Name,
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "luckycity":
                    case "spirecity":
                        #region luckycity
                        {
                            //Kills all enemies on the planet, then builds a new Spire City there
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            //First, wipe enemies on the planet
                            foreach ( GameEntity_Squad entity in OnPlanet.Squads() )
                            {
                                if ( entity.GetIsHostileTowards_Safe( PlayerFactionIssuingCommand ) )
                                {
                                    if ( entity.TypeData.TargetTypeForPlayer != PlayerTargetType.NeverTarget )
                                    {
                                        if ( entity.ExtraStackedSquadsInThis > 0 )
                                        {
                                            entity.AddOrSetExtraStackedSquadsInThis( 0, true );
                                        }
                                        entity.Die( Context, true );
                                    }
                                }
                            }
                            //then build a spire city
                            if ( PlayerFactionIssuingCommand.Type != FactionType.Player )
                                throw new Exception( PlayerFactionIssuingCommand.GetDisplayName() + " is not allowed to summon a spire relic" );
                            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireCityHub" );
                            if ( entityData == null )
                                throw new Exception( "Could not find entity data for SpireCityHub. Perhaps there's no XML?" );
                            ArcenPoint spawnLocation = OnPlanet.GetSafePlacementPointAroundPlanetCenter( Context.GetHostOnlyContext(), entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 400 ) );

                            PlayerTypeData playerType = PlayerFactionIssuingCommand.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( (playerType != null && !playerType.CountsAsSpireFaction) && FactionUtilityMethods.Instance.GetFallenSpireFaction() == null )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Fallen Spire isn't on, and you aren't Spire Empire, sorry!",
                                    isCheat, false, null );
                                break;

                            }

                            PlanetFaction pFaction = OnPlanet.GetPlanetFactionForFaction( PlayerFactionIssuingCommand );
                            GameEntity_Squad newCity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                                                                        pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context.GetHostOnlyContext(), "Cheat-City" );

                            FallenSpirePerUnitBaseInfo data = newCity.CreateExternalBaseInfo<FallenSpirePerUnitBaseInfo>( "FallenSpirePerUnitBaseInfo" );
                            Fleet cityFleet = newCity.FleetMembership.Fleet;

                            cityFleet.NameRaw = "Spire City 'FromDebug'";
                            cityFleet.CreateExternalBaseInfo<FallenSpireCityFleetBaseInfo>( "FallenSpireCityFleetBaseInfo" );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Spire city spawned for player " + PlayerFactionIssuingCommand.GetDisplayName() + " on planet " + OnPlanet.Name,
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "popcity":
                    case "populatespirecity":
                        #region populatespirecity
                        {
                            //If there is a spire city here, build lots of structures for it
                            if ( isIronman )
                                break;

                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            GameEntity_Squad city = null;
                            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "SpireCity" ) )
                            {
                                if ( entity.Planet == OnPlanet )
                                {
                                    city = entity;
                                    break;
                                }
                            }
                            if ( city == null )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "No spire city found on " + OnPlanet.Name,
                                    isCheat, false, null );
                                break;
                            }
                            Fleet cityFleet = city.FleetMembership.Fleet;
                            int socketsLeft = cityFleet.CalculateRemainingCitySockets();
                            if ( socketsLeft == 0 )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Spire City has no open sockets. Perhaps it is crippled, or you don't own the planet?",
                                    isCheat, false, null );
                                break;
                            }

                            debugStage = 4000;
                            int retries = 100;

                            while ( socketsLeft > 0 && retries > 0 )
                            {
                                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SpireCityBuildMenu" );
                                if ( entityData.CitySocketCost <= socketsLeft && entityData.MinimumRequiredCityLevelForConstruction <= city.CurrentMarkLevel )
                                {
                                    socketsLeft -= entityData.CitySocketCost;
                                    GameCommand buildCommand = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.PlaceSelfBuildingUnit],
                                                                              GameCommandSource.AnythingElse );
                                    buildCommand.RelatedString2 = entityData.InternalName;
                                    buildCommand.RelatedPoints.Add( OnPlanet.GetSafePlacementPoint_AroundEntity( Context.GetHostOnlyContext(), entityData, city,
                                                                                                            FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) ) );
                                    buildCommand.RelatedMagnitude = 1;
                                    buildCommand.RelatedEntityIDs.Add( city.PrimaryKeyID );
                                    buildCommand.RelatedIntegers.Add( cityFleet.FleetID );
                                    buildCommand.RelatedIntegers2.Add( 10 ); //ignore cap restrictions
                                    buildCommand.RelatedBool = true; //insta-build please
                                    World_AIW2.Instance.QueueGameCommand( PlayerFactionIssuingCommand, buildCommand, false );
                                }
                                retries--;
                            }
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Spire city populated for player " + city.PlanetFaction.Faction.GetDisplayName() + " on planet " + OnPlanet.Name,
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "decimate":
                    case "decimation":
                        #region decimate
                        {
                            if ( isIronman )
                                break;

                            int unitsDestroyed = 0;
                            int unitsSpared = 0;
                            int unitsNotValid = 0;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            int entityCounter = 0;
                            //First, wipe enemies on the planet
                            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads() )
                            {
                                if ( entity.TypeData.TargetTypeForPlayer != PlayerTargetType.NeverTarget && 
                                     !entity.TypeData.IsFleetLeader && 
                                     entity.TypeData.AIPOnDeath <= 0 && 
                                     entity.TypeData.AIPOnDeathWhenNoneLeft == 0 && 
                                     entity.TypeData.AIPToClaim == 0 && 
                                     entity.TypeData.AIPWhenGrantedByHack == 0 )
                                {
                                    switch ( entity.TypeData.SpecialType )
                                    {
                                        case SpecialEntityType.DroneFrigate:
                                        case SpecialEntityType.DroneGeneral:
                                        case SpecialEntityType.AIGuardian:
                                        case SpecialEntityType.LargeShipNotStackable:
                                        case SpecialEntityType.LargeShipYesStacks:
                                        case SpecialEntityType.Minefield:
                                        case SpecialEntityType.SmallShipNotStackable:
                                        case SpecialEntityType.SmallShipYesStacks:
                                        case SpecialEntityType.None:
                                            break; //only process the above types
                                        default:
                                            unitsNotValid++;
                                            continue;
                                    }
                                    entityCounter++;
                                    if ( entityCounter <= 9 )
                                    {
                                        unitsDestroyed++;
                                        unitsDestroyed += entity.ExtraStackedSquadsInThis;
                                        entity.Die( Context, true );
                                    }
                                    else
                                    {
                                        entityCounter = 0;
                                        unitsSpared++;
                                        unitsSpared += entity.ExtraStackedSquadsInThis;
                                    }
                                }
                                else
                                {
                                    unitsNotValid++;
                                    unitsNotValid += entity.GetCountOfContentsIfAny();
                                }
                            }

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "DECIMATION: 90% of most non-leader, non-guardpost, non-contained/transported units in the galaxy have been destroyed by a command from " + PlayerFactionIssuingCommand.GetDisplayName() +
                                " (" + unitsDestroyed.ToString( "#,##0" ) + " destroyed, " + unitsSpared.ToString( "#,##0" ) + " spared, " + unitsNotValid.ToString( "#,##0" ) + " not valid for decimation.)",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "duplicate":
                    case "duplication":
                        #region duplicate
                        {
                            if ( isIronman )
                                break;

                            int unitsDuplicated = 0;
                            int unitsNotValid = 0;
                            isCheat = true; //always a cheat
                            debugStage = 2000;
                            //Make a copy of everyone smaller!
                            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads() )
                            {
                                if ( entity.TypeData.TargetTypeForPlayer != PlayerTargetType.NeverTarget && !entity.TypeData.IsFleetLeader &&
                                    entity.TypeData.AIPOnDeath <= 0 && entity.TypeData.AIPOnDeathWhenNoneLeft == 0 && entity.TypeData.AIPToClaim == 0 && entity.TypeData.AIPWhenGrantedByHack == 0 )
                                {
                                    switch ( entity.TypeData.SpecialType )
                                    {
                                        case SpecialEntityType.DroneFrigate:
                                        case SpecialEntityType.DroneGeneral:
                                        case SpecialEntityType.AIGuardian:
                                        case SpecialEntityType.LargeShipNotStackable:
                                        case SpecialEntityType.LargeShipYesStacks:
                                        case SpecialEntityType.Minefield:
                                        case SpecialEntityType.SmallShipNotStackable:
                                        case SpecialEntityType.SmallShipYesStacks:
                                        case SpecialEntityType.None:
                                            break; //only process the above types
                                        default:
                                            unitsNotValid++;
                                            continue;
                                    }
                                    unitsDuplicated++;
                                    if ( entity.GameSecondCreated < World_AIW2.Instance.GameSecond - 1 ) //don't duplicate anything from the last second, or we get an infinite loop
                                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( entity.PlanetFaction, entity.TypeData, entity.CurrentMarkLevel, entity.GetFleetOrNull_Safe(), 0,
                                            entity.WorldLocation, Context.GetHostOnlyContext(), "Duplication Cheat" );
                                }
                                else
                                {
                                    unitsNotValid++;
                                    unitsNotValid += entity.GetCountOfContentsIfAny();
                                }
                            }

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "DUPLICATION: Most non-leader, non-guardpost, non-contained/transported units in the galaxy have been duplicated by a command from " + PlayerFactionIssuingCommand.GetDisplayName() +
                                " (" + unitsDuplicated.ToString( "#,##0" ) + " duplicated, " + unitsNotValid.ToString( "#,##0" ) + " not valid for duplication.)",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "emptyquarantine":
                        #region empty quarantine
                        {
                            isCheat = false; //never a cheat

                            int startingItemsQuarantined = TimeBasedPoolBase.GetCountOfQuarantinedItemsInAllPools();

                            //do this on a BG thread
                            ArcenThreading.RunTaskOnBackgroundThread( "_Cheat.EmptyQuarantine", false, false, delegate
                            {
                                TimeBasedPoolBase.DoAdvanceAllQueuesToMain();
                            } );

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "EMPTY QUARANTINE: " + startingItemsQuarantined.ToString( "#,##0" ) + 
                                " objects that were being held for safekeeping for another few seconds or minutes are now back in pools ready for immediate use.",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "zmhere":
                        #region zmhere
                        {
                            //spawns a zenith miner probe on this planet
                            if ( isIronman )
                                break;
                            if ( !ExpansionTable.Instance.GetRowByName( "2_Zenith_Onslaught" ).IsInstalledAndEnabled )
                                break;

                            isCheat = true; //could be used to make the game easier
                            debugStage = 2100;
                            Faction zmFaction = FactionUtilityMethods.Instance.GetZenithMinerFaction();
                            if ( zmFaction == null )
                                break;
                            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                            if ( planet == null )
                                break;
                            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                                break;
                            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ZenithMinerProbe" );
                            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( zmFaction );

                            ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context.GetHostOnlyContext(), entityData, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 75 ) );
                            GameEntity_Squad probe = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                                                 pFaction.Faction.LooseFleet, 0, spawnLocation, Context.GetHostOnlyContext(), "Cheat-ZMProbe" );  //is fine, main sim thread
                            if ( probe != null )
                            {
                                ZenithMinersPerUnitBaseInfo data = probe.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );
                                data.Effect = ZenithMinerEffect.DestroyPlanet;
                                data.RemainingDuration = 60;
                            }
                        }
                        #endregion
                        break;
                     case "elderhere":
                     case "elderling":
                        #region elderling
                        {
                            debugStage = 2110;
                            //spawns a zenith miner probe on this planet
                            if ( isIronman )
                                break;
                            if ( !ExpansionTable.Instance.GetRowByName( "3_The_Neinzul_Abyss" ).IsInstalledAndEnabled )
                                break;
                            
                            isCheat = true; //could be used to make the game easier
                            debugStage = 2120;
                            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                            {
                                Faction otherFaction = World_AIW2.Instance.Factions[i];
                                if ( !otherFaction.SpecialFactionData.LurableElderling )
                                    continue;

                                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                                if ( planet == null )
                                    break;
                                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                                    break;
                                debugStage = 2130;
                                for ( int k = 0; k < commandParts.Length; k++ )
                                    ArcenDebugging.ArcenDebugLogSingleLine("parts " + i + ": " + commandParts[k], Verbosity.DoNotShow );
                                ArcenDebugging.ArcenDebugLogSingleLine("Trying to spawn " + commandParts[1] + " for " + otherFaction.GetDisplayName(), Verbosity.DoNotShow );
                                debugStage = 2140;
                                //GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, commandParts[1] );
                                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( commandParts[1] );
                                if ( entityData == null )
                                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, commandParts[1] );
                                if ( entityData == null )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine("\tCould not find this unit", Verbosity.DoNotShow );
                                    break;
                                }
                                debugStage = 2150;
                                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( otherFaction );

                                ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context.GetHostOnlyContext(), entityData, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 75 ) );
                                GameEntity_Squad probe = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                                                                          pFaction.Faction.LooseFleet, 0, spawnLocation, Context.GetHostOnlyContext(), "Cheat-elderling" );  //is fine, main sim thread
                            }
                        }
                        #endregion
                        break;
                    case "achievementteset":
                        #region achievementteset
                        {
                            isCheat = false; //never a cheat, but wow is it totall a huge cheat!
                            debugStage = 2000;
                            Achievement ach = null;// AchievementTable.Instance.GetRowByName( commandParts[1], false, null ); //make it always fail!
                            if ( ach == null )
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, PlayerFactionIssuingCommand.GetDisplayName() + " wanted to grant themselves the achievement '" + commandParts[1] + "', but no achievement with that name could be found.",
                                   isCheat, false, null );
                                break;
                            }
                            ach.MarkCompleteAndReturnIfAnyDataChanged( true );
                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, PlayerFactionIssuingCommand.GetDisplayName() + " granted themselves the achievement '" + commandParts[1] + "'",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "journal":
                        #region journal
                        {
                            isCheat = false;
                            debugStage = 2000;
                            string name = commandParts[1];
                            debugStage = 2100;
                            string groupID = commandParts[2];
                            debugStage = 3000;
                            Faction relatedFaction = GetFirstFactionContainingText( commandParts[3] );
                            debugStage = 4000;
                            Planet relatedPlanet = GetFirstPlanetContainingText( false, commandParts[4] );
                            debugStage = 4100;
                            if ( relatedPlanet == null )
                                relatedPlanet = GetFirstPlanetContainingText( true, commandParts[4] );
                            debugStage = 5000;
                            GameEntityTypeData relatedTypeData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( commandParts[5] );

                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                                World_AIW2.Instance.QueueLogJournalEntryToSidebar( name, groupID, relatedFaction, relatedTypeData, relatedPlanet, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Journal test for : " + (name == null ? "null" : name),
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "makescenario":
                    case "makequickstart":
                        #region makequickstart
                        bool as_scenario = command == "makescenario";
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            debugStage = 2000;
                            
                            isCheat = false;
                            string qsname = null;
                            string qsfolder = null;

                            debugStage = 2001;
                            if (commandParts.Length > 1)
                                qsname = commandParts[1];
                            if (commandParts.Length > 2)
                                qsfolder = commandParts[2];
                            //if (commandParts.Length > 3)
                            //    qstooltip = commandParts[3];

                            string chat_msg;
                            
                            debugStage = 2002;
                            
                            var log = new Log();
                            bool success = SaveLoadMethods.SaveWorldToDisk_AsQuickStart( qsname, qsfolder, as_scenario, log );
                            
                            debugStage = 2003;
                            
                            if (success)
                            {
                                log.AppendFormat("Don't forget to add a .tooltip file!\n");
                                log.AppendFormat(
                                    "Used DLC:\n{0}Used Mods:\n{1}",
                                    Engine_Universal.GetExpansionsString().OrNull(), 
                                    Engine_Universal.GetXmlModsString().OrNull());
                            }
                            
                            chat_msg = log.ToString();

                            debugStage = 2005;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, chat_msg,
                                isCheat, success, null );
                            
                            debugStage = 2006;
                        }
                        #endregion
                        break;
                    case "findp":
                        #region findp
                        //ignore except for the local faction issuing the command
                        if ( PlayerAccountIssuingCommand == PlayerAccount.Local )
                        {
                            debugStage = 2000;
                            isCheat = false;
                            string planetName = commandParts[1];
                            FindPlanetInString( planetName );

                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Searching for planet containing name '" + commandParts[1] + "', and found " +
                                LastPlanetMatchingCount + " match(es)." + (LastPlanetFound == null ? string.Empty : "  Centering on planet: " + LastPlanetFound.Name +
                                (LastPlanetMatchingCount > 1 ? ".  Repeat the search to go to the next in the list." : string.Empty)),
                                isCheat, true, null );

                            if ( LastPlanetFound != null )
                            {
                                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                                {
                                    if ( Window_ChatboxWindow.Instance.IsOpen )
                                        Window_ChatboxWindow.Instance.Close( false );//Camera won't move while it is open.
                                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( LastPlanetFound, true );
                                }

                                World_AIW2.Instance.SwitchViewToPlanet( LastPlanetFound );
                            }
                        }
                        #endregion
                        break;
                    case "destroyp":
                        #region destroyp
                        //ignore except for the local faction issuing the command
                        if ( PlayerAccountIssuingCommand == PlayerAccount.Local )
                        {
                            debugStage = 2000;
                            if ( isIronman )
                                break;

                            isCheat = true;
                            string planetName = commandParts[1];
                            FindPlanetInString( planetName );

                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Searching for planet containing name '" + commandParts[1] + "', and found " +
                                LastPlanetMatchingCount + " match(es)." + (LastPlanetFound == null ? string.Empty : "  Destroying: " + LastPlanetFound.Name + "."),
                                isCheat, true, null );

                            if ( LastPlanetFound != null )
                            {
                                LastPlanetFound.IsPlanetToBeDestroyed = true;
                                LastPlanetFound.Network_HostOnly_NeedToSyncWormholesToClients = true;
                                LastPlanetFound.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                            }
                        }
                        #endregion
                        break;
                    case "destroycurrentp":
                        #region destroycurrentp
                        //ignore except for the local faction issuing the command
                        if ( PlayerAccountIssuingCommand == PlayerAccount.Local && OnPlanet != null )
                        {
                            debugStage = 2000;
                            if ( isIronman )
                                break;

                            isCheat = true;
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Destroying planet " + OnPlanet.Name + ".",
                                isCheat, true, null );

                            OnPlanet.IsPlanetToBeDestroyed = true;
                            OnPlanet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                            OnPlanet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                        }
                        #endregion
                        break;
                    case "scrap":
                        #region scrap
                        {
                            //kills everything selected

                            isCheat = true; 
                            debugStage = 2000;
                            
                            int count = 0;
                            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads() )
                            {
                                if (entity.GetIsSelected())
                                {
                                    entity.Die( Context, true, null, DamageSource.BeingScrapped );
                                    count++;
                                }
                            }
                            
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, string.Format("Killed '{0}' selected entities",count), isCheat, true, null );
                        }
                        break;
                        #endregion
                    case "kill":
                        #region kill
                        {
                            //kills everything selected

                            isCheat = true; 
                            debugStage = 2000;
                            
                            int count = 0;
                            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads() )
                            {
                                if (entity.GetIsSelected())
                                {
                                    entity.Die( Context, true, null, DamageSource.SomeSortOfEnemy );
                                    count++;
                                }
                            }
                            
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, string.Format("Killed '{0}' selected entities",count), isCheat, true, null );
                        }
                        break;
                        #endregion
                    case "renamecampaign":
                        #region renamecampaign
                        //ignore except for the local faction issuing the command who is NOT a client
                        if ( PlayerAccountIssuingCommand == PlayerAccount.Local && ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        {
                            debugStage = 2000;
                            if ( isIronman )
                                break;

                            string newName = commandParts[1];
                            newName = newName.ConvertToValidFormatNoStartingSpaces();
                            if ( newName.Length == 0 )
                            {
                                ArcenDebugging.ArcenDebugLog( "Could not rename campaign, because the campaign name is empty!", Verbosity.ShowAsError );
                                return;
                            }

                            isCheat = false;
                            debugStage = 4000;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "Renaming campaign to '" + newName + "'.  Savegames will go in this folder from now on.",
                                isCheat, true, null );

                            World.Instance.CampaignName = newName;
                        }
                        #endregion
                        break;
                    case "voicelady":
                        #region voicelady
                        {
                            isCheat = false;
                            debugStage = 3000;

                            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            {
                                SFXItemType_NonPositional randomToPlay = (SFXItemType_NonPositional)Context.RandomToUse.Next( (int)SFXItemType_NonPositional.AnyEnemiesAlarm, (int)SFXItemType_NonPositional.CannotDoThatThing );
                                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, randomToPlay );
                            }

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Voice Lady: Playing a random voice sound.",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "livegraftship":
                        #region livegraftship
                        try
                        {
                            /* syntax:
                             * "livegraftship"
                             * [new entity name postfix]
                             * [entity name to copy from]
                             * [entity name to copy systems from]
                             * {
                             *  for systems:
                             *      "s"
                             *      [system internal name]
                             *      [amount of times (for weapons only)]
                             *  for hulls:
                             *      "h"
                             *      [hull internal name]
                             * }
                             */

                            ArcenDebugging.SingleLineQuickDebug( "livegraftship:" );

                            var newType = GameEntityTypeDataTable.Instance.GetRowByName(commandParts[1]);

                            //GameEntityTypeDataTable.Instance.DoPostInitializationAndSortingLogic_BackgroundThreads();
                            debugStage = 13100;
                            //GameEntityTypeDataTable.Instance.RecalculateAllBalanceStats();
                            debugStage = 14000;
                            Faction localPlayer = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                            debugStage = 15000;
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( localPlayer.GetFactionKing().PlanetFaction, newType, 1,
                                localPlayer.LooseFleet, 0, ArcenPoint.ZeroZeroPoint, Context.GetHostOnlyContext(), "Cheat-LiveGraftShip" );
                        } catch(Exception e)
                        {
                            ArcenDebugging.SingleLineQuickDebug( "Error in graftliveship at " + debugStage + ":");
                            ArcenDebugging.ArcenDebugLog( e );
                        }
                        #endregion
                        break;
                    case "instigator":
                        #region instigator
                            void MakeAnInstigator()
                            {
                                var faction = World_AIW2.Instance.GetSpecialFactionInstanceByName("Instigators");
                                if (faction == null)
                                    return;

                                var info = faction.BaseInfo as InstigatorFactionBaseInfo;
                                if ( info == null )
                                    return;
                    
                                info.TimeForNextInstigatorBaseSpawn = World_AIW2.Instance.GameSecond;
                            }
                        MakeAnInstigator();
                        #endregion
                        break;
                    case "nomad":
                        #region nomad
                        OnPlanet.MakePlanetNomadic();

                        int GetCrashTime( Planet nomadPlanet, Planet targetPlanet, int distance )
                        {
                            //Nomad Planets start at base time for the crash, then it goes up based on distance between nomad and target to a max

                            int MinCrashTime = 360;
                            int MaxCrashTime = 720;
                            int CrashTimeIncreasePerDistanceUnit = -1;
                            int CrashDistanceUnit = -1;

                            MinCrashTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_MinCrashTime" );
                            MaxCrashTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_MaxCrashTime" );
                            CrashTimeIncreasePerDistanceUnit = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_CrashTimeIncreasePerDistanceUnit" );
                            CrashDistanceUnit = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_CrashDistanceUnit" );

                            int baseCrashTime = MinCrashTime;
                            int distanceUnits = distance / CrashDistanceUnit;
                            int distanceBasedIncrease = distanceUnits * CrashTimeIncreasePerDistanceUnit;
                            int totalCrashTime = baseCrashTime + distanceBasedIncrease;
                            if ( totalCrashTime > MaxCrashTime )
                                totalCrashTime = MaxCrashTime;

                            return totalCrashTime;
                        }

                        var target = World_AIW2.Instance.CurrentGalaxy.GetRandomPlanet(false, Context.GetHostOnlyContext());
                        
                        OnPlanet.SecondsTillNomadCrashes = 60 * 5;
                        OnPlanet.NomadTargetPlanetIdx = target.Index;

                        int dist = Mat.DistanceBetweenPointsImprecise( OnPlanet.GalaxyLocation, target.GalaxyLocation );
                        OnPlanet.SecondsTillNomadCrashes = GetCrashTime( OnPlanet, target, dist);

                        OnPlanet.TimeForNextMove = World_AIW2.Instance.GameSecond + 30;

                        isCheat=true;
                        WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet,
                                "Coverted a planet to be Nomadic.",
                                isCheat, true, null );
                        #endregion
                        break;
                    case "allnecroflagships":
                        #region allnecroflagships
                        {
                            if ( isIronman )
                                break;
                            isCheat = true;
                            debugStage = 2000;
                            NecromancerUpgradeTable.Instance.CheatAllUpgrades_HostOnly(hostCtx);

                            debugStage = 4000;

                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "All necromancer flagships are now unlocked!",
                                isCheat, true, null );
                        }
                        #endregion
                        break;
                    case "warp":
                        void doWarpCmd()
                        {
                            if (OnPlanet == null)
                            {
                                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "No planet viewed or selected!",
                                    isCheat, true, null );
                                
                                return;
                            }
                            
                            int count = 0;
                            foreach ( GameEntity_Squad squad in Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo )
                            {
                                var placementLocation = OnPlanet.GetSafePlacementPoint_AroundZone(Context.GetHostOnlyContext(), squad.TypeData, PlanetSeedingZone.InnerSystem);
                                squad.WarpToPlanet(OnPlanet, "Cheat_Cmd_Warp");
                                count++;
                            }
                            
                            isCheat = true;
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, string.Format("Warped {0} selected squads to {1}!", count, OnPlanet.Name),
                                    isCheat, true, null );
                        }
                        doWarpCmd();
                        break;
                    default:
                        var cmdRet = new CmdReturnValue();
                        ArcenExternalCodeHook hook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "OnCheatCommand" );
                        if ( hook != null )
                            hook.HandleAllSubscribedHooks( command, cmdRet, new object[] { OnPlanet, commandParts }, Context );
                        else
                            ArcenDebugging.ArcenDebugLog( "Could not find OnCheatCommand Hook", Verbosity.ShowAsError );

                        if (!cmdRet.Handled)
                            WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "unknown command: '" + commandParts[0] + "'", isCheat, false, null );
                        break;
                }
            }
            catch ( Exception e )
            {
                WriteCheatOrCommandResult( CameFrom, RawText, OnPlanet, "debugStage: " + debugStage, isCheat, false, e );
            }
        }

        private static void GrantAllTech(Faction faction, ArcenHostOnlySimContext context)
        {
            List<TechUpgrade> upgrades = TechUpgradeTable.Instance.SortedTechUpgrades;
            for ( int i = 0; i < upgrades.Count; i++ )
            {
                TechUpgrade upgrade = upgrades[i];
                while (faction.UnlockTech( upgrade, false, false) == ArcenRejectionReason.Unknown) {
                    // Upgrade everything.
                }
            }
        }

        #region WriteCheatOrCommandResult
        public static void WriteCheatOrCommandResult( string CameFrom, string RawText, Planet OnPlanet, string Result, bool IsCheat, bool IsSuccess, Exception eOrNull )
        {
            string finalMessage = CameFrom + (IsSuccess ? " used" : " attemped") + (IsCheat ? " cheat" : " command") + " on planet " + (OnPlanet == null ? "null" : OnPlanet.Name );
            if ( IsSuccess )
                finalMessage += " with result:</color> " + Result;
            else
                finalMessage += " (" + RawText + ") with failure:</color> " + Result;

            Engine_Universal.WriteToLocalMomentaryDisplayLog( finalMessage, null );
            World_AIW2.Instance.WriteToLongTermChatLog( finalMessage, null );
            ArcenDebugging.ArcenDebugLogSingleLine( finalMessage + (eOrNull == null ? string.Empty : " full error: " + eOrNull ), Verbosity.DoNotShow );

            if ( IsCheat && IsSuccess )
                World.Instance.HaveDoneAnyCheatingThatBlocksAchievemets = true;
        }
        #endregion

        #region GetFirstFactionContainingText
        public static Faction GetFirstFactionContainingText( string Text )
        {
            Faction fac = null;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                fac = World_AIW2.Instance.Factions[i];
                if ( fac.GetDisplayName().Contains( Text ) )
                    return fac;
                if ( fac.SpecialFactionData.InternalName.Contains( Text ) )
                    return fac;
            }
            return null;
        }
        #endregion

        #region GetFirstPlanetContainingText
        public static Planet GetFirstPlanetContainingText( bool IncludeDestroyed, string Text )
        {
            Planet finalPlan = null;
            foreach ( Planet plan in World_AIW2.Instance.Planets( IncludeDestroyed ) )
            {
                if ( plan.Name.Contains( Text ) )
                {
                    finalPlan = plan;
                    continue;
                }
            }
            return finalPlan;
        }
        #endregion

        #region FindPlanetInString
        public static Planet LastPlanetFound = null;
        public static string LastPlanetFoundByString = string.Empty;
        public static int LastPlanetMatchingCount = 0;

        public static void GotoLastPlanet()
        {
            if ( LastPlanetFound != null )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                {
                    if ( Window_ChatboxWindow.Instance.IsOpen )
                        Window_ChatboxWindow.Instance.Close( false );//Camera won't move while it is open.
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( LastPlanetFound, true );
                }

                World_AIW2.Instance.SwitchViewToPlanet( LastPlanetFound );
            }
        }
        public static void FindPlanetInString( String text )
        {
            try
            {
                if ( text != LastPlanetFoundByString )
                {
                    LastPlanetFoundByString = text;
                    LastPlanetFound = null;
                }

                Planet newFoundPlanet = null;
                bool needsToFindPriorPlanetFirst = (LastPlanetFound != null); //if found something before with same search terms, then only search after that one

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( needsToFindPriorPlanetFirst )
                    {
                        if ( planet == LastPlanetFound )
                            needsToFindPriorPlanetFirst = false;
                        continue; //skip all until we find the first planet from before
                    }
                    //now check if the planet name contains this text, ignoring case
                    if ( planet.Name.Contains( text, StringComparison.InvariantCultureIgnoreCase ) )
                    {
                        newFoundPlanet = planet;
                        break;
                    }
                }
                //if we found nothing this time, but last time we found something with the same search query
                if ( newFoundPlanet == null && LastPlanetFound != null )
                {
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        //search from the start, aka not caring about needsToFindPriorPlanetFirst
                        //just check if the planet name contains this text, ignoring case
                        if ( planet.Name.Contains( text, StringComparison.InvariantCultureIgnoreCase ) )
                        {
                            newFoundPlanet = planet;
                            break;
                        }
                    }
                }

                if ( newFoundPlanet != null )
                {
                    LastPlanetFound = newFoundPlanet;

                    LastPlanetMatchingCount = 0;
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        //just check if the planet name contains this text, ignoring case, to get count
                        if ( planet.Name.Contains( text, StringComparison.InvariantCultureIgnoreCase ) )
                        {
                            LastPlanetMatchingCount++;
                            continue;
                        }
                    }
                }
                else
                    LastPlanetMatchingCount = 0;

            }
            catch ( Exception )
            { }

        }
        #endregion

        #region PlayerPotluck
        public static void PlayerPotluck( Faction faction, Planet planet, ArcenPoint Location, string FlagshipName, string FleetNameStart, ArcenHostOnlySimContext Context, bool IncludeOtherShips )
        {
            if ( Context == null ) //client
                return; //don't even try this on clients
            int debugStage = 100;
            try
            {
                FInt strengthToSpawn = FInt.FromParts( 8000, 000 );
                int numFailures = 10;

                debugStage = 600;
                PlanetFaction pFaction = planet.Factions[faction.FactionIndex];
                debugStage = 700;
                GameEntityTypeData fleetLeaderTypeData = GameEntityTypeDataTable.Instance.GetRowByName( FlagshipName );
                debugStage = 800;
                GameEntity_Squad newFleetFleader = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, fleetLeaderTypeData, fleetLeaderTypeData.MarkFor( pFaction ),
                    null, 0, Location, Context, "Cheat-PlayerPotluck" );
                debugStage = 900;

                List<string> fleetNames = FleetNameTypeDataTable.Instance.Rows[Context.RandomToUse.Next( 0, FleetNameTypeDataTable.Instance.Rows.Count )].GetPossibleNames();
                debugStage = 1000;
                Fleet newFleet = newFleetFleader.GetFleetOrNull_Safe();
                if ( newFleet != null )
                    newFleet.NameRaw = FleetNameStart + fleetNames[Context.RandomToUse.Next( 0, fleetNames.Count )];

                debugStage = 1100;
                if ( IncludeOtherShips )
                {
                    while ( strengthToSpawn > FInt.Zero )
                    {
                        bool SpawnGolem = Context.RandomToUse.Next( 0, 100 ) < 3;
                        debugStage = 1200;
                        int randomNumber = Context.RandomToUse.Next( 0, 4 );
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HighHRFSpawn" );
                        debugStage = 1300;
                        if ( randomNumber == 0 )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HighHRFSpawn" );
                        else if ( randomNumber == 1 )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MedHRFSpawn" );
                        else if ( randomNumber == 2 )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "LowHRFSpawn" );
                        debugStage = 1400;
                        if ( entityData == null )
                        {
                            numFailures--;
                            if ( numFailures <= 0 )
                                break;
                            continue;
                        }
                        debugStage = 1500;
                        if ( SpawnGolem )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowOfSpecialType( Context, SpecialEntityType.LoneGolem );
                        debugStage = 1600;
                        if ( entityData == null )
                        {
                            numFailures--;
                            if ( numFailures <= 0 )
                                break;
                            continue;
                        }
                        debugStage = 1700;
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                            newFleetFleader.GetFleetOrNull_Safe(), 0, Location, Context, "Cheat-PlayerPotluck" );
                        debugStage = 1800;
                        strengthToSpawn -= entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in PlayerPotluck. debugStage " + debugStage + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        #region PlayerShipSpawn
        public static void PlayerShipSpawn( Faction faction, Planet planet, ArcenPoint Location, string FlagshipName, string FleetNameStart, ArcenHostOnlySimContext Context, params string[] OtherShipsToSpawn )
        {
            if ( Context == null ) //client
                return; //don't even try this on clients
            int debugStage = 100;
            try
            {
                int numFailures = 10;

                debugStage = 600;
                PlanetFaction pFaction = planet.Factions[faction.FactionIndex];
                debugStage = 700;
                GameEntityTypeData fleetLeaderTypeData = GameEntityTypeDataTable.Instance.GetRowByName( FlagshipName );
                debugStage = 800;
                GameEntity_Squad newFleetFleader = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, fleetLeaderTypeData, fleetLeaderTypeData.MarkFor( pFaction ),
                    null, 0, Location, Context, "Cheat-PlayerShipSpawn" );
                debugStage = 900;

                List<string> fleetNames = FleetNameTypeDataTable.Instance.Rows[Context.RandomToUse.Next( 0, FleetNameTypeDataTable.Instance.Rows.Count )].GetPossibleNames();
                debugStage = 1000;
                Fleet newFleet = newFleetFleader.GetFleetOrNull_Safe();
                if ( newFleet != null )
                    newFleet.NameRaw = FleetNameStart + fleetNames[Context.RandomToUse.Next( 0, fleetNames.Count )];

                debugStage = 1100;
                if ( OtherShipsToSpawn != null && OtherShipsToSpawn.Length > 0 )
                {
                    foreach ( string shipName in OtherShipsToSpawn )
                    {
                        debugStage = 1200;
                        int randomNumber = Context.RandomToUse.Next( 0, 4 );
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( shipName );
                        debugStage = 1300;
                        debugStage = 1400;
                        if ( entityData == null )
                        {
                            numFailures--;
                            if ( numFailures <= 0 )
                                break;
                            continue;
                        }
                        debugStage = 1700;
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                            newFleetFleader.GetFleetOrNull_Safe(), 0, Location, Context, "Cheat-PlayerShipSpawn" );
                        debugStage = 1800;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in PlayerShipSpawn. debugStage " + debugStage + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        #region PlayerShipSpawnSpecific
        public static void PlayerShipSpawnSpecific( Faction faction, Planet planet, ArcenPoint Location, string FlagshipName, string FleetNameStart, ArcenHostOnlySimContext Context, 
            int SpecificCountOfEach1, string[] OtherShipsToSpawn1, int SpecificCountOfEach2, string[] OtherShipsToSpawn2 )
        {
            if ( Context == null ) //client
                return; //don't even try this on clients
            int debugStage = 100;
            try
            {
                debugStage = 600;
                PlanetFaction pFaction = planet.Factions[faction.FactionIndex];
                debugStage = 700;
                GameEntityTypeData fleetLeaderTypeData = GameEntityTypeDataTable.Instance.GetRowByName( FlagshipName );
                debugStage = 800;
                GameEntity_Squad newFleetFleader = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, fleetLeaderTypeData, fleetLeaderTypeData.MarkFor( pFaction ),
                    null, 0, Location, Context, "Cheat-PlayerShipSpawnSpecific" );
                debugStage = 900;

                List<string> fleetNames = FleetNameTypeDataTable.Instance.Rows[Context.RandomToUse.Next( 0, FleetNameTypeDataTable.Instance.Rows.Count )].GetPossibleNames();
                debugStage = 1000;
                Fleet newFleet = newFleetFleader.GetFleetOrNull_Safe();
                if ( newFleet != null )
                    newFleet.NameRaw = FleetNameStart + fleetNames[Context.RandomToUse.Next( 0, fleetNames.Count )];

                debugStage = 1100;
                if ( OtherShipsToSpawn1 != null && OtherShipsToSpawn1.Length > 0 )
                {
                    foreach ( string shipName in OtherShipsToSpawn1 )
                    {
                        debugStage = 1200;
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( shipName );
                        debugStage = 1300;
                        if ( entityData == null )
                            continue;
                        debugStage = 1400;
                        for ( int i = 0; i < SpecificCountOfEach1; i++ )
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                newFleetFleader.GetFleetOrNull_Safe(), 0, Location, Context, "Cheat-PlayerShipSpawnSpecific" );
                        debugStage = 1500;
                    }
                }

                debugStage = 2100;
                if ( OtherShipsToSpawn2 != null && OtherShipsToSpawn2.Length > 0 )
                {
                    foreach ( string shipName in OtherShipsToSpawn2 )
                    {
                        debugStage = 2200;
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( shipName );
                        debugStage = 2300;
                        if ( entityData == null )
                            continue;
                        debugStage = 2400;
                        for ( int i = 0; i < SpecificCountOfEach2; i++ )
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                newFleetFleader.GetFleetOrNull_Safe(), 0, Location, Context, "Cheat-PlayerShipSpawnSpecific" );
                        debugStage = 2500;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in PlayerShipSpawnSpecific. debugStage " + debugStage + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        #region DumpAllShipInfo

        private static List<KeyValuePair<int, SquadRegistryInfo>> nullEntries = List<KeyValuePair<int, SquadRegistryInfo>>.Create_WillNeverBeGCed( 300, "CheatsAndCommands-nullEntries", 300 );
        public static void DumpAllShipInfo()
        {
            World_AIW2.Instance.FullShipStatsLogger.StartNewLog();

            nullEntries.Clear();

            ConcurrentDictionary<int, SquadRegistryInfo> centralDict = World_AIW2.Instance.GetSquadCentralLookup_OnlyDoThisIfYouReallyKnowWhatYouAreDoing();
            foreach ( KeyValuePair<int, SquadRegistryInfo> kv in centralDict )
            {
                if ( kv.Value.Squad == null )
                    nullEntries.Add( kv );
                else
                    World_AIW2.Instance.FullShipStatsLogger.PreLog( kv.Key, (kv.Value.Squad == null ? "[null]" : kv.Value.Squad.TypeData.InternalName +
                        (kv.Value.Squad.IsInQuarantine ? " (InQuarantine)" : string.Empty ) ) + "  " +
                        (kv.Value.Faction == null ? "[null]" : kv.Value.Faction.GetNameForListLogging()) + "  " +
                        (kv.Value.Fleet == null ? "[null]" : kv.Value.Fleet.GetName()) + "  " +
                        kv.Value.CurrentStatus + "\n" + kv.Value.LastReasonForAdding );
            }

            World_AIW2.Instance.FullShipStatsLogger.CommitToLogWithHeader( "Ship Dictionary" );

            foreach ( KeyValuePair<int, SquadRegistryInfo> kv in nullEntries )
            {
                World_AIW2.Instance.FullShipStatsLogger.PreLog( kv.Key, (kv.Value.Squad == null ? "[null]" : kv.Value.Squad.TypeData.InternalName +
                       (kv.Value.Squad.IsInQuarantine ? " (InQuarantine)" : string.Empty)) + "  " +
                       (kv.Value.Faction == null ? "[null]" : kv.Value.Faction.GetNameForListLogging()) + "  " +
                       (kv.Value.Fleet == null ? "[null]" : kv.Value.Fleet.GetName()) + "  " +
                       kv.Value.CurrentStatus + "\n" + kv.Value.LastReasonForAdding );
            }

            World_AIW2.Instance.FullShipStatsLogger.CommitToLogWithHeader( "Ship Death Registry" );

            foreach ( Planet planet in World_AIW2.Instance.Planets( true ) )
            {
 
                for ( int i = 0; i < planet.Factions.Count; i++ )
                {
                    PlanetFaction pFac = planet.Factions[i];
                    foreach ( GameEntity_Squad squad in pFac.Entities.Squads() )
                    {
                        if ( squad == null )
                            continue;

                        if ( squad.IsInQuarantine )
                        {
                            World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad is in quarantine!" );
                            continue; // was RemoveAndContinue (no-op) //hopefully this fixes it at least
                        }

                        SquadRegistryInfo registryInfo = World_AIW2.Instance.GetEntityRegistryInfo_Squad( squad.PrimaryKeyID );
                        if ( registryInfo != null )
                        {
                            if ( registryInfo.Squad != squad )
                            {
                                if ( registryInfo.Squad == null )
                                {
                                    if ( squad.HasBeenRemovedFromSim || squad.IsInPoolAtAll )
                                    {
                                        World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad is duplicated, and the new squad is dead!" );
                                    }
                                    else
                                    {
                                        InstancedRendererDeactivationReason deactivated = registryInfo.CurrentStatus;
                                        if ( deactivated < InstancedRendererDeactivationReason.IAmAliveAndFineActually )
                                        {
                                            World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad is duplicated, and deactivated status: " + deactivated );
                                        }
                                        else if ( deactivated == InstancedRendererDeactivationReason.IAmAliveAndFineActually )
                                        {
                                            World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad is duplicated, and we are overrwriting." );
                                        }
                                    }
                                }
                                else if ( registryInfo.Squad != null )
                                {
                                    if ( registryInfo.Squad.FleetMembership == squad.FleetMembership )
                                    {
                                        World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad is duplicated, and we are removing because it was the same fleet." );
                                    }
                                    else
                                    {
                                        if ( registryInfo.Squad.HasBeenRemovedFromSim )
                                        {
                                            World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad is duplicated, and we are overrwriting becuase old squad was dead." );
                                        }
                                        else
                                        {
                                            World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad is duplicated, and we are removing the new one because no special status." );
                                        }
                                    }
                                }
                                else
                                {
                                    World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, planet.Name + ": " + pFac.Faction.GetNameForListLogging() + ": " +
                                        squad.TypeData.InternalName + ( squad.IsInQuarantine ? " (InQuarantine)" : string.Empty) + "  " +
                                        registryInfo.CurrentStatus + "\n" + registryInfo.LastReasonForAdding );
                                }
                            }
                        }
                        else
                        {
                            World_AIW2.Instance.FullShipStatsLogger.PreLog( squad.PrimaryKeyID, "Squad central registry null!" );
                        }
                    }
                }
            }

            World_AIW2.Instance.FullShipStatsLogger.EndLog();
        }
        #endregion
    }
}
