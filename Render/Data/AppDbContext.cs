using Microsoft.EntityFrameworkCore;
using Render.Shared;

namespace Render.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
modelBuilder.Entity<Like>()
        .HasKey(l => new { l.UserId, l.PostId });

    modelBuilder.Entity<Like>()
        .HasOne(l => l.User)
        .WithMany(u => u.Likes)
        .HasForeignKey(l => l.UserId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Like>()
        .HasOne(l => l.Post)
        .WithMany(p => p.Likes)
        .HasForeignKey(l => l.PostId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<User>()
        .HasIndex(u => u.Username)
        .IsUnique();

    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();

    modelBuilder.Entity<Post>()
        .Property(p => p.LikesCount)
        .HasDefaultValue(0);
}

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Like> Likes { get; set; } = null!;
}