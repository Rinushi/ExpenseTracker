using System.Diagnostics;
using ExpenseTracker.Models;

namespace ExpenseTracker.Demos;

/// <summary>
/// Demonstrates three concurrency tools in C#: PLINQ (data parallelism), Task.WhenAll
/// (asynchronous concurrency) and a data race versus Interlocked (thread safety).
/// </summary>
public static class ConcurrencyDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== CONCURRENCY DEMO ===");
        Console.WriteLine($"Logical processors available: {Environment.ProcessorCount}");

        // ---- 1. Data parallelism with PLINQ -------------------------------------------
        var random = new Random(42);
        var categories = Enum.GetValues<Category>();
        var items = Enumerable.Range(1, 1_000_000)
            .Select(i => new Expense(i, random.Next(1, 1000), new DateOnly(2026, 1, 1).AddDays(i % 300),
                                     categories[random.Next(categories.Length)]))
            .ToList();

        var stopwatch = Stopwatch.StartNew();
        double sequentialResult = items.Sum(e => Work(e));
        long sequentialMs = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        double parallelResult = items.AsParallel().Sum(e => Work(e));   // only change: AsParallel()
        long parallelMs = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"1. Sequential LINQ : {sequentialMs,5} ms  (result {sequentialResult:N0})");
        Console.WriteLine($"   PLINQ           : {parallelMs,5} ms  (result {parallelResult:N0})");

        // ---- 2. Asynchronous concurrency with Task.WhenAll ----------------------------
        stopwatch.Restart();
        await SimulatedIoAsync();
        await SimulatedIoAsync();
        long oneAfterAnother = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        await Task.WhenAll(SimulatedIoAsync(), SimulatedIoAsync());     // both waits overlap
        long together = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"2. Two 500 ms I/O waits, awaited one after another: {oneAfterAnother} ms");
        Console.WriteLine($"   Two 500 ms I/O waits, Task.WhenAll             : {together} ms");

        // ---- 3. Data race versus Interlocked ------------------------------------------
        int unsafeCount = 0;
        int safeCount = 0;
        Parallel.For(0, 1_000_000, _ =>
        {
            unsafeCount++;                          // NOT thread-safe: read-modify-write can interleave
            Interlocked.Increment(ref safeCount);   // atomic operation
        });
        Console.WriteLine($"3. Expected 1,000,000 | unsynchronised: {unsafeCount:N0} | Interlocked: {safeCount:N0}");
        Console.WriteLine();
    }

    /// <summary>CPU-heavy work per item so that parallel speed-up is measurable.</summary>
    private static double Work(Expense e)
    {
        double x = (double)e.Amount;
        for (int k = 0; k < 50; k++)
            x = Math.Sqrt(x + k);
        return x;
    }

    private static Task SimulatedIoAsync() => Task.Delay(500);   // stands in for a network/disk wait
}
