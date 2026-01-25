using Microsoft.EntityFrameworkCore;
using SampleApp.Data.Entities;

namespace SampleApp.Data;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired();
            entity.Property(e => e.CreatedDate)
                .IsRequired();
            entity.Property(e => e.RoleType)
                .IsRequired();
        });

        // Configure BlogPost entity
        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.BlogPostId);
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(5000);
            entity.Property(e => e.PublishedDate)
                .IsRequired();
            entity.Property(e => e.IsPublished)
                .IsRequired();

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Comment entity
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId);
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(1000);
            entity.Property(e => e.CreatedDate)
                .IsRequired();

            entity.HasOne(e => e.BlogPost)
                .WithMany()
                .HasForeignKey(e => e.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
