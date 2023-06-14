namespace Data.Base.Exceptions
{
    public class LengthValidationException : Exception
    {
        public LengthValidationException(string message)
            : base(message)
        {
        }

        public LengthValidationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public LengthValidationException(int maxLength) : this($"String must be {maxLength} or less characters long.")
        {
        }
    }
}
