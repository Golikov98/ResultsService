using Microsoft.EntityFrameworkCore;
using ResultsService.Api.Models;

namespace ResultsService.Api.Data
{
    public class ResultsDbContext: DbContext
    {
        public ResultsDbContext(DbContextOptions<ResultsDbContext> options) : base(options) { }
        public DbSet<ParseFileResult> Values => Set<ParseFileResult>();
        public DbSet<Result> Results => base.Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ParseFileResult>(entity =>
            {
                entity.ToTable("Values");
                entity.HasKey(x=>x.Id);

                entity.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(255);

                entity.Property(x => x.Date)
                .IsRequired();

                entity.Property(x => x.ExecutionTime)
                .IsRequired();

                entity.Property(x => x.Value)
                .IsRequired()
                .HasPrecision(18, 6);
            });

            modelBuilder.Entity<Result>(entity =>
            {
                entity.ToTable("Results");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.TimeDelta)
                    .IsRequired();

                entity.Property(x => x.StartDate)
                    .IsRequired();

                entity.Property(x => x.AverageExecutionTime)
                    .IsRequired();

                entity.Property(x => x.AverageValue)
                    .IsRequired()
                    .HasPrecision(18, 6);

                entity.Property(x => x.MedianValue)
                    .IsRequired()
                    .HasPrecision(18, 6);

                entity.Property(x => x.MaxValue)
                    .IsRequired()
                    .HasPrecision(18, 6);

                entity.Property(x => x.MinValue)
                    .IsRequired()
                    .HasPrecision(18, 6);
            });
        }
    }
}
