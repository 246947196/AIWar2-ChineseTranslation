using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SplinteringSpireFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        // Important Values
        public static SplinteringSpireFactionBaseInfo Instance;
        public int Intensity;
        public SplinteringSpireDifficulty Difficulty;
        public readonly DoubleBufferedList<Faction> ActiveCoalitionFactions = DoubleBufferedList<Faction>.Create_WillNeverBeGCed( 6, "SplinteringSpireBaseInfo-ActiveCoalitionFactions" );

        #region Constants
        public const string Tag_CoalitionSpawner = "SplinteringSpireCoalitionSpawner";
        public const string Tag_Derelict = "SplinteringSpireDerelict";
        public const string Tag_Collector = "SplinteringSpireCollector";

        public const string Tag_CollectorGraySpire = "GraySpireCollector";
        public const string Tag_CollectorChromaticSpire = "ChromaticSpireCollector";
        public const string Tag_CollectorZenith = "ZenithCollector";

        public const string Tag_SphereBastion = "SplinteringSpireSphereBastion";

        public const string Tag_CoalitionUnit = "SplinteringSpireCoalitionUnit";
        public const string Tag_CoalitionUnit_PrefixRegular = "SSCU";
        public const string Tag_CoalitionUnit_PrefixDarkAlliance = "DASSCU";

        public const string FactionField_CoalitionMember = "PartOfTheAntiDarkSpireCoalition";
        public const string FactionField_SplinteringSpireSubfaction = "SplinteringSpireSubfaction";
        public const string FactionField_CoalitionActivationTime = "AntiDarkSpireCoalitionLateAwakeTimer";

        public const string StateOfMatter = "SphereShielded";
        public const byte MaxUnitTier = SphereFactionBaseInfo.MaxUnitTier;
        #endregion

        #region Variables And Collections
        // Serialized
        public readonly ConcurrentDictionary<Faction, short> UnclaimedVictoryPoints = ConcurrentDictionary<Faction, short>.Create_WillNeverBeGCed( "SplinteringSpireBaseInfo-UnclaimedVictoryPoints" );
        public readonly ConcurrentDictionary<Faction, short> TotalTimesWon = ConcurrentDictionary<Faction, short>.Create_WillNeverBeGCed( "SplinteringSpireBaseInfo-TotalTimesWon" );
        public readonly DoubleBufferedDictionaryOfDictionaries<Faction, Planet, int> TotalCollectedFromDerelictPlanetByFaction = DoubleBufferedDictionaryOfDictionaries<Faction, Planet, int>.Create_WillNeverBeGCed( 5, 6, "SplinteringSpireBaseInfo-TotalCollectedFromDerelictPlanetByFaction" );

        public readonly DictionaryOfLists<Faction, FInt> CoalitionStoredBudgetByTier = DictionaryOfLists<Faction, FInt>.Create_WillNeverBeGCed( 5, 6, "SplinteringSpireBaseInfo-CoalitionBudgetByTier" );

        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "SplinteringSpireBaseInfo-Teams" );

        public readonly ConcurrentDictionary<Planet, int> GameSecondControlWasEstablishedOnPlanet = ConcurrentDictionary<Planet, int>.Create_WillNeverBeGCed( "SplinteringSpireBaseInfo-GameSecondControlWasEstablishedOnPlanet" );

        public bool SentAwakeningJournal;

        // Nonserialized
        public DoubleBufferedDictionary<Planet, int> CollectorsAssignedByPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 40, "SplinteringSpireBaseInfo-CollectorsAssignedByPlanet" );

        public List<Planet> InvalidPlanetsToBeRemoved = List<Planet>.Create_WillNeverBeGCed( 5, "SplinteringSpireBaseInfo-CollectorsAssignedByPlanet" );

        public DoubleBufferedDictionaryOfDictionaries<Faction, Planet, FInt> ControlByFactionOnDerelictPlanet = DoubleBufferedDictionaryOfDictionaries<Faction, Planet, FInt>.Create_WillNeverBeGCed( 5, 6, "SplinteringSpireBaseInfo-ControlByFactionOnDerelictPlanet" );
        private Dictionary<Planet, FInt> ControlOnDerelictPlanetByFaction_Helper = Dictionary<Planet, FInt>.Create_WillNeverBeGCed( 5, "SplinteringSpireBaseInfo-ControlOnDerelictPlanetByFaction_Helper" );

        public DoubleBufferedList<SafeSquadWrapper> Derelicts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 5, "SplinteringSpireBaseInfo-Derelicts" );
        public DoubleBufferedList<SafeSquadWrapper> CollectorsToDecay = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 101, "SplinteringSpireBaseInfo-CollectorsToDecay" );
        public DoubleBufferedList<SafeSquadWrapper> CoalitionSpawners = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 101, "SplinteringSpireBaseInfo-CoalitionSpawners" );

        // Note - It is far, far too heavy for Stage2 to keep track and equally divide all units by their owning faction. Instead, we'll just be counting strength.
        public DoubleBufferedDictionary<short, int> CoalitionStrengthByFactionIndex = DoubleBufferedDictionary<short, int>.Create_WillNeverBeGCed( 6, "SplinteringSpireBaseInfo-CoalitionStrengthByFactionIndex" );

        public int CoalitionLateAwakeningSecond;
        #endregion

        #region Budget Access/Modification
        public void StartOrClearStoredBudgetForFaction( Faction faction )
        {
            if ( CoalitionStoredBudgetByTier.CheckIfAlreadyHasKey( faction ) )
            {
                for ( byte x = 0; x < MaxUnitTier; x++ )
                    if ( CoalitionStoredBudgetByTier[faction].Count > x )
                        CoalitionStoredBudgetByTier[faction][x] = FInt.Zero;
                    else
                        CoalitionStoredBudgetByTier[faction].Add( FInt.Zero );
            }
            else
            {
                CoalitionStoredBudgetByTier.AddToList( faction, FInt.Zero );
                CoalitionStoredBudgetByTier.AddToList( faction, FInt.Zero );
                CoalitionStoredBudgetByTier.AddToList( faction, FInt.Zero );
            }
        }

        public FInt GetStoredBudgetForTierForFaction( Faction faction, byte tier )
        {
            if ( !CoalitionStoredBudgetByTier.CheckIfAlreadyHasKey( faction ) )
                StartOrClearStoredBudgetForFaction( faction );

            return CoalitionStoredBudgetByTier[faction][tier];
        }

        public void AddToStoredBudgetForTierForFaction( Faction faction, byte tier, int valueToAdd ) => AddToStoredBudgetForTierForFaction( faction, tier, FInt.Zero + valueToAdd );
        public void AddToStoredBudgetForTierForFaction( Faction faction, byte tier, FInt valueToAdd )
        {
            if ( !CoalitionStoredBudgetByTier.CheckIfAlreadyHasKey( faction ) )
                StartOrClearStoredBudgetForFaction( faction );

            CoalitionStoredBudgetByTier[faction][tier] += valueToAdd;
        }

        public void OverrideStoredBudgetForTierForFaction( Faction faction, byte tier, int valueToUse ) => OverrideStoredBudgetForTierForFaction( faction, tier, FInt.Zero + valueToUse );
        public void OverrideStoredBudgetForTierForFaction( Faction faction, byte tier, FInt valueToUse )
        {
            if ( !CoalitionStoredBudgetByTier.CheckIfAlreadyHasKey( faction ) )
                StartOrClearStoredBudgetForFaction( faction );

            CoalitionStoredBudgetByTier[faction][tier] = valueToUse;
        }
        #endregion

        #region Budget/Max Strength Calculations
        private FInt PerSecondBudgetBeforeMultiplier => Difficulty.BudgetPerSecond_Base +
            (Difficulty.BudgetPerSecond_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
            (Difficulty.BudgetPerSecond_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600));
        public FInt GetPerSecondBudget( SphereFactionBaseInfo sphereInfo ) => PerSecondBudgetBeforeMultiplier * (sphereInfo?.BudgetMultiplierFromHacks ?? FInt.One);

        private FInt MaxStrengthBeforeMultiplier => Difficulty.MaxStrength_Base +
            (Difficulty.MaxStrength_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
            (Difficulty.MaxStrength_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600));

        public FInt GetMaxStrength( SphereFactionBaseInfo sphereInfo ) => MaxStrengthBeforeMultiplier * (sphereInfo?.MaxStrengthMultiplierFromHacks ?? FInt.One);
        #endregion

        #region Collection Shortcuts
        public int GetTotalCollectedForFactionOnPlanet( Faction faction, Planet planet )
        {
            if ( TotalCollectedFromDerelictPlanetByFaction.CheckIfAlreadyHasKeyDisplay( faction ) && TotalCollectedFromDerelictPlanetByFaction.Display[faction].ContainsKey( planet ) )
                return TotalCollectedFromDerelictPlanetByFaction.Display[faction][planet];
            else
                return 0;
        }

        public void ResetTotalCollectedForFactionOnPlanet( Faction faction, Planet planet )
        {
            if ( TotalCollectedFromDerelictPlanetByFaction.CheckIfAlreadyHasKeyDisplay( faction ) && TotalCollectedFromDerelictPlanetByFaction.Display[faction].ContainsKey( planet ) )
                TotalCollectedFromDerelictPlanetByFaction.Display[faction][planet] = 0;
        }

        public FInt GetControlForFactionOnPlanet( Faction faction, Planet planet )
        {
            if ( ControlByFactionOnDerelictPlanet.CheckIfAlreadyHasKeyDisplay( faction ) && ControlByFactionOnDerelictPlanet.Display[faction].ContainsKey( planet ) )
                return ControlByFactionOnDerelictPlanet.Display[faction][planet];
            else
                return FInt.Zero;
        }
        #endregion

        #region Enabled Reasons
        public enum CoalitionAwakenReason
        {
            NotAwakened,
            Time,
            DarkSpire,
            DarkAlliance
        }
        public CoalitionAwakenReason CurrentCoalitionAwakeningReason;
        #endregion

        public string GetCollectorTag( Faction faction )
        {
            SphereFactionBaseInfo info = faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( info == null )
                return string.Empty;
            else
                switch ( info.SphereType )
                {
                    case SphereFactionBaseInfo.SphereType_ChromaticSpire:
                    case SphereFactionBaseInfo.SphereType_ImperialSpire:
                        return Tag_CollectorChromaticSpire;
                    case SphereFactionBaseInfo.SphereType_GraySpire:
                    case SphereFactionBaseInfo.SphereType_DarkSpire:
                        return Tag_CollectorGraySpire;
                    case SphereFactionBaseInfo.SphereType_Zenith:
                        return Tag_CollectorZenith;
                    default:
                        return string.Empty;
                }
        }

        public GameEntityTypeData GetCoalitionUnitForFactionForTier( Faction faction, byte tier, ArcenHostOnlySimContext Context )
        {
            SphereFactionBaseInfo info = faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( info == null )
            {
                ArcenDebugging.ArcenDebugLog( $"Error! Failed to get SphereFactionBaseInfo for {faction.GetDisplayName()}.", Verbosity.ShowAsError );
                return null;
            }
            else
            {
                string baseTag;
                switch ( tier )
                {
                    case 0:
                        baseTag = info.GetUnitTagForTier( 0 );
                        break;
                    case 1:
                        baseTag = info.GetUnitTagForTier( 1 );
                        break;
                    default:
                        baseTag = info.GetUnitTagForTier( 2 );
                        break;
                }
                if ( CurrentCoalitionAwakeningReason == CoalitionAwakenReason.DarkAlliance )
                    return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Tag_CoalitionUnit_PrefixDarkAlliance + baseTag );
                else
                    return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Tag_CoalitionUnit_PrefixRegular + baseTag );
            }
        }

        public SplinteringSpireFactionBaseInfo() => Cleanup();

        protected override void Cleanup()
        {
            Instance = null;
            Intensity = 0;
            ActiveCoalitionFactions.Clear();

            UnclaimedVictoryPoints.Clear();
            TotalTimesWon.Clear();
            TotalCollectedFromDerelictPlanetByFaction.Clear();

            CoalitionStoredBudgetByTier.Clear();

            CollectorsAssignedByPlanet.Clear();

            InvalidPlanetsToBeRemoved.Clear();

            ControlByFactionOnDerelictPlanet.Clear();
            ControlOnDerelictPlanetByFaction_Helper.Clear();

            Derelicts.Clear();
            CollectorsToDecay.Clear();
            CoalitionSpawners.Clear();

            CoalitionStrengthByFactionIndex.Clear();

            CoalitionLateAwakeningSecond = 0;

            Teams.Clear();

            GameSecondControlWasEstablishedOnPlanet.Clear();

            SentAwakeningJournal = false;
            CurrentCoalitionAwakeningReason = CoalitionAwakenReason.NotAwakened;
        }

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            byte count, subcount;

            count = (byte)ActiveCoalitionFactions.Count;
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, count, "Active Coalition Factions" );

            foreach ( Faction workingFaction in ActiveCoalitionFactions.GetDisplayList() )
            {
                Buffer.AddFactionIndex_Neg1ToPos( MetaData, workingFaction.FactionIndex, "Active Coalition Faction Index" );

                if ( UnclaimedVictoryPoints.TryGetValue( workingFaction, out short victoryPoints ) )
                    Buffer.AddInt16( MetaData, ReadStyle.NonNeg, victoryPoints, "Unclaimed Victory Points" );
                else
                    Buffer.AddInt16( MetaData, ReadStyle.NonNeg, 0, "Unclaimed Victory Points" );

                if ( TotalTimesWon.TryGetValue( workingFaction, out short timesWon ) )
                    Buffer.AddInt16( MetaData, ReadStyle.NonNeg, timesWon, "Total Times Won" );
                else
                    Buffer.AddInt16( MetaData, ReadStyle.NonNeg, 0, "Total Times Won" );

                if ( TotalCollectedFromDerelictPlanetByFaction.CheckIfAlreadyHasKeyDisplay( workingFaction ) )
                    subcount = (byte)TotalCollectedFromDerelictPlanetByFaction.Display[workingFaction].Count;
                else
                    subcount = 0;

                Buffer.AddByte( MetaData, ReadStyleByte.Normal, subcount, "Total Collected From Derelict Planets Sub Dict Count" );

                foreach ( KeyValuePair<Planet, int> pair in TotalCollectedFromDerelictPlanetByFaction.Display[workingFaction] )
                {
                    Buffer.AddPlanetIndex_Neg1ToPos( MetaData, pair.Key.Index, "Collected From Derelict Planet Key" );
                    Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Value, "Collected From Derelict Planet Value" );
                }

                Buffer.AddFInt( MetaData, GetStoredBudgetForTierForFaction( workingFaction, 0 ), "Budget" );
                Buffer.AddFInt( MetaData, GetStoredBudgetForTierForFaction( workingFaction, 1 ), "Budget" );
                Buffer.AddFInt( MetaData, GetStoredBudgetForTierForFaction( workingFaction, 2 ), "Budget" );
            }

            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, Teams );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)GameSecondControlWasEstablishedOnPlanet.Count, "Control On Planets Count" );
            foreach ( KeyValuePair<Planet, int> pair in GameSecondControlWasEstablishedOnPlanet )
            {
                Buffer.AddPlanetIndex_Neg1ToPos( MetaData, pair.Key, "Control on Planets Key" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Value, "Control On Planets Value" );
            }

            Buffer.AddBool( MetaData, SentAwakeningJournal, "Sent Awakening Journal" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)CurrentCoalitionAwakeningReason, "Coalition Awakening Reason" );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Cleanup();
            byte count, subcount;
            count = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Active Coalition Factions" );
            for ( byte x = 0; x < count; x++ )
            {
                Faction workingFaction = World_AIW2.Instance.GetFactionByIndex( Buffer.ReadFactionIndex_Neg1ToPos( MetaData, "Active Coalition Faction Index" ) );
                UnclaimedVictoryPoints.TryAdd( workingFaction, Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "Unclaimed Victory Points" ) );
                TotalTimesWon.TryAdd( workingFaction, Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "Total Times Won" ) );
                subcount = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Total Collected From Derelict Planets Sub Dict Count" );
                TotalCollectedFromDerelictPlanetByFaction.ClearConstructionDictForStartingConstruction();
                for ( byte y = 0; y < subcount; y++ )
                    TotalCollectedFromDerelictPlanetByFaction.SetToInnerDictConstruction( workingFaction, Buffer.ReadPlanetFromIndex_Neg1ToPos( MetaData, "Collected From Derelict Planet Key" ), Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Collected From Derelict Planet Value" ) );
                TotalCollectedFromDerelictPlanetByFaction.SwitchConstructionToDisplay();

                OverrideStoredBudgetForTierForFaction( workingFaction, 0, Buffer.ReadFInt( MetaData, "Budget" ) );
                OverrideStoredBudgetForTierForFaction( workingFaction, 1, Buffer.ReadFInt( MetaData, "Budget" ) );
                OverrideStoredBudgetForTierForFaction( workingFaction, 2, Buffer.ReadFInt( MetaData, "Budget" ) );
            }

            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, Teams, "SplinteringSpire" );

            short sCount = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "Control on Planets Count" );
            for ( short x = 0; x < sCount; x++ )
                GameSecondControlWasEstablishedOnPlanet.TryAdd( Buffer.ReadPlanetFromIndex_Neg1ToPos( MetaData, "Control On Planets Key" ), Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Control On Planets Value" ) );

            SentAwakeningJournal = Buffer.ReadBool( MetaData, "Sent Awakening Journal" );

            CurrentCoalitionAwakeningReason = (CoalitionAwakenReason)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Coalition Awakening Reason" );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant() => Intensity;

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }

        protected override void DoRefreshFromFactionSettings()
        {
            Intensity = AttachedFaction.Config.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );

            if ( Difficulty == null )
                Difficulty = SplinteringSpireDifficultyTable.Instance.GetRowForFaction( AttachedFaction );

            CoalitionLateAwakeningSecond = AttachedFaction.Config.GetIntValueForCustomFieldOrDefaultValue( FactionField_CoalitionActivationTime, true ) * 60;
        }

        public override void SetStartingFactionRelationships()
        {
            foreach ( Faction otherFaction in World_AIW2.Instance.Factions )
            {
                if ( AttachedFaction == otherFaction )
                    continue;

                if ( otherFaction.Type == FactionType.Player )
                {
                    // Be friendly to players, so they can see us.
                    AttachedFaction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( AttachedFaction );
                }
                else if ( otherFaction.BaseInfo.Source.InternalName == "DarkSpireFactionBaseInfo" || otherFaction.BaseInfo.Allegiance == "Dark Alliance" )
                {
                    // Fight what we're designed to fight.
                    AttachedFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( AttachedFaction );
                }
                else
                {
                    // Ignore and be ignored by everything else.
                    AttachedFaction.MakeNeutralTo( otherFaction );
                    otherFaction.MakeNeutralTo( AttachedFaction );
                }
            }
        }

        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            return Target.TypeData.GetHasTag( "VengeanceGeneratorVulnerable" ) || Target.TypeData.GetHasTag( "VengeanceGeneratorConquestSpawn" ) || Target.TypeData.GetHasTag( "VengeanceGeneratorLocus" );
        }

        #region Notifier
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;

            NotifierFillData fillData; ;

            #region Collectors
            if ( Derelicts.GetDisplayList().Count > 0 )
            {
                fillData = NotifierFillData.GetFromPoolOrCreate();
                try
                {
                    int shortestRemainingDuration = 99999999;
                    int derelictCount = 0;

                    foreach ( GameEntity_Squad derelict in Derelicts.DisplaySquads() )
                    {
                        Planet derelictPlanet = derelict.Planet;
                        if ( derelictPlanet == null )
                            continue;

                        if ( derelictPlanet.IntelLevel == PlanetIntelLevel.Unexplored )
                            continue;

                        derelictCount++;
                        int timeRemaining = Difficulty.DurationOfDerelicts - derelict.GetSecondsSinceCreation();
                        if ( timeRemaining < shortestRemainingDuration )
                            shortestRemainingDuration = timeRemaining;

                        fillData.PlanetList.Add( derelictPlanet );

                        fillData.StringList.Add( $"黑暗尖塔残骸存在于 {derelictPlanet.Name}，将在 " +
                            $"{(timeRemaining / 60).ToString( "0" )}:" +
                            $"{(timeRemaining % 60).ToString( "00" )} 后完全腐烂\n" );

                        string collectedString = "已收集总数：";
                        bool started = false;

                        foreach ( Faction workingFaction in ActiveCoalitionFactions.GetDisplayList() )
                        {
                            if ( started )
                                collectedString += "; ";

                            collectedString += $"{workingFaction.StartFactionColourForLog()}{workingFaction.GetExternalBaseInfoAs<SphereFactionBaseInfo>().SphereType} Sphere</color>: {GetTotalCollectedForFactionOnPlanet( workingFaction, derelictPlanet )}";

                            started = true;
                        }

                        fillData.StringList.Add( collectedString + "\n\n" );
                    }

                    if ( fillData.StringList.Count > 0 )
                    {
                        fillData.Int16List.Add( (short)(derelictCount) );
                        fillData.Int16List.Add( (short)(shortestRemainingDuration / 60) );
                        fillData.Int16List.Add( (short)(shortestRemainingDuration % 60) );

                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( SplinteringSpireCollectorNotifier.Instance, fillData, "", 0, "Splintering Spire Collector Notifier", SortedNotificationPriorityLevel.Medium );
                    }
                    else
                        fillData.ReturnToPool();
                }
                catch ( System.Exception )
                {
                    fillData.ReturnToPool();
                }
            }
            #endregion

            #region Dismantling
            if ( GameSecondControlWasEstablishedOnPlanet.Count > 0 )
            {
                fillData = NotifierFillData.GetFromPoolOrCreate();
                try
                {
                    short lowestTimer = 9999;
                    string message = $"反黑暗尖塔联盟目前正在拆除一个黑暗尖塔 VG,位于 ";
                    if ( GameSecondControlWasEstablishedOnPlanet.Count == 1 )
                        message += "一个星球。";
                    else
                        message += "多个星球。";
                    message += $"在此之前需要维持一段时间的军事优势。\n\n";

                    fillData.StringList.Add( message );

                    foreach ( KeyValuePair<Planet, int> pair in GameSecondControlWasEstablishedOnPlanet )
                    {
                        short secondsLeft = (short)(Difficulty.SecondsToDismantleDarkSpireVG - (World_AIW2.Instance.GameSecond - pair.Value));

                        if ( secondsLeft < 0 || secondsLeft > Difficulty.SecondsToDismantleDarkSpireVG - 30 )
                            continue;

                        if ( secondsLeft < lowestTimer )
                            lowestTimer = secondsLeft;

                        fillData.StringList.Add( $"{pair.Key.Name} ({(secondsLeft / 60).ToString( "0" )}:{(secondsLeft % 60).ToString( "00" )})\n" );

                        fillData.PlanetList.Add( pair.Key );
                    }

                    if ( fillData.StringList.Count > 1 )
                    {
                        fillData.Int16List.Add( lowestTimer );

                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( SplinteringSpireDismantleNotifier.Instance, fillData, "", 0, "Splintering Spire Dismantling Notifier", SortedNotificationPriorityLevel.Medium );
                    }
                    else
                        fillData.ReturnToPool();
                }
                catch ( System.Exception )
                {
                    fillData.ReturnToPool();
                }
            }
            #endregion
        }
        #endregion

        #region Stage2
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            #region Coalition Factions
            ActiveCoalitionFactions.ClearConstructionListForStartingConstruction();
            foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
            {
                if ( !workingFaction.Config.GetBoolValueForCustomFieldOrDefaultValue( FactionField_CoalitionMember, false ) )
                    continue;

                ActiveCoalitionFactions.AddToConstructionList( workingFaction );
            }
            ActiveCoalitionFactions.SwitchConstructionToDisplay();
            #endregion

            CollectorsAssignedByPlanet.ClearConstructionDictForStartingConstruction();
            ControlByFactionOnDerelictPlanet.ClearConstructionDictForStartingConstruction();
            CollectorsToDecay.ClearConstructionListForStartingConstruction();

            #region Coalition Factions Processing
            foreach ( Faction workingFaction in ActiveCoalitionFactions.GetDisplayList() )
            {
                ControlOnDerelictPlanetByFaction_Helper.Clear();
                foreach ( GameEntity_Squad collector in workingFaction.Squads( Tag_Collector ) )
                {
                    Planet derelictPlanet = World_AIW2.Instance.GetEntityByID_Squad( collector.MinorFactionStackingID )?.Planet;

                    if ( derelictPlanet == null )
                    {
                        CollectorsToDecay.AddToConstructionList( collector );
                    }
                    else
                    {
                        if ( CollectorsAssignedByPlanet.ConstructionContainsKey( derelictPlanet ) )
                            CollectorsAssignedByPlanet.Construction[derelictPlanet]++;
                        else
                            CollectorsAssignedByPlanet.SetToConstructionDict( derelictPlanet, 1 );

                        if ( collector.Planet == derelictPlanet )
                        {
                            if ( ControlOnDerelictPlanetByFaction_Helper.GetHasKey( derelictPlanet ) )
                                ControlOnDerelictPlanetByFaction_Helper[derelictPlanet] += FInt.FromParts( 0, 040 );
                            else
                                ControlOnDerelictPlanetByFaction_Helper.Add( derelictPlanet, FInt.FromParts( 0, 040 ) );
                        }
                    }
                }

                foreach ( KeyValuePair<Planet, FInt> pair in ControlOnDerelictPlanetByFaction_Helper )
                {
                    ControlByFactionOnDerelictPlanet.SetToInnerDictConstruction( workingFaction, pair.Key, pair.Value );
                }

                int strength = 0;
                foreach ( GameEntity_Squad unit in workingFaction.Squads( Tag_CoalitionUnit ) )
                {
                    strength += unit.GetStrengthOfSelfAndContents();
                }
            }
            #endregion

            CollectorsAssignedByPlanet.SwitchConstructionToDisplay();
            ControlByFactionOnDerelictPlanet.SwitchConstructionToDisplay();
            CollectorsToDecay.SwitchConstructionToDisplay();

            #region Derelicts
            Derelicts.ClearConstructionListForStartingConstruction();
            foreach ( GameEntity_Squad derelict in AttachedFaction.Squads( Tag_Derelict ) )
            {
                Derelicts.AddToConstructionListIfNotAlreadyIn( derelict );
            }
            Derelicts.SwitchConstructionToDisplay();
            #endregion

            #region Coalition Strength
            CoalitionStrengthByFactionIndex.ClearConstructionDictForStartingConstruction();
            foreach ( GameEntity_Squad unit in AttachedFaction.Squads( Tag_CoalitionUnit ) )
            {
                if ( !CoalitionStrengthByFactionIndex.ConstructionContainsKey( unit.PlanetFaction.Faction.FactionIndex ) )
                    CoalitionStrengthByFactionIndex.SetToConstructionDict( unit.PlanetFaction.Faction.FactionIndex, unit.GetStrengthOfSelfAndContents() );
                else
                    CoalitionStrengthByFactionIndex.Construction[unit.PlanetFaction.Faction.FactionIndex] += unit.GetStrengthOfSelfAndContents();
            }
            CoalitionStrengthByFactionIndex.SwitchConstructionToDisplay();
            #endregion

            #region Coalition Spawners
            CoalitionSpawners.ClearConstructionListForStartingConstruction();
            foreach ( Faction workingFaction in ActiveCoalitionFactions.GetDisplayList() )
            {
                foreach ( GameEntity_Squad spawner in workingFaction.Squads( Tag_CoalitionSpawner ) )
                {
                    CoalitionSpawners.AddToConstructionList( spawner );
                }
            }
            CoalitionSpawners.SwitchConstructionToDisplay();
            #endregion
        }
        #endregion

        #region Stage3
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            #region VG Control
            foreach ( GameEntity_Squad VG in DarkSpireFactionBaseInfo.Instance.AttachedFaction.Squads( "VengeanceGeneratorNormalSpawn" ) )
            {
                bool fullAdvantage, smallAdvantage, hadAdvantageBefore;

                var strengthData = VG.Planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance;

                fullAdvantage = strengthData[FactionStance.Self].TotalStrength / 2 > strengthData[FactionStance.Hostile].TotalStrength;
                smallAdvantage = strengthData[FactionStance.Self].TotalStrength > strengthData[FactionStance.Hostile].TotalStrength / 2;
                hadAdvantageBefore = GameSecondControlWasEstablishedOnPlanet.ContainsKey( VG.Planet );

                if ( fullAdvantage && !hadAdvantageBefore ) // We're strong, start the advantage.
                {
                    GameSecondControlWasEstablishedOnPlanet.TryAdd( VG.Planet, World_AIW2.Instance.GameSecond );
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SplinteringSpire_TheDismantlingBegins", string.Empty, AttachedFaction, null, VG.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                else if ( !smallAdvantage && hadAdvantageBefore ) // We're weak, stop the advantage.
                    GameSecondControlWasEstablishedOnPlanet.TryRemove( VG.Planet, out int unused );
            }
            // Remove any invalid entries.
            InvalidPlanetsToBeRemoved.Clear();
            foreach ( KeyValuePair<Planet, int> pair in GameSecondControlWasEstablishedOnPlanet )
            {
                if ( pair.Key.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGeneratorNormalSpawn", true, true ) == null ||
                        World_AIW2.Instance.GameSecond - pair.Value > Difficulty.SecondsToDismantleDarkSpireVG + 10 )
                    InvalidPlanetsToBeRemoved.Add( pair.Key ); // VG killed by some other means, stop.
            }
            InvalidPlanetsToBeRemoved.ForEach( planet =>
            {
                GameSecondControlWasEstablishedOnPlanet.TryRemove( planet, 2 );
            } );
            #endregion
        }
        #endregion

        public override void DoPerSecondLogic_Stage4AdvancedAllegianceCode_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            // Make sure that Coaltion hate each other. They just tell their strikeforces not to in their stead. Politics are hard.
            foreach ( Faction primary in ActiveCoalitionFactions.GetDisplayList() )
            {
                foreach ( Faction secondary in ActiveCoalitionFactions.GetDisplayList() )
                {
                    if ( primary == secondary )
                        continue;

                    primary.MakeHostileTo( secondary );
                    secondary.MakeHostileTo( primary );
                }
            }

            // Make sure we're doing what we need to. We don't change.
            SetStartingFactionRelationships();
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            int load = 30 + Intensity * 3;

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Splintering Spire" );

            return load;
        }

        public override Fireteam GetFireteamById( int id ) => FireteamBaseUtility.GetFireteamById( Teams, id );
    }
}
