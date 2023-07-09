using BuildingBlocks.IdsGenerator;
using BuildingBlocks.Utils;

namespace BuildingBlocks.Core.Event;

public interface IInternalCommand
{
    public long Id => SnowFlakIdGenerator.NewId();

    public DateTime OccurredOn => DateTime.Now;

    public string Type => TypeProvider.GetTypeName(GetType());
}