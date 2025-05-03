// Adapter/SystemClock.cs
using System;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new();
        private SystemClock() { }
        public DateTime UtcNow => DateTime.UtcNow;
    }
}