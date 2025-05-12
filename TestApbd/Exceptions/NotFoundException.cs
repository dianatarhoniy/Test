namespace TestApbd.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string? message = null, Exception? inner = null) : base(message, inner) { }
}

public class ConflictException : Exception
{
    public ConflictException(string? message = null, Exception? inner = null) : base(message, inner) { }
}