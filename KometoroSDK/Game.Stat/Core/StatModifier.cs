using System;
using System.Collections.Generic;
using System.Linq;

namespace EverbloomingLab.KometoroSDK.Game
{
    [Serializable]
    public class StatModifier
    {
        protected readonly List<IStatModifySource> sources = new List<IStatModifySource>();

        public IStatModifySource this[int index] => sources[index];

        internal IStat BaseStat { get; set; }

        public RawValue GetModifiedValue(RawValue raw)
        {
            var inst = 0d;
            var per = 0d;

            foreach (var source in sources)
            {
                if (!source.EnableModification) continue;
                var sourceDelta = source.GetModifiedDeltaValue();
                if (source.PercentageModify)
                    per += sourceDelta;
                else
                    inst += sourceDelta;
            }

            return (RawValue)((raw + inst) * (1 + per));
        }

        public void AddSource(IStatModifySource source)
        {
            sources.Add(source);
            source.OnSourceChanged += s_NotifyBaseStatValChanged;
        }

        public void AddUniqueSource(IStatModifySource source)
        {
            if (sources.Contains(source)) return;
            sources.Add(source);
            source.OnSourceChanged += s_NotifyBaseStatValChanged;
        }

        public void RemoveSource(IStatModifySource source, bool removeAllDuplicates = false)
        {
            var count = removeAllDuplicates
                ? sources.RemoveAll(i => i == source)
                : sources.Remove(source) // 只移动一个
                    ? 1                  //移除了
                    : 0;                 // 没有

            for (var i = 0; i < count; i++)
            {
                source.OnSourceChanged -= s_NotifyBaseStatValChanged;
            }

            if (count != 0) BaseStat.NotifyValueChanged();
        }

        public void ClearAllSources(bool clearPersistent = true)
        {
            if (!sources.Any()) return;

            if (clearPersistent)
            {
                foreach (var source in sources)
                {
                    source.OnSourceChanged -= s_NotifyBaseStatValChanged;
                }

                sources.Clear();
            }
            else
            {
                var toRemove = sources.Where(s => !s.Persistent).ToList();
                if (!toRemove.Any()) return;

                foreach (var source in toRemove)
                {
                    sources.Remove(source);
                    source.OnSourceChanged -= s_NotifyBaseStatValChanged;
                }
            }

            BaseStat.NotifyValueChanged();
        }

        public void ClearDisabledSource(bool clearPersistent = false)
        {
            var toRemove = sources.Where(h => !h.EnableModification && (clearPersistent || !h.Persistent)).ToList();

            if (!toRemove.Any()) return;

            foreach (var source in toRemove)
            {
                sources.Remove(source);
                source.OnSourceChanged -= s_NotifyBaseStatValChanged;
            }

            BaseStat.NotifyValueChanged();
        }

        private void s_NotifyBaseStatValChanged(IStatModifySource _) => BaseStat.NotifyValueChanged();
    }
}