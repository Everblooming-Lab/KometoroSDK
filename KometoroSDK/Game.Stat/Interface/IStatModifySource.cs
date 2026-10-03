using System;

namespace EverbloomingLab.KometoroSDK.Game
{
    public interface IStatModifySource
    {
        public StatModifyInfo StatModifyInfo { get; }

        public event Action<IStatModifySource> OnSourceChanged;

        public bool EnableModification => StatModifyInfo.EnableModification;

        public bool PercentageModify => StatModifyInfo.PercentageModify;

        public bool Persistent => StatModifyInfo.Persistent;

        public double GetModifiedDeltaValue();
    }
}