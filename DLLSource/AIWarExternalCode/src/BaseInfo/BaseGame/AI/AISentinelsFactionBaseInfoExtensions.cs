using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    ///First off: Why do we have this for the AI Sentinels and not for other factions?
    ///           The general answer to that is that many different subfactions of the AI,
    ///           including the hunter, warden, PG, and on and on -- all need to access it.
    ///           
    ///           Normally when we're talking about the data for a faction, we can just get the BaseInfo
    ///           data from that faction.  However, there are MANY factions that need to be able to get
    ///           this specific BaseInfo from their parent faction instead of themselves directly.
    ///           
    ///           These extensions facilitate that.          

    public static class AISentinelsFactionBaseInfoExtensions
    {
        public static AISentinelsFactionBaseInfo GetAISentinelsCoreData( this Faction ParentObject )
        {
            if ( ParentObject == null )
                return null;
            if ( ParentObject.Type == FactionType.AI )
            {
                return ParentObject.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            }
            
            if ( FactionUtilityMethods.Instance.IsACoreAISubFaction( ParentObject ) )
            {
                Faction parentFaction = ParentObject.GetParentFactionOrNull();
                if ( parentFaction == null )
                {
                    ArcenDebugging.ArcenDebugLog( "GetAISentinelsCoreData: Could not find any parent AI faction for faction of type " + ParentObject.SpecialFactionData.InternalName, Verbosity.ShowAsError );
                    return null;
                }
                return parentFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            }
            
            ArcenDebugging.ArcenDebugLog( "Tried to call GetAISentinelsCoreData on a faction of type " + ParentObject.SpecialFactionData.InternalName, Verbosity.ShowAsError );
            return null;
        }

        public static AISentinelsFactionBaseInfo TryGetAISentinelsCoreData( this Faction ParentObject )
        {
            if ( ParentObject == null )
                return null;
            if ( ParentObject.Type == FactionType.AI )
            {
                return ParentObject.TryGetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            }
            else if ( FactionUtilityMethods.Instance.IsACoreAISubFaction( ParentObject ) )
            {
                Faction parentFaction = ParentObject.GetParentFactionOrNull();
                if ( parentFaction == null )
                    return null;
                return parentFaction.TryGetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            }
            else
                return null;
        }
    }
}
