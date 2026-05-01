using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DNBot.Configuration;
using DNBot.Features.Reminders;
using DNBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "DNBOT_");

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services
    .AddOptions<DiscordBotOptions>()
    .Bind(builder.Configuration.GetSection(DiscordBotOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Token), "Discord:Token is required.")
    .Validate(options => options.Prefix.Length is > 0 and <= 8, "Discord:Prefix must be 1-8 characters.")
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<DiscordBotOptions>>().Value;

    return new DiscordSocketClient(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildMessages
            | GatewayIntents.DirectMessages
            | GatewayIntents.MessageContent,
        AlwaysDownloadUsers = false,
        LogGatewayIntentWarnings = true,
        LogLevel = options.DiscordNetLogLevel
    });
});

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<DiscordSocketClient>();
    var options = sp.GetRequiredService<IOptions<DiscordBotOptions>>().Value;

    return new InteractionService(client.Rest, new InteractionServiceConfig
    {
        DefaultRunMode = Discord.Interactions.RunMode.Async,
        LogLevel = options.DiscordNetLogLevel,
        UseCompiledLambda = true
    });
});

builder.Services.AddSingleton(_ => new CommandService(new CommandServiceConfig
{
    CaseSensitiveCommands = false,
    DefaultRunMode = Discord.Commands.RunMode.Async,
    LogLevel = LogSeverity.Info
}));

builder.Services.AddSingleton<ReminderStore>();
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<ReminderDeliveryService>();
builder.Services.AddHostedService<StatusRotationService>();

await builder.Build().RunAsync();
