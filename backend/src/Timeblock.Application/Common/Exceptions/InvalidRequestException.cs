namespace Timeblock.Application.Common.Exceptions;

public sealed class InvalidRequestException(string message, Exception? innerException = null)
    : Exception(message, innerException);
