using Microsoft.EntityFrameworkCore;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) {}

    // public DbSet<Voters> Voters { get; set; }
    public DbSet<BallotStyle> BallotStyles { get; set; }
    public DbSet<BallotStyleLink> BallotStyleLinks { get; set; }
    public DbSet<Voter> Voters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Use VUID as the primary key for BallotStyle.
    modelBuilder.Entity<BallotStyle>()
        .HasKey(b => b.VUID);

    base.OnModelCreating(modelBuilder);
}

}

