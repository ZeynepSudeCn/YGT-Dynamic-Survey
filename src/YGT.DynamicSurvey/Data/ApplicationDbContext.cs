using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }


    // =====================================================
    // TABLOLAR
    // =====================================================

    public DbSet<Survey> Surveys { get; set; } = null!;

    public DbSet<Question> Questions { get; set; } = null!;

    public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;

    public DbSet<SurveyResponse> SurveyResponses { get; set; } = null!;

    public DbSet<Answer> Answers { get; set; } = null!;

    public DbSet<SystemLog> SystemLogs { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    public DbSet<UserNotification> UserNotifications { get; set; } = null!;

    public DbSet<Event> Events { get; set; } = null!;


    // =====================================================
    // MODEL AYARLARI
    // =====================================================

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        // =================================================
        // SURVEY
        // =================================================

        builder.Entity<Survey>()
            .HasIndex(x => x.Code)
            .IsUnique();


        // Anket silinirse soruları da silinsin.
        builder.Entity<Question>()
            .HasOne(x => x.Survey)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);


        // =================================================
        // QUESTION OPTION
        // =================================================

        // Soru silinirse seçenekleri de silinsin.
        builder.Entity<QuestionOption>()
            .HasOne(x => x.Question)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);


        // =================================================
        // SURVEY RESPONSE
        // =================================================

        // Anket silinirse o ankete ait yanıt kayıtları da silinsin.
        builder.Entity<SurveyResponse>()
            .HasOne(x => x.Survey)
            .WithMany()
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);


        // =================================================
        // ANSWER
        // =================================================

        // SurveyResponse silinirse cevapları da silinsin.
        builder.Entity<Answer>()
            .HasOne(x => x.SurveyResponse)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.SurveyResponseId)
            .OnDelete(DeleteBehavior.Cascade);


        // SQL Server multiple cascade path hatasını önlemek için
        // Question -> Answer ilişkisinde otomatik silme kullanmıyoruz.
        builder.Entity<Answer>()
            .HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);


        // =================================================
        // SYSTEM LOG
        // =================================================

        builder.Entity<SystemLog>()
            .HasIndex(x => x.CreatedAt);

        builder.Entity<SystemLog>()
            .HasIndex(x => x.Category);


        // =================================================
        // NOTIFICATION
        // =================================================

        builder.Entity<Notification>()
            .HasIndex(x => x.CreatedAt);

        builder.Entity<Notification>()
            .HasIndex(x => x.IsActive);

        builder.Entity<Notification>()
            .HasIndex(x => x.SurveyId);


        // =================================================
        // USER NOTIFICATION
        // =================================================

        // Bir Notification silinirse ona bağlı kullanıcı
        // bildirim kayıtları da silinsin.
        builder.Entity<UserNotification>()
            .HasOne(x => x.Notification)
            .WithMany(x => x.UserNotifications)
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);


        // Kullanıcı silinirse UserNotification kaydı için
        // SQL Server'da olası multiple cascade path sorununu
        // önlemek amacıyla Cascade yerine NoAction kullanıyoruz.
        builder.Entity<UserNotification>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);


        // Aynı bildirimin aynı kullanıcıya iki kez eklenmesini engeller.
        builder.Entity<UserNotification>()
            .HasIndex(x => new
            {
                x.NotificationId,
                x.UserId
            })
            .IsUnique();


        // Bildirim listesi sorgularını hızlandırır.
        builder.Entity<UserNotification>()
            .HasIndex(x => new
            {
                x.UserId,
                x.IsRead
            });

        builder.Entity<Event>().HasIndex(x => new { x.IsPublished, x.StartsAt, x.EndsAt });
        builder.Entity<Event>()
            .HasOne(x => x.Survey)
            .WithMany()
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
