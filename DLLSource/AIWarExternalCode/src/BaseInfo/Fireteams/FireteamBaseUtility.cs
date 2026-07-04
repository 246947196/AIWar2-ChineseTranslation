using Arcen.AIW2.Core;
using System;


using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class FireteamBaseUtility
    {
        // GetNextFireteamId / GetFireteamById are called heavily on background planning threads --
        // AIWardenFactionDeepInfo's long-range planning alone hit GetFireteamById 800+ times per pass,
        // and the old delegate form allocated a capturing closure plus a delegate on every call. We
        // now walk the list with ArcenLessLinkedList's struct Enumerator (zero alloc -- no closure, no
        // delegate, no ThreadStatic state) and replicate exactly what Fireteam.DF does as it goes:
        // prune any null or Disbanded team in place. RemoveCurrent() is safe mid-iteration because the
        // enumerator caches the next link before we touch the current one.
        public static int GetNextFireteamId( ArcenLessLinkedList<Fireteam> Teams )
        {
            int nextId = 1;
            if ( Teams == null )
                return nextId;
            ArcenLessLinkedList<Fireteam>.Enumerator emtor = Teams.GetEnumerator();
            while ( emtor.MoveNext() )
            {
                Fireteam team = emtor.Current;
                if ( team == null || team.status == FireteamStatus.Disbanded )
                {
                    if ( team != null )
                        team.ReturnToPool();
                    emtor.RemoveCurrent();
                    continue;
                }
                if ( team.FireTeamID >= nextId )
                    nextId = team.FireTeamID + 1;
            }
            return nextId;
        }
        public static Fireteam GetFireteamById( ArcenLessLinkedList<Fireteam> Teams, int id )
        {
            if ( Teams == null )
                return null;
            ArcenLessLinkedList<Fireteam>.Enumerator emtor = Teams.GetEnumerator();
            while ( emtor.MoveNext() )
            {
                Fireteam team = emtor.Current;
                if ( team == null || team.status == FireteamStatus.Disbanded )
                {
                    if ( team != null )
                        team.ReturnToPool();
                    emtor.RemoveCurrent();
                    continue;
                }
                if ( team.FireTeamID == id )
                    return team;
            }
            return null;
        }

        #region SerializeFireteams
        public static void SerializeFireteams( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType, ArcenLessLinkedList<Fireteam> Teams )
        {
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) )
            {
                Buffer.AddBool( MetaData, true, "HasAnotherFireteam" );
                team.SerializeTo( MetaData, Buffer, SerializationCmdType );
            }
            Buffer.AddBool( MetaData, false, "HasAnotherFireteam" );
        }
        #endregion

        #region DeserializeFireteamsAndDiscardAnyExtraLeftovers
        public static void DeserializeFireteamsAndDiscardAnyExtraLeftovers( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType, ArcenLessLinkedList<Fireteam> Teams, string ForDebugging_FactionName )
        {
            //Make sure NOT to clear the Teams list prior to calling this method if you want the benefits of reusing existing fireteam objects

            foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) )
                team.WorkingHasBeenUpdatedThisDeserializationPass = false;
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 503 ) )
            {
                while ( Buffer.ReadBool( MetaData, "HasAnotherFireteam" ) )
                {
                    int id = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.Q_ˉ1_To_1ˌ048ˌ575, "FireTeamID" );
                    Fireteam existingFireteam = FireteamBaseUtility.GetFireteamById( Teams, id );
                    if ( existingFireteam != null )
                    {
                        existingFireteam.DeserializedIntoSelf( MetaData, id, Buffer, SerializationCmdType, ForDebugging_FactionName );
                        existingFireteam.WorkingHasBeenUpdatedThisDeserializationPass = true;
                    }
                    else
                    {
                        //still use AddIfNotAlreadyIn rather than Add, because it's possible the initial list had errant duplicates
                        existingFireteam = Fireteam.DeserializeNewFrom_Pooled( MetaData, id, Buffer, SerializationCmdType, ForDebugging_FactionName );
                        existingFireteam.WorkingHasBeenUpdatedThisDeserializationPass = true;
                        Teams.AddIfNotAlreadyIn( existingFireteam );
                    }
                }
            }
            else
            {
                //old style serialization
                int numTeams = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "countOfLivingFireteams" );
                for ( int i = 0; i < numTeams; i++ )
                {
                    int id = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.Q_ˉ1_To_1ˌ048ˌ575, "FireTeamID" );
                    Fireteam existingFireteam = FireteamBaseUtility.GetFireteamById( Teams, id );
                    if ( existingFireteam != null )
                    {
                        existingFireteam.DeserializedIntoSelf( MetaData, id, Buffer, SerializationCmdType, ForDebugging_FactionName );
                        existingFireteam.WorkingHasBeenUpdatedThisDeserializationPass = true;
                    }
                    else
                    {
                        //still use AddIfNotAlreadyIn rather than Add, because it's possible the initial list had errant duplicates
                        existingFireteam = Fireteam.DeserializeNewFrom_Pooled( MetaData, id, Buffer, SerializationCmdType, ForDebugging_FactionName );
                        existingFireteam.WorkingHasBeenUpdatedThisDeserializationPass = true;
                        Teams.AddIfNotAlreadyIn( existingFireteam );
                    }
                }
            }

            ArcenLessLinkedList<Fireteam>.Enumerator emtor = Teams.GetEnumerator();
            while ( emtor.MoveNext() )
            {
                Fireteam team = emtor.Current;
                if ( !team.WorkingHasBeenUpdatedThisDeserializationPass )
                {
                    team.ReturnToPool();   // the fireteam-specific bit DF did
                    emtor.RemoveCurrent();
                }
            }
        }
        #endregion
    }
}
