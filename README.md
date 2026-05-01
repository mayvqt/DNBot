# DNBot

A .NET 10 Discord bot template built on Discord.Net, the generic host, dependency injection, typed configuration, slash commands, prefix commands, and background services.

## Included Features

- Slash commands: `/ping`, `/server`, `/avatar`, `/remind`
- Prefix commands: `!ping`, `!say`
- In-memory reminder queue with a hosted delivery service
- Rotating bot status messages
- Structured console logging
- Environment-variable configuration
- Optional guild-only slash command registration for fast development

## Setup

1. Create a Discord application and bot in the Discord Developer Portal.
2. Enable the `MESSAGE CONTENT INTENT` for prefix commands.
3. Invite the bot with `bot` and `applications.commands` scopes.
4. Set your token:

```bash
export DNBOT_Discord__Token="your_bot_token"
```

For quick slash command updates while developing, also set a guild id:

```bash
export DNBOT_Discord__DevelopmentGuildId="123456789012345678"
```

## Run

```bash
DOTNET_CLI_HOME=.dotnet dotnet run
```

The project targets `net10.0`. `DOTNET_CLI_HOME=.dotnet` keeps local CLI first-run files inside the repo, which is handy in sandboxed environments.

## Expand It

- Add new slash command modules under `Features/<FeatureName>`.
- Add prefix command modules by inheriting `ModuleBase<SocketCommandContext>`.
- Add services with `builder.Services.AddSingleton`, `AddScoped`, or `AddHostedService` in `Program.cs`.
- Replace `ReminderStore` with a database-backed store when reminders need to survive restarts.
