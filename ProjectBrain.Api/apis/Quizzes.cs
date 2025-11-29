using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class QuizServices(
    ILogger<QuizServices> logger,
    IQuizService quizService,
    IQuizResponseService quizResponseService,
    IIdentityService identityService)
{
    public ILogger<QuizServices> Logger { get; } = logger;
    public IQuizService QuizService { get; } = quizService;
    public IQuizResponseService QuizResponseService { get; } = quizResponseService;
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
        group.MapGet("/insights", GetQuizInsights).WithName("GetQuizInsights");
    }

    private static async Task<IResult> GetAllQuizzes([AsParameters] QuizServices services)
    {
        try
        {
            var quizzes = await services.QuizService.GetAll();
            var response = quizzes.Select(q => new
            {
                id = q.Id.ToString(),
                title = q.Title,
                description = q.Description,
                createdAt = q.CreatedAt.ToString("O"),
                updatedAt = q.UpdatedAt.ToString("O")
            });

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving quizzes");
            return Results.Problem("An error occurred while retrieving quizzes.");
        }
    }

    private static async Task<IResult> GetQuizById(
        [AsParameters] QuizServices services,
        string quizId)
    {
        try
        {
            if (!Guid.TryParse(quizId, out var id))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_ID",
                        message = "Invalid quiz ID format: " + quizId
                    }
                });
            }

            var quiz = await services.QuizService.GetById(id);
            if (quiz == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "QUIZ_NOT_FOUND",
                        message = "The specified quiz does not exist"
                    }
                });
            }

            var response = new
            {
                id = quiz.Id.ToString(),
                title = quiz.Title,
                description = quiz.Description,
                questions = quiz.Questions.Select(q => new
                {
                    id = q.Id.ToString(),
                    label = q.Label,
                    inputType = q.InputType,
                    mandatory = q.Mandatory,
                    visible = q.Visible,
                    minValue = q.MinValue,
                    maxValue = q.MaxValue,
                    choices = q.Choices,
                    placeholder = q.Placeholder,
                    hint = q.Hint
                }),
                createdAt = quiz.CreatedAt.ToString("O"),
                updatedAt = quiz.UpdatedAt.ToString("O")
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving quiz {QuizId}", quizId);
            return Results.Problem("An error occurred while retrieving the quiz.");
        }
    }

    private static async Task<IResult> CreateQuiz(
        [AsParameters] QuizServices services,
        CreateQuizRequest request)
    {
        var userId = services.IdentityService.UserId!;

        // Check admin permissions
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        try
        {
            // Validate quiz structure
            var validationResult = ValidateQuizStructure(request);
            if (validationResult != null)
            {
                return Results.BadRequest(validationResult);
            }

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
                var question = new QuizQuestion
                {
                    Id = questionRequest.Id.HasValue ? questionRequest.Id.Value : Guid.NewGuid(),
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

            var response = new
            {
                id = savedQuiz.Id.ToString(),
                title = savedQuiz.Title,
                description = savedQuiz.Description,
                questions = savedQuiz.Questions.Select(q => new
                {
                    id = q.Id.ToString(),
                    label = q.Label,
                    inputType = q.InputType,
                    mandatory = q.Mandatory,
                    visible = q.Visible,
                    minValue = q.MinValue,
                    maxValue = q.MaxValue,
                    choices = q.Choices,
                    placeholder = q.Placeholder,
                    hint = q.Hint
                }),
                createdAt = savedQuiz.CreatedAt.ToString("O"),
                updatedAt = savedQuiz.UpdatedAt.ToString("O")
            };

            return Results.Created($"/quizes/{savedQuiz.Id}", response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating quiz for user {UserId}", userId);
            return Results.Problem("An error occurred while creating the quiz.");
        }
    }

    private static async Task<IResult> UpdateQuiz(
        [AsParameters] QuizServices services,
        string quizId,
        CreateQuizRequest request)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            if (!Guid.TryParse(quizId, out var id))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_ID",
                        message = "Invalid quiz ID format"
                    }
                });
            }

            var existingQuiz = await services.QuizService.GetById(id);
            if (existingQuiz == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "QUIZ_NOT_FOUND",
                        message = "The specified quiz does not exist"
                    }
                });
            }

            // Validate quiz structure
            var validationResult = ValidateQuizStructure(request);
            if (validationResult != null)
            {
                return Results.BadRequest(validationResult);
            }

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
                var questionId = questionRequest.Id.HasValue ? questionRequest.Id.Value : Guid.NewGuid();
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

            var updatedQuiz = await services.QuizService.Update(existingQuiz);

            // Reload to get properly ordered questions
            var reloadedQuiz = await services.QuizService.GetById(id);

            var response = new
            {
                id = reloadedQuiz!.Id.ToString(),
                title = reloadedQuiz.Title,
                description = reloadedQuiz.Description,
                questions = reloadedQuiz.Questions.Select(q => new
                {
                    id = q.Id.ToString(),
                    label = q.Label,
                    inputType = q.InputType,
                    mandatory = q.Mandatory,
                    visible = q.Visible,
                    minValue = q.MinValue,
                    maxValue = q.MaxValue,
                    choices = q.Choices,
                    placeholder = q.Placeholder,
                    hint = q.Hint
                }),
                createdAt = reloadedQuiz.CreatedAt.ToString("O"),
                updatedAt = reloadedQuiz.UpdatedAt.ToString("O")
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error updating quiz {QuizId} for user {UserId}", quizId, userId);
            return Results.Problem("An error occurred while updating the quiz.");
        }
    }

    private static async Task<IResult> DeleteQuiz(
        [AsParameters] QuizServices services,
        string quizId)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            if (!Guid.TryParse(quizId, out var id))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_ID",
                        message = "Invalid quiz ID format"
                    }
                });
            }

            var quiz = await services.QuizService.GetById(id);
            if (quiz == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "QUIZ_NOT_FOUND",
                        message = "The specified quiz does not exist"
                    }
                });
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
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting quiz {QuizId} for user {UserId}", quizId, userId);
            return Results.Problem("An error occurred while deleting the quiz.");
        }
    }

    private static async Task<IResult> SubmitQuizResponse(
        [AsParameters] QuizServices services,
        string quizId,
        SubmitQuizResponseRequest request)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            if (!Guid.TryParse(quizId, out var id))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_ID",
                        message = "Invalid quiz ID format"
                    }
                });
            }

            var quiz = await services.QuizService.GetById(id);
            if (quiz == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "QUIZ_NOT_FOUND",
                        message = "The specified quiz does not exist"
                    }
                });
            }

            // Convert JsonElement values to proper types based on question input types
            var convertedAnswers = ConvertAnswersFromJsonElements(quiz, request.Answers);

            // Validate answers
            var validationResult = await ValidateAnswers(services, quiz, convertedAnswers);
            if (validationResult != null)
            {
                services.Logger.LogError("Validation result: {ValidationResult}", JsonSerializer.Serialize(validationResult));
                return Results.BadRequest(validationResult);
            }

            // Note: We allow multiple responses per user per quiz
            // If you want to restrict to one response, uncomment the following:
            // var existingResponse = await services.QuizResponseService.GetByQuizAndUser(id, userId);
            // if (existingResponse != null)
            // {
            //     return Results.Conflict(new
            //     {
            //         error = new
            //         {
            //             code = "DUPLICATE_RESPONSE",
            //             message = "A response for this quiz already exists"
            //         }
            //     });
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

            var responseObj = new
            {
                id = savedResponse.Id.ToString(),
                quizId = savedResponse.QuizId.ToString(),
                userId = savedResponse.UserId,
                answers = savedResponse.Answers,
                score = savedResponse.Score,
                completedAt = savedResponse.CompletedAt.ToString("O"),
                createdAt = savedResponse.CreatedAt.ToString("O")
            };

            return Results.Created($"/quizes/{id}/responses/{savedResponse.Id}", responseObj);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error submitting quiz response for quiz {QuizId} and user {UserId}", quizId, userId);
            return Results.Problem("An error occurred while submitting the quiz response.");
        }
    }

    private static async Task<IResult> GetQuizResponses(
        [AsParameters] QuizServices services,
        string quizId)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            if (!Guid.TryParse(quizId, out var id))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_ID",
                        message = "Invalid quiz ID format"
                    }
                });
            }

            var responses = await services.QuizResponseService.GetByQuizForUser(id, userId);
            var response = responses.Select(r => new
            {
                id = r.Id.ToString(),
                quizId = r.QuizId.ToString(),
                quizTitle = r.Quiz?.Title,
                completedAt = r.CompletedAt.ToString("O"),
                score = r.Score
            });

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving quiz responses for quiz {QuizId} and user {UserId}", quizId, userId);
            return Results.Problem("An error occurred while retrieving quiz responses.");
        }
    }

    private static async Task<IResult> GetAllQuizResponsesForUser([AsParameters] QuizServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var responses = await services.QuizResponseService.GetAllForUser(userId);
            var response = responses.Select(r => new
            {
                id = r.Id.ToString(),
                quizId = r.QuizId.ToString(),
                quizTitle = r.Quiz?.Title,
                completedAt = r.CompletedAt.ToString("O"),
                score = r.Score
            });
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving all quiz responses for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving all quiz responses.");
        }
    }

    private static async Task<IResult> GetQuizInsights([AsParameters] QuizServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
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
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving quiz insights for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving quiz insights.");
        }
    }

    // Validation methods
    private static object? ValidateQuizStructure(CreateQuizRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return new
            {
                error = new
                {
                    code = "INVALID_QUIZ_DATA",
                    message = "Quiz title is required"
                }
            };
        }

        if (request.Questions == null || request.Questions.Count == 0)
        {
            return new
            {
                error = new
                {
                    code = "INVALID_QUIZ_DATA",
                    message = "Quiz must have at least one question"
                }
            };
        }

        var validInputTypes = new[] { "text", "number", "email", "date", "choice", "multipleChoice", "scale", "textarea", "tel", "url" };
        var questionOrder = 0;

        foreach (var question in request.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.Label))
            {
                return new
                {
                    error = new
                    {
                        code = "INVALID_QUESTION_DATA",
                        message = $"Question {questionOrder + 1} must have a label"
                    }
                };
            }

            if (string.IsNullOrWhiteSpace(question.InputType) || !validInputTypes.Contains(question.InputType))
            {
                return new
                {
                    error = new
                    {
                        code = "INVALID_QUESTION_DATA",
                        message = $"Question {questionOrder + 1} has invalid input type. Valid types: {string.Join(", ", validInputTypes)}"
                    }
                };
            }

            // Validate choices for choice/multipleChoice types
            if ((question.InputType == "choice" || question.InputType == "multipleChoice"))
            {
                if (question.Choices == null || question.Choices.Count == 0)
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_QUESTION_DATA",
                            message = $"Question {questionOrder + 1} of type {question.InputType} must have choices"
                        }
                    };
                }
            }

            // Validate min/max for number/scale types
            if ((question.InputType == "number" || question.InputType == "scale"))
            {
                if (question.MinValue.HasValue && question.MaxValue.HasValue && question.MinValue > question.MaxValue)
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_QUESTION_DATA",
                            message = $"Question {questionOrder + 1} has invalid min/max values"
                        }
                    };
                }
            }

            questionOrder++;
        }

        return null;
    }

    private static async Task<object?> ValidateAnswers(
        QuizServices services,
        Quiz quiz,
        Dictionary<string, object> answers)
    {
        var questions = quiz.Questions.Where(q => q.Visible).OrderBy(q => q.QuestionOrder).ToList();

        // Check mandatory questions
        foreach (var question in questions.Where(q => q.Mandatory))
        {
            if (!answers.ContainsKey(question.Id.ToString()) || IsAnswerEmpty(answers[question.Id.ToString()]))
            {
                services.Logger.LogError("Missing mandatory answer for question {QuestionId}", question.Id.ToString());
                return new
                {
                    error = new
                    {
                        code = "MISSING_MANDATORY_ANSWER",
                        message = "Required questions must be answered",
                        details = new
                        {
                            questionId = question.Id.ToString(),
                            reason = "This question is mandatory"
                        }
                    }
                };
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
                return new
                {
                    error = new
                    {
                        code = "QUESTION_NOT_FOUND",
                        message = "Referenced question doesn't exist in quiz",
                        details = new
                        {
                            questionId = answer.Key
                        }
                    }
                };
            }

            var validationError = ValidateAnswer(question, answer.Value);
            if (validationError != null)
            {
                services.Logger.LogError("Validation error: {ValidationError}", JsonSerializer.Serialize(validationError));
                return validationError;
            }
        }

        return null;
    }

    private static object? ValidateAnswer(QuizQuestion question, object answer)
    {
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
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_ANSWER_TYPE",
                            message = "Answer type does not match question input type",
                            details = new
                            {
                                questionId = question.Id.ToString(),
                                expectedType = "string",
                                providedType = answer.GetType().Name
                            }
                        }
                    };
                }

                // Validate format
                if (question.InputType == "email" && !IsValidEmail(answer.ToString()!))
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_ANSWER_FORMAT",
                            message = "Invalid email format",
                            details = new
                            {
                                questionId = question.Id.ToString()
                            }
                        }
                    };
                }

                if (question.InputType == "url" && !IsValidUrl(answer.ToString()!))
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_ANSWER_FORMAT",
                            message = "Invalid URL format",
                            details = new
                            {
                                questionId = question.Id.ToString()
                            }
                        }
                    };
                }
                break;

            case "number":
            case "scale":
                if (!IsNumeric(answer))
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_ANSWER_TYPE",
                            message = "Answer type does not match question input type",
                            details = new
                            {
                                questionId = question.Id.ToString(),
                                expectedType = "number",
                                providedType = answer.GetType().Name
                            }
                        }
                    };
                }

                var numValue = Convert.ToDecimal(answer);
                if (question.MinValue.HasValue && numValue < question.MinValue.Value)
                {
                    return new
                    {
                        error = new
                        {
                            code = "VALUE_OUT_OF_RANGE",
                            message = "Answer value is below minimum",
                            details = new
                            {
                                questionId = question.Id.ToString(),
                                minValue = question.MinValue.Value,
                                providedValue = numValue
                            }
                        }
                    };
                }

                if (question.MaxValue.HasValue && numValue > question.MaxValue.Value)
                {
                    return new
                    {
                        error = new
                        {
                            code = "VALUE_OUT_OF_RANGE",
                            message = "Answer value is above maximum",
                            details = new
                            {
                                questionId = question.Id.ToString(),
                                maxValue = question.MaxValue.Value,
                                providedValue = numValue
                            }
                        }
                    };
                }
                break;

            case "date":
                if (answer is not string dateStr || !IsValidDate(dateStr))
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_ANSWER_TYPE",
                            message = "Answer type does not match question input type",
                            details = new
                            {
                                questionId = question.Id.ToString(),
                                expectedType = "date (YYYY-MM-DD)",
                                providedType = answer.GetType().Name
                            }
                        }
                    };
                }
                break;

            case "choice":
                if (answer is not string choiceValue)
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_ANSWER_TYPE",
                            message = "Answer type does not match question input type",
                            details = new
                            {
                                questionId = question.Id.ToString(),
                                expectedType = "string",
                                providedType = answer.GetType().Name
                            }
                        }
                    };
                }

                if (question.Choices == null || !question.Choices.Contains(choiceValue))
                {
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_CHOICE",
                            message = "Selected choice is not in the allowed choices list",
                            details = new
                            {
                                questionId = question.Id.ToString(),
                                providedChoice = choiceValue
                            }
                        }
                    };
                }
                break;

            case "multipleChoice":
                if (answer is not JsonElement jsonElement || jsonElement.ValueKind != JsonValueKind.Array)
                {
                    // Try to parse as array
                    if (answer is not List<object> && answer is not string[])
                    {
                        return new
                        {
                            error = new
                            {
                                code = "INVALID_ANSWER_TYPE",
                                message = "Answer type does not match question input type",
                                details = new
                                {
                                    questionId = question.Id.ToString(),
                                    expectedType = "array",
                                    providedType = answer.GetType().Name
                                }
                            }
                        };
                    }
                }

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
                    return new
                    {
                        error = new
                        {
                            code = "INVALID_QUESTION_DATA",
                            message = "Question does not have choices defined"
                        }
                    };
                }

                foreach (var choice in choices)
                {
                    if (!question.Choices.Contains(choice))
                    {
                        return new
                        {
                            error = new
                            {
                                code = "INVALID_CHOICE",
                                message = "Selected choice is not in the allowed choices list",
                                details = new
                                {
                                    questionId = question.Id.ToString(),
                                    providedChoice = choice
                                }
                            }
                        };
                    }
                }
                break;

            default:
                return new
                {
                    error = new
                    {
                        code = "INVALID_INPUT_TYPE",
                        message = $"Unsupported input type: {question.InputType}"
                    }
                };
        }

        return null;
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

// Request/Response DTOs
public class CreateQuizRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required List<CreateQuestionRequest> Questions { get; init; }
}

public class CreateQuestionRequest
{
    public Guid? Id { get; init; }
    public required string Label { get; init; }
    public required string InputType { get; init; }
    public bool Mandatory { get; init; } = false;
    public bool Visible { get; init; } = true;
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public List<string>? Choices { get; init; }
    public string? Placeholder { get; init; }
    public string? Hint { get; init; }
}

public class SubmitQuizResponseRequest
{
    public required Dictionary<string, object> Answers { get; init; }
    public DateTime? CompletedAt { get; init; }
}

