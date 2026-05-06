using System;

namespace IndieGabo.HandyTools.HandyServiceLocatorModule
{
    /// <summary>
    /// Represents a stable identifier for named service registrations.
    /// </summary>
    public readonly struct ServiceIdentifier : IEquatable<ServiceIdentifier>
    {
        private readonly string _stringValue;
        private readonly Guid _guidValue;
        private readonly Kind _kind;
        private readonly int _hashCode;

        /// <summary>
        /// Gets a value indicating whether the identifier was created from a valid source.
        /// </summary>
        public bool IsValid => _kind != Kind.None;

        /// <summary>
        /// Creates an identifier backed by a string value.
        /// </summary>
        /// <param name="value">String identifier value.</param>
        public ServiceIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Service identifier string cannot be null, empty, or whitespace.",
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
        public ServiceIdentifier(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(
                    "Service identifier GUID cannot be empty.",
                    nameof(value)
                );
            }

            _stringValue = null;
            _guidValue = value;
            _kind = Kind.Guid;
            _hashCode = value.GetHashCode();
        }

        /// <summary>
        /// Creates an identifier backed by a string value.
        /// </summary>
        /// <param name="value">String identifier value.</param>
        /// <returns>Created identifier.</returns>
        public static ServiceIdentifier Create(string value)
        {
            return new ServiceIdentifier(value);
        }

        /// <summary>
        /// Creates an identifier backed by a GUID value.
        /// </summary>
        /// <param name="value">GUID identifier value.</param>
        /// <returns>Created identifier.</returns>
        public static ServiceIdentifier Create(Guid value)
        {
            return new ServiceIdentifier(value);
        }

        /// <summary>
        /// Returns the string representation used for diagnostics.
        /// </summary>
        public override string ToString()
        {
            return _kind == Kind.Guid
                ? _guidValue.ToString("N")
                : _stringValue ?? string.Empty;
        }

        /// <summary>
        /// Determines whether the current identifier matches another identifier.
        /// </summary>
        /// <param name="other">Identifier to compare.</param>
        /// <returns>True when both identifiers represent the same value.</returns>
        public bool Equals(ServiceIdentifier other)
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
        /// Determines whether the current identifier matches another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when the object is an equal identifier.</returns>
        public override bool Equals(object obj)
        {
            return obj is ServiceIdentifier other && Equals(other);
        }

        /// <summary>
        /// Returns the cached hash code for the identifier.
        /// </summary>
        /// <returns>Cached identifier hash code.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Converts a string into a service identifier.
        /// </summary>
        /// <param name="value">String identifier value.</param>
        public static implicit operator ServiceIdentifier(string value)
        {
            return new ServiceIdentifier(value);
        }

        /// <summary>
        /// Converts a GUID into a service identifier.
        /// </summary>
        /// <param name="value">GUID identifier value.</param>
        public static implicit operator ServiceIdentifier(Guid value)
        {
            return new ServiceIdentifier(value);
        }

        /// <summary>
        /// Compares two identifiers for equality.
        /// </summary>
        /// <param name="left">Left identifier.</param>
        /// <param name="right">Right identifier.</param>
        /// <returns>True when both identifiers are equal.</returns>
        public static bool operator ==(ServiceIdentifier left, ServiceIdentifier right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two identifiers for inequality.
        /// </summary>
        /// <param name="left">Left identifier.</param>
        /// <param name="right">Right identifier.</param>
        /// <returns>True when the identifiers are different.</returns>
        public static bool operator !=(ServiceIdentifier left, ServiceIdentifier right)
        {
            return !left.Equals(right);
        }

        private enum Kind : byte
        {
            None,
            String,
            Guid,
        }
    }
}