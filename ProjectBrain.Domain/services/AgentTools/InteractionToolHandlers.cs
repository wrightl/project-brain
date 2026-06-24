namespace ProjectBrain.Domain.AgentTools;

using System.Text.Json;

public sealed class AskUserToolHandler : IAgentToolHandler
{
  private const int MinOptions = 2;
  private const int MaxOptions = 6;
  private const int MaxLabelLength = 80;

  public string Name => "ask_user";

  public bool PausesTurn => true;

  public Dictionary<string, object> GetDefinition() => new()
  {
    ["type"] = "function",
    ["function"] = new Dictionary<string, object>
    {
      ["name"] = Name,
      ["description"] = "Present the user with clickable multiple-choice options. Use when offering 2-6 discrete choices instead of listing options in plain text.",
      ["parameters"] = new Dictionary<string, object>
      {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
          ["prompt"] = new Dictionary<string, object>
          {
            ["type"] = "string",
            ["description"] = "Optional short question shown with the options"
          },
          ["allowMultiple"] = new Dictionary<string, object>
          {
            ["type"] = "boolean",
            ["description"] = "Whether the user may select more than one option"
          },
          ["options"] = new Dictionary<string, object>
          {
            ["type"] = "array",
            ["description"] = "2-6 choices for the user",
            ["items"] = new Dictionary<string, object>
            {
              ["type"] = "object",
              ["properties"] = new Dictionary<string, object>
              {
                ["id"] = new Dictionary<string, object>
                {
                  ["type"] = "string",
                  ["description"] = "Stable identifier for this option"
                },
                ["label"] = new Dictionary<string, object>
                {
                  ["type"] = "string",
                  ["description"] = "Short label shown on the clickable option"
                }
              },
              ["required"] = new[] { "id", "label" }
            }
          }
        },
        ["required"] = new[] { "options" }
      }
    }
  };

  public Task<object> ExecuteAsync(
    AgentToolContext context,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken = default)
  {
    var prompt = parameters.TryGetValue("prompt", out var promptValue)
      ? AgentToolParameterParser.ParseString(promptValue, "prompt", required: false)
      : null;

    var allowMultiple = parameters.TryGetValue("allowMultiple", out var allowMultipleValue)
      ? AgentToolParameterParser.ParseBool(allowMultipleValue, "allowMultiple")
      : false;

    if (!parameters.TryGetValue("options", out var optionsValue) || optionsValue is null)
    {
      throw new ArgumentException("options parameter is required");
    }

    var options = ParseOptions(optionsValue);
    if (options.Count < MinOptions || options.Count > MaxOptions)
    {
      throw new ArgumentException($"options must contain between {MinOptions} and {MaxOptions} items");
    }

    return Task.FromResult<object>(new
    {
      success = true,
      status = "awaiting_user_input",
      prompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt.Trim(),
      allowMultiple,
      options
    });
  }

  private static List<object> ParseOptions(object value)
  {
    var options = new List<object>();

    if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
    {
      foreach (var item in jsonElement.EnumerateArray())
      {
        options.Add(ParseOption(item));
      }

      return options;
    }

    if (value is IEnumerable<object> enumerable)
    {
      foreach (var item in enumerable)
      {
        if (item is JsonElement element)
        {
          options.Add(ParseOption(element));
        }
        else if (item is Dictionary<string, object> dict)
        {
          options.Add(ParseOption(dict));
        }
        else
        {
          throw new ArgumentException("options must be an array of objects with id and label");
        }
      }

      return options;
    }

    throw new ArgumentException("options must be an array of objects with id and label");
  }

  private static object ParseOption(JsonElement item)
  {
    if (item.ValueKind != JsonValueKind.Object)
    {
      throw new ArgumentException("Each option must be an object with id and label");
    }

    if (!item.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.String)
    {
      throw new ArgumentException("Each option must include a string id");
    }

    if (!item.TryGetProperty("label", out var labelElement) || labelElement.ValueKind != JsonValueKind.String)
    {
      throw new ArgumentException("Each option must include a string label");
    }

    var id = idElement.GetString() ?? string.Empty;
    var label = labelElement.GetString() ?? string.Empty;
    ValidateOption(id, label);

    return new { id, label };
  }

  private static object ParseOption(Dictionary<string, object> item)
  {
    if (!item.TryGetValue("id", out var idValue) || !item.TryGetValue("label", out var labelValue))
    {
      throw new ArgumentException("Each option must include id and label");
    }

    var id = AgentToolParameterParser.ParseString(idValue, "id");
    var label = AgentToolParameterParser.ParseString(labelValue, "label");
    ValidateOption(id, label);

    return new { id, label };
  }

  private static void ValidateOption(string id, string label)
  {
    if (string.IsNullOrWhiteSpace(id))
    {
      throw new ArgumentException("Each option id must be non-empty");
    }

    if (string.IsNullOrWhiteSpace(label))
    {
      throw new ArgumentException("Each option label must be non-empty");
    }

    if (label.Length > MaxLabelLength)
    {
      throw new ArgumentException($"Each option label must be at most {MaxLabelLength} characters");
    }
  }
}
