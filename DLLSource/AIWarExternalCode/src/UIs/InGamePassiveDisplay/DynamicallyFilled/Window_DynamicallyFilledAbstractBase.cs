using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public abstract class Window_DynamicallyFilledAbstractBase : WindowControllerAbstractBase, IInputActionHandler
    {
        protected float topBuffer = -3;
        protected float leftBuffer = 2;
        protected float rowHeight = ROW_HEIGHT_DEFAULT;
        protected float rowBuffer = 1.5f;

        protected const float ROW_HEIGHT_DEFAULT = 24;
        protected const float ROW_HEIGHT_SHORT = 18;
        protected const float ROW_HEIGHT_SPACER = 10;
        protected const float ROW_HEIGHT_TALL_HEADER = 80;

        #region CalculateBoundsSingle
        protected void CalculateBoundsSingle( out Rect soleBounds, ref float runningY, float SoleWidth )
        {
            soleBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, SoleWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsDual
        protected void CalculateBoundsDual( out Rect nameBounds, out Rect valueSettingControlBounds, ref float runningY, float NameWidth, float ValueWidth )
        {
            nameBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, rowHeight );

            valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, ValueWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsTriple
        protected void CalculateBoundsTriple( out Rect nameBounds, out Rect valueSettingControlBounds, out Rect valueDescriptionBounds, ref float runningY, float NameWidth, float ValueWidth, float DescriptionWidth )
        {
            nameBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, rowHeight );

            valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, ValueWidth, rowHeight );

            valueDescriptionBounds = ArcenRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, DescriptionWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsQuadruple
        protected void CalculateBoundsQuadruple( out Rect nameBounds, out Rect valueSettingControlBounds, out Rect valueDescriptionBounds, out Rect fourthBounds, ref float runningY, float NameWidth, float ValueWidth, float DescriptionWidth, float FourthWidth )
        {
            nameBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, rowHeight );

            valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, ValueWidth, rowHeight );

            valueDescriptionBounds = ArcenRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, DescriptionWidth, rowHeight );

            fourthBounds = ArcenRectangle.CreateUnityRect( valueDescriptionBounds.xMax, runningY, FourthWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region Open/Close Stuff

        public abstract void Close();

        //from IInputActionHandler
        public virtual void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    if ( Engine_Universal.CurrentPopups.Count <= 0 )
                    {
                        this.Close();
                        
                        //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                        ArcenInput.BlockForAJustPartOfOneSecond();
                    }
                    break;
            }
        }
        
        #endregion        
    }
}
