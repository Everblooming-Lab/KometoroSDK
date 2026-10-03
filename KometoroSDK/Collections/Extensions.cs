using System.Collections.Generic;

namespace EverbloomingLab.KometoroSDK.Collections
{
    public static class Extensions
    {
        public static IEnumerable<LoopIterationInfo<T>> GetLoopStep<T>(this IReadOnlyList<T>? list, int targetLoopCount, int startIdx = 0)
        {
            if (list == null || list.Count == 0 || targetLoopCount <= 0)
                yield break;

            var count = list.Count;
            var totalSteps = count * targetLoopCount;

            for (var i = 0; i < totalSteps; i++)
            {
                // 计算各项指标
                var virtualIdx = i + startIdx;
                var currentStep = virtualIdx % count;
                var currentLoop = virtualIdx / count;

                yield return new LoopIterationInfo<T>(list[currentStep],
                                                      currentLoop,
                                                      targetLoopCount,
                                                      currentStep,
                                                      i,
                                                      list.Count,
                                                      totalSteps
                                                     );
            }
        }
    }
}