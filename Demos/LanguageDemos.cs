using System.Text.Json;
using ExpenseTracker.Models;
using ExpenseTracker.Services;

namespace ExpenseTracker.Demos;

/// <summary>
/// Small, self-contained demonstrations used as evidence in the technical report:
/// two GOOD aspects of C# (LINQ; records + pattern matching) and
/// two BAD aspects (nullable reference types are only advisory; a forgotten 'await' loses errors).
/// </summary>
public static class LanguageDemos
{
    // =====================================================================
    // GOOD ASPECT 1: LINQ (Language Integrated Query)
    // =====================================================================
    public static void LinqDemo(IReadOnlyList<Expense> expenses)
    {
        Console.WriteLine("=== GOOD ASPECT 1: LINQ ===");

        // Method syntax: filter -> sort -> take -> project, in one readable chain.
        var topThree = expenses
            .OrderByDescending(e => e.Amount)
            .Take(3)
            .Select(e => $"#{e.Id} {e.Category} {e.Amount:N2}");
        Console.WriteLine("Top 3 expenses : " + string.Join("; ", topThree));

        // Query syntax: reads like SQL but is checked by the C# compiler.
        var perCategory =
            from e in expenses
            group e by e.Category into g
            select $"{g.Key,-13} {g.Count()} item(s), total {g.Sum(x => x.Amount):N2}";
        foreach (var line in perCategory)
            Console.WriteLine("  " + line);

        // Deferred execution: the query runs when it is enumerated, not when it is declared.
        var numbers = new List<int> { 1, 2, 3 };
        var query = numbers.Where(n => n > 1);      // nothing executed yet
        numbers.Add(4);                              // data changes AFTER the query was written
        Console.WriteLine("Deferred execution  : " + string.Join(",", query) + "   (4 is included)");
        Console.WriteLine();
    }

    // =====================================================================
    // GOOD ASPECT 2: records, 'with' expressions and pattern matching
    // =====================================================================
    public static void RecordsAndPatternsDemo()
    {
        Console.WriteLine("=== GOOD ASPECT 2: Records and pattern matching ===");

        var a = new Expense(1, 12.50m, new DateOnly(2026, 10, 1), Category.Food, "lunch");
        var b = new Expense(1, 12.50m, new DateOnly(2026, 10, 1), Category.Food, "lunch");
        var c = a with { Amount = 600m };            // non-destructive copy with one change

        Console.WriteLine($"a == b  (value equality)    : {a == b}");
        Console.WriteLine($"ReferenceEquals(a, b)       : {ReferenceEquals(a, b)}");
        Console.WriteLine($"a.SizeBand / c.SizeBand     : {a.SizeBand} / {c.SizeBand}");
        Console.WriteLine($"a.ToString() (generated)    : {a}");

        foreach (object? value in new object?[] { 42, -7, 3.5m, "", "hello", null, a })
            Console.WriteLine($"  Describe({value ?? "null"}) -> {Describe(value)}");
        Console.WriteLine();
    }

    /// <summary>Switch expression combining type, relational, property and 'when' patterns.</summary>
    private static string Describe(object? value) => value switch
    {
        int n when n < 0     => "negative integer",
        int                  => "integer",
        decimal d            => $"decimal {d:N2}",
        string { Length: 0 } => "empty string",
        string s             => $"string of length {s.Length}",
        _                    => value?.GetType().Name ?? "null"
    };

    // =====================================================================
    // BAD ASPECT 1: nullable reference types are only compiler WARNINGS
    // =====================================================================
    public sealed class Customer
    {
        // Declared NON-nullable (string, not string?) - yet it can still become null at run time.
        public string Name { get; set; } = "unknown";
    }

    public static void NullableReferenceDemo()
    {
        Console.WriteLine("=== BAD ASPECT 1: nullable reference types are advisory only ===");
        var e = new Expense(99, 5m, new DateOnly(2026, 10, 1), Category.Other);   // Note is null

        // (a) Build-time warning CS8602 (dereference of a possibly null reference) on the next line.
        //     The program still compiles and runs, so the mistake reaches run time.
        try
        {
            int length = e.Note.Length;
            Console.WriteLine($"(a) length = {length}");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("(a) Warning ignored by the build -> NullReferenceException at run time.");
        }

        // (b) The null-forgiving operator '!' switches the warning off completely.
        try
        {
            int length = e.Note!.Length;
            Console.WriteLine($"(b) length = {length}");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("(b) With '!' the compiler is silent, but the program still crashes.");
        }

        // (c) The annotation is not enforced at run time: JSON can put null into a non-nullable property.
        var customer = JsonSerializer.Deserialize<Customer>("{\"Name\": null}");
        Console.WriteLine($"(c) Non-nullable 'Name' is null after deserialising: {customer!.Name is null}");
        Console.WriteLine();
    }

    // =====================================================================
    // BAD ASPECT 2: a forgotten 'await' compiles and silently loses the exception
    // =====================================================================
    public static async Task ForgottenAwaitDemoAsync()
    {
        Console.WriteLine("=== BAD ASPECT 2: forgotten await ===");
        string badPath = Path.Combine(AppContext.BaseDirectory, "no_such_folder", "expenses.json");
        var repository = new ExpenseRepository(badPath);
        var data = new List<Expense>();

        // BUG (deliberate): 'await' is missing. The build only gives warning CS4014.
        // The Task fails (the folder does not exist) but nobody observes the failure.
        repository.SaveAsync(data);

        await Task.Delay(300);   // give the background task time to fail
        Console.WriteLine("(a) Without await: no error shown, program carried on as if saving worked.");
        Console.WriteLine($"    Did the file get written? {File.Exists(badPath)}");

        try
        {
            await repository.SaveAsync(data);   // correct version
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine($"(b) With await the failure is visible: {ex.GetType().Name}");
        }
        Console.WriteLine();
    }

    // =====================================================================
    // Exception handling demonstration (try / catch with filter / finally)
    // =====================================================================
    public static void ExceptionHandlingDemo()
    {
        Console.WriteLine("=== EXCEPTION HANDLING DEMO ===");
        var manager = new ExpenseManager();
        var today = DateOnly.FromDateTime(DateTime.Today);

        try
        {
            manager.Add(-5m, today, Category.Food, "negative amount");
        }
        catch (InvalidExpenseException ex) when (ex.Field == "Amount")   // exception filter
        {
            Console.WriteLine($"Caught (filter: Amount) -> {ex.Message}");
        }
        catch (InvalidExpenseException ex)
        {
            Console.WriteLine($"Caught (any other field) -> {ex.Field}: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("finally block always runs (cleanup code goes here).");
        }

        try
        {
            manager.Add(10m, today.AddDays(5), Category.Food, "future date");
        }
        catch (InvalidExpenseException ex)
        {
            Console.WriteLine($"Caught -> {ex.Field}: {ex.Message}");
        }
        Console.WriteLine();
    }
}
