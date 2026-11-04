using FlagsRally.Services;

namespace FlagsRallyTests.Services;

public class BoardUpdateHistoryTests
{
    [Fact]
    public void Reads_the_published_columns_newest_first()
    {
        var csv = "﻿date,collection,action,file,name,count,note\r\n" +
                  "2025-11-16,マンホールカード,added,マンホールカード第23弾.json.encrypted,マンホールカード第23弾,41,\r\n" +
                  "2026-10-10,マンホールカード,updated,マンホールカード第26弾.json.encrypted,マンホールカード第26弾,40,藤沢市を追加\r\n" +
                  "2026-10-10,マンホールカード,added,マンホールカード第13弾.json.encrypted,マンホールカード第13弾,50,\r\n";

        var entries = BoardUpdateHistory.Parse(csv);

        Assert.Equal(
            [
                new BoardUpdateEntry(new DateOnly(2026, 10, 10), "マンホールカード", "updated", "マンホールカード第26弾.json.encrypted", "マンホールカード第26弾", 40, "藤沢市を追加"),
                new BoardUpdateEntry(new DateOnly(2026, 10, 10), "マンホールカード", "added", "マンホールカード第13弾.json.encrypted", "マンホールカード第13弾", 50, ""),
                new BoardUpdateEntry(new DateOnly(2025, 11, 16), "マンホールカード", "added", "マンホールカード第23弾.json.encrypted", "マンホールカード第23弾", 41, ""),
            ],
            entries);
        Assert.True(entries[0].IsUpdated);
        Assert.True(entries[1].IsAdded);
    }

    [Fact]
    public void Quoted_fields_may_hold_commas_quotes_and_line_breaks()
    {
        var csv = "date,collection,action,file,name,count,note\n" +
                  "2026-10-11,Expo25,added,a.json,A,3,\"Tokyo, Osaka and \"\"Kyoto\"\"\nsecond line\"\n";

        var entry = Assert.Single(BoardUpdateHistory.Parse(csv));

        Assert.Equal("Tokyo, Osaka and \"Kyoto\"\nsecond line", entry.Note);
    }

    [Fact]
    public void Columns_are_found_by_header_name()
    {
        var csv = "name,date,note\nA,2026-01-02,hi\n";

        var entry = Assert.Single(BoardUpdateHistory.Parse(csv));

        Assert.Equal(new BoardUpdateEntry(new DateOnly(2026, 1, 2), "", "", "", "A", null, "hi"), entry);
    }

    [Fact]
    public void Broken_lines_are_skipped()
    {
        var csv = "date,collection,action,file,name,count,note\n" +
                  "someday,x,added,a.json,A,1,\n" +
                  "2026-01-02,x,added,,,1,\n" +
                  "\n" +
                  "2026-01-03,x,added,b.json.encrypted,,many,\n";

        var entry = Assert.Single(BoardUpdateHistory.Parse(csv));

        Assert.Equal("b", entry.Name);
        Assert.Null(entry.Count);
    }

    [Fact]
    public void Empty_text_has_no_entries()
    {
        Assert.Empty(BoardUpdateHistory.Parse(""));
        Assert.Empty(BoardUpdateHistory.Parse("﻿"));
    }

    [Fact]
    public void Only_the_history_csv_is_the_history_file()
    {
        Assert.True(BoardUpdateHistory.IsHistoryFile(new DriveEntry("1", "更新履歴.csv", IsFolder: false)));
        Assert.False(BoardUpdateHistory.IsHistoryFile(new DriveEntry("2", "更新履歴.csv", IsFolder: true)));
        Assert.False(BoardUpdateHistory.IsHistoryFile(new DriveEntry("3", "other.csv", IsFolder: false)));
    }
}
