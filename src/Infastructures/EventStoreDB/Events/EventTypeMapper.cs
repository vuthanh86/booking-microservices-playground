using System.Collections.Concurrent;

using BuildingBlocks.Utils;

using JetBrains.Annotations;

namespace BuildingBlocks.EventStoreDB.Events;

public class EventTypeMapper
{
    private static readonly EventTypeMapper _instance = new();

    private readonly ConcurrentDictionary<string, Type?> _typeMap = new();
    private readonly ConcurrentDictionary<Type, string> _typeNameMap = new();

    public static void AddCustomMap<T>(string mappedEventTypeName)
    {
        AddCustomMap(typeof(T), mappedEventTypeName);
    }

    public static void AddCustomMap(Type eventType, string mappedEventTypeName)
    {
        _instance._typeNameMap.AddOrUpdate(eventType, mappedEventTypeName, (_, _) => mappedEventTypeName);
        _instance._typeMap.AddOrUpdate(mappedEventTypeName, eventType, (_, _) => eventType);
    }

    public static string ToName<TEventType>()
    {
        return ToName(typeof(TEventType));
    }

    public static string ToName(Type eventType)
    {
        return _instance._typeNameMap.GetOrAdd(eventType, _ =>
        {
            string eventTypeName = eventType.FullName!.Replace(".", "_");

            _instance._typeMap.AddOrUpdate(eventTypeName, eventType, (_, _) => eventType);

            return eventTypeName;
        });
    }

    [CanBeNull]
    public static Type ToType(string eventTypeName)
    {
        return _instance._typeMap.GetOrAdd(eventTypeName, _ =>
        {
            Type type = TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly(eventTypeName.Replace("_", "."));

            if (type == null)
                return null;

            _instance._typeNameMap.AddOrUpdate(type, eventTypeName, (_, _) => eventTypeName);

            return type;
        });
    }
}
