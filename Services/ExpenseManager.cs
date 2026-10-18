using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// Business-logic layer. Owns the collection of expenses, validates new data, raises an
/// event when an expense is added and answers summary questions using LINQ.
/// The list itself is private (encapsulation): outside code can only use the public members.
/// </summary>
public sealed class ExpenseManager
{
    private readonly List<Expense> _expenses = new();
    private int _nextId = 1;

    /// <summary>Event (publisher side of the observer pattern): raised after each successful Add.</summary>
    public event EventHandler<Expense>? ExpenseAdded;

    /// <summary>Read-only view of all expenses.</summary>
    public IReadOnlyList<Expense> All => _expenses.AsReadOnly();

    /// <summary>Validates and stores a new expense. Throws InvalidExpenseException on bad input.</summary>
    public Expense Add(decimal amount, DateOnly date, Category category, string? note)
    {
        Validate(amount, date);
        var expense = new Expense(_nextId++, amount, date, category, note);
        _expenses.Add(expense);
        ExpenseAdded?.Invoke(this, expense);   // '?.' = only call if someone subscribed
        return expense;
    }

    private static void Validate(decimal amount, DateOnly date)
    {
        if (amount <= 0m)
            throw new InvalidExpenseException("Amount", "must be greater than zero.");
        if (decimal.Round(amount, 2) != amount)
            throw new InvalidExpenseException("Amount", "cannot have more than two decimal places.");
        if (date > DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidExpenseException("Date", "cannot be in the future.");
    }

    /// <summary>Removes the expense with the given id; returns false if it does not exist.</summary>
    public bool Remove(int id) => _expenses.RemoveAll(e => e.Id == id) > 0;

    /// <summary>Replaces the current data (used after loading from the JSON file).</summary>
    public void ReplaceAll(IEnumerable<Expense> expenses)
    {
        _expenses.Clear();
        _expenses.AddRange(expenses);
        _nextId = _expenses.Count == 0 ? 1 : _expenses.Max(e => e.Id) + 1;
    }

    /// <summary>All expenses in one category, oldest first (LINQ method syntax).</summary>
    public IEnumerable<Expense> ByCategory(Category category) =>
        _expenses.Where(e => e.Category == category).OrderBy(e => e.Date);

    /// <summary>Total spend per category, largest first (LINQ query syntax).</summary>
    public IEnumerable<(Category Category, decimal Total)> TotalsByCategory() =>
        from e in _expenses
        group e by e.Category into g
        orderby g.Sum(x => x.Amount) descending
        select (Category: g.Key, Total: g.Sum(x => x.Amount));

    /// <summary>Total spend per calendar month, oldest first (LINQ method syntax).</summary>
    public IEnumerable<(string Month, decimal Total)> MonthlyTotals() =>
        _expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => (Month: $"{g.Key.Year}-{g.Key.Month:D2}", Total: g.Sum(e => e.Amount)));

    /// <summary>The single most expensive item, or null when there are no expenses.</summary>
    public Expense? Largest() => _expenses.MaxBy(e => e.Amount);
}
