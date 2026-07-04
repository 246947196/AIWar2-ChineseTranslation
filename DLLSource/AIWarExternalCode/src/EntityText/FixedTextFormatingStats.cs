using Arcen.Universal;
using System;

using System.Linq;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class FixedTextFormatingStats
    {
        private readonly string _name;
        public string Name => _name;

        private TextStyle _style;
        public TextStyle Style
        {
            get
            {
                var val = TextStyle.Get(this.Name);
                if (val != _style)
                {
                    _style = val;
                }
                
                return _style;
            }
        }

        public bool HasColor => !string.IsNullOrEmpty(Color);
        public string Color => Style?.Color ?? _hexcolor;
        
        public bool HasSprite => !string.IsNullOrEmpty(Sprite);
        public string Sprite => Style?.Sprite ?? _sprite;
        
        public bool HasAfterSprite => !string.IsNullOrEmpty(AfterSprite);
        public string AfterSprite => _aftericon;

        private string _hexcolor;
        private string _sprite;
        private string _aftericon;
        public FixedTextFormatingStats(string name, string hexcolor = null, string sprite = null, string afterIcon = null)
        {
            _name = name;
            _hexcolor = hexcolor;
            _sprite = sprite;
            _aftericon = afterIcon;
        }
        
        [Obsolete]
        public string SpriteCustomColorStart => ToString_IconAndStartColor();
        
        public string ToString_IconAndStartColor(string color=null, bool geo=false)
        {
            var buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("ToString_IconAndStartColor.buffer");
            if (this.HasSprite)
            {
                buffer.AddSprite(this.Sprite, color??this.Color, geo);
                if (this.HasAfterSprite)
                    buffer.Add(this.AfterSprite, TextCaps.Normal);
            }
            
            buffer.StartColor(color??this.Color);
            
            return buffer.ToStringAndReturnToPool();
        }
        
        public string ToString_Icon(string color=null, bool geo=false)
        {
            var buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("ToString_Icon.buffer");
            if (this.HasSprite)
            {
                buffer.AddSprite(this.Sprite, color??this.Color, geo);
                if (this.HasAfterSprite)
                    buffer.Add(this.AfterSprite, TextCaps.Normal);
            }
            return buffer.ToStringAndReturnToPool();
        }
        
        public string ToString_Color()
        {
            var buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("ToString_Color.buffer");
            buffer.StartColor(this.Color);
            return buffer.ToStringAndReturnToPool();
        }
    }
}
