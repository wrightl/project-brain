namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.SystemTags;
using System.Text.Json;

public class SystemTagService : ISystemTagService
{
    private readonly ISystemTagRepository _repository;

    public SystemTagService(ISystemTagRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<List<SystemTag>> GetAllWithFields(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllWithFieldsAsync(cancellationToken);
    }

    public async Task<List<SystemTag>> GetByIds(IEnumerable<Guid> systemTagIds, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdsAsync(systemTagIds, cancellationToken);
    }

    public static List<SystemTagResponseDto> ToDtoList(IEnumerable<SystemTag> systemTags)
    {
        return systemTags.Select(ToDto).ToList();
    }

    public static SystemTagResponseDto ToDto(SystemTag systemTag)
    {
        var fieldDtos = (systemTag.FieldDefinitions ?? new List<SystemTagFieldDefinition>())
            .OrderBy(fd => fd.FieldOrder)
            .Select(fd => new SystemTagFieldDefinitionResponseDto
            {
                Id = fd.Id.ToString(),
                FieldKey = fd.FieldKey,
                Label = fd.Label,
                InputType = fd.InputType,
                Required = fd.Required,
                FieldOrder = fd.FieldOrder,
                Placeholder = fd.Placeholder,
                Hint = fd.Hint,
                Options = DeserializeOptions(fd.OptionsJson),
                MinValue = fd.MinValue,
                MaxValue = fd.MaxValue,
                StepValue = fd.StepValue
            })
            .ToList();

        return new SystemTagResponseDto
        {
            Id = systemTag.Id.ToString(),
            Key = systemTag.Key,
            Name = systemTag.Name,
            Description = systemTag.Description,
            FieldDefinitions = fieldDtos
        };
    }

    private static List<string>? DeserializeOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsJson);
        }
        catch
        {
            return null;
        }
    }
}

public interface ISystemTagService
{
    Task<List<SystemTag>> GetAllWithFields(CancellationToken cancellationToken = default);
    Task<List<SystemTag>> GetByIds(IEnumerable<Guid> systemTagIds, CancellationToken cancellationToken = default);
}

