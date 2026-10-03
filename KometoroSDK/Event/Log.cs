using System;

namespace EverbloomingLab.KometoroSDK.Event
{
    public static class Log
    {
        public static Action<string>? Print { get; set; } = msg => Console.WriteLine($"Event Center: {msg}, //KMTR_RDK.Event Native Log");

        public static Action<Type, Delegate> OnSubscribed;
        public static Action<Type, Delegate> OnUnsubscribed;
        public static Action<Type, object> OnTriggered;
        public static Action OnClearSubscriptions;
    }
}