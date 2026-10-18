using System.Globalization;
using System.Text.Json;
using ExpenseTracker.Demos;
using ExpenseTracker.Models;
using ExpenseTracker.Services;

namespace ExpenseTracker;

/// <summary>
/// Presentation layer: a text menu that collects input, calls the ExpenseManager and prints results.
/// </summary>
internal static class Program
{
    // The JSON data file is stored in the folder the program is started from.
    private static readonly string DataFile = Path.Combine(Environment.CurrentDirectory, "expenses.json");

    // 'async Task Main' lets the whole program use await (asynchronous file I/O).
    private static async Task Main()
    {
        var manager = new ExpenseManager();
        var repository = new ExpenseRepository(DataFile);

        // Event handling: subscribe a lambda (anonymous method) to the manager's event.
        manager.ExpenseAdded += (_, expense) =>
            Console.WriteLine($"  [event] Expense #{expense.Id} added: {expense.Category}, {expense.Amount:N2}");

        Console.WriteLine("=== Personal Expense Tracker (C# / .NET) ===");
        await LoadAsync(manager, repository);

        bool running = true;
        while (running)
        {
            ShowMenu();
            string? choice = Console.ReadLine();

            try
            {
                switch (choice?.Trim().ToLowerInvariant())
                {
                    case "1": AddExpense(manager); break;
                    case "2": ListExpenses(manager.All); break;
                    case "3": FilterByCategory(manager); break;
                    case "4": ShowSummary(manager); break;
                    case "5": RemoveExpense(manager); break;
                    case "6":
                        await repository.SaveAsync(manager.All);
                        Console.WriteLine($"  Saved {manager.All.Count} expense(s) to expenses.json");
                        break;
                    case "7":
                        LanguageDemos.LinqDemo(manager.All);
                        LanguageDemos.RecordsAndPatternsDemo();
                        break;
                    case "8":
                        LanguageDemos.NullableReferenceDemo();
                        await LanguageDemos.ForgottenAwaitDemoAsync();
                        break;
                    case "9":
                        LanguageDemos.ExceptionHandlingDemo();
                        await ConcurrencyDemo.RunAsync();
                        break;
                    case "s": SeedSampleData(manager); break;
                    case "0":
                        await repository.SaveAsync(manager.All);
                        Console.WriteLine("  Data saved. Goodbye!");
                        running = false;
                        break;
                    default:
                        Console.WriteLine("  Please choose one of the menu options.");
                        break;
                }
            }
            catch (InvalidExpenseException ex)
            {
                // Our own validation exception: show a friendly message and keep running.
                Console.WriteLine($"  Invalid {ex.Field}: {ex.Message}");
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Exception filter ('when') limits this handler to file-related problems only.
                Console.WriteLine($"  File problem: {ex.Message}");
            }
        }
    }

    private static void ShowMenu()
    {
        Console.WriteLine();
        Console.WriteLine("--------------- MENU ---------------");
        Console.WriteLine(" 1  Add expense          5  Remove expense");
        Console.WriteLine(" 2  List all expenses    6  Save to file");
        Console.WriteLine(" 3  Filter by category   7  Demo: good aspects (LINQ, records)");
        Console.WriteLine(" 4  Summary (LINQ)       8  Demo: bad aspects (nullable, await)");
        Console.WriteLine(" s  Load sample data     9  Demo: exceptions + concurrency");
        Console.WriteLine(" 0  Save and exit");
        Console.Write("Choose: ");
    }

    // ---------------------------------------------------------------- loading
    private static async Task LoadAsync(ExpenseManager manager, ExpenseRepository repository)
    {
        try
        {
            List<Expense> items = await repository.LoadAsync();
            manager.ReplaceAll(items);
            Console.WriteLine($"Loaded {items.Count} expense(s) from expenses.json");
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Console.WriteLine($"Could not read saved data ({ex.GetType().Name}). Starting with an empty list.");
        }
    }

    // ---------------------------------------------------------------- commands
    private static void AddExpense(ExpenseManager manager)
    {
        decimal amount = ReadAmount();
        DateOnly date = ReadDate();
        Category category = ReadCategory();
        Console.Write("  Note (optional): ");
        string? note = Console.ReadLine();

        manager.Add(amount, date, category, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
    }

    private static void ListExpenses(IEnumerable<Expense> expenses)
    {
        var rows = expenses.ToList();
        if (rows.Count == 0)
        {
            Console.WriteLine("  No expenses recorded yet.");
            return;
        }

        Console.WriteLine("   Id | Date       | Category      |    Amount | Size   | Note");
        Console.WriteLine("  ----+------------+---------------+-----------+--------+---------------");
        foreach (Expense e in rows)
            Console.WriteLine($"  {e.Id,3} | {e.Date:yyyy-MM-dd} | {e.Category,-13} | {e.Amount,9:N2} | {e.SizeBand,-6} | {e.Note}");
    }

    private static void FilterByCategory(ExpenseManager manager)
    {
        Category category = ReadCategory();
        ListExpenses(manager.ByCategory(category));
    }

    private static void ShowSummary(ExpenseManager manager)
    {
        if (manager.All.Count == 0)
        {
            Console.WriteLine("  Nothing to summarise yet.");
            return;
        }

        Console.WriteLine("  Total by category:");
        foreach (var (category, total) in manager.TotalsByCategory())     // tuple deconstruction
            Console.WriteLine($"    {category,-13} {total,10:N2}");

        Console.WriteLine("  Total by month:");
        foreach (var (month, total) in manager.MonthlyTotals())
            Console.WriteLine($"    {month,-13} {total,10:N2}");

        Console.WriteLine("  Items by size band:");
        foreach (var band in manager.All.GroupBy(e => e.SizeBand).OrderBy(g => g.Key))
            Console.WriteLine($"    {band.Key,-13} {band.Count(),10}");

        Expense? largest = manager.Largest();
        if (largest is not null)
            Console.WriteLine($"  Largest expense: #{largest.Id} {largest.Category} {largest.Amount:N2}");

        Console.WriteLine($"  Grand total: {manager.All.Sum(e => e.Amount):N2}");
    }

    private static void RemoveExpense(ExpenseManager manager)
    {
        Console.Write("  Id to remove: ");
        if (int.TryParse(Console.ReadLine(), out int id) && manager.Remove(id))
            Console.WriteLine($"  Expense #{id} removed.");
        else
            Console.WriteLine("  No expense with that id.");
    }

    private static void SeedSampleData(ExpenseManager manager)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        manager.Add(12.50m, today.AddDays(-1), Category.Food, "Lunch");
        manager.Add(45.00m, today.AddDays(-3), Category.Transport, "Train pass top-up");
        manager.Add(120.75m, today.AddDays(-5), Category.Utilities, "Electricity bill");
        manager.Add(18.00m, today.AddDays(-6), Category.Entertainment, "Cinema");
        manager.Add(560.00m, today.AddDays(-10), Category.Health, "Dental check-up");
        manager.Add(32.40m, today.AddDays(-12), Category.Food, "Groceries");
        manager.Add(75.00m, today.AddDays(-20), Category.Other, null);
        manager.Add(9.99m, today.AddDays(-35), Category.Entertainment, "Streaming");
    }

    // ---------------------------------------------------------------- input helpers
    private static decimal ReadAmount()
    {
        Console.Write("  Amount (e.g. 12.50): ");
        string? text = Console.ReadLine();
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
            throw new InvalidExpenseException("Amount", $"'{text}' is not a valid number.");
        return amount;
    }

    private static DateOnly ReadDate()
    {
        Console.Write("  Date (yyyy-mm-dd, blank = today): ");
        string? text = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(text))
            return DateOnly.FromDateTime(DateTime.Today);
        if (!DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            throw new InvalidExpenseException("Date", $"'{text}' is not a valid date.");
        return date;
    }

    private static Category ReadCategory()
    {
        Console.WriteLine($"  Categories: {string.Join(", ", Enum.GetNames<Category>())}");
        Console.Write("  Category: ");
        string? text = Console.ReadLine();
        if (!Enum.TryParse(text, ignoreCase: true, out Category category) || !Enum.IsDefined(category))
            throw new InvalidExpenseException("Category", $"'{text}' is not one of the listed categories.");
        return category;
    }
}
