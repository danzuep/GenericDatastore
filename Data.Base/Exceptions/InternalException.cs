using System.Runtime.Serialization;

namespace Data.Base.Exceptions
{
    /// <summary>
    /// This exception class is thrown by the application if it encounters an unrecoverable error.
    /// </summary>
    [Serializable]
    public class InternalException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <overloads>There are four overloads for the constructor</overloads>
        public InternalException()
        {
        }

        /// <inheritdoc />
        public InternalException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public InternalException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc />
        protected InternalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc cref="Exception(string, Exception)"/>
        public InternalException(Exception innerException) : base(innerException.Message, innerException)
        {
        }
    }
}