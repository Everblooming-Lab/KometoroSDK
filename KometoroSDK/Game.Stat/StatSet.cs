using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EverbloomingLab.KometoroSDK.Game
{
    public class StatSet : IEnumerable<IStat>
    {
        protected readonly List<IStat> stats = new List<IStat>();

        public int Count => stats.Count;

        public IStat? this[int statSeq] => statSeq >= 0 && statSeq < stats.Count ? stats[statSeq] : null;

        public IStat? this[string name] => stats.Find(s => s.Info.Name == name);

        public IStat? GetStat(int statId) => stats.Find(s => s.Info.Id == statId);
        public IStat? GetStat(string name) => this[name];
        public TStat? GetStat<TStat>(int statId) where TStat : class, IStat => (TStat?)stats.Find(s => s.Info.Id == statId);
        public TStat? GetStat<TStat>(string name) where TStat : class, IStat => (TStat?)this[name];

        public void Add(IStat stat)
        {
            stats.Add(stat);
        }

        public void AddUnique(IStat stat)
        {
            if (stats.All(s => s.Info.Id != stat.Info.Id))
                stats.Add(stat);
            else
                throw new InvalidOperationException($"Stat id {stat.Info.Id} already exists.");
        }

        public void Remove(IStat stat) => stats.RemoveAll(s => s == stat);

        public void Remove(int statId) => stats.RemoveAll(s => s.Info.Id == statId);

        public void Remove(string name) => stats.RemoveAll(s => s.Info.Name == name);

        public void Clear() => stats.Clear();

        public IEnumerator<IStat> GetEnumerator() => stats.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}