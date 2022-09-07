namespace Litdex.Utilities.Extension
{
	/// <summary>
	///		Byte[] Extension.
	/// </summary>
	public static class ByteArrayExtensions
	{
		/// <summary>
		///		Convert array of <see cref="byte"/>s to UTF-8 <see cref="string"/>.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/>s to convert.
		///	</param>
		/// <returns>
		///		UTF-8 <see cref="string"/>.
		///	</returns>
		public static string GetString(this byte[] bytes)
		{
			return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		}
	}
}
