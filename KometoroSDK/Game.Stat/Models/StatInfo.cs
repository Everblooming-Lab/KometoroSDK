namespace EverbloomingLab.KometoroSDK.Game
{
    public class StatInfo
    {
        public readonly string Name;
        public readonly int Id;
        public string Remark { get; set; }

        public StatInfo(string name, int id, string remark = "")
        {
            Name = name;
            Remark = remark;
            Id = id;
        }
    }
}