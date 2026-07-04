using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class RiskAnalyzerFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public RiskAnalyzerFactionBaseInfo BaseInfo;
        public static RiskAnalyzerFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<RiskAnalyzerFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
        }
                
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            int numToSeed = AttachedFaction.CustomData_NumberToSeed( true );
            if ( numToSeed <= 0 )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "SeedStartingEntities_LaterEverythingElse: RiskAnalyzers were asked to seed, but then told to seed zero of themselves?" );
                return;
            }
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, "RiskAnalyzer", SeedingType.HardcodedCount, numToSeed, 
                MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 2, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
        }

        
        
        //okay because only used in the method below
        private readonly List<SafeSquadWrapper> playerAnalyzers = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "RiskAnalyzerFactionDeepInfo-playerAnalyzers" );
        private void updateExoStatusForRiskAnalyzersOnly(Faction faction, ArcenHostOnlySimContext Context )
        {
            int numAIAnalyzer = 0;
            int numNeutralAnalyzer = 0;
            int numHumanAnalyzer = 0;
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.RiskAnalyzer );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "RiskAn-updateExoStatusForRiskAnalyzersOnly-trace", 10f ) : null;
            #endregion
            if(World_AIW2.Instance.GameSecond < this.BaseInfo.MinutesInToStartChargingExo * 60)
            {
//                ArcenDebugging.ArcenDebugLogSingleLine("Current time: " + World_AIW2.Instance.GameSecond + " not allowed to charge exo until " + (this.MinutesInToStartChargingExo * 60), Verbosity.DoNotShow );
                return;
            }
            playerAnalyzers.Clear();
            foreach ( GameEntity_Squad entity in faction.Squads( RiskAnalyzerFactionBaseInfo.ANALYZER_TAG ) )
            {
                if(entity.Planet.GetControllingFactionType() == FactionType.Player)
                {
                    playerAnalyzers.Add(entity);
                    numHumanAnalyzer++;
                }
                else if(entity.Planet.GetControllingFactionType() == FactionType.AI)
                    numAIAnalyzer++;
                else
                    numNeutralAnalyzer++;
            }

            if(this.BaseInfo.exoData.StrengthRequiredForNextExo == 0)
            {
                this.BaseInfo.exoData.StrengthRequiredForNextExo = this.BaseInfo.BaseExoWaveStrength;
                this.BaseInfo.exoData.CurrentExoStrength = FInt.Zero;
                this.BaseInfo.exoData.NumExosSoFar = 0;
                this.BaseInfo.exoData.PercentToStartWarning = this.BaseInfo.ExoPercentForWarning;
                this.BaseInfo.exoData.FactionIndexOfOriginFaction = faction.FactionIndex;
                this.BaseInfo.exoData.FactionIndexOfExoSpawnFaction = World_AIW2.GetRandomAIFaction(Context).FactionIndex;
            }
            if((World_AIW2.Instance.GameSecond % 60) == 0)
            {
                this.BaseInfo.exoData.SyncExoToCPAIfAllowed( faction, Context );
                FInt exoStrengthIncrease = FInt.Zero;
                Int16 numAsIntensity = (Int16)faction.CustomData_NumberToSeed( true );
                if(numAsIntensity >= this.BaseInfo.HighExoIntensity )
                    exoStrengthIncrease = this.BaseInfo.ExoIncomePerAIRiskAnalyzerHigh * numAIAnalyzer + 
                        this.BaseInfo.ExoIncomePerPlayerRiskAnalyzerHigh * numHumanAnalyzer + 
                        this.BaseInfo.ExoIncomePerNeutralRiskAnalyzerHigh * numNeutralAnalyzer;
                else
                    exoStrengthIncrease = this.BaseInfo.ExoIncomePerAIRiskAnalyzer * numAIAnalyzer + 
                        this.BaseInfo.ExoIncomePerPlayerRiskAnalyzer * numHumanAnalyzer + 
                        this.BaseInfo.ExoIncomePerNeutralRiskAnalyzer * numNeutralAnalyzer;
                this.BaseInfo.exoData.UpdateExoStrength( exoStrengthIncrease );
                if ( tracing )
                {
                    FInt estimatedTimeForExo = exoStrengthIncrease <= 0 ? FInt.Zero : (( this.BaseInfo.exoData.StrengthRequiredForNextExo - this.BaseInfo.exoData.CurrentExoStrength) / exoStrengthIncrease );
                    tracingBuffer.Add( "Risk Analyzer: Exo wave strength update at " + World_AIW2.Instance.GameSecond + ": Current exo wave strength: " + this.BaseInfo.exoData.CurrentExoStrength + " increase this second: " + exoStrengthIncrease + " Strength required: " + BaseInfo.exoData.StrengthRequiredForNextExo + " increase breakdown: AI analyzer: " + this.BaseInfo.ExoIncomePerAIRiskAnalyzer + " * " + numAIAnalyzer + " neutral: " + this.BaseInfo.ExoIncomePerNeutralRiskAnalyzer + " * " + numNeutralAnalyzer + " player: " + this.BaseInfo.ExoIncomePerPlayerRiskAnalyzer + " * " + numHumanAnalyzer + ". Estimated Minutes till next wave: " + estimatedTimeForExo );
                }
            }

            //FInt exoChargePercent =  (this.BaseInfo.exoData.CurrentExoStrength * 100) / this.BaseInfo.exoData.StrengthRequiredForNextExo ;


            if( this.BaseInfo.exoData.ShouldLaunchExo() )
            {
                this.BaseInfo.exoData.ResetSync();

                //Send an Exo. Prefer to target player risk analyzers, but if they don't have any then go for the home command station
                List<SafeSquadWrapper> targets = playerAnalyzers;
                if(targets.Count == 0)
                    ExoGalacticAttackManager.GetAllHumanHomeCommandStations( targets );
                if(targets.Count == 0)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Could not find suitable target for Risk Analyzer exo strike", Verbosity.DoNotShow );
                    return;
                }
                //We can either attack all the risk analyzers or just one
                if(Context.RandomToUse.Next(0, 10) % 2 == 0)
                {
                    //attack a single randomly chosen risk analyzer
                    GameEntity_Squad target = targets[Context.RandomToUse.Next(0, targets.Count)].GetSquad();
                    if ( target == null )
                        return;
                    ExoOptions options = ExoOptions.CreateWithDefaults(target, this.BaseInfo.exoData.CurrentExoStrength.IntValue, World_AIW2.Instance.GetFactionByIndex(this.BaseInfo.exoData.FactionIndexOfExoSpawnFaction), faction);
                    options.type = ExoGalacticAttackType.Reconquest;
                    ExoGalacticAttackManager.SendExoGalacticAttack(options, Context);
                    
//                    ExoGalacticAttackManager.SendExoGalacticAttack(target, this.BaseInfo.exoData.CurrentExoStrength.IntValue, null, World_AIW2.Instance.GetFactionByIndex(this.BaseInfo.exoData.FactionIndexOfExoSpawnFaction), Context, ExoGalacticAttackType.Reconquest); //not sure why there were 2 of those
                }
                else
                {
                    //spread the attack across all player-owned Risk Analyzers
                    ExoOptions options = ExoOptions.CreateWithDefaults(targets, this.BaseInfo.exoData.CurrentExoStrength.IntValue,World_AIW2.Instance.GetFactionByIndex(this.BaseInfo.exoData.FactionIndexOfExoSpawnFaction), faction );
                    options.type = ExoGalacticAttackType.Reconquest;
                    ExoGalacticAttackManager.SendExoGalacticAttack(options, Context);
                }
                //update the numbers
                this.BaseInfo.exoData.CurrentExoStrength = FInt.Zero;
                this.BaseInfo.exoData.NumExosSoFar++;
                this.BaseInfo.exoData.StrengthRequiredForNextExo = BaseInfo.BaseExoWaveStrength + this.BaseInfo.exoData.NumExosSoFar * BaseInfo.NextExoMultiplier;
                this.BaseInfo.exoData.FactionIndexOfExoSpawnFaction = World_AIW2.GetRandomAIFaction(Context).FactionIndex;
                if(this.BaseInfo.exoData.StrengthRequiredForNextExo >= BaseInfo.MaxExoWaveStrength )
                    this.BaseInfo.exoData.StrengthRequiredForNextExo = BaseInfo.MaxExoWaveStrength;
            }

        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            //bool localDebug = false;
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.RiskAnalyzer );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "RiskAn-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            #endregion

            if(World_AIW2.Instance.GameSecond <= 1)
                return;

            if(this.BaseInfo.ExoIncomePerPlayerRiskAnalyzer == 0)
            {
                //Check this one variable to make sure the XML loaded correctly, for paranoia
                ArcenDebugging.ArcenDebugLogSingleLine("Bug: ExoIncomePerPlayherRiskAnalyzer XML not set", Verbosity.DoNotShow );
            }
            updateExoStatusForRiskAnalyzersOnly(AttachedFaction, Context);

            if ( BaseInfo.timeIntervalForAnalyzer == 0)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Bug: time interval " + BaseInfo.timeIntervalForAnalyzer + " increase " + BaseInfo.AIPIncrease + " on death " + BaseInfo.AIPIncreaseOnDeath + " decrease " + BaseInfo.AIPDecrease + ". Something is wrong with the XML, since the risk analyzers will never fire.", Verbosity.DoNotShow );
                return;
            }
            int nextFiringTime = AttachedFaction.GetExternalBaseInfoAs<RiskAnalyzerFactionBaseInfo>().TimeForNextFiring;
            if( nextFiringTime == -1)
            {
                nextFiringTime = World_AIW2.Instance.GameSecond + BaseInfo.timeIntervalForAnalyzer;
                AttachedFaction.GetExternalBaseInfoAs<RiskAnalyzerFactionBaseInfo>().TimeForNextFiring = nextFiringTime;
                return;
            }
            if(World_AIW2.Instance.GameSecond >= nextFiringTime )
            {
                if(tracing)
                    tracingBuffer.Add("Updating risk analyzer effects at " + World_AIW2.Instance.GameSecond);
                int netAIP = BaseInfo.GetNetAIPChangeForThisFiring();
                int increaseOnly = BaseInfo.GetAIPIncreaseOnlyForThisFiring();
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)netAIP, AIPChangeReason.RiskAnalyzer, null, AttachedFaction.FactionIndex, -1, -1 );
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "Net AI Progress from <color=#" + AttachedFaction.FactionCenterColor.ColorHexBrighter + ">Risk Analyzers</color>: " + netAIP, 
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_RiskAnalyzersActivate", null );
                //we need to check for our allegiances because things like the Dyson or
                //Nanocaust can change allegiance
                AllegianceHelper.AllyThisFactionToEveryoneButPlayers(AttachedFaction);
                updateRiskAnalyzerTotals(AttachedFaction, increaseOnly, netAIP);
                BaseInfo.TimeForNextFiring = World_AIW2.Instance.GameSecond + BaseInfo.timeIntervalForAnalyzer;
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        private void updateRiskAnalyzerTotals(Faction faction, int totalIncrease, int netIncrease)
        {
            bool debug = false;
            Int16 prevTotalIncrease = BaseInfo.TotalAIPIncrease;
            Int16 prevNetIncrease = BaseInfo.NetAIPIncrease;
            Int16 newTotal = (Int16)(prevTotalIncrease + totalIncrease);
            Int16 newNet = (Int16)(prevNetIncrease + netIncrease);
            BaseInfo.NetAIPIncrease = newNet;
            BaseInfo.TotalAIPIncrease = newTotal;
            if(debug)
                ArcenDebugging.ArcenDebugLogSingleLine("netIncrease: " + prevNetIncrease + " --> " + newNet + " totalIncrease: " + prevTotalIncrease + " --> " + newTotal, Verbosity.DoNotShow );
        }
        public override void DoOnFirstDeathLogic_OnlyAferFullStackDeath_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.GetHasTag( RiskAnalyzerFactionBaseInfo.ANALYZER_TAG ) )
            {
                int AIPIncreaseOnDeath = BaseInfo.AIPIncreaseOnDeath;
                Int16 factionIndex = -1;
                if ( FiringSystemOrNull != null )
                    factionIndex = FiringSystemOrNull.ParentEntity.GetFactionIndex_Safe();
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)AIPIncreaseOnDeath, AIPChangeReason.EntityDeath, entity.TypeData, factionIndex, entity.Planet.Index, entity.GetFactionIndex_Safe() );
            }
        }
    }
}
