using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class EntityTypeDrawingBag_DrawLinkImplementation : EntityTypeDrawingBag_DrawLink
    {
        public override int Draw( 
            EntityTypeDrawingBag Bag, 
            ArcenHostOnlySimContext Context, 
            Faction ForAI_ForAIBudgets, 
            byte MarkToSpawnAt,
            StructList<EntityTypeAndCount> Results,
            EntityTypeDrawingBag_AlternativeFactionMarkMode AlternativeFactionMarkMode = EntityTypeDrawingBag_AlternativeFactionMarkMode.Unused )
        {
            int count = 0;
            
            if ( Bag.UseRandomPickMode )
            {
                int primary;
                for ( int i = 0; i < Bag.RandomPickCount; i++ )
                {
                    primary = Bag.DrawPrimaryEntry( Context );
                    
                    AddToResults( Bag, Results, primary, 
                        Bag.PickOneRandomEntityTypeForTier( ForAI_ForAIBudgets, primary, Context ),
                        ForAI_ForAIBudgets, MarkToSpawnAt, 
                        Context, AlternativeFactionMarkMode );
                }
                
                return count;
            }
            
            List<GameEntityTypeData> temp = null;
            try
            {
                for ( int i = 0; i < Bag.Count; i++ )
                {
                    if ( Bag.MultiPickList[i] > 1 )
                    {
                        if (temp == null)
                            temp = GameEntityTypeData.GetTemporaryGameEntityTypeDataList("EntityTypeDrawingBag_DrawLinkImplementation.Draw.Temp", 5.0f);
                        else
                            temp.Clear();
                        if ( temp == null ) //blocked for teardown/shutdown; bail
                            return count;

                        Bag.FillAllPossibleEntityTypes_ForSpecificIndex( temp, ForAI_ForAIBudgets, i );
                        
                        AddToResults( Bag, Results, i, temp, ForAI_ForAIBudgets, MarkToSpawnAt, Context, AlternativeFactionMarkMode, true );
                    } 
                    else
                    {
                        AddToResults( Bag, Results, i, 
                            Bag.PickOneRandomEntityTypeForTier( ForAI_ForAIBudgets, i, Context ),
                            ForAI_ForAIBudgets, MarkToSpawnAt, 
                            Context, AlternativeFactionMarkMode );
                    }
                }
            }
            finally
            {
                GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataList(temp);
            }

            return count;
        }

        public void AddToResults( EntityTypeDrawingBag Bag, StructList<EntityTypeAndCount> Result, 
            int Index, List<GameEntityTypeData> TypeData, Faction ForAI_ForAIBudgets, byte MarkToSpawnAt,
            ArcenHostOnlySimContext Context, EntityTypeDrawingBag_AlternativeFactionMarkMode AlternativeFactionMarkMode, bool SplitBudget )
        {
            if ( SplitBudget )
            {
                DeleteResultsDownToFittingNumber( TypeData, Bag.MultiPickList[Index], Context, Result, Bag.DuplicateModeList[Index] );
                
                int totalBudget = Bag.GetBudgetFor( Index, Context );
                int budgetPerPiece = totalBudget / TypeData.Count;
                int overflow = totalBudget - (budgetPerPiece * TypeData.Count);
                for(int i = 0; i < TypeData.Count; i++ )
                {
                    AddOrIncreaseIfAlreadyPresent( 
                        Result, 
                        GetDataFor( TypeData[i], Bag.CountTypeList[Index], budgetPerPiece + overflow,
                        AdjustMarkIfNeeded( TypeData[i], ForAI_ForAIBudgets, MarkToSpawnAt, AlternativeFactionMarkMode ), out overflow ) );
                }
                
                return;
            } 
            
            for ( int i = 0; i < TypeData.Count; i++ )
                AddToResults( Bag, Result, Index, TypeData[i], ForAI_ForAIBudgets, MarkToSpawnAt, Context, AlternativeFactionMarkMode );
        }

        public void AddToResults( EntityTypeDrawingBag Bag, StructList<EntityTypeAndCount> Result, int Index, GameEntityTypeData TypeData, Faction ForAI_ForAIBudgets, byte MarkToSpawnAt,
            ArcenHostOnlySimContext Context, EntityTypeDrawingBag_AlternativeFactionMarkMode AlternativeFactionMarkMode )
        {
            if ( !IsAllowedToAddToResults( Result, TypeData, Bag.DuplicateModeList[Index] ) )
                return;
            
            var mark = AdjustMarkIfNeeded( TypeData, ForAI_ForAIBudgets, MarkToSpawnAt, AlternativeFactionMarkMode );
            
            var item = GetDataFor( 
                TypeData, 
                Bag.CountTypeList[Index], 
                Bag.GetBudgetFor( Index, Context ), 
                mark, 
                out _ );
            
            AddOrIncreaseIfAlreadyPresent( Result, item);
        }

        public void DeleteResultsDownToFittingNumber( List<GameEntityTypeData> TypeData, int TargetCount, ArcenHostOnlySimContext Context, StructList<EntityTypeAndCount> Result,
            EntityTypeDrawingBag_DuplicateMode DuplicateMode )
        {
            int theoreticalCount = TypeData.Count;
            if ( DuplicateMode == EntityTypeDrawingBag_DuplicateMode.GenerallyUnallowed )//delete all items that are already in the results list
            {
                for ( int i = 0; i < theoreticalCount; i++ )
                    if ( !IsAllowedToAddToResults( Result, TypeData[i], DuplicateMode ) )
                        PseudoDeleteListItem( TypeData, i, ref theoreticalCount );
            }
            if ( DuplicateMode != EntityTypeDrawingBag_DuplicateMode.GenerallyAllowed )//delete all internal duplicates
            {
                for ( int i = 0; i < theoreticalCount - 1; i++ )
                    for ( int j = i + 1; j < theoreticalCount; j++ )
                        if ( TypeData[i] == TypeData[j] )
                        {
                            PseudoDeleteListItem( TypeData, i, ref theoreticalCount );
                            break;
                        }
            }

            if ( TargetCount >= TypeData.Count )//early out if there's too few items already...
                return;

            //The choice here is simple: Either continue deleting items until the threshold is reached, or build an entirely new list.
            //If there's only few final items required compared to the current size then it's probably worth building a new list
            //If there's a lot of final items required compared to the current size then it's probably worth to just keep deleting
            //Since new lists churn the GC it's *probably* a good judgement call make a ~40% threshold...

            // jcf: It's not just churning the GC, its literally leaking a list here.
            //      List.Create_WillNeverBeGCed should only be called for permanent objects and be held on to permanently as a field.
            //      So, i added 'true' so the first branch is always taken.
            
            if ( true || (TargetCount * 10) / 4 > theoreticalCount )
            {
                while ( theoreticalCount > TargetCount )
                    PseudoDeleteListItem( TypeData, Context.RandomToUse.Next( theoreticalCount ), ref theoreticalCount );
                ManifestPseudoDeletions( TypeData, theoreticalCount );
            }
            else
            {
                List<GameEntityTypeData> types = List<GameEntityTypeData>.Create_WillNeverBeGCed( TargetCount, "DeleteResultsDownToFittingNumber.types" );
                int rnd;
                for ( int i = 0; i < TargetCount; i++ )
                {
                    rnd = Context.RandomToUse.Next( theoreticalCount );
                    types.Add( TypeData[rnd] );
                    PseudoDeleteListItem( TypeData, rnd, ref theoreticalCount );
                }
                TypeData = types;
            }

            ManifestPseudoDeletions( TypeData, theoreticalCount );
        }

        //instead of outright *deleting* items in the middle all the time this code overwrites the "deleted" item with the "current last item" X times and then bulk-removes down to size
        //all of this code for drawing bags is already inefficient enough... This potentially saves a *ton* of list adjustments
        public void PseudoDeleteListItem( List<GameEntityTypeData> List, int Index, ref int TheoreticalCount )
        {
            List[Index] = List[TheoreticalCount--];
        }

        public void ManifestPseudoDeletions( List<GameEntityTypeData> List, int TheoreticalCount )
        {
            if ( List.Count <= TheoreticalCount )
                return;
            List.RemoveRange( TheoreticalCount, List.Count - TheoreticalCount );
        }

        public void AddOrIncreaseIfAlreadyPresent( StructList<EntityTypeAndCount> Result, EntityTypeAndCount CurrentItem )
        {
            for ( int i = 0; i < Result.Count; i++ )
                if ( Result[i].TypeData == CurrentItem.TypeData )
                {
                    //simply increase the count if it's already there. Sadly since it's a struct it's not possible to simply do result[i].count += X
                    Result[i] = new EntityTypeAndCount( CurrentItem.TypeData, CurrentItem.Count + Result[i].Count );
                    return;
                }
            Result.Add( CurrentItem );
        }

        public bool IsAllowedToAddToResults( StructList<EntityTypeAndCount> Result, GameEntityTypeData TypeData, EntityTypeDrawingBag_DuplicateMode DuplicateMode )
        {
            if ( DuplicateMode != EntityTypeDrawingBag_DuplicateMode.GenerallyUnallowed )//delete all items that already are selected
                return true;
            for ( int i = 0; i < Result.Count; i++ )
                if ( Result[i].TypeData == TypeData )
                    return false;
            return true;
        }

        public byte AdjustMarkIfNeeded( GameEntityTypeData TypeData, Faction ForAI_ForAIBudgets, byte MarkToSpawnAt, EntityTypeDrawingBag_AlternativeFactionMarkMode AlternativeFactionMarkMode )
        {
            switch ( AlternativeFactionMarkMode )
            {
                case EntityTypeDrawingBag_AlternativeFactionMarkMode.Unused:
                    break;
                case EntityTypeDrawingBag_AlternativeFactionMarkMode.GeneralMarkLevel:
                    MarkToSpawnAt = ForAI_ForAIBudgets.CurrentGeneralMarkLevel;
                    break;
                case EntityTypeDrawingBag_AlternativeFactionMarkMode.SpecificUnitMarkLevel:
                    MarkToSpawnAt = ForAI_ForAIBudgets.GetGlobalMarkLevelForShipLine( TypeData );
                    break;
                default:
                    throw new NotImplementedException( "Error: Unimplemented AlternativeFactionMarkMode Mode: " + AlternativeFactionMarkMode + "!" );
            }
            if ( MarkToSpawnAt < TypeData.StartingMarkLevel.Ordinal )
                MarkToSpawnAt = TypeData.StartingMarkLevel.Ordinal;
            else if ( MarkToSpawnAt > TypeData.MaxMarkLevel )
                MarkToSpawnAt = TypeData.MaxMarkLevel;

            if ( MarkToSpawnAt > Balance_MarkLevelTable.Instance.MaxOrdinal )
                MarkToSpawnAt = Balance_MarkLevelTable.Instance.MaxOrdinal;
            else if ( MarkToSpawnAt < Balance_MarkLevelTable.Instance.MinOrdinal )
                MarkToSpawnAt = Balance_MarkLevelTable.Instance.MinOrdinal;

            return MarkToSpawnAt;
        }

        //returns the count of ships for the various factors it's based on. The overflow budget will be whatever is left over, and CAN be negative if the budget was't enough for a single unit
        public static EntityTypeAndCount GetDataFor( GameEntityTypeData TypeData, EntityTypeDrawingBag_SpawnMode Mode, int Budget, byte MarkToSpawnAt, out int OverflowBudget )
        {
            int count;
            int factor;
            factor = GetCostFactorForTypeAndCost( Mode, TypeData, MarkToSpawnAt );
            count = Math.Max( 1, Budget / factor );
            OverflowBudget = Budget - (count * factor);
            return new EntityTypeAndCount( TypeData, count );
        }

        public static int GetCostFactorForTypeAndCost( EntityTypeDrawingBag_SpawnMode Mode, GameEntityTypeData TypeData, byte MarkToSpawnAt )
        {
            switch ( Mode )
            {
                case EntityTypeDrawingBag_SpawnMode.RawCount:
                    {
                        return 1;
                    }
                case EntityTypeDrawingBag_SpawnMode.AIBudget:
                    {
                        return TypeData.CostForAIToPurchase;
                    }
                case EntityTypeDrawingBag_SpawnMode.Strength_BaseMark:
                    {
                        return TypeData.GetForMark(0).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    }
                case EntityTypeDrawingBag_SpawnMode.Strength_CurrentMark:
                    {
                        return TypeData.GetForMark(MarkToSpawnAt).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    }
                case EntityTypeDrawingBag_SpawnMode.MetalCost:
                    {
                        return TypeData.GetForMark(MarkToSpawnAt).MetalCost;
                    }
                case EntityTypeDrawingBag_SpawnMode.EnergyCost:
                    {
                        return TypeData.EnergyUsage;
                    }
                default://just in case I forget something, or something new is implemented
                    throw new NotImplementedException( "Error: Unimplemented spawn mode: " + Mode + "!" );
            }
        }
    }

    public static class EntityTypeDrawingBag_Extensions
    {
        public static void FillAllPossibleEntityTypes( this EntityTypeDrawingBag Bag, ListOfLists<GameEntityTypeData> Result, Faction ForAI_ForAIBudgets )
        {
            Result.Clear();
            for ( int i = 0; i < Bag.Count; i++ )
            {
                List<GameEntityTypeData> resultInner;
                if ( Result.OuterListCount > i )
                    resultInner = Result[i];
                else
                    resultInner = Result.AddInnerList();
                
                Bag.FillAllPossibleEntityTypes_ForSpecificIndex( resultInner, ForAI_ForAIBudgets, i );
            }
        }

        public static void FillAllPossibleEntityTypes_ForSpecificIndex( this EntityTypeDrawingBag Bag, List<GameEntityTypeData> Result, Faction ForAI_ForAIBudgets, int Index )
        {
            ObjectList workingBudget = null;
            ObjectList workingGroupCategory = null;
            ObjectList workingCategories = null;
            try
            {
                Result.Clear();
                
                switch ( Bag.ModeList[Index] )
                {
                    case EntityTypeDrawingBag_ParseMode.Disabled:
                        {
                            break;
                        }
                    case EntityTypeDrawingBag_ParseMode.EntityName:
                        {
                            Result.Add( Bag.EntityList[Index] );
                            break;
                        }
                    case EntityTypeDrawingBag_ParseMode.Tag:
                        {
                            List<GameEntityTypeData> workingList = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( Bag.TagList[Index] );
                            if ( workingList != null )
                                for ( int i = 0; i < workingList.Count; i++ )
                                    Result.Add( workingList[i] );
                            break;
                        }
                    case EntityTypeDrawingBag_ParseMode.AIShipGroup:
                        {
                            DrawBag<GameEntityTypeData> workingBag = Bag.AIShipGroupList[Index].DrawBag;
                            for ( int i = 0; i < workingBag.InternalListSize; i++ )
                                Result.Add( workingBag.GetItemByIndex( i ) );
                            break;
                        }
                    case EntityTypeDrawingBag_ParseMode.AIShipGroupCategory:
                        {
                            DrawBag<GameEntityTypeData> workingBag = Bag.AIShipGroupList[Index].DrawBag;
                            for ( int i = 0; i < workingBag.InternalListSize; i++ )
                                Result.Add( workingBag.GetItemByIndex( i ) );
                            break;
                        }
                    case EntityTypeDrawingBag_ParseMode.AIBudgetCategory:
                        {
                            var budgetItems = ForAI_ForAIBudgets.GetAISentinelsCoreData().SentinelInfo.AIType.BudgetItems;
                            
                            if (workingBudget == null)
                                workingBudget = ObjectList.GetTemporary("GetAllPossibleEntities_ForSpecificIndex.workingBudget", 1);
                            else
                                workingBudget.Clear();
                            if ( workingBudget == null ) //blocked for teardown/shutdown; bail
                                return;

                            switch ( Bag.AIBudgetCategoryList[Index] )
                            {
                                case EntityTypeDrawingBag_AIBudgetCategory.Reinforcement:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.Reinforcement]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.Warden:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.Warden]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.PraetorianGuard:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.PraetorianGuard]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.HunterFleet:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.HunterFleet]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.BorderAggression:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.BorderAggression]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.CPA:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.CPA]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.Reconquest:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.Reconquest]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.WormholeInvasion:
                                    {
                                        workingBudget.Add(budgetItems[AIBudgetType.WormholeInvasion]);
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.All:
                                    {
                                        for ( int i = 0; i < budgetItems.Size(); i++ )
                                            workingBudget.Add( budgetItems[(AIBudgetType) i] );
                                        break;
                                    }
                                default:
                                    throw new Exception( "Error: Forgot to implement EntityTypeDrawingBag_AIBudgetCategory " + Bag.AIBudgetCategoryList[Index] );
                            }
                            
                            if (workingGroupCategory == null)
                                workingGroupCategory = ObjectList.GetTemporary("GetAllPossibleEntities_ForSpecificIndex.workingGroupCategory", 1);
                            else
                                workingGroupCategory.Clear();
                            if ( workingGroupCategory == null ) //blocked for teardown/shutdown; bail
                                return;

                            for (int i = 0; i < workingBudget.Count; i++ )
                            {
                                var workingBudgetItem = workingBudget[i] as AIBudgetItem;
                                
                                switch ( Bag.AIBudgetSubCategoryList[Index] )
                                {
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.NormalAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.NormalAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.GuardPostAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.GuardPostAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.DireGuardPostAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.DireGuardPostAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.UnarmedGuardPostAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.UnarmedGuardPostAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.TurretAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.TurretAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.NonTurretDefenseAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.NonTurretDefenseAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.GuardianAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.GuardianAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.WormholeSentinelAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.WormholeSentinelAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.ForcefieldGuardianAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.ForcefieldGuardianAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.DecloakerAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.DecloakerAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.DireGuardianAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.DireGuardianAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.RegularSingularFreakySurprisesAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.RegularSingularFreakySurprisesAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.DireSingularFreakySurprisesAIShipGroup:
                                        {
                                            if ( workingBudgetItem.DireSingularFreakySurprisesAIShipGroup == null  ) //it is valid for dire to not be set, then we just get regular
                                                workingGroupCategory.Add( workingBudgetItem.RegularSingularFreakySurprisesAIShipGroup );
                                            else
                                                workingGroupCategory.Add( workingBudgetItem.DireSingularFreakySurprisesAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.ExoLeaderAIShipGroup:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.ExoLeaderAIShipGroup );
                                            break;
                                        }
                                    case EntityTypeDrawingBag_AIBudgetSubCategory.All:
                                        {
                                            workingGroupCategory.Add( workingBudgetItem.NormalAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.GuardPostAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.DireGuardPostAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.UnarmedGuardPostAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.TurretAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.NonTurretDefenseAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.GuardianAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.WormholeSentinelAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.ForcefieldGuardianAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.DecloakerAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.DireGuardianAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.RegularSingularFreakySurprisesAIShipGroup );
                                            if ( workingBudgetItem.DireSingularFreakySurprisesAIShipGroup != null ) //it is valid for dire singular freaky to be null, ignore if so
                                                workingGroupCategory.Add( workingBudgetItem.DireSingularFreakySurprisesAIShipGroup );
                                            workingGroupCategory.Add( workingBudgetItem.ExoLeaderAIShipGroup );
                                            break;
                                        }
                                    default:
                                        throw new Exception( "Error: Forgot to implement EntityTypeDrawingBag_AIBudgetSubCategory " + Bag.AIBudgetSubCategoryList[Index] );
                                }
                                
                                for (int j = 0; j < workingGroupCategory.Count; j++ )
                                {
                                    var workingGroupBag = (workingGroupCategory[j] as AIShipGroupCategory).DrawBag;
                                    for ( int k = 0; k < workingGroupBag.InternalListSize; k++ )
                                    {
                                        var workingSquadBag = workingGroupBag.GetItemByIndex( k ).DrawBag;
                                        for (int l = 0; l < workingSquadBag.InternalListSize; l++ )
                                            Result.Add( workingSquadBag.GetItemByIndex( l ) );
                                    }
                                }
                            }
                            break;
                        }
                    case EntityTypeDrawingBag_ParseMode.FleetDesignTemplate:
                        {
                            if (workingCategories == null)
                                workingCategories = ObjectList.GetTemporary("GetAllPossibleEntities_ForSpecificIndex.workingCategories", 1 );
                            else
                                workingCategories.Clear();
                            if ( workingCategories == null ) //blocked for teardown/shutdown; bail
                                return;

                            switch ( Bag.FleetDesignTemplateSubCategoryList[Index] )
                            {
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Strike:
                                    {
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Strikecraft );
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Frigate:
                                    {
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Frigates );
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Turret:
                                    {
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Turrets );
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.OtherDefense:
                                    {
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].OtherDefenses );
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Centerpiece:
                                    {
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Centerpieces );
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Civilian:
                                    {
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Civilians );
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.All:
                                    {
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Strikecraft );
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Frigates );
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Turrets );
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].OtherDefenses );
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Centerpieces );
                                        workingCategories.Add( Bag.FleetDesignTemplateList[Index].Civilians );
                                        break;
                                    }
                                default:
                                    throw new Exception( "Error: Forgot to implement EntityTypeDrawingBag_FleetDesignTemplateSubCategory " + Bag.FleetDesignTemplateSubCategoryList[Index] );
                            }
                            
                            for(int i = 0; i < workingCategories.Count; i++ )
                            {
                                var workingBag = (workingCategories[i] as FleetItemCategory).DrawBag;
                                for ( int j = 0; j < workingBag.InternalListSize; j++ )
                                    Result.Add( workingBag.GetItemByIndex( j ).TypeData );
                            }
                            
                            break;
                        }
                    case EntityTypeDrawingBag_ParseMode.SpecialEntityType:
                        {
                            for(int i = 0; i < GameEntityTypeDataTable.Instance.Rows.Count; i++ )
                            {
                                var row = GameEntityTypeDataTable.Instance.Rows[i];
                                if ( row.SpecialType == Bag.SpecialEntityTypeList[i] )
                                    Result.Add( row );
                            }
                            break;
                        }
                    default:
                        throw new Exception( "Error: Forgot to implement EntityTypeDrawingBag_ParseMode " + Bag.ModeList[Index] );
                }
            }
            finally
            {
                ObjectList.ReleaseTemporary(workingBudget);
                ObjectList.ReleaseTemporary(workingGroupCategory);
                ObjectList.ReleaseTemporary(workingCategories);
            }
        }

        public static GameEntityTypeData PickOneRandomEntityTypeForTier(this EntityTypeDrawingBag Bag, Faction ForAI_ForAIBudgets, int Index, ArcenHostOnlySimContext Context)
        {
            int debug = 0;
            try
            {
                switch ( Bag.ModeList[Index] )
                {
                    case EntityTypeDrawingBag_ParseMode.Disabled:
                        {
                            throw new Exception( "Error: Tried to draw from " + Bag.InternalName + " but it was using \"Disabled\" as mode!" );
                        }
                    case EntityTypeDrawingBag_ParseMode.EntityName:
                        {
                            debug = 1000;
                            return Bag.EntityList[Index];
                        }
                    case EntityTypeDrawingBag_ParseMode.Tag:
                        {
                            debug = 2000;
                            return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Bag.TagList[Index] );
                        }
                    case EntityTypeDrawingBag_ParseMode.AIShipGroup:
                        {
                            return Bag.AIShipGroupList[Index].DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                        }
                    case EntityTypeDrawingBag_ParseMode.AIShipGroupCategory:
                        {
                            return Bag.AIShipGroupCategoryList[Index].DrawBag.PickRandomItemAndReplace( Context.RandomToUse ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                        }
                    case EntityTypeDrawingBag_ParseMode.AIBudgetCategory:
                        {
                            if ( ForAI_ForAIBudgets.BaseInfo.AttachedFaction.FactionIndexOfMyParentIfIHaveOne >= 0 )
                                ForAI_ForAIBudgets = ForAI_ForAIBudgets.BaseInfo.AttachedFaction.GetParentFactionOrNull();
                            if ( ForAI_ForAIBudgets.Type != FactionType.AI )
                                throw new Exception( "Error: No AI to spawn for was passed with parse mode " + Bag.ModeList + ", instead the faction was a " + ForAI_ForAIBudgets.Type + "!" );
                            EnumIndexedArray<AIBudgetType, AIBudgetItem> budgetItems = ForAI_ForAIBudgets.GetAISentinelsCoreData().SentinelInfo.AIType.BudgetItems;
                            AIBudgetItem chosenBudget;
                            switch ( Bag.AIBudgetCategoryList[Index] )
                            {
                                case EntityTypeDrawingBag_AIBudgetCategory.Reinforcement:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.Reinforcement];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.Warden:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.Warden];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.PraetorianGuard:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.PraetorianGuard];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.HunterFleet:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.HunterFleet];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.BorderAggression:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.BorderAggression];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.CPA:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.CPA];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.Reconquest:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.Reconquest];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.WormholeInvasion:
                                    {
                                        chosenBudget = budgetItems[AIBudgetType.WormholeInvasion];
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetCategory.All:
                                    {
                                        chosenBudget = budgetItems[(AIBudgetType) Context.RandomToUse.Next( budgetItems.Size() )];
                                        break;
                                    }
                                default://just in case I forget something, or something new is implemented
                                    throw new NotImplementedException( "Error: Unimplemented AIBudgetCategory: " + Bag.AIBudgetCategoryList[Index] + "!" );
                            }
                            if ( chosenBudget == null )
                                throw new Exception( "Error: No budget for " + Bag.AIBudgetCategoryList[Index] + "!" );
                            EntityTypeDrawingBag_AIBudgetSubCategory category = Bag.AIBudgetSubCategoryList[Index];
                            if ( category == EntityTypeDrawingBag_AIBudgetSubCategory.All )//randomize the sub-categories
                                category = (EntityTypeDrawingBag_AIBudgetSubCategory) Context.RandomToUse.Next( (int) EntityTypeDrawingBag_AIBudgetSubCategory.All );
                            AIShipGroupCategory chosenCategory;
                            switch ( category )
                            {
                                case EntityTypeDrawingBag_AIBudgetSubCategory.NormalAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.NormalAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.GuardPostAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.GuardPostAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DireGuardPostAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.DireGuardPostAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.UnarmedGuardPostAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.UnarmedGuardPostAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.TurretAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.TurretAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.NonTurretDefenseAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.NonTurretDefenseAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.GuardianAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.GuardianAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.WormholeSentinelAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.WormholeSentinelAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.ForcefieldGuardianAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.ForcefieldGuardianAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DecloakerAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.DecloakerAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DireGuardianAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.DireGuardianAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.RegularSingularFreakySurprisesAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.RegularSingularFreakySurprisesAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DireSingularFreakySurprisesAIShipGroup:
                                    {
                                        if ( chosenBudget.DireSingularFreakySurprisesAIShipGroup == null ) //it is valid for dire to not be set, then we just get regular
                                            chosenCategory = chosenBudget.RegularSingularFreakySurprisesAIShipGroup;
                                        else
                                            chosenCategory = chosenBudget.DireSingularFreakySurprisesAIShipGroup;
                                        break;
                                    }
                                case EntityTypeDrawingBag_AIBudgetSubCategory.ExoLeaderAIShipGroup:
                                    {
                                        chosenCategory = chosenBudget.ExoLeaderAIShipGroup;
                                        break;
                                    }
                                default://just in case I forget something, or something new is implemented
                                    throw new NotImplementedException( "Error: Unimplemented AIBudgetSubCategory: " + category + "!" );
                            }
                            if ( chosenCategory == null )
                                throw new Exception( "Error: No budget defined for " + category + "!" );
                            return chosenCategory.DrawBag.PickRandomItemAndReplace( Context.RandomToUse ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                        }
                    case EntityTypeDrawingBag_ParseMode.FleetDesignTemplate:
                        {
                            EntityTypeDrawingBag_FleetDesignTemplateSubCategory subcategory;
                            FleetItemCategory chosenCategory;
                            subcategory = Bag.FleetDesignTemplateSubCategoryList[Index];
                            if ( subcategory == EntityTypeDrawingBag_FleetDesignTemplateSubCategory.All )
                            {
                                subcategory = (EntityTypeDrawingBag_FleetDesignTemplateSubCategory) Context.RandomToUse.Next( (int) EntityTypeDrawingBag_FleetDesignTemplateSubCategory.All );
                            }
                            switch ( subcategory )
                            {
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Strike:
                                    {
                                        chosenCategory = Bag.FleetDesignTemplateList[Index].Strikecraft;
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Frigate:
                                    {
                                        chosenCategory = Bag.FleetDesignTemplateList[Index].Frigates;
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Turret:
                                    {
                                        chosenCategory = Bag.FleetDesignTemplateList[Index].Turrets;
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.OtherDefense:
                                    {
                                        chosenCategory = Bag.FleetDesignTemplateList[Index].OtherDefenses;
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Centerpiece:
                                    {
                                        chosenCategory = Bag.FleetDesignTemplateList[Index].Centerpieces;
                                        break;
                                    }
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Civilian:
                                    {
                                        chosenCategory = Bag.FleetDesignTemplateList[Index].Civilians;
                                        break;
                                    }
                                default://just in case I forget something, or something new is implemented
                                    throw new NotImplementedException( "Error: Unimplemented FleetDesignTemplateSubCategory: " + subcategory + "!" );
                            }
                            return chosenCategory.DrawBag.PickRandomItemAndReplace( Context.RandomToUse ).TypeData;
                        }
                    case EntityTypeDrawingBag_ParseMode.SpecialEntityType:
                        {
                            return GameEntityTypeDataTable.Instance.GetRandomRowOfSpecialType( Context, Bag.SpecialEntityTypeList[Index] );
                        }
                    default://just in case I forget something, or something new is implemented
                        throw new NotImplementedException( "Error: Unimplemented Drawing Mode: " + Bag.ModeList + "!" );
                }
            } catch (Exception e)
            {
                throw new Exception( "Error at " + debug + ": " + e.Message );
            }
        }
    }
}
