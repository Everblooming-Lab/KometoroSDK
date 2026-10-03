using System;

namespace EverbloomingLab.KometoroSDK.Data.ReadOnlyDatabase
{
    public static class Log
    {
        public static Action<string, string>? Print { get; set; } = (msg, type) => Console.WriteLine($"{type}: {msg} //KMTR_RDK.Unity.Data.RODB Native Log");
    }
}