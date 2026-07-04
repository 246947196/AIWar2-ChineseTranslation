using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// No need to separate out anything for necromancer or eldering or whatever.
    /// Any given DLC or mod should really just have one of these sorts of extensions, period, to maximize efficiency.
    /// </summary>
    public class DLC3GameEntityTypeDataExtension : ExternalGameEntityTypeDataExtension
    {
        //The necromancer has distinct resources, so give them their own equivalent "grant resources on death"
        //NOTA BENE: Don't give the same unit type regular metal/science/hacking on death and the necromancer equivalent as well
        public int NecromancerScienceToGrantOnDeath;
        public int NecromancerHackingToGrantOnDeath;
        public FInt NecromancerResourceOneToGrantOnDeath;
        public FInt NecromancerResourceOneToGrantOnDeathPerMark;

        public FInt DysonSidekickResourceOneToGrantOnDeath;
        public FInt DysonSidekickResourceOneToGrantOnDeathPerMark;

        public int DarkZenithSidekickScienceToGrantOnDeath;
        public int DarkZenithSidekickHackingToGrantOnDeath;

        public DZResource DarkZenithSidekickResourceToGrant;
        public int DarkZenithSidekickResourceAmountToGrant;

        public int SpireSidekickScienceToGrantOnDeath;
        public int SpireSidekickHackingToGrantOnDeath;

        public int ScourgeEmpireCorbomiteToGrantOnDeath;

        public int ArmadaScienceToGrantOnDeath;
        public int ArmadaHackingToGrantOnDeath;
        public int ArmadaMetalToGrantOnDeath;
        public int ArmadaResourceOneToGrantOnDeath;
        public FInt ArmadaResourceOneToGrantOnDeathPerMark;

        public int ApkalluScienceToGrantOnDeath;
        public int ApkalluHackingToGrantOnDeath;
        public int ApkalluMetalToGrantOnDeath;
        public int ApkalluResourceOneToGrantOnDeath;
        public FInt ApkalluResourceOneToGrantOnDeathPerMark;
        public int ApkalluResourceTwoToGrantOnDeath;
        public FInt ApkalluResourceTwoToGrantOnDeathPerMark;
        public int ApkalluResourceThreeToGrantOnDeath;
        public FInt ApkalluResourceThreeToGrantOnDeathPerMark;

        
        [NotForDumping]
        public INecromancerResourceGranter NecromancerResourceGranter = null;
        [NotForDumping]
        public readonly ArcenExternalDllTypeInfoAndCache CachedTypeForNecromancerResourceGranter = new ArcenExternalDllTypeInfoAndCache();

        //for necromancer
        public string SkeletonTag = string.Empty;
        public int SkeletonTypePercent = 0;
        public int BonusSkeletonPercent = 0;
        public int SkeletonCapIncrease = 0;
        public int SkeletonCapIncreasePerMark = 0;

        public string WightTag = string.Empty;
        public int WightTypePercent = 0;
        public int BonusWightPercent = 0;
        public int WightCapIncrease = 0;
        public int WightCapIncreasePerMark = 0;

        public string MummyTag = string.Empty;
        public int MummyTypePercent = 0;
        public int BonusMummyPercent = 0;
        public int MummyCapIncrease = 0;
        public int MummyCapIncreasePerMark = 0;

        private string NecromancerUpgradeToGrantOnDeathName = string.Empty;
        public NecromancerUpgrade NecromancerUpgradeToGrantOnDeath = null;

        public bool ImmuneToNecromancy = false;

        //For necromancer-related Elderling hacks
        public string NameForElderlingTransformation = string.Empty;

        private static ArcenTypeAnalyzer<DLC3GameEntityTypeDataExtension> typeAnalyzer;
        public override void WipeForReuseAsNewObject() //happens when xml is reloaded, but that's it.
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<DLC3GameEntityTypeDataExtension>( new DLC3GameEntityTypeDataExtension() );
            typeAnalyzer.ApplyDefaults( this );
        }

        public override void NodeProcessor( ArcenXMLElement Data )
        {
            Data.Fill( "necromancer_science_to_grant_on_death", ref this.NecromancerScienceToGrantOnDeath, false );
            Data.Fill( "necromancer_hacking_to_grant_on_death", ref this.NecromancerHackingToGrantOnDeath, false );
            Data.Fill( "necromancer_resource_one_to_grant_on_death", ref this.NecromancerResourceOneToGrantOnDeath, false );
            Data.Fill( "necromancer_resource_one_to_grant_on_death_per_mark", ref this.NecromancerResourceOneToGrantOnDeathPerMark, false );

            Data.Fill( "dyson_sidekick_resource_one_to_grant_on_death", ref this.DysonSidekickResourceOneToGrantOnDeath, false );
            Data.Fill( "dyson_sidekick_resource_one_to_grant_on_death_per_mark", ref this.DysonSidekickResourceOneToGrantOnDeathPerMark, false );

            Data.Fill( "dark_zenith_sidekick_science_to_grant_on_death", ref this.DarkZenithSidekickScienceToGrantOnDeath, false );
            Data.Fill( "dark_zenith_sidekick_hacking_to_grant_on_death", ref this.DarkZenithSidekickHackingToGrantOnDeath, false );
            string tempString = "";
            Data.Fill( "dark_zenith_sidekick_resource_to_grant", ref tempString, false );
            Data.Fill( "dark_zenith_sidekick_resource_amount_to_grant", ref this.DarkZenithSidekickResourceAmountToGrant, false );
            this.DarkZenithSidekickResourceToGrant = DarkZenithFactionBaseInfoRoot.GetDZResourceFromString( tempString );

            Data.Fill( "spire_sidekick_science_to_grant_on_death", ref this.SpireSidekickScienceToGrantOnDeath, false );
            Data.Fill( "spire_sidekick_hacking_to_grant_on_death", ref this.SpireSidekickHackingToGrantOnDeath, false );

            Data.Fill( "scourge_empire_corbomite_to_grant_on_death", ref this.ScourgeEmpireCorbomiteToGrantOnDeath, false );

            Data.Fill( "armada_metal_to_grant_on_death", ref this.ArmadaMetalToGrantOnDeath, false );
            Data.Fill( "armada_hacking_to_grant_on_death", ref this.ArmadaHackingToGrantOnDeath, false );
            Data.Fill( "armada_science_to_grant_on_death", ref this.ArmadaScienceToGrantOnDeath, false );
            Data.Fill( "armada_resource_one_to_grant_on_death", ref this.ArmadaResourceOneToGrantOnDeath, false );
            Data.Fill( "armada_resource_one_to_grant_on_death_per_mark", ref this.ArmadaResourceOneToGrantOnDeathPerMark, false );

            Data.Fill( "apkallu_metal_to_grant_on_death", ref this.ApkalluMetalToGrantOnDeath, false );
            Data.Fill( "apkallu_hacking_to_grant_on_death", ref this.ApkalluHackingToGrantOnDeath, false );
            Data.Fill( "apkallu_science_to_grant_on_death", ref this.ApkalluScienceToGrantOnDeath, false );
            Data.Fill( "apkallu_resource_one_to_grant_on_death", ref this.ApkalluResourceOneToGrantOnDeath, false );
            Data.Fill( "apkallu_resource_one_to_grant_on_death_per_mark", ref this.ApkalluResourceOneToGrantOnDeathPerMark, false );
            Data.Fill( "apkallu_resource_two_to_grant_on_death", ref this.ApkalluResourceTwoToGrantOnDeath, false );
            Data.Fill( "apkallu_resource_two_to_grant_on_death_per_mark", ref this.ApkalluResourceTwoToGrantOnDeathPerMark, false );
            Data.Fill( "apkallu_resource_three_to_grant_on_death", ref this.ApkalluResourceThreeToGrantOnDeath, false );
            Data.Fill( "apkallu_resource_three_to_grant_on_death_per_mark", ref this.ApkalluResourceThreeToGrantOnDeathPerMark, false );

            Data.Fill( "necromancer_resource_granter_dll", ref this.CachedTypeForNecromancerResourceGranter.DllName, false );
            Data.Fill( "necromancer_resource_granter_type", ref this.CachedTypeForNecromancerResourceGranter.TypeName, false );

            Data.Fill( "skeleton_tag", ref this.SkeletonTag, false );
            Data.Fill( "skeleton_type_percent", ref this.SkeletonTypePercent, false );
            Data.Fill( "bonus_skeleton_percent", ref this.BonusSkeletonPercent, false );
            Data.Fill( "skeleton_cap_increase", ref this.SkeletonCapIncrease, false );
            Data.Fill( "skeleton_cap_increase_per_mark", ref this.SkeletonCapIncreasePerMark, false );
            Data.Fill( "wight_tag", ref this.WightTag, false );
            Data.Fill( "wight_type_percent", ref this.WightTypePercent, false );
            Data.Fill( "bonus_wight_percent", ref this.BonusWightPercent, false );
            Data.Fill( "wight_cap_increase", ref this.WightCapIncrease, false );
            Data.Fill( "wight_cap_increase_per_mark", ref this.WightCapIncreasePerMark, false );

            Data.Fill( "mummy_tag", ref this.MummyTag, false );
            Data.Fill( "mummy_type_percent", ref this.MummyTypePercent, false );
            Data.Fill( "bonus_mummy_percent", ref this.BonusMummyPercent, false );
            Data.Fill( "mummy_cap_increase", ref this.MummyCapIncrease, false );
            Data.Fill( "mummy_cap_increase_per_mark", ref this.MummyCapIncreasePerMark, false );

            Data.Fill( "necromancer_upgrade_to_grant_on_death", ref this.NecromancerUpgradeToGrantOnDeathName, false );

            Data.Fill( "name_for_elderling_transformation", ref this.NameForElderlingTransformation, false );

            Data.Fill( "immune_to_necromancy", ref this.ImmuneToNecromancy, false );
        }

        public override void DoPostInitializationAndSortingLogic_BackgroundThreads()
        {
            if (this.NecromancerUpgradeToGrantOnDeathName != null && this.NecromancerUpgradeToGrantOnDeathName.Length > 0) {
                this.NecromancerUpgradeToGrantOnDeath = NecromancerUpgradeTable.Instance.GetRowByName(this.NecromancerUpgradeToGrantOnDeathName);
                if (this.NecromancerUpgradeToGrantOnDeath == null) {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Unknown Necromancer Upgrade to grant on death " + this.NecromancerUpgradeToGrantOnDeathName + " for " + this.AttachedGameEntityTypeData.InternalName, Verbosity.ShowAsError );
                }
            }

            HackingType transformElderlingHack = HackingTypeTable.Instance.GetRowByName("TransformElderling");
            bool hasElderlingTransformation = this.NameForElderlingTransformation != null && this.NameForElderlingTransformation.Length > 0;
            // Can't use GameEntityTypeData.GetIsEligibleForHack since it might not be initialized yet.
            bool hasElderlingTransformationHack = this.AttachedGameEntityTypeData.GetListOfHacks().Contains(transformElderlingHack);
            if (hasElderlingTransformation && !hasElderlingTransformationHack) {
                ArcenDebugging.ArcenDebugLogSingleLine( "Unit " + this.AttachedGameEntityTypeData.InternalName + " can be transformed into " + this.NameForElderlingTransformation + " but is not eligible for the TransformElderling hack.", Verbosity.ShowAsError );
            } else if (!hasElderlingTransformation && hasElderlingTransformationHack) {
                ArcenDebugging.ArcenDebugLogSingleLine( "Unit " + this.AttachedGameEntityTypeData.InternalName + " is eligible for the TransformElderling hack, but does not have a type to be transformed into.", Verbosity.ShowAsError );
            }

            this.NecromancerResourceGranter = null;
            if ( this.CachedTypeForNecromancerResourceGranter != null )
            {
                this.CachedTypeForNecromancerResourceGranter.LoadCachedTypeFromExternalAssembly( this.AttachedGameEntityTypeData );
                if ( this.CachedTypeForNecromancerResourceGranter.CachedType != null )
                    this.NecromancerResourceGranter = this.CachedTypeForNecromancerResourceGranter.CachedType.GetOrCreateFirstAvailableInstanceOfType_AnyThread<INecromancerResourceGranter>( this.AttachedGameEntityTypeData.InternalName + "-DLC3GameEntityTypeDataExtension" );
            }
        }

        public override void AddToTooltip_MidSection_ForEntity( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull, FleetMembership relatedMembershipOrNull, GameEntityTypeData relatedEntityTypeData, TooltipDetail detailLevel )
        {
            int markLevel = relatedSquadOrNull?.CurrentMarkLevel ?? relatedMembershipOrNull?.EffectiveMark ?? 1;
            //these are things for the necromancer only
            string fleetPlaceholderOne = "被这座死灵城支持的舰队";
            string fleetPlaceholderTwo = "被这座死灵城支持的舰队"; //a capital first letter
            int debugCode = 0;
            try{
            debugCode = 100;
            if ( relatedSquadOrNull != null && relatedSquadOrNull.FleetMembership != null )
            {
                debugCode = 200;
                Fleet myFleet = relatedSquadOrNull.FleetMembership.Fleet;
                if ( myFleet != null )
                {
                    debugCode = 300;
                    Fleet bolsteredFleet = World_AIW2.Instance.GetFleetByID( myFleet.CityBolstersFleetID );
                    if ( bolsteredFleet != null )
                    {
                        debugCode = 400;
                        buffer.Add("这支舰队正在支持 ").Add( bolsteredFleet.GetName(), "a1ffa1").Add(".\n");
                        fleetPlaceholderOne = "那支舰队";
                        fleetPlaceholderTwo = "那支舰队";
                    }

                }
            }
            debugCode = 500;
            if ( !String.IsNullOrEmpty( this.SkeletonTag ) )
            {
                buffer.Add( "如果会为" + fleetPlaceholderOne +"创建一个骷髅，它将有" ).Add( this.SkeletonTypePercent, "ffa1a1" ).Add( "%的额外几率成为" ).Add( this.SkeletonTag, "a1a1ff" ).Add( "。\n" );
            }
            debugCode = 600;
            if ( this.BonusSkeletonPercent > 0 )
            {
                buffer.Add( "如果会为" + fleetPlaceholderOne + "创建一个骷髅，它将有" ).Add( this.BonusSkeletonPercent, "a1ffa1" ).Add( "%的额外几率同时创建两个骷髅。\n" );
            }
            debugCode = 700;
            if ( this.SkeletonCapIncrease > 0 || this.SkeletonCapIncreasePerMark > 0 ) {
                 buffer.Add( fleetPlaceholderTwo + "可以额外创建 ").Add(this.SkeletonCapIncrease + (markLevel - 1) * this.SkeletonCapIncreasePerMark).Add(" 个骷髅。\n");
            }
            if ( !String.IsNullOrEmpty( this.WightTag ) )
            {
                buffer.Add( "如果会为" + fleetPlaceholderOne +"创建一个亡魂，它将有" ).Add( this.WightTypePercent, "ffa1a1" ).Add( "%的额外几率成为" ).Add( this.WightTag, "a1a1ff" ).Add( "。\n" );
            }
            debugCode = 800;
            if ( this.BonusWightPercent > 0 )
            {
                buffer.Add( "如果会为" + fleetPlaceholderOne + "创建一个亡魂，它将有" ).Add( this.BonusWightPercent, "a1ffa1" ).Add( "%的额外几率同时创建两个亡魂。\n" );
            }
            if ( this.WightCapIncrease > 0 || this.WightCapIncreasePerMark > 0 ) {
                 buffer.Add(fleetPlaceholderTwo + "可以额外创建 ").Add(this.WightCapIncrease + (markLevel - 1) * this.WightCapIncreasePerMark).Add(" 个亡魂。\n");
            }
            debugCode = 900;
            if ( !String.IsNullOrEmpty( this.MummyTag ) )
            {
                buffer.Add( "如果会为" + fleetPlaceholderOne + "创建一个木乃伊，它将有" ).Add( this.MummyTypePercent, "ffa1a1" ).Add( "%的额外几率成为" ).Add( this.MummyTag, "a1a1ff" ).Add( "。\n" );
            }
            debugCode = 1000;
            if ( this.BonusMummyPercent > 0 )
            {
                buffer.Add( "如果会为" + fleetPlaceholderOne + "创建一个木乃伊，它将有" ).Add( this.BonusMummyPercent, "a1ffa1" ).Add( "%的额外几率同时创建两个木乃伊。\n" );
            }
            debugCode = 1100;
            if ( this.MummyCapIncrease > 0 || this.MummyCapIncreasePerMark > 0 ) {
                 buffer.Add( fleetPlaceholderTwo + "可以额外创建 ").Add(this.MummyCapIncrease + (markLevel - 1) * this.MummyCapIncreasePerMark).Add(" 个木乃伊。\n");
            }
            debugCode = 1200;
            if ( this.NecromancerUpgradeToGrantOnDeath != null )
            {
                buffer.Add( "如果死灵法师协助击杀此单位，它将获得一项新升级。\n" );
            }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in AddToTooltip_MidSection_ForEntity from DLC3 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public void GetNecromancerResourcesToGrantOnDeath(GameEntity_Squad relatedSquadOrNull,
                out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt resourceOneToGrantOnDeath)
        {
                scienceToGrantOnDeath = (FInt)this.NecromancerScienceToGrantOnDeath;
                hackingToGrantOnDeath = (FInt)this.NecromancerHackingToGrantOnDeath;
                resourceOneToGrantOnDeath = this.NecromancerResourceOneToGrantOnDeath;
                if ( this.NecromancerResourceOneToGrantOnDeathPerMark > 0 &&
                        relatedSquadOrNull != null )
                {
                    resourceOneToGrantOnDeath += this.NecromancerResourceOneToGrantOnDeathPerMark * (relatedSquadOrNull.CurrentMarkLevel - 1);
                }
                if ( this.NecromancerResourceGranter != null ) {
                    resourceOneToGrantOnDeath += this.NecromancerResourceGranter.GetResourceOneToGrantOnDeath(relatedSquadOrNull);
                }
                int multiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "BonusNecromancerResources" );
                if (multiplier > 0) {
                    scienceToGrantOnDeath *= multiplier;
                    hackingToGrantOnDeath *= multiplier;
                    resourceOneToGrantOnDeath *= multiplier;
                }
        }

        public void GetDysonResourcesToGrantOnDeath(GameEntity_Squad relatedSquadOrNull,
                out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt metalToGrantOnDeath, out FInt resourceOneToGrantOnDeath)
        {
            // scienceToGrantOnDeath = (FInt)this.NecromancerScienceToGrantOnDeath;
            // hackingToGrantOnDeath = (FInt)this.NecromancerHackingToGrantOnDeath;
            scienceToGrantOnDeath = FInt.Zero;
            hackingToGrantOnDeath = FInt.Zero;
            metalToGrantOnDeath = FInt.Zero;
            resourceOneToGrantOnDeath = this.DysonSidekickResourceOneToGrantOnDeath;
            if ( this.DysonSidekickResourceOneToGrantOnDeathPerMark > 0 &&
                 relatedSquadOrNull != null )
            {
                resourceOneToGrantOnDeath += this.DysonSidekickResourceOneToGrantOnDeathPerMark * (relatedSquadOrNull.CurrentMarkLevel - 1);
            }
        }
        public void GetDarkZenithSidekickResourcesToGrantOnDeath(GameEntity_Squad relatedSquadOrNull,
                out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out DZResource resource, out int resourceAmount)
        {
            scienceToGrantOnDeath = FInt.Zero;
            hackingToGrantOnDeath = FInt.Zero;
            resourceAmount = 0;
            resource = DZResource.None;

            if ( this.DarkZenithSidekickScienceToGrantOnDeath > 0 )
            {
                scienceToGrantOnDeath += this.DarkZenithSidekickScienceToGrantOnDeath;
            }
            if ( this.DarkZenithSidekickHackingToGrantOnDeath > 0 )
            {
                hackingToGrantOnDeath += this.DarkZenithSidekickHackingToGrantOnDeath;
            }
            if ( this.DarkZenithSidekickResourceToGrant != DZResource.None)
            {
                resource = this.DarkZenithSidekickResourceToGrant;
                resourceAmount = this.DarkZenithSidekickResourceAmountToGrant;
            }

        }
        public void GetSpireSidekickResourcesToGrantOnDeath(GameEntity_Squad relatedSquadOrNull,
                out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath)
        {
            scienceToGrantOnDeath = FInt.Zero;
            hackingToGrantOnDeath = FInt.Zero;

            if ( this.SpireSidekickScienceToGrantOnDeath > 0 )
            {
                scienceToGrantOnDeath += this.SpireSidekickScienceToGrantOnDeath;
            }
            if ( this.SpireSidekickHackingToGrantOnDeath > 0 )
            {
                hackingToGrantOnDeath += this.SpireSidekickHackingToGrantOnDeath;
            }

        }
        public void GetScourgeEmpireResourcesToGrantOnDeath(GameEntity_Squad relatedSquadOrNull,
                out FInt corbomiteToGrantOnDeath )
        {
            corbomiteToGrantOnDeath = FInt.Zero;

            if ( this.ScourgeEmpireCorbomiteToGrantOnDeath > 0 )
            {
                corbomiteToGrantOnDeath += this.ScourgeEmpireCorbomiteToGrantOnDeath;
            }

        }
        public void GetArmadaResourcesToGrantOnDeath(GameEntity_Squad relatedSquadOrNull,
                             out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath,
                             out FInt metalToGrantOnDeath, out FInt resourceOneToGrantOnDeath )
        {
            scienceToGrantOnDeath = FInt.Zero;
            hackingToGrantOnDeath = FInt.Zero;
            metalToGrantOnDeath = FInt.Zero;
            resourceOneToGrantOnDeath = FInt.Zero;

            if ( this.ArmadaMetalToGrantOnDeath > 0 )
            {
                metalToGrantOnDeath += this.ArmadaMetalToGrantOnDeath;
            }
            if ( this.ArmadaScienceToGrantOnDeath > 0 )
            {
                scienceToGrantOnDeath += this.ArmadaScienceToGrantOnDeath;
            }
            if ( this.ArmadaHackingToGrantOnDeath > 0 )
            {
                hackingToGrantOnDeath += this.ArmadaHackingToGrantOnDeath;
            }
            if ( this.ArmadaResourceOneToGrantOnDeath > 0 )
            {
                resourceOneToGrantOnDeath += this.ArmadaResourceOneToGrantOnDeath;
                if ( this.ArmadaResourceOneToGrantOnDeathPerMark > 0 &&
                     relatedSquadOrNull != null )
                {
                    resourceOneToGrantOnDeath += this.ArmadaResourceOneToGrantOnDeathPerMark * (relatedSquadOrNull.CurrentMarkLevel - 1);
                }

            }

        }
        public void GetApkalluResourcesToGrantOnDeath(GameEntity_Squad relatedSquadOrNull,
                             out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath,
                             out FInt metalToGrantOnDeath, out FInt resourceOneToGrantOnDeath,
                             out FInt resourceTwoToGrantOnDeath, out FInt resourceThreeToGrantOnDeath )
        {
            scienceToGrantOnDeath = FInt.Zero;
            hackingToGrantOnDeath = FInt.Zero;
            metalToGrantOnDeath = FInt.Zero;
            resourceOneToGrantOnDeath = FInt.Zero;
            resourceTwoToGrantOnDeath = FInt.Zero;
            resourceThreeToGrantOnDeath = FInt.Zero;

            if ( this.ApkalluMetalToGrantOnDeath > 0 )
            {
                metalToGrantOnDeath += this.ApkalluMetalToGrantOnDeath;
            }
            if ( this.ApkalluScienceToGrantOnDeath > 0 )
            {
                scienceToGrantOnDeath += this.ApkalluScienceToGrantOnDeath;
            }
            if ( this.ApkalluHackingToGrantOnDeath > 0 )
            {
                hackingToGrantOnDeath += this.ApkalluHackingToGrantOnDeath;
            }
            if ( this.ApkalluResourceOneToGrantOnDeath > 0 )
            {
                resourceOneToGrantOnDeath += this.ApkalluResourceOneToGrantOnDeath;
                if ( this.ApkalluResourceOneToGrantOnDeathPerMark > 0 &&
                     relatedSquadOrNull != null )
                {
                    resourceOneToGrantOnDeath += this.ApkalluResourceOneToGrantOnDeathPerMark * (relatedSquadOrNull.CurrentMarkLevel - 1);
                }
            }
            if ( this.ApkalluResourceTwoToGrantOnDeath > 0 )
            {
                resourceTwoToGrantOnDeath += this.ApkalluResourceTwoToGrantOnDeath;
                if ( this.ApkalluResourceTwoToGrantOnDeathPerMark > 0 &&
                     relatedSquadOrNull != null )
                {
                    resourceTwoToGrantOnDeath += this.ApkalluResourceTwoToGrantOnDeathPerMark * (relatedSquadOrNull.CurrentMarkLevel - 1);
                }
            }
            if ( this.ApkalluResourceThreeToGrantOnDeath > 0 )
            {
                resourceThreeToGrantOnDeath += this.ApkalluResourceThreeToGrantOnDeath;
                if ( this.ApkalluResourceThreeToGrantOnDeathPerMark > 0 &&
                     relatedSquadOrNull != null )
                {
                    resourceThreeToGrantOnDeath += this.ApkalluResourceThreeToGrantOnDeathPerMark * (relatedSquadOrNull.CurrentMarkLevel - 1);
                }
            }

        }

        public override void AddToTooltip_GainsSection_ForEntity( 
            ArcenCharacterBufferBase buffer, 
            GameEntity_Squad relatedSquadOrNull, 
            FleetMembership relatedMembershipOrNull, 
            GameEntityTypeData relatedEntityTypeData, 
            TooltipDetail detailLevel )
        {
            bool for_encyclopedia = (buffer.Writer()?.Config.ExtraFlags & ShipExtraDetailFlags.Encyclopedia) > 0;
            
            //show the necromancer hacking or science stuff only if there is a necromancer in game.
            // .. or, this is the encyclopedia?
            if ( NecromancerEmpireFactionBaseInfo.GetNecromancerFactionCount() > 0 ||
                 for_encyclopedia )
            {
                this.GetNecromancerResourcesToGrantOnDeath(relatedSquadOrNull, 
                    out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt resourceOneToGrantOnDeath);

                if (scienceToGrantOnDeath > FInt.Zero || 
                    hackingToGrantOnDeath > FInt.Zero ||
                    resourceOneToGrantOnDeath > FInt.Zero)
                {
                    buffer.Open(TextStyle.Attr_Line);
                        
                    buffer.Add("如果").Add("死灵法师", TextStyle.PlayerType_Name).Add("协助摧毁此单位，他们将获得 ");

                    int count = 0;
                    if ( scienceToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(scienceToGrantOnDeath, "+", TextTerm.Science, TermUse.Icon);
                        count++;
                    }
                    
                    if ( hackingToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(hackingToGrantOnDeath, "+", TextTerm.Hacking, TermUse.Icon);
                        count++;
                    }
                    
                    if ( resourceOneToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(resourceOneToGrantOnDeath, "+", TextTerm.Essence, TermUse.Icon);
                        count++;
                    }
                    
                    buffer.Add(".");
                    buffer.Close(TextStyle.Attr_Line);
                }
            }
            
            if ( DarkZenithFactionBaseInfo.GetDZSidekickFactionCount() > 0 ||
                 for_encyclopedia )
            {
                this.GetDarkZenithSidekickResourcesToGrantOnDeath(relatedSquadOrNull, out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out DZResource resource, out int resourceAmount);

                if (scienceToGrantOnDeath > FInt.Zero || 
                    hackingToGrantOnDeath > FInt.Zero )
                {
                    buffer.Open(TextStyle.Newline_NoLabel);
                        
                    buffer.Add("如果").Add("黑暗泽尼斯", TextStyle.PlayerType_Name).Add("协助摧毁此单位，他们将获得 ");

                    int count = 0;
                    if ( scienceToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(scienceToGrantOnDeath, "+", TextTerm.Science, TermUse.Icon);
                        count++;
                    }
                    
                    if ( hackingToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(hackingToGrantOnDeath, "+", TextTerm.Hacking, TermUse.Icon);
                        count++;
                    }
                    if ( resource != DZResource.None && resourceAmount > 0 )
                    {
                        string color = DarkZenithFactionBaseInfoRoot.ResourceColour[resource];
                        buffer.Add(" + ", color).Add( resourceAmount.ToString(), color ).Add(" " +DarkZenithFactionBaseInfoRoot.ResourceFancyName[resource], color);
                    }
                    buffer.Add(". ");
                    buffer.Close(TextStyle.Newline_NoLabel);
                }
            }
            if ( ScourgeInfusedHumanEmpireFactionBaseInfo.GetScourgeEmpireFactionCount() > 0 ||
                 for_encyclopedia )
            {
                this.GetScourgeEmpireResourcesToGrantOnDeath(relatedSquadOrNull, out FInt corbomiteToGrantOnDeath);

                if (corbomiteToGrantOnDeath > 0 )
                {
                    buffer.Open(TextStyle.Newline_NoLabel);
                        
                    buffer.Add("如果").Add("天灾帝国", TextStyle.PlayerType_Name).Add("协助摧毁此单位，他们将获得 ");

                    int count = 0;
                    if ( corbomiteToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(corbomiteToGrantOnDeath, "+", TextTerm.Essence, TermUse.Icon);
                        count++;
                    }

                    buffer.Add(". ");
                    buffer.Close(TextStyle.Newline_NoLabel);
                }
            }
            if ( FallenSpireFactionBaseInfo.GetSpireSidekickFactionCount() > 0 ||
                 for_encyclopedia )
            {
                this.GetSpireSidekickResourcesToGrantOnDeath(relatedSquadOrNull, out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath);

                if (scienceToGrantOnDeath > FInt.Zero || 
                    hackingToGrantOnDeath > FInt.Zero )
                {
                    buffer.Open(TextStyle.Newline_NoLabel);
                        
                    buffer.Add("如果").Add("尖塔副官", TextStyle.PlayerType_Name).Add("协助摧毁此单位，他们将获得 ");

                    int count = 0;
                    if ( scienceToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(scienceToGrantOnDeath, "+", TextTerm.Science, TermUse.Icon);
                        count++;
                    }
                    
                    if ( hackingToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(hackingToGrantOnDeath, "+", TextTerm.Hacking, TermUse.Icon);
                        count++;
                    }
                    
                    buffer.Add(". ");
                    buffer.Close(TextStyle.Newline_NoLabel);
                }
            }
            if ( ArmadaFactionBaseInfo.GetArmadaFactionCount() > 0 ||
                 for_encyclopedia )
            {

                this.GetArmadaResourcesToGrantOnDeath(relatedSquadOrNull, out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt metalToGrantOnDeath, out FInt resourceOneToGrantOnDeath );
                if (scienceToGrantOnDeath > FInt.Zero ||
                    metalToGrantOnDeath > FInt.Zero ||
                    hackingToGrantOnDeath > FInt.Zero ||
                    resourceOneToGrantOnDeath > FInt.Zero )
                {
                    buffer.Open(TextStyle.Newline_NoLabel);
                    buffer.Add("如果").Add(" 舰队 ", TextStyle.PlayerType_Name).Add("协助摧毁此单位，他们将获得 ");

                    int count = 0;
                    if ( scienceToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(scienceToGrantOnDeath, "+", TextTerm.Science, TermUse.Icon);
                        count++;
                    }
                    if ( metalToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(metalToGrantOnDeath, "+", TextTerm.Metal, TermUse.Icon);
                        count++;
                    }

                    if ( hackingToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(hackingToGrantOnDeath, "+", TextTerm.Hacking, TermUse.Icon);
                        count++;
                    }

                    if ( resourceOneToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                         buffer.AddNumber(resourceOneToGrantOnDeath, "+", TextTerm.Essence, TermUse.Icon);
                        count++;
                    }

                    buffer.Add(". ");
                    buffer.Close(TextStyle.Newline_NoLabel);
                }
            }
            if ( DysonSidekickFactionBaseInfo.GetDysonFactionCount() > 0 ||
                 for_encyclopedia )
            {

                this.GetDysonResourcesToGrantOnDeath(relatedSquadOrNull, out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt metalToGrantOnDeath, out FInt resourceOneToGrantOnDeath );
                if (scienceToGrantOnDeath > FInt.Zero ||
                    metalToGrantOnDeath > FInt.Zero ||
                    hackingToGrantOnDeath > FInt.Zero ||
                    resourceOneToGrantOnDeath > FInt.Zero )
                {
                    buffer.Open(TextStyle.Newline_NoLabel);
                    buffer.Add("如果").Add(" 戴森 ", TextStyle.PlayerType_Name).Add("协助摧毁此单位，他们将获得 ");

                    int count = 0;
                    if ( scienceToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(scienceToGrantOnDeath, "+", TextTerm.Science, TermUse.Icon);
                        count++;
                    }
                    if ( metalToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(metalToGrantOnDeath, "+", TextTerm.Metal, TermUse.Icon);
                        count++;
                    }

                    if ( hackingToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(hackingToGrantOnDeath, "+", TextTerm.Hacking, TermUse.Icon);
                        count++;
                    }

                    if ( resourceOneToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                         buffer.AddNumber(resourceOneToGrantOnDeath, "+", TextTerm.Essence, TermUse.Icon);
                        count++;
                    }

                    buffer.Add(". ");
                    buffer.Close(TextStyle.Newline_NoLabel);
                }
            }
            if ( ApkalluFactionBaseInfo.GetApkalluFactionCount() > 0 ||
                 for_encyclopedia )
            {

                this.GetApkalluResourcesToGrantOnDeath(relatedSquadOrNull, out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt metalToGrantOnDeath, out FInt resourceOneToGrantOnDeath, out FInt resourceTwoToGrantOnDeath, out FInt resourceThreeToGrantOnDeath );
                if (scienceToGrantOnDeath > FInt.Zero ||
                    metalToGrantOnDeath > FInt.Zero ||
                    hackingToGrantOnDeath > FInt.Zero ||
                    resourceOneToGrantOnDeath > FInt.Zero ||
                    resourceTwoToGrantOnDeath > FInt.Zero ||
                    resourceThreeToGrantOnDeath > FInt.Zero )
                {
                    buffer.Open(TextStyle.Newline_NoLabel);
                    buffer.Add("如果").Add(" 阿普卡鲁 ", TextStyle.PlayerType_Name).Add("协助摧毁此单位，他们将获得 ");

                    int count = 0;
                    if ( scienceToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(scienceToGrantOnDeath, "+", TextTerm.Science, TermUse.Icon);
                        count++;
                    }
                    if ( metalToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(metalToGrantOnDeath, "+", TextTerm.Metal, TermUse.Icon);
                        count++;
                    }

                    if ( hackingToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(hackingToGrantOnDeath, "+", TextTerm.Hacking, TermUse.Icon);
                        count++;
                    }

                    if ( resourceOneToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(resourceOneToGrantOnDeath, "+", TextTerm.FuelArgon, TermUse.Icon);
                        count++;
                    }

                    if ( resourceTwoToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(resourceTwoToGrantOnDeath, "+", TextTerm.FuelRadon, TermUse.Icon);
                        count++;
                    }

                    if ( resourceThreeToGrantOnDeath > 0 )
                    {
                        if (count > 0)
                            buffer.Add(" ");
                        buffer.AddNumber(resourceThreeToGrantOnDeath, "+", TextTerm.FuelXenon, TermUse.Icon);
                        count++;
                    }

                    buffer.Add(". ");
                    buffer.Close(TextStyle.Newline_NoLabel);
                }
            }
            
        }
    }

    public interface INecromancerResourceGranter : IArcenExternalClassNothingSpecialToManageSingleton, IExternalBaseInfo_Singleton
    {
        FInt GetResourceOneToGrantOnDeath(GameEntity_Squad relatedSquadOrNull);
    }
}
