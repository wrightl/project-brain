using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Pagination;
using ProjectBrain.Shared.Dtos.Quizzes;

public class QuizServices(
    ILogger<QuizServices> logger,
    IQuizService quizService,
    IQuizResponseService quizResponseService,
    IQuizRepository quizRepository,
    IQuizResponseRepository quizResponseRepository,
    IIdentityService identityService)
{
    public ILogger<QuizServices> Logger { get; } = logger;
    public IQuizService QuizService { get; } = quizService;
    public IQuizResponseService QuizResponseService { get; } = quizResponseService;
    public IQuizRepository QuizRepository { get; } = quizRepository;
    public IQuizResponseRepository QuizResponseRepository { get; } = quizResponseRepository;
    public IIdentityService IdentityService { get; } = identityService;
}

public static class QuizEndpoints
{
    public static void MapQuizEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("quizes").RequireAuthorization();

        group.MapGet("/", GetAllQuizzes).WithName("GetAllQuizzes");
        group.MapGet("/{quizId}", GetQuizById).WithName("GetQuizById");
        group.MapPost("/", CreateQuiz).WithName("CreateQuiz").RequireAuthorization("AdminOnly");
        group.MapPut("/{quizId}", UpdateQuiz).WithName("UpdateQuiz").RequireAuthorization("AdminOnly");
        group.MapDelete("/{quizId}", DeleteQuiz).WithName("DeleteQuiz").RequireAuthorization("AdminOnly");
        group.MapPost("/{quizId}/responses", SubmitQuizResponse).WithName("SubmitQuizResponse");
        group.MapGet("/{quizId}/responses", GetQuizResponses).WithName("GetQuizResponses");
        group.MapGet("/responses", GetAllQuizResponsesForUser).WithName("GetAllQuizResponsesForUser");
        group.MapGet("/responses/{responseId}", GetQuizResponseById).WithName("GetQuizResponseById");
        group.MapGet("/responses/count", GetQuizResponsesCount).WithName("GetUserQuizResponsesCount");
        group.MapDelete("/responses/{responseId}", DeleteQuizResponse).WithName("DeleteQuizResponse");
        group.MapGet("/insights", GetQuizInsights).WithName("GetQuizInsights");
    }

    private static async Task<IResult> GetAllQuizzes([AsParameters] QuizServices services, HttpRequest request)
    {
        // Parse pagination parameters
        var pagedRequest = new PagedRequest();
        if (request.Query.TryGetValue("page", out var pageValue) &&
            int.TryParse(pageValue, out var page) && page > 0)
        {
            pagedRequest.Page = page;
        }
        if (request.Query.TryGetValue("pageSize", out var pageSizeValue) &&
            int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0)
        {
            pagedRequest.PageSize = pageSize;
        }

        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var totalCount = await services.QuizRepository.CountAllAsync(CancellationToken.None);
        var quizzes = await services.QuizRepository.GetPagedOrderedByDateAsync(skip, take, CancellationToken.None);
        var quizDtos = QuizMapper.ToDto(quizzes, includeQuestions: false);
        var response = PagedResponse<QuizResponseDto>.Create(pagedRequest, quizDtos, totalCount);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetQuizById(
        [AsParameters] QuizServices services,
        string quizId)
    {
        if (!Guid.TryParse(quizId, out var id))
        {
            throw new ValidationException("quizId", $"Invalid quiz ID format: {quizId}");
        }

        var quiz = await services.QuizService.GetById(id);
        if (quiz == null)
        {
            throw new NotFoundException("QUIZ_NOT_FOUND", "The specified quiz does not exist");
        }

        var response = QuizMapper.ToDto(quiz, includeQuestions: true);
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateQuiz(
        [AsParameters] QuizServices services,
        CreateQuizRequestDto request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        // Check admin permissions
        if (!services.IdentityService.IsAdmin)
        {
            throw new AppException("FORBIDDEN", "Admin access required", 403);
        }

        // Validate quiz structure
        ValidateQuizStructure(request);

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add questions
        var order = 0;
        foreach (var questionRequest in request.Questions)
        {
            var questionId = !string.IsNullOrEmpty(questionRequest.Id) && Guid.TryParse(questionRequest.Id, out var parsedId)
                ? parsedId
                : Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = questionId,
                QuizId = quiz.Id,
                Label = questionRequest.Label,
                InputType = questionRequest.InputType,
                Mandatory = questionRequest.Mandatory,
                Visible = questionRequest.Visible,
                MinValue = questionRequest.MinValue,
                MaxValue = questionRequest.MaxValue,
                Choices = questionRequest.Choices,
                Placeholder = questionRequest.Placeholder,
                Hint = questionRequest.Hint,
                QuestionOrder = order++,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            quiz.Questions.Add(question);
        }

        var savedQuiz = await services.QuizService.Add(quiz);
        var response = QuizMapper.ToDto(savedQuiz, includeQuestions: true);

        return Results.Created($"/quizes/{savedQuiz.Id}", response);
    }

    private static async Task<IResult> UpdateQuiz(
        [AsParameters] QuizServices services,
        string quizId,
        CreateQuizRequestDto request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(quizId, out var id))
        {
            throw new ValidationException("quizId", "Invalid quiz ID format");
        }

        var existingQuiz = await services.QuizService.GetById(id);
        if (existingQuiz == null)
        {
            throw new NotFoundException("QUIZ_NOT_FOUND", "The specified quiz does not exist");
        }

        // Validate quiz structure
        ValidateQuizStructure(request);

        // Store existing question creation dates before clearing
        var existingQuestionDates = existingQuiz.Questions.ToDictionary(q => q.Id, q => q.CreatedAt);

        // Update quiz basic info
        existingQuiz.Title = request.Title;
        existingQuiz.Description = request.Description;
        existingQuiz.UpdatedAt = DateTime.UtcNow;

        // Remove all existing questions
        existingQuiz.Questions.Clear();

        // Add updated questions
        var order = 0;
        foreach (var questionRequest in request.Questions)
        {
            var questionId = !string.IsNullOrEmpty(questionRequest.Id) && Guid.TryParse(questionRequest.Id, out var parsedId)
                ? parsedId
                : Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = questionId,
                QuizId = existingQuiz.Id,
                Label = questionRequest.Label,
                InputType = questionRequest.InputType,
                Mandatory = questionRequest.Mandatory,
                Visible = questionRequest.Visible,
                MinValue = questionRequest.MinValue,
                MaxValue = questionRequest.MaxValue,
                Choices = questionRequest.Choices,
                Placeholder = questionRequest.Placeholder,
                Hint = questionRequest.Hint,
                QuestionOrder = order++,
                CreatedAt = existingQuestionDates.TryGetValue(questionId, out var createdDate)
                    ? createdDate
                    : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            existingQuiz.Questions.Add(question);
        }

        await services.QuizService.Update(existingQuiz);

        // Reload to get properly ordered questions
        var reloadedQuiz = await services.QuizService.GetById(id);
        if (reloadedQuiz == null)
        {
            throw new NotFoundException("QUIZ_NOT_FOUND", "Quiz was not found after update");
        }

        var response = QuizMapper.ToDto(reloadedQuiz, includeQuestions: true);
        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteQuiz(
        [AsParameters] QuizServices services,
        string quizId)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(quizId, out var id))
        {
            throw new ValidationException("quizId", "Invalid quiz ID format");
        }

        var quiz = await services.QuizService.GetById(id);
        if (quiz == null)
        {
            throw new NotFoundException("QUIZ_NOT_FOUND", "The specified quiz does not exist");
        }

        // Check if quiz has responses (optional: prevent deletion)
        var hasResponses = await services.QuizService.HasResponses(id);
        if (hasResponses)
        {
            // For now, we'll allow deletion but log a warning
            services.Logger.LogWarning("Deleting quiz {QuizId} that has existing responses", id);
        }

        await services.QuizService.Delete(id);

        return Results.NoContent();
    }

    private static async Task<IResult> SubmitQuizResponse(
        [AsParameters] QuizServices services,
        string quizId,
        SubmitQuizResponseRequestDto request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(quizId, out var id))
        {
            throw new ValidationException("quizId", "Invalid quiz ID format");
        }

        var quiz = await services.QuizService.GetById(id);
        if (quiz == null)
        {
            throw new NotFoundException("QUIZ_NOT_FOUND", "The specified quiz does not exist");
        }

        // Convert JsonElement values to proper types based on question input types
        var convertedAnswers = ConvertAnswersFromJsonElements(quiz, request.Answers);

        // Validate answers
        await ValidateAnswers(services, quiz, convertedAnswers);

        // Note: We allow multiple responses per user per quiz
        // If you want to restrict to one response, uncomment the following:
        // var existingResponse = await services.QuizResponseService.GetByQuizAndUser(id, userId);
        // if (existingResponse != null)
        // {
        //     throw new AppException("DUPLICATE_RESPONSE", "A response for this quiz already exists", 409);
        // }

        // Calculate score if applicable (simple implementation)
        var score = CalculateScore(quiz, convertedAnswers);

        var completedAt = request.CompletedAt ?? DateTime.UtcNow;

        var response = new QuizResponse
        {
            Id = Guid.NewGuid(),
            QuizId = id,
            UserId = userId,
            AnswersJson = JsonSerializer.Serialize(convertedAnswers),
            Answers = convertedAnswers,
            Score = score,
            CompletedAt = completedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedResponse = await services.QuizResponseService.Add(response);
        var responseDto = QuizResponseMapper.ToDto(savedResponse);

        return Results.Created($"/quizes/{id}/responses/{savedResponse.Id}", responseDto);
    }

    private static async Task<IResult> GetQuizResponses(
        [AsParameters] QuizServices services,
        string quizId)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(quizId, out var id))
        {
            throw new ValidationException("quizId", "Invalid quiz ID format");
        }

        var responses = await services.QuizResponseService.GetByQuizForUser(id, userId);
        var response = QuizResponseMapper.ToDto(responses);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAllQuizResponsesForUser([AsParameters] QuizServices services, HttpRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        // Parse pagination parameters
        var pagedRequest = new PagedRequest();
        if (request.Query.TryGetValue("page", out var pageValue) &&
            int.TryParse(pageValue, out var page) && page > 0)
        {
            pagedRequest.Page = page;
        }
        if (request.Query.TryGetValue("pageSize", out var pageSizeValue) &&
            int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0)
        {
            pagedRequest.PageSize = pageSize;
        }

        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var totalCount = await services.QuizResponseService.CountForUser(userId);
        var responses = await services.QuizResponseRepository.GetPagedForUserAsync(userId, skip, take, CancellationToken.None);
        var responseDtos = QuizResponseMapper.ToDto(responses);
        var response = PagedResponse<QuizResponseResponseDto>.Create(pagedRequest, responseDtos, totalCount);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetQuizResponseById(
        [AsParameters] QuizServices services,
        string responseId)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(responseId, out var id))
        {
            throw new ValidationException("responseId", "Invalid response ID format");
        }

        var response = await services.QuizResponseService.GetById(id, userId);
        if (response == null)
        {
            throw new NotFoundException("QUIZ_RESPONSE_NOT_FOUND", "The specified quiz response does not exist");
        }

        var responseDto = QuizResponseMapper.ToDto(response);
        return Results.Ok(responseDto);
    }

    private static async Task<IResult> GetQuizResponsesCount([AsParameters] QuizServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var count = await services.QuizResponseService.CountForUser(userId);
        return Results.Ok(new { count });
    }

    private static async Task<IResult> DeleteQuizResponse(
        [AsParameters] QuizServices services,
        string responseId)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(responseId, out var id))
        {
            throw new ValidationException("responseId", "Invalid response ID format");
        }

        var deleted = await services.QuizResponseService.Delete(id, userId);
        if (!deleted)
        {
            throw new NotFoundException("QUIZ_RESPONSE_NOT_FOUND", "The specified quiz response does not exist");
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetQuizInsights([AsParameters] QuizServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var responses = await services.QuizResponseService.GetAllForUser(userId);
        var responsesList = responses.ToList();

        if (!responsesList.Any())
        {
            return Results.Ok(new
            {
                summary = "You haven't completed any quizzes yet. Complete quizzes to receive personalized insights.",
                keyInsights = Array.Empty<string>(),
                lastUpdated = DateTime.UtcNow.ToString("O")
            });
        }

        // Generate insights based on responses
        var insights = GenerateInsights(responsesList);

        return Results.Ok(insights);
    }

    // Validation methods
    private static void ValidateQuizStructure(CreateQuizRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("title", new[] { "Quiz title is required" });
        }

        if (request.Questions == null || request.Questions.Count == 0)
        {
            errors.Add("questions", new[] { "Quiz must have at least one question" });
        }

        var validInputTypes = new[] { "text", "number", "email", "date", "choice", "multipleChoice", "scale", "textarea", "tel", "url" };
        var questionOrder = 0;

        foreach (var question in request.Questions ?? new List<CreateQuestionRequestDto>())
        {
            if (string.IsNullOrWhiteSpace(question.Label))
            {
                errors.Add($"questions[{questionOrder}].label", new[] { $"Question {questionOrder + 1} must have a label" });
            }

            if (string.IsNullOrWhiteSpace(question.InputType) || !validInputTypes.Contains(question.InputType))
            {
                errors.Add($"questions[{questionOrder}].inputType", new[] { $"Question {questionOrder + 1} has invalid input type. Valid types: {string.Join(", ", validInputTypes)}" });
            }

            // Validate choices for choice/multipleChoice types
            if ((question.InputType == "choice" || question.InputType == "multipleChoice"))
            {
                if (question.Choices == null || question.Choices.Count == 0)
                {
                    errors.Add($"questions[{questionOrder}].choices", new[] { $"Question {questionOrder + 1} of type {question.InputType} must have choices" });
                }
            }

            // Validate min/max for number/scale types
            if ((question.InputType == "number" || question.InputType == "scale"))
            {
                if (question.MinValue.HasValue && question.MaxValue.HasValue && question.MinValue > question.MaxValue)
                {
                    errors.Add($"questions[{questionOrder}].minValue", new[] { $"Question {questionOrder + 1} has invalid min/max values" });
                }
            }

            questionOrder++;
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static async Task ValidateAnswers(
        QuizServices services,
        Quiz quiz,
        Dictionary<string, object> answers)
    {
        var errors = new Dictionary<string, string[]>();
        var questions = quiz.Questions.Where(q => q.Visible).OrderBy(q => q.QuestionOrder).ToList();

        // Check mandatory questions
        foreach (var question in questions.Where(q => q.Mandatory))
        {
            if (!answers.ContainsKey(question.Id.ToString()) || IsAnswerEmpty(answers[question.Id.ToString()]))
            {
                services.Logger.LogError("Missing mandatory answer for question {QuestionId}", question.Id.ToString());
                errors.Add($"answers[{question.Id}]", new[] { "This question is mandatory and must be answered" });
            }
        }

        // Validate each answer
        foreach (var answer in answers)
        {
            if (!Guid.TryParse(answer.Key, out var questionId))
            {
                continue; // Skip invalid question IDs
            }

            var question = questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null)
            {
                services.Logger.LogError("Question not found: {QuestionId}", questionId.ToString());
                errors.Add($"answers[{answer.Key}]", new[] { "Referenced question doesn't exist in quiz" });
                continue;
            }

            var validationErrors = ValidateAnswer(question, answer.Value);
            if (validationErrors != null && validationErrors.Count > 0)
            {
                foreach (var error in validationErrors)
                {
                    errors.Add($"answers[{answer.Key}]", new[] { error });
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static List<string>? ValidateAnswer(QuizQuestion question, object answer)
    {
        var errors = new List<string>();

        // Check answer type matches input type
        switch (question.InputType)
        {
            case "text":
            case "textarea":
            case "email":
            case "tel":
            case "url":
                if (answer is not string)
                {
                    errors.Add($"Answer type does not match question input type. Expected: string, Provided: {answer.GetType().Name}");
                }
                else
                {
                    // Validate format
                    if (question.InputType == "email" && !IsValidEmail(answer.ToString()!))
                    {
                        errors.Add("Invalid email format");
                    }

                    if (question.InputType == "url" && !IsValidUrl(answer.ToString()!))
                    {
                        errors.Add("Invalid URL format");
                    }
                }
                break;

            case "number":
            case "scale":
                if (!IsNumeric(answer))
                {
                    errors.Add($"Answer type does not match question input type. Expected: number, Provided: {answer.GetType().Name}");
                }
                else
                {
                    var numValue = Convert.ToDecimal(answer);
                    if (question.MinValue.HasValue && numValue < question.MinValue.Value)
                    {
                        errors.Add($"Answer value ({numValue}) is below minimum ({question.MinValue.Value})");
                    }

                    if (question.MaxValue.HasValue && numValue > question.MaxValue.Value)
                    {
                        errors.Add($"Answer value ({numValue}) is above maximum ({question.MaxValue.Value})");
                    }
                }
                break;

            case "date":
                if (answer is not string dateStr || !IsValidDate(dateStr))
                {
                    errors.Add($"Answer type does not match question input type. Expected: date (YYYY-MM-DD), Provided: {answer.GetType().Name}");
                }
                break;

            case "choice":
                if (answer is not string choiceValue)
                {
                    errors.Add($"Answer type does not match question input type. Expected: string, Provided: {answer.GetType().Name}");
                }
                else if (question.Choices == null || !question.Choices.Contains(choiceValue))
                {
                    errors.Add($"Selected choice '{choiceValue}' is not in the allowed choices list");
                }
                break;

            case "multipleChoice":
                if (answer is not JsonElement jsonElement || jsonElement.ValueKind != JsonValueKind.Array)
                {
                    // Try to parse as array
                    if (answer is not List<object> && answer is not string[])
                    {
                        errors.Add($"Answer type does not match question input type. Expected: array, Provided: {answer.GetType().Name}");
                    }
                }

                if (errors.Count == 0)
                {
                    var choices = new List<string>();
                    if (answer is JsonElement je && je.ValueKind == JsonValueKind.Array)
                    {
                        choices = je.EnumerateArray().Select(e => e.GetString()!).ToList();
                    }
                    else if (answer is List<object> list)
                    {
                        choices = list.Select(o => o.ToString()!).ToList();
                    }
                    else if (answer is string[] arr)
                    {
                        choices = arr.ToList();
                    }

                    if (question.Choices == null)
                    {
                        errors.Add("Question does not have choices defined");
                    }
                    else
                    {
                        foreach (var choice in choices)
                        {
                            if (!question.Choices.Contains(choice))
                            {
                                errors.Add($"Selected choice '{choice}' is not in the allowed choices list");
                            }
                        }
                    }
                }
                break;

            default:
                errors.Add($"Unsupported input type: {question.InputType}");
                break;
        }

        return errors.Count > 0 ? errors : null;
    }

    // Helper methods
    private static Dictionary<string, object> ConvertAnswersFromJsonElements(Quiz quiz, Dictionary<string, object> answers)
    {
        var converted = new Dictionary<string, object>();
        var questions = quiz.Questions.ToDictionary(q => q.Id.ToString(), q => q);

        foreach (var answer in answers)
        {
            if (!Guid.TryParse(answer.Key, out var questionId))
            {
                // Keep invalid question IDs as-is
                converted[answer.Key] = answer.Value;
                continue;
            }

            if (!questions.TryGetValue(answer.Key, out var question))
            {
                // Keep answers for questions not found in quiz as-is
                converted[answer.Key] = answer.Value;
                continue;
            }

            // Convert JsonElement to proper type based on question input type
            if (answer.Value is JsonElement jsonElement)
            {
                var convertedValue = ConvertJsonElementToType(jsonElement, question.InputType);
                converted[answer.Key] = convertedValue;
            }
            else
            {
                // Already converted or not a JsonElement
                converted[answer.Key] = answer.Value;
            }
        }

        return converted;
    }

    private static object ConvertJsonElementToType(JsonElement jsonElement, string inputType)
    {
        switch (inputType)
        {
            case "text":
            case "textarea":
            case "email":
            case "tel":
            case "url":
            case "date":
            case "choice":
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString() ?? string.Empty;
                if (jsonElement.ValueKind == JsonValueKind.Null)
                    return string.Empty;
                return jsonElement.ToString();

            case "number":
            case "scale":
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    // Try to preserve the original numeric type
                    if (jsonElement.TryGetInt32(out var intValue))
                        return intValue;
                    if (jsonElement.TryGetInt64(out var longValue))
                        return longValue;
                    if (jsonElement.TryGetDecimal(out var decimalValue))
                        return decimalValue;
                    if (jsonElement.TryGetDouble(out var doubleValue))
                        return doubleValue;
                }
                if (jsonElement.ValueKind == JsonValueKind.String && decimal.TryParse(jsonElement.GetString(), out var parsedDecimal))
                    return parsedDecimal;
                return 0m;

            case "multipleChoice":
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString()! : e.ToString())
                        .ToList();
                }
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    // Try to parse as JSON array string
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<List<string>>(jsonElement.GetString() ?? "[]");
                        return parsed ?? new List<string>();
                    }
                    catch
                    {
                        return new List<string> { jsonElement.GetString() ?? string.Empty };
                    }
                }
                return new List<string>();

            default:
                // For unknown types, convert to string
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString() ?? string.Empty;
                return jsonElement.ToString();
        }
    }

    private static bool IsAnswerEmpty(object? answer)
    {
        if (answer == null)
            return true;

        if (answer is string str)
            return string.IsNullOrWhiteSpace(str);

        if (answer is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.String)
                return string.IsNullOrWhiteSpace(je.GetString());
            if (je.ValueKind == JsonValueKind.Array)
                return je.GetArrayLength() == 0;
            if (je.ValueKind == JsonValueKind.Null)
                return true;
        }

        if (answer is List<object> list)
            return list.Count == 0;

        if (answer is Array arr)
            return arr.Length == 0;

        return false;
    }

    private static bool IsNumeric(object value)
    {
        return value is sbyte || value is byte || value is short || value is ushort ||
               value is int || value is uint || value is long || value is ulong ||
               value is float || value is double || value is decimal;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsValidDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return false;

        return DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _);
    }

    private static decimal? CalculateScore(Quiz quiz, Dictionary<string, object> answers)
    {
        // Simple scoring: sum of scale values, normalized to 0-100
        // This is a basic implementation - can be enhanced
        decimal totalScore = 0;
        int scaleQuestionCount = 0;
        decimal maxPossibleTotal = 0;

        foreach (var question in quiz.Questions.Where(q => q.InputType == "scale" && q.Visible))
        {
            if (answers.TryGetValue(question.Id.ToString(), out var answer))
            {
                if (IsNumeric(answer))
                {
                    totalScore += Convert.ToDecimal(answer);
                    scaleQuestionCount++;
                    maxPossibleTotal += question.MaxValue ?? 5;
                }
            }
        }

        if (scaleQuestionCount == 0 || maxPossibleTotal == 0)
            return null;

        return (totalScore / maxPossibleTotal) * 100;
    }

    private static object GenerateInsights(List<QuizResponse> responses)
    {
        // Simple insights generation - can be enhanced with AI/ML
        var totalQuizzes = responses.Count;
        var averageScore = responses.Where(r => r.Score.HasValue).Select(r => r.Score!.Value).DefaultIfEmpty(0).Average();
        var latestResponse = responses.OrderByDescending(r => r.CompletedAt).First();

        var insights = new List<string>();

        if (averageScore > 70)
        {
            insights.Add("Your quiz scores indicate strong self-awareness and understanding of neurodiversity traits.");
        }
        else if (averageScore > 50)
        {
            insights.Add("Your quiz responses show moderate indicators. Consider exploring further resources.");
        }
        else
        {
            insights.Add("Your responses suggest areas for further exploration and support.");
        }

        insights.Add($"You have completed {totalQuizzes} quiz{(totalQuizzes != 1 ? "s" : "")}.");

        if (totalQuizzes > 1)
        {
            insights.Add("Consider reviewing your progress over time to identify patterns.");
        }

        var summary = $"Based on your {totalQuizzes} quiz response{(totalQuizzes != 1 ? "s" : "")}, " +
                     $"you show indicators that may benefit from further assessment. " +
                     $"Consider consulting with a healthcare professional or coach for personalized guidance.";

        return new
        {
            summary,
            keyInsights = insights,
            lastUpdated = DateTime.UtcNow.ToString("O")
        };
    }
}


