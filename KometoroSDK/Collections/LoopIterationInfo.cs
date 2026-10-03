namespace EverbloomingLab.KometoroSDK.Collections
{
    /// <summary>
    /// 循环的迭代信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct LoopIterationInfo<T>
    {
        /// <summary>
        /// 当前的元素
        /// </summary>
        public readonly T Item;

        /// <summary>
        /// 当前的循环轮次
        /// </summary>
        public readonly int CurrentLoop;

        /// <summary>
        /// 当前的循环步进次数
        /// </summary>
        public readonly int CurrentStep;

        /// <summary>
        /// 总循环步进次数
        /// </summary>
        public readonly int TotalLoopStep;

        /// <summary>
        /// <summary>
        /// 总循环轮次数
        /// </summary>
        public readonly int TotalLoopCount;

        private readonly int listCount;
        private readonly int totalSteps;

        /// <summary>
        /// 归化到0-1之间的循环进度，0表示开始，1表示结束
        /// </summary>
        public float NormalizedProgress => totalSteps > 1 ? (float)TotalLoopStep / (totalSteps - 1) : 1f;

        /// <summary>
        /// 当前轮次的归化进度，0表示当前轮次开始，1表示当前轮次结束
        /// </summary>
        public float NormalizedProgressInCurrentLoop => listCount > 1 ? (float)CurrentStep / (listCount - 1) : 1f;

        public LoopIterationInfo(T item, int currentLoop, int totalLoopCount, int currentStep, int totalLoopStep, int listCount, int totalSteps)
        {
            Item = item;
            CurrentLoop = currentLoop;
            TotalLoopCount = totalLoopCount;
            CurrentStep = currentStep;
            TotalLoopStep = totalLoopStep;
            this.listCount = listCount;
            this.totalSteps = totalSteps;
        }
    }
}