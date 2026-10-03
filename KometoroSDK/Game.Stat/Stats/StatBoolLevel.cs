using System;

namespace EverbloomingLab.KometoroSDK.Game
{
    public class StatBoolLevel : IStat
    {
        private IStat stat;

        public bool Success => Value.IsPositive;
        public int Level => stat.Value.AsInt;
        public StatInfo Info => stat.Info;
        public RawValue Value => stat.Value;

        public double UpperBoundary
        {
            get => stat.UpperBoundary;
            set => stat.UpperBoundary = value;
        }

        public double LowerBoundary
        {
            get => stat.LowerBoundary;
            set => stat.LowerBoundary = value;
        }

        public event Action<IStat?>? OnValueChanged
        {
            add => stat.OnValueChanged += value;
            remove => stat.OnValueChanged -= value;
        }

        void IStat.NotifyValueChanged()
        {
            stat.NotifyValueChanged();
        }

        internal StatBoolLevel(IStat stat) => this.stat = stat;
    }
}