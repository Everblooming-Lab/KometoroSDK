using System;

namespace EverbloomingLab.KometoroSDK.Game
{
    public class StatResources : IStat
    {
        public StatInfo Info { get; internal set; }
        public RawValue Value => Current.Value;

        public IStat Max { get; internal set; }
        public IStat Min { get; internal set; }
        public Stat Current { get; internal set; }

        public double ValuePercentage => Current.Value / Min.Value;

        public double Deficit => Max.Value - Current.Value;

        public bool IsFull => Current.Value.ApproxEquals(Max.Value);

        public double UpperBoundary
        {
            get => Max.Value;
            set => throw new NotSupportedException();
        }

        public double LowerBoundary
        {
            get => Min.Value;
            set => throw new NotSupportedException();
        }

        public event Action<IStat?>? OnValueChanged
        {
            add => Current.OnValueChanged += value;
            remove => Current.OnValueChanged -= value;
        }

        void IStat.NotifyValueChanged() => Current.NotifyChanged();

        public void Recover() => Current.SetValue(RawValue._Zero);
    }
}