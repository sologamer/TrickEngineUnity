using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Litdex.Utilities.Extension
{
	/// <summary>
	///		Object Extension.
	/// </summary>
	public static class ObjectExtensions
	{
		/// <summary>
		///		Get object address in RAM.
		/// </summary>
		/// <param name="obj">
		///		
		/// </param>
		/// <returns>
		///		64-bit signed integer RAM address.
		/// </returns>
		public static long GetObjectAddress(this object obj)
		{
			var handle = System.Runtime.InteropServices.GCHandle.Alloc(obj, System.Runtime.InteropServices.GCHandleType.WeakTrackResurrection);
			var address = System.Runtime.InteropServices.GCHandle.ToIntPtr(handle).ToInt64();
			handle.Free();
			return address;
		}

		/// <summary>
		///		Perform a deep Copy of the object.
		/// </summary>
		/// <typeparam name="T">
		///		The type of object being copied.
		///	</typeparam>
		/// <param name="source">
		///		The object instance to copy.
		///	</param>
		/// <returns>
		///		The copied object.
		///	</returns>
		public static T Clone<T>(this T source)
		{
			if (!typeof(T).IsSerializable)
			{
				throw new ArgumentException("The type must be serializable.", nameof(source));
			}

			// Don't serialize a null object, simply return the default for that object
			if (source == null)
			{
				return default(T);
			}

			using (var stream = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(stream);
			}
		}

		/// <summary>
		///		Check is the object is serializable.
		/// </summary>
		/// <param name="obj">
		///		Object to check.
		///	</param>
		/// <returns>
		///		
		/// </returns>
		public static bool IsSerializable(this object obj)
		{
			if (obj == null)
			{
				return false;
			}
			var type = obj.GetType();
			return type.IsSerializable;
		}
	}
}
