using FlagsRally.Services;

namespace FlagsRallyTests.Services;

public class DriveBoardCatalogTests
{
    // Trimmed from https://drive.google.com/embeddedfolderview?id=... (one folder and two files).
    private const string FolderHtml = """
        <div class="flip-entries"><div class="flip-entry" id="entry-1_fCjq2perY9YYO0AddMgdOzqelsh_gwZ" tabindex="0" role="link"><div class="flip-entry-info"><a href="https://drive.google.com/file/d/1_fCjq2perY9YYO0AddMgdOzqelsh_gwZ/view?usp=drive_web" target="_blank"><div class="flip-entry-visual"><div class="flip-entry-visual-card"><div class="flip-entry-icon"><img src="https://drive-thirdparty.googleusercontent.com/128/type/application/octet-stream" alt="As is"/></div></div></div><div class="flip-entry-list-icon"><img src="https://drive-thirdparty.googleusercontent.com/16/type/application/octet-stream" alt=""/></div><div class="flip-entry-title">全国一の宮巡り.json.encrypted</div></a></div><div class="flip-entry-last-modified"><div>Oct 6</div></div></div>
        <div class="flip-entry" id="entry-1Rf_ffmlASVKqAReH80THKvpcvMdEeRDq" tabindex="0" role="link"><div class="flip-entry-info"><a href="https://drive.google.com/drive/folders/1Rf_ffmlASVKqAReH80THKvpcvMdEeRDq" target="_blank"><div class="flip-entry-visual"><div class="flip-entry-visual-card"><div class="flip-entry-icon"><div aria-label="Folder" class="icon-color-1 drive-sprite-folder-grid-shared-icon"></div></div></div></div><div class="flip-entry-list-icon"><div aria-label="Folder" class="icon-color-1 drive-sprite-folder-list-shared-icon"></div></div><div class="flip-entry-title">Expo25</div></a></div><div class="flip-entry-last-modified"><div>9/28/25</div></div></div>
        <div class="flip-entry" id="entry-abc-123" tabindex="0" role="link"><div class="flip-entry-info"><a href="https://drive.google.com/file/d/abc-123/view?usp=drive_web" target="_blank"><div class="flip-entry-title">Notes &amp; Ideas.txt</div></a></div></div>
        </div>
        """;

    [Fact]
    public void Lists_folders_first_then_files()
    {
        var entries = DriveBoardCatalog.Parse(FolderHtml);

        Assert.Equal(
            [
                new DriveEntry("1Rf_ffmlASVKqAReH80THKvpcvMdEeRDq", "Expo25", IsFolder: true),
                new DriveEntry("abc-123", "Notes & Ideas.txt", IsFolder: false),
                new DriveEntry("1_fCjq2perY9YYO0AddMgdOzqelsh_gwZ", "全国一の宮巡り.json.encrypted", IsFolder: false),
            ],
            entries);
    }

    [Fact]
    public void Only_json_files_are_boards()
    {
        var entries = DriveBoardCatalog.Parse(FolderHtml);

        Assert.Equal(["全国一の宮巡り"], entries.Where(x => x.IsBoardFile).Select(x => x.DisplayName));
    }

    [Fact]
    public void An_empty_or_unexpected_page_has_no_entries()
    {
        Assert.Empty(DriveBoardCatalog.Parse("<html><body>Sign in</body></html>"));
    }

    [Theory]
    [InlineData("board.json", true, "board")]
    [InlineData("board.JSON.encrypted", true, "board")]
    [InlineData("board.zip", false, "board.zip")]
    public void Recognises_board_files(string fileName, bool isBoard, string displayName)
    {
        Assert.Equal(isBoard, CustomBoardFile.IsBoardFile(fileName));
        Assert.Equal(displayName, CustomBoardFile.StripExtension(fileName));
    }
}
