using JetBrains.Annotations;

namespace BuildingBlocks.Utils;

public static class NoSynchronizationContextScope
{
    public static Disposable Enter()
    {
        SynchronizationContext context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        return new Disposable(context);
    }

    public struct Disposable : IDisposable
    {
        [CanBeNull] private readonly SynchronizationContext _synchronizationContext;

        public Disposable([CanBeNull] SynchronizationContext synchronizationContext)
        {
            this._synchronizationContext = synchronizationContext;
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
        }
    }
}
