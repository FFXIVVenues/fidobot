namespace Fidobot.Models.DB;

public class DBForum
{
    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }
    public long StartedTimestamp { get; set; }
    public bool EatExisting { get; set; } // Should fido check older than StartTime threads.
    public ulong EatOffset { get; set; } // UNIX Timestamp offset from thread creation date and time
}
