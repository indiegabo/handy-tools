using System.Runtime.CompilerServices;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.Extensions
{
    /// <summary>
    /// Converts vector values between Unity and System.Numerics types.
    /// </summary>
    public static class VectorConversionExtensions
    {
        /// <summary>
        /// Converts one System.Numerics.Vector2 into a Unity Vector2.
        /// </summary>
        /// <param name="vector">Vector value to convert.</param>
        /// <returns>The equivalent Unity vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToUnityVector(this System.Numerics.Vector2 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        /// <summary>
        /// Converts one Unity Vector2 into a System.Numerics.Vector2.
        /// </summary>
        /// <param name="vector">Vector value to convert.</param>
        /// <returns>The equivalent System.Numerics vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 ToSystemVector(this Vector2 vector)
        {
            return new System.Numerics.Vector2(vector.x, vector.y);
        }

        /// <summary>
        /// Converts one System.Numerics.Vector3 into a Unity Vector3.
        /// </summary>
        /// <param name="vector">Vector value to convert.</param>
        /// <returns>The equivalent Unity vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToUnityVector(this System.Numerics.Vector3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        /// <summary>
        /// Converts one Unity Vector3 into a System.Numerics.Vector3.
        /// </summary>
        /// <param name="vector">Vector value to convert.</param>
        /// <returns>The equivalent System.Numerics vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 ToSystemVector(this Vector3 vector)
        {
            return new System.Numerics.Vector3(vector.x, vector.y, vector.z);
        }
    }
}