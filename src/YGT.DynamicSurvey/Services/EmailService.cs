using System.Net;
using System.Net.Mail;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Smtp:Host"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Smtp:Username"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Smtp:Password"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Smtp:From"] ?? _configuration["Smtp:Username"]);

    public async Task<int> SendEventAnnouncementAsync(Event item, IEnumerable<ApplicationUser> users)
    {
        var host = _configuration["Smtp:Host"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? username;
        if (!IsConfigured)
        {
            _logger.LogWarning("Etkinlik e-postaları gönderilemedi: SMTP yapılandırması eksik.");
            return 0;
        }

        using var client = new SmtpClient(host, _configuration.GetValue("Smtp:Port", 587))
        {
            EnableSsl = _configuration.GetValue("Smtp:EnableSsl", true),
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };

        var sentCount = 0;
        var baseUrl = (_configuration["Application:BaseUrl"] ?? "http://localhost:5037").TrimEnd('/');
        foreach (var user in users.Where(x => !string.IsNullOrWhiteSpace(x.Email)))
        {
            try
            {
                using var message = new MailMessage(from!, user.Email!);
                message.Subject = $"Yeni etkinlik: {item.Title}";
                message.IsBodyHtml = true;
                message.Body = $"""
                    <div style="font-family:Arial,sans-serif;max-width:620px;margin:auto;color:#15323a">
                      <div style="padding:24px;background:#08212c;color:#fff;border-radius:16px 16px 0 0"><b style="color:#42d7ce">YGT KKU</b><h1 style="margin:10px 0 0;font-size:25px">{WebUtility.HtmlEncode(item.Title)}</h1></div>
                      <div style="padding:26px;border:1px solid #d8e8e8;border-top:0;border-radius:0 0 16px 16px">
                        <p>Merhaba {WebUtility.HtmlEncode(user.FullName)},</p><p>{WebUtility.HtmlEncode(item.Summary)}</p>
                        <p><b>Başlangıç:</b> {item.StartsAt:dd.MM.yyyy HH:mm}<br><b>Konum:</b> {WebUtility.HtmlEncode(item.Location ?? "Daha sonra duyurulacak")}</p>
                        <a href="{baseUrl}/Announcement/Detail/{item.Id}" style="display:inline-block;padding:12px 18px;background:#24c8c0;color:#052b2e;text-decoration:none;border-radius:9px;font-weight:bold">Etkinliği Görüntüle</a>
                        <p style="margin-top:24px;color:#71878c;font-size:12px">Bu e-posta, profilinizde etkinlik duyurularına izin verdiğiniz için gönderildi.</p>
                      </div>
                    </div>
                    """;
                await client.SendMailAsync(message);
                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Etkinlik e-postası {Email} adresine gönderilemedi.", user.Email);
            }
        }
        return sentCount;
    }

    public async Task<bool> SendPasswordResetAsync(ApplicationUser user, string resetUrl)
    {
        var host = _configuration["Smtp:Host"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? username;
        if (!IsConfigured || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Şifre sıfırlama e-postası gönderilemedi: SMTP yapılandırması eksik.");
            return false;
        }
        try
        {
            using var client = new SmtpClient(host, _configuration.GetValue("Smtp:Port", 587)) { EnableSsl = _configuration.GetValue("Smtp:EnableSsl", true), Credentials = new NetworkCredential(username, password) };
            using var message = new MailMessage(from!, user.Email) { Subject = "YGT KKU şifre sıfırlama", IsBodyHtml = true, Body = $"<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'><h2>Şifreni sıfırla</h2><p>Merhaba {WebUtility.HtmlEncode(user.FullName)},</p><p>Şifreni yenilemek için aşağıdaki bağlantıyı kullan.</p><p><a href='{WebUtility.HtmlEncode(resetUrl)}' style='display:inline-block;padding:12px 18px;background:#24c8c0;color:#052f32;text-decoration:none;border-radius:9px;font-weight:bold'>Şifremi Sıfırla</a></p><small>Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin.</small></div>" };
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Şifre sıfırlama e-postası gönderilemedi."); return false; }
    }

    public async Task<int> SendAdminApplicationAsync(
        ApplicationUser applicant,
        IEnumerable<ApplicationUser> managers)
    {
        if (!IsConfigured) return 0;

        var host = _configuration["Smtp:Host"]!;
        var username = _configuration["Smtp:Username"]!;
        var password = _configuration["Smtp:Password"]!;
        var from = _configuration["Smtp:From"] ?? username;
        var baseUrl = (_configuration["Application:BaseUrl"] ?? "http://localhost:5037").TrimEnd('/');
        var recipients = managers
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.NormalizedEmail ?? x.Email!.ToUpperInvariant())
            .Select(x => x.First())
            .ToList();
        var sent = 0;

        using var client = new SmtpClient(host, _configuration.GetValue("Smtp:Port", 587))
        {
            EnableSsl = _configuration.GetValue("Smtp:EnableSsl", true),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };

        foreach (var manager in recipients)
        {
            try
            {
                using var message = new MailMessage(from!, manager.Email!)
                {
                    Subject = $"Yeni yönetici başvurusu: {applicant.FullName}",
                    IsBodyHtml = true,
                    Body = $"<div style='font-family:Arial,sans-serif;max-width:620px;margin:auto;color:#15323a'><div style='padding:24px;background:#08212c;color:#fff;border-radius:16px 16px 0 0'><b style='color:#42d7ce'>YGT KKU</b><h1>Yeni yönetici başvurusu</h1></div><div style='padding:26px;border:1px solid #d8e8e8'><p>Merhaba {WebUtility.HtmlEncode(manager.FullName)},</p><p><b>{WebUtility.HtmlEncode(applicant.FullName)}</b> yönetici hesabı için başvurdu.</p><p><b>E-posta:</b> {WebUtility.HtmlEncode(applicant.Email)}</p><a href='{baseUrl}/Admin/Users?status=Pending' style='display:inline-block;padding:12px 18px;background:#24c8c0;color:#052b2e;text-decoration:none;border-radius:9px;font-weight:bold'>Başvuruyu İncele</a></div></div>"
                };
                await client.SendMailAsync(message);
                sent++;
                _logger.LogInformation("Yönetici başvurusu e-postası {Email} adresine gönderildi.", manager.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yönetici başvurusu e-postası {Email} adresine gönderilemedi.", manager.Email);
            }
        }

        return sent;
    }

    public async Task<int> SendSurveyAnnouncementAsync(Survey survey, IEnumerable<ApplicationUser> users)
    {
        var host = _configuration["Smtp:Host"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? username;
        if (!IsConfigured)
        {
            _logger.LogWarning("Yeni anket e-postası gönderilemedi: SMTP yapılandırması eksik.");
            return 0;
        }

        using var client = new SmtpClient(host!, _configuration.GetValue("Smtp:Port", 587))
        {
            EnableSsl = _configuration.GetValue("Smtp:EnableSsl", true),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };
        var baseUrl = (_configuration["Application:BaseUrl"] ?? "http://localhost:5037").TrimEnd('/');
        var sent = 0;
        foreach (var user in users.Where(x => !string.IsNullOrWhiteSpace(x.Email)))
        {
            try
            {
                using var message = new MailMessage(from!, user.Email!)
                {
                    Subject = $"Yeni YGT anketi: {survey.Title}",
                    IsBodyHtml = true,
                    Body = $"<div style='font-family:Arial,sans-serif;max-width:620px;margin:auto;color:#15323a'><div style='padding:24px;background:#08212c;color:#fff;border-radius:16px 16px 0 0'><b style='color:#42d7ce'>YGT KKU</b><h1>{WebUtility.HtmlEncode(survey.Title)}</h1></div><div style='padding:26px;border:1px solid #d8e8e8'><p>Merhaba {WebUtility.HtmlEncode(user.FullName)},</p><p>{WebUtility.HtmlEncode(survey.Description ?? "Yeni topluluk anketimiz yayında.")}</p><a href='{baseUrl}/Survey/Join?code={survey.Code}' style='display:inline-block;padding:12px 18px;background:#24c8c0;color:#052b2e;text-decoration:none;border-radius:9px;font-weight:bold'>Ankete Katıl</a><p style='margin-top:24px;color:#71878c;font-size:12px'>Bu e-posta, profilinizde topluluk duyurularına izin verdiğiniz için gönderildi.</p></div></div>"
                };
                await client.SendMailAsync(message);
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Anket e-postası {Email} adresine gönderilemedi.", user.Email);
            }
        }
        return sent;
    }
}
