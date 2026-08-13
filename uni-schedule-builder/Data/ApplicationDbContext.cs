using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using uni_schedule_builder.Models;

namespace uni_schedule_builder.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RegularClassTerm> RegularClassTerms { get; set; }
    public DbSet<StudentSchedule> StudentSchedules { get; set; }
    public DbSet<TermException> TermExceptions { get; set; }
    public DbSet<StudyGroup> StudyGroups { get; set; }
    public DbSet<RegularClassTermGroup> RegularClassTermGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StudentSchedule>()
            .HasOne(ss => ss.Student)
            .WithMany()
            .HasForeignKey(ss => ss.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentSchedule>()
            .HasOne(ss => ss.RegularClassTerm)
            .WithMany()
            .HasForeignKey(ss => ss.RegularClassTermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TermException>()
            .HasOne(te => te.NewRoom)
            .WithMany()
            .HasForeignKey(te => te.NewRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudyGroup>()
            .HasIndex(g => g.Name)
            .IsUnique();

        builder.Entity<RegularClassTerm>()
            .HasIndex(t => t.SourceId)
            .IsUnique();

        builder.Entity<RegularClassTermGroup>()
            .HasKey(rctg => new { rctg.RegularClassTermId, rctg.StudyGroupId });

        builder.Entity<RegularClassTermGroup>()
            .HasOne(rctg => rctg.RegularClassTerm)
            .WithMany(t => t.RegularClassTermGroups)
            .HasForeignKey(rctg => rctg.RegularClassTermId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RegularClassTermGroup>()
            .HasOne(rctg => rctg.StudyGroup)
            .WithMany(g => g.RegularClassTermGroups)
            .HasForeignKey(rctg => rctg.StudyGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}