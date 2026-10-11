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
    public void BuildSearchDocumentId_trims_whitespace_for_guid_parse()
    {
        var guid = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

        var id = AzureSearchDocumentIds.BuildSearchDocumentId($"  {guid}  ", 1);

        id.Should().Be($"{guid}_1");
    }

    [Fact]
    public void TryGetResourceId_round_trips_guid_document_id()
    {
        var guid = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var documentId = AzureSearchDocumentIds.BuildSearchDocumentId(guid.ToString(), 12);

        AzureSearchDocumentIds.TryGetResourceId(documentId, out var resourceId).Should().BeTrue();
        resourceId.Should().Be(guid);
    }

    [Fact]
    public void TryGetResourceId_accepts_bare_guid()
    {
        var guid = Guid.NewGuid();

        AzureSearchDocumentIds.TryGetResourceId(guid.ToString(), out var resourceId).Should().BeTrue();
        resourceId.Should().Be(guid);
    }

    [Theory]
    [InlineData("readme_1")]
    [InlineData("3fa85f64-5717-4562-b3fc-2c963f66afa6_x")]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public void TryGetResourceId_rejects_ids_without_a_guid(string id)
    {
        AzureSearchDocumentIds.TryGetResourceId(id, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetResourceId_rejects_hashed_document_id()
    {
        var documentId = AzureSearchDocumentIds.BuildSearchDocumentId("onboarding.md", 1);

        AzureSearchDocumentIds.TryGetResourceId(documentId, out _).Should().BeFalse();
    }
}
