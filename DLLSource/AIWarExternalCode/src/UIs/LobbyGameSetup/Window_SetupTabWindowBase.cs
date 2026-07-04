
using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public abstract class Window_SetupTabWindowBase : Window_SetupTabWithChatWindowBase
    {
        protected float topBuffer = 3;
        protected float leftBuffer = 2;
        protected float rowHeight = 24;
        protected float rowBuffer = 1.5f;

        #region CalculateBoundsSingle
        protected void CalculateBoundsSingle( out Rect soleBounds, ref float runningY, float SoleWidth )
        {
            soleBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, SoleWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsSingleCustomHeight
        protected void CalculateBoundsSingleCustomHeight( out Rect soleBounds, ref float runningY, float SoleWidth, float customHeight, float customBuffer )
        {
            soleBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, SoleWidth, customHeight );

            runningY += customHeight + customBuffer;
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

        #region CalculateBoundsDualCustomHeight
        protected void CalculateBoundsDualCustomHeight( out Rect nameBounds, out Rect valueSettingControlBounds, ref float runningY, float NameWidth, float ValueWidth, float customHeight, float customBuffer )
        {
            nameBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, customHeight );

            valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, ValueWidth, customHeight );

            runningY += customHeight + customBuffer;
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

        protected abstract void PopulateSubclassControls( ArcenUI_SetOfCreateElementDirectives Set, ref float runningY );
    }
}
