using System.Collections.Concurrent;
using System.Reflection;

using JetBrains.Annotations;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BuildingBlocks.EventStoreDB.Serialization;

public static class JsonObjectContractProvider
{
    private static readonly Type _constructorAttributeType = typeof(JsonConstructorAttribute);
    private static readonly ConcurrentDictionary<string, JsonObjectContract> _constructors = new();

    public static JsonObjectContract UsingNonDefaultConstructor(
        JsonObjectContract contract,
        Type objectType,
        Func<ConstructorInfo, JsonPropertyCollection, IList<JsonProperty>> createConstructorParameters)
    {
        return _constructors.GetOrAdd(objectType.AssemblyQualifiedName!, _ =>
        {
            ConstructorInfo nonDefaultConstructor = GetNonDefaultConstructor(objectType);

            if (nonDefaultConstructor == null) return contract;

            contract.OverrideCreator = GetObjectConstructor(nonDefaultConstructor);
            contract.CreatorParameters.Clear();
            foreach (JsonProperty constructorParameter in
                createConstructorParameters(nonDefaultConstructor, contract.Properties))
                contract.CreatorParameters.Add(constructorParameter);

            return contract;
        });
    }

    private static ObjectConstructor<object> GetObjectConstructor(MethodBase method)
    {
        ConstructorInfo c = method as ConstructorInfo;

        if (c == null)
            return a => method.Invoke(null, a)!;

        if (!c.GetParameters().Any())
            return _ => c.Invoke(Array.Empty<object?>());

        return a => c.Invoke(a);
    }

    [CanBeNull]
    private static ConstructorInfo GetNonDefaultConstructor(Type objectType)
    {
        // Use default contract for non-object types.
        if (objectType.IsPrimitive || objectType.IsEnum)
            return null;

        return GetAttributeConstructor(objectType)
               ?? GetTheMostSpecificConstructor(objectType);
    }

    [CanBeNull]
    private static ConstructorInfo GetAttributeConstructor(Type objectType)
    {
        // Use default contract for non-object types.
        if (objectType.IsPrimitive || objectType.IsEnum)
            return null;

        List<ConstructorInfo> constructors = objectType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(c => c.GetCustomAttributes().Any(a => a.GetType() == _constructorAttributeType)).ToList();

        return constructors.Count switch
        {
            1   => constructors[0],
            > 1 => throw new JsonException($"Multiple constructors with a {_constructorAttributeType.Name}."),
            _   => null
        };
    }

    [CanBeNull]
    private static ConstructorInfo GetTheMostSpecificConstructor(Type objectType)
    {
        return objectType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(e => e.GetParameters().Length)
            .FirstOrDefault();
    }
}
