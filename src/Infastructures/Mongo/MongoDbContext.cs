using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace BuildingBlocks.Mongo;

// https://www.thecodebuzz.com/mongodb-repository-implementation-unit-testing-net-core-example/

public class MongoDbContext : IMongoDbContext
{
    protected readonly IList<Func<Task>> Commands;

    public MongoDbContext(IOptions<MongoOptions> options)
    {
        RegisterConventions();

        MongoClient = new MongoClient(options.Value.ConnectionString);
        string databaseName = options.Value.DatabaseName;
        Database = MongoClient.GetDatabase(databaseName);

        // Every command will be stored and it'll be processed at SaveChanges
        Commands = new List<Func<Task>>();
    }

    [CanBeNull] public IClientSessionHandle Session { get; set; }
    public IMongoDatabase Database { get; }
    public IMongoClient MongoClient { get; }

    public IMongoCollection<T> GetCollection<T>([CanBeNull] string name = null)
    {
        return Database.GetCollection<T>(name ?? typeof(T).Name.ToLower());
    }

    public void Dispose()
    {
        while (Session is { IsInTransaction: true })
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

        GC.SuppressFinalize(this);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = Commands.Count;

        using (Session = await MongoClient.StartSessionAsync(cancellationToken: cancellationToken))
        {
            Session.StartTransaction();

            try
            {
                IEnumerable<Task> commandTasks = Commands.Select(c => c());

                await Task.WhenAll(commandTasks);

                await Session.CommitTransactionAsync(cancellationToken);
            }
            catch (System.Exception ex)
            {
                await Session.AbortTransactionAsync(cancellationToken);
                Commands.Clear();
                throw;
            }
        }

        Commands.Clear();
        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        Session = await MongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        Session.StartTransaction();
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is { IsInTransaction: true })
            await Session.CommitTransactionAsync(cancellationToken);

        Session?.Dispose();
    }

    public async Task RollbackTransaction(CancellationToken cancellationToken = default)
    {
        await Session?.AbortTransactionAsync(cancellationToken)!;
    }

    public void AddCommand(Func<Task> func)
    {
        Commands.Add(func);
    }

    private static void RegisterConventions()
    {
        ConventionRegistry.Register(
                                    "conventions",
                                    new ConventionPack
                                    {
                                        new CamelCaseElementNameConvention(),
                                        new IgnoreExtraElementsConvention(true),
                                        new EnumRepresentationConvention(BsonType.String),
                                        new IgnoreIfDefaultConvention(false),
                                        new ImmutablePocoConvention()
                                    }, _ => true);
    }

    public async Task ExecuteTransactionalAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            await action();

            await CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransaction(cancellationToken);
            throw;
        }
    }

    public async Task<T> ExecuteTransactionalAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            T result = await action();

            await CommitTransactionAsync(cancellationToken);

            return result;
        }
        catch
        {
            await RollbackTransaction(cancellationToken);
            throw;
        }
    }
}
