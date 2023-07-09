using System.Collections.Concurrent;

using JetBrains.Annotations;

namespace BuildingBlocks.EventStoreDB.Events;

public class StreamNameMapper
{
    private static readonly StreamNameMapper _instance = new();

    private readonly ConcurrentDictionary<Type, string> _typeNameMap = new();

    public static void AddCustomMap<TStream>(string mappedStreamName)
    {
        AddCustomMap(typeof(TStream), mappedStreamName);
    }

    public static void AddCustomMap(Type streamType, string mappedStreamName)
    {
        _instance._typeNameMap.AddOrUpdate(streamType, mappedStreamName, (_, _) => mappedStreamName);
    }

    public static string ToStreamId<TStream>(object aggregateId, [CanBeNull] object tenantId = null)
    {
        return ToStreamId(typeof(TStream), aggregateId);
    }

    public static string ToStreamId(Type streamType, object aggregateId, [CanBeNull] object tenantId = null)
    {
        string tenantPrefix = tenantId != null ? $"{tenantId}_" : "";

        return $"{tenantPrefix}{streamType.Name}-{aggregateId}";
    }
}
