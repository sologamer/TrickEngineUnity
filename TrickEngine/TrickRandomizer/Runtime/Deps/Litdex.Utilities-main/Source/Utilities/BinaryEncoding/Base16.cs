using System;

namespace Litdex.Utilities.BinaryEncoding
{
	/// <summary>
	///		Encode and decode in base 16 (hexadecimal).
	/// </summary>
	public static class Base16
	{
		/// <summary>
		///		Encode <paramref name="bytes"/> to hexadecimal <see cref="string"/>.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/>s to encode.
		///	</param>
		/// <param name="upperCase">
		///		Hexadecimal output <see cref="string"/> letter case. Default is <see langword="true"/> to upper case.
		///	</param>
		/// <returns>
		///		Hexadecimal <see cref="string"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="bytes"/> is null or empty.
		/// </exception>
		public static string Encode(byte[] bytes, bool upperCase = true)
		{
			if (bytes == null || bytes.Length == 0)
			{
				throw new ArgumentNullException(nameof(bytes), "Array can't null or empty.");
			}

			if (upperCase)
			{
#if NET5_0_OR_GREATER
				return Convert.ToHexString(bytes);
#else
				return EncodeUpper(bytes);
#endif
			}
#if NET5_0_OR_GREATER
			return Convert.ToHexString(bytes).ToLower();
#else
			return EncodeLower(bytes);
#endif
		}

		/// <summary>
		///		Encode array of <see cref="byte"/>s to hexadecimal <see cref="string"/> in upper case.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/>s to encode.
		///	</param>
		/// <returns>
		///		Hexadecimal <see cref="string"/> in upper case.
		/// </returns>
		private static string EncodeUpper(byte[] bytes)
		{
			var c = new char[bytes.Length * 2];
			int b;
			for (var i = 0; i < bytes.Length; i++)
			{
				b = bytes[i] >> 4;
				c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
				b = bytes[i] & 0xF;
				c[(i * 2) + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
			}
			return new string(c);
		}

		/// <summary>
		///		Encode array of <see cref="byte"/>s to hexadecimal string in lower case.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/>s to encode.
		///	</param>
		/// <returns>
		///		Hexadecimal <see cref="string"/> in lower case.
		/// </returns>
		private static string EncodeLower(byte[] bytes)
		{
			var c = new char[bytes.Length * 2];
			int b;
			for (var i = 0; i < bytes.Length; i++)
			{
				b = bytes[i] >> 4;
				c[i * 2] = (char)(87 + b + (((b - 10) >> 31) & -39));
				b = bytes[i] & 0xF;
				c[(i * 2) + 1] = (char)(87 + b + (((b - 10) >> 31) & -39));
			}
			return new string(c);
		}

		/// <summary>
		///		Decode hexadecimal <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="hexString">
		///		Hexadecimal <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/>s from decoded <paramref name="hexString"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="hexString"/> is null, empty or only containing white spaces.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="hexString"/> length is odd.
		/// </exception>
		public static byte[] Decode(string hexString)
		{
			if (string.IsNullOrWhiteSpace(hexString))
			{
				throw new ArgumentNullException(nameof(hexString), "Hexadecimal string can't null, empty or containing white spaces.");
			}

			if ((hexString.Length & 1) != 0)
			{
				throw new ArgumentOutOfRangeException(nameof(hexString), "The hexadecimal string is invalid because it has an odd length.");
			}

			var result = new byte[hexString.Length / 2];

			int high, low;

			for (var i = 0; i < result.Length; i++)
			{
				high = hexString[i * 2];
				low = hexString[i * 2 + 1];
				high = (high & 0xf) + ((high & 0x40) >> 6) * 9;
				low = (low & 0xf) + ((low & 0x40) >> 6) * 9;
				result[i] = (byte)((high << 4) | low);
			}
			return result;
		}

		/// <summary>
		///		Decode hexadecimal <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="hexString">
		///		Hexadecimal <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/>s from decoded <paramref name="hexString"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="hexString"/> is null, empty or only containing white spaces.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="hexString"/> length is odd.
		/// </exception>
		public static byte[] Decode(char[] hexString)
		{
			if (hexString.Length == 0 || hexString == null)
			{
				throw new ArgumentNullException(nameof(hexString), "Hexadecimal string can't null, empty or containing white spaces.");
			}

			if ((hexString.Length & 1) != 0)
			{
				throw new ArgumentOutOfRangeException(nameof(hexString), "The hexadecimal string is invalid because it has an odd length.");
			}

			var result = new byte[hexString.Length / 2];

			int high, low;

			for (var i = 0; i < result.Length; i++)
			{
				high = hexString[i * 2];
				low = hexString[i * 2 + 1];
				high = (high & 0xf) + ((high & 0x40) >> 6) * 9;
				low = (low & 0xf) + ((low & 0x40) >> 6) * 9;
				result[i] = (byte)((high << 4) | low);
			}
			return result;
		}
	}
}