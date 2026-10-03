using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NUnitTest")]

namespace EverbloomingLab.KometoroSDK.Game
{
    public abstract class StatBase : IStat
    {
        internal double upperBoundary;
        internal double lowerBoundary;
        protected Action<IStat?>? onValueChanged;

        public StatInfo Info { get; }
        public abstract RawValue Value { get; }

        public double UpperBoundary
        {
            get => upperBoundary;
            set
            {
                var valOld = Value;

                upperBoundary = value < lowerBoundary
                    ? lowerBoundary
                    : value;

                if (!Value.ApproxEquals(valOld)) onValueChanged?.Invoke(this);
            }
        }

        public double LowerBoundary
        {
            get => lowerBoundary;
            set
            {
                var valOld = Value;
                lowerBoundary = value > upperBoundary
                    ? upperBoundary
                    : value;
                if (!Value.ApproxEquals(valOld)) onValueChanged?.Invoke(this);
            }
        }

        internal StatBase()
        {
            upperBoundary = double.MaxValue;
            lowerBoundary = double.MinValue;
        }

        internal StatBase(StatInfo info) : this() => Info = info;
        public abstract event Action<IStat?>? OnValueChanged;

        void IStat.NotifyValueChanged() => onValueChanged?.Invoke(this);

        public void NotifyChanged() => onValueChanged?.Invoke(this);
    }

    public class Stat : StatBase
    {
        public RawValue Raw { get; internal set; }

        public override RawValue Value => Raw.Clamp(LowerBoundary, UpperBoundary);

        public override event Action<IStat?>? OnValueChanged
        {
            add => onValueChanged += value;
            remove => onValueChanged -= value;
        }

        internal Stat() { }
        internal Stat(StatInfo info) : base(info) { }

        public virtual void SetValue(RawValue rawValue)
        {
            var old = Value;
            Raw = rawValue;
            if (Value.ApproxEquals(old)) return;

            var hdlr = onValueChanged;
            hdlr?.Invoke(this);
        }
    }

    public class DynamicStat : StatBase
    {
        internal Func<object?, RawValue>? onRawValue;

        public override event Action<IStat?>? OnValueChanged
        {
            add => onValueChanged += value;
            remove => onValueChanged -= value;
        }

        public override RawValue Value => (onRawValue?.Invoke(null) ?? RawValue._Zero).Clamp(LowerBoundary, UpperBoundary);


        internal DynamicStat() { }
        internal DynamicStat(StatInfo info) : base(info) { }

        public virtual RawValue GetValue(object? obj) => onRawValue?.Invoke(obj).Clamp(LowerBoundary, UpperBoundary) ?? RawValue._Zero;


        public virtual void SetValueFunc(Func<object?, RawValue>? onRawValue)
        {
            this.onRawValue = onRawValue;
            var hdlr = onValueChanged;
            hdlr?.Invoke(this);
        }
    }

    public class ModifiableStat : Stat, IModifiableStat
    {
        internal ModifiableStat()
        {
            Modifier = new StatModifier();
            Modifier.BaseStat = this;
        }

        internal ModifiableStat(StatInfo info) : base(info)
        {
            Modifier = new StatModifier();
            Modifier.BaseStat = this;
        }


        public override RawValue Value => Modifier.GetModifiedValue(Raw).Clamp(LowerBoundary, UpperBoundary);

        public StatModifier Modifier { get; }
    }

    public class ModifiableDynamicStat : DynamicStat, IModifiableStat
    {
        internal ModifiableDynamicStat()
        {
            Modifier = new StatModifier();
            Modifier.BaseStat = this;
        }

        internal ModifiableDynamicStat(StatInfo info) : base(info)
        {
            Modifier = new StatModifier();
            Modifier.BaseStat = this;
        }

        public override RawValue Value => Modifier.GetModifiedValue(onRawValue?.Invoke(null)                ?? RawValue._Zero).Clamp(LowerBoundary, UpperBoundary);
        public override RawValue GetValue(object? obj) => Modifier.GetModifiedValue(onRawValue?.Invoke(obj) ?? RawValue._Zero).Clamp(LowerBoundary, UpperBoundary);

        public StatModifier Modifier { get; }
    }
}