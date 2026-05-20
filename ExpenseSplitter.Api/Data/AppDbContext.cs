using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();
    public DbSet<Settlement> Settlements => Set<Settlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        //unique email per user
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        //decimal precion for money
        modelBuilder.Entity<Expense>()
            .Property(e => e.Amount)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<ExpenseSplit>()
            .Property(es => es.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Settlement>()
            .Property(s => s.Amount)
            .HasPrecision(18, 2);

        //prevent cascading delete cycles
        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMemberships)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.PaidBy)
            .WithMany(u => u.PaidExpenses)
            .OnDelete(DeleteBehavior.Restrict);
    }
}