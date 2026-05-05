using System;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.Extensions
{
    /// <summary>
    /// Converts DateTime values into deterministic GUID-compatible payloads.
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly DateTime _epoch = new(1970, 1, 1, 0, 0, 0, 0);

        /// <summary>
        /// Converts the DateTime value into a GUID derived from milliseconds
        /// since the Unix epoch.
        /// </summary>
        /// <param name="dateTime">DateTime value to convert.</param>
        /// <returns>A GUID that encodes the provided timestamp.</returns>
        public static Guid ToGuid(this DateTime dateTime)
        {
            // Calculate the difference in milliseconds since the epoch
            double millisecondsSinceEpoch = (dateTime - _epoch).TotalMilliseconds;

            // Convert the milliseconds to a byte array
            byte[] millisecondsBytes = BitConverter.GetBytes(millisecondsSinceEpoch);

            // Pad the byte array with 8 bytes of zeros
            Array.Resize(ref millisecondsBytes, 16);

            // Create a new Guid using the padded byte array
            Guid guid = new(millisecondsBytes);

            return guid;
        }
    }
}