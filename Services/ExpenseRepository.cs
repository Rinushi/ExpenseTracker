using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// Data-access layer. Saves and loads expenses as JSON using asynchronous file I/O so the
/// program thread is not blocked while the disk is read or written.
/// </summary>
public sealed class ExpenseRepository
{
    private readonly string _path;

    // One shared, reusable set of options: pretty-printed JSON, enums written as text.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ExpenseRepository(string path)
    {
        _path = path;
    }

    /// <summary>Writes the expenses to the JSON file (overwrites any existing file).</summary>
    public async Task SaveAsync(IEnumerable<Expense> expenses, CancellationToken ct = default)
    {
        // 'await using' guarantees the stream is closed even if an exception is thrown.
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, expenses, Options, ct);
    }

    /// <summary>Reads the expenses back; returns an empty list if the file does not exist yet.</summary>
    public async Task<List<Expense>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return new List<Expense>();

        await using var stream = File.OpenRead(_path);
        var items = await JsonSerializer.DeserializeAsync<List<Expense>>(stream, Options, ct);
        return items ?? new List<Expense>();   // '??' supplies a fallback when the result is null
    }
}
