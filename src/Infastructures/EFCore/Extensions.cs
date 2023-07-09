using System.Linq.Expressions;

using BuildingBlocks.Core.Model;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EFCore;

public static class Extensions
{
    public static IServiceCollection AddCustomDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext, IDbContext
    {
        services.AddDbContext<TContext>(options =>
                                            options.UseSqlServer(
                                                                 configuration.GetConnectionString("DefaultConnection"),
                                                                 x => x.MigrationsAssembly(typeof(TContext).Assembly
                                                                     .GetName().Name)));

        services.AddScoped<IDbContext>(provider => provider.GetService<TContext>());

        return services;
    }

    public static IApplicationBuilder UseMigration<TContext>(this IApplicationBuilder app, IWebHostEnvironment env)
        where TContext : DbContext, IDbContext
    {
        MigrateDatabaseAsync<TContext>(app.ApplicationServices).GetAwaiter().GetResult();

        if (!env.IsEnvironment("test"))
            SeedDataAsync(app.ApplicationServices).GetAwaiter().GetResult();

        return app;
    }


    // ref: https://github.com/pdevito3/MessageBusTestingInMemHarness/blob/main/RecipeManagement/src/RecipeManagement/Databases/RecipesDbContext.cs
    public static void FilterSoftDeletedProperties(this ModelBuilder modelBuilder)
    {
        Expression<Func<IAggregate, bool>> filterExpr = e => !e.IsDeleted;
        foreach (IMutableEntityType mutableEntityType in modelBuilder.Model.GetEntityTypes()
            .Where(m => m.ClrType.IsAssignableTo(typeof(IEntity))))
        {
            // modify expression to handle correct child type
            ParameterExpression parameter = Expression.Parameter(mutableEntityType.ClrType);
            Expression body = ReplacingExpressionVisitor
                .Replace(filterExpr.Parameters.First(), parameter, filterExpr.Body);
            LambdaExpression lambdaExpression = Expression.Lambda(body, parameter);

            // set filter
            mutableEntityType.SetQueryFilter(lambdaExpression);
        }
    }

    private static async Task MigrateDatabaseAsync<TContext>(IServiceProvider serviceProvider)
        where TContext : DbContext, IDbContext
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        TContext context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync();
    }

    private static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IEnumerable<IDataSeeder> seeders = scope.ServiceProvider.GetServices<IDataSeeder>();
        foreach (IDataSeeder seeder in seeders)
            await seeder.SeedAllAsync();
    }
}
