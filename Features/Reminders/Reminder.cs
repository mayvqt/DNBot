namespace DNBot.Features.Reminders;

public sealed record Reminder(
    long Id,
    ulong ChannelId,
    ulong UserId,
    DateTimeOffset DueAt,
    string Text);
