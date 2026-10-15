
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// This class connects our C# models to the RaceDay database.
/// It also defines the relationships and data rules that SQL Server
/// must enforce when records are created or changed.
/// </summary>
public class RaceDayDbContext : DbContext
{
    // Entity Framework provides these options when the application starts.
    // They will include our SQL Server connection settings.
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    // Each DbSet represents a table that the application can work with.
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<EventType> EventTypes { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Enrolment> Enrolments { get; set; } = null!;
    public DbSet<Result> Results { get; set; } = null!;

    /// <summary>
    /// Configures the database tables, relationships and constraints.
    /// We keep these rules here so the database follows our Part 1 design.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -----------------------------------------------------
        // 1. USER
        // -----------------------------------------------------
        // Stores organiser and participant accounts.
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", table =>
            {
                // Only the two roles in our Part 1 design are allowed.
                table.HasCheckConstraint(
                    "CK_User_Role",
                    "[Role] IN ('ORGANISER', 'PARTICIPANT')");
            });

            entity.HasKey(user => user.UserID)
                .HasName("PK_User");

            // Match the VARCHAR sizes used by our Part 1 SQL script.
            entity.Property(user => user.FirstName)
                .HasColumnType("varchar(50)")
                .IsRequired();

            entity.Property(user => user.LastName)
                .HasColumnType("varchar(50)")
                .IsRequired();

            entity.Property(user => user.Email)
                .HasColumnType("varchar(255)")
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasColumnType("varchar(255)")
                .IsRequired();

            entity.Property(user => user.Role)
                .HasColumnType("varchar(20)")
                .IsRequired();

            entity.Property(user => user.PhoneNumber)
                .HasColumnType("varchar(20)");

            // SQL Server fills in the creation date when no value is supplied.
            entity.Property(user => user.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            // Two accounts must not share the same email address.
            entity.HasAlternateKey(user => user.Email)
                .HasName("UQ_User_Email");
        });

        // -----------------------------------------------------
        // 2. EVENT TYPE
        // -----------------------------------------------------
        // Lists the types of events RaceDay supports.
        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventType");

            entity.HasKey(type => type.EventTypeID)
                .HasName("PK_EventType");

            entity.Property(type => type.TypeName)
                .HasColumnType("varchar(50)")
                .IsRequired();

            entity.Property(type => type.Description)
                .HasColumnType("varchar(200)");

            // Each event type must have a unique name.
            entity.HasAlternateKey(type => type.TypeName)
                .HasName("UQ_EventType_TypeName");
        });

        // -----------------------------------------------------
        // 3. EVENT
        // -----------------------------------------------------
        // Stores the event details and identifies its organiser.
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event", table =>
            {
                // An event cannot have a zero or negative distance.
                table.HasCheckConstraint(
                    "CK_Event_Distance",
                    "[Distance] > 0");
            });

            entity.HasKey(raceEvent => raceEvent.EventID)
                .HasName("PK_Event");

            entity.Property(raceEvent => raceEvent.EventName)
                .HasColumnType("varchar(150)")
                .IsRequired();

            entity.Property(raceEvent => raceEvent.Description)
                .HasColumnType("varchar(500)")
                .IsRequired();

            // The Part 1 database stores the event date without a time.
            entity.Property(raceEvent => raceEvent.EventDate)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(raceEvent => raceEvent.Location)
                .HasColumnType("varchar(200)")
                .IsRequired();

            entity.Property(raceEvent => raceEvent.Distance)
                .HasColumnType("decimal(6,2)")
                .IsRequired();

            entity.Property(raceEvent => raceEvent.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            // Connects the event to its event type.
            entity.HasOne(raceEvent => raceEvent.EventType)
                .WithMany()
                .HasForeignKey(raceEvent => raceEvent.EventTypeID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Event_EventType");

            // Connects the event to the user who organises it.
            entity.HasOne(raceEvent => raceEvent.Organiser)
                .WithMany()
                .HasForeignKey(raceEvent => raceEvent.OrganiserID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Event_Organiser");
        });

        // -----------------------------------------------------
        // 4. CATEGORY
        // -----------------------------------------------------
        // Each category belongs to an event, for example a 5 km run.
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category", table =>
            {
                // Age limits cannot be negative.
                table.HasCheckConstraint(
                    "CK_Category_MinimumAge",
                    "[MinimumAge] IS NULL OR [MinimumAge] >= 0");

                table.HasCheckConstraint(
                    "CK_Category_MaximumAge",
                    "[MaximumAge] IS NULL OR [MaximumAge] >= 0");

                // If both ages are supplied, maximum age must be
                // greater than or equal to minimum age.
                table.HasCheckConstraint(
                    "CK_Category_AgeRange",
                    "[MinimumAge] IS NULL OR [MaximumAge] IS NULL " +
                    "OR [MaximumAge] >= [MinimumAge]");
            });

            entity.HasKey(category => category.CategoryID)
                .HasName("PK_Category");

            entity.Property(category => category.CategoryName)
                .HasColumnType("varchar(100)")
                .IsRequired();

            entity.Property(category => category.Description)
                .HasColumnType("varchar(200)");

            // This relationship connects the category to its event.
            entity.HasOne(category => category.Event)
                .WithMany()
                .HasForeignKey(category => category.EventID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Category_Event");

            // A single event cannot contain two categories with the same name.
            entity.HasAlternateKey(category =>
                new { category.EventID, category.CategoryName })
                .HasName("UQ_Category_Event_Name");

            // This combination also identifies the category within an event.
            // Enrolment uses it to ensure its category belongs to its event.
            entity.HasAlternateKey(category =>
                new { category.EventID, category.CategoryID })
                .HasName("UQ_Category_Event_Category");
        });

        // -----------------------------------------------------
        // 5. ENROLMENT
        // -----------------------------------------------------
        // Records who entered an event and which category they selected.
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolment", table =>
            {
                table.HasCheckConstraint(
                    "CK_Enrolment_Status",
                    "[Status] IN ('ACTIVE', 'CANCELLED', 'COMPLETED')");
            });

            entity.HasKey(enrolment => enrolment.EnrolmentID)
                .HasName("PK_Enrolment");

            entity.Property(enrolment => enrolment.EnrolmentDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            entity.Property(enrolment => enrolment.Status)
                .HasColumnType("varchar(20)")
                .HasDefaultValue("ACTIVE")
                .IsRequired();

            // The same participant may not enter the same event twice.
            entity.HasAlternateKey(enrolment =>
                new { enrolment.ParticipantID, enrolment.EventID })
                .HasName("UQ_Enrolment_Participant_Event");

            // Identifies the participant who made the entry.
            entity.HasOne(enrolment => enrolment.Participant)
                .WithMany()
                .HasForeignKey(enrolment => enrolment.ParticipantID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolment_Participant");

            // Identifies the event being entered.
            entity.HasOne(enrolment => enrolment.Event)
                .WithMany()
                .HasForeignKey(enrolment => enrolment.EventID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolment_Event");

            // Connects the enrolment to the chosen category.
            entity.HasOne(enrolment => enrolment.Category)
                .WithMany()
                .HasForeignKey(enrolment => enrolment.CategoryID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolment_Category");

            // This additional composite foreign key is important:
            // it prevents an enrolment from choosing a category belonging
            // to a different event.
            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(enrolment =>
                    new { enrolment.EventID, enrolment.CategoryID })
                .HasPrincipalKey(category =>
                    new { category.EventID, category.CategoryID })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolment_Event_Category");
        });

        // -----------------------------------------------------
        // 6. RESULT
        // -----------------------------------------------------
        // Stores a participant's finishing time and position.
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Result", table =>
            {
                table.HasCheckConstraint(
                    "CK_Result_FinishingPosition",
                    "[FinishingPosition] > 0");
            });

            entity.HasKey(result => result.ResultID)
                .HasName("PK_Result");

            entity.Property(result => result.FinishTime)
                .HasColumnType("time(0)")
                .IsRequired();

            entity.Property(result => result.RecordedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            // Each enrolment can have only one result.
            entity.HasAlternateKey(result => result.EnrolmentID)
                .HasName("UQ_Result_Enrolment");

            entity.HasOne(result => result.Enrolment)
                .WithOne(enrolment => enrolment.Result)
                .HasForeignKey<Result>(result => result.EnrolmentID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Result_Enrolment");
        });
    }
}
