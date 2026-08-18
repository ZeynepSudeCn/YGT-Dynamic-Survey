using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Services;

public static class CommunityContentSeeder
{
    public static async Task<(int Events, int Notifications, int Emails)> SeedAndPublishAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var email = services.GetRequiredService<EmailService>();
        var surveys = await db.Surveys.ToListAsync();
        var now = DateTime.Now;
        const string sayzekGallery = "/uploads/events/sayzek-gercek-1.png|/uploads/events/sayzek-gercek-2.png|/uploads/events/sayzek-gercek-3.png|/uploads/events/sayzek-gercek-4.png|/uploads/events/sayzek-gercek-5.png";
        const string havelsanGallery = "/uploads/events/havelsan-gercek-1.png|/uploads/events/havelsan-gercek-2.png";
        const string bursaGallery = "/uploads/events/bursa-gezisi-1.png|/uploads/events/bursa-gezisi-2.png|/uploads/events/bursa-gezisi-3.png";

        Survey? FindSurvey(string part) => surveys.FirstOrDefault(x => x.Title.Contains(part, StringComparison.OrdinalIgnoreCase));
        var definitions = new[]
        {
            new Event { Title="SAYZEK 2025: Yapay Zekâ ve Yazılım Zirvesi", Category="Zirve", Summary="Yapay zekâ, yazılım dünyası ve kariyer fırsatlarını konuştuğumuz ilham dolu zirve.", Description="Alanında uzman konuşmacılar, öğrenci proje sunumları ve sektör buluşmalarıyla dolu SAYZEK 2025'i başarıyla tamamladık. Katılımcılar güncel yapay zekâ uygulamalarını, yazılım kariyer yollarını ve gerçek proje deneyimlerini keşfetti.", StartsAt=new DateTime(2026,5,16,10,0,0), EndsAt=new DateTime(2026,5,16,18,0,0), Location="Kırıkkale Üniversitesi Mavi Salon", ImageUrl=sayzekGallery, Survey=FindSurvey("SAYZEK Zirvesi") },
            new Event { Title="HAVELSAN Teknoloji Kampüsü Teknik Gezisi", Category="Teknik Gezi", Summary="Savunma yazılımları, simülasyon ve mühendislik süreçlerini yerinde inceledik.", Description="HAVELSAN teknoloji kampüsünde gerçekleştirilen gezide simülasyon teknolojileri, siber güvenlik, komuta kontrol sistemleri ve yazılım mühendisliği ekiplerinin çalışma biçimlerini yakından tanıdık.", StartsAt=new DateTime(2026,6,20,8,0,0), EndsAt=new DateTime(2026,6,20,19,0,0), Location="HAVELSAN Teknoloji Kampüsü, Ankara", ImageUrl=havelsanGallery, Survey=FindSurvey("HAVELSAN Teknik") },
            new Event { Title="YGT KKU Yaz Kodla Üret Hackathonu", Category="Hackathon", Summary="Takımlar 24 saat boyunca topluluk sorunlarına yenilikçi yazılım çözümleri geliştirdi.", Description="Farklı seviyelerden öğrenciler ekipler kurarak fikirlerini çalışan prototiplere dönüştürdü. Mentorluk oturumları, kod incelemeleri ve final sunumlarıyla üretken bir hafta sonu geçirdik.", StartsAt=new DateTime(2026,7,11,10,0,0), EndsAt=new DateTime(2026,7,12,12,0,0), Location="Kırıkkale Üniversitesi Teknoloji Merkezi", ImageUrl="/uploads/events/ygt-hackathon-2026.png" },
            new Event { Title="YGT KKU Bursa Kültür ve Teknoloji Gezisi", Category="Topluluk Gezisi", Summary="Bursa'nın tarihî dokusunu keşfettiğimiz, üyelerimizle bağlarımızı güçlendirdiğimiz keyifli topluluk gezisi.", Description="YGT KKU üyeleriyle Bursa'nın tarihî ve kültürel noktalarını birlikte keşfettik. Tophane, Cumalıkızık ve şehrin simge duraklarını kapsayan gezi; yeni arkadaşlıklar, ekip ruhu ve unutulmaz anılarla tamamlandı.", StartsAt=new DateTime(2026,7,26,7,0,0), EndsAt=new DateTime(2026,7,27,23,0,0), Location="Bursa", ImageUrl=bursaGallery },
            new Event { Title="Git ve GitHub ile Takım Çalışması Kampı", Category="Atölye", Summary="Branch, pull request ve kod inceleme pratiği yaptığımız uygulamalı yazılım kampı.", Description="Katılımcılar gerçek bir takım deposunda görev alarak Git akışını, anlaşılır commit yazmayı, pull request hazırlamayı ve yapıcı kod incelemesi yapmayı uygulamalı olarak öğreniyor.", StartsAt=now.AddHours(-1), EndsAt=now.AddHours(3), Location="YGT KKU Topluluk Odası", ImageUrl="/uploads/events/ygt-hackathon-2026.png" },
            new Event { Title="SAYZEK 2026 Yazılım ve Yapay Zekâ Zirvesi", Category="Zirve", Summary="Yeni nesil yapay zekâ, ürün geliştirme ve teknoloji kariyerleri aynı sahnede.", Description="SAYZEK 2026; akademisyenleri, sektör profesyonellerini ve teknoloji üreten öğrencileri bir araya getiriyor. Teknik oturumlar, kariyer paneli, öğrenci projeleri ve networking alanıyla dolu bir gün seni bekliyor.", StartsAt=new DateTime(2026,8,20,10,0,0), EndsAt=new DateTime(2026,8,20,18,0,0), Location="Kırıkkale Üniversitesi Mavi Salon", ImageUrl=sayzekGallery, Survey=FindSurvey("SAYZEK Zirvesi") },
            new Event { Title="Modern Web Geliştirme: ASP.NET Core Atölyesi", Category="Eğitim", Summary="MVC, veritabanı, kimlik doğrulama ve modern arayüzü tek projede birleştiriyoruz.", Description="Başlangıçtan çalışan ürüne uzanan uygulamalı atölyede ASP.NET Core MVC, Entity Framework Core, Identity, responsive tasarım ve yayınlama adımlarını birlikte tamamlayacağız.", StartsAt=new DateTime(2026,8,27,17,30,0), EndsAt=new DateTime(2026,8,27,20,30,0), Location="Mühendislik Fakültesi Bilgisayar Laboratuvarı", ImageUrl="/uploads/events/ygt-hackathon-2026.png", Survey=FindSurvey("Eğitim ve Atölye") },
            new Event { Title="Siber Güvenliğe Giriş ve Capture The Flag", Category="Atölye", Summary="Temel güvenlik kavramlarını öğrenip başlangıç seviyesi CTF görevlerini çözüyoruz.", Description="Web güvenliği, parola güvenliği, sosyal mühendislik ve güvenli yazılım geliştirme başlıklarının ardından takımlar halinde başlangıç seviyesi Capture The Flag senaryoları çözülecek.", StartsAt=new DateTime(2026,9,5,13,0,0), EndsAt=new DateTime(2026,9,5,17,0,0), Location="Kırıkkale Üniversitesi Teknoloji Merkezi", ImageUrl="/uploads/events/ygt-teknik-gezi-2026.png" }
        };

        var newEvents = new List<Event>();
        foreach (var definition in definitions)
        {
            var existing = await db.Events.FirstOrDefaultAsync(x => x.Title == definition.Title);
            if (existing is not null)
            {
                if (existing.Title.Contains("SAYZEK", StringComparison.OrdinalIgnoreCase))
                    existing.ImageUrl = sayzekGallery;
                else if (existing.Title.Contains("HAVELSAN", StringComparison.OrdinalIgnoreCase))
                    existing.ImageUrl = havelsanGallery;
                else if (existing.Title.Contains("Bursa", StringComparison.OrdinalIgnoreCase))
                    existing.ImageUrl = bursaGallery;
                continue;
            }
            definition.IsPublished = true;
            definition.CreatedAt = DateTime.UtcNow;
            db.Events.Add(definition);
            newEvents.Add(definition);
        }
        await db.SaveChangesAsync();

        var activeUsers = await users.Users.Where(x => x.IsActive).ToListAsync();
        var mailUsers = activeUsers.Where(x => x.AnnouncementConsent).ToList();
        var notificationCount = 0;
        var emailCount = 0;

        foreach (var survey in surveys)
        {
            if (await db.Notifications.AnyAsync(x => x.SurveyId == survey.Id)) continue;
            var notification = new Notification { Title="Yeni anket yayında", Message=$"“{survey.Title}” anketine katılarak fikrini paylaşabilirsin.", Url=$"/Survey/Join?code={survey.Code}", Type="Survey", SurveyId=survey.Id, CreatedAt=DateTime.UtcNow, IsActive=true };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            db.UserNotifications.AddRange(activeUsers.Select(x => new UserNotification { NotificationId=notification.Id, UserId=x.Id }));
            await db.SaveChangesAsync();
            notificationCount++;
            emailCount += await email.SendSurveyAnnouncementAsync(survey, mailUsers);
        }

        foreach (var item in newEvents.Where(x => x.EndsAt >= now))
        {
            if (await db.Notifications.AnyAsync(x => x.EventId == item.Id)) continue;
            var notification = new Notification { Title="Yeni etkinlik: " + item.Title, Message=item.Summary, Url=$"/Announcement/Detail/{item.Id}", Type="Event", EventId=item.Id, CreatedAt=DateTime.UtcNow, IsActive=true };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            db.UserNotifications.AddRange(activeUsers.Select(x => new UserNotification { NotificationId=notification.Id, UserId=x.Id }));
            await db.SaveChangesAsync();
            notificationCount++;
            emailCount += await email.SendEventAnnouncementAsync(item, mailUsers);
        }

        return (newEvents.Count, notificationCount, emailCount);
    }
}
