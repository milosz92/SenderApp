namespace Messages;

// Exception that should trigger retry policy
public class RetryableException : Exception
{
    public RetryableException(string message) : base(message) { }
}

// Exception that should trigger circuit breaker
public class CircuitBreakerException : Exception
{
    public CircuitBreakerException(string message) : base(message) { }
}

// Exception that will just throw without any resilience policy
public class FatalException : Exception
{
    public FatalException(string message) : base(message) { }
}
