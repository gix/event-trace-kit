namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public interface IEventSymbolSource
    {
        string TryGetSymbol(EventKey eventKey);
    }

    public class EventSymbolSource : IEventSymbolSource
    {
        private ConcurrentDictionary<EventKey, string> symbols = new ConcurrentDictionary<EventKey, string>();

        public string TryGetSymbol(EventKey eventKey)
        {
            symbols.TryGetValue(eventKey, out var symbol);
            return symbol;
        }

        public void Update(ConcurrentDictionary<EventKey, string> newSymbols)
        {
            Interlocked.Exchange(ref symbols, newSymbols);
        }
    }

    public struct EventKey : IEquatable<EventKey>
    {
        public EventKey(Guid providerId, ushort id, byte version)
        {
            ProviderId = providerId;
            EventIdAndVersion = (uint)(id << 16) | version;
        }

        public Guid ProviderId { get; }
        public uint EventIdAndVersion { get; }

        public bool Equals(EventKey other)
        {
            return ProviderId.Equals(other.ProviderId) && EventIdAndVersion == other.EventIdAndVersion;
        }

        public override bool Equals(object obj)
        {
            return obj is EventKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            unchecked {
                return (ProviderId.GetHashCode() * 397) ^ (int)EventIdAndVersion;
            }
        }

        public static bool operator ==(EventKey left, EventKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventKey left, EventKey right)
        {
            return !left.Equals(right);
        }
    }
}
