using System;

namespace GitLabTimeManager.Types
{
    public class IncorrectProfileException : Exception
    {
        public IncorrectProfileException()
        {
        }
        
        public IncorrectProfileException(Exception exception) : base(exception.Message, exception)
        {
        }
    }

    public class UnableConnectionException : Exception
    {
        public UnableConnectionException(Exception exception) : base(exception.Message, exception)
        {
        }
    }
    
    public class AuthorizationException : Exception
    {
        public AuthorizationException(Exception exception) : base(exception.Message, exception)
        {
        }
    }
}