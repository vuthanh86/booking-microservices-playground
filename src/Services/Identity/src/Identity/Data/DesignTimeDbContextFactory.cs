using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityContext>
{
    public IdentityContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<IdentityContext> builder = new DbContextOptionsBuilder<IdentityContext>();

        builder.UseSqlServer("Server=.\\sqlexpress;Database=IdentityDB;Trusted_Connection=True;MultipleActiveResultSets=true");
        return new IdentityContext(builder.Options, null);
    }
}
