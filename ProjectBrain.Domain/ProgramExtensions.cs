using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProjectBrain.Domain;

public static class ProgramExtensions
{
    public static void AddProjectBrainDomain(this WebApplicationBuilder builder)
    {
        builder.AddProjectBrainDbContext();

        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IChatService, ChatService>();
        builder.Services.AddScoped<IConversationService, ConversationService>();
        builder.Services.AddScoped<IResourceService, ResourceService>();
    }
}