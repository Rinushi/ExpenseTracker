namespace ExpenseTracker.Models;

/// <summary>
/// The spending categories supported by the application.
/// An enum is a value type that restricts a variable to a fixed set of named constants,
/// so an invalid category such as "Fod" cannot be stored by mistake.
/// </summary>
public enum Category
{
    Food,
    Transport,
    Utilities,
    Entertainment,
    Health,
    Other
}
