using LibSquirl.Protocol.Models;

namespace LibSquirl.Protocol;

public class LibSqlException : Exception
{
    public LibSqlException(string message)
        : base(message) { }

    public LibSqlException(string message, Exception innerException)
        : base(message, innerException) { }

    public LibSqlException(LibSqlError error)
        : base(error.Message)
    {
        LibSqlError = error;
    }

    public LibSqlError? LibSqlError { get; }
}
