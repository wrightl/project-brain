using FluentAssertions;

namespace ProjectBrain.Api.Tests;

public class AzureSearchDocumentIdsTests
{
    [Fact]
    public void BuildSearchDocumentId_filename_with_dot_uses_hex_hash()
    {
        var id = AzureSearchDocumentIds.BuildSearchDocumentId("onboarding.md", 1);

        id.Should().MatchRegex("^[0-9a-f]{64}$");
        id.Should().NotContain(".").And.NotContain("_");
    }

    [Fact]
    public void BuildSearchDocumentId_guid_resource_keeps_guid_underscore_page_suffix()
    {
        var guid = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

        var id = AzureSearchDocumentIds.BuildSearchDocumentId(guid, 2);

        id.Should().Be($"{guid}_2");
    }

    [Fact]
    public void BuildSearchDocumentId_safe_plain_id_uses_composite_form()
    {
        var id = AzureSearchDocumentIds.BuildSearchDocumentId("readme", 1);

        id.Should().Be("readme_1");
    }

    [Fact]
    public void BuildSearchDocumentId_is_deterministic_for_hashed_path()
    {
        var a = AzureSearchDocumentIds.BuildSearchDocumentId("onboarding.md", 1);
        var b = AzureSearchDocumentIds.BuildSearchDocumentId("onboarding.md", 1);

        a.Should().Be(b);
    }

    [Fact]
    public void BuildSearchDocumentId_differs_by_page_for_hashed_path()
    {
        var p1 = AzureSearchDocumentIds.BuildSearchDocumentId("onboarding.md", 1);
        var p2 = AzureSearchDocumentIds.BuildSearchDocumentId("onboarding.md", 2);

        p1.Should().NotBe(p2);
    }

    [Fact]
    public void BuildSearchDocumentId_differs_by_user_scoped_blob_path()
    {
        var userA = AzureSearchDocumentIds.BuildSearchDocumentId("auth0|user-a/onboarding/onboarding.md", 1);
        var userB = AzureSearchDocumentIds.BuildSearchDocumentId("auth0|user-b/onboarding/onboarding.md", 1);

        userA.Should().NotBe(userB);
    }

    [Fact]
    public void BuildSearchDocumentId_trims_whitespace_for_guid_parse()
    {
        var guid = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

        var id = AzureSearchDocumentIds.BuildSearchDocumentId($"  {guid}  ", 1);

        id.Should().Be($"{guid}_1");
    }
}
