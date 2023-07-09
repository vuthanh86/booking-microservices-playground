using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Passenger.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PassengerDbContext>
{
    public PassengerDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PassengerDbContext> builder = new DbContextOptionsBuilder<PassengerDbContext>();

        builder.UseSqlServer(
                             "Data Source=.\\sqlexpress;Initial Catalog=PassengerDB;Persist Security Info=False;Integrated Security=SSPI");
        return new PassengerDbContext(builder.Options, null);
    }
}
