namespace Litdex.Utilities.Extension
{
	/// <summary>
	///		String Extension
	/// </summary>
	public static class StringExtension
	{
		/// <summary>
		///		Convert <see cref="string"/> to an array of <see cref="byte"/>.
		/// </summary>
		/// <param name="str">
		///		A <see cref="string"/> to convert.
		/// </param>
		/// <returns>
		///		An array of <see cref="byte"/>s from <see cref="string"/>.
		///	</returns>
		public static byte[] GetBytes(this string str)
		{
			return System.Text.Encoding.UTF8.GetBytes(str);
		}

		/// <summary>
		///		Convert <see cref="string"/> to an array of <see cref="byte"/>.
		/// </summary>
		/// <param name="str">
		///		A <see cref="string"/> to convert.
		/// </param>
		/// <param name="encoding">
		///		encodes all the characters in the specified string into a sequence of bytes.
		/// </param>
		/// <returns>
		///		An array of <see cref="byte"/>s from <see cref="string"/>.
		///	</returns>
		public static byte[] GetBytes(this string str, System.Text.Encoding encoding)
		{
			return encoding.GetBytes(str);
		}

		/// <summary>
		///		Convert array of <see cref="char"/> to an array of <see cref="byte"/>.
		/// </summary>
		/// <param name="str">
		///		A array of <see cref="char"/> to convert.
		/// </param>
		/// <returns>
		///		An array of <see cref="byte"/>s from array of <see cref="char"/>.
		///	</returns>
		public static byte[] GetBytes(this char[] str)
		{
			return System.Text.Encoding.UTF8.GetBytes(str);
		}
		/// <summary>
		///		Convert <see cref="string"/> to an array of <see cref="byte"/>.
		/// </summary>
		/// <param name="str">
		///		A <see cref="string"/> to convert.
		/// </param>
		/// <param name="encoding">
		///		encodes all the characters in the specified string into a sequence of bytes.
		/// </param>
		/// <returns>
		///		An array of <see cref="byte"/>s from <see cref="string"/>.
		///	</returns>
		public static byte[] GetBytes(this char[] str, System.Text.Encoding encoding)
		{
			return encoding.GetBytes(str);
		}
	}
}
