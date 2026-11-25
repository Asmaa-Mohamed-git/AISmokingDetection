using AISmokingDetection.Model;
using Microsoft.EntityFrameworkCore;

namespace AISmokingDetection.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<Evidence> Evidences => Set<Evidence>();
        public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
        public DbSet<ReviewHistory> ReviewHistories => Set<ReviewHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasKey(i => i.IncidentId);

                // Idempotency key: an AI event is stored at most once.
                entity.HasIndex(i => i.EventId).IsUnique();
                entity.HasIndex(i => i.ProcessingJobId);

                entity.Property(i => i.TrackId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(i => i.EventType)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.Property(i => i.Status)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.Property(i => i.IdentityStatus)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.Property(i => i.ReviewStatus)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.Property(i => i.ConfidenceScore)
                      .HasPrecision(5, 4);

                entity.HasOne(i => i.ProcessingJob)
                      .WithMany(p => p.Incidents)
                      .HasForeignKey(i => i.ProcessingJobId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Evidence>(entity =>
            {
                entity.HasKey(e => e.EvidenceId);

                entity.Property(e => e.EvidenceType)
                      .IsRequired()
                      .HasMaxLength(30);

                entity.Property(e => e.StorageReference)
                      .HasMaxLength(500);

                entity.Property(e => e.AvailabilityStatus)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.HasOne(e => e.Incident)
                      .WithMany(i => i.Evidences)
                      .HasForeignKey(e => e.IncidentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProcessingJob>(entity =>
            {
                entity.HasKey(p => p.ProcessingJobId);

                // Public identifier used in all AI callbacks.
                entity.HasIndex(p => p.JobId).IsUnique();
                entity.HasIndex(p => p.VideoId);
                entity.HasIndex(p => new { p.Status, p.UpdatedAtUtc });

                entity.Property(p => p.VideoId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.SchemaVersion)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(p => p.VideoReference)
                      .HasMaxLength(500);

                entity.Property(p => p.Status)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.Property(p => p.DataOrigin)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(p => p.ModelVersion).HasMaxLength(100);
                entity.Property(p => p.ConfigVersion).HasMaxLength(100);
                entity.Property(p => p.GalleryVersion).HasMaxLength(100);

                entity.Property(p => p.ErrorCode).HasMaxLength(100);
                entity.Property(p => p.ErrorMessage).HasMaxLength(500);
                entity.Property(p => p.LastSubmissionError).HasMaxLength(500);
            });

            modelBuilder.Entity<ReviewHistory>(entity =>
            {
                entity.HasKey(r => r.ReviewHistoryId);

                entity.Property(r => r.Decision)
                      .IsRequired()
                      .HasMaxLength(30);

                entity.Property(r => r.ReviewReason)
                      .HasMaxLength(500);

                entity.HasOne(r => r.Incident)
                      .WithMany(i => i.ReviewHistories)
                      .HasForeignKey(r => r.IncidentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
