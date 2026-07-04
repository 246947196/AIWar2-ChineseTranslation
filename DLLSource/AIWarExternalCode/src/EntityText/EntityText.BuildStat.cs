using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public struct EntityBuildStatText
    {
        public readonly GameEntity_Squad Squad;
        public readonly int Count;
        public readonly EntityText.Config Config;

        public EntityBuildStatText(GameEntity_Squad squad, int count, EntityText.Config config)
        {
            Squad = squad;
            Count = count;
            Config = config;
        }
        
        public void Write(ArcenCharacterBufferBase buffer)
        {
            var map = TextVarMap.Build_Stat_Row;
            buffer.AddVarReplace(map, AppendVar);
        }

        private void AppendVar( string name, TextStyle style, ArcenCharacterBufferBase buffer, object args )
        {
            if (name.Equals("count"))
            {
                buffer
                    .StartColor("dbef21")
                    .Add( this.Count ).Add( "×" )
                    .EndColor();
                
                return;
            }
            
            if (name.Equals("strength"))
            {
                float val = ((float)(Squad.GetStrengthPerSquad() * this.Count)) / 1000.0f;
                if (val >= 1)
                {
                    buffer
                        .Open( TextTerm.Strength, TermUse.Icon, TextStyle.Empty )
                        .AddNumber( val, null, TextStyle.Empty )
                        .Close( TextTerm.Strength );
                }
                        
                return;
            }
            
            if (name.Equals("ehp"))
            {
                int val = Squad.GetMaxEHP() * this.Count;
                
                buffer
                        .Open( TextTerm.EHP, TermUse.Icon, TextStyle.Empty )
                        .AddNumber( val, null, TextStyle.Empty )
                        .Close( TextTerm.EHP );
                        
                return;
            }
            
            if (name.Equals("dps"))
            {
                FInt min, max;
                Squad.GetDps(out min, out max);

                min *= Count;
                max *= Count;
                
                if (min >= 1 && max >= 1)
                {
                    buffer.Open( TextTerm.DPS, TermUse.Icon, TextStyle.Empty );
                    
                    if (min == max)
                        buffer.AddNumber(min, null, TextStyle.Empty );
                    else
                        buffer.AddNumber( min, null, TextStyle.Empty ).Add(" » ").AddNumber( max, null, TextStyle.Empty );
                    
                    buffer.Close( TextTerm.DPS );
                }
                        
                return;
            }
            
            if (name.Equals("energy"))
            {
                int val = Squad.GetEnergyUsage() * this.Count;
                if (val > 0)
                {
                    buffer
                            .Open( TextTerm.Energy, TermUse.Icon, TextStyle.Empty )
                            .AddNumber( val, null, TextStyle.Empty )
                            .Close( TextTerm.Energy );
                }
                        
                return;
            }
        }
    }

}