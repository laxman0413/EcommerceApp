namespace EcommerceApp.Application.Common.Exceptions;

// Base for exceptions the API's global exception middleware knows how to translate into
// a specific HTTP status code, instead of returning a generic 500 for every failure.
public abstract class AppException(string message) : Exception(message);

public class NotFoundAppException(string message) : AppException(message);

public class ConflictAppException(string message) : AppException(message);

public class UnauthorizedAppException(string message) : AppException(message);

// The processor reached a decision and said no — a business outcome, not a bug. Maps to 402.
public class PaymentDeclinedAppException(string message) : AppException(message);

// The processor itself couldn't be reached/timed out — transient/infrastructure, not the
// caller's fault. Maps to 502 so clients know a retry may succeed.
public class PaymentGatewayAppException(string message) : AppException(message);
