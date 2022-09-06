using System;
using System.Collections.Generic;
using System.Text;

namespace Litdex.Utilities.BinaryEncoding
{
	/// <summary>
	///		Encode and decode in base91.
	/// </summary>
	public static class Base91
	{
		private static readonly char[] EncodeTable = new char[]
		{
			'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
			'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
			'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
			'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '!', '#', '$',
			'%', '&', '(', ')', '*', '+', ',', '.', '/', ':', ';', '<', '=',
			'>', '?', '@', '[', ']', '^', '_', '`', '{', '|', '}', '~', '"'
		};

		private static Dictionary<byte, int> DecodeTable;

		static Base91()
		{
			InitDecodeTable();
		}

		private static void InitDecodeTable()
		{
			DecodeTable = new Dictionary<byte, int>();
			//for (int i = 0; i < 255; i++)
			//{
			//    DecodeTable[(byte)i] = -1;
			//}
			for (int i = 0; i < EncodeTable.Length; i++)
			{
				DecodeTable[(byte)EncodeTable[i]] = i;
			}
		}

		/// <summary>
		///		Convert array of <see cref="byte"/>s to Base91 string.
		/// </summary>
		/// <param name="bytes">
		///		Array of <see cref="byte"/>s to encode.
		///	</param>
		/// <returns>
		///		Base91 <see cref="string"/>.
		///	</returns>
		///	<exception cref="ArgumentNullException">
		///		<paramref name="bytes"/> can't null or empty.
		/// </exception>
		public static string Encode(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
			{
				throw new ArgumentNullException(nameof(bytes), "Array can't null or empty.");
			}

			var sb = new StringBuilder();
			var b = 0;
			var n = 0;
			int v;
			for (var i = 0; i < bytes.Length; i++)
			{
				b |= (bytes[i] & 255) << n;
				n += 8;
				if (n > 13)
				{
					v = b & 8191;
					if (v > 88)
					{
						b >>= 13;
						n -= 13;
					}
					else
					{
						v = b & 16383;
						b >>= 14;
						n -= 14;
					}
					int quotient = Math.DivRem(v, 91, out int remainder);
					sb.Append(EncodeTable[remainder]);
					sb.Append(EncodeTable[quotient]);
				}
			}

			if (n != 0)
			{
				int quotient = Math.DivRem(b, 91, out int remainder);
				sb.Append(EncodeTable[remainder]);
				if (n > 7 || b > 90)
				{
					sb.Append(EncodeTable[quotient]);
				}
			}
			return sb.ToString();
		}

		/// <summary>
		///		Convert Base91 <see cref="string"/> to array of <see cref="byte"/>s.
		/// </summary>
		/// <param name="base91String">
		///		Base91 <see cref="string"/> to decode.
		///	</param>
		/// <returns>
		///		Array of <see cref="byte"/>s from <paramref name="base91String"/>.
		///	</returns>
		///	<exception cref="ArgumentNullException">
		///		<paramref name="base91String"/> can't null or empty.
		///	</exception>
		public static byte[] Decode(string base91String)
		{
			if (base91String == null || base91String.Length == 0)
			{
				throw new ArgumentNullException(nameof(base91String), "String can't null or empty.");
			}

			var output = new List<byte>();
			int c;
			var v = -1;
			var b = 0;
			var n = 0;
			for (var i = 0; i < base91String.Length; i++)
			{
				c = DecodeTable[(byte)base91String[i]];
				if (c == -1)
				{
					continue;
				}
				if (v < 0)
				{
					v = c;
				}
				else
				{
					v += c * 91;
					b |= v << n;
					n += (v & 8191) > 88 ? 13 : 14;
					do
					{
						output.Add((byte)(b & 255));
						b >>= 8;
						n -= 8;
					} while (n > 7);
					v = -1;
				}
			}
			if (v + 1 != 0)
			{
				output.Add((byte)((b | v << n) & 255));
			}
			return output.ToArray();
		}
	}
}