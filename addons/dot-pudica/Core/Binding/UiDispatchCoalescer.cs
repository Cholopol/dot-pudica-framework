using System;

namespace DotPudica.Core.Binding;

/// <summary>
/// Version-stamp + coalesce: many writers → one consumer channel; keeps the latest snapshot and merges duplicate posts.
/// Interlocked for cross-thread safety; consumers run on the UI thread.
/// </summary>
internal sealed class UiDispatchCoalescer
{
    private long _version;

    public long CurrentVersion => Interlocked.Read(ref _version);

    public void AdvanceVersion() => Interlocked.Increment(ref _version);

    public Channel CreateChannel() => new();

    public sealed class Channel
    {
        internal long _version = -1;
        internal int _scheduled;

        public void Stamp(long version) => Interlocked.Exchange(ref _version, version);

        /// <summary>
        /// CAS: returns false if already stamped with <paramref name="version"/> (skip duplicate schedule);
        /// otherwise writes and returns true. Used by virtualized refresh.
        /// </summary>
        public bool TryStampIfNew(long version)
        {
            while (true)
            {
                var current = Interlocked.Read(ref _version);
                if (current == version)
                    return false;
                if (Interlocked.CompareExchange(ref _version, version, current) == current)
                    return true;
            }
        }

        /// <summary>If not queued, set flag and invoke <paramref name="post"/>; returns whether this was the first post.</summary>
        public bool TryMarkQueued(Action post)
        {
            if (Interlocked.Exchange(ref _scheduled, 1) == 0)
            {
                post();
                return true;
            }
            return false;
        }

        public void ClearScheduled() => Interlocked.Exchange(ref _scheduled, 0);

        public void Clear() => Interlocked.Exchange(ref _version, -1);

        public void ClearIf(long version) => Interlocked.CompareExchange(ref _version, -1, version);

        public long ReadVersion() => Interlocked.Read(ref _version);
    }
}
