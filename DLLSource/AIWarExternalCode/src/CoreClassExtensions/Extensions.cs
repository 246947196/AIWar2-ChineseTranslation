using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public static partial class Extensions
    {
        private static Sys.Dictionary<Type, Sys.Dictionary<Enum, string>> _lookup = new Sys.Dictionary<Type, Sys.Dictionary<Enum, string>>();
        private static Sys.Dictionary<Type, Array> _valueslookup = new Sys.Dictionary<Type, Array>();
        private static Sys.Dictionary<Type, string[]> _nameslookup = new Sys.Dictionary<Type, string[]>();
        
        // technically not an extension
        public static string ToString<T>( T e ) where T : Enum
        {
            var type = typeof(T);
            
            Sys.Dictionary<Enum, string> names;
            if (!_lookup.TryGetValue(type, out names))
            {
                names = new Sys.Dictionary<Enum, string>();
                _lookup[type] = names;
            }
            
            string name;
            if (!names.TryGetValue(e, out name))
            {
                name = e.ToString();
                names[e] = name;
            }
            
            return name;
        }
        
        // technically not an extension
        public static Array GetEnumValues<T>() where T : Enum
        {
            return GetEnumValues(typeof(T));
        }
        public static Array GetEnumValues(Type type)
        {
            Array values;
            if (!_valueslookup.TryGetValue(type, out values))
            {
                values = type.GetEnumValues();
                _valueslookup[type] = values;
            }
            
            return values;
        }

        // technically not an extension
        public static string[] GetEnumNames<T>() where T : Enum
        {
            return GetEnumNames(typeof(T));
        }
        public static string[] GetEnumNames(Type type)
        {
            string[] names;
            if (!_nameslookup.TryGetValue(type, out names))
            {
                names = type.GetEnumNames();
                _nameslookup[type] = names;
            }
            
            return names;
        }
    }
}
