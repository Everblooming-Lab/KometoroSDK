using System;

namespace EverbloomingLab.KometoroSDK.Game
{
    public class SourceStatPack : IStatModifySource
    {
        public readonly IStat SourceStat;
        public StatModifyInfo StatModifyInfo { get; set; }
        public event Action<IStatModifySource>? OnSourceChanged;

        internal SourceStatPack(IStat source, StatModifyInfo modifyInfo)
        {
            SourceStat = source;
            StatModifyInfo = modifyInfo;

            SourceStat.OnValueChanged += s_LinkEvent;
        }

        public double GetModifiedDeltaValue() => SourceStat.Value;

        private void s_LinkEvent(IStat? _) => OnSourceChanged?.Invoke(this);

        public void DestroyPack()
        {
            SourceStat.OnValueChanged -= s_LinkEvent;
        }
    }
}