using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Services;

public static class DemoSurveySeeder
{
    public static async Task<int> SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var owner = await users.FindByEmailAsync("ygtkku@gmail.com");
        var now = DateTime.Now;
        var created = 0;

        var definitions = new List<(string Title, string Description, List<Question> Questions)>
        {
            ("Yazılım Geliştirme Topluluğundan 2026-2027 Eğitim Öğretim Yılında Beklentileriniz Nedir?",
             "Yeni dönemde eğitim, etkinlik, proje ve topluluk deneyimini birlikte planlayalım.",
             new()
             {
                 Choice(1, "Bu yıl topluluğumuzdan en çok hangi alanda içerik bekliyorsun?", true, "Yapay zekâ ve veri bilimi", "Web geliştirme", "Mobil uygulama", "Siber güvenlik", "Oyun geliştirme", "Kariyer gelişimi"),
                 Multi(2, "Katılmak istediğin etkinlik türlerini seçer misin?", true, "Uygulamalı atölye", "Teknik gezi", "Hackathon", "Sektör söyleşisi", "Proje takımı", "Sosyal buluşma"),
                 Rating(3, "YGT KKU'nun geçen dönemki çalışmalarını genel olarak değerlendirir misin?"),
                 YesNo(4, "Bu yıl aktif bir proje ekibinde görev almak ister misin?"),
                 Text(5, "İlgilendiğin proje fikrini kısaca anlatır mısın?", QuestionType.LongText, false, 4, "Evet"),
                 Choice(6, "Etkinlikler için sana en uygun zaman hangisi?", true, "Hafta içi öğle arası", "Hafta içi ders sonrası", "Cumartesi", "Pazar"),
                 Text(7, "Bu yıl mutlaka yapılmasını istediğin bir etkinlik veya konuşmacı önerin var mı?", QuestionType.LongText, false)
             }),

            ("SAYZEK Zirvesi Değerlendirme Anketi",
             "SAYZEK Zirvesi deneyimini değerlendir; sonraki zirveyi birlikte daha güçlü hale getirelim.",
             new()
             {
                 Rating(1, "Zirveden genel olarak ne kadar memnun kaldın?"),
                 Likert(2, "Konuşmalar güncel ve faydalı içerikler sundu."),
                 Rating(3, "Konuşmacıların anlatım kalitesini değerlendirir misin?"),
                 Choice(4, "En faydalı bulduğun oturum türü hangisiydi?", true, "Teknik sunum", "Kariyer söyleşisi", "Panel", "Proje tanıtımı", "Networking"),
                 YesNo(5, "Bir sonraki SAYZEK Zirvesi'ne tekrar katılmak ister misin?"),
                 Text(6, "Hayır cevabının temel nedenini paylaşır mısın?", QuestionType.LongText, false, 5, "Hayır"),
                 Text(7, "Bir sonraki zirvede hangi konuyu veya konuşmacıyı görmek istersin?", QuestionType.LongText, false)
             }),

            ("HAVELSAN Teknik Gezisi Değerlendirme Anketi",
             "HAVELSAN teknik gezisinin organizasyonunu ve mesleki katkısını değerlendirebilirsin.",
             new()
             {
                 Rating(1, "Teknik gezinin mesleki gelişimine katkısını değerlendirir misin?"),
                 Likert(2, "HAVELSAN'daki sunumlar ve teknik anlatımlar beklentimi karşıladı."),
                 Rating(3, "Ulaşım ve organizasyon sürecinden ne kadar memnun kaldın?"),
                 Choice(4, "Gezide en çok hangi bölüm ilgini çekti?", true, "Simülasyon teknolojileri", "Savunma yazılımları", "Siber güvenlik", "Komuta kontrol sistemleri", "Kariyer olanakları"),
                 YesNo(5, "HAVELSAN'da staj veya kariyer fırsatlarıyla ilgileniyor musun?"),
                 Text(6, "İlgilendiğin çalışma alanını belirtir misin?", QuestionType.ShortText, false, 5, "Evet"),
                 Text(7, "Gelecekte düzenlenmesini istediğin başka bir teknik gezi var mı?", QuestionType.LongText, false)
             }),

            ("YGT Eğitim ve Atölye Planlama Anketi",
             "Yeni dönemin teknik eğitim takvimini ilgi alanlarına ve seviyene göre hazırlayalım.",
             new()
             {
                 Choice(1, "Yazılım geliştirme seviyeni nasıl tanımlarsın?", true, "Yeni başlıyorum", "Temel", "Orta", "İleri"),
                 Multi(2, "Hangi eğitim başlıkları ilgini çekiyor?", true, "C# ve .NET", "Python", "JavaScript ve React", "Mobil geliştirme", "Yapay zekâ", "DevOps ve bulut", "Siber güvenlik"),
                 Choice(3, "Tercih ettiğin eğitim biçimi nedir?", true, "Yüz yüze uygulamalı", "Çevrim içi canlı", "Hibrit", "Kayıtlı video + mentorluk"),
                 Rating(4, "Haftalık düzenli eğitime katılma motivasyonunu değerlendirir misin?"),
                 YesNo(5, "Eğitim sonunda ekip projesi geliştirmek ister misin?"),
                 Text(6, "Geliştirmek istediğin proje türünü anlatır mısın?", QuestionType.LongText, false, 5, "Evet"),
                 Text(7, "Eğitmen veya içerik önerin varsa paylaşabilirsin.", QuestionType.LongText, false)
             }),

            ("YGT KKU Üye Deneyimi ve İletişim Anketi",
             "Topluluk iletişimini, duyuruları ve üyelik deneyimini daha kullanıcı dostu hale getirelim.",
             new()
             {
                 Rating(1, "Topluluğun genel iletişiminden ne kadar memnunsun?"),
                 Multi(2, "Duyuruları hangi kanallardan almak istersin?", true, "E-posta", "Instagram", "LinkedIn", "WhatsApp", "Site bildirimleri"),
                 Likert(3, "Etkinlik duyuruları bana yeterince erken ulaşıyor."),
                 Choice(4, "Topluluk içinde kendini ne kadar aktif hissediyorsun?", true, "Çok aktif", "Ara sıra katılıyorum", "Takip ediyorum ama katılamıyorum", "Yeni üyeyim"),
                 YesNo(5, "Organizasyon ekibinde gönüllü görev almak ister misin?"),
                 Multi(6, "Hangi ekiplerde görev almak istersin?", false, 5, "Evet", "Yazılım ve teknik ekip", "Etkinlik organizasyonu", "Sosyal medya", "Tasarım", "Sponsorluk ve iletişim"),
                 Text(7, "YGT KKU deneyimini iyileştirecek en önemli önerin nedir?", QuestionType.LongText, false)
             })
        };

        foreach (var definition in definitions)
        {
            if (await db.Surveys.AnyAsync(x => x.Title == definition.Title)) continue;
            var survey = new Survey
            {
                Title = definition.Title,
                Description = definition.Description,
                Code = await UniqueCodeAsync(db),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                StartDate = now.AddMinutes(-5),
                EndDate = now.AddDays(60),
                CreatedByUserId = owner?.Id,
                Questions = definition.Questions
            };
            db.Surveys.Add(survey);
            created++;
        }

        await db.SaveChangesAsync();
        return created;
    }

    private static Question Base(int order, string text, QuestionType type, bool required = true) =>
        new() { Order = order, Text = text, Type = type, IsRequired = required, RatingMaxValue = type == QuestionType.Rating ? 5 : null };
    private static Question Rating(int order, string text) => Base(order, text, QuestionType.Rating);
    private static Question Text(int order, string text, QuestionType type, bool required, int? depends = null, string? answer = null)
    {
        var q = Base(order, text, type, required); q.DependsOnQuestionOrder = depends; q.ConditionOperator = depends.HasValue ? BranchConditionOperator.Equals : null; q.ShowWhenAnswerEquals = answer; return q;
    }
    private static Question Choice(int order, string text, bool required, params string[] options) => WithOptions(Base(order, text, QuestionType.SingleChoice, required), options);
    private static Question Multi(int order, string text, bool required, params string[] options) => WithOptions(Base(order, text, QuestionType.MultipleChoice, required), options);
    private static Question Multi(int order, string text, bool required, int depends, string answer, params string[] options)
    {
        var q = Multi(order, text, required, options); q.DependsOnQuestionOrder = depends; q.ConditionOperator = BranchConditionOperator.Equals; q.ShowWhenAnswerEquals = answer; return q;
    }
    private static Question YesNo(int order, string text) => WithOptions(Base(order, text, QuestionType.YesNo), new[] { "Evet", "Hayır" });
    private static Question Likert(int order, string text) => WithOptions(Base(order, text, QuestionType.Likert), new[] { "Kesinlikle katılmıyorum", "Katılmıyorum", "Kararsızım", "Katılıyorum", "Kesinlikle katılıyorum" });
    private static Question WithOptions(Question q, IEnumerable<string> options)
    {
        q.Options = options.Select((text, index) => new QuestionOption { Text = text, Order = index + 1 }).ToList(); return q;
    }
    private static async Task<string> UniqueCodeAsync(ApplicationDbContext db)
    {
        string code; do { code = Random.Shared.Next(100000, 1000000).ToString(); } while (await db.Surveys.AnyAsync(x => x.Code == code)); return code;
    }
}
