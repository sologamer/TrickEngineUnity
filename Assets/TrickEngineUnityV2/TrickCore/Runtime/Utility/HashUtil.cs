using System.Linq;
using System.Text;

namespace TrickCore
{
    public static class HashUtil
    {
        // http://www.cse.yorku.ca/~oz/hash.html
        
        /// <summary>
        /// Deterministic string to integer (DJB2 algorithm)
        /// </summary>
        /// <param name="str">The string to hash</param>
        /// <returns>The hash</returns>
        static uint DJB2_hash(string str)
        {
            uint hash = 5381;
            uint c;
            int index = 0;
            while ((c = str[index++]) != 0)
                hash = ((hash << 5) + hash) + c; /* hash * 33 + c */

            return hash;
        }


        //http://softwareengineering.stackexchange.com/questions/49550/which-hashing-algorithm-is-best-for-uniqueness-and-speed

        /*
        Hash Size    Prime                       Offset
        ===========  =========================== =================================
        32-bit       16777619                    2166136261
        64-bit       1099511628211               14695981039346656037
        128-bit      309485009821345068724781371 144066263297769815596495629667062367629
        256-bit
        prime: 2^168 + 2^8 + 0x63 = 374144419156711147060143317175368453031918731002211
        offset: 100029257958052580907070968620625704837092796014241193945225284501741471925557
        512-bit
        prime: 2^344 + 2^8 + 0x57 = 35835915874844867368919076489095108449946327955754392558399825615420669938882575126094039892345713852759
        offset: 9659303129496669498009435400716310466090418745672637896108374329434462657994582932197716438449813051892206539805784495328239340083876191928701583869517785
        1024-bit
        prime: 2^680 + 2^8 + 0x8d = 5016456510113118655434598811035278955030765345404790744303017523831112055108147451509157692220295382716162651878526895249385292291816524375083746691371804094271873160484737966720260389217684476157468082573
        offset: 1419779506494762106872207064140321832088062279544193396087847491461758272325229673230371772250864096521202355549365628174669108571814760471015076148029755969804077320157692458563003215304957150157403644460363550505412711285966361610267868082893823963790439336411086884584107735010676915
        */

        /// <summary>
        /// Deterministic string to integer (FNV1a algorithm)
        /// </summary>
        /// <param name="str">The string to hash</param>
        /// <returns>Returns the 32-bit hash</returns>
        public static uint GetHash32(string str)
        {
            return str.Aggregate(2166136261, (current, t) => (current ^ t) * 16777619);
        }
        
        /// <summary>
        /// Deterministic string to integer (FNV1a algorithm)
        /// </summary>
        /// <param name="str">The string to hash</param>
        /// <returns>Returns the 64-bit hash</returns>
        public static ulong GetHash64(string str)
        {
            return str.Aggregate(14695981039346656037, (current, t) => (current ^ t) * 16777619);
        }

        /// <summary>
        /// Transforms the input into a MD5 hash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Doing a ROT13 (caesar cipher)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ROT13(string input)
        {
            char Selector(char s)
            {
                return (char) (s >= 97 && s <= 122 ? s + 13 > 122 ? s - 13 : s + 13 : s >= 65 && s <= 90 ? s + 13 > 90 ? s - 13 : s + 13 : s);
            }
            return !string.IsNullOrEmpty(input) ? new string (input.ToCharArray().Select(Selector).ToArray() ) : input;
        }

        /// <summary>
        /// Hash the input with MD5 after doing a ROT13 (caesar cipher)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string MD5ROT13(string input)
        {
            return CreateMD5(ROT13(input));
        }
    }
}