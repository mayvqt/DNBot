namespace DNBot.Features.Levels;

public sealed record LevelProfile(ulong GuildId, ulong UserId, int Xp, int Level)
{
    public int XpForNextLevel => LevelStore.XpRequiredForLevel(Level + 1);
}