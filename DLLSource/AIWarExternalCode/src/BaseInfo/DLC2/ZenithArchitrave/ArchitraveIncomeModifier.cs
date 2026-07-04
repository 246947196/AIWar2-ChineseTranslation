using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public struct ArchitraveIncomeModifier
    {
        //These are read in from the ZA Difficulty XML, and then a list of "Currently applied modifiers" is kept
        //in GlobalData.
        //The rule is "Take the Basic Income from the Difficulty and then you apply all the modifiers

        public string Name;
        public bool CivilWarDefensive; //If we are in civil war because we expanded too much
        public bool CivilWarOffensiveOnly; //only for civil war because our enemies have expanded too much
        public bool WithoutFullTerritory; //only when we are claiming or reclaiming our home territory
        public int FallenSpireCities; //only for when the player has This Many fallen spire cities
        public int ControlsLessThanXPlanets; //this modifier is applied if we have less than this many planets

        public int TimeInterval; //this modifier is applied every Interval seconds
        public int AdditiveIncrease; //add this number to the base income
        public FInt MultiplicativeIncrease; // BaseIncome = BaseIncome + BaseIncome * MultiplicativeIncrease
        public FInt Multiplier; // BaseIncome = BaseIncome * Multiplier
        public string UnitTag; //unused right now

        public int TimesApplied; //used only for the GlobalData list
                                 //Example: we have a 10 second interval for +1 additive increase. so in 20 seconds we're +2

        public override string ToString()
        {
            ArcenCharacterBuffer Buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "ArchitraveIncomeModifier-ToString" );

            Buffer.Add( this.Name ).Add( ": " );
            if ( this.CivilWarDefensive )
            {
                Buffer.Add( "Civil War (Defensive). " );
            }
            if ( this.CivilWarOffensiveOnly )
            {
                Buffer.Add( "Civil War (Offensive Only). " );
            }
            if ( this.WithoutFullTerritory )
            {
                Buffer.Add( "Without Full Territory. " );
            }
            if ( this.FallenSpireCities > 0 )
            {
                Buffer.Add( "There are " + this.FallenSpireCities + " fallen spire cities. " );
            }
            if ( this.ControlsLessThanXPlanets > 0 )
            {
                Buffer.Add( "Controls fewer than " + this.ControlsLessThanXPlanets + " planets. " );
            }

            Buffer.Add( "Time Interval: " ).Add( this.TimeInterval ).Add( ". " ).Add( this.TimesApplied ).Add( "x.\n" );

            if ( this.AdditiveIncrease > 0 )
                Buffer.Add( "\tAdditive Increase: " ).Add( this.AdditiveIncrease ).Add( ". " );
            if ( this.MultiplicativeIncrease > FInt.Zero )
                Buffer.Add( "\tMultiplicative Increase: " ).Add( this.MultiplicativeIncrease.ReadableString ).Add( ". " );
            if ( this.Multiplier > FInt.Zero )
                Buffer.Add( "\tMultiplier: " ).Add( this.Multiplier.ReadableString ).Add( ". " );
            if ( !String.IsNullOrEmpty( this.UnitTag ) )
                Buffer.Add( "\tUnit: " ).Add( this.UnitTag ).Add( ". " );
            return Buffer.ToStringAndReturnToPool();
        }
        public string ToStringForDisplay()
        {
            ArcenCharacterBuffer Buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "ArchitraveIncomeModifier-ToStringForDisplay" );

            Buffer.Add( this.Name, "a1ffa1" ).Add( ": " );
            if ( this.CivilWarDefensive )
            {
                Buffer.Add( "Civil War (Defensive). ", "bb55bb" );
            }
            if ( this.CivilWarOffensiveOnly )
            {
                Buffer.Add( "Civil War (Offensive Only). ", "bb55bb" );
            }
            if ( this.WithoutFullTerritory )
            {
                Buffer.Add( "Without Full Territory. ", "bb55bb" );
            }
            if ( this.FallenSpireCities > 0 )
            {
                Buffer.Add( "With " ).Add( this.FallenSpireCities, "bb55bb" ).Add( " fallen spire cities. " );
            }

            if ( this.FallenSpireCities > 0 )
            {
                Buffer.Add( "Controls fewer than " ).Add( this.ControlsLessThanXPlanets, "bb55bb" ).Add( " planets. " );
            }

            Buffer.Add( "Time Interval: " ).Add( this.TimeInterval, "0000ff" ).Add( ". " ).Add( this.TimesApplied, "3333aa" ).Add( "x.\n" );

            if ( this.AdditiveIncrease > 0 )
                Buffer.Add( "\tAdditive Increase: " ).Add( this.AdditiveIncrease, "a1a1ff" ).Add( ". " );
            if ( this.MultiplicativeIncrease > FInt.Zero )
                Buffer.Add( "\tMultiplicative Increase: " ).Add( this.MultiplicativeIncrease.ReadableString, "ffa1a1" ).Add( ". " );
            if ( this.Multiplier > FInt.Zero )
                Buffer.Add( "\tMultiplier: " ).Add( this.Multiplier.ReadableString, "a1ffa1" ).Add( ". " );
            if ( !String.IsNullOrEmpty( this.UnitTag ) )
                Buffer.Add( "\tUnit: " ).Add( this.UnitTag ).Add( ". " );
            return Buffer.ToStringAndReturnToPool();
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddString_Condensed( MetaData, this.Name, "ArchitraveIncomeModifier.Name" );
            Buffer.AddBool( MetaData, this.CivilWarDefensive, "CivilWarDefensive" );
            Buffer.AddBool( MetaData, this.CivilWarOffensiveOnly, "CivilWarOffensiveOnly" );
            Buffer.AddBool( MetaData, this.WithoutFullTerritory, "WithoutFullTerritory" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)this.FallenSpireCities, "FallenSpireCities" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)this.ControlsLessThanXPlanets, "ControlsLessThanXPlanets" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeInterval, "TimeInterval" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.AdditiveIncrease, "AdditiveIncrease" );
            Buffer.AddFInt( MetaData, this.MultiplicativeIncrease, "MultiplicativeIncrease" );
            Buffer.AddFInt( MetaData, this.Multiplier, "Multiplier" );
            Buffer.AddString_Condensed( MetaData, this.UnitTag, "UnitTag" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TimesApplied, "TimesApplied" );
        }
        public ArchitraveIncomeModifier( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.Name = Buffer.ReadString_Condensed( MetaData, "ArchitraveIncomeModifier.Name" );
            this.CivilWarDefensive = Buffer.ReadBool( MetaData, "CivilWarDefensive" );
            this.CivilWarOffensiveOnly = Buffer.ReadBool( MetaData, "CivilWarOffensiveOnly" );
            this.WithoutFullTerritory = Buffer.ReadBool( MetaData, "WithoutFullTerritory" );
            this.FallenSpireCities = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "FallenSpireCities" );
            this.ControlsLessThanXPlanets = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "ControlsLessThanXPlanets" );
            this.TimeInterval = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeInterval" );
            this.AdditiveIncrease = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "AdditiveIncrease" );
            this.MultiplicativeIncrease = Buffer.ReadFInt( MetaData, "MultiplicativeIncrease" );
            this.Multiplier = Buffer.ReadFInt( MetaData, "Multiplier" );
            this.UnitTag = Buffer.ReadString_Condensed( MetaData, "UnitTag" );
            this.TimesApplied = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TimesApplied" );
        }
    }
}
