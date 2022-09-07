using Litdex.Utilities.BinaryEncoding;

namespace Litdex.Utilities.Extension
{
	/// <summary>
	///		Encoding and decoding binary dato to text and vice versa.
	/// </summary>
	public static class BinaryEncodingExtensions
	{
		#region Encoding

		#region From array of bytes to string

		/// <summary>
		///		Encode the array of <see cref="byte"/>s to Base16 <see cref="string"/>.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/> to encode.
		/// </param>
		/// <param name="upperCase">
		///		Base16 <see cref="string"/> letter case. Default is upper case.
		/// </param>
		/// <returns>
		///		Base16 <see cref="string"/>.
		/// </returns>
		public static string ToBase16(this byte[] bytes, bool upperCase = true)
		{
			return Base16.Encode(bytes, upperCase);
		}

		/// <summary>
		///		Encode the array of <see cref="byte"/>s to Base64 <see cref="string"/>.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/> to encode.
		/// </param>
		/// <returns>
		///		Base64 <see cref="string"/>.
		/// </returns>
		public static string ToBase64(this byte[] bytes)
		{
			return Base64.Encode(bytes);
		}

		/// <summary>
		///		Encode the array of <see cref="byte"/>s to Base85 <see cref="string"/>.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/> to encode.
		/// </param>
		/// <returns>
		///		Base85 <see cref="string"/>.
		/// </returns>
		public static string ToBase85(this byte[] bytes)
		{
			return Base85.Encode(bytes);
		}

		/// <summary>
		///		Encode the array of <see cref="byte"/>s to Base91 <see cref="string"/>.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/> to encode.
		/// </param>
		/// <returns>
		///		Base91 <see cref="string"/>.
		/// </returns>
		public static string ToBase91(this byte[] bytes)
		{
			return Base91.Encode(bytes);
		}

		#endregion From array of bytes to string

		#region From string to string

		/// <summary>
		///		Encode the <see cref="string"/> to Base16 <see cref="string"/>.
		/// </summary>
		/// <param name="text">
		///		<see cref="string"/> to encode.
		/// </param>
		/// <param name="upperCase">
		///		Base16 <see cref="string"/> letter case. Default is upper case.
		/// </param>
		/// <returns>
		///		Base16 <see cref="string"/>.
		/// </returns>
		public static string ToBase16(this string text, bool upperCase = true)
		{
			return Base16.Encode(text.GetBytes(), upperCase);
		}

		/// <summary>
		///		Encode the <see cref="string"/> to Base64 <see cref="string"/>.
		/// </summary>
		/// <param name="text">
		///		<see cref="string"/> to encode.
		/// </param>
		/// <returns>
		///		Base64 <see cref="string"/>.
		/// </returns>
		public static string ToBase64(this string text)
		{
			return Base64.Encode(text.GetBytes());
		}

		/// <summary>
		///		Encode the <see cref="string"/> to Base85 <see cref="string"/>.
		/// </summary>
		/// <param name="text">
		///		<see cref="string"/> to encode.
		/// </param>
		/// <returns>
		///		Base85 <see cref="string"/>.
		/// </returns>
		public static string ToBase85(this string text)
		{
			return Base85.Encode(text.GetBytes());
		}

		/// <summary>
		///		Encode the <see cref="string"/> to Base91 <see cref="string"/>.
		/// </summary>
		/// <param name="text">
		///		<see cref="string"/> to encode.
		/// </param>
		/// <returns>
		///		Base91 <see cref="string"/>.
		/// </returns>
		public static string ToBase91(this string text)
		{
			return Base91.Encode(text.GetBytes());
		}

		#endregion From string to string

		#endregion Encoding

		#region Decoding

		#region From string to array of bytes

		/// <summary>
		///		Decode Base16 <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="str">
		///		Base16 <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/> that decoded from Base16 <see cref="string"/>.
		///	</returns>
		public static byte[] DecodeBase16(this char[] str)
		{
			return Base16.Decode(str);
		}

		/// <summary>
		///		Decode Base16 <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="str">
		///		Base16 <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/> that decoded from Base16 <see cref="string"/>.
		///	</returns>
		public static byte[] DecodeBase16(this string str)
		{
			return Base16.Decode(str);
		}

		/// <summary>
		///		Decode Base64 <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="str">
		///		Base64 <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/> that decoded from Base64 <see cref="string"/>.
		///	</returns>
		public static byte[] DecodeBase64(this string str)
		{
			return Base64.Decode(str);
		}

		/// <summary>
		///		Decode Base85 <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="str">
		///		Base85 <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/> that decoded from Base85 <see cref="string"/>.
		///	</returns>
		public static byte[] DecodeBase85(this string str)
		{
			return Base85.Decode(str);
		}

		/// <summary>
		///		Decode Base91 <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="str">
		///		Base91 <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/> that decoded from Base91 <see cref="string"/>.
		///	</returns>
		public static byte[] DecodeBase91(this string str)
		{
			return Base91.Decode(str);
		}

		#endregion From string to array of bytes

		#region From string to string

		#endregion From string to string

		#endregion Decoding
	}
}
