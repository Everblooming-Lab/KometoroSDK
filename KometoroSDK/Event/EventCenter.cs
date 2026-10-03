using System;
using System.Collections.Generic;

namespace EverbloomingLab.KometoroSDK.Event
{
    public static class EventCenter
    {
        private static readonly Dictionary<Type, List<IHandler>> _subscribers = new Dictionary<Type, List<IHandler>>();

        private static readonly Dictionary<Type, List<Type>> _hierarchy_cache = new Dictionary<Type, List<Type>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<IHandler>();
                _subscribers[type] = list;
            }

            list.Add(new Handler<T>(handler));
            Log.OnSubscribed?.Invoke(type, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list)) return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].IsSameHandler(handler))
                {
                    list.RemoveAt(i);
                    Log.OnUnsubscribed?.Invoke(typeof(T), handler);
                    break;
                }
            }
        }

        public static void UnsubscribeAll<T>(Action<T> handler)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list)) return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].IsSameHandler(handler))
                {
                    list.RemoveAt(i);
                    Log.OnUnsubscribed?.Invoke(typeof(T), handler);
                }
            }
        }

        public static void ClearAll()
        {
            _subscribers.Clear();
            _hierarchy_cache.Clear();
            Log.OnClearSubscriptions?.Invoke();
            Log.Print?.Invoke("All event subscriptions cleared.");
        }

        public static void Trigger<T>(T eventData)
        {
            var runtimeType = eventData is null ? typeof(T) : eventData.GetType();
            Log.OnTriggered?.Invoke(runtimeType, eventData);

            var typesToTrigger = CacheTypesHierarchy(runtimeType);


            for (var i = 0; i < typesToTrigger.Count; i++)
            {
                if (!_subscribers.TryGetValue(typesToTrigger[i], out var handlers))
                    continue;
                for (var j = handlers.Count - 1; j >= 0; j--)
                {
                    handlers[j].Invoke(eventData);
                }
            }
        }

        private static List<Type> CacheTypesHierarchy(Type type)
        {
            if (_hierarchy_cache.TryGetValue(type, out var cached)) return cached;

            var result = new List<Type>();
            var current = type;
            while (current != null && current != typeof(object))
            {
                result.Add(current);
                current = current.BaseType;
            }

            result.AddRange(type.GetInterfaces());
            _hierarchy_cache.TryAdd(type, result);

            return result;
        }


        private interface IHandler
        {
            public void Invoke(object eventData);
            public bool IsSameHandler(Delegate del);
        }

        private class Handler<T> : IHandler
        {
            private readonly Action<T> action;
            public Handler(Action<T> action) => this.action = action;

            public void Invoke(object eventData)
            {
                action.Invoke((T)eventData);
            }

            public bool IsSameHandler(Delegate del) => Equals(action, del);
        }
    }
}