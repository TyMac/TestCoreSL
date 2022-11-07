global using Microsoft.EntityFrameworkCore;

namespace TestCoreSL
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<CoreMetric> CoreMetrics => Set<CoreMetric>();
    }
}
