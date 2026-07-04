using System;
using Arcen.AIW2.Core;
using Arcen.Universal;


namespace Arcen.AIW2.External
{
    #region Support Types
    public enum TimeIntensity {
        None,
        OneMinute,
        TenMinutes,
    };
    
    public struct ListToAppend
    {
        private readonly ArcenCharacterBufferBase _buffer;
        private int _count;
        private string _delim;
        private string _term;
        
        public ListToAppend(ArcenCharacterBufferBase buffer, string delim, string term)
        {
            _buffer = buffer;
            _count = 0;
            _delim = delim;
            _term = term;
        }
        
        public ArcenCharacterBufferBase Item()
        {
            if (_count > 0)
                _buffer.Add(_delim);
            
            _count++;
            return _buffer;
        }
        
        public ArcenCharacterBufferBase Done()
        {
            if (_count > 0)
                _buffer.Add(_term);
            return _buffer;
        }
    }
    #endregion

    public static class ExtensionMethodsFor_ArcenCharacterBufferBase
    {
        #region AdditionsToAdd

        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase Buffer, string[] Items )
        {
            for ( int i = 0; i < Items.Length; i++ )
            {
                Buffer.Add( Items[i] );
            }
            return Buffer;
        }

        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase Buffer, List<string> Items )
        {
            for ( int i = 0; i < Items.Count; i++ )
            {
                Buffer.Add( Items[i] );
            }
            return Buffer;
        }

        #endregion

        #region Delimited Lists
        public static ListToAppend List( this ArcenCharacterBufferBase buffer, string delim, string term )
        {
            return new ListToAppend(buffer, delim, term);
        }
        #endregion

        #region NumberFormating

        public static ArcenCharacterBufferBase AddPercentFormated( this ArcenCharacterBufferBase Buffer, int Value, int Max )
        {
            return Buffer.AddPercentFormated( FInt.FromParts( Value, 0 ).ToPercent( Max ) );
        }

        public static ArcenCharacterBufferBase AddPercentFormated( this ArcenCharacterBufferBase Buffer, FInt Percent )
        {
            return Buffer.Add( Percent ).Add( "%" );
        }

        public static ArcenCharacterBufferBase AddPercentRounded( this ArcenCharacterBufferBase Buffer, int Value, int Max, int MaxDecimals )
        {
            return Buffer.AddPercentFormated( FInt.FromParts( Value, 0 ).ToPercent( Max ).ToRounded( MaxDecimals ) );
        }

        public static ArcenCharacterBufferBase AddPercentRounded( this ArcenCharacterBufferBase Buffer, FInt Percent, int MaxDecimals )
        {
            return Buffer.AddPercentFormated( Percent.ToRounded( MaxDecimals ) );
        }

        public static ArcenCharacterBufferBase AddPercentRoundedDynamically( this ArcenCharacterBufferBase Buffer, int Value, int Max )
        {
            return Buffer.AddPercentFormated( FInt.FromParts( Value, 0 ).ToPercent( Max ).ToRoundedDynamic() );
        }

        public static ArcenCharacterBufferBase AddPercentRoundedDynamically( this ArcenCharacterBufferBase Buffer, FInt Percent )
        {
            return Buffer.AddPercentFormated( Percent.ToRoundedDynamic() );
        }

        public static ArcenCharacterBufferBase AddFIntTruncated( this ArcenCharacterBufferBase Buffer, FInt Value )
        {
            return Buffer.AddNumberTruncated( Value );
        }

        public static ArcenCharacterBufferBase AddNumberTruncated( this ArcenCharacterBufferBase Buffer, FInt count )
        {
            string magnitude = null;
            bool negative = count < 0;
            if ( negative )
            {
                count *= -1;
            }
            
            if ( count.IntValue >= 1000 && count.IntValue >= ArcenExternalUIUtilities.PreventMagnitudeFormatingIfLowerThan )
            {
                int mod = 0;
                while ( mod < ArcenExternalUIUtilities.UsedMagnitudeFormating.Length && count.IntValue >= 1000 )
                {
                    count /= 1000;
                    mod++;
                }
                magnitude = ArcenExternalUIUtilities.UsedMagnitudeFormating[mod];
            }
            
            if ( count.IntValue >= 100 )
            {
                count = FInt.Create( (count.RawValue / 1000) * 1000, false );
            } 
            else 
            if ( count.IntValue >= 10 )
            {
                count = FInt.Create( (count.RawValue / 100) * 100, false );
            } 
            else 
            if ( count.IntValue >= 1 )
            {
                count = FInt.Create( (count.RawValue / 10) * 10, false );
            }
            
            if ( negative )
            {
                count *= -1;
            }
            Buffer.Add( count );
            
            if ( magnitude != null )
            {
                Buffer.Add( magnitude );
            }
            
            return Buffer;
        }
        
        public static ArcenCharacterBufferBase AddNumberTruncated( this ArcenCharacterBufferBase Buffer, float count )
        {
            return Buffer.AddNumberTruncated(count.ToFInt());
        }

        public static ArcenCharacterBufferBase AddNumberTruncated( this ArcenCharacterBufferBase Buffer, int count )
        {
            if(System.Math.Abs(count) < 1000)
            {
                Buffer.Add( count );
            } else
            {
                AddNumberTruncated( Buffer, FInt.Create( count, true ) );
            }
            return Buffer;
        }

        // AddNumberMoreReadable(int/long/double/FInt) and the (value, hexcolor) variants
        // were promoted to instance methods on Arcen.Universal.ArcenCharacterBufferBase so
        // they are reachable from ArcenAIW2Core (which can't reference AIWarExternalCode).
        // The TextStyle("Infinity") styling for the MaxValue sentinel is dropped 鈥?base
        // class can't see TextStyle; output is plain "鈭?.

        public static ArcenCharacterBufferBase AddPercent( this ArcenCharacterBufferBase Buffer, FInt Amount )
        {
            FInt perc = Amount.ToPercent(1);// - 100.ToFInt();

            Buffer
                .Open(TextStyle.Number)
                .Add(perc).Add("%")
                .Close(TextStyle.Number);
            
            return Buffer;
        }

        #endregion

        #region CommonResourceFormating

        #region Metal
        public static ArcenCharacterBufferBase StartMetal( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Metal, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddMetal( this ArcenCharacterBufferBase Buffer, int Metal, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMetal(startWithIcon, overwriteColorOrNull).Add( Metal ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMetal( this ArcenCharacterBufferBase Buffer, FInt Metal, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMetal( startWithIcon, overwriteColorOrNull ).Add( Metal ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMetal_MoreReadable( this ArcenCharacterBufferBase Buffer, int Metal, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMetal( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Metal ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMetal_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Metal, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMetal( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Metal ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMetal_Truncated( this ArcenCharacterBufferBase Buffer, int Metal, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMetal( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Metal ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMetal_Truncated( this ArcenCharacterBufferBase Buffer, FInt Metal, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMetal( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Metal ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMetal( this ArcenCharacterBufferBase Buffer, string Metal, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMetal( startWithIcon, overwriteColorOrNull ).Add( Metal ).EndColor();
        }

        #endregion

        #region Energy
        public static ArcenCharacterBufferBase StartEnergy( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Energy, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddEnergy( this ArcenCharacterBufferBase Buffer, int Energy, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEnergy( startWithIcon, overwriteColorOrNull ).Add( Energy ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEnergy( this ArcenCharacterBufferBase Buffer, FInt Energy, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEnergy( startWithIcon, overwriteColorOrNull ).Add( Energy ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEnergy_MoreReadable( this ArcenCharacterBufferBase Buffer, int Energy, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEnergy( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Energy ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEnergy_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Energy, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEnergy( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Energy ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEnergy_Truncated( this ArcenCharacterBufferBase Buffer, int Energy, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEnergy( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Energy ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEnergy_Truncated( this ArcenCharacterBufferBase Buffer, FInt Energy, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEnergy( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Energy ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEnergy( this ArcenCharacterBufferBase Buffer, string Energy, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEnergy( startWithIcon, overwriteColorOrNull ).Add( Energy ).EndColor();
        }

        #endregion

        #region Argon
        public static ArcenCharacterBufferBase StartArgon( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Argon, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddArgon( this ArcenCharacterBufferBase Buffer, int Argon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArgon( startWithIcon, overwriteColorOrNull ).Add( Argon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArgon( this ArcenCharacterBufferBase Buffer, FInt Argon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArgon( startWithIcon, overwriteColorOrNull ).Add( Argon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArgon_MoreReadable( this ArcenCharacterBufferBase Buffer, int Argon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArgon( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Argon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArgon_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Argon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArgon( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Argon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArgon_Truncated( this ArcenCharacterBufferBase Buffer, int Argon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArgon( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Argon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArgon_Truncated( this ArcenCharacterBufferBase Buffer, FInt Argon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArgon( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Argon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArgon( this ArcenCharacterBufferBase Buffer, string Argon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArgon( startWithIcon, overwriteColorOrNull ).Add( Argon ).EndColor();
        }

        #endregion

        #region Radon
        public static ArcenCharacterBufferBase StartRadon( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Radon, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddRadon( this ArcenCharacterBufferBase Buffer, int Radon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRadon( startWithIcon, overwriteColorOrNull ).Add( Radon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddRadon( this ArcenCharacterBufferBase Buffer, FInt Radon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRadon( startWithIcon, overwriteColorOrNull ).Add( Radon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddRadon_MoreReadable( this ArcenCharacterBufferBase Buffer, int Radon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRadon( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Radon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddRadon_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Radon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRadon( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Radon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddRadon_Truncated( this ArcenCharacterBufferBase Buffer, int Radon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRadon( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Radon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddRadon_Truncated( this ArcenCharacterBufferBase Buffer, FInt Radon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRadon( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Radon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddRadon( this ArcenCharacterBufferBase Buffer, string Radon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRadon( startWithIcon, overwriteColorOrNull ).Add( Radon ).EndColor();
        }

        #endregion

        #region Xenon
        public static ArcenCharacterBufferBase StartXenon( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Xenon, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddXenon( this ArcenCharacterBufferBase Buffer, int Xenon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartXenon( startWithIcon, overwriteColorOrNull ).Add( Xenon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddXenon( this ArcenCharacterBufferBase Buffer, FInt Xenon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartXenon( startWithIcon, overwriteColorOrNull ).Add( Xenon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddXenon_MoreReadable( this ArcenCharacterBufferBase Buffer, int Xenon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartXenon( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Xenon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddXenon_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Xenon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartXenon( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Xenon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddXenon_Truncated( this ArcenCharacterBufferBase Buffer, int Xenon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartXenon( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Xenon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddXenon_Truncated( this ArcenCharacterBufferBase Buffer, FInt Xenon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartXenon( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Xenon ).EndColor();
        }

        public static ArcenCharacterBufferBase AddXenon( this ArcenCharacterBufferBase Buffer, string Xenon, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartXenon( startWithIcon, overwriteColorOrNull ).Add( Xenon ).EndColor();
        }

        #endregion

        #region Science
        public static ArcenCharacterBufferBase StartScience( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Science, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddScience( this ArcenCharacterBufferBase Buffer, int Science, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartScience( startWithIcon, overwriteColorOrNull ).Add( Science ).EndColor();
        }

        public static ArcenCharacterBufferBase AddScience( this ArcenCharacterBufferBase Buffer, FInt Science, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartScience( startWithIcon, overwriteColorOrNull ).Add( Science ).EndColor();
        }

        public static ArcenCharacterBufferBase AddScience_MoreReadable( this ArcenCharacterBufferBase Buffer, int Science, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartScience( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Science ).EndColor();
        }

        public static ArcenCharacterBufferBase AddScience_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Science, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartScience( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Science ).EndColor();
        }

        public static ArcenCharacterBufferBase AddScience_Truncated( this ArcenCharacterBufferBase Buffer, int Science, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartScience( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Science ).EndColor();
        }

        public static ArcenCharacterBufferBase AddScience_Truncated( this ArcenCharacterBufferBase Buffer, FInt Science, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartScience( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Science ).EndColor();
        }

        public static ArcenCharacterBufferBase AddScience( this ArcenCharacterBufferBase Buffer, string Science, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartScience( startWithIcon, overwriteColorOrNull ).Add( Science ).EndColor();
        }

        #endregion

        #region Hacking
        public static ArcenCharacterBufferBase StartHacking( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Hacking, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddHacking( this ArcenCharacterBufferBase Buffer, int Hacking, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHackingWrapper( startWithIcon, overwriteColorOrNull ).Add( Hacking ).EndHackingWrapper(false);
        }

        public static ArcenCharacterBufferBase AddHacking( this ArcenCharacterBufferBase Buffer, FInt Hacking, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHacking( startWithIcon, overwriteColorOrNull ).Add( Hacking ).EndHackingWrapper(false);
        }

        public static ArcenCharacterBufferBase AddHacking_MoreReadable( this ArcenCharacterBufferBase Buffer, int Hacking, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHacking( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Hacking ).EndHackingWrapper(false);
        }

        public static ArcenCharacterBufferBase AddHacking_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Hacking, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHacking( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Hacking ).EndHackingWrapper(false);
        }

        public static ArcenCharacterBufferBase AddHacking_Truncated( this ArcenCharacterBufferBase Buffer, int Hacking, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHacking( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Hacking ).EndHackingWrapper(false);
        }

        public static ArcenCharacterBufferBase AddHacking_Truncated( this ArcenCharacterBufferBase Buffer, FInt Hacking, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHacking( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Hacking ).EndHackingWrapper(false);
        }

        public static ArcenCharacterBufferBase AddHacking( this ArcenCharacterBufferBase Buffer, string Hacking, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHacking( startWithIcon, overwriteColorOrNull ).Add( Hacking ).EndHackingWrapper(false);
        }

        #endregion

        #region ResourceOne
        public static ArcenCharacterBufferBase StartResourceOne( this ArcenCharacterBufferBase Buffer, bool startWithIcon )
        {
            Faction f = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( startWithIcon && f != null && f.Resource1TextColorAndIcon.Length > 0 )
                Buffer.Add( f.Resource1TextColorAndIcon );
            return Buffer.StartColor( f != null && f.Resource1Color.Length > 0 ? f.Resource1Color : "ffffff" );
        }

        public static ArcenCharacterBufferBase AddResourceOne( this ArcenCharacterBufferBase Buffer, int ResourceOne, bool startWithIcon )
        {
            return Buffer.StartResourceOne( startWithIcon ).Add( ResourceOne ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceOne( this ArcenCharacterBufferBase Buffer, FInt ResourceOne, bool startWithIcon )
        {
            return Buffer.StartResourceOne( startWithIcon ).Add( ResourceOne ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceOne_MoreReadable( this ArcenCharacterBufferBase Buffer, int ResourceOne, bool startWithIcon )
        {
            return Buffer.StartResourceOne( startWithIcon ).AddNumberMoreReadable( ResourceOne ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceOne_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt ResourceOne, bool startWithIcon )
        {
            return Buffer.StartResourceOne( startWithIcon ).AddNumberMoreReadable( ResourceOne ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceOne_Truncated( this ArcenCharacterBufferBase Buffer, int ResourceOne, bool startWithIcon )
        {
            return Buffer.StartResourceOne( startWithIcon ).AddNumberTruncated( ResourceOne ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceOne_Truncated( this ArcenCharacterBufferBase Buffer, FInt ResourceOne, bool startWithIcon )
        {
            return Buffer.StartResourceOne( startWithIcon ).AddNumberTruncated( ResourceOne ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceOne( this ArcenCharacterBufferBase Buffer, string ResourceOne, bool startWithIcon )
        {
            return Buffer.StartResourceOne( startWithIcon ).Add( ResourceOne ).EndColor();
        }

        #endregion

        #region ResourceTwo
        public static ArcenCharacterBufferBase StartResourceTwo( this ArcenCharacterBufferBase Buffer, bool startWithIcon )
        {
            Faction f = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( startWithIcon && f != null && f.Resource2TextColorAndIcon.Length > 0 )
                Buffer.Add( f.Resource2TextColorAndIcon );
            return Buffer.StartColor( f != null && f.Resource2Color.Length > 0 ? f.Resource2Color : "ffffff" );
        }

        public static ArcenCharacterBufferBase AddResourceTwo( this ArcenCharacterBufferBase Buffer, int ResourceTwo, bool startWithIcon )
        {
            return Buffer.StartResourceTwo( startWithIcon ).Add( ResourceTwo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceTwo( this ArcenCharacterBufferBase Buffer, FInt ResourceTwo, bool startWithIcon )
        {
            return Buffer.StartResourceTwo( startWithIcon ).Add( ResourceTwo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceTwo_MoreReadable( this ArcenCharacterBufferBase Buffer, int ResourceTwo, bool startWithIcon )
        {
            return Buffer.StartResourceTwo( startWithIcon ).AddNumberMoreReadable( ResourceTwo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceTwo_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt ResourceTwo, bool startWithIcon )
        {
            return Buffer.StartResourceTwo( startWithIcon ).AddNumberMoreReadable( ResourceTwo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceTwo_Truncated( this ArcenCharacterBufferBase Buffer, int ResourceTwo, bool startWithIcon )
        {
            return Buffer.StartResourceTwo( startWithIcon ).AddNumberTruncated( ResourceTwo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceTwo_Truncated( this ArcenCharacterBufferBase Buffer, FInt ResourceTwo, bool startWithIcon )
        {
            return Buffer.StartResourceTwo( startWithIcon ).AddNumberTruncated( ResourceTwo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceTwo( this ArcenCharacterBufferBase Buffer, string ResourceTwo, bool startWithIcon )
        {
            return Buffer.StartResourceTwo( startWithIcon ).Add( ResourceTwo ).EndColor();
        }

        #endregion

        #region ResourceThree
        public static ArcenCharacterBufferBase StartResourceThree( this ArcenCharacterBufferBase Buffer, bool startWithIcon )
        {
            Faction f = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( startWithIcon && f != null && f.Resource3TextColorAndIcon.Length > 0 )
                Buffer.Add( f.Resource3TextColorAndIcon );
            return Buffer.StartColor( f != null && f.Resource3Color.Length > 0 ? f.Resource3Color : "ffffff" );
        }

        public static ArcenCharacterBufferBase AddResourceThree( this ArcenCharacterBufferBase Buffer, int ResourceThree, bool startWithIcon )
        {
            return Buffer.StartResourceThree( startWithIcon ).Add( ResourceThree ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceThree( this ArcenCharacterBufferBase Buffer, FInt ResourceThree, bool startWithIcon )
        {
            return Buffer.StartResourceThree( startWithIcon ).Add( ResourceThree ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceThree_MoreReadable( this ArcenCharacterBufferBase Buffer, int ResourceThree, bool startWithIcon )
        {
            return Buffer.StartResourceThree( startWithIcon ).AddNumberMoreReadable( ResourceThree ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceThree_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt ResourceThree, bool startWithIcon )
        {
            return Buffer.StartResourceThree( startWithIcon ).AddNumberMoreReadable( ResourceThree ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceThree_Truncated( this ArcenCharacterBufferBase Buffer, int ResourceThree, bool startWithIcon )
        {
            return Buffer.StartResourceThree( startWithIcon ).AddNumberTruncated( ResourceThree ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceThree_Truncated( this ArcenCharacterBufferBase Buffer, FInt ResourceThree, bool startWithIcon )
        {
            return Buffer.StartResourceThree( startWithIcon ).AddNumberTruncated( ResourceThree ).EndColor();
        }

        public static ArcenCharacterBufferBase AddResourceThree( this ArcenCharacterBufferBase Buffer, string ResourceThree, bool startWithIcon )
        {
            return Buffer.StartResourceThree( startWithIcon ).Add( ResourceThree ).EndColor();
        }

        #endregion

        #region Strength
        public static ArcenCharacterBufferBase StartStrength( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Strength, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddStrength( this ArcenCharacterBufferBase Buffer, int Strength, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartStrength( startWithIcon, overwriteColorOrNull ).Add( FInt.Create(Strength, false ) ).EndStrengthWrapper(false);
        }

        public static ArcenCharacterBufferBase AddStrength( this ArcenCharacterBufferBase Buffer, FInt Strength, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartStrength( startWithIcon, overwriteColorOrNull ).Add( Strength / 1000 ).EndStrengthWrapper(false);
        }

        public static ArcenCharacterBufferBase AddStrength_MoreReadable( this ArcenCharacterBufferBase Buffer, int Strength, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartStrength( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( FInt.Create( Strength, false ) ).EndStrengthWrapper(false);
        }

        public static ArcenCharacterBufferBase AddStrength_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Strength, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartStrength( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Strength / 1000 ).EndStrengthWrapper(false);
        }

        public static ArcenCharacterBufferBase AddStrength_Truncated( this ArcenCharacterBufferBase Buffer, int Strength, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartStrength( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( FInt.Create( Strength, false ) ).EndStrengthWrapper(false);
        }

        public static ArcenCharacterBufferBase AddStrength_Truncated( this ArcenCharacterBufferBase Buffer, FInt Strength, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartStrength( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Strength / 1000 ).EndStrengthWrapper(false);
        }

        public static ArcenCharacterBufferBase AddStrength( this ArcenCharacterBufferBase Buffer, string Strength, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartStrength( startWithIcon, overwriteColorOrNull ).Add( Strength ).EndStrengthWrapper(false);
        }

        #endregion

        #region AIP
        public static ArcenCharacterBufferBase StartAIP( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.AIP, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddAIP( this ArcenCharacterBufferBase Buffer, int AIP, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIP( startWithIcon, overwriteColorOrNull ).Add( AIP ).EndAIPWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIP( this ArcenCharacterBufferBase Buffer, FInt AIP, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIP( startWithIcon, overwriteColorOrNull ).Add( AIP ).EndAIPWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIP_MoreReadable( this ArcenCharacterBufferBase Buffer, int AIP, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIP( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( AIP ).EndAIPWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIP_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt AIP, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIP( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( AIP ).EndAIPWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIP_Truncated( this ArcenCharacterBufferBase Buffer, int AIP, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIP( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( AIP ).EndAIPWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIP_Truncated( this ArcenCharacterBufferBase Buffer, FInt AIP, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIP( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( AIP ).EndAIPWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIP( this ArcenCharacterBufferBase Buffer, string AIP, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIP( startWithIcon, overwriteColorOrNull ).Add( AIP ).EndAIPWrapper(false);
        }

        #endregion

        #region AIPReduction
        public static ArcenCharacterBufferBase StartAIPReduction( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.AIPReduction, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddAIPReduction( this ArcenCharacterBufferBase Buffer, int AIPReduction, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIPReduction( startWithIcon, overwriteColorOrNull ).Add( AIPReduction ).EndAIPReductionWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIPReduction( this ArcenCharacterBufferBase Buffer, FInt AIPReduction, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIPReduction( startWithIcon, overwriteColorOrNull ).Add( AIPReduction ).EndAIPReductionWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIPReduction_MoreReadable( this ArcenCharacterBufferBase Buffer, int AIPReduction, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIPReduction( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( AIPReduction ).EndAIPReductionWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIPReduction_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt AIPReduction, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIPReduction( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( AIPReduction ).EndAIPReductionWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIPReduction_Truncated( this ArcenCharacterBufferBase Buffer, int AIPReduction, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIPReduction( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( AIPReduction ).EndAIPReductionWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIPReduction_Truncated( this ArcenCharacterBufferBase Buffer, FInt AIPReduction, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIPReduction( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( AIPReduction ).EndAIPReductionWrapper(false);
        }

        public static ArcenCharacterBufferBase AddAIPReduction( this ArcenCharacterBufferBase Buffer, string AIPReduction, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAIPReduction( startWithIcon, overwriteColorOrNull ).Add( AIPReduction ).EndAIPReductionWrapper(false);
        }

        #endregion

        #region Damage
        public static ArcenCharacterBufferBase StartDamage( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Damage, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddDamage( this ArcenCharacterBufferBase Buffer, int Damage, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartDamage( startWithIcon, overwriteColorOrNull ).Add( Damage ).EndDamageWrapper(false);
        }

        public static ArcenCharacterBufferBase AddDamage( this ArcenCharacterBufferBase Buffer, FInt Damage, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartDamage( startWithIcon, overwriteColorOrNull ).Add( Damage ).EndDamageWrapper(false);
        }

        public static ArcenCharacterBufferBase AddDamage_MoreReadable( this ArcenCharacterBufferBase Buffer, int Damage, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartDamage( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Damage ).EndDamageWrapper(false);
        }

        public static ArcenCharacterBufferBase AddDamage_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Damage, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartDamage( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Damage ).EndDamageWrapper(false);
        }

        public static ArcenCharacterBufferBase AddDamage_Truncated( this ArcenCharacterBufferBase Buffer, int Damage, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartDamage( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Damage ).EndDamageWrapper(false);
        }

        public static ArcenCharacterBufferBase AddDamage_Truncated( this ArcenCharacterBufferBase Buffer, FInt Damage, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartDamage( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Damage ).EndDamageWrapper(false);
        }

        public static ArcenCharacterBufferBase AddDamage( this ArcenCharacterBufferBase Buffer, string Damage, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartDamage( startWithIcon, overwriteColorOrNull ).Add( Damage ).EndDamageWrapper(false);
        }

        #endregion

        #region Range
        public static ArcenCharacterBufferBase StartRange( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Range, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddRange( this ArcenCharacterBufferBase Buffer, int Range, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRange( startWithIcon, overwriteColorOrNull ).Add( Range ).EndRangeWrapper(false);
        }

        public static ArcenCharacterBufferBase AddRange( this ArcenCharacterBufferBase Buffer, FInt Range, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRange( startWithIcon, overwriteColorOrNull ).Add( Range ).EndRangeWrapper(false);
        }

        public static ArcenCharacterBufferBase AddRange_MoreReadable( this ArcenCharacterBufferBase Buffer, int Range, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRange( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Range ).EndRangeWrapper(false);
        }

        public static ArcenCharacterBufferBase AddRange_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Range, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRange( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Range ).EndRangeWrapper(false);
        }

        public static ArcenCharacterBufferBase AddRange_Truncated( this ArcenCharacterBufferBase Buffer, int Range, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRange( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Range ).EndRangeWrapper(false);
        }

        public static ArcenCharacterBufferBase AddRange_Truncated( this ArcenCharacterBufferBase Buffer, FInt Range, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRange( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Range ).EndRangeWrapper(false);
        }

        public static ArcenCharacterBufferBase AddRange( this ArcenCharacterBufferBase Buffer, string Range, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartRange( startWithIcon, overwriteColorOrNull ).Add( Range ).EndRangeWrapper(false);
        }

        #endregion

        #region Hull
        public static ArcenCharacterBufferBase StartHull( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Hull, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddHull( this ArcenCharacterBufferBase Buffer, int Hull, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHull( startWithIcon, overwriteColorOrNull ).Add( Hull ).EndColor();
        }

        public static ArcenCharacterBufferBase AddHull( this ArcenCharacterBufferBase Buffer, FInt Hull, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHull( startWithIcon, overwriteColorOrNull ).Add( Hull ).EndColor();
        }

        public static ArcenCharacterBufferBase AddHull_MoreReadable( this ArcenCharacterBufferBase Buffer, int Hull, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHull( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Hull ).EndColor();
        }

        public static ArcenCharacterBufferBase AddHull_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Hull, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHull( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Hull ).EndColor();
        }

        public static ArcenCharacterBufferBase AddHull_Truncated( this ArcenCharacterBufferBase Buffer, int Hull, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHull( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Hull ).EndColor();
        }

        public static ArcenCharacterBufferBase AddHull_Truncated( this ArcenCharacterBufferBase Buffer, FInt Hull, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHull( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Hull ).EndColor();
        }

        public static ArcenCharacterBufferBase AddHull( this ArcenCharacterBufferBase Buffer, string Hull, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartHull( startWithIcon, overwriteColorOrNull ).Add( Hull ).EndColor();
        }

        #endregion

        #region Shield
        public static ArcenCharacterBufferBase StartShield( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Shield, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddShield( this ArcenCharacterBufferBase Buffer, int Shield, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartShield( startWithIcon, overwriteColorOrNull ).Add( Shield ).EndColor();
        }

        public static ArcenCharacterBufferBase AddShield( this ArcenCharacterBufferBase Buffer, FInt Shield, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartShield( startWithIcon, overwriteColorOrNull ).Add( Shield ).EndColor();
        }

        public static ArcenCharacterBufferBase AddShield_MoreReadable( this ArcenCharacterBufferBase Buffer, int Shield, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartShield( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Shield ).EndColor();
        }

        public static ArcenCharacterBufferBase AddShield_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Shield, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartShield( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Shield ).EndColor();
        }

        public static ArcenCharacterBufferBase AddShield_Truncated( this ArcenCharacterBufferBase Buffer, int Shield, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartShield( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Shield ).EndColor();
        }

        public static ArcenCharacterBufferBase AddShield_Truncated( this ArcenCharacterBufferBase Buffer, FInt Shield, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartShield( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Shield ).EndColor();
        }

        public static ArcenCharacterBufferBase AddShield( this ArcenCharacterBufferBase Buffer, string Shield, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartShield( startWithIcon, overwriteColorOrNull ).Add( Shield ).EndColor();
        }

        #endregion

        #region Cloak
        public static ArcenCharacterBufferBase StartCloak( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Cloak, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddCloak( this ArcenCharacterBufferBase Buffer, int Cloak, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartCloak( startWithIcon, overwriteColorOrNull ).Add( Cloak ).EndColor();
        }

        public static ArcenCharacterBufferBase AddCloak( this ArcenCharacterBufferBase Buffer, FInt Cloak, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartCloak( startWithIcon, overwriteColorOrNull ).Add( Cloak ).EndColor();
        }

        public static ArcenCharacterBufferBase AddCloak_MoreReadable( this ArcenCharacterBufferBase Buffer, int Cloak, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartCloak( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Cloak ).EndColor();
        }

        public static ArcenCharacterBufferBase AddCloak_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Cloak, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartCloak( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Cloak ).EndColor();
        }

        public static ArcenCharacterBufferBase AddCloak_Truncated( this ArcenCharacterBufferBase Buffer, int Cloak, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartCloak( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Cloak ).EndColor();
        }

        public static ArcenCharacterBufferBase AddCloak_Truncated( this ArcenCharacterBufferBase Buffer, FInt Cloak, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartCloak( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Cloak ).EndColor();
        }

        public static ArcenCharacterBufferBase AddCloak( this ArcenCharacterBufferBase Buffer, string Cloak, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartCloak( startWithIcon, overwriteColorOrNull ).Add( Cloak ).EndColor();
        }

        #endregion

        #region Armor
        public static ArcenCharacterBufferBase StartArmor( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Armor, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddArmor( this ArcenCharacterBufferBase Buffer, int Armor, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArmor( startWithIcon, overwriteColorOrNull ).Add( Armor ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArmor( this ArcenCharacterBufferBase Buffer, FInt Armor, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArmor( startWithIcon, overwriteColorOrNull ).Add( Armor ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArmor_MoreReadable( this ArcenCharacterBufferBase Buffer, int Armor, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArmor( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Armor ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArmor_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Armor, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArmor( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Armor ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArmor_Truncated( this ArcenCharacterBufferBase Buffer, int Armor, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArmor( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Armor ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArmor_Truncated( this ArcenCharacterBufferBase Buffer, FInt Armor, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArmor( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Armor ).EndColor();
        }

        public static ArcenCharacterBufferBase AddArmor( this ArcenCharacterBufferBase Buffer, string Armor, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartArmor( startWithIcon, overwriteColorOrNull ).Add( Armor ).EndColor();
        }

        #endregion

        #region Mass
        public static ArcenCharacterBufferBase StartMass( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Mass, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddMass( this ArcenCharacterBufferBase Buffer, int Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).Add( Mass ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMass( this ArcenCharacterBufferBase Buffer, FInt Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).Add( Mass ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMass_MoreReadable( this ArcenCharacterBufferBase Buffer, int Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Mass ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMass_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Mass ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMass_Truncated( this ArcenCharacterBufferBase Buffer, int Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Mass ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMass_Truncated( this ArcenCharacterBufferBase Buffer, FInt Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Mass ).EndColor();
        }
        
        public static ArcenCharacterBufferBase AddMass_Truncated( this ArcenCharacterBufferBase Buffer, float Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Mass ).EndColor();
        }

        public static ArcenCharacterBufferBase AddMass( this ArcenCharacterBufferBase Buffer, string Mass, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartMass( startWithIcon, overwriteColorOrNull ).Add( Mass ).EndColor();
        }

        #endregion

        #region Albedo
        public static ArcenCharacterBufferBase StartAlbedo( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Albedo, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddAlbedo( this ArcenCharacterBufferBase Buffer, int Albedo, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAlbedo( startWithIcon, overwriteColorOrNull ).Add( Albedo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddAlbedo( this ArcenCharacterBufferBase Buffer, FInt Albedo, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAlbedo( startWithIcon, overwriteColorOrNull ).Add( Albedo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddAlbedo_MoreReadable( this ArcenCharacterBufferBase Buffer, int Albedo, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAlbedo( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Albedo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddAlbedo_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Albedo, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAlbedo( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Albedo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddAlbedo_Truncated( this ArcenCharacterBufferBase Buffer, int Albedo, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAlbedo( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Albedo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddAlbedo_Truncated( this ArcenCharacterBufferBase Buffer, FInt Albedo, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAlbedo( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Albedo ).EndColor();
        }

        public static ArcenCharacterBufferBase AddAlbedo( this ArcenCharacterBufferBase Buffer, string Albedo, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartAlbedo( startWithIcon, overwriteColorOrNull ).Add( Albedo ).EndColor();
        }

        #endregion

        #region Speed
        public static ArcenCharacterBufferBase StartSpeed( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Speed, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddSpeed( this ArcenCharacterBufferBase Buffer, int Speed, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartSpeed( startWithIcon, overwriteColorOrNull ).Add( Speed ).EndColor();
        }

        public static ArcenCharacterBufferBase AddSpeed( this ArcenCharacterBufferBase Buffer, FInt Speed, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartSpeed( startWithIcon, overwriteColorOrNull ).Add( Speed ).EndColor();
        }

        public static ArcenCharacterBufferBase AddSpeed_MoreReadable( this ArcenCharacterBufferBase Buffer, int Speed, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartSpeed( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Speed ).EndColor();
        }

        public static ArcenCharacterBufferBase AddSpeed_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Speed, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartSpeed( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Speed ).EndColor();
        }

        public static ArcenCharacterBufferBase AddSpeed_Truncated( this ArcenCharacterBufferBase Buffer, int Speed, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartSpeed( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Speed ).EndColor();
        }

        public static ArcenCharacterBufferBase AddSpeed_Truncated( this ArcenCharacterBufferBase Buffer, FInt Speed, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartSpeed( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Speed ).EndColor();
        }

        public static ArcenCharacterBufferBase AddSpeed( this ArcenCharacterBufferBase Buffer, string Speed, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartSpeed( startWithIcon, overwriteColorOrNull ).Add( Speed ).EndSpeedWrapper(false);
        }

        #endregion

        #region Engine
        public static ArcenCharacterBufferBase StartEngine( this ArcenCharacterBufferBase Buffer, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartFormatWrapper(ArcenExternalUIUtilities.Engine, startWithIcon, overwriteColorOrNull);
        }

        public static ArcenCharacterBufferBase AddEngine( this ArcenCharacterBufferBase Buffer, int Engine, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEngine( startWithIcon, overwriteColorOrNull ).Add( Engine ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEngine( this ArcenCharacterBufferBase Buffer, FInt Engine, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEngine( startWithIcon, overwriteColorOrNull ).Add( Engine ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEngine_MoreReadable( this ArcenCharacterBufferBase Buffer, int Engine, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEngine( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Engine ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEngine_MoreReadable( this ArcenCharacterBufferBase Buffer, FInt Engine, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEngine( startWithIcon, overwriteColorOrNull ).AddNumberMoreReadable( Engine ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEngine_Truncated( this ArcenCharacterBufferBase Buffer, int Engine, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEngine( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Engine ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEngine_Truncated( this ArcenCharacterBufferBase Buffer, FInt Engine, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEngine( startWithIcon, overwriteColorOrNull ).AddNumberTruncated( Engine ).EndColor();
        }

        public static ArcenCharacterBufferBase AddEngine( this ArcenCharacterBufferBase Buffer, string Engine, bool startWithIcon, string overwriteColorOrNull = null )
        {
            return Buffer.StartEngine( startWithIcon, overwriteColorOrNull ).Add( Engine ).EndColor();
        }

        #endregion

        #endregion

        #region CommonFormating_Mk2

        public static ArcenCharacterBufferBase StartFormatWrapper( this ArcenCharacterBufferBase Buffer, FixedTextFormatingStats Format, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            var col = OverrideColorOrNull ?? Format.Color;

            if ( StartWithIcon && Format.HasSprite )
            {
                Buffer.AddSprite( Format.Sprite, col );
                if (Format.HasAfterSprite)
                    Buffer.Add(Format.AfterSprite, TextCaps.Normal);
            }
            
            return Buffer.StartColor( col );
        }

        public static ArcenCharacterBufferBase EndFormatWrapper( this ArcenCharacterBufferBase Buffer, FixedTextFormatingStats Format, bool EndWithName=false )
        {
            if ( EndWithName )
                Buffer.Add(" ").Add( Format.Name );
            
            Buffer.EndColor();

            return Buffer;
        }

        public static ArcenCharacterBufferBase WrapFormat( this ArcenCharacterBufferBase Buffer, string Text, FixedTextFormatingStats Format, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( Format, StartWithIcon, OverrideColorOrNull ).Add( Text ).EndFormatWrapper( Format, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapFormat( this ArcenCharacterBufferBase Buffer, int Amount, FixedTextFormatingStats Format, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( Format, StartWithIcon, OverrideColorOrNull ).Add( Amount ).EndFormatWrapper( Format, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapFormat( this ArcenCharacterBufferBase Buffer, FInt Amount, FixedTextFormatingStats Format, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( Format, StartWithIcon, OverrideColorOrNull ).Add( Amount ).EndFormatWrapper( Format, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapFormatMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, FixedTextFormatingStats Format, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( Format, StartWithIcon, OverrideColorOrNull ).AddNumberMoreReadable( Amount ).EndFormatWrapper( Format, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapFormatMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, FixedTextFormatingStats Format, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( Format, StartWithIcon, OverrideColorOrNull ).AddNumberMoreReadable( Amount ).EndFormatWrapper( Format, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapFormatTruncated( this ArcenCharacterBufferBase Buffer, int Amount, FixedTextFormatingStats Format, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( Format, StartWithIcon, OverrideColorOrNull ).AddNumberTruncated( Amount ).EndFormatWrapper( Format, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapFormatTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, FixedTextFormatingStats Format, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( Format, StartWithIcon, OverrideColorOrNull ).AddNumberTruncated( Amount ).EndFormatWrapper( Format, EndWithName );
        }

        #region GenericResource

        public static ArcenCharacterBufferBase StartGenericResourceWrapper( this ArcenCharacterBufferBase Buffer, ResourceType Resource, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndGenericResourceWrapper( this ArcenCharacterBufferBase Buffer, ResourceType Resource, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.GetFormatForResource( Resource ), EndWithName );
        }
        public static ArcenCharacterBufferBase WrapGenericResource( this ArcenCharacterBufferBase Buffer, string Text, ResourceType Resource, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapGenericResource( this ArcenCharacterBufferBase Buffer, int Amount, ResourceType Resource, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapGenericResource( this ArcenCharacterBufferBase Buffer, FInt Amount, ResourceType Resource, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapGenericResourceMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, ResourceType Resource, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapGenericResourceMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, ResourceType Resource, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapGenericResourceTruncated( this ArcenCharacterBufferBase Buffer, int Amount, ResourceType Resource, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapGenericResourceTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, ResourceType Resource, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.GetFormatForResource( Resource ), StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Metal

        public static ArcenCharacterBufferBase StartMetalWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Metal, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndMetalWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Metal, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapMetal( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Metal, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapMetal( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Metal, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMetal( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Metal, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMetalMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Metal, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMetalMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Metal, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMetalTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Metal, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMetalTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Metal, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Energy

        public static ArcenCharacterBufferBase StartEnergyWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Energy, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndEnergyWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Energy, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapEnergy( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Energy, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapEnergy( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Energy, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEnergy( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Energy, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEnergyMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Energy, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEnergyMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Energy, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEnergyTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Energy, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEnergyTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Energy, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Argon

        public static ArcenCharacterBufferBase StartArgonWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Argon, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndArgonWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Argon, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapArgon( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Argon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapArgon( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Argon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArgon( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Argon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArgonMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Argon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArgonMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Argon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArgonTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Argon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArgonTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Argon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Radon

        public static ArcenCharacterBufferBase StartRadonWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Radon, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndRadonWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Radon, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapRadon( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Radon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapRadon( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Radon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRadon( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Radon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRadonMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Radon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRadonMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Radon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRadonTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Radon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRadonTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Radon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Xenon

        public static ArcenCharacterBufferBase StartXenonWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Xenon, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndXenonWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Xenon, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapXenon( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Xenon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapXenon( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Xenon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapXenon( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Xenon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapXenonMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Xenon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapXenonMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Xenon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapXenonTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Xenon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapXenonTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Xenon, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Science

        public static ArcenCharacterBufferBase StartScienceWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Science, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndScienceWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Science, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapScience( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Science, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapScience( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Science, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapScience( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Science, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapScienceMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Science, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapScienceMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Science, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapScienceTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Science, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapScienceTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Science, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Hacking

        public static ArcenCharacterBufferBase StartHackingWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Hacking, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndHackingWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Hacking, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapHacking( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Hacking, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapHacking( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Hacking, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHacking( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Hacking, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHackingMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Hacking, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHackingMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Hacking, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHackingTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Hacking, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHackingTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Hacking, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Strength

        public static ArcenCharacterBufferBase StartStrengthWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Strength, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndStrengthWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Strength, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapStrength( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Strength, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapStrength( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( FInt.Create( Amount, false ), ArcenExternalUIUtilities.Strength, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapStrength( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount / 1000, ArcenExternalUIUtilities.Strength, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapStrengthMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( FInt.Create( Amount, false ), ArcenExternalUIUtilities.Strength, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapStrengthMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount / 1000, ArcenExternalUIUtilities.Strength, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapStrengthTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( FInt.Create( Amount, false ), ArcenExternalUIUtilities.Strength, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapStrengthTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount / 1000, ArcenExternalUIUtilities.Strength, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region AIP

        public static ArcenCharacterBufferBase StartAIPWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.AIP, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndAIPWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.AIP, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapAIP( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.AIP, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapAIP( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.AIP, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIP( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.AIP, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.AIP, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.AIP, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.AIP, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.AIP, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region AIPReduction

        public static ArcenCharacterBufferBase StartAIPReductionWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.AIPReduction, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndAIPReductionWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.AIPReduction, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapAIPReduction( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.AIPReduction, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapAIPReduction( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.AIPReduction, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPReduction( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.AIPReduction, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPReductionMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.AIPReduction, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPReductionMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.AIPReduction, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPReductionTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.AIPReduction, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAIPReductionTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.AIPReduction, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Damage

        public static ArcenCharacterBufferBase StartDamageWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Damage, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndDamageWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Damage, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapDamage( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Damage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapDamage( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Damage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamage( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Damage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamageMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Damage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamageMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Damage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamageTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Damage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamageTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Damage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Range

        public static ArcenCharacterBufferBase StartRangeWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Range, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndRangeWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Range, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapRange( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Range, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapRange( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Range, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRange( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Range, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRangeMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Range, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRangeMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Range, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRangeTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Range, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapRangeTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Range, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Hull

        public static ArcenCharacterBufferBase StartHullWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Hull, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndHullWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Hull, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapHull( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Hull, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapHull( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Hull, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHull( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Hull, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHullMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Hull, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHullMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Hull, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHullTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Hull, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapHullTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Hull, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Shield

        public static ArcenCharacterBufferBase StartShieldWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Shield, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndShieldWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Shield, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapShield( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Shield, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapShield( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Shield, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapShield( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Shield, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapShieldMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Shield, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapShieldMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Shield, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapShieldTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Shield, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapShieldTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Shield, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Cloak

        public static ArcenCharacterBufferBase StartCloakWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Cloak, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndCloakWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Cloak, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapCloak( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Cloak, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapCloak( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Cloak, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCloak( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Cloak, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCloakMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Cloak, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCloakMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Cloak, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCloakTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Cloak, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCloakTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Cloak, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Armor

        public static ArcenCharacterBufferBase StartArmorWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Armor, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndArmorWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Armor, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapArmor( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Armor, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapArmor( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Armor, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArmor( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Armor, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArmorMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Armor, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArmorMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Armor, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArmorTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Armor, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapArmorTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Armor, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Mass

        public static ArcenCharacterBufferBase StartMassWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Mass, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndMassWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Mass, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapMass( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Mass, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapMass( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Mass, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMass( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Mass, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMassMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Mass, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMassMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Mass, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMassTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Mass, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMassTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Mass, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Albedo

        public static ArcenCharacterBufferBase StartAlbedoWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Albedo, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndAlbedoWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Albedo, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapAlbedo( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Albedo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapAlbedo( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Albedo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAlbedo( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Albedo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAlbedoMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Albedo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAlbedoMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Albedo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAlbedoTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Albedo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapAlbedoTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Albedo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Speed

        public static ArcenCharacterBufferBase StartSpeedWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Speed, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndSpeedWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Speed, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapSpeed( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Speed, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapSpeed( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Speed, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapSpeed( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Speed, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapSpeedMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Speed, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapSpeedMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Speed, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapSpeedTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Speed, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapSpeedTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Speed, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Engine

        public static ArcenCharacterBufferBase StartEngineWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Engine, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndEngineWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Engine, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapEngine( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Engine, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapEngine( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Engine, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEngine( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Engine, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEngineMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Engine, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEngineMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Engine, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEngineTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Engine, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapEngineTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Engine, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion



        #region ExoticDamage

        public static ArcenCharacterBufferBase StartExoticDamageWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndExoticDamageWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.ExoticDamage, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapExoticDamage( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapExoticDamage( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapExoticDamage( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapExoticDamageMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapExoticDamageMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapExoticDamageTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapExoticDamageTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.ExoticDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region CorrosiveDamage

        public static ArcenCharacterBufferBase StartCorrosiveDamageWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndCorrosiveDamageWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.CorrosiveDamage, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapCorrosiveDamage( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapCorrosiveDamage( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCorrosiveDamage( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCorrosiveDamageMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCorrosiveDamageMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCorrosiveDamageTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapCorrosiveDamageTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.CorrosiveDamage, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Multishot

        public static ArcenCharacterBufferBase StartMultishotWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Multishot, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndMultishotWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Multishot, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapMultishot( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Multishot, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapMultishot( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Multishot, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMultishot( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Multishot, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMultishotMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Multishot, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMultishotMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Multishot, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMultishotTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Multishot, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapMultishotTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Multishot, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region DamagePerSecond

        public static ArcenCharacterBufferBase StartDamagePerSecondWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndDamagePerSecondWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.DamagePerSecond, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapDamagePerSecond( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapDamagePerSecond( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamagePerSecond( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamagePerSecondMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamagePerSecondMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamagePerSecondTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapDamagePerSecondTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.DamagePerSecond, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region Reload

        public static ArcenCharacterBufferBase StartReloadWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.Reload, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndReloadWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.Reload, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapReload( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.Reload, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapReload( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Reload, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapReload( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.Reload, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapReloadMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Reload, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapReloadMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.Reload, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapReloadTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Reload, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapReloadTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.Reload, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion


        /* Disabled for now
        #region PlayerResourceOne

        public static ArcenCharacterBufferBase StartPlayerResourceOneWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndPlayerResourceOneWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.PlayerResourceOne, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceOne( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapPlayerResourceOne( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceOne( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceOneMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceOneMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceOneTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceOneTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.PlayerResourceOne, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        #region PlayerResourceTwo

        public static ArcenCharacterBufferBase StartPlayerResourceTwoWrapper( this ArcenCharacterBufferBase Buffer, bool StartWithIcon, string OverrideColorOrNull = null )
        {
            return Buffer.StartFormatWrapper( ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase EndPlayerResourceTwoWrapper( this ArcenCharacterBufferBase Buffer, bool EndWithName )
        {
            return Buffer.EndFormatWrapper( ArcenExternalUIUtilities.PlayerResourceTwo, EndWithName );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceTwo( this ArcenCharacterBufferBase Buffer, string Text, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Text, ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        public static ArcenCharacterBufferBase WrapPlayerResourceTwo( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceTwo( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormat( Amount, ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceTwoMoreReadable( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceTwoMoreReadable( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatMoreReadable( Amount, ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceTwoTruncated( this ArcenCharacterBufferBase Buffer, int Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }
        public static ArcenCharacterBufferBase WrapPlayerResourceTwoTruncated( this ArcenCharacterBufferBase Buffer, FInt Amount, bool StartWithIcon, bool EndWithName, string OverrideColorOrNull = null )
        {
            return Buffer.WrapFormatTruncated( Amount, ArcenExternalUIUtilities.PlayerResourceTwo, StartWithIcon, EndWithName, OverrideColorOrNull );
        }

        #endregion

        */
        #endregion

        #region Bold, Italic, Etc.

        public static ArcenCharacterBufferBase StartBold( this ArcenCharacterBufferBase Buffer )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( "<b>", TextCaps.Normal );
        }
        
        public static ArcenCharacterBufferBase EndBold( this ArcenCharacterBufferBase Buffer )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( "</b>", TextCaps.Normal );
        }
        
        public static ArcenCharacterBufferBase Bold( this ArcenCharacterBufferBase Buffer, bool Enabled )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            
            if (Enabled)
                Buffer.StartBold();
            else
                Buffer.EndBold();
            
            return Buffer;
        }
        
        public static ArcenCharacterBufferBase Align( this ArcenCharacterBufferBase Buffer, string value )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( "<align=\"", TextCaps.Normal ).Add(value, TextCaps.Normal).Add("\">", TextCaps.Normal);
        }
        
        public static ArcenCharacterBufferBase EndAlign( this ArcenCharacterBufferBase Buffer )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( "</align>", TextCaps.Normal );
        }
        
        public static ArcenCharacterBufferBase Indent( this ArcenCharacterBufferBase Buffer, string value )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( "<indent=\"", TextCaps.Normal ).Add(value, TextCaps.Normal).Add("\">", TextCaps.Normal);
        }
        
        public static ArcenCharacterBufferBase EndIndent( this ArcenCharacterBufferBase Buffer )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( "</indent>", TextCaps.Normal );
        }
        
        #endregion
        
        #region Sizes

        public static ArcenCharacterBufferBase StartSize( this ArcenCharacterBufferBase Buffer, string Size )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( "<size=", TextCaps.Normal ).Add(Size, TextCaps.Normal).Add(">");
        }
        
        public static ArcenCharacterBufferBase EndSize( this ArcenCharacterBufferBase Buffer )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            return Buffer.Add( FontSizes.END_SIZE );
        }

        public static ArcenCharacterBufferBase AddSize_Tiny( this ArcenCharacterBufferBase Buffer )
        {
            return Buffer.Add( FontSizes.MUCH_SMALLER_SIZE_STRING );
        }

        public static ArcenCharacterBufferBase AddSize_VerySmall( this ArcenCharacterBufferBase Buffer )
        {
            return Buffer.Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING );
        }

        public static ArcenCharacterBufferBase AddSize_Small( this ArcenCharacterBufferBase Buffer )
        {
            return Buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING );
        }

        public static ArcenCharacterBufferBase AddSize_BitSmall( this ArcenCharacterBufferBase Buffer )
        {
            return Buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING );
        }

        public static ArcenCharacterBufferBase AddSize_Normal( this ArcenCharacterBufferBase Buffer )
        {
            return Buffer.Add( FontSizes.BASE_SIZE_STRING );
        }

        public static ArcenCharacterBufferBase AddSize_Large( this ArcenCharacterBufferBase Buffer )
        {
            return Buffer.Add( FontSizes.SLIGHTLY_LARGER_SIZE_STRING );
        }

        public static ArcenCharacterBufferBase AddSize_Huge( this ArcenCharacterBufferBase Buffer )
        {
            return Buffer.Add( FontSizes.MUCH_LARGER_SIZE_STRING );
        }

        #endregion

        #region Misc

        /// <summary>
        /// Insert <space="Space">
        /// Where 'Space' is the argument passed to this method.
        /// ex.
        ///     buffer.Space("10px")
        ///     buffer.Space("5%")
        ///     buffer.Space("1em")
        /// </summary>
        public static ArcenCharacterBufferBase Space( this ArcenCharacterBufferBase Buffer, string Space )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;

            return Buffer.Add( "<space=", TextCaps.Normal ).Add( Space, TextCaps.Normal ).Add( ">" );
        }
        
        /// <summary>
        /// Insert <pos="Pos">
        ///     
        /// Where 'Pos' is the argument passed to this method.
        /// ex.
        ///     buffer.Pos("100px")
        ///     buffer.Pos("50%")
        ///     buffer.Pos("25em")
        /// </summary>
        public static ArcenCharacterBufferBase Pos( this ArcenCharacterBufferBase Buffer, string Pos )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            
            return Buffer.Add( "<pos=", TextCaps.Normal ).Add( Pos, TextCaps.Normal ).Add( ">" );
        }
        
        public static ArcenCharacterBufferBase ToPos( this ArcenCharacterBufferBase Buffer, CharacterPosInfo posInfo )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            
            return Buffer.Add( "<pos=", TextCaps.Normal ).Add( posInfo.CurrentPos ).Add( "px>", TextCaps.Normal );
        }

        public static ArcenCharacterBufferBase AddIconAsColor( this ArcenCharacterBufferBase Buffer, string Icon, string ColorHex )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            
            return Buffer.Add( "<sprite=\"", TextCaps.Normal ).Add( Icon ).Add( "\" color=#", TextCaps.Normal ).Add( ColorHex, TextCaps.Normal ).Add( ">" );
        }

        public static ArcenCharacterBufferBase AddIcon( this ArcenCharacterBufferBase Buffer, string Icon )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            
            return Buffer.Add( "<sprite=\"", TextCaps.Normal ).Add( Icon, TextCaps.Normal ).Add( "\">" );
        }

        public static ArcenCharacterBufferBase AddFactionColoredString( this ArcenCharacterBufferBase Buffer, string Text, Faction Faction )
        {
            if ( Faction == null )
                return Buffer.Add( Text );

            var color = Faction.FactionCenterColor;
            if ( color == null )
                return Buffer.Add( Text );
            
            return Buffer.AddColor( Text, color.ColorHexBrighter );
        }

        public static ArcenCharacterBufferBase AddFactionNameInItsColor( this ArcenCharacterBufferBase Buffer, Faction Faction, bool IncludeFactionIndex = false )
        {
            if ( Faction == null )
                return Buffer.Add( "[null faction]" );
            if ( !IncludeFactionIndex )
                return Buffer.AddFactionColoredString( Faction.GetDisplayName(), Faction );
            
            return Buffer.StartColor( Faction.FactionCenterColor.ColorHexBrighter ).Add( Faction.GetDisplayName() ).Add( " (" ).Add( Faction.FactionIndex ).Add( ")").EndColor();
        }

        public static ArcenCharacterBufferBase AddPlanetNameFormated( this ArcenCharacterBufferBase Buffer, Planet Planet, bool writeMarkLevelIfAIOwned )
        {
            string planetName = Planet == null ? "[null planet]" : Planet.Name;
            Buffer.AddFactionColoredString( planetName, Planet == null ? null : Planet.GetControllingOrInfluencingFaction() );
            if ( writeMarkLevelIfAIOwned && Planet != null && Planet.GetIsControlledByFactionType( FactionType.AI ) )
            {
                Buffer.Add( " " ).AddMarkLevelFormated( Planet.MarkLevelForAIOnly );
            }
            return Buffer;
        }

        public static ArcenCharacterBufferBase AddMarkLevelFormated( this ArcenCharacterBufferBase Buffer, Balance_MarkLevel Mark )
        {
            if ( Mark == null )
                return Buffer;
            return Buffer.Add( Mark.MapDisplayWithColor );
        }

        public static ArcenCharacterBufferBase AddMarkLevelFormated( this ArcenCharacterBufferBase Buffer, byte Mark )
        {
            return Buffer.AddMarkLevelFormated( Balance_MarkLevelTable.Instance.RowsByOrdinal[Mark] );
        }
        
        public static ArcenCharacterBufferBase AddMarkLevelFormated( this ArcenCharacterBufferBase Buffer, int Mark )
        {
            return Buffer.AddMarkLevelFormated( Balance_MarkLevelTable.Instance.RowsByOrdinal[Mark] );
        }

        public static ArcenCharacterBufferBase StartPercentageInColor( this ArcenCharacterBufferBase Buffer, FInt Percentage, bool higherPrecentageIsBetter, bool shiftOutOfBlueInsteadOfGreen )
        {
            if (Buffer.WriteAsTextFileOutput)
                return Buffer;
            
            if (EntityText.Use == WriterToUse.Formatted)
            {
                float h = 0;
                float s = 100;
                float v = 100;
                HSVColor hsv = new HSVColor(h/360.0f,s/100.0f,v/100.0f);
                
                var perc_100 = Percentage.ToFloat();
                hsv.h = ((perc_100 / 100.0f).Saturate() * 110.0f) / 360.0f;
                
                var col = hsv.ToRGB();
                
                Buffer.StartColor(col);
                
                return Buffer;
            }
            
            long rawValue = Percentage.RawValue;
            if ( rawValue < 0 )
            {
                rawValue *= -1;
            }
            int oneHundredPercent = (int) (100 * FInt.MULTIPLIER);
            if ( rawValue > oneHundredPercent )
            {
                rawValue = oneHundredPercent;
            }
            if ( !higherPrecentageIsBetter )
            {
                rawValue = (oneHundredPercent) - rawValue;
            }
            double red;
            double green;
            double blue;
            if ( shiftOutOfBlueInsteadOfGreen )
            {
                red = ((oneHundredPercent - (rawValue / 2)) * 2.5d) / (int) FInt.MULTIPLIER;
                green = (rawValue * 2.5d) / (int) FInt.MULTIPLIER;
                green = System.Math.Sqrt( green ) * 16;
                blue = green;
            } 
            else
            {
                red = ((oneHundredPercent - rawValue) * 2.55d) / (int) FInt.MULTIPLIER;
                red = System.Math.Sqrt( red ) * 16;
                green = (rawValue * 2.55d) / (int) FInt.MULTIPLIER;
                green = System.Math.Sqrt( green ) * 16;
                blue = 0;
            }

            Buffer
                .Add( ArcenExternalUIUtilities.ColorStart, TextCaps.Normal )
                .Add( ArcenExternalUIUtilities.ByteHexesByIndex[(int) System.Math.Round( red )], TextCaps.Normal )
                .Add( ArcenExternalUIUtilities.ByteHexesByIndex[(int) System.Math.Round( green )], TextCaps.Normal )
                .Add( ArcenExternalUIUtilities.ByteHexesByIndex[(int) System.Math.Round( blue )], TextCaps.Normal ).Add( ">", TextCaps.Normal );
            
            return Buffer;
        }

        public static ArcenCharacterBufferBase AddPercentageInColor( this ArcenCharacterBufferBase Buffer, float Percentage, bool higherPrecentageIsBetter, bool shiftOutOfBlueInsteadOfGreen )
        {
            return Buffer.AddPercentageInColor(Percentage.ToFInt(), higherPrecentageIsBetter, shiftOutOfBlueInsteadOfGreen);
        }
        
        public static ArcenCharacterBufferBase AddPercentageInColor( this ArcenCharacterBufferBase Buffer, FInt Percentage, bool higherPrecentageIsBetter, bool shiftOutOfBlueInsteadOfGreen )
        {
            return Buffer.StartPercentageInColor( Percentage, higherPrecentageIsBetter, shiftOutOfBlueInsteadOfGreen ).AddPercentRoundedDynamically( Percentage ).EndColor();
        }

        public static ArcenCharacterBufferBase AddPercentageInColor( this ArcenCharacterBufferBase Buffer, string Text, FInt Percentage, bool higherPrecentageIsBetter, bool shiftOutOfBlueInsteadOfGreen )
        {
            return Buffer.StartPercentageInColor( Percentage, higherPrecentageIsBetter, shiftOutOfBlueInsteadOfGreen ).Add( Text ).EndColor();
        }

        public static ArcenCharacterBufferBase AddDebug_Wasteful( this ArcenCharacterBufferBase Buffer, params string[] str )
        {
            return Add( Buffer, str );
        }

        // literally just calls .ToString() on each object
        public static ArcenCharacterBufferBase AddDebug_Wasteful( this ArcenCharacterBufferBase Buffer, params object[] Objects )
        {
            for ( int i = 0; i < Objects.Length; i++ )
            {
                Buffer.Add( Objects[i].ToString() );
            }
            return Buffer;
        }

        public static ArcenCharacterBufferBase AddHoursAndMinutes( this ArcenCharacterBufferBase buffer, int seconds, string colorOrNull = null )
        {
            int minutes = seconds / 60;
            int hours = minutes / 60;
            minutes %= 60;
            seconds %= 60;

            if ( colorOrNull != null )
                buffer.StartColor(colorOrNull);
            
            if ( hours > 0 )
                buffer.Add(hours).Add("h ");
            
            if ( minutes > 0 || hours > 0 )
                buffer.Add(minutes).Add("m ");
            
            buffer.Add(seconds).Add("s");

            if ( colorOrNull != null )
                buffer.EndColor();
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddMinutesAndSeconds( this ArcenCharacterBufferBase buffer, int seconds, string colorOrNull = null )
        {
            return buffer.AddMinutesAndSeconds((float)seconds, colorOrNull);
        }
        
        public static ArcenCharacterBufferBase AddMinutesAndSeconds( this ArcenCharacterBufferBase buffer, FInt seconds, string colorOrNull = null )
        {
            return buffer.AddMinutesAndSeconds((float)seconds, colorOrNull);
        }
        
        public static ArcenCharacterBufferBase AddMinutesAndSeconds( this ArcenCharacterBufferBase buffer, float fseconds, string colorOrNull = null, bool showZeroSeconds=false, string suffix=null )
        {
            int minutes = 0;
            int seconds = (int)fseconds;
            if (seconds > 120)
            {
                minutes = seconds / 60;
                seconds %= 60;
            }

            buffer.Open(TextStyle.MinutesAndSeconds);
            if ( colorOrNull != null )
                buffer.StartColor(colorOrNull);

            if ( minutes > 0 )
            {
                buffer.Add(minutes).Add("m", TextStyle.MinutesAndSeconds_Units);
                
                if (minutes > 10)
                {
                    seconds = 0;
                    showZeroSeconds = false;
                }
            }
            
            if ( seconds > 0 || showZeroSeconds )
            {
                if (minutes > 0)
                    buffer.Add(" ", TextStyle.MinutesAndSeconds_Spacer);
                
                buffer.Add(seconds);
                
                float rem = fseconds % 1;
                rem = (float)Math.Round(rem, 1);
                rem *= 10;
                if (rem > 0)
                    buffer.Add(".").Add(rem);
                
                buffer.Add("s", TextStyle.MinutesAndSeconds_Units);
            }

            if (!string.IsNullOrEmpty(suffix))
                buffer.Add(suffix);
            

            if ( colorOrNull != null )
                buffer.EndColor();
            buffer.Close(TextStyle.MinutesAndSeconds);
            
            return buffer;
        }

        public static ArcenCharacterBufferBase AddSecondsRemaining( this ArcenCharacterBufferBase buffer, int secondsRemaining, TimeIntensity timeIntensity = TimeIntensity.OneMinute)
        {
            switch ( timeIntensity ) {
                case TimeIntensity.OneMinute:
                    if ( secondsRemaining < 20 )
                        buffer.StartColor( "#ff4f32" ); //red
                    else if ( secondsRemaining < 60 )
                        buffer.StartColor( "#ffd632" ); //yellow
                    break;
                case TimeIntensity.TenMinutes:
                    buffer.StartColor(ArcenExternalUIUtilities.GetColorForNomadMoveTime( secondsRemaining ));
                    break;
                case TimeIntensity.None:
                    break;
            }

            buffer.Add( secondsRemaining / 60 );
            buffer.Add( ":" );
            int secondsPortion = secondsRemaining % 60;
            if ( secondsPortion < 10 ) //GC-neutral instead of ToString with formatting
                buffer.Add( "0" );
            buffer.Add( secondsPortion );

            if ( timeIntensity != TimeIntensity.None )
                buffer.EndColor();

            return buffer;
        }
        #endregion

        #region Ship Icon
        
        public static ArcenCharacterBufferBase AddShipIconInline( this ArcenCharacterBufferBase buffer, 
            GameEntity_Squad squad, TextStyle style=null )
        {
            return buffer.AddShipIconInline( squad.TypeData, squad.GetFactionCenterColor().ColorHex, squad.GetFactionTrimColor().ColorHex, style );
        }
        
        public static ArcenCharacterBufferBase AddShipIconInline( this ArcenCharacterBufferBase buffer, 
            GameEntityTypeData type, Faction faction, TextStyle style=null )
        {
            var e = EntityText.GetFakeEntity(type, faction:faction);
            buffer.AddShipIconInline( e, style );
            EntityText.ReleaseFakeEntity(e);

            return buffer;
        }
        
        
        public static ArcenCharacterBufferBase AddShipIconInline( this ArcenCharacterBufferBase buffer, 
            GameEntityTypeData typeOrNull, Faction faction, Balance_MarkLevel MarkOrNull, float SizePercent = 50 )
        {
            return buffer.AddShipIconInline( typeOrNull, 
                typeOrNull?.OverrideFactionColor_Center?.ColorHex ?? faction?.FactionCenterColor?.ColorHex ?? PlayerProfile_AIW2.Local.DefaultFactionCenterColor.ColorHex,
                typeOrNull?.OverrideFactionColor_Trim?.ColorHex ?? faction?.FactionTrimColor?.ColorHex ?? PlayerProfile_AIW2.Local.DefaultFactionTrimColor.ColorHex, 
                MarkOrNull, SizePercent );
        }

        public static ArcenCharacterBufferBase AddShipIconInline( this ArcenCharacterBufferBase buffer, 
            GameEntityTypeData typeOrNull, string colorCenter, string colorTrim, Balance_MarkLevel MarkOrNull, float SizePercent = 50 )
        {
            string tSprite = typeOrNull?.TexEmbedSprite_Icon?.InternalName;
            string tSpriteBorder = typeOrNull?.TexEmbedSprite_IconBorder?.InternalName;
            string tSpriteOverlay = typeOrNull?.TexEmbedSprite_IconOverlay?.InternalName;

            if ( tSprite != null && colorCenter != null && colorTrim != null )
            {
                buffer.Add( "<size=" ).Add( SizePercent ).Add( "%><voffset=1.5em>" );
                buffer.StartDoNotAdvanceXTagIfTrue( tSpriteBorder != null || tSpriteOverlay != null );
                buffer.Add( "<sprite=\"" ).Add( tSprite )
                    .Add( "\" color=\"#" ).Add( colorCenter ).Add( "\">" );
                if ( tSpriteBorder != null )
                {
                    buffer.EndDoNotAdvanceXTagIfTrue( tSpriteOverlay == null );
                    buffer.Add( "<sprite=\"" ).Add( tSpriteBorder )
                        .Add( "\" color=\"#" ).Add( colorTrim ).Add( "\">" );
                }
                if ( tSpriteOverlay != null )
                {
                    buffer.EndDoNotAdvanceXTagIfTrue( true );
                    buffer.Add( "<sprite=\"" ).Add( tSpriteOverlay ).Add( "\">" );
                }
                buffer.Add( "</size></voffset> " );
                if ( MarkOrNull != null )
                {
                    buffer.Add( MarkOrNull.ColorHexStart ).Add( MarkOrNull.Abbreviation ).EndColor();
                }
            }
            return buffer;
        }


        public static ArcenCharacterBufferBase AddShipIconInline( 
            this ArcenCharacterBufferBase buffer, 
            GameEntityTypeData type, string colorCenter, string colorTrim, 
            TextStyle style = null )
        {
            string sprite = type.TexEmbedSprite_Icon?.InternalName;
            string border = type.TexEmbedSprite_IconBorder?.InternalName;
            string overlay = type.TexEmbedSprite_IconOverlay?.InternalName;

            if ( sprite != null && 
                 border != null &&
                 colorCenter != null && 
                 colorTrim != null )
            {
                if (style == null)
                    style = TextStyle.Ship_Sprite;
                
                if (style == null)
                    throw new ArgumentNullException("style");
                
                buffer.Open(style);
                buffer.AddSprite(sprite, border, overlay, colorCenter, colorTrim, null);
                buffer.Close(style);
            }
            
            return buffer;
        }
        #endregion
        
        public static ArcenCharacterBufferBase StartColor( this ArcenCharacterBufferBase buffer, FixedTextFormatingStats format )
        {
            return buffer.StartColor(format.Color);
        }
        
        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase buffer, FixedTextFormatingStats format, string color=null, bool geo=false )
        {
            if (format.HasSprite)
            {
                buffer.AddSprite(format.Sprite, color??format.Color, geo:geo);
                if (format.HasAfterSprite)
                    buffer.Add(format.AfterSprite);
            }
            
            return buffer;
        }
    }
}
