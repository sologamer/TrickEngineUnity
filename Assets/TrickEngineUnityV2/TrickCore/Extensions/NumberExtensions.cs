using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TrickCore
{
    public static class NumberExtensions
    {
        private static readonly Dictionary<int, string> RomanCache = new Dictionary<int, string>();

        public static (int left, int right) ConvertLongToTwoInts(this long data) => ((int) (data >> 32), (int) (data & 0xffffffffL));
        public static long ConvertTwoIntsIntoLong(this (int left, int right) tuple) => (long)tuple.left << 32 | (long)(uint)tuple.right;

        public static string IndexToRomanString(this int index)
        {
            return index == 0 ? "0" : index.ToRoman();
        }
        
        public static string ToRoman(this int number)
        {
            if ((number < 0) || (number > 3999)) return string.Empty;
            string ret = number < 1 ? string.Empty :
                number >= 1000 ? $"M{ToRoman(number - 1000)}" :
                number >= 900 ? $"CM{ToRoman(number - 900)}" :
                number >= 500 ? $"D{ToRoman(number - 500)}" :
                number >= 400 ? $"CD{ToRoman(number - 400)}" :
                number >= 100 ? $"C{ToRoman(number - 100)}" :
                number >= 90 ? $"XC{ToRoman(number - 90)}" :
                number >= 50 ? $"L{ToRoman(number - 50)}" :
                number >= 40 ? $"XL{ToRoman(number - 40)}" :
                number >= 10 ? $"X{ToRoman(number - 10)}" :
                number >= 9 ? $"IX{ToRoman(number - 9)}" :
                number >= 5 ? $"V{ToRoman(number - 5)}" :
                number >= 4 ? $"IV{ToRoman(number - 4)}" : $"I{ToRoman(number - 1)}";
            
            if (RomanCache.TryGetValue(number, out var cachedValue)) return cachedValue;
            
            RomanCache.Add(number, ret);
            return ret;
        }

        public static IEnumerable<float> SplitValue2SumPreviousValues(this IRandomizer rand, float value, int count, float pct)
        {
            var splitValue = SplitValue2(rand, value, count, pct).ToList();
            float sum = 0.0f;
            for (var index = 0; index < splitValue.Count; index++)
            {
                sum += splitValue[index];
                splitValue[index] = sum;
            }

            return splitValue;
        }

        public static IEnumerable<float> SplitValue2(this IRandomizer rand, float value, int count, float pct)
        {
            if (count <= 0) throw new ArgumentException("count must be greater than zero.", "count");
            var result = new float[count];

            float runningTotal = 0f;
            for (int i = 0; i < count; i++)
            {
                var remainder = value - runningTotal;
                float share;
                if (remainder > 0f)
                {
                    var v = (float) Math.Max(Math.Round(remainder / ((float) (count - i)), 2), .01f);
                    share = Math.Min((float) rand.Next(v * (1.0f - pct), v * (1.0f + pct)), value);
                }
                else
                    share = 0f;
                result[i] = share;
                runningTotal += share;
            }

            if (runningTotal < value) result[count - 1] += value - runningTotal;
            if (runningTotal > value) result[count - 1] += value - runningTotal;

            return result;
        }

        public static int RoundValue(this int value, int nearest)
        {
            return (int)(Math.Ceiling(value / (float)nearest) * nearest);
        }

        public static float RoundValue(this float value, int decimals)
        {
            double div = Math.Pow(10, decimals);
            return (float)(Math.Round(value * div) / div);
        }

        public static float CeilValue(this float value, int decimals)
        {
            double div = Math.Pow(10, decimals);
            return (float)(Math.Ceiling(value * div) / div);
        }

        public static float FloorValue(this float value, int decimals)
        {
            double div = Math.Pow(10, decimals);
            return (float)(Math.Floor(value * div) / div);
        }

        private static readonly string[] SizeSuffixes = 
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(this long byteCount, int decimalPlaces = 1)
        {
            if (byteCount == 0 || byteCount == long.MinValue)
                return "0 " + SizeSuffixes[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), decimalPlaces);
            return $"{(Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture)} {SizeSuffixes[place]}";
        }

        public static Vector3 MultiplyXYZ(this Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }
    }
}