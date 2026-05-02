namespace DNBot.Features.Tags;

public sealed record Tag(
    ulong GuildId,
    string Name,
    string Content,
    ulong OwnerId,
    DateTimeOffset CreatedAt);