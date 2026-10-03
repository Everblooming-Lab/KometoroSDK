using System.Collections.Generic;

namespace EverbloomingLab.KometoroSDK.Collections
{
    /// <summary>
    /// List导航器，可循环，步进
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListNavigator<T>
    {
        public readonly IReadOnlyList<T> List;

        public bool Loopable { get; set; }

        /// <summary>
        /// 当前idx
        /// </summary>
        public int CurrentIndex { get; private set; }

        /// <summary>
        /// 总计执行步数(只计算前进多少)
        /// </summary>
        public int TotalStepsForward { get; private set; }

        /// <summary>
        /// 总执行步数
        /// </summary>
        public int TotalSteps { get; private set; }

        public T Current => List[CurrentIndex];

        public int ListCount => List?.Count ?? 0;

        public ListNavigator(IReadOnlyList<T> list, int startIdx = 0)
        {
            List = list;
            CurrentIndex = (startIdx % ListCount + ListCount) % ListCount;
        }

        public T Next()
        {
            if (Loopable)
            {
                CurrentIndex = ++CurrentIndex % ListCount;
            }
            else
            {
                if (CurrentIndex < ListCount - 1) CurrentIndex++;
            }

            TotalSteps++;
            TotalStepsForward++;
            return Current;
        }

        public T Previous()
        {
            if (Loopable)
            {
                CurrentIndex = (CurrentIndex - 1 + ListCount) % ListCount;
            }
            else
            {
                if (CurrentIndex > 0) CurrentIndex--;
            }

            TotalSteps++;
            TotalStepsForward--;
            return Current;
        }

        public void Reset(int startIdx = 0)
        {
            CurrentIndex = (startIdx % ListCount + ListCount) % ListCount;
            TotalSteps = 0;
            TotalStepsForward = 0;
        }

        public IEnumerable<LoopIterationInfo<T>> GetLoopIterationInfos(int targetLoopCount, int startIdx = 0) => List.GetLoopStep(targetLoopCount, startIdx);
    }
}