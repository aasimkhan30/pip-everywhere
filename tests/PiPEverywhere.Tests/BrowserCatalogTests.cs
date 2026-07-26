using PiPEverywhere.Services;

namespace PiPEverywhere.Tests;

public sealed class BrowserCatalogTests
{
    [Theory]
    [InlineData("msedge", "Chrome_WidgetWin_1", "Picture in picture", "edge")]
    [InlineData("chrome", "Chrome_WidgetWin_1", "Picture-in-Picture", "chrome")]
    [InlineData("firefox", "MozillaDialogClass", "Picture-in-Picture", "firefox")]
    [InlineData("brave", "Chrome_WidgetWin_1", "PICTURE IN PICTURE", "brave")]
    [InlineData("opera", "Chrome_WidgetWin_1", "Picture in picture", "opera")]
    [InlineData("vivaldi", "Chrome_WidgetWin_1", "Picture in picture", "vivaldi")]
    public void Match_ReturnsExpectedBrowser(
        string process,
        string windowClass,
        string title,
        string expectedId)
    {
        var browser = BrowserCatalog.Match(process, windowClass, title);

        Assert.NotNull(browser);
        Assert.Equal(expectedId, browser.Id);
    }

    [Theory]
    [InlineData("msedge", "Chrome_WidgetWin_1", "A normal browser tab")]
    [InlineData("notepad", "Notepad", "Picture in picture")]
    [InlineData("msedge", "UnexpectedWindowClass", "Picture in picture")]
    public void Match_RejectsNonPipWindows(string process, string windowClass, string title)
    {
        Assert.Null(BrowserCatalog.Match(process, windowClass, title));
    }

    [Fact]
    public void Catalog_HasUniqueIds()
    {
        var ids = BrowserCatalog.All.Select(browser => browser.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
