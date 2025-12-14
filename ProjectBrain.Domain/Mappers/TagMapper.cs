namespace ProjectBrain.Domain.Mappers;

using ProjectBrain.Shared.Dtos.Tags;

public static class TagMapper
{
    public static TagResponseDto ToDto(Tag tag)
    {
        return new TagResponseDto
        {
            Id = tag.Id.ToString(),
            Name = tag.Name,
            CreatedAt = tag.CreatedAt.ToString("O")
        };
    }

    public static List<TagResponseDto> ToDtoList(IEnumerable<Tag> tags)
    {
        return tags.Select(ToDto).ToList();
    }
}

