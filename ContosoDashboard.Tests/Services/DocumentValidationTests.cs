using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class DocumentValidationTests
{
    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".png", "image/png")]
    public void AllowListContainsSupportedType(string extension, string contentType)
    {
        Assert.True(DocumentRules.AllowedTypes.TryGetValue(extension, out var expected));
        Assert.Equal(contentType, expected);
    }

    [Fact]
    public void SizeBoundaryIsTwentyFiveMegabytes()
    {
        Assert.True(DocumentRules.MaxFileSize <= 25 * 1024 * 1024);
        Assert.False(DocumentRules.MaxFileSize + 1 <= 25 * 1024 * 1024);
    }
}