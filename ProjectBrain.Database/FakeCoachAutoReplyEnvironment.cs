using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProjectBrain.Database;

[Obsolete("Use FakeCoachEnvironment instead.")]
public static class FakeCoachAutoReplyEnvironment
{
    public const string DefaultMessage = FakeCoachEnvironment.DefaultMessage;

    public static bool IsEnabled(IConfiguration configuration, IHostEnvironment? environment = null) =>
        FakeCoachEnvironment.IsEnabled(configuration);
}
