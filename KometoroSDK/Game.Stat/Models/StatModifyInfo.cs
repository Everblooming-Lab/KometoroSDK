namespace EverbloomingLab.KometoroSDK.Game
{
    public class StatModifyInfo
    {
        public readonly bool Persistent;

        public bool EnableModification { get; set; }

        public bool PercentageModify { get; set; }

        public StatModifyInfo(bool persistent)
        {
            EnableModification = false;
            PercentageModify = false;
            Persistent = persistent;
        }

        public StatModifyInfo(bool enable, bool percentageModify, bool persistent)
        {
            EnableModification = enable;
            PercentageModify = percentageModify;
            Persistent = persistent;
        }
    }
}