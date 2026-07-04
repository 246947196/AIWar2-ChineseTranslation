
using System;
using Arcen.Universal;
using Arcen.AIW2.Core;
using UnityEngine;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    #region LineGrant
    public struct LineGrant
    {
        public GameEntityTypeData Type;
        public int Cap;

        public LineGrant( GameEntityTypeData type, int cap )
        {
            this.Type = type;
            this.Cap = cap;
        }

        public int GetNumShipsForHackAndHacker( GameEntity_Squad Hacker, HackingType Hack )
        {
            int retVal = this.Cap;
            if ( Hack != null )
            {
                if ( Hack.ExtraMultiplierForGrantShipLines != FInt.One && Hack.ExtraMultiplierForGrantShipLines != FInt.Zero )
                    retVal = (Hack.ExtraMultiplierForGrantShipLines * retVal).GetNearestIntPreferringHigher();
                if ( Hack.GrantedShipCountIncludesDefensiveStructureCapMultiplierOfHacker )
                {
                    if ( Hacker != null && Hacker.TypeData.DefensiveStructureCap_Multiplier != FInt.One )
                        retVal = (Hacker.TypeData.DefensiveStructureCap_Multiplier * retVal).GetNearestIntPreferringHigher();
                }
            }
            return retVal;
        }
    }
    #endregion
    
    #region ShipKey
    public struct ShipKey : IEquatable<ShipKey>
    {
        public GameEntityTypeData TypeData;
        public Faction Faction;
        public bool Unclaimed;
        public GameEntityTypeData.MarkLevelStats ForMark;

        #region Equals
        
        public override bool Equals( object obj )
        {
            if (obj is ShipKey)
                return this.Equals((ShipKey)obj);
            
            return base.Equals( obj );
        }
        
        public bool Equals( ShipKey obj )
        {
            if (this.TypeData != obj.TypeData ||
                this.Faction != obj.Faction ||
                this.Unclaimed != obj.Unclaimed ||
                this.ForMark != obj.ForMark)
            {
                return false;
            }
            
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                if ( TypeData != null )
                    hash = hash * 31 + TypeData.GetHashCode();

                if ( Faction != null )
                    hash = hash * 31 + Faction.GetHashCode();
                
                hash = hash * 31 + Unclaimed.GetHashCode();

                if ( ForMark != null )
                    hash = hash * 31 + ForMark.GetHashCode();

                return hash;
            }
        }

        public static bool operator == (ShipKey a, ShipKey b) => a.Equals(b);

        public static bool operator != (ShipKey a, ShipKey b) => !a.Equals(b);

        #endregion

        #region Get
        
        public static ShipKey Get(Faction faction, byte mark, GameEntityTypeData type)
        {
            var formark = type.GetForMark( mark );
            
            var key = new ShipKey()
            {
                TypeData = type,
                Faction = faction,
                Unclaimed = false,
                ForMark = formark,
            };
            
            return key;
        }
        
        public static ShipKey Get(GameEntity_Squad squad)
        {
            var type = squad.TypeData;
            var fac = squad.GetFactionOrNull_Safe();
            var formark = squad.DataForMark;
            var unclaimed = fac != null && fac.Type == FactionType.NaturalObject;
            
            if (unclaimed)
            {
                var localPlayer = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if (localPlayer != null)
                    fac = localPlayer;
                
                formark = type.GetForMark( fac.GetGlobalMarkLevelForShipLine( type ) );
            }
            
            var key = new ShipKey()
            {
                TypeData = type,
                Faction = fac,
                Unclaimed = unclaimed,
                ForMark = formark,
            };
            
            return key;
        }
        
        public static ShipKey Get(FleetMembership mem)
        {
            var type = mem.TypeData;
            var fac = mem.Fleet.Faction;
            var formark = mem.ForMark;
            if (formark == null)
                formark = mem.TypeData.MarkStatsFor( mem.EffectiveMark );
                
            var unclaimed = mem.Fleet.Faction.Type == FactionType.NaturalObject;
            
            if (unclaimed)
            {
                var localPlayer = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if (localPlayer != null)
                    fac = localPlayer;
                
                formark = type.GetForMark( fac.GetGlobalMarkLevelForShipLine( type ) );
            }

            var key = new ShipKey()
            {
                TypeData = type,
                Faction = fac,
                Unclaimed = unclaimed,
                ForMark = formark,
            };
            
            return key;
        }

        public static ShipKey Get(LineGrant line)
        {
            var type = line.Type;
            
            var fac = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            
            var mk = 0;
            if (fac != null)
                mk = fac.GetGlobalMarkLevelForShipLine( type );
            
            var formark = type.GetForMark(mk);

            var key = new ShipKey()
            {
                TypeData = type,
                Faction = fac,
                Unclaimed = true,
                ForMark = formark,
            };
            
            return key;
        }
        
        #endregion
    }
    #endregion
    
    #region ShipForDisplay
    public struct ShipForDisplay
    {
        public readonly ShipKey Key;
        public readonly EntityText.Config Config;
        
        public int OrderAdded;
        
        public GameEntityTypeData TypeData => Key.TypeData;
        public GameEntityTypeData.MarkLevelStats ForMark => Key.ForMark;
        public Faction Faction => Key.Faction;
        public bool Unclaimed => Key.Unclaimed;
        
        public int Id;
        public int NumLines;
        public int Cap;
        public int Count;
        public ShipsOfStatusCollection Counts;
        public int CostSpentTowardsNext;
        public int CostToBuild;
        public int CurrentStrength;
        public int MaximumStrength;
        public bool ShowStrength;
        
        public TextStyle IconStyleOverride;
        
        public void Add( ShipForDisplay ship )
        {
            if (this.Key != ship.Key)
                throw new ArgumentException("ship");
            
            this.NumLines += ship.NumLines;
            this.Cap += ship.Cap;
            this.Count += ship.Count;
            this.Counts += ship.Counts;
            this.CostSpentTowardsNext += ship.CostSpentTowardsNext;
            this.CostToBuild += ship.CostToBuild;
            this.CurrentStrength += ship.CurrentStrength;
            this.MaximumStrength += ship.MaximumStrength;
        }

        public ShipForDisplay( GameEntity_Squad squad, EntityText.Config config )
            : this()
        {
            this.Config = config;
            this.Key = ShipKey.Get(squad);

            Reset();
            Add(squad);
        }
        
        public void Reset()
        {
            NumLines = 0;
            Cap = 0;
            Count = 0;
            Counts = new ShipsOfStatusCollection();
            CostSpentTowardsNext = 0;
            CostToBuild = 0;
            CurrentStrength = 0;
            MaximumStrength = 0;
            OrderAdded = -1;
            ShowStrength=false;
            IconStyleOverride = null;
            Id = -1;
        }
        
        public void Add( GameEntity_Squad squad )
        {
            if ( squad == null )
                throw new ArgumentNullException( "squad" );

            var key = ShipKey.Get(squad);
            if (key != this.Key)
                throw new ArgumentException(string.Format("squad.key != this.key;\nthis.key={0}\nsquad.key={1}", ObjToStr.Format(this.Key, ObjToStr.Style.TypeAndMembersSingleLine), ObjToStr.Format(key, ObjToStr.Style.TypeAndMembersSingleLine)));

            Count += squad.ShipCount;
            Counts[ShipIconStatus.Alive] += squad.ShipCount;
            CurrentStrength += squad.GetStrengthOfStack();
        }

        public ShipForDisplay( ShipLineEntry entry, EntityText.Config config )
            : this(new LineGrant(entry.TypeData, entry.BaseNumShips), config)
        {
        }
        
        public void Add( ShipLineEntry entry )
        {
            Add(new LineGrant(entry.TypeData, entry.BaseNumShips));
        }
        
        public ShipForDisplay( LineGrant line, EntityText.Config config )
            : this()
        {
            this.Config = config;
            this.Key = ShipKey.Get(line);

            Reset();
            Add(line);
        }
        
        public void Add( LineGrant line )
        {
            var key = ShipKey.Get(line);
            if (key != this.Key)
                throw new ArgumentException("entry.key != this.key");
            
            int base_cap = line.GetNumShipsForHackAndHacker( Config.ActiveHackerAgainstUs, Config.ActiveHackAgainstUs );
            int cap = TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( TypeData, base_cap, base_cap, ForMark.MarkLevel );
            
            NumLines += 1;
            Cap += cap;
            MaximumStrength += ForMark.GetCalculatedStrengthPerSquadForFleetOrNull(null) * Cap;
        }
        
        public ShipForDisplay( Faction faction, byte mark, GameEntityTypeData type, int count, EntityText.Config config )
            : this()
        {
            this.Config = config;

            this.Key = new ShipKey()
            {
                TypeData = type,
                ForMark = type.GetForMark(mark),
                Faction = faction,
                Unclaimed = false,
            };
            
            Reset();
            Add(this.Key, count);
        }
        
        public void Add( ShipKey key, int count )
        {
            if (key != this.Key)
                throw new ArgumentException("key != this.key");
            
            Count += count;
            CurrentStrength += ForMark.GetCalculatedStrengthPerSquadForFleetOrNull(null) * count;
        }
        
        public ShipForDisplay( FleetMembership mem, EntityText.Config config )
            : this()
        {
            this.Config = config;
            this.Key = ShipKey.Get(mem);
            
            Reset();
            Add(mem);
        }

        public void Add( FleetMembership mem )
        {
            if ( mem == null )
                throw new ArgumentNullException( "mem" );

            if (this.TypeData != mem.TypeData)
                throw new ArgumentException(string.Format("argument 'mem' is of type '{1}' but we are for type '{2}'", mem.TypeData.InternalName, this.TypeData.InternalName));
            
            //if (this.ForMark != mem.ForMark)
                //throw new ArgumentException(string.Format("argument 'mem' is for mark '{1}' but we are for mark '{2}'", mem.ForMark.MarkLevel.InternalName, this.ForMark.MarkLevel.InternalName));
         
            int debugstage = 0;
            try
            {
                debugstage = 100;
                
                int cap_inc = 0;
                int count_inc = 0;
                int num_alive = 0;
                int num_crippled = 0;
                int num_remains = 0;
                int numloaded = mem.CalculateTransportedContentsCount();
                int numloaded_drones = mem.NumberCreatedButNotDeployed;
                
                debugstage = 101;
                
                foreach ( GameEntity_Squad e in mem.Entities )
                {
                        debugstage = 102;
                        if (e.GetIsCrippled())
                            num_crippled += e.ShipCount;
                        else 
                        if (e.GetIsRemains())
                            num_remains += e.ShipCount;
                        else
                            num_alive += e.ShipCount;

                        debugstage = 103;
                }

                debugstage = 104;
                if (this.Unclaimed)
                {
                    debugstage = 105;
                    cap_inc = mem.GetMaxTotalCount_ForUIOnly(true, this.Faction);
                    debugstage = 106;
                    count_inc = mem.GetCurrentTotalCount_ForUIOnly();
                }
                // if this line costs city sockets to build
                // then don't show them as having a cap/max
                // since it will almost always be less than that.
                
                else
                if (mem.TypeData.CitySocketCost > 0)
                {
                    debugstage = 107;
                    count_inc = mem.GetCurrentTotalCount_ForUIOnly();
                }
                
                // same for self-building in general
                // we show [count_alive]/[count_remains] not [count][cap]
                // actually, this depends on the type of fleet
                // for battlestations we do want to show [built]/[cap]
                /*
                else
                if (mem.TypeData.SelfConstructs)
                {
                    bool show_only_built = false;
                    if (mem.Fleet?.Centerpiece?.TypeData?.IsMobile ?? mem.Fleet?.Category == FleetCategory.PlayerPlanetaryCommand ||)
                    {
                        
                    }
                    
                    if (mem.Fleet.Category == FleetCategory.)
                    count_inc = mem.GetCurrentTotalCount_ForUIOnly();
                }
                */
                else
                {
                    //int a = mem.GetBaseSquadCapWithAdditions();
                    //int b = mem.EffectiveSquadCap;    
                    //if (a != b)
                    //{
                    //    LOG.Msg("fleet {0} mem {1} base+add={2} effective={3}", mem.Fleet, mem, a, b);
                    //}
                    debugstage = 108;
                    //cap_inc = mem.GetBaseSquadCapWithAdditions();
                    cap_inc = mem.GetMaxTotalCount_ForUIOnly(false, this.Faction);
                    count_inc = mem.GetCurrentTotalCount_ForUIOnly();
                }
                
                debugstage = 109;
                
                NumLines += 1;
                
                Cap += cap_inc;
                Count += count_inc;
                
                debugstage = 110;
                
                Counts[ShipIconStatus.Alive] += num_alive;
                Counts[ShipIconStatus.Crippled] += num_crippled;
                Counts[ShipIconStatus.Remains] += num_remains;
                Counts[ShipIconStatus.BeingTransported] += numloaded;
                Counts[ShipIconStatus.LoadedDrone] += numloaded_drones;

                debugstage = 111;
                
                CostSpentTowardsNext = mem.MetalSpentConstructingCurrentReplacement.GetNearestIntPreferringHigher();
                CostToBuild = ForMark.MetalCost;
                
                debugstage = 112;
                
                int strPer = this.ForMark.GetCalculatedStrengthPerSquadForFleetOrNull( mem );
                CurrentStrength += count_inc * strPer;
                MaximumStrength += cap_inc * strPer;
                
                debugstage = 113;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
        }
        
        public void AppendVarValue(string key, TextStyle style, ArcenCharacterBufferBase buffer, object args)
        {
            int debugstage = 0;
            try
            {
                debugstage = 100;
                if ( key.Equals("icon") )
                {
                    debugstage = 110;
                    buffer.AddShipIconInline( this.TypeData, this.Faction, this.IconStyleOverride??style );
                    
                    return;
                }

                debugstage = 120;
                if (key.Equals("marklevel"))
                {
                    debugstage = 130;
                    if (!string.IsNullOrEmpty(this.ForMark?.MarkLevel?.MapDisplay))
                    {
                        buffer
                            .StartColor(this.ForMark.MarkLevel.ColorHex)
                            //.Add("Mk")
                            .Add(this.ForMark.MarkLevel.MapDisplay)
                            .EndColor();
                    }
                    
                    return;
                }

                debugstage = 140;
                if (key.Equals("displayname"))
                {
                    debugstage = 145;
                    if (this.Config.ExtraFlags.HasFlag(ShipExtraDetailFlags.HighestDetail))
                    {
                        buffer.Add(this.TypeData.GetDisplayName());
                        
                        return;
                    }
                    
                    debugstage = 146;
                    if (this.Config.Detail > TooltipDetail.SuperShort)
                    {
                        buffer.Add(this.TypeData.GetShortDisplayName());
                    }
                    
                    return;
                }
                
                debugstage = 150;
                if (key.Equals("countunbuilt"))
                {
                    debugstage = 155;
                    
                    int num = this.Cap - this.Count;
                    if ( num > 0 )
                    {
                        buffer.Add("×").Add( num );
                    }

                    return;
                }
                
                debugstage = 160;
                if (key.Equals("countpercent"))
                {
                    debugstage = 165;
                    if (!this.Unclaimed && 
                        this.Cap > 0)
                    {
                        debugstage = 166;
                        int max = this.Cap;
                        int count = this.Count;
                        FInt perc = FInt.OneHundred;
                        if ( max > 0 )
                            perc = ((int)(100.0f * count / max)).ToFInt();
                        
                        debugstage = 167;
                        buffer.AddVarReplace(TextVarMap.Parenthetical, (a,b,c,d)=>c.AddPercentageInColor( perc, true, false ), null);
                    }
                            
                    return;
                }
                
                debugstage = 170;
                if (key.Equals("count"))
                {
                    debugstage = 171;
                    int max = this.Cap;
                    int count = this.Count;
                    
                    if (this.Unclaimed)
                    {
                        debugstage = 172;
                        buffer.Add("×").Add( max );
                    }
                    /*
                    else 
                    if (this.TypeData.SelfConstructs)
                    {
                        debugstage = 172;
                        max = count;
                        count = this.Counts[ShipIconStatus.Alive];
                        
                        buffer.Add("<size=60%>×</size>");
                        buffer.Add( count );
                        
                        if (max > 0)
                        {
                            debugstage = 173;
                            if (count != max)
                            {
                                debugstage = 174;
                                buffer.Open(TextStyle.Fraction_Gray).Add( "/" ).Add( Mathf.Max(max, 0) ).Close(TextStyle.Fraction_Gray);
                            }
                        }
                    }*/
                    else
                    if (max == 0)
                    {
                        debugstage = 175;
                        if (count > 1)
                        {
                            debugstage = 176;
                            buffer.Add("×").Add( count );
                        }
                    }
                    else
                    {
                        debugstage = 177;
                        buffer.Add("×");
                        buffer.Add( count );
                        
                        if (max > 0)
                        {
                            debugstage = 178;
                            if (count != max)
                            {
                                debugstage = 179;
                                buffer.Open(TextStyle.Fraction_Gray).Add( "/" ).Add( Mathf.Max(max, 0) ).Close(TextStyle.Fraction_Gray);
                            }
                        }
                    }

                    //LOG.Msg("{0}.count took stage {1}", this.TypeData.InternalName, debugstage);
                    return;
                }
                
                debugstage = 180;
                if (key.Equals("buildprogress"))
                {
                    debugstage = 190;
                    
                    if (this.CostToBuild > 0)
                    {
                        var perc = ((int)((float)this.CostSpentTowardsNext / (float)this.CostToBuild * 100.0f)).ToFInt();
                        buffer.AddVarReplace(TextVarMap.Parenthetical, (a,b,c,d)=> c.AddPercentageInColor( perc, true, false ), null);
                    }

                    return;
                }
                
                debugstage = 200;
                if (key.Equals("techs"))
                {
                    if ( this.Unclaimed &&
                         this.TypeData.TechUpgradesThatBenefitMe.Count > 0 &&
                         this.Config.Detail >= TooltipDetail.Medium )
                    {
                        //buffer.Open(TextStyle.Inline_Techs);
                        
                        for ( int j = 0; j < this.TypeData.TechUpgradesThatBenefitMe.Count; j++ )
                        {
                            var upg = this.TypeData.TechUpgradesThatBenefitMe[j];
                            //if (j > 0)
                                //buffer.Add( "|" );
                            buffer.Writer().WriteTech(buffer, upg, this.Faction, true, true, ref debugstage );
                        }
                        
                        //buffer.Close(TextStyle.Inline_Techs);
                    }
                    
                    return;
                }
                
                debugstage = 220;
                if (key.Equals("aip"))
                {
                    if ( this.Unclaimed &&
                         this.TypeData.AIPWhenGrantedByHack > 0 )
                    {
                        buffer.Add(" ").Open(TextTerm.AIP, TermUse.Icon).Add("+").Add(this.TypeData.AIPWhenGrantedByHack.IntValue).Close(TextTerm.AIP);
                    }
                        
                    return;
                }
                
                debugstage = 220;
                if (key.Equals("strength"))
                {
                    if (ShowStrength)
                        buffer.Open(TextTerm.Strength, TermUse.Icon).AddNumber(this.MaximumStrength/1000.0f, null, TextStyle.Empty).Close(TextTerm.Strength);
                        
                    return;
                }
                
                debugstage = 230;
                if (key.Equals("id"))
                {
                    if ( this.Id != -1 )
                    {
                        buffer.Add(" #").Add(this.Id,TextStyle.Brighter);
                    }
                        
                    return;
                }
                
                // not found
                {
                    debugstage = 500;
                    buffer.Add("{").Add(key).Add("}");   
                }
                
                debugstage = 1000;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} Mk{1} {2}/{3} ({4} A, {5} T, {6} D) in {7} Lines", 
                TypeData.InternalName, ForMark.MarkLevel.Ordinal, Count, Cap, Counts[ShipIconStatus.Alive], Counts[ShipIconStatus.BeingTransported], Counts[ShipIconStatus.LoadedDrone], NumLines);
        }

        public void Write( ArcenCharacterBufferBase buffer, TextVarMap format )
        {
            //var line = format.Line;
            var type = this.TypeData;
            int current = this.Count;
            
            int max;
            max = this.Cap;
            if ( max < current )
                max = current;
            
            if ( current <= 0 && max <= 0 )
                return;
            
            buffer.AddVarReplace(format, this.AppendVarValue );
        }
    }
    #endregion
    
    #region ShipsForDisplay
    public class ShipsForDisplay : IDisposable
    {
        public readonly Sys.List<ShipForDisplay> Ships;
        public string Delimiter;
        public TextVarMap Varmap;
        
        public int Strength
        {
            get
            {
                int result = 0;
                for (int i = 0; i < this.Ships.Count; i++)
                {
                    var ship = this.Ships[i];
                    result += ship.CurrentStrength;
                }
                return result;
            }
        }
        
        public int Strength_Max
        {
            get
            {
                int result = 0;
                for (int i = 0; i < this.Ships.Count; i++)
                {
                    var ship = this.Ships[i];
                    result += ship.MaximumStrength;
                }
                return result;
            }
        }

        #region Pooling
        
        private static readonly ConcurrentQueue<ShipsForDisplay> _pool = ConcurrentQueue<ShipsForDisplay>.Create_WillNeverBeGCed( "ShipsForDisplay._pool" );
        
        public static ShipsForDisplay Get()
        {
            ShipsForDisplay res;
            if ( !_pool.TryDequeue( out res ) )
                res = new ShipsForDisplay();

            return res;
        }
        
        public void Return()
        {
            this.Clear();
            _pool.Enqueue(this);    
        }
        
        #endregion
        
        public ShipsForDisplay()
        {
            Ships = new Sys.List<ShipForDisplay>();
        }
        
        public void Clear()
        {
            this.Ships.Clear();
            this.Delimiter = null;
            this.Varmap = null;
        }
        
        public void Dispose()
        {
            this.Return();
        }
        
        public void Add(ShipForDisplay item)
        {
            for (int i = 0; i < Ships.Count; i++ )
            {
                var itr = Ships[i];
                if (itr.Key == item.Key)
                {
                    itr.Add(item);
                    Ships[i] = itr;
                    
                    return;
                }
            }

            item.OrderAdded = Ships.Count;
            Ships.Add( item );
        }
        
        #region Sort
        
        public void Sort(int sort_mode)
        {
            
            //if ( sort_mode == 0 ) //Type > Name > Strength > Mark Level > Current Unit Count > Max Unit Count > Transporting Count
            {
                this.Ships.InsertionSort( 
                    (a, b)=>
                    {
                        int x;
                        
                        x = CompareType( a.TypeData, b.TypeData );
                        if ( x != 0 ) return x;
                        
                        x = b.MaximumStrength.CompareTo(a.MaximumStrength);
                        if ( x != 0 ) return x;
                        
                        x = b.Cap.CompareTo(a.Cap);
                        if ( x != 0 ) return x;

                        x = a.TypeData.GetDisplayName().CompareTo(b.TypeData.GetDisplayName());
                        if ( x != 0 )
                            return x;
                        
                        return a.OrderAdded.CompareTo(b.OrderAdded);
                    } );
            }
        }

        private static int CompareType( GameEntityTypeData a, GameEntityTypeData b )
        {
            int c = 0;
            
            c = a.NoExplicitCap.CompareTo(b.NoExplicitCap);
            if (c != 0) return c;
            c = a.IsDrone.CompareTo(b.IsDrone);
            if (c != 0) return c;
            c = a.IsTurret.CompareTo(b.IsTurret);
            if (c != 0) return c;
            c = a.IsStrikecraft.CompareTo(b.IsStrikecraft);
            if (c != 0) return c;

            c = (a.SpecialType == SpecialEntityType.Cruiser).CompareTo( (b.SpecialType != SpecialEntityType.Cruiser ) );
            if (c != 0) return c;
            c = (a.SpecialType == SpecialEntityType.Destroyer).CompareTo( (b.SpecialType != SpecialEntityType.Cruiser ) );
            if (c != 0) return c;
            c = a.IsGuardian.CompareTo(b.IsGuardian);
            if (c != 0) return c;
            
            return 0;
        }

        private static int CompareName( GameEntityTypeData a, GameEntityTypeData b )
        {
            return a.DisplayName.CompareTo( b.DisplayName );
        }

        private static int CompareStrength( ShipForDisplay a, ShipForDisplay b )
        {
            return a.MaximumStrength.CompareTo(b.MaximumStrength);
        }

        private static int CompareMark( byte a, byte b )
        {
            return a.CompareTo( b );
        }

        private static int CompareCount( int a, int b )
        {
            return a.CompareTo( b );
        }

        #endregion
    }
    #endregion
    
    public struct FleetText
    {
        public readonly GameEntity_Squad Squad;
        public readonly EntityText.Config Config;

        public FleetText(GameEntity_Squad squad, EntityText.Config config)
        {
            Squad = squad;
            Config = config;
        }
        
        public void Write(EntityTextWriter writer)
        {
            var buffer = writer.Buffer;
            
            // todo: purge this?
            FleetMembership relatedMembershipOrNull = Config.OptMembership;
            Fleet relatedMemFleetOrNull = Config.OptFleet;
            Faction localPlayerFactionOrNull = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            Faction relatedSquadFactionOrNull = Squad.GetFactionOrNull_Safe();
            Planet thisPlanetOrNull = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            PlanetFaction localPlayerPlanetFactionOrNull = thisPlanetOrNull?.GetPlanetFactionForFaction( localPlayerFactionOrNull );
            
            //var space_before_style = TextStyle.Get("Centerpiece_Space_Before");
            //var block_style = TextStyle.Get("Centerpiece_Block");
            //var label_style = TextStyle.Get("Centerpiece_Label");
            var extra_text_style = TextStyle.Get("Centerpiece_TimesCrippled");
            var member_line_style = TextStyle.Get("Fleet_Member_Line");
            var fraction_style = TextStyle.Get("Fraction");
            var fraction_gray_style = TextStyle.Get("Fraction_Gray");
            
            var config = this.Config;
            var ships = ShipsForDisplay.Get();
            
            int debugstage = 0;
            try
            {
                //buffer.Add(line2_style.OpenTags);
                
                #if false
                #region FleetDesignTemplateIUseForDrones
                if ( Squad.IsFakeEntity && 
                     Squad.TypeData.FleetDesignTemplateIUseForDrones != null &&
                     ( Squad.TypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag.GetHasItems() ||
                       Squad.TypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag.GetHasItems() ) )
                {
                    debugstage = 340;
                    if ( Config.Detail < TooltipDetail.Full )
                        buffer.Add( "Auto-builds drones of types: <color=#ffdf72>" );
                    else
                        buffer.Add( "Auto-builds drones of the following sorts, and releases them when threatened: <color=#ffdf72>" );
                    bool isFirst = true;

                    debugstage = 350;
                    DrawBag<FleetItem> drones = Squad.TypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag;
                    if ( drones != null )
                    {
                        for ( int j = 0; j < drones.InternalListSize; j++ )
                        {
                            FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                            if ( droneItem == null )
                                continue;
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( droneItem.TypeData.DisplayName );
                            buffer.Add( " x" );
                            int cap = droneItem.Cap;
                            if ( Squad.TypeData.MultipliedNonFrigateShipCapForDrones > FInt.Zero )
                                cap = (cap * Squad.TypeData.MultipliedNonFrigateShipCapForDrones).GetNearestIntPreferringHigher();
                            buffer.Add( cap );
                        }
                    }
                    drones = Squad.TypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag;
                    if ( drones != null )
                    {
                        for ( int j = 0; j < drones.InternalListSize; j++ )
                        {
                            FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                            if ( droneItem == null )
                                continue;
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( droneItem.TypeData.DisplayName );
                            buffer.Add( " x" );
                            int cap = droneItem.Cap;
                            if ( Squad.TypeData.MultipliedFrigateShipCapForDrones > FInt.Zero )
                                cap = (cap * Squad.TypeData.MultipliedFrigateShipCapForDrones).GetNearestIntPreferringHigher();
                            buffer.Add( cap );
                        }
                    }
                    buffer.Add( "</color>" ).EndStatement(EndStatementStyle.Normal);
                }
                #endregion
                
                #region FleetDesignTemplatesIAlwaysGrant
                if ( Squad.IsFakeEntity)
                {
                    debugstage = 6460;
                    if ( Squad.TypeData.FleetDesignTemplatesIAlwaysGrant != null && 
                         Squad.TypeData.FleetDesignTemplatesIAlwaysGrant.Count > 0 )
                    {
                        debugstage = 6461;
                        buffer.Add( "I am the centerpiece of what would become a fleet with the following items:  " );

                        debugstage = 6462;
                        for ( int j = 0; j < Squad.TypeData.FleetDesignTemplatesIAlwaysGrant.Count; j++ )
                        {
                            FleetDesignTemplate template = Squad.TypeData.FleetDesignTemplatesIAlwaysGrant[j];
                            var thisSquad = this.Squad;
                            
                            foreach ( FleetItem Item in template.FleetItems() )
                            {
                                if ( Item.TypeData == thisSquad.TypeData )
                                    continue;

                                bool isExcludedSpecialType = false;
                                switch ( Item.TypeData.SpecialType )
                                {
                                    case SpecialEntityType.AICommandStationOriginal:
                                    case SpecialEntityType.AICommandStationReconquest:
                                    case SpecialEntityType.BattlestationBasic:
                                    case SpecialEntityType.BattlestationCitadel:
                                    case SpecialEntityType.CityCenter:
                                    case SpecialEntityType.HumanHomeCommand:
                                    case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                                    case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                                    case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                                    case SpecialEntityType.MobileSupportFleetFlagship:
                                    case SpecialEntityType.NormalHumanCommandStation:
                                    case SpecialEntityType.NPCFactionCenterpiece:
                                    case SpecialEntityType.WardenSecretNinjaHideout:
                                        isExcludedSpecialType = true;
                                        break;
                                }
                                if ( isExcludedSpecialType )
                                    continue;


                                buffer.Add( Item.TypeData == null ? "nulltype" : Item.TypeData.DisplayName ).Add( " " );
                                //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                                int CapToShow = Item.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( Item.TypeData, Item.Cap, Item.Cap, thisSquad.DataForMark.MarkLevel );

                                buffer.StartColor( QuickColors.NewValue );
                                buffer.Add( " x" ).Add( CapToShow );
                                buffer.EndColor();

                                buffer.Add( "  " );
                            }
                        }
                    }
                }
                #endregion
                #endif
                //if ( !Squad.IsFakeEntity )
                {
                    #region Factory Things
                    debugstage = 5900;
                    if ( Squad.TypeData.HasFactoryFlows )
                    {
                        buffer.BeginStatement(TextStyle.Attr_Line);
                        
                        if ( Squad.GetIsCrippled() )
                            buffer.Add( "Crippled factories are unable to spend metal until they are repaired." );
                        else 
                        if ( Squad.GetIsNonFunctional() )
                            buffer.Add( "Non-functional factories are unable to spend metal until their functionality is restored." );
                        else 
                        if ( Squad.ComputeDisabledReason( ArcenRejectionReason.Unknown ) != ArcenRejectionReason.Unknown )
                            buffer.Add( "Disabled factories are unable to spend metal until they are re-enabled." );
                        else
                        {
                            debugstage = 5901;

                            var thisSquad = Squad;
                            
                            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                            {
                                    debugstage = 5910;
                                    
                                    if ( fleet == null )
                                        break;
                                    
                                    if ( fleet.IsFleetConstructionPaused )
                                        continue;
                                    
                                    if ( fleet.IsFleetConstructionBlocked )
                                        continue;
                                    
                                    if ( fleet.Faction == null || 
                                         fleet.Faction.Type != FactionType.Player )
                                        continue;
                                    
                                    //debugstage = 59102;
                                    //if ( fleet.Category != FleetCategory.PlayerMobile && fleet.Category != FleetCategory.PlayerCustomMobile )
                                    //    continue;
                                    debugstage = 59103;
                                    
                                    if ( fleet.SupportingFactoriesInRange.Count <= 0 )
                                        continue;

                                    debugstage = 59104;
                                    
                                    bool foundMyself = false;
                                    var supportingFactories = fleet.SupportingFactoriesInRange.GetDisplayList();
                                    for ( int i = 0; i < supportingFactories.Count; i++ )
                                    {
                                        try
                                        {
                                            debugstage = 59105;
                                            if ( supportingFactories[i].GetPrimaryKeyID() == thisSquad.PrimaryKeyID )
                                            {
                                                foundMyself = true;
                                                break;
                                            }
                                        } catch ( Exception ) { } //cross-threading issues
                                    }
                                    
                                    debugstage = 59106;
                                    if ( !foundMyself )
                                        continue;
                                    
                                    debugstage = 5911;
                                    
                                    Faction faction = fleet.Faction;
                                    bool areAllAtShipCap = true;

                                    ships.Clear();
                                    
                                    foreach ( FleetMembership mem in fleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                                    {
                                        debugstage = 5912;

                                        if ( mem.TypeData.IsDrone || mem.TypeData.SelfConstructs )
                                            continue;

                                        if ( mem.EffectiveSquadCap <= mem.EntitiesOfFMem.Count )
                                            continue;

                                        var canbuild = mem.GetCanBuildAnother( true, -1, ExtraFromStacks.IncludePrecalc );

                                        if (canbuild == ArcenRejectionReason.GalaxyWideCapForPlayersHasBeenHit ||
                                            canbuild == ArcenRejectionReason.FactionDoesNotHaveEnoughCap ||
                                            canbuild == ArcenRejectionReason.NotEnoughCitySockets)
                                        {
                                            continue;
                                        }

                                        areAllAtShipCap = false;

                                        var ship = new ShipForDisplay(mem, config);
                                        ships.Add(ship);
                                    }

                                    if (ships.Ships.Count == 0)
                                    {
                                        buffer.Open(TextStyle.Attr_Line);
                                        
                                        if ( areAllAtShipCap )
                                        {
                                            buffer.Add( "Finished all construction for fleet " )
                                                .AddFactionColoredString(fleet.GetName(), fleet.Faction).Add(".");
                                        }
                                        else 
                                        if ( faction != null && 
                                             faction.NetEnergy <= 0 )
                                        {
                                            buffer.Add( "Unable to build for fleet " )
                                                .AddFactionColoredString(fleet.GetName(), fleet.Faction)
                                                .Add( " because of insufficient Energy." );
                                        }
                                        else
                                        {
                                            buffer.Add( "Unable to build for fleet " )
                                                .AddFactionColoredString(fleet.GetName(), fleet.Faction)
                                                .Add( " because some ship lines blocked." );
                                        }
                                        
                                        buffer.Close(TextStyle.Attr_Line);
                                    }
                                    else
                                    {
                                        buffer.Open(TextStyle.Attr_Line);
                                        
                                        buffer.Add( "Building for " ).AddFactionColoredString( fleet.GetName(), faction ).Add(": ");
                                                
                                        ships.Delimiter = " ";
                                        ships.Varmap = TextVarMap.Inline_Ship_Format_Building;
                                    
                                        buffer
                                            .Open(TextStyle.Fleet_Members_Inline)
                                            .WriteShips(ships)
                                            .Close(TextStyle.Fleet_Members_Inline);

                                        ships.Delimiter = null;
                                        ships.Varmap = null;
                                        
                                        buffer.Close(TextStyle.Attr_Line);
                                    }
                                        
                                }
                        }
                        
                        buffer.EndStatement(TextStyle.Attr_Line);
                    }
                    #endregion

                    #region Construction Blocked
                    debugstage = 5950;
                    var constructionBlockedReason = Squad.GetIsSelfConstructionBlocked();
                    if ( constructionBlockedReason != ArcenRejectionReason.Unknown )
                    {
                        debugstage = 5951;
                        if ( relatedMemFleetOrNull != null && 
                             relatedMemFleetOrNull.Category == FleetCategory.PlayerPlanetaryCommand )
                        {
                            buffer.Add( "<color=#ff5842>Cannot be constructed without fully-built command station here!</color>", TextStyle.Newline_NoLabel );
                        }
                        else
                        {
                            buffer.Add( "<color=#ff5842>Cannot be constructed without its flagship being here and non-crippled.</color>", TextStyle.Newline_NoLabel );
                        }

                        if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowConstructionBlockedReason" ) )
                        {
                            buffer.Add( " [" ).Add( Extensions.ToString(constructionBlockedReason) ).Add( "]" );
                        }
                    }
                    #endregion
                    
                    #region Fire-Delay Unloading
                    if ( Squad.TypeData.FiringDelayForTransportedShips > FInt.Zero &&
                         ( Squad.IsFakeEntity || Squad.GetFactionTypeSafe() == FactionType.Player ) &&
                           Squad.TypeData.FiringDelayForTransportedShips > 6 )
                    {
                        if ( Config.Detail >= TooltipDetail.Medium )
                        {
                            buffer
                                .Open(extra_text_style)
                                .Add( "Ships unloading have a " ).Add( Squad.TypeData.FiringDelayForTransportedShips, "a1ffa1" ).Add( " second delay before firing." )
                                .Close(extra_text_style);
                        }
                    }
                    #endregion

                    #region Fleet Centerpiece Things
                    debugstage = 5460;
                    if ( relatedMembershipOrNull == null || 
                         relatedMemFleetOrNull == null )
                    {
                        //debugstage = 546100;
                        //if ( Squad.HasBeenRemovedFromSim || Squad.ToBeRemovedAtEndOfThisFrame )
                        //    buffer.Add( "<color=#ff5842>This ship has died.</color>  " );
                        //else
                        //    buffer.Add( "<color=#ff5842>ERROR: my FleetMembership is null!</color>  " );
                    }
                    else 
                    if ( relatedMemFleetOrNull.Centerpiece.GetSquad() == Squad )
                    {
                        bool isCity = Squad.TypeData.SpecialType == SpecialEntityType.CityCenter;
                        
                        ships.Clear();
                        
                        int counter = 0;
                        foreach ( FleetMembership mem in relatedMemFleetOrNull.MemberGroupsUnsorted_Sim )
                        {
                                var item = new ShipForDisplay(mem, config);
                                //LOG.Msg("#{0} : {1}", counter, item);
                                counter++;
                                ships.Add(item);
                                
                            }

                        ships.Sort(0);
                        
                        debugstage = 546200;
                        
                        buffer.Open(TextStyle.Attr_Line);
                        
                        buffer
                            .Add("Fleet Leader", TextStyle.Attr_Label).Add(": ")
                            .Add(" This is the centerpiece of ").AddFactionColoredString(relatedMemFleetOrNull.GetName(), Squad.GetFactionOrNull_Safe());

                        debugstage = 546300;

                        {
                            float currentStrength = ships.Strength / 1000.0f;
                            float maxStrength = ships.Strength_Max / 1000.0f;
                            float perc = 0.0f;
                            if (maxStrength > 0)
                                perc = (currentStrength / maxStrength) * 100.0f;
                            
                            buffer.Add(" ");
                            buffer.Open(TextTerm.Strength, TermUse.Icon);
                            if ( Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                            {
                                buffer.AddNumber(maxStrength, null, TextStyle.Empty);
                            }
                            else
                            {
                                buffer.AddNumber(currentStrength, null, TextStyle.Empty);
                                if (currentStrength < maxStrength)
                                {
                                    buffer.Open(fraction_style).Add( "/" ).AddNumber( maxStrength, null, TextStyle.Empty ).Close(fraction_style);
                                    buffer.Add( " " ).AddVarReplace(TextVarMap.InParenthesis, null, (a,b,c,d)=>c.AddPercentageInColor(perc, true, false), null );
                                }
                            }

                            buffer.Close(TextTerm.Strength);
                        }
                        
                        debugstage = 546400;
                        
                        #region TransportMode (loading/unloading)
                        if ( relatedMemFleetOrNull != null && 
                             relatedMemFleetOrNull.IsFleetInTransportLoadMode && 
                             !isCity )
                        {
                            buffer.Add( "  " ).AddVarReplace(TextVarMap.InParenthesis, null, (a,b,c,d)=>c.Add("loading"), null );
                        }
                        #endregion
                        
                        debugstage = 546500;

                        #region City, Used/Total Sockets
                        if ( isCity && 
                             relatedMemFleetOrNull != null )
                        {
                            int totalCityPoints = relatedMemFleetOrNull.CalculateTotalCitySockets();
                            if ( totalCityPoints > 0 )
                            {
                                int spentCityPoints = relatedMemFleetOrNull.CalculateSpentCitySockets();
                                buffer
                                    .Open( TextStyle.Attr_Line2 )
                                    .Open( TextStyle.Attr_Label2 )
                                    .NewLineIfNeeded()
                                    .Add( Squad.TypeData.NameForCitySockets_Plural ).Add( " Used").Close(TextStyle.Attr_Label2)
                                    .Add(": ")
                                    .Add( spentCityPoints )
                                    .Open(TextStyle.Fraction).Add( "/" ).Add( totalCityPoints ).Close(TextStyle.Fraction)
                                    .Close( TextStyle.Attr_Line2 );
                            }
                        }
                        #endregion

                        /*
                        if ( Config.Detail >= TooltipDetail.Full && 
                             relatedMemFleetOrNull != null && 
                             relatedMemFleetOrNull.FleetOnFriendlyPlanet && 
                             !isCity )
                        {
                            buffer.Add( "This fleet is on friendly planets, and can rebuild its ships quicker" ).EndStatement(EndStatementStyle.Normal);
                        }
                        */

                        #region Fleet Members
                        debugstage = 5462;
                        if ( relatedMemFleetOrNull != null && 
                             localPlayerFactionOrNull != null )
                        {
                            var fleetinfo = relatedMemFleetOrNull.BaseInfo;
                            if (fleetinfo != null)
                            {
                                buffer.Open(TextStyle.Attr_Line2).NewLineIfNeeded();
                                fleetinfo.AddToTooltipForFleet( buffer, Config.Detail );
                                buffer.Close(TextStyle.Attr_Label2);
                            }
                            
                            buffer.Open(TextStyle.Fleet_Member_Line);
                            writer.WriteShips( ships, (itr)=>!itr.TypeData.IsFleetLeader );
                            buffer.Close(TextStyle.Fleet_Member_Line);
                        }
                        #endregion
                    }
                    
                    #endregion
                    
                    #region IsReinforcementLocation
                    if ( Squad.TypeData.IsReinforcementLocation )
                    {
                        buffer.BeginStatement(TextStyle.Attr_Line);
                        
                        buffer
                            .Add("AI-Garrison", TextStyle.Attr_Label).Add(": ")
                            .Add(" This is a reinforcement point");

                        int countOfItemTypes = 0;
                        float strengthOfItems = 0;
                        if (Squad.AIReinforcementPointContents != null)
                        {
                            for ( int i = 0; i < Squad.AIReinforcementPointContents.Count; i++ )
                            {
                                var content = Squad.AIReinforcementPointContents[i];
                                if ( content.RightItem > 0 )
                                {
                                    countOfItemTypes++;
                                    strengthOfItems += (content.RightItem * content.LeftItem.MarkStatsFor( Squad.CurrentMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership);
                                }
                            }
                        }
                        strengthOfItems /= 1000;
                            
                        if ( countOfItemTypes == 0 )
                        {
                            buffer.Add(".");
                        }
                        else
                        {
                            buffer
                                .Add(" containing ")
                                .Open(TextTerm.Strength, TermUse.Icon)
                                .AddNumber(strengthOfItems, null, TextStyle.Empty)
                                .Close(TextTerm.Strength);
                            
                            buffer.Open(member_line_style);
                            
                            ships.Clear();
                            //using (var ships2 = ShipsForDisplay.Get())
                            {
                                for ( int i = 0; i < Squad.AIReinforcementPointContents.Count; i++ )
                                {
                                    debugstage = 3857;
                                    
                                    var content = Squad.AIReinforcementPointContents[i];
                                    var ship = new ShipForDisplay(Squad.GetFactionOrNull_Safe(), Squad.CurrentMarkLevel, content.LeftItem, content.RightItem, Config);
                                    ships.Add(ship);
                                }
                                
                                buffer.WriteShips(ships);
                            }
                            
                            buffer.Close(member_line_style);
                        }
                        
                        buffer.EndStatement(TextStyle.Attr_Line);
                    }
                    #endregion
                    
                    #region Progenitor
                    if ( Squad.TypeData.BuildPointsPerSecond > 0 )
                    {
                        buffer.BeginStatement(TextStyle.Attr_Line);
                        
                        var squad = Squad;
                        var type = Squad.TypeData;
                        var cap = type.PersonalShipCapForPerSecondBuildPointConstruction;
                        var cost = type.BuildPointCostForPerSecondConstruction;
                        var rate = type.BuildPointsPerSecond;
                        var tag = type.TagToSpawnFromForBuildPointForPerSecondConstruction;
                        var types = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull(Squad.TypeData.TagToSpawnFromForBuildPointForPerSecondConstruction);
                        
                        
                        var str = 0;
                        var count = 0;
                        foreach ( var child in Squad.ChildSquads )
                        {
                            var e = child.GetSquad();
                            if ( e == null )
                                continue;
                            
                            if (e.TypeData.GetHasTag(tag))
                            {
                                count += e.ShipCount;
                                str += e.GetStrengthOfStack();
                            }
                        }
                        
                        //int count;
                        //int str;
                        //Squad.GetChildrenInfo(null, tag, -1, out count, out str);
                        
                        buffer.AddVarReplace(TextVarMap.Progenitor_Format, 
                            (a,b,c,d)=>
                                {
                                    debugstage = 160;

                                    if (a.Equals("count", StringComparison.OrdinalIgnoreCase))
                                    {
                                        debugstage = 170;
                                        
                                        buffer.Open(TextStyle.Number);
                                        
                                        if (squad.IsFakeEntity)
                                        {
                                            buffer.Add("<size=60%>×</size>").Add( cap );
                                        }
                                        else
                                        {
                                            buffer.Add("<size=60%>×</size>");
                                            buffer.Add( count );
                                            
                                            if (count != cap)
                                                buffer.Open(TextStyle.Fraction_Gray).Add( "/" ).Add( Mathf.Max(cap, 0) ).Close(TextStyle.Fraction_Gray);
                                        }
                                        
                                        buffer.Close(TextStyle.Number);
                    
                                        return;
                                    }
                                    
                                    if (a.Equals("strength", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (str > 0)
                                        {
                                            buffer
                                                .Open( TextTerm.Strength, TermUse.Icon, TextStyle.Empty )
                                                .AddNumber( str / 1000.0f, null, TextStyle.Empty )
                                                .Close( TextTerm.Strength );
                                        }

                                        return;
                                    }
                                    
                                    if (a.Equals("progress", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (squad.IsFakeEntity)
                                            return;
                                        
                                        var perc = ((int)((float)squad.BuildPoints / (float)cost * 100.0f)).ToFInt();
                                        buffer.AddVarReplace(TextVarMap.Parenthetical, (_a,_b,_c,_d)=> _c.AddPercentageInColor( perc, true, false ), null);
                                        
                                        return;
                                    }
                                    
                                    if (a.Equals("types", StringComparison.OrdinalIgnoreCase))
                                    {
                                        for (int i = 0; i < types.Count; i++)
                                        {
                                            var t = types[i];
                                            if (i > 0)
                                                buffer.Add(", ");
                                            
                                            buffer.WriteSpawn(t);
                                        }
                                        return;
                                    }
                                    
                                    if (a.Equals("children", StringComparison.OrdinalIgnoreCase))
                                    {
                                        using (var ships2 = ShipsForDisplay.Get())
                                        {
                                            foreach ( var child in squad.ChildSquads )
                                            {
                                                var e = child.GetSquad();
                                                if ( e == null )
                                                    continue;
                                                
                                                if (e.TypeData.GetHasTag(tag))
                                                    ships2.Add(new ShipForDisplay(e, config));
                                            }
                                            
                                            buffer.WriteShips(ships2);
                                        }
                                        
                                        return;
                                    }
                                    
                                    if (a.Equals("rate", StringComparison.OrdinalIgnoreCase))
                                    {
                                        float sec_per = (float)cost / rate.ToFloat();
                                        buffer.AddMinutesAndSeconds((int)sec_per);

                                        return;
                                    }
                                    
                                    if (a.Equals("cap", StringComparison.OrdinalIgnoreCase))
                                    {
                                        buffer.AddNumber(cap, null, b);

                                        return;
                                    }
                                });

                        /*
                         Progenitor: Produces a descendants over time {count}/{max} {strength} {progress}
                         {living children ships}
                        */

                        debugstage = 546300;

                        buffer.EndStatement(TextStyle.Attr_Line);
                    }
                    #endregion
                }
            }
            catch (Exception e)
            {
                LOG.Err("error at debugstage {0}\n{1}", debugstage, e);
            }
            finally
            {
                ships.Dispose();
            }
        }
    }
}
