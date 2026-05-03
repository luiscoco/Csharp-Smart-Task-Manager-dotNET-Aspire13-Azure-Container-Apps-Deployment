using Microsoft.EntityFrameworkCore;
using SmartTaskManager.Infrastructure.Persistence.Models;

namespace SmartTaskManager.Infrastructure.Persistence;

public sealed class SmartTaskManagerDbContext : DbContext
{
    public SmartTaskManagerDbContext(DbContextOptions<SmartTaskManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserRecord> Users => Set<UserRecord>();

    public DbSet<TaskRecord> Tasks => Set<TaskRecord>();

    public DbSet<TaskHistoryRecord> TaskHistoryEntries => Set<TaskHistoryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRecord>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.UserName)
                .IsUnique();

            entity.Property(user => user.UserName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.CreatedOnUtc)
                .HasColumnType("datetime2")
                .IsRequired();
        });

        modelBuilder.Entity<TaskRecord>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);

            entity.Property(task => task.TaskType)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(task => task.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(task => task.Description)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(task => task.DueDate)
                .HasColumnType("datetime2")
                .IsRequired();

            entity.Property(task => task.CategoryName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(task => task.CategoryDescription)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(task => task.CategoryId)
                .IsRequired();

            entity.Property(task => task.Priority)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(task => task.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.HasIndex(task => new { task.UserId, task.DueDate });

            entity.HasOne<UserRecord>()
                .WithMany()
                .HasForeignKey(task => task.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(task => task.HistoryEntries)
                .WithOne()
                .HasForeignKey(history => history.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskHistoryRecord>(entity =>
        {
            entity.ToTable("TaskHistoryEntries");
            entity.HasKey(history => history.Id);

            entity.Property(history => history.OccurredOnUtc)
                .HasColumnType("datetime2")
                .IsRequired();

            entity.Property(history => history.Action)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(history => history.Details)
                .HasMaxLength(1000)
                .IsRequired();

            entity.HasIndex(history => new { history.TaskId, history.Sequence })
                .IsUnique();
        });
    }
}
