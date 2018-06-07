using System;

namespace discordbot
{
    public static class DateUtils
    {
        public static ulong DateTimeToEpoch(DateTime dateTime)
        {
            return (ulong)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}