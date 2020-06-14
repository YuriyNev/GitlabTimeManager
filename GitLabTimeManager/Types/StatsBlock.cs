using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Models
{
    public class StatsBlock : NotifyObject
    {
        public string Title { get; }
        public string Description { get; }

        private double _value;
        public double Value
        {
            get => _value;
            private set
            {
                if (value.Equals(_value)) return;
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        private double _total;
        public double Total
        {
            get => _total;
            private set
            {
                if (value.Equals(_total)) return;
                _total = value;
                OnPropertyChanged(nameof(Total));
            }
        }

        public StatsBlock(string title, double value, double total, string description = null)
        {
            Value = value;
            Total = total;
            Title = title;
            Description = description;
        }

        public void Update(double value, double total)
        {
            Value = value;
            Total = total;
        }

    }
}
