using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public enum CommonAllegiances : byte
    {
        Unknown,
        FriendlyToPlayers,
        AlliedToAI,
        RedTeam,
        BlueTeam,
        GreenTeam,
        DarkAlliance,
        HostileToAll,
        Neutral,
    }
    
    public static class AllegianceHelper
    {
        #region WriteSidebarAllegiance
        public static void WriteSidebarAllegiance( Faction AttachedFaction, ArcenDoubleCharacterBuffer buffer, ref bool hasAdded )
        {
            if ( hasAdded )
                buffer.Add( "    " );
            else
                hasAdded = true;

            if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "Vassal", false ) )
                buffer.Add( "Vassal" );
            else
            {
                string value = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false );
                if ( value != null && value.Length > 0 )
                    buffer.Add( value );
            }
        }
        #endregion

        #region SetDefaultStartingFactionRelationships
        public static void SetDefaultStartingFactionRelationships( Faction thisFaction )
        {
            SpecialFactionData thisSpecialData = thisFaction.SpecialFactionData;

            if ( thisSpecialData == null )
                return; //during game quit

            ExternalFactionBaseInfo thisBaseInfo = thisFaction.BaseInfo;

            if ( thisBaseInfo == null )
                return; //during game quit

            string thisFactionAllegiance = thisBaseInfo.Allegiance;
            bool thisFactionIsAI = thisFaction.Type == FactionType.AI || thisSpecialData.AlliedToAIByDefault || thisFactionAllegiance == "Allied To AI";

            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Setting starting faction relationships for " + thisFaction.GetDisplayName() + " allegiance <" + thisFactionAllegiance + ">", Verbosity.DoNotShow );
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\tSetting relationship to " + otherFaction.GetDisplayName() + " from Helper_IsThereASpecificRelationshipToSetAndThenSkip", Verbosity.DoNotShow );
                }
                else
                {
                    SpecialFactionData otherSpecialData = otherFaction.SpecialFactionData;
                    if ( otherSpecialData == null )
                        continue;
                    
                    ExternalFactionBaseInfo otherBaseInfo = otherFaction.BaseInfo;
                    if ( otherBaseInfo == null )
                        continue;

                    string otherFactionAllegiance = otherBaseInfo.Allegiance;
                    bool otherFactionIsAI = otherFaction.Type == FactionType.AI || otherSpecialData.AlliedToAIByDefault || otherFactionAllegiance == "Allied To AI";

                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\tSetting relationship to " + otherFaction.GetDisplayName() + " allegiance " + otherFactionAllegiance, Verbosity.DoNotShow );

                    if ( String.Equals( thisFactionAllegiance, "Hostile To All", StringComparison.OrdinalIgnoreCase ) ||
                         String.Equals( otherFactionAllegiance, "Hostile To All", StringComparison.OrdinalIgnoreCase ) )
                    {
                        thisFaction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( thisFaction );
                    }
                    else if ( !String.IsNullOrEmpty( thisFactionAllegiance ) &&
                              thisFactionAllegiance == otherFactionAllegiance )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t\tMatched allegiance", Verbosity.DoNotShow );
                        
                        //if we have our allegiances set and they match, we are friends!
                        thisFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( thisFaction );
                    }
                    else if ( thisFactionAllegiance == "Friendly To Players" && otherSpecialData.AlliedToAIByDefault ||
                              otherFactionAllegiance == "Friendly To Players" && thisSpecialData.AlliedToAIByDefault )
                    {
                        thisFaction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( thisFaction );
                    }
                    else if ( thisFactionAllegiance == "Friendly To Players" && otherFaction.Type == FactionType.Player ||
                              otherFactionAllegiance == "Friendly To Players" && thisFaction.Type == FactionType.Player )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t\tPlayer and ally", Verbosity.DoNotShow );

                        thisFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( thisFaction );
                    }
                    else if ( thisFactionIsAI && otherFactionIsAI ) //if they are AI or part of the AI, then always be friends
                    {
                        //Minor factions friendly to the AI should all be friendly to eachother (path 2)
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t\tai allies", Verbosity.DoNotShow );

                        thisFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( thisFaction );

                    }
                    else if ( (thisSpecialData.AlliedToAIByDefault && !otherSpecialData.AlliedToAIByDefault) ||
                              (!thisSpecialData.AlliedToAIByDefault && otherSpecialData.AlliedToAIByDefault) )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t\tOne of us is an AI ally and the other is not", Verbosity.DoNotShow );
                        thisFaction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( thisFaction );
                    }
                    else if ( thisFaction.Type == otherFaction.Type )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t\tmatched type", Verbosity.DoNotShow );

                        thisFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( thisFaction );
                    }
                    else
                    {
                        thisFaction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( thisFaction );
                    }
                }
                if ( debug )
                {
                    if ( thisFaction.GetIsHostileTowards( otherFaction ) )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t\tThe relationship is Hostile ", Verbosity.DoNotShow );
                    else if ( thisFaction.GetIsFriendlyTowards( otherFaction ) )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t\tThe relationship is Friendly ", Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t\tThe relationship is Neutral ", Verbosity.DoNotShow );
                }
            }
        }
        #endregion end SetDefaultStartingFactionRelationships

        #region Helper_IsThereASpecificRelationshipToSetAndThenSkip
        private static bool Helper_IsThereASpecificRelationshipToSetAndThenSkip( Faction thisFaction, Faction otherFaction )
        {
            if ( thisFaction == null || otherFaction == null )
                return false;
            
            SpecialFactionData thisSpecialData = thisFaction.SpecialFactionData;
            SpecialFactionData otherSpecialData = otherFaction.SpecialFactionData;

            PlayerTypeData thisPlayerTypeDataOrNull = thisFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            PlayerTypeData otherPlayerTypeDataOrNull = otherFaction.PlayerTypeDataOrNull_ModeratelyExpensive;

            if ( thisSpecialData == null || otherSpecialData == null )
                return false; //during game quit

            ExternalFactionBaseInfo thisBaseInfo = thisFaction.BaseInfo;
            ExternalFactionBaseInfo otherBaseInfo = otherFaction.BaseInfo;

            if ( thisBaseInfo == null || otherBaseInfo == null )
                return false; //during game quit

            if ( otherFaction == thisFaction ) //neutral to the self
            {                
                thisFaction.MakeNeutralTo( otherFaction );
                otherFaction.MakeNeutralTo( thisFaction );
                return true;
            }
            
            if ( otherFaction.Type == FactionType.NaturalObject ||
                thisFaction.Type == FactionType.NaturalObject ||
                otherSpecialData.Allegiance_ShouldAlwaysBeNeutralToAll ||
                thisSpecialData.Allegiance_ShouldAlwaysBeNeutralToAll )
            {
                thisFaction.MakeNeutralTo( otherFaction );
                otherFaction.MakeNeutralTo( thisFaction );
                return true;
            }

            if ( otherSpecialData.Allegiance_ShouldAlwaysBeFriendlyToAll ||
                thisSpecialData.Allegiance_ShouldAlwaysBeFriendlyToAll )
            {
                thisFaction.MakeFriendlyTo( otherFaction );
                otherFaction.MakeFriendlyTo( thisFaction );
                return true;
            }

            //we always hate each other, no overriding it
            if ( (otherPlayerTypeDataOrNull != null && otherPlayerTypeDataOrNull.Allegiance_FactionsIAlwaysEnemy.ContainsKey( thisSpecialData.InternalName ) ) ||
                ( thisPlayerTypeDataOrNull != null && thisPlayerTypeDataOrNull.Allegiance_FactionsIAlwaysEnemy.ContainsKey( otherSpecialData.InternalName ) ) )
            {
                thisFaction.MakeHostileTo( otherFaction );
                otherFaction.MakeHostileTo( thisFaction );
                return true;
            }

            //we are always buddies, no overriding it
            if ( (otherPlayerTypeDataOrNull != null && otherPlayerTypeDataOrNull.Allegiance_FactionsIAlwaysAlly.ContainsKey( thisSpecialData.InternalName ) ) ||
                (thisPlayerTypeDataOrNull != null && thisPlayerTypeDataOrNull.Allegiance_FactionsIAlwaysAlly.ContainsKey( otherSpecialData.InternalName ) ) )
            {
                thisFaction.MakeFriendlyTo( otherFaction );
                otherFaction.MakeFriendlyTo( thisFaction );
                return true;
            }

            //we always hate each other, no overriding it
            if ( otherSpecialData.Allegiance_FactionsIAlwaysEnemy_NPC.ContainsKey( thisSpecialData.InternalName ) ||
                thisSpecialData.Allegiance_FactionsIAlwaysEnemy_NPC.ContainsKey( otherSpecialData.InternalName ) )
            {
                thisFaction.MakeHostileTo( otherFaction );
                otherFaction.MakeHostileTo( thisFaction );
                return true;
            }
            //we are always buddies, no overriding it
            if ( otherSpecialData.Allegiance_FactionsIAlwaysAlly_NPC.ContainsKey( thisSpecialData.InternalName ) ||
                thisSpecialData.Allegiance_FactionsIAlwaysAlly_NPC.ContainsKey( otherSpecialData.InternalName ) )
            {
                thisFaction.MakeFriendlyTo( otherFaction );
                otherFaction.MakeFriendlyTo( thisFaction );
                return true;
            }

            //we always hate each other, no overriding it
            if ( (thisPlayerTypeDataOrNull != null && otherSpecialData.Allegiance_PlayerTypesIAlwaysEnemy_NPC.ContainsKey( thisPlayerTypeDataOrNull.InternalName ) ) ||
                (otherPlayerTypeDataOrNull != null && thisSpecialData.Allegiance_PlayerTypesIAlwaysEnemy_NPC.ContainsKey( otherPlayerTypeDataOrNull.InternalName ) ) )
            {
                thisFaction.MakeHostileTo( otherFaction );
                otherFaction.MakeHostileTo( thisFaction );
                return true;
            }
            
            //we are always buddies, no overriding it
            if ( (thisPlayerTypeDataOrNull != null && otherSpecialData.Allegiance_PlayerTypesIAlwaysAlly_NPC.ContainsKey( thisPlayerTypeDataOrNull.InternalName ) ) ||
                (otherPlayerTypeDataOrNull != null && thisSpecialData.Allegiance_PlayerTypesIAlwaysAlly_NPC.ContainsKey( otherPlayerTypeDataOrNull.InternalName ) ) )
            {
                thisFaction.MakeFriendlyTo( otherFaction );
                otherFaction.MakeFriendlyTo( thisFaction );
                return true;
            }

            string thisFactionAllegiance = thisBaseInfo.Allegiance;
            if ( !string.IsNullOrEmpty(thisFactionAllegiance) )
            {
                string otherFactionAllegiance = otherBaseInfo.Allegiance;
                if ( thisFactionAllegiance == otherFactionAllegiance )
                {
                    //if we have the same allegiance, consider an override
                    switch ( thisFactionAllegiance )
                    {
                        //if we are on a team together, nothing can break our bond
                        case "Minor Faction Team Red":
                        case "Minor Faction Team Blue":
                        case "Minor Faction Team Green":
                            {
                                thisFaction.MakeFriendlyTo( otherFaction );
                                otherFaction.MakeFriendlyTo( thisFaction );
                            }
                            return true;
                    }
                }
            }

            return false; //nope, nothing special
        }
        #endregion

        public static void EnemyThisFactionToAll( Faction thisFaction )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;
                thisFaction.MakeHostileTo( otherFaction );
                otherFaction.MakeHostileTo( thisFaction );
            }
        }
        public static void AllyThisFactionToMinorFactionTeam( Faction thisFaction, string team )
        {
            //Faction chosenHumanFaction = null;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;
                if ( otherFaction.Type == FactionType.Player || otherFaction.Type == FactionType.AI )
                {
                    thisFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( thisFaction );
                    continue;
                }

                ExternalFactionBaseInfo otherBaseInfo = otherFaction.BaseInfo;
                if ( otherBaseInfo == null )
                    continue; //during game quit

                string allegiance = otherBaseInfo.Allegiance;
                if ( allegiance == team )
                {
                    thisFaction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( thisFaction );
                }
                else if ( allegiance != team )
                {
                    thisFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( thisFaction );
                }
            }
        }
        public static void EnemyThisFactionToPlayers( Faction thisFaction )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;
                if ( otherFaction.Type == FactionType.Player )
                {
                    thisFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( thisFaction );
                }
            }
        }
        public static void AllyThisFactionToHumans( Faction thisFaction )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;
                if ( otherFaction.Type == FactionType.Player ) //ally them to all the human factions
                    AllyThisFactionToOtherFaction( thisFaction, otherFaction );
            }
        }
        public static void AllyThisFactionToAI( Faction thisFaction )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;
                SpecialFactionData otherSpecialData = otherFaction.SpecialFactionData;
                if ( otherSpecialData == null )
                    continue; //during game quit
                ExternalFactionBaseInfo otherBaseInfo = otherFaction.BaseInfo;
                if ( otherBaseInfo == null )
                    continue; //during game quit

                if ( otherFaction.Type == FactionType.AI || otherSpecialData.AlliedToAIByDefault ||
                   otherBaseInfo.Allegiance == "Allied To AI" )
                {
                    thisFaction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( thisFaction );
                }
                else
                {
                    thisFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( thisFaction );
                }
            }
        }
        public static void SetAlliesForScourgeCivilWar( Faction thisFaction )
        {
            //this works very similarly to AllyThisFactionToAI, but I make it a separate function
            //in case it needs additional functionality later
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;
                SpecialFactionData otherSpecialData = otherFaction.SpecialFactionData;
                if ( otherSpecialData == null )
                    continue; //during game quit
                ExternalFactionBaseInfo otherBaseInfo = otherFaction.BaseInfo;
                if ( otherBaseInfo == null )
                    continue; //during game quit

                if ( otherFaction.Type == FactionType.AI || otherSpecialData.AlliedToAIByDefault ||
                   otherBaseInfo.Allegiance == "Allied To AI" )
                {
                    thisFaction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( thisFaction );
                }
                else
                {
                    thisFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( thisFaction );
                }
            }
        }
        public static void AllyThisFactionToEveryoneButPlayers( Faction thisFaction )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;

                if ( otherFaction.Type == FactionType.Player )
                {
                    thisFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( thisFaction );
                    continue;
                }
                else
                {
                    thisFaction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( thisFaction );
                    continue;
                }
            }
        }

        public static void AllyThisFactionToOtherFaction( Faction thisFaction, Faction alliedFaction )
        {
            /* This faction is now an ally of alliedFaction. Every faction alliedFaction is friendly with is now friendly with
               thisFaction, and vica versa */
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( Helper_IsThereASpecificRelationshipToSetAndThenSkip( thisFaction, otherFaction ) )
                    continue;

                if ( alliedFaction.GetIsFriendlyTowards( otherFaction ) || otherFaction == alliedFaction )
                {
                    thisFaction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( thisFaction );
                }
                else if ( alliedFaction.GetIsHostileTowards( otherFaction ) )
                {
                    thisFaction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( thisFaction );
                }
                else if ( alliedFaction.GetIsNeutralTowards( otherFaction ) )
                {
                    thisFaction.MakeNeutralTo( otherFaction );
                    otherFaction.MakeNeutralTo( thisFaction );
                }
            }
        }

        public static void MakeAIHostileToOtherAIs( Faction faction )
        {
            if ( faction == null )
                return;
            if ( faction.Type != FactionType.AI && !FactionUtilityMethods.Instance.IsACoreAISubFaction( faction ) )
            {
                ArcenDebugging.ArcenDebugLog( "Called MakeAIHostileToOtherAIs() on a non-AI faction of type: " + faction.SpecialFactionData.InternalName, Verbosity.ShowAsError );
                return;
            }
            int myParentAIFaction = faction.FactionIndexOfMyParentIfIHaveOne;

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue;
                if ( otherFaction.FactionIndex == myParentAIFaction || //if I am a sub faction and this is my (dead) AI
                     otherFaction.FactionIndexOfMyParentIfIHaveOne == faction.FactionIndex || //if I am the (dead) AI and this is one of my sub-factions
                     (myParentAIFaction != -1 && otherFaction.FactionIndexOfMyParentIfIHaveOne == myParentAIFaction) ) //if I am a sub-faction and this is an allied sub-faction
                    continue;

                if ( otherFaction.Type == FactionType.AI )
                {
                    faction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( faction );
                }
                else if ( FactionUtilityMethods.Instance.IsACoreAISubFaction( otherFaction ) )
                {
                    faction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( faction );
                }
            }
        }

        public static void MakeMeNeutralToAll( Faction faction )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                var fac = World_AIW2.Instance.Factions[i];
                fac.MakeNeutralTo(faction);
                faction.MakeNeutralTo(fac);
            }
        }
        
        public static CommonAllegiances GetCommonAllegiance(this Faction faction)
        {
            var name = faction?.BaseInfo?.Allegiance;
            
            if ( string.IsNullOrWhiteSpace(name) )
                return CommonAllegiances.Unknown;
            
	        if ( name == "Friendly To Players" )
	            return CommonAllegiances.FriendlyToPlayers;
	        if ( name == "Allied To AI" )
                return CommonAllegiances.AlliedToAI;
            if ( name == "Minor Faction Team Red" )
	            return CommonAllegiances.RedTeam;
	        if ( name == "Minor Faction Team Blue" )
	            return CommonAllegiances.BlueTeam;
	        if ( name == "Minor Faction Team Green" )
	            return CommonAllegiances.GreenTeam;
	        if ( name == "Dark Alliance" )
	            return CommonAllegiances.DarkAlliance;
            if ( name == "Hostile To All")
                return CommonAllegiances.HostileToAll;
	        
	        return CommonAllegiances.Unknown;
        }
        
        public static void SetupRelations(Faction faction, CommonAllegiances team)
        {
            switch (team)
            {
                case CommonAllegiances.FriendlyToPlayers:
                    {
                        AllegianceHelper.AllyThisFactionToHumans(faction);
                        break;
                    }
                case CommonAllegiances.AlliedToAI:
                    {
                        AllegianceHelper.AllyThisFactionToAI(faction);
                        break;
                    }
                case CommonAllegiances.RedTeam:
                    {
                        AllegianceHelper.AllyThisFactionToMinorFactionTeam(faction, "Minor Faction Team Red");
                        break;
                    }
                case CommonAllegiances.BlueTeam:
                    {
                        AllegianceHelper.AllyThisFactionToMinorFactionTeam(faction, "Minor Faction Team Blue");
                        break;
                    }
                case CommonAllegiances.GreenTeam:
                    {
                        AllegianceHelper.AllyThisFactionToMinorFactionTeam(faction, "Minor Faction Team Green");
                        break;
                    }
                case CommonAllegiances.DarkAlliance:
                    {
                        AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Dark Alliance" );
                        break;
                    }
                case CommonAllegiances.HostileToAll:
                    {
                        AllegianceHelper.EnemyThisFactionToAll( faction );
                        break;
                    }
                case CommonAllegiances.Neutral:
                    {
                        AllegianceHelper.MakeMeNeutralToAll(faction);
                        break;
                    }
                case CommonAllegiances.Unknown:
                default:
                    {
                        break;
                    }
            }
        }
    }
}
