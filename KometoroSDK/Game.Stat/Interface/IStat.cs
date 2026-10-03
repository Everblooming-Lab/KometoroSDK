using System;

namespace EverbloomingLab.KometoroSDK.Game
{
    public interface IStat
    {
        public StatInfo Info { get; }

        public RawValue Value { get; }

        public double UpperBoundary { get; set; }
        public double LowerBoundary { get; set; }

        public event Action<IStat?>? OnValueChanged;

        internal void NotifyValueChanged();
    }

    public interface IModifiableStat : IStat
    {
        public StatModifier Modifier { get; }
    }
}