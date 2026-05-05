using System.Runtime.CompilerServices;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.Extensions
{
    /// <summary>
    /// Converts quaternion values between Unity and System.Numerics types.
    /// </summary>
    public static class QuaternionConversionExtensions
    {
        /// <summary>
        /// Converts one System.Numerics quaternion into a Unity quaternion.
        /// </summary>
        /// <param name="quaternion">Quaternion value to convert.</param>
        /// <returns>The equivalent Unity quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToUnityQuaternion(this System.Numerics.Quaternion quaternion)
        {
            return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        /// <summary>
        /// Converts one Unity quaternion into a System.Numerics quaternion.
        /// </summary>
        /// <param name="quaternion">Quaternion value to convert.</param>
        /// <returns>The equivalent System.Numerics quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Quaternion ToSystemQuaternion(this Quaternion quaternion)
        {
            return new System.Numerics.Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }
    }
}