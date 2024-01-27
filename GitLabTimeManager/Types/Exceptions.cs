using System;

namespace GitLabTimeManager.Types;

public class IncorrectProfileException : Exception
{
    public IncorrectProfileException()
    {
    }
}

public class UnableConnectionException(Exception exception) : Exception(exception.Message, exception);
    
public class AuthorizationException(Exception exception) : Exception(exception.Message, exception);