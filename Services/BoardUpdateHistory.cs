using System.Globalization;
using System.Text;

namespace FlagsRally.Services;

/// <summary>
/// One line of the update history kept next to the shared boards (更新履歴.csv):
/// a stamp trail that was added to or updated in the folder.
/// </summary>
public record BoardUpdateEntry(DateOnly Date, string Collection, string Action, string File, string Name, int? Count, string Note)
{
    public bool IsAdded => Action.Equals("added", StringComparison.OrdinalIgnoreCase);
    public bool IsUpdated => Action.Equals("updated", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Reads the update history CSV published in the shared board folder.
/// Columns: date,collection,action,file,name,count,note (a header line, UTF-8, optionally with a BOM).
/// </summary>
public static class BoardUpdateHistory
{
    public const string FileName = "更新履歴.csv";

    public static bool IsHistoryFile(DriveEntry entry) =>
        !entry.IsFolder && entry.Name.Equals(FileName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The entries newest first; entries of the same day keep the order of the file.
    /// Lines without a valid date or name are skipped, so a hand-edited file never breaks the page.
    /// </summary>
    public static List<BoardUpdateEntry> Parse(string csv)
    {
        var rows = ReadRows(csv.TrimStart('﻿'));
        if (rows.Count == 0) return [];

        var header = rows[0].Select(x => x.Trim().ToLowerInvariant()).ToList();
        string Field(List<string> row, string column)
        {
            var index = header.IndexOf(column);
            return index >= 0 && index < row.Count ? row[index].Trim() : "";
        }

        var entries = new List<BoardUpdateEntry>();
        foreach (var row in rows.Skip(1))
        {
            if (!DateOnly.TryParseExact(Field(row, "date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;

            var file = Field(row, "file");
            var name = Field(row, "name");
            if (name.Length == 0) name = CustomBoardFile.StripExtension(file);
            if (name.Length == 0) continue;

            int? count = int.TryParse(Field(row, "count"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
            entries.Add(new BoardUpdateEntry(date, Field(row, "collection"), Field(row, "action"), file, name, count, Field(row, "note")));
        }

        // OrderByDescending is stable, so a day's entries stay in file order.
        return entries.OrderByDescending(x => x.Date).ToList();
    }

    // RFC 4180: fields may be quoted, quotes inside are doubled, and quoted fields may hold commas and line breaks.
    static List<List<string>> ReadRows(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        void EndField()
        {
            row.Add(field.ToString());
            field.Clear();
        }
        void EndRow()
        {
            EndField();
            if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
            row = [];
        }

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else field.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') EndField();
            else if (c == '\r') { }
            else if (c == '\n') EndRow();
            else field.Append(c);
        }
        if (field.Length > 0 || row.Count > 0) EndRow();
        return rows;
    }
}
