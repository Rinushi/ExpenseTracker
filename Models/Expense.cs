using System.Text.Json.Serialization;

namespace ExpenseTracker.Models;

/// <summary>
/// One recorded expense.
/// Declared as a positional 'record': the compiler generates the constructor, init-only
/// (read-only) properties, value-based equality, ToString() and support for 'with' copies.
/// </summary>
/// <param name="Id">Unique identifier assigned by the ExpenseManager.</param>
/// <param name="Amount">Amount spent (decimal avoids binary floating-point rounding errors).</param>
/// <param name="Date">Calendar date of the expense (DateOnly has no time-of-day component).</param>
/// <param name="Category">Spending category.</param>
/// <param name="Note">Optional free-text note; 'string?' means null is allowed.</param>
public sealed record Expense(
    int Id,
    decimal Amount,
    DateOnly Date,
    Category Category,
    string? Note = null)
{
    /// <summary>
    /// Classifies the expense by size using a switch expression with property patterns.
    /// [JsonIgnore] stops this calculated value being written to the JSON file.
    /// </summary>
    [JsonIgnore]
    public string SizeBand => this switch
    {
        { Amount: >= 500m } => "Large",
        { Amount: >= 50m }  => "Medium",
        _                   => "Small"
    };
}
