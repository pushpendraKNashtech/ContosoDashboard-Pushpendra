using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class DocumentRulesTests
{
    [Fact]
    public void SupportedTypesIncludeRequiredFormats()
    {
        Assert.Contains(".pdf", DocumentRules.AllowedTypes.Keys);
        Assert.Contains(".docx", DocumentRules.AllowedTypes.Keys);
        Assert.Contains(".xlsx", DocumentRules.AllowedTypes.Keys);
        Assert.Contains(".pptx", DocumentRules.AllowedTypes.Keys);
        Assert.Contains(".png", DocumentRules.AllowedTypes.Keys);
    }

    [Fact]
    public void MaximumFileSizeIsTwentyFiveMegabytes()
    {
        Assert.Equal(25 * 1024 * 1024, DocumentRules.MaxFileSize);
    }

    [Fact]
    public void CategoriesAreControlled()
    {
        Assert.Contains("Project Documents", DocumentRules.Categories);
        Assert.Contains("Personal Files", DocumentRules.Categories);
        Assert.DoesNotContain("Executable", DocumentRules.Categories);
    }
}
