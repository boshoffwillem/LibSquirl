namespace LibSquirl.Platform;

public class TursoPlatformException : Exception
{
    public int StatusCode { get; }

    public TursoPlatformException(string message) : base(message) { }

    public TursoPlatformException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public TursoPlatformException(string message, Exception innerException) : base(message, innerException) { }
}
