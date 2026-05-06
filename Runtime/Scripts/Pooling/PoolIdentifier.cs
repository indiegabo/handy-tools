using System;

namespace IndieGabo.HandyTools.PoolingModule
{
    /// <summary>
    /// Represents a stable identifier for named pool registrations.
    /// </summary>
    public readonly struct PoolIdentifier : IEquatable<PoolIdentifier>
    {
        private readonly string _stringValue;
        private readonly Guid _guidValue;
        private readonly Kind _kind;
        private readonly int _hashCode;

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the identifier was created from a
        /// valid source.
        /// </summary>
        public bool IsValid => _kind != Kind.None;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an identifier backed by a string value.
        /// </summary>
        /// <param name="value">String identifier value.</param>
        public PoolIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Pool identifier string cannot be null, empty, or whitespace.",
                    nameof(value)
                );
            }

            _stringValue = value;
            _guidValue = Guid.Empty;
            _kind = Kind.String;
            _hashCode = StringComparer.Ordinal.GetHashCode(value);
        }

        /// <summary>
        /// Creates an identifier backed by a GUID value.
        /// </summary>
        /// <param name="value">GUID identifier value.</param>
        public PoolIdentifier(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(
                    "Pool identifier GUID cannot be empty.",
                    nameof(value)
                );
            }

            _stringValue = null;
            _guidValue = value;
            _kind = Kind.Guid;
            _hashCode = value.GetHashCode();
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates an identifier backed by a string value.
        /// </summary>
        /// <param name="value">String identifier value.</param>
        /// <returns>Created identifier.</returns>
        public static PoolIdentifier Create(string value)
        {
            return new PoolIdentifier(value);
        }

        /// <summary>
        /// Creates an identifier backed by a GUID value.
        /// </summary>
        /// <param name="value">GUID identifier value.</param>
        /// <returns>Created identifier.</returns>
        public static PoolIdentifier Create(Guid value)
        {
            return new PoolIdentifier(value);
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Returns the string representation used for diagnostics.
        /// </summary>
        /// <returns>Diagnostic identifier string.</returns>
        public override string ToString()
        {
            return _kind == Kind.Guid
                ? _guidValue.ToString("N")
                : _stringValue ?? string.Empty;
        }

        /// <summary>
        /// Determines whether the current identifier matches another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when the object is an equal identifier.</returns>
        public override bool Equals(object obj)
        {
            return obj is PoolIdentifier other && Equals(other);
        }

        /// <summary>
        /// Returns the cached hash code for the identifier.
        /// </summary>
        /// <returns>Cached identifier hash code.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        #endregion

        #region Equality

        /// <summary>
        /// Determines whether the current identifier matches another
        /// identifier.
        /// </summary>
        /// <param name="other">Identifier to compare.</param>
        /// <returns>True when both identifiers represent the same value.</returns>
        public bool Equals(PoolIdentifier other)
        {
            if (_kind != other._kind)
            {
                return false;
            }

            return _kind switch
            {
                Kind.String => string.Equals(
                    _stringValue,
                    other._stringValue,
                    StringComparison.Ordinal
                ),
                Kind.Guid => _guidValue == other._guidValue,
                _ => false,
            };
        }

        /// <summary>
        /// Compares two identifiers for equality.
        /// </summary>
        /// <param name="left">Left identifier.</param>
        /// <param name="right">Right identifier.</param>
        /// <returns>True when both identifiers are equal.</returns>
        public static bool operator ==(PoolIdentifier left, PoolIdentifier right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two identifiers for inequality.
        /// </summary>
        /// <param name="left">Left identifier.</param>
        /// <param name="right">Right identifier.</param>
        /// <returns>True when the identifiers are different.</returns>
        public static bool operator !=(PoolIdentifier left, PoolIdentifier right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Conversions

        /// <summary>
        /// Converts a string into a pool identifier.
        /// </summary>
        /// <param name="value">String identifier value.</param>
        public static implicit operator PoolIdentifier(string value)
        {
            return new PoolIdentifier(value);
        }

        /// <summary>
        /// Converts a GUID into a pool identifier.
        /// </summary>
        /// <param name="value">GUID identifier value.</param>
        public static implicit operator PoolIdentifier(Guid value)
        {
            return new PoolIdentifier(value);
        }

        #endregion

        private enum Kind : byte
        {
            None,
            String,
            Guid,
        }
    }
}