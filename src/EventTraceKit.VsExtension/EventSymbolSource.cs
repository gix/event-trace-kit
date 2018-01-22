namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public interface IEventSymbolSource
    {
        string TryGetSymbol(EventKey eventKey);
    }

    public class EventSymbolSource : IEventSymbolSource
    {
        private Dictionary<EventKey, string> symbols = new Dictionary<EventKey, string>();

        public string TryGetSymbol(EventKey eventKey)
        {
            symbols.TryGetValue(eventKey, out var symbol);
            return symbol;
        }

        public void Update(Dictionary<EventKey, string> newSymbols)
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
            if (ReferenceEquals(null, obj)) return false;
            return obj is EventKey && Equals((EventKey)obj);
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
