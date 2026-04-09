using Microsoft.EntityFrameworkCore;
using GradeTrackerApp.API.Models;

namespace GradeTrackerApp.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Subject> Subjects => Set<Subject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // User → Subjects relationship
        modelBuilder.Entity<Subject>()
            .HasOne(s => s.User)
            .WithMany(u => u.Subjects)
            .HasForeignKey(s => s.UserId);

        // Fix decimal precision warning
        modelBuilder.Entity<Subject>()
            .Property(s => s.Grade)
            .HasPrecision(4, 2);

        // Fix decimal precision in any other decimal properties
        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedOnAdd();
    }
}