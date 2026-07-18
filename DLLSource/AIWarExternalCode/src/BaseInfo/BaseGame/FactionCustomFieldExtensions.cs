using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    ///This is a convenient way of converting from the old format into the new.
    ///There are other ways you could call these things directly, but for now
    ///there's a certain amount of excellence in mirroring the past.

    public static class FactionCustomFieldExtensions
    {
        #region CustomData_NumberToSeed
        public static int CustomData_NumberToSeed( this Faction ParentObject, bool ErrorOnMissingField )
        {
            return ParentObject.GetIntValueForCustomFieldOrDefaultValue( "NumberToSeed", ErrorOnMissingField );
        }
        #endregion

        #region CustomData_WormholeBorerIncomeModifier
        public static FInt CustomData_WormholeBorerIncomeModifier( this Faction ParentObject, bool ErrorOnMissingField )
        {
            string fieldValue = ParentObject.GetStringValueForCustomFieldOrDefaultValue( "WormholeBorerIncomeModifier", ErrorOnMissingField );
            if ( fieldValue == "Disabled" )
                return FInt.Zero;
            if ( fieldValue == "Slower" )
                return FInt.FromParts( 0, 500 );
            if ( fieldValue == "Normal" )
                return FInt.One;
            if ( fieldValue == "Faster" )
                return FInt.FromParts( 1, 500 );
            if ( fieldValue == "Very Fast" )
                return FInt.FromParts( 2, 000 );
            if ( fieldValue.Length == 0 ) //older saves, etc
                return FInt.One;
            if ( ErrorOnMissingField )
                ArcenDebugging.ArcenDebugLog( "CustomData_WormholeBorerIncomeModifier: Unknown field value '" + fieldValue + "'", Verbosity.ShowAsError );
            return FInt.One;
        }
        #endregion

        #region CustomData_StartingAIPlanetRatio
        public static AIStartSize CustomData_StartingAIPlanetRatio( this Faction ParentObject )
        {
            //no chance of erroring, since you have to have DLC2 in for this to be a thing
            string fieldValue = ParentObject.GetStringValueForCustomFieldOrDefaultValue( "StartingAIPlanetRatio", false ); 
            if ( fieldValue == "Very Small" )
                return AIStartSize.VerySmall;
            if ( fieldValue == "Small" )
                return AIStartSize.Small;
            if ( fieldValue == "Normal" )
                return AIStartSize.Normal;
            if ( fieldValue == "Large" )
                return AIStartSize.Large;
            if ( fieldValue == "Huge" )
                return AIStartSize.Huge;
            if ( fieldValue.Length == 0 ) //older saves, etc
                return AIStartSize.Normal;
            return AIStartSize.Normal;
        }
        #endregion

        #region CustomData_StartingAILayout
        public static AIOwnershipLayout CustomData_StartingAILayout( this Faction ParentObject )
        {
            string fieldValue = ParentObject.GetStringValueForCustomFieldOrDefaultValue( "StartingAILayout", false );
            if ( fieldValue == "小簇" || fieldValue == "Normal" )
                return AIOwnershipLayout.SmallCluster;
            if ( fieldValue == "大簇" )
                return AIOwnershipLayout.LargeCluster;
            if ( fieldValue == "随机大小簇" )
                return AIOwnershipLayout.RandomCluster;
            if ( fieldValue == "随机" )
                return AIOwnershipLayout.Random;
            if ( fieldValue.Length == 0 ) //older saves, etc
                return AIOwnershipLayout.SmallCluster;
            return AIOwnershipLayout.SmallCluster;
        }
        #endregion

        #region GetAntiMinorFactionWaveDataOrNull
        public static AntiMinorFactionWaveData GetAntiMinorFactionWaveDataOrNull( this Faction ParentObject )
        {
            if ( ParentObject.BaseInfo.GetDoesThisImplementInterface( typeof( IExoDataHolder ) ) )
                return ((IAntiMinorFactionWaveDataHolder)ParentObject.BaseInfo).GetAntiMinorFactionWaveData();
            return null;
        }
        #endregion
    }
}
