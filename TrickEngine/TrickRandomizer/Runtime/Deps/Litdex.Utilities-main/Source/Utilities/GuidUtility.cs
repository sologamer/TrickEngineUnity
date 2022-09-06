using System;
using System.Security.Cryptography;

using Litdex.Utilities.Extension;

namespace Litdex.Utilities
{
	/// <summary>
	///		Helper methods for working with <see cref="Guid"/>.
	/// </summary>
	public static class GuidUtility
	{
		/// <summary>
		///		The namespace for fully-qualified domain names (from RFC 4122, Appendix C).
		/// </summary>
		public static readonly Guid DnsNamespace = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

		/// <summary>
		///		The namespace for URLs (from RFC 4122, Appendix C).
		/// </summary>
		public static readonly Guid UrlNamespace = new Guid("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

		/// <summary>
		///		The namespace for ISO OIDs (from RFC 4122, Appendix C).
		/// </summary>
		public static readonly Guid IsoOidNamespace = new Guid("6ba7b812-9dad-11d1-80b4-00c04fd430c8");

		/// <summary>
		///		Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
		/// </summary>
		/// <param name="namespaceId">
		///		The ID of the namespace.
		///	</param>
		/// <param name="name">
		///		The name (within that namespace).
		///	</param>
		///	<param name="version">
		///		The version number of the UUID to create; this value must be either
		///		3 (for MD5 hashing) or 5 (for SHA-1 hashing).
		///	</param>
		/// <returns>
		///		A UUID derived from the <paramref name="namespaceId"/> and <paramref name="name"/>.
		///	</returns>
		///	<exception cref="ArgumentNullException">
		///		Name can't null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		Version must be either 3 or 5.
		/// </exception>
		public static Guid Create(Guid namespaceId, string name, int version)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name), "Name can't null.");
			}

			if (version != 3 && version != 5)
			{
				throw new ArgumentOutOfRangeException(nameof(version), "Version must be either 3 or 5.");
			}

			// convert the namespace UUID to network order (step 3)
			var namespaceBytes = namespaceId.ToByteArray();
			SwapByteOrder(namespaceBytes);

			// compute the hash of the namespace ID concatenated with the name (step 4)
			byte[] guid;
			using (HashAlgorithm algorithm = version == 3 ? (HashAlgorithm)MD5.Create() : SHA1.Create())
			{
				algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
				algorithm.TransformFinalBlock(name.GetBytes(), 0, name.Length);

				if (version == 5)
				{
					guid = algorithm.Hash.Slice(0, 16);
				}
				else
				{
					guid = algorithm.Hash;
				}
			}

			// set the four most significant bits (bits 12 through 15) of the time_hi_and_version field to the appropriate 4-bit version number from Section 4.1.3 (step 8)
			guid[6] = (byte)((guid[6] & 0x0F) | (version << 4));

			// set the two most significant bits (bits 6 and 7) of the clock_seq_hi_and_reserved to zero and one, respectively (step 10)
			guid[8] = (byte)((guid[8] & 0x3F) | 0x80);

			// convert the resulting UUID to local byte order (step 13)
			SwapByteOrder(guid);

			return new Guid(guid);
		}

		/// <summary>
		///		Converts a GUID (expressed as a byte array) to/from network order (MSB-first).
		/// </summary>
		/// <param name="guid">
		///		
		/// </param>
		internal static void SwapByteOrder(byte[] guid)
		{
			SwapBytes(guid, 0, 3);
			SwapBytes(guid, 1, 2);
			SwapBytes(guid, 4, 5);
			SwapBytes(guid, 6, 7);
		}

		private static void SwapBytes(byte[] guid, int left, int right)
		{
			byte temp = guid[left];
			guid[left] = guid[right];
			guid[right] = temp;
		}
	}
}