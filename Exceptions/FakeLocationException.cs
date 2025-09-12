namespace FlagsRally.Exceptions;

public class FakeLocationException : Exception
{
    public FakeLocationException() { }
    public FakeLocationException(string message) : base(message) { }
    public FakeLocationException(string message, Exception inner) : base(message, inner) { }
}
