using System;

namespace GitLabTimeManager.Services
{
    public class MoneyCalculator : IMoneyCalculator
    {
        private DevelopLevel DevelopLevel { get; } = DevelopLevel.Middle;
        public double MinimalEarning { get; } = 14_000;
        public double DesiredEstimate { get; } = 100;

        public double Calculate(TimeSpan estimates)
        {
            //=(70 +(A6-100))*1000
            var baseEarning = DevelopLevel switch
            {
                DevelopLevel.Junior => 40_000,
                DevelopLevel.Middle => 70_000,
                DevelopLevel.Senior => 100_000,
                _ => 0
            };
            var earning = baseEarning + (estimates.TotalHours - DesiredEstimate) * 1000;
            earning = Math.Max(earning, MinimalEarning);
            return earning;
        }
    }

    public interface IMoneyCalculator
    {
        double DesiredEstimate { get; }
        double Calculate(TimeSpan estimates);
        double MinimalEarning { get; }
    }

    public enum DevelopLevel : byte
    {
        Junior, Middle, Senior
    }
}
