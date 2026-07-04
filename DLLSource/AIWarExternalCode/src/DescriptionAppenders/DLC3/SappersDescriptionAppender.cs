using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SappersDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers ));
            if ( RelatedEntityTypeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                return;
            }

            SappersPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<SappersPerUnitBaseInfo>();
            if ( data == null )
            {
                Buffer.Add( "No per unit data?" );
                return;
            }
            Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
            SappersFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<SappersFactionBaseInfo>();

            int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            SappersDifficulty diff = SappersDifficultyTable.Instance.GetRowByIntensity( globaldata.Intensity, faction );

            if ( data.IsBeachheadTurret )
                Buffer.Add( "This unit was created as a beachhead turret, and will attrition once all enemies on this planet are killed." );

            if ( RelatedEntityTypeData.GetHasTag( "SapperBasicDefensiveStructure" ) )
            {
                int markupTime = SappersFactionBaseInfo.GetSecondsTillMarkup( RelatedEntityOrNull );
                if ( markupTime > -1 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( markupTime );
                    Buffer.Add( "This structure will mark up in " ).Add( markupTime.ToString(), color ).Add( " seconds." );
                }

            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperHabitat" ) )
            {
                Buffer.Add( "This unit has " ).Add( data.MetalStored, "a1ffa1" ).Add( " metal for a sapper to pick up." );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperBeachheadConstructor" ) )
            {
                if ( data.UnitToBuild != null && data.PlanetToBuildOn != null )
                    Buffer.Add( "This Constructor is going to build a " ).Add( data.UnitToBuild.GetDisplayName(), "a1ffa1" ).Add( " on " ).Add( data.PlanetToBuildOn.Name, "a1ffa1" ).Add( ".\n" );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperBeachheader" ) )
            {
                if ( World_AIW2.Instance.GameSecond - data.TimeLastMadeConstructor < diff.BeachheadTurretInterval )
                    Buffer.Add( "This beachheader will be able to build a new turret in " ).Add( (diff.BeachheadTurretInterval - (World_AIW2.Instance.GameSecond - data.TimeLastMadeConstructor)), "ffa1a1" ).Add( " seconds." );
                else
                    Buffer.Add( "This beachheader is ready to send a turret to help its allies." );
                int markupTime = SappersFactionBaseInfo.GetSecondsTillMarkup( RelatedEntityOrNull );
                if ( markupTime > -1 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( markupTime );
                    Buffer.Add( "This beachheader will mark up in " ).Add( markupTime.ToString(), color ).Add( " seconds." );
                }
            }
            if ( RelatedEntityTypeData.GetHasTag( "Sapper" ) )
            {
                if ( data.MetalStored > 0 )
                    Buffer.Add( "This Sapper has " ).Add( data.MetalStored, "a1ffa1" ).Add( " metal to spend.\n" );
                if ( data.BloodstoneStored > 0 )
                    Buffer.Add( "This Sapper has " ).Add( data.BloodstoneStored, "ffa143" ).Add( " bloodstone to spend.\n" );
                if ( data.MoonstoneStored > 0 )
                    Buffer.Add( "This Sapper has " ).Add( data.MoonstoneStored, "43a1ff" ).Add( " moonstone to spend.\n" );
                if ( data.TagForUnitToBuildNext != "" )
                    Buffer.Add( "This sapper is saving up to buy a " ).Add( data.TagForUnitToBuildNext, "a1a1ff" ).Add( "\n" );
                GameEntity_Squad destSquad = data.DestinationForSappers.GetSquad();
                if ( destSquad != null )
                    Buffer.Add( "This Sapper is off to a " ).Add( destSquad.TypeData.GetDisplayName(), "a1ffa1" ).Add( " on " ).Add( destSquad.Planet.Name, "a1ffa1" ).Add( " to collect resources.\n" );
                if ( data.UnitToBuild != null && data.PlanetToBuildOn != null )
                    Buffer.Add( "This Sapper is going to build a " ).Add( data.UnitToBuild.GetDisplayName(), "a1ffa1" ).Add( " on " ).Add( data.PlanetToBuildOn.Name, "a1ffa1" ).Add( ".\n" );

            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperBaseCrystal" ) )
            {
                int flowerTime = (RelatedEntityOrNull.GameSecondEnteredThisPlanet + diff.TimeForCrystalToFlower) - World_AIW2.Instance.GameSecond;
                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( flowerTime );
                Buffer.Add( "This crystal will flower in " ).Add( (data.FloweringTime - World_AIW2.Instance.GameSecond).ToString(), color ).Add( " seconds." );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperFloweredCrystal" ) )
            {
                Buffer.Add( "This flowered crystal is eligible to be harvested for " ).Add( diff.ResourceFromHarvestingCrystal, "a1ffa1" ).Add( " resources." );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperWatchtower" ) )
            {
                Buffer.Add( "This " ).Add( RelatedEntityTypeData.GetDisplayName() ).Add( " has the following inside: " );
                Buffer.Add( data.ShipsInside_ForUI );
                int strengthToShow = data.GetStrengthInside( RelatedEntityOrNull ) / 1000;
                if ( data.GetStrengthInside( RelatedEntityOrNull ) > 0 && strengthToShow == 0 )
                    strengthToShow = 1;
                if ( strengthToShow >= 1 )
                    Buffer.Add( " with approx " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add( strengthToShow, "a1ffa1" );
                Buffer.Add( ". Watchtower max " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add( (globaldata.MaxWatchtowerStrength / 1000), "ff1a1a" ).Add( ".\n" );
                Buffer.Add( "It has " ).Add( data.MetalStored, "a1ffa1" ).Add( " metal available to purchase new ships.\n" );
                Planet helpPlanet = data.PlanetWatchtowerWantsToHelp;
                if ( helpPlanet != null )
                    Buffer.Add( "We have detected enemies on at least " + helpPlanet.Name + " that we want to help fight.\n" );
                int markupTime = SappersFactionBaseInfo.GetSecondsTillMarkup( RelatedEntityOrNull );
                if ( markupTime > -1 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( markupTime );
                    Buffer.Add( "This watchtower will mark up in " ).Add( markupTime.ToString(), color ).Add( " seconds." );
                }
            }
        }
    }
}
