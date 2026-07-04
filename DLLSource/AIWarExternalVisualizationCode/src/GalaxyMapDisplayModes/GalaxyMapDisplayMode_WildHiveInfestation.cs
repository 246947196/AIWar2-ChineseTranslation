using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_WildHiveInfestation : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            // Need at least one Wild Hive faction to exist.
            return World_AIW2.Instance.GetFirstFactionWithBaseInfoSourceType( "WildHivesFactionBaseInfo" ) != null;
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );

            // Add any large hives
            WildHivesFactionBaseInfo.AllWildHiveFactions.ForEach( workingFaction =>
            {
                foreach ( GameEntity_Squad entity in planet.GetPlanetFactionForFaction( workingFaction.AttachedFaction ).Entities.Squads( WildHivesFactionBaseInfo.NeinzulHiveTag ) )
                {
                    if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                         continue;

                    if ( newOtherGimbalIndex < ArrayToFill.Length && entity.TypeData.InternalName == WildHivesFactionBaseInfo.NeinzulHiveName_Advanced || entity.TypeData.InternalName == WildHivesFactionBaseInfo.NeinzulHiveName_Advanced_Agitated )
                    {
                        ArrayToFill[newOtherGimbalIndex] = entity;
                        newOtherGimbalIndex++;
                    }
                }
            } );

            // Add any workers
            WildHivesFactionBaseInfo.AllWildHiveFactions.ForEach( workingFaction =>
            {
                foreach ( GameEntity_Squad entity in planet.GetPlanetFactionForFaction( workingFaction.AttachedFaction ).Entities.Squads( WildHivesFactionBaseInfo.WorkerTag ) )
                {
                    if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                         continue;

                    if ( newOtherGimbalIndex < ArrayToFill.Length )
                    {
                        ArrayToFill[newOtherGimbalIndex] = entity;
                        newOtherGimbalIndex++;
                    }
                }
            } );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            Faction highestInfectionFaction = null;
            int highestInfection = 0, totalInfection = 0;
            int hiveStrength = 0;

            WildHivesFactionBaseInfo.AllWildHiveFactions.ForEach( workingFaction =>
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( workingFaction.AttachedFaction );
                hiveStrength += pFaction.DataByStance[FactionStance.Self].TotalStrength;
                short thisHiveCount = 0;
                foreach ( GameEntity_Squad hive in pFaction.Entities.Squads( WildHivesFactionBaseInfo.NeinzulHiveTag ) )
                {
                    hiveStrength += hive.AdditionalStrengthFromFactions;
                    thisHiveCount++;
                }
                totalInfection += thisHiveCount;
                if ( thisHiveCount > highestInfection )
                {
                    highestInfection = thisHiveCount;
                    highestInfectionFaction = workingFaction.AttachedFaction;
                }
            } );

            // Left Buffer
            if ( hiveStrength > 0 )
            {
                string textColor = highestInfectionFaction?.FactionCenterColor.ColorHex ?? QuickColors.White;

                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( LeftBuffer, textColor );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( LeftBuffer, hiveStrength, true, true );
                LeftBuffer.EndColor();
            }

            RightBuffer.Add( "\n" );

            if ( highestInfectionFaction != null )
                RightBuffer.AddFactionColoredString( totalInfection.ToString(), highestInfectionFaction );
        }
    }
}