namespace ExpenseTracker.Models;

/// <summary>
/// Custom (user-defined) exception raised when an expense fails validation.
/// Deriving from System.Exception lets callers catch it by type.
/// </summary>
public sealed class InvalidExpenseException : Exception
{
    /// <summary>The name of the field that failed validation (e.g. "Amount").</summary>
    public string Field { get; }

    public InvalidExpenseException(string field, string message) : base(message)
    {
        Field = field;
    }
}
