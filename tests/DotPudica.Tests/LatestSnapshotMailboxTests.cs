using DotPudica.Core.Threading;

namespace DotPudica.Tests;

public class LatestSnapshotMailboxTests
{
    [Fact]
    public void Publish_ThenDrain_ReturnsLatestOnly()
    {
        var mailbox = new LatestSnapshotMailbox<string>();
        mailbox.Publish("a");
        mailbox.Publish("b");
        mailbox.Publish("c");

        Assert.True(mailbox.TryDrainLatest(out var snapshot));
        Assert.Equal("c", snapshot);
        Assert.False(mailbox.TryDrainLatest(out _));
    }

    [Fact]
    public void TryDrainLatest_Empty_ReturnsFalse()
    {
        var mailbox = new LatestSnapshotMailbox<int>();
        Assert.False(mailbox.TryDrainLatest(out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Publish_Null_Throws()
    {
        var mailbox = new LatestSnapshotMailbox<string>();
        Assert.Throws<ArgumentNullException>(() => mailbox.Publish(null!));
    }

    [Fact]
    public void ConcurrentPublish_DrainSeesOneOfPublished()
    {
        var mailbox = new LatestSnapshotMailbox<string>();
        Parallel.For(0, 200, i => mailbox.Publish($"v{i}"));

        Assert.True(mailbox.TryDrainLatest(out var snapshot));
        Assert.StartsWith("v", snapshot);
        Assert.False(mailbox.TryDrainLatest(out _));
    }
}
