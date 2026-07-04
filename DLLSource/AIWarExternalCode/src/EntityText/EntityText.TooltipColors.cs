using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public static class TooltipColors
    {
        public static Color Grey = ColorMath.HexToColor("cccccc");
        public static Color Dim_Grey = ColorMath.HexToColor("aaaaaa");
        public static Color Dim_Green = ColorMath.HexToColor("118811");
        public static Color Green = ColorMath.HexToColor("33ff33");
        public static Color Yellow = ColorMath.HexToColor("eeee33");
        public static Color Normal_White = ColorMath.HexToColor("eeeeee");
        public static Color Light_Yellow = ColorMath.HexToColor("ffdf72");
        public static Color Red = ColorMath.HexToColor("f25e1c");
        public static Color Yellow2 = ColorMath.HexToColor("ffdf72");
        
        public static Color Description = Dim_Grey;
        
        public static Color WeaponSystem_Debug = Green;
        public static Color WeaponSystem_Debug_Dim = Dim_Green;
        public static Color WeaponSystem_Activity = Dim_Grey;
        public static Color WeaponSystem_Status_Off = Light_Yellow;
        
        public static Color System_Term = Red;
        public static Color System_Term_Desc = Grey;
        public static Color System_Term_Desc_Num = Yellow2;

        public static Color CustomSystem_Status_Cooldown = ColorMath.HexToColor("f54254");
        public static Color CustomSystem_Status_Ready = ColorMath.HexToColor("fffc38");
        public static Color CustomSystem_Status_On = ColorMath.HexToColor("86eb34");
        public static Color CustomSystem_Time = ColorMath.HexToColor("ff5dff");
        
        // placeholders, same color as Time currently
        public static Color CustomSystem_Adjust_Speed = ColorMath.HexToColor("ff5dff");
        public static Color CustomSystem_Adjust_MaxHull = ColorMath.HexToColor("ff5dff");
        public static Color CustomSystem_Adjust_MaxShield = ColorMath.HexToColor("ff5dff");
        public static Color CustomSystem_Adjust_Damage = ColorMath.HexToColor("ff5dff");

        public static Color Charges_Title = Normal_White;
        public static Color Charges_Text = Normal_White;
        
        public static Color Error = ColorMath.HexToColor("ff5842");
    }
}