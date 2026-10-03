using System;

namespace EverbloomingLab.KometoroSDK.Game
{
    public static class StatFactory
    {
        public static IStat CreateBaseStat(Request request)
        {
            var info = new StatInfo(request.Name, request.Id, request.Remark);

            StatBase stat;

            if (request.Modifiable)
            {
                if (request.DynamicMode)
                    stat = new ModifiableDynamicStat(info)
                    {
                        onRawValue = request.OnRawValueFunc,
                    };
                else
                    stat = new ModifiableStat(info)
                    {
                        Raw = (RawValue)request.RawValue,
                    };
            }
            else
            {
                if (request.DynamicMode)
                    stat = new DynamicStat(info)
                    {
                        onRawValue = request.OnRawValueFunc,
                    };
                else
                    stat = new Stat(info)
                    {
                        Raw = (RawValue)request.RawValue,
                    };
            }

            stat.upperBoundary = request.UpperBoundary;
            stat.lowerBoundary = request.LowerBoundary;

            return stat;
        }

        public static TStat CreateBaseStat<TStat>(Request request) where TStat : IStat
        {
            var stat = CreateBaseStat(request);
            return (TStat)stat;
        }

        public static StatBoolLevel CreateBoolLevelStat(Request request) => new StatBoolLevel(CreateBaseStat(request));

        public static StatResources CreateResourcesStat(Request request)
        {
            var cur = new Stat(new StatInfo(request.Name + "_current", 0, "current"));
            IStat max;
            IStat min;

            if (request.Modifiable)
            {
                if (request.DynamicMode)
                {
                    max = new ModifiableDynamicStat(new StatInfo(request.Name + "max", 2, "max"));
                    min = new ModifiableDynamicStat(new StatInfo(request.Name + "min", 1, "min"));
                }
                else
                {
                    max = new ModifiableStat(new StatInfo(request.Name + "max", 2, "max"));
                    min = new ModifiableStat(new StatInfo(request.Name + "min", 1, "min"));
                }
            }
            else
            {
                if (request.DynamicMode)
                {
                    max = new DynamicStat(new StatInfo(request.Name + "max", 2, "max"));
                    min = new DynamicStat(new StatInfo(request.Name + "min", 1, "min"));
                }
                else
                {
                    max = new Stat(new StatInfo(request.Name + "max", 2, "max"));
                    min = new Stat(new StatInfo(request.Name + "min", 1, "min"));
                }
            }

            var sr = new StatResources
            {
                Max = max,
                Min = min,
                Current = cur,
                Info = new StatInfo(request.Name, request.Id, request.Remark),
            };
            return sr;
        }

        public static StatResources CreateResourcesStat(Stat current, IStat max, IStat min, StatInfo? info = null) =>
            new StatResources
            {
                Current = current,
                Max = max,
                Min = min,
                Info = info ?? new StatInfo(current.Info.Name, current.Info.Id, current.Info.Remark),
            };

        public static SourceStatPack CreateSourceStatPack(Request request, Request.Modify modifyRequest)
        {
            var attr = CreateBaseStat(request);
            return CreateSourceStatPack(attr, modifyRequest);
        }

        public static SourceStatPack CreateSourceStatPack(IStat stat, Request.Modify modifyRequest)
        {
            var modifyInfo = new StatModifyInfo(modifyRequest.EnableModification, modifyRequest.PercentageModify, modifyRequest.Persistent);
            return new SourceStatPack(stat, modifyInfo);
        }

        public class Request
        {
            public double RawValue;
            public Func<object?, RawValue>? OnRawValueFunc = null;
            public double UpperBoundary = double.MaxValue;
            public double LowerBoundary = double.MinValue;
            public string Name = "";
            public string Remark = "";
            public int Id = 0;
            public bool DynamicMode = false;
            public bool Modifiable = false;

            public Request(bool dynamicMode = false) => DynamicMode = dynamicMode;

            public class Modify
            {
                public bool Persistent;
                public bool EnableModification;
                public bool PercentageModify;
            }
        }
    }
}