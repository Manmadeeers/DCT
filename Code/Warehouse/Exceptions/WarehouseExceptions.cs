namespace Warehouse.Exceptions
{
    public sealed class UnavailableServerException : Exception
    {
        public UnavailableServerException(string message) : base(message) { }
        public UnavailableServerException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class IncorrectOperationException : Exception
    {
        public IncorrectOperationException(string message) : base(message) { }
    }

    public sealed class ObjectMissingException : Exception
    {
        public ObjectMissingException(string message) : base(message) { }
    }

    public sealed class IncorrectInputDataException : Exception
    {
        public IncorrectInputDataException(string message) : base(message) { }
        public IncorrectInputDataException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class NetworkException : Exception
    {
        public NetworkException(string message) : base(message) { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }
}