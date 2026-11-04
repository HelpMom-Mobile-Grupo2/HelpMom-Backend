using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Companion;

public sealed class DailyTip : Entity
{
    public const int FirstWeek = 1;
    public const int LastWeek = 42;

    private DailyTip()
    {
        Title = string.Empty;
        Body = string.Empty;
    }

    private DailyTip(Guid id, string title, string body, int fromWeek, int toWeek)
        : base(id)
    {
        Title = title;
        Body = body;
        FromWeek = fromWeek;
        ToWeek = toWeek;
    }

    public string Title { get; private set; }
    public string Body { get; private set; }
    public int FromWeek { get; private set; }
    public int ToWeek { get; private set; }

    // Tips are catalogue data seeded with stable ids, so the id is supplied by the caller.
    public static DailyTip Create(Guid id, string title, string body, int fromWeek, int toWeek)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        if (fromWeek < FirstWeek || toWeek > LastWeek || fromWeek > toWeek)
        {
            throw new ArgumentOutOfRangeException(nameof(fromWeek), $"Weeks must be within {FirstWeek} and {LastWeek}.");
        }

        return new DailyTip(id, title.Trim(), body.Trim(), fromWeek, toWeek);
    }

    public bool AppliesTo(int week) => week >= FromWeek && week <= ToWeek;
}
