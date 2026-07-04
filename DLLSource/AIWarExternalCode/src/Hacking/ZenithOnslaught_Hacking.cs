using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /* DZ Hacks */

    /* Nomad Hacks */
    public class Hacking_ActivateNomadStructure : BaseHackingImplementation
    {
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "NomadPlanetNexus" );
            PlanetFaction pFaction = Target.PlanetFaction;
            ArcenPoint spawnLocation = Target.WorldLocation;
            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 0,
                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Hacking-NomadActivate" );
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );
            return true;
        }
    }

    public class Hacking_DeactivateNomadStructure : BaseHackingImplementation
    {
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Target.Planet.IsDisabledNomad = true;
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );
            return true;
        }
    }
    public class Hacking_SendNomadToNearestAIKing : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            string output = "Hover over individual planets to see time estimates ";
            return output;
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
            {
                RejectionReasonDescription = "Your science advisor regretfully informs you that sacrificing the remainder of humanity to destroy the AI is not worth it";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            // Planet closestKingPlanet = GetBestKingPlanet( null );
            // if ( closestKingPlanet == null )
            // {
            //     RejectionReasonDescription = "You haven't found an AI Overlord yet to crash the planet into.";
            //     return Hackable.NeverBeHacked_ButStillShow;
            // }
            // if ( Target.Planet.NomadTargetPlanetIdx >= 0 )
            // {
            //     RejectionReasonDescription = "This hack has already been done";
            //     return Hackable.NeverCanBeHacked_Hide;
            // }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( planet == null )
                {
                    debugCode = 200;
                    throw new Exception("Failed to find target to crash into");
                    // debugCode = 200;
                    // ArcenDebugging.ArcenDebugLogSingleLine("???", Verbosity.DoNotShow );
                    // planet = GetBestKingPlanet( Target.Planet ); //emergency fallback; unused but can be if necessary
                }
                debugCode = 300;
                if ( planet != null )
                {
                    debugCode = 400;
                    Target.Planet.NomadTargetPlanetIdx = planet.Index;
                    Target.Planet.TimeForNextMove = World_AIW2.Instance.GameSecond;
                    debugCode = 500;
                    int distance = Mat.DistanceBetweenPointsImprecise( Hacker.Planet.GalaxyLocation, planet.GalaxyLocation );
                    Target.Planet.SecondsTillNomadCrashes = NomadPlanetsFactionBaseInfo.Instance == null ? -1 : NomadPlanetsFactionBaseInfo.Instance.GetCrashTime( planet, Hacker.Planet, distance );
                    debugCode = 600;
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "CrashingNomadPlanetNexus" );
                    debugCode = 700;
                    PlanetFaction pFaction = Target.PlanetFaction;
                    ArcenPoint spawnLocation = Target.WorldLocation;
                    debugCode = 800;
                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 0,
                                                                                               pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Hacking-NomadCrash" );
                    if ( entity == null )
                        throw new Exception("Failed to spawn crashing nexus");
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine("We now have " + entity.ToStringWithPlanet(), Verbosity.DoNotShow );
                    debugCode = 900;
                    Target.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );
                }
                else
                    throw new Exception("Could not find planet to crash into!");
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception hit during send nomad to ai homeworld DoOneSecondOfHackingLogic_AsPartOfMainSim debug code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return true;
        }

        private Planet GetBestKingPlanet( Planet planetOrNull )
        {
            //this is currently unused
            GameEntity_Squad closestKing = null;
            int minDistance = 9999;
            int newDistance = 9999;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                 if ( entity.GetFactionTypeSafe() != FactionType.AI )
                     continue;
                 if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                     continue;
                 if ( closestKing == null )
                 {
                     closestKing = entity;
                     if ( planetOrNull != null )
                         minDistance = Mat.DistanceBetweenPointsImprecise( planetOrNull.GalaxyLocation, entity.Planet.GalaxyLocation );
                     continue;
                 }
                 if ( planetOrNull != null )
                     newDistance = Mat.DistanceBetweenPointsImprecise( planetOrNull.GalaxyLocation, entity.Planet.GalaxyLocation );
                 if ( newDistance < minDistance )
                 {
                     closestKing = entity;
                     minDistance = newDistance;
                 }
             }
            if ( closestKing == null )
                return null;
            else
                return closestKing.Planet;
        }
    }
    /* Zenith Miner Hacks */
    public class Hacking_DestroyProbe : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            ZenithMinersPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
            if ( data == null )
                throw new Exception( "Invalid zenith miners per unit data" );

            if ( data.RemainingDuration < this.GetTotalSecondsToHack( Target, planet, HackerOrNull, Type ) )
            {
                RejectionReasonDescription = "Not enough time to finish the hack before the Miner arrives";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //the base method should destroy the probe
            return true;
        }
    }
    public class Hacking_RerouteProbe : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            ZenithMinersPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
            if ( data == null )
                throw new Exception( "Invalid zenith miners per unit data" );

            bool foundLegalMove = false;

            Faction facOrNull = Target.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                RejectionReasonDescription = "Missing faction";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            ZenithMinersFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ZenithMinersFactionBaseInfo>();
            if ( gdata.BlockedPlanets.Count > 0 )
            {
                foreach ( Planet neighbor in Target.Planet.LinkedNeighbors( false ) )
                {
                    if ( !gdata.BlockedPlanets.Contains( neighbor ) )
                    {
                        foundLegalMove = true;
                        break;
                    }
                }
            }
            else
                foundLegalMove = true;
            if ( !foundLegalMove )
            {
                RejectionReasonDescription = "No legal adjacent planets for the Miners";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( data.RemainingDuration < this.GetTotalSecondsToHack( Target, planet, HackerOrNull, Type ) )
            {
                RejectionReasonDescription = "Not enough time to finish the hack before the Miner arrives";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Faction facOrNull = Target.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                return false;
            }

            //make it so that the hack always gives the same result for the given probe on a given planet, so players can't save-scum
            Planet newPlanet = planet;
            // int retries = 100;
            // ZenithMinersFactionBaseInfo data = facOrNull.TryGetExternalBaseInfoAs<ZenithMinersFactionBaseInfo>();
            // while ( newPlanet != null && retries-- > 100 );
            // {
            //     old code to pick a random target
            //     newPlanet = Target.Planet.GetRandomNeighbor_DeterministicGivenAddedSeed( false, Target.PrimaryKeyID );
            //     if ( newPlanet == null )
            //         throw new Exception( "Could not find new planet" );
            //     retries--;
            //     if ( data.BlockedPlanets.Contains( newPlanet ) )
            //         newPlanet = null;
            // }
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ZenithMinerProbe" );
            if ( entityData == null )
                throw new Exception( "No ZenithMinerProbe defined in XML" );
            PlanetFaction pFaction = newPlanet.GetPlanetFactionForFaction( Target.PlanetFaction.Faction );
            GameEntity_Squad newProbe = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                                    pFaction.Faction.LooseFleet, 0, Target.WorldLocation, Context, "Hacking-ZenithMinerProbe" );  //is fine, main sim thread
            if ( newProbe != null )
            {
                ZenithMinersPerUnitBaseInfo newData = newProbe.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );
                ZenithMinersPerUnitBaseInfo oldData = Target.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                oldData.CopyTo( newData );
            }

            return true;
        }
    }
    public class Hacking_ReprogramProbeForFasterShips : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            ZenithMinersPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
            if ( data == null )
                throw new Exception( "Invalid zenith miners per unit data" );

            if ( data.RemainingDuration < this.GetTotalSecondsToHack( Target, planet, HackerOrNull, Type ) )
            {
                RejectionReasonDescription = "Not enough time to finish the hack before the Miner arrives";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            ZenithMinersPerUnitBaseInfo data = Target.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );
            data.Effect = ZenithMinerEffect.SpeedupShipsOnPlanet;
            return true;
        }
    }

    public class Hacking_ReprogramProbeForSlowerShips : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            ZenithMinersPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
            if ( data == null )
                throw new Exception( "Invalid zenith miners per unit data" );

            if ( data.RemainingDuration < this.GetTotalSecondsToHack( Target, planet, HackerOrNull, Type ) )
            {
                RejectionReasonDescription = "Not enough time to finish the hack before the Miner arrives";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            ZenithMinersPerUnitBaseInfo data = Target.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );
            data.Effect = ZenithMinerEffect.SlowShipsOnPlanet;
            return true;
        }
    }

    public class Hacking_MakePlanetNomadic : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            ZenithMinersPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
            if ( data == null )
                throw new Exception( "Invalid zenith miners per unit data" );

            if ( data.RemainingDuration < this.GetTotalSecondsToHack( Target, planet, HackerOrNull, Type ) )
            {
                RejectionReasonDescription = "Not enough time to finish the hack before the Miner arrives";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            ZenithMinersPerUnitBaseInfo data = Target.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );
            data.Effect = ZenithMinerEffect.MakePlanetNomadic;
            return true;
        }
    }


    /* Zenith Architrave Hacks */

    public class Hacking_ZATruce : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Faction faction = Target.GetFactionOrNull_Safe();
            ZenithArchitraveFactionBaseInfo globalData = faction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
            if ( ArcenStrings.Equals( faction.BaseInfo.Allegiance, "Friendly To Players" ) )
            {
                RejectionReasonDescription = "You cannot hack your allies; this hack is only for hostile-to-all Architraves";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            if ( faction.BaseInfo.Allegiance != "Hostile To All" )
            {
                RejectionReasonDescription = "You can only have a truce with hostile-to-all Architraves";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( globalData.PlayersArchitraveIsFriendlyToward.Contains( HackerFaction.FactionIndex ) )
            {
                RejectionReasonDescription = "This hack has already been done";
                return Hackable.NeverCanBeHacked_Hide;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Faction facOrNull = Target.GetFactionOrNull_Safe();
            if ( facOrNull == null )
                return;

            ZenithArchitraveFactionBaseInfo globalData = facOrNull.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
            globalData.IsBeingHacked = true;
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Faction faction = Target.GetFactionOrNull_Safe();
            if ( faction == null )
                return false;
            ZenithArchitraveFactionBaseInfo globalData = faction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
            globalData.PlayersArchitraveIsFriendlyToward.Add( Hacker.GetFactionIndex_Safe() );
            globalData.IsBeingHacked = false;

            ZenithArchitraveFactionBaseInfo.ConvertPortalToHackedPortal( Target, Context );

            return true;
        }
        public override void DoOnCancel_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event, ArcenHostOnlySimContext Context )
        {
            Faction facOrNull = Target.GetFactionOrNull_Safe();
            if ( facOrNull == null )
                return;
            ZenithArchitraveFactionBaseInfo globalData = facOrNull.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
            globalData.IsBeingHacked = false;
        }
    }
    public class Hacking_ZAQuiesce : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Faction faction = Target.GetFactionOrNull_Safe();
            ZenithArchitraveFactionBaseInfo globalData = faction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
            if ( ArcenStrings.Equals( faction.BaseInfo.Allegiance, "Friendly To Players" ) )
            {
                RejectionReasonDescription = "You cannot hack your allies; this hack is only for hostile Architraves";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( globalData.PlayersArchitraveHates.Contains( HackerFaction.FactionIndex ) )
            {
                RejectionReasonDescription = "This hack has already been done";
                return Hackable.NeverCanBeHacked_Hide;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Faction faction = Target.GetFactionOrNull_Safe();
            ZenithArchitraveFactionBaseInfo globalData = faction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
            globalData.PlayersArchitraveHates.Add( Hacker.GetFactionIndex_Safe() );
            globalData.QuiesceStartTime = World_AIW2.Instance.GameSecond;
            globalData.IsQuiesced = true;
            globalData.MaxTerritorySize += 2; //bigger territory now

            ZenithArchitraveFactionBaseInfo.ConvertPortalToHackedPortal( Target, Context );
            return true;
        }
    }
}
