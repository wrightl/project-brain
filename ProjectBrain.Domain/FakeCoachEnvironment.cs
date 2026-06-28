using Microsoft.Extensions.Configuration;

namespace ProjectBrain.Domain;

public static class FakeCoachEnvironment
{
    public const string DefaultMessage =
        "Thank you for getting in touch, let's schedule a call together. Here's my calendly link for a 15 minute call to discuss further.";

    public static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("FakeCoachAutoReply:Enabled");
}
