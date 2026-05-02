using System.Collections.Concurrent;

namespace DNBot.Features.Reminders;

public sealed class ReminderStore
{
    private readonly ConcurrentDictionary<long, Reminder> _reminders = [];
    private long _nextId;

    public Reminder Add(ulong channelId, ulong userId, DateTimeOffset dueAt, string text)
    {
        var id = Interlocked.Increment(ref _nextId);
        var reminder = new Reminder(id, channelId, userId, dueAt, text);
        _reminders[id] = reminder;
        return reminder;
    }

    public IReadOnlyList<Reminder> TakeDue(DateTimeOffset now)
    {
        var due = _reminders.Values
            .Where(reminder => reminder.DueAt <= now)
            .OrderBy(reminder => reminder.DueAt)
            .ToArray();

        foreach (var reminder in due) _reminders.TryRemove(reminder.Id, out _);

        return due;
    }
}