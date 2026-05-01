# DNBot

A .NET 10 Discord bot template built on Discord.Net, ASP.NET Core, dependency injection, typed configuration, slash commands, prefix commands, background services, and a local setup dashboard.

## Included Features

- Slash commands: `/ping`, `/server`, `/avatar`, `/remind`
- Utility commands: `/userinfo`
- Polls: `/poll`
- Moderation: `/mod kick`, `/mod ban`, `/mod purge`, `/mod slowmode`
- Tags/custom responses: `/tag create`, `/tag get`, `/tag list`, `/tag delete`
- XP and levels: `/levels rank`, `/levels leaderboard`
- Prefix commands: `!ping`, `!say`
- Autorole for new members, configured from the dashboard
- Per-server prefix overrides and welcome messages
- In-memory reminder queue with a hosted delivery service
- Rotating bot status messages
- Structured console logging
- Optional environment-variable seeding for deployment
- Optional guild-only slash command registration for fast development
- Local web dashboard for setup and customization

## Setup

1. Create a Discord application and bot in the Discord Developer Portal.
2. Enable `MESSAGE CONTENT INTENT` for prefix commands.
3. Enable `SERVER MEMBERS INTENT` for autorole.
4. Invite the bot with `bot` and `applications.commands` scopes.
5. Run the app and use the dashboard as the main setup surface:

```bash
DOTNET_CLI_HOME=.dotnet dotnet run
```

Open:

```text
http://localhost:5080/dashboard
```

Save your token, prefix, development guild, status rotation, and autorole settings there. The dashboard writes to `data/settings.json`, which is the bot's main runtime config. Restart after saving a new token or changing the development guild used for slash command registration.

The server selector in the dashboard scopes server-specific pages. Selecting a server loads that server's prefix override, welcome message settings, autorole rules, assignable roles, level leaderboard, and tags. Global settings stay under Setup.

You can still manually edit environment variables for automation or deployment. Env/appsettings values seed `data/settings.json` only when that file does not exist yet:

```bash
export DNBOT_Discord__Token="your_bot_token"
export DNBOT_Discord__Prefix="!"
export DNBOT_Discord__DevelopmentGuildId="123456789012345678"
export Dashboard__Url="http://localhost:5080"
```

## Run

```bash
DOTNET_CLI_HOME=.dotnet dotnet run
```

The project targets `net10.0`. `DOTNET_CLI_HOME=.dotnet` keeps local CLI first-run files inside the repo, which is handy in sandboxed environments.

## Data Storage

Template data is stored as JSON under `data/`:

- `settings.json`: prefix, development guild id, rotating statuses
- `levels.json`: guild/user XP and levels
- `tags.json`: custom server tags
- autorole settings live in `settings.json` under `autoRoles`
- per-server settings live in `settings.json` under `guildSettings`

The `data/` folder is ignored by git so tokens, server data, and local state do not get committed by accident.

Config precedence is intentionally simple: once `data/settings.json` exists, the dashboard owns bot settings. Delete that file if you want env/appsettings values to seed a fresh local config again.

If a JSON data file is malformed, the bot preserves it with a `.corrupt-*` suffix and recreates clean defaults where possible.

## Expand It

- Add new slash command modules under `Features/<FeatureName>`.
- Add prefix command modules by inheriting `ModuleBase<SocketCommandContext>`.
- Add services with `builder.Services.AddSingleton`, `AddScoped`, or `AddHostedService` in `Program.cs`.
- Replace the JSON stores (`TagStore`, `LevelStore`, `BotSettingsStore`) with database-backed stores when the bot outgrows single-node file storage.
- Replace `ReminderStore` with a persisted store when reminders need to survive restarts.
