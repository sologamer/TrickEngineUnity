using System;

namespace Litdex.Utilities.Extension
{
	/// <summary>
	///		<see cref="Guid"/> extensions based on RFC 4122.
	/// </summary>
	/// <remarks>
	///		https://github.com/LogosBible/Logos.Utility/blob/master/src/Logos.Utility/GuidUtility.cs
	/// </remarks>
	public static class GuidExtensions
	{
		/// <summary>
		///		Converts a GUID to a lowercase string with no dashes.
		/// </summary>
		/// <param name="guid">
		///		The GUID.
		///	</param>
		/// <returns>
		///		The GUID as a lowercase string with no dashes.
		///	</returns>
		public static string ToLowerNoDashString(this Guid guid)
		{
			return guid.ToString("N");
		}

		/// <summary>
		///		Creates a name-based UUID version 5 using the algorithm from RFC 4122 §4.3.
		/// </summary>
		/// <param name="namespaceId">
		///		The ID of the namespace.
		///	</param>
		/// <param name="name">
		///		The name (within that namespace).
		///	</param>
		/// <returns>
		///		A UUID derived from the namespace and name.
		///	</returns>
		public static Guid CreateV5(this Guid namespaceId, string name)
		{
			return GuidUtility.Create(namespaceId, name, 5);
		}

		/// <summary>
		///		Creates a name-based UUID version 3 using the algorithm from RFC 4122 §4.3.
		/// </summary>
		/// <param name="namespaceId">
		///		The ID of the namespace.
		///	</param>
		/// <param name="name">
		///		The name (within that namespace).
		///	</param>
		/// <returns>
		///		A UUID derived from the namespace and name.
		///	</returns>
		public static Guid CreateV3(this Guid namespaceId, string name)
		{
			return GuidUtility.Create(namespaceId, name, 3);
		}
	}
}
