using System;
using System.Text;

namespace IndieGabo.HandyTools.Utils.Crypto
{
    public static class StringEncoder
    {
        public static string ToBase64(this string subject, string salt = "509d5d08-5e42-4830-b3eb-a5da03d7c266", Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            byte[] bytes = encoding.GetBytes($"{subject}_{salt}");
            return Convert.ToBase64String(bytes);
        }

        public static string FromBase64(this string encoded, string salt = "509d5d08-5e42-4830-b3eb-a5da03d7c266", Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            byte[] bytes = Convert.FromBase64String(encoded);
            string decoded = encoding.GetString(bytes);

            int stripLength = salt.Length + 1;

            return decoded.Substring(0, decoded.Length - stripLength);
        }
    }
}