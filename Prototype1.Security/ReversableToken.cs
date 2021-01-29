using System;
using System.Text;

namespace Prototype1.Security
{
    public class ReversableToken
    {
        public static string Tokenize(string args)
        {
            var ticks = DateTime.UtcNow.Ticks.ToString();
            return Base64StringEncode(ticks + "|" + Aes256Encryption.Encrypt(args, CreateKey(ticks)));
        }

        public static string DeTokenize(string token, int maxDays = 14)
        {
            var decoded = Base64StringDecode(token).Split('|');

            long ticks;
            if (!long.TryParse(decoded[0], out ticks))
                return null;

            if (Math.Abs((DateTime.UtcNow - new DateTime(ticks)).TotalDays) > maxDays)
                throw new TokenExpiredException();

            return Aes256Encryption.DecryptString(decoded[1], CreateKey(decoded[0]));
        }

        private static byte[] CreateKey(string t)
        {
            var seed = int.Parse(MaxLength(t, 9));
            return Aes256Encryption.CreateKey(seed);
        }

        private static string Base64StringEncode(string value)
        {
            var encbuff = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(encbuff);
        }

        private static string Base64StringDecode(string value)
        {
            byte[] decbuff = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(decbuff);
        }

        private static string MaxLength(string str, int maxLen)
        {
            return string.IsNullOrEmpty(str) ? str : str.Substring(0, Math.Min(maxLen, str.Length));
        }

        public class TokenExpiredException : Exception
        {
        }
    }
}