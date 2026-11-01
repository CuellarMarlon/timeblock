namespace Timeblock.Application.Common.Exceptions;

public sealed class ResourceConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
