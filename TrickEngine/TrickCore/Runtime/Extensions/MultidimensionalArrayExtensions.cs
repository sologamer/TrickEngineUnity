using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TrickCore
{
    public static class MultidimensionalArrayExtensions
    {
        public static int GetFirstEmptyRowIndex<T>(this T[,] array) where T : struct
        {
            int width = array.GetLength(0);
            int height = array.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                bool allDefault = true;
                for (int x = 0; x < width; x++)
                {
                    if (!EqualityComparer<T>.Default.Equals(array[x, y], default))
                    {
                        allDefault = false;
                        break;
                    }
                }
                if (allDefault) return y;
            }
            return height;
        }
        
        public static T[] GetRow<T>(this T[,] array, int row) where T : struct
        {
            if (!typeof(T).IsPrimitive)
                throw new InvalidOperationException("Not supported for managed types.");

            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int cols = array.GetUpperBound(1) + 1;
            T[] result = new T[cols];

            int size;

            if (typeof(T) == typeof(bool))
                size = 1;
            else if (typeof(T) == typeof(char))
                size = 2;
            else
                size = Marshal.SizeOf<T>();

            Buffer.BlockCopy(array, row*cols*size, result, 0, cols*size);

            return result;
        }
    }
}