using System;
using System.Collections.Generic;
using System.Linq;
using TrickCore;

namespace TrickCore
{
    public static class DropTableExtensions
    {
        public static DropTable<T> ToDropTable<T>(this IEnumerable<T> enumerable, Func<T, double> getDropWeight)
        {
            if (enumerable == null || getDropWeight == null) return new DropTable<T>();
            var list = enumerable.ToList();
            DropTable<T> dropTable = new DropTable<T>(list.Count);
            foreach (var obj in list) dropTable.Add(obj, getDropWeight.Invoke(obj));
            return dropTable;
        }
    
        public static DropTable<T2> ToDropTable<T, T2>(this IEnumerable<T> enumerable, Func<T, T2> getInstance, Func<T, double> getDropWeight)
        {
            if (enumerable == null || getInstance == null || getDropWeight == null) return new DropTable<T2>();
            var list = enumerable.ToList();
            DropTable<T2> dropTable = new DropTable<T2>(list.Count);
            foreach (var obj in list) dropTable.Add(getInstance.Invoke(obj), getDropWeight.Invoke(obj));
            return dropTable;
        }
    
        public static DropTable<T> ToDropTable<T>(this IDictionary<T, float> dict)
        {
            if (dict == null) return new DropTable<T>();
            DropTable<T> dropTable = new DropTable<T>(dict.Count);
            foreach (var obj in dict) dropTable.Add(obj.Key, obj.Value);
            return dropTable;
        }
    }
}