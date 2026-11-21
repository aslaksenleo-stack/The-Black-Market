using System;

namespace BlackMarket.Core
{
    /// <summary>
    /// Shop opening hours. Time moves as visitors come and go (and slowly in real time), and the shop
    /// closes at ClosingMinute; the day's results are shown before the next day starts.
    /// </summary>
    public sealed class DayClock
    {
        public const int OpeningMinute = 9 * 60;
        public const int ClosingMinute = 18 * 60;

        public DayClock(int day = 1)
        {
            Day = Math.Max(1, day);
            MinuteOfDay = OpeningMinute;
        }

        public int Day { get; private set; }

        /// <summary>Minutes since midnight.</summary>
        public int MinuteOfDay { get; private set; }

        public bool IsClosingTime => MinuteOfDay >= ClosingMinute;

        public event Action Changed;
        public event Action<int> DayStarted;

        public void Advance(int minutes)
        {
            if (minutes <= 0) return;
            MinuteOfDay = Math.Min(ClosingMinute, MinuteOfDay + minutes);
            Changed?.Invoke();
        }

        public void StartNextDay()
        {
            Day++;
            MinuteOfDay = OpeningMinute;
            Changed?.Invoke();
            DayStarted?.Invoke(Day);
        }

        /// <summary>"10:24".</summary>
        public string TimeText => $"{MinuteOfDay / 60:00}:{MinuteOfDay % 60:00}";
    }
}
