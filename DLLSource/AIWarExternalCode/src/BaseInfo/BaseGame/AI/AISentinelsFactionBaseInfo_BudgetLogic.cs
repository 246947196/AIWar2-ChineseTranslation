using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public partial class AISentinelsFactionBaseInfo
    {
        private AIBudgetCurrentConfiguration temporaryBudgetConfig = new AIBudgetCurrentConfiguration();

		public int GetSpecificBudgetThreshold( AIBudgetType BudgetType )
        {
            return GetSpecificBudgetThreshold( BudgetType, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
        }
		
        public int GetSpecificBudgetThreshold( AIBudgetType BudgetType, FInt Aip )
        {
            //include only decreases when figuring out when the "spend threshold" is.  That way if there's an increase it will cause it to spend more frequently rather than in larger bursts
            return (this.GetSpecificBudgetAIPurchaseCostGainPerSecond( BudgetType, false, true, Aip ) * this.GetSpecificBudgetSpendingInterval( BudgetType, Aip )).GetNearestIntPreferringHigher();
        }

        public FInt GetOverallAIPurchaseCostGainPerSecond( bool IncludeMultipliersFromMultipleFactionsEtc, FInt Aip )
        {
            //Please see https://docs.google.com/spreadsheets/d/1oSsxmKXDxP28Hc-ZYXngCnqnu3mc0vXipGEdj3HqKUo/edit#gid=331947214 for details.
            //If you want to see the curves and make changes to them, then that's the spreadsheet to base that work off of.

            //Chris notes: We are using floats for precision here, and then convert to FInt when we are done.  Otherwise the math is likely to go very bad.
            //             This really only happens on the host anyhow, so floats are fine.

            float currentAIP = Aip.ToFloatNonSim();
            
            float mainFormula = this.SentinelInfo.AIDifficulty.Budget_BaseIncome +
                ((Math.Min( 50f, currentAIP - 10 )) * this.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForAIPOver10) +
                (Math.Max( 0f, Math.Min( currentAIP - 50, 100f ) ) * this.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForAIPOver50) +
                (Math.Max( 0f, Math.Min( currentAIP - 100, 200f ) ) * this.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForAIPOver100) +
                (Math.Max( 0f, Math.Min( currentAIP - 200, 400f ) ) * this.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForAIPOver200) +
                (Math.Max( 0f, Math.Min( currentAIP - 400, 600f ) ) * this.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForAIPOver400) +
                (Math.Max( 0f, currentAIP - 600f ) * this.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForAIPOver600);

            if ( IncludeMultipliersFromMultipleFactionsEtc )
            {
                // this is the method that calculates ai budget multiplier
                // based on number of player and ai factions

                int humanEmpireCount = World_AIW2.Instance.EmpireStylePlayerFactions.Count;
                if ( humanEmpireCount > 1 )
                {
                    if ( humanEmpireCount >= 4 )
                        mainFormula *= 2.4f;
                    else if ( humanEmpireCount >= 3 )
                        mainFormula *= 1.8f;
                    else
                        mainFormula *= 1.33f;
                }

                //note that this counts the current faction
                int numAIFactionsFriendlyToMe = 0;
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                {
                    Faction aiFaction = World_AIW2.Instance.AIFactions[i];
                    if ( aiFaction.GetIsHostileTowards( AttachedFaction ) )
                        continue;
                    if ( aiFaction.FactionIsDefeated )
                        continue;
                    numAIFactionsFriendlyToMe++;
                }
                if ( numAIFactionsFriendlyToMe == 0 && !AttachedFaction.FactionIsDefeated )
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: somehow this ai faction has no allies, not even including itself", Verbosity.DoNotShow );

                //Note that for the civil war case, all AIs get full income.
                if ( numAIFactionsFriendlyToMe > 1 )
                {
                    if ( numAIFactionsFriendlyToMe >= 4 )
                        mainFormula *= 0.7f;
                    else if ( numAIFactionsFriendlyToMe >= 3 )
                        mainFormula *= 0.8f;
                    else
                        mainFormula *= 0.9f;
                }
            }

            FInt result = FInt.CreateFromDoubleNonSim( mainFormula ); //this is fine for the sim, actually!

            return result;
        }

        public FInt GetSpecificBudgetAIPurchaseCostGainPerSecond( AIBudgetType BudgetType, bool IncludeIncreases, bool IncludeDecreases, FInt Aip )
        {
            FInt baseBudgetToGain = this.GetOverallAIPurchaseCostGainPerSecond( true, Aip );

            //this is based on the AI personality and balance levers
            FInt multiplierForBudgetType = FInt.One;

            if ( this != null && this.SentinelInfo.AIType != null )
            {
                switch ( BudgetType )
                {
                    case AIBudgetType.Reinforcement:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_Reinforcement;
                        break;
                    case AIBudgetType.Wave:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_Wave;
                        break;
                    case AIBudgetType.CPA:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_CPA;
                        break;
                    case AIBudgetType.Warden:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_WardenFleet;
                        break;
                    case AIBudgetType.Reconquest:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_Reconquest;
                        break;
                    case AIBudgetType.HunterFleet:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_HunterFleet;
                        break;
                    case AIBudgetType.PraetorianGuard:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_PraetorianGuard;
                        break;
                    case AIBudgetType.WormholeInvasion:
                        multiplierForBudgetType = this.SentinelInfo.AIType.MultiplierForBudget_WormholeInvasion;
                        break;
                }

                if ( multiplierForBudgetType != FInt.One )
                {
                    if ( IncludeIncreases && multiplierForBudgetType > FInt.One )
                        baseBudgetToGain *= multiplierForBudgetType;
                    if ( IncludeDecreases && multiplierForBudgetType < FInt.One )
                        baseBudgetToGain *= multiplierForBudgetType;
                }

                switch ( BudgetType )
                {
                    case AIBudgetType.Wave:
                        if ( IncludeIncreases )
                            baseBudgetToGain *= this.SentinelInfo.AIDifficulty.WaveBudgetMultiplier;
                        break;
                }
            }

            AIBudgetCurrentConfiguration budgetConfig = AttachedFaction.TryGetAISentinelsCoreData().SentinelInfo.CurrentBudgetConfiguration;
            //if (Aip != GlobalAIWorldBaseInfo.Instance.AIProgress_Effective)
            {
                budgetConfig = temporaryBudgetConfig;
                SentinelInfo.AIType.Implementation.SetSpendingRatios( AttachedFaction, temporaryBudgetConfig, Aip );
            }

            baseBudgetToGain *= budgetConfig[BudgetType].BudgetPortion;

            return baseBudgetToGain;
        }

        public int GetSpecificBudgetSpendingInterval( AIBudgetType BudgetType, FInt Aip )
        {
            AIBudgetItem budgetItem = this.SentinelInfo.AIType.BudgetItems[BudgetType];

            int result = 0;

            if ( budgetItem.SpendOnThreshold )
            {
                result = budgetItem.SecondsBetweenAttemptsToSpend;

                AIBudgetCurrentConfiguration config = this.SentinelInfo.CurrentBudgetConfiguration;
                if (Aip != GlobalAIWorldBaseInfo.Instance.AIProgress_Effective)
                {
                    config = temporaryBudgetConfig;
                    this.SentinelInfo.AIType.Implementation.SetSpendingRatios(AttachedFaction, config, Aip);
                }
                result = (result * config[BudgetType].IntervalMultiplier).IntValue;
                return result;
            }

            // not spending on threshold, but on a specific time interval defined for this budget type
            //

            if ( budgetItem.DoubleWaveIntervalEachTime )
            {
                result = this.SentinelInfo.PreviousWaveLength;
            }
            else
            {
                result = budgetItem.SecondsBetweenAttemptsToSpend;
            }

            return result;
        }

        public FInt GetApproxStrengthCanPurchase( FInt BudgetAmount, FInt Aip )
        {
            var basedOnType = GameEntityTypeDataTable.Instance.GetRowByName("PikeGuardian");

            var aipformark = this.AttachedFaction.GetAISentinelsCoreData().SentinelInfo.AIDifficulty.AIPForMarkLevel;
            
            int atMarkLevel = 0;
            for (int i = 0; i < aipformark.Count; i++)
            {
                if (aipformark[i] <= Aip)
                    atMarkLevel = i;
            }

            var strPerCost = basedOnType.AIValue(atMarkLevel);

            return (BudgetAmount * strPerCost);
        }

        public FInt GetApproxStrengthCanPurchase( int BudgetAmount, FInt Aip )
        {
            var basedOnType = GameEntityTypeDataTable.Instance.GetRowByName("PikeGuardian");

            var aipformark = this.AttachedFaction.GetAISentinelsCoreData().SentinelInfo.AIDifficulty.AIPForMarkLevel;
            
            int atMarkLevel = 0;
            for (int i = 0; i < aipformark.Count; i++)
            {
                if (aipformark[i] <= Aip)
                    atMarkLevel = i;
            }

            var strPerCost = basedOnType.AIValue(atMarkLevel);

            return ((FInt)BudgetAmount * strPerCost);
        }
    }
}
