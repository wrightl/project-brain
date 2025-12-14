using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Shared.Dtos.Tags;

public class TagServices(
    ITagService tagService,
    IIdentityService identityService,
    ILogger<TagServices> logger)
{
    public ILogger<TagServices> Logger { get; } = logger;
    public ITagService TagService { get; } = tagService;
    public IIdentityService IdentityService { get; } = identityService;
}

public static class TagEndpoints
{
    public static void MapTagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("tag").RequireAuthorization();

        group.MapPost("/", CreateTag).WithName("CreateTag");
        group.MapGet("/{id:guid}", GetTagById).WithName("GetTagById");
        group.MapGet("/", GetAllTagsForUser).WithName("GetAllTagsForUser");
        group.MapGet("/name/{name}", GetTagByName).WithName("GetTagByName");
        group.MapPut("/{id:guid}", UpdateTag).WithName("UpdateTag");
        group.MapDelete("/{id:guid}", DeleteTag).WithName("DeleteTag");
    }

    private static async Task<IResult> CreateTag(
        [AsParameters] TagServices services,
        CreateTagRequestDto request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var createdTag = await services.TagService.Add(tag);
        var dto = TagMapper.ToDto(createdTag);
        return Results.Created($"/tag/{createdTag.Id}", dto);
    }

    private static async Task<IResult> GetTagById(
        [AsParameters] TagServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var tag = await services.TagService.GetById(id, userId);
        if (tag == null)
        {
            return Results.NotFound();
        }

        var dto = TagMapper.ToDto(tag);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetAllTagsForUser(
        [AsParameters] TagServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var tags = await services.TagService.GetAllForUser(userId);
        var tagDtos = TagMapper.ToDtoList(tags);
        return Results.Ok(tagDtos);
    }

    private static async Task<IResult> GetTagByName(
        [AsParameters] TagServices services,
        string name)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var tag = await services.TagService.GetByName(name, userId);
        if (tag == null)
        {
            return Results.NotFound();
        }

        var dto = TagMapper.ToDto(tag);
        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdateTag(
        [AsParameters] TagServices services,
        Guid id,
        CreateTagRequestDto request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var tag = await services.TagService.GetById(id, userId);
        if (tag == null)
        {
            return Results.NotFound();
        }

        tag.Name = request.Name.Trim();
        var updatedTag = await services.TagService.Update(tag);
        var dto = TagMapper.ToDto(updatedTag);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteTag(
        [AsParameters] TagServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var tag = await services.TagService.GetById(id, userId);
        if (tag == null)
        {
            return Results.NotFound();
        }

        await services.TagService.Remove(tag);
        return Results.NoContent();
    }
}

