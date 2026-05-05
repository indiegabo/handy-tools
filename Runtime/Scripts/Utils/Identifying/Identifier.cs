using System;
using System.IO;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.Identifying
{
    [Serializable]
    /// <summary>
    /// Stores a GUID-compatible identifier as four serialized unsigned integers.
    /// </summary>
    public struct Identifier : IEquatable<Identifier>
    {

#if UNITY_EDITOR
        public const string VALUE0_FIELDNAME = nameof(m_Value0);
        public const string VALUE1_FIELDNAME = nameof(m_Value1);
        public const string VALUE2_FIELDNAME = nameof(m_Value2);
        public const string VALUE3_FIELDNAME = nameof(m_Value3);
#endif

        [SerializeField, HideInInspector] uint m_Value0;
        [SerializeField, HideInInspector] uint m_Value1;
        [SerializeField, HideInInspector] uint m_Value2;
        [SerializeField, HideInInspector] uint m_Value3;

        /// <summary>
        /// Gets the first serialized identifier segment.
        /// </summary>
        public uint Value0 => m_Value0;

        /// <summary>
        /// Gets the second serialized identifier segment.
        /// </summary>
        public uint Value1 => m_Value1;

        /// <summary>
        /// Gets the third serialized identifier segment.
        /// </summary>
        public uint Value2 => m_Value2;

        /// <summary>
        /// Gets the fourth serialized identifier segment.
        /// </summary>
        public uint Value3 => m_Value3;

        /// <summary>
        /// Initializes the identifier from four raw unsigned integer segments.
        /// </summary>
        /// <param name="val0">First segment.</param>
        /// <param name="val1">Second segment.</param>
        /// <param name="val2">Third segment.</param>
        /// <param name="val3">Fourth segment.</param>
        public void Initialize(uint val0, uint val1, uint val2, uint val3)
        {
            m_Value0 = val0;
            m_Value1 = val1;
            m_Value2 = val2;
            m_Value3 = val3;
        }

        /// <summary>
        /// Initializes the identifier from one Guid value.
        /// </summary>
        /// <param name="guid">Guid value to copy.</param>
        public void Initialize(Guid guid)
        {
            m_Value0 = 0U;
            m_Value1 = 0U;
            m_Value2 = 0U;
            m_Value3 = 0U;
            TryParse(guid, out this);
        }

        /// <summary>
        /// Initializes the identifier from one hexadecimal GUID string.
        /// </summary>
        /// <param name="hexString">Hexadecimal GUID string.</param>
        public void Initialize(string hexString)
        {
            m_Value0 = 0U;
            m_Value1 = 0U;
            m_Value2 = 0U;
            m_Value3 = 0U;
            TryParse(hexString, out this);
        }

        /// <summary>
        /// Converts the identifier to a compact hexadecimal string.
        /// </summary>
        /// <returns>The hexadecimal identifier representation.</returns>
        public string ToHexString()
        {
            string hex = $"{m_Value0:X8}{m_Value1:X8}{m_Value2:X8}{m_Value3:X8}";
            return hex;
        }

        /// <summary>
        /// Converts the identifier to a Guid value.
        /// </summary>
        /// <returns>The equivalent Guid value.</returns>
        public Guid ToGuid()
        {
            string hex = ToHexString();
            return new Guid(hex);
        }

        /// <summary>
        /// Converts the identifier to a canonical GUID string.
        /// </summary>
        /// <returns>The GUID string representation.</returns>
        public string ToGuidString()
        {
            return ToGuid().ToString();
        }

        static void TryParse(string hexString, out Identifier identifier)
        {
            identifier.m_Value0 = Convert.ToUInt32(hexString.Substring(0, 8), 16);
            identifier.m_Value1 = Convert.ToUInt32(hexString.Substring(8, 8), 16);
            identifier.m_Value2 = Convert.ToUInt32(hexString.Substring(16, 8), 16);
            identifier.m_Value3 = Convert.ToUInt32(hexString.Substring(24, 8), 16);
        }

        static void TryParse(Guid guid, out Identifier identifier)
        {
            string guidString = guid.ToString("N");
            identifier.m_Value0 = Convert.ToUInt32(guidString.Substring(0, 8), 16);
            identifier.m_Value1 = Convert.ToUInt32(guidString.Substring(8, 8), 16);
            identifier.m_Value2 = Convert.ToUInt32(guidString.Substring(16, 8), 16);
            identifier.m_Value3 = Convert.ToUInt32(guidString.Substring(24, 8), 16);
        }

        /// <summary>
        /// Checks whether the identifier matches the provided Guid.
        /// </summary>
        /// <param name="guid">Guid to compare.</param>
        /// <returns>True when both identifiers are equal.</returns>
        public bool CompareToGuid(Guid guid)
        {
            return guid.Equals(ToGuid());
        }

        public static bool operator ==(Identifier x, Identifier y) => x.m_Value0 == y.m_Value0 && x.m_Value1 == y.m_Value1 && x.m_Value2 == y.m_Value2 && x.m_Value3 == y.m_Value3;
        public static bool operator !=(Identifier x, Identifier y) => !(x == y);

        /// <summary>
        /// Compares this identifier with another identifier.
        /// </summary>
        /// <param name="other">Identifier to compare.</param>
        /// <returns>True when both identifiers are equal.</returns>
        public bool Equals(Identifier other) => this == other;

        /// <summary>
        /// Compares this identifier with another object instance.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when the object is an equal identifier.</returns>
        public override bool Equals(object obj) => obj != null && obj is Identifier && Equals((Identifier)obj);

        /// <summary>
        /// Returns a hash code derived from the serialized identifier segments.
        /// </summary>
        /// <returns>The identifier hash code.</returns>
        public override int GetHashCode() => (((int)m_Value0 * 397 ^ (int)m_Value1) * 397 ^ (int)m_Value2) * 397 ^ (int)m_Value3;

        /// <summary>
        /// Returns the canonical GUID string representation.
        /// </summary>
        /// <returns>The GUID string representation.</returns>
        public override string ToString() => this.ToGuidString();
    }

    #region BinaryReader and BinaryWriter Extensions
    /// <summary>
    /// Adds identifier deserialization helpers to BinaryReader.
    /// </summary>
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads one identifier from the binary stream.
        /// </summary>
        /// <param name="reader">Reader that provides the identifier segments.</param>
        /// <returns>The deserialized identifier.</returns>
        public static Identifier ReadIdentifier(this BinaryReader reader)
        {
            Identifier identifier = new();
            identifier.Initialize(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
            return identifier;
        }
    }

    /// <summary>
    /// Adds identifier serialization helpers to BinaryWriter.
    /// </summary>
    public static class BinaryWriterExtensions
    {
        /// <summary>
        /// Writes one identifier to the binary stream.
        /// </summary>
        /// <param name="writer">Writer that receives the identifier segments.</param>
        /// <param name="identifier">Identifier to serialize.</param>
        public static void Write(this BinaryWriter writer, Identifier identifier)
        {
            writer.Write(identifier.Value0);
            writer.Write(identifier.Value1);
            writer.Write(identifier.Value2);
            writer.Write(identifier.Value3);
        }
    }
    #endregion
}