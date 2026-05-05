using System;
using System.Text;

namespace IndieGabo.HandyTools.Utils.Crypto
{
    /// <summary>
    /// Encodes and decodes strings through a salted Base64 representation.
    /// </summary>
    public static class StringEncoder
    {
        /// <summary>
        /// Encodes one string into Base64 after appending the provided salt.
        /// </summary>
        /// <param name="subject">Plain-text input to encode.</param>
        /// <param name="salt">Salt appended before encoding.</param>
        /// <param name="encoding">Text encoding used for byte conversion.</param>
        /// <returns>The salted Base64 representation.</returns>
        public static string ToBase64(this string subject, string salt = "509d5d08-5e42-4830-b3eb-a5da03d7c266", Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            byte[] bytes = encoding.GetBytes($"{subject}_{salt}");
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decodes one salted Base64 string and strips the appended salt.
        /// </summary>
        /// <param name="encoded">Salted Base64 input string.</param>
        /// <param name="salt">Salt expected in the decoded payload.</param>
        /// <param name="encoding">Text encoding used for byte conversion.</param>
        /// <returns>The decoded plain-text value.</returns>
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