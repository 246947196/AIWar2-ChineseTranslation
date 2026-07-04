using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public struct ElderlingEssenceTier {
        public FInt EssenceToGrantOnDeath;
        public FInt EssenceToGrantOnDeath_PerMark;
    }
    public struct ElderlingEssenceConstants_Normal {
        public ElderlingEssenceTier LowestElderling, LowElderling, MedElderling, HighElderling;
    }
    public struct ElderlingEssenceConstants_Scarce {
        public ElderlingEssenceTier LowestElderling;
        public FInt EssenceToGrantOnDeath;
        public FInt EssenceToGrantOnDeath_PerInitialTier;
        public FInt EssenceToGrantOnDeath_PerLevel_Tier1;
        public FInt EssenceToGrantOnDeath_PerLevel_Tier2;
        public FInt EssenceToGrantOnDeath_PerLevel_Tier3;
        public FInt EssenceToGrantOnDeath_SuicideMultiplier;
    }

    public class ElderlingResourceGranter : INecromancerResourceGranter
    {
        private bool HaveLoadedData = false;

        ElderlingEssenceConstants_Normal Normal;
        ElderlingEssenceConstants_Scarce Scarce;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            Normal.LowestElderling.EssenceToGrantOnDeath = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_LowestElderling_EssenceToGrantOnDeath");
            Normal.LowestElderling.EssenceToGrantOnDeath_PerMark = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_LowestElderling_EssenceToGrantOnDeath_PerMark");
            Normal.LowElderling.EssenceToGrantOnDeath = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_LowElderling_EssenceToGrantOnDeath");
            Normal.LowElderling.EssenceToGrantOnDeath_PerMark = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_LowElderling_EssenceToGrantOnDeath_PerMark");
            Normal.MedElderling.EssenceToGrantOnDeath = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_MedElderling_EssenceToGrantOnDeath");
            Normal.MedElderling.EssenceToGrantOnDeath_PerMark = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_MedElderling_EssenceToGrantOnDeath_PerMark");
            Normal.HighElderling.EssenceToGrantOnDeath = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_HighElderling_EssenceToGrantOnDeath");
            Normal.HighElderling.EssenceToGrantOnDeath_PerMark = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_HighElderling_EssenceToGrantOnDeath_PerMark");
            Scarce.LowestElderling.EssenceToGrantOnDeath = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_LowestElderling_EssenceToGrantOnDeath");
            Scarce.LowestElderling.EssenceToGrantOnDeath_PerMark = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_LowestElderling_EssenceToGrantOnDeath_PerMark");
            Scarce.EssenceToGrantOnDeath = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_EssenceToGrantOnDeath");
            Scarce.EssenceToGrantOnDeath_PerInitialTier = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_EssenceToGrantOnDeath_PerInitialTier");
            Scarce.EssenceToGrantOnDeath_PerLevel_Tier1 = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_EssenceToGrantOnDeath_PerLevel_Tier1");
            Scarce.EssenceToGrantOnDeath_PerLevel_Tier2 = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_EssenceToGrantOnDeath_PerLevel_Tier2");
            Scarce.EssenceToGrantOnDeath_PerLevel_Tier3 = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_EssenceToGrantOnDeath_PerLevel_Tier3");
            Scarce.EssenceToGrantOnDeath_SuicideMultiplier = ExternalConstants.Instance.GetCustomFInt_Slow("custom_FInt_Scarce_EssenceToGrantOnDeath_SuicideMultiplier");
            HaveLoadedData = true;
        }

        public FInt GetResourceOneToGrantOnDeath(GameEntity_Squad relatedSquadOrNull) {
            if (relatedSquadOrNull == null) {
                return FInt.Zero;
            }
            if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("NecromancerScarceResources")) {
                return this.GetResourceOneToGrantOnDeath_Scarce(relatedSquadOrNull);
            } else {
                return this.GetResourceOneToGrantOnDeath_Normal(relatedSquadOrNull);
            }
        }

        public FInt GetResourceOneToGrantOnDeath_Tier(GameEntity_Squad entity, ElderlingEssenceTier tier) {
            return tier.EssenceToGrantOnDeath + tier.EssenceToGrantOnDeath_PerMark * (entity.CurrentMarkLevel - 1);
        }

        public FInt GetResourceOneToGrantOnDeath_Normal(GameEntity_Squad entity) {
            LoadCustomDataIfNeeded();
            if (entity.TypeData.GetHasTag("LowestElderling")) {
                return GetResourceOneToGrantOnDeath_Tier(entity, Normal.LowestElderling);
            } else if (entity.TypeData.GetHasTag("HighElderling")) {
                return GetResourceOneToGrantOnDeath_Tier(entity, Normal.HighElderling);
            } else if (entity.TypeData.GetHasTag("MedElderling")) {
                return GetResourceOneToGrantOnDeath_Tier(entity, Normal.MedElderling);
            } else if (entity.TypeData.GetHasTag("LowElderling")) {
                return GetResourceOneToGrantOnDeath_Tier(entity, Normal.LowElderling);
            }
            return FInt.Zero;
        }

        public FInt GetResourceOneToGrantOnDeath_Scarce(GameEntity_Squad entity) {
            LoadCustomDataIfNeeded();

            if (entity.TypeData.GetHasTag("LowestElderling")) {
                return GetResourceOneToGrantOnDeath_Tier(entity, Scarce.LowestElderling);
            }

            ElderlingsPerUnitBaseInfo unitInfo = entity.GetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
            if ( unitInfo != null ) {
                FInt resource = Scarce.EssenceToGrantOnDeath;
                int numberOfTimesLeveledUp = unitInfo.NumberOfTimesLeveledUp;

                // PerIntialTier bonues
                if (entity.TypeData.GetHasTag("HighElderling")) {
                    if (numberOfTimesLeveledUp < 7) {
                        resource += Scarce.EssenceToGrantOnDeath_PerInitialTier * 2;
                    } else if (numberOfTimesLeveledUp < 14) {
                        resource += Scarce.EssenceToGrantOnDeath_PerInitialTier;
                    }
                } else if (entity.TypeData.GetHasTag("MedElderling")) {
                    if (numberOfTimesLeveledUp < 7) {
                        resource += Scarce.EssenceToGrantOnDeath_PerInitialTier;
                    }
                }

                // PerLevel bonues, cummulative
                if (numberOfTimesLeveledUp > 13) {
                    resource += Scarce.EssenceToGrantOnDeath_PerLevel_Tier3 * (numberOfTimesLeveledUp - 13);
                    numberOfTimesLeveledUp = 13;
                }
                if (numberOfTimesLeveledUp > 6) {
                    resource += Scarce.EssenceToGrantOnDeath_PerLevel_Tier2 * (numberOfTimesLeveledUp - 7);
                    numberOfTimesLeveledUp = 6;
                }
                if (numberOfTimesLeveledUp > 0) {
                    resource += Scarce.EssenceToGrantOnDeath_PerLevel_Tier1 * numberOfTimesLeveledUp;
                }
                if (unitInfo.SuicideMode) {
                    resource *= Scarce.EssenceToGrantOnDeath_SuicideMultiplier;
                }
                return resource;
            }
            return FInt.Zero;
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() {
            HaveLoadedData = false; //trigger a reload of xml
        }
    }
}
