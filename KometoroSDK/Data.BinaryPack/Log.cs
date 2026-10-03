using System;

namespace EverbloomingLab.KometoroSDK.Data.BinPack
{
    public static class Log
    {
        public static Action<string>? Print { get; set; } = msg => Console.WriteLine($"BinPack: {msg} //KMTR_RDK.Unity.Data.BinPack Native Log");
    }
}