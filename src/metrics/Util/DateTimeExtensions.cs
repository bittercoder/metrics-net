﻿using System;

namespace metrics.graphite
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().ToUniversalTime();

        public static long ToUnixTime(this DateTime self) {
            return (long)(self.ToUniversalTime() - EPOCH).TotalSeconds;
        }
    }
}