using System;

namespace Litdex.Utilities.Extension
{
	/// <summary>
	///		Array extensions.
	/// </summary>
	public static class ArrayExtensions
	{
		/// <summary>
		///		Clear all element inside the array.
		/// </summary>
		/// <param name="arr">
		///		<see cref="Array"/> to clear.
		/// </param>
		public static void Clear(this Array arr)
		{
			Array.Clear(arr, 0, arr.Length);
		}

		/// <summary>
		///		Slice the array to create a new copy of new array.
		/// </summary>
		/// <typeparam name="T">
		///		The type of objects in array.
		/// </typeparam>
		/// <param name="array">
		///		An array that want to slice.
		/// </param>
		/// <param name="start">
		///		The start index to slice the array.
		/// </param>
		/// <returns>
		///		A deep copy of elements from the specified start index until the last element from the array.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///		- Start index can't be negative number and larger than arrray length.
		///		- Requested length can't exceed from remaining length of array after the start index.
		/// </exception>
		public static T[] Slice<T>(this T[] array, int start)
		{
			return ArrayExtensions.Slice(array, start, array.Length - start);
		}

		/// <summary>
		///		Slice the array to create a new copy of new array.
		/// </summary>
		/// <typeparam name="T">
		///		The type of objects in array.
		/// </typeparam>
		/// <param name="array">
		///		An array that want to slice.
		/// </param>
		/// <param name="start">
		///		The start index to slice the array.
		/// </param>
		/// <param name="length">
		///		A requested length of how many element to slice from the start index.
		/// </param>
		/// <returns>
		///		A deep copy of elements between the specified start index and requested length.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///		- Start index can't be negative number and larger than arrray length.
		///		- Requested length can't exceed from remaining length of array after the start index.
		/// </exception>
		public static T[] Slice<T>(this T[] array, int start, int length)
		{
			if (start < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(start), "Start index can't be negative, must positive number.");
			}

			if (length < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(length), "Requested length can't be negative, must positive number.");
			}

			if (start > array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(start), "Start index can't be larger than array length.");
			}

			if (length > array.Length - start)
			{
				throw new ArgumentOutOfRangeException(nameof(length), "Requested length can't exceed from remaining length of array after the start index.");
			}

#if NET5_0_OR_GREATER

			var span = new Span<T>(array);

			return span.Slice(start, length).ToArray();

#else

			var temp = new T[length];

			Array.Copy(array, start, temp, 0, length);

			return temp;

#endif
		}

		/// <summary>
		///		Concat with another array to create a new array.
		/// </summary>
		/// <typeparam name="T">
		///		The type of objects in array.
		/// </typeparam>
		/// <param name="currentArray">
		///		
		/// </param>
		/// <param name="array">
		///		Array to concat.
		/// </param>
		/// <returns>
		///		Concation of 2 array.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		The two or one of the array is null.
		/// </exception>
		public static T[] Concat<T>(this T[] currentArray, T[] array)
		{
			if (currentArray == null)
			{
				throw new ArgumentNullException(nameof(currentArray), "Current array is null.");
			}

			if (array == null)
			{
				throw new ArgumentNullException(nameof(array), "Array is null.");
			}

			var temp = new T[currentArray.Length + array.Length];

			currentArray.CopyTo(temp, 0);
			array.CopyTo(temp, currentArray.Length);

			return temp;
		}

	}
}
