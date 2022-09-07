using System;

namespace Litdex.Utilities.BinaryEncoding
{
	/// <summary>
	///		Encode and decode in base 64.
	/// </summary>
	public static class Base64
	{
		/// <summary>
		///		Converts an array of 8-bit unsigned integers to its 
		///		equivalent string representation that is encoded with 
		///		base-64 digits.
		/// </summary>
		/// <param name="data">
		///		 An array of 8-bit unsigned integers.
		///	</param>
		/// <returns>
		///		The string representation, in base 64, of the contents <paramref name="data"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="data"/> is null or empty.
		/// </exception>
		public static string Encode(byte[] data)
		{
			if (data == null || data.Length == 0)
			{
				throw new ArgumentNullException(nameof(data), "Array is null or empty.");
			}

			return Convert.ToBase64String(data);
		}

		/// <summary>
		///		Converts the specified string, 
		///		which encodes binary data as base-64 digits, 
		///		to an equivalent 8-bit unsigned integer array.
		/// </summary>
		/// <param name="data">
		///		The string to convert.
		///	</param>
		/// <returns>
		///		An array of 8-bit unsigned integers that is equivalent to <paramref name="data"/>.
		///	</returns>
		///	<exception cref="ArgumentNullException">
		///	</exception>
		///	<exception cref="FormatException">
		///	</exception>
		public static byte[] Decode(string data)
		{
			return Convert.FromBase64String(data);
		}
	}
}