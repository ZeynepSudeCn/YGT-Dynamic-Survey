using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;
using YGT.DynamicSurvey.Models.ViewModels.Survey;
using YGT.DynamicSurvey.Services;

namespace YGT.DynamicSurvey.Controllers;

public class SurveyController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SystemLogService _systemLogService;
    private readonly NotificationService _notificationService;

    public SurveyController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SystemLogService systemLogService,
        NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _systemLogService = systemLogService;
        _notificationService = notificationService;
    }


    // =====================================================
    // ANKET KODU İLE KATIL
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Join(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["SurveyError"] =
                "Lütfen 6 haneli anket kodunu giriniz.";

            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        code = code.Trim();

        var survey =
            await _context.Surveys
                .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x =>
                    x.Code == code &&
                    x.IsActive
                );

        if (survey is null)
        {
            TempData["SurveyError"] =
                "Bu kodla eşleşen aktif bir anket bulunamadı.";

            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        var now = DateTime.Now;

        if (
            survey.StartDate != default &&
            now < survey.StartDate
        )
        {
            TempData["SurveyError"] =
                "Bu anket henüz katılıma açılmamıştır.";

            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        if (
            survey.EndDate != default &&
            now > survey.EndDate
        )
        {
            TempData["SurveyError"] =
                "Bu anketin katılım süresi sona ermiştir.";

            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user is not null)
            {
                var alreadyParticipated =
                    await _context.SurveyResponses
                        .AnyAsync(x =>
                            x.SurveyId == survey.Id &&
                            x.UserId == user.Id
                        );

                if (alreadyParticipated)
                {
                    TempData["SurveyError"] =
                        "Bu ankete daha önce katıldınız.";

                    return RedirectToAction(
                        "History",
                        "Participation"
                    );
                }
            }
        }

        var model =
            BuildTakeSurveyViewModel(
                survey
            );

        return View(model);
    }


    // =====================================================
    // ANKET CEVAPLARINI KAYDET
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        TakeSurveyViewModel model)
    {
        var survey =
            await _context.Surveys
                .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x =>
                    x.Id == model.SurveyId &&
                    x.IsActive
                );

        if (survey is null)
        {
            return NotFound();
        }

        var now = DateTime.Now;

        if (
            survey.StartDate != default &&
            now < survey.StartDate
        )
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu anket henüz katılıma açılmamıştır."
            );
        }

        if (
            survey.EndDate != default &&
            now > survey.EndDate
        )
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu anketin katılım süresi sona ermiştir."
            );
        }

        model.Answers ??=
            new List<SurveyAnswerInputViewModel>();


        // =================================================
        // SADECE GÖRÜNÜR SORULARI DOĞRULA
        // =================================================

        foreach (
            var question in survey.Questions
                .OrderBy(x => x.Order))
        {
            if (
                !IsQuestionVisible(
                    question,
                    survey,
                    model.Answers
                )
            )
            {
                continue;
            }

            var submittedAnswer =
                model.Answers
                    .FirstOrDefault(x =>
                        x.QuestionId == question.Id
                    );

            var hasValue =
                !string.IsNullOrWhiteSpace(
                    submittedAnswer?.Value
                );

            var hasOptions =
                submittedAnswer?.SelectedOptionIds is not null &&
                submittedAnswer.SelectedOptionIds.Count > 0;

            if (question.IsRequired)
            {
                var answered =
                    question.Type switch
                    {
                        QuestionType.SingleChoice => hasOptions,
                        QuestionType.MultipleChoice => hasOptions,
                        QuestionType.Likert => hasOptions,
                        QuestionType.YesNo => hasOptions,
                        QuestionType.ShortText => hasValue,
                        QuestionType.LongText => hasValue,
                        QuestionType.Number => hasValue,
                        QuestionType.Rating => hasValue,
                        _ => false
                    };

                if (!answered)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"\"{question.Text}\" sorusu zorunludur."
                    );
                }
            }

            if (
                question.Type == QuestionType.Number &&
                hasValue
            )
            {
                var numberText =
                    submittedAnswer!.Value!.Trim();

                var isNumber =
                    decimal.TryParse(
                        numberText,
                        NumberStyles.Any,
                        CultureInfo.CurrentCulture,
                        out _
                    )
                    ||
                    decimal.TryParse(
                        numberText,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out _
                    );

                if (!isNumber)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"\"{question.Text}\" sorusuna geçerli bir sayı giriniz."
                    );
                }
            }

            if (
                question.Type == QuestionType.Rating &&
                hasValue
            )
            {
                var maxValue =
                    question.RatingMaxValue ?? 5;

                if (
                    !int.TryParse(
                        submittedAnswer!.Value,
                        out var ratingValue
                    )
                    ||
                    ratingValue < 1
                    ||
                    ratingValue > maxValue
                )
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"\"{question.Text}\" sorusu için 1-{maxValue} arasında bir puan seçiniz."
                    );
                }
            }

            if (
                submittedAnswer?.SelectedOptionIds is not null &&
                submittedAnswer.SelectedOptionIds.Count > 0
            )
            {
                var validOptionIds =
                    question.Options
                        .Select(x => x.Id)
                        .ToHashSet();

                var hasInvalidOption =
                    submittedAnswer.SelectedOptionIds
                        .Any(id =>
                            !validOptionIds.Contains(id)
                        );

                if (hasInvalidOption)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Geçersiz bir seçenek gönderildi."
                    );
                }
            }
        }


        if (!ModelState.IsValid)
        {
            var invalidModel =
                BuildTakeSurveyViewModel(
                    survey,
                    model.Answers
                );

            return View(
                "Join",
                invalidModel
            );
        }


        string? currentUserId = null;
        ApplicationUser? currentUser = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser is not null)
            {
                currentUserId =
                    currentUser.Id;

                var alreadyParticipated =
                    await _context.SurveyResponses
                        .AnyAsync(x =>
                            x.SurveyId == survey.Id &&
                            x.UserId == currentUserId
                        );

                if (alreadyParticipated)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Bu ankete daha önce katıldınız."
                    );

                    return View(
                        "Join",
                        BuildTakeSurveyViewModel(
                            survey,
                            model.Answers
                        )
                    );
                }
            }
        }


        var surveyResponse =
            new SurveyResponse
            {
                SurveyId =
                    survey.Id,

                SubmittedAt =
                    DateTime.UtcNow,

                UserId =
                    currentUserId
            };


        // =================================================
        // SADECE GERÇEKTEN GÖRÜNEN SORULARI KAYDET
        // =================================================

        foreach (
            var question in survey.Questions
                .OrderBy(x => x.Order))
        {
            if (
                !IsQuestionVisible(
                    question,
                    survey,
                    model.Answers
                )
            )
            {
                continue;
            }

            var submittedAnswer =
                model.Answers
                    .FirstOrDefault(x =>
                        x.QuestionId == question.Id
                    );

            if (submittedAnswer is null)
            {
                continue;
            }

            var answer =
                new Answer
                {
                    QuestionId =
                        question.Id,

                    Value =
                        string.IsNullOrWhiteSpace(
                            submittedAnswer.Value
                        )
                            ? null
                            : submittedAnswer.Value.Trim()
                };

            if (
                submittedAnswer.SelectedOptionIds is not null &&
                submittedAnswer.SelectedOptionIds.Count > 0
            )
            {
                var validIds =
                    question.Options
                        .Select(x => x.Id)
                        .ToHashSet();

                var selectedIds =
                    submittedAnswer.SelectedOptionIds
                        .Where(id =>
                            validIds.Contains(id)
                        )
                        .Distinct()
                        .ToList();

                if (selectedIds.Count > 0)
                {
                    answer.SelectedOptionIds =
                        string.Join(
                            ",",
                            selectedIds
                        );
                }
            }

            if (
                string.IsNullOrWhiteSpace(answer.Value) &&
                string.IsNullOrWhiteSpace(answer.SelectedOptionIds)
            )
            {
                continue;
            }

            surveyResponse.Answers.Add(
                answer
            );
        }

        _context.SurveyResponses.Add(
            surveyResponse
        );

        await _context.SaveChangesAsync();


        await _systemLogService.LogAsync(
            "Anket Yanıtlandı",
            currentUser is null
                ? $"\"{survey.Title}\" anketine anonim bir yanıt gönderildi."
                : $"{currentUser.FullName}, \"{survey.Title}\" anketini yanıtladı.",
            "Survey",
            currentUser
        );


        return RedirectToAction(
            nameof(Tesekkur),
            new
            {
                code = survey.Code
            }
        );
    }


    // =====================================================
    // TEŞEKKÜR EKRANI
    // =====================================================

    [HttpGet]
    public IActionResult Tesekkur(
        string? code)
    {
        ViewBag.SurveyCode =
            code;

        return View();
    }


    // =====================================================
    // YENİ ANKET - GET
    // SADECE YÖNETİCİ
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public IActionResult Create(string? template)
    {
        var model = BuildSurveyTemplate(template);
        ViewBag.SelectedTemplate = template ?? "blank";

        return View(model);
    }

    private static CreateSurveyViewModel BuildSurveyTemplate(string? template)
    {
        var model = new CreateSurveyViewModel
        {
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(7)
        };

        void Add(string text, QuestionType type, bool required = true, params string[] options) =>
            model.Questions.Add(new CreateQuestionViewModel
            {
                Order = model.Questions.Count + 1,
                Text = text,
                Type = type,
                IsRequired = required,
                RatingMaxValue = type == QuestionType.Rating ? 5 : null,
                Options = options.ToList()
            });

        switch (template?.Trim().ToLowerInvariant())
        {
            case "education":
                model.Title = "Eğitim Değerlendirme Anketi";
                model.Description = "Katıldığınız eğitimi değerlendirerek gelişmemize yardımcı olun.";
                Add("Eğitimin genel faydasını değerlendirir misiniz?", QuestionType.Rating);
                Add("Eğitmenin anlatımı anlaşılır mıydı?", QuestionType.Likert);
                Add("Bir sonraki eğitimde hangi konuyu görmek istersiniz?", QuestionType.LongText, false);
                break;
            case "event":
                model.Title = "Etkinlik Memnuniyet Anketi";
                model.Description = "Etkinlik deneyiminizi bizimle paylaşın.";
                Add("Etkinlikten ne kadar keyif aldınız?", QuestionType.Rating);
                Add("Etkinlik beklentinizi karşıladı mı?", QuestionType.YesNo);
                Add("Geliştirmemizi istediğiniz bir alan var mı?", QuestionType.LongText, false);
                break;
            case "trip":
                model.Title = "Teknik Gezi Değerlendirme Anketi";
                model.Description = "Teknik gezi deneyiminizi değerlendirin.";
                Add("Gezinin mesleki katkısını değerlendirir misiniz?", QuestionType.Rating);
                Add("Ulaşım ve organizasyondan memnun kaldınız mı?", QuestionType.Likert);
                Add("Sonraki teknik gezi için öneriniz nedir?", QuestionType.LongText, false);
                break;
            case "suggestion":
                model.Title = "YGT Öneri Formu";
                model.Description = "Topluluğumuzu geliştirecek fikrini paylaş.";
                Add("Önerinin konusu nedir?", QuestionType.ShortText);
                Add("Önerini ayrıntılı biçimde anlatır mısın?", QuestionType.LongText);
                Add("Bu öneri ne kadar öncelikli?", QuestionType.Rating);
                break;
            default:
                Add("", QuestionType.SingleChoice, true, "", "");
                break;
        }

        return model;
    }


    // =====================================================
    // YENİ ANKET - POST
    // SADECE YÖNETİCİ
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateSurveyViewModel model)
    {
        model.Questions ??=
            new List<CreateQuestionViewModel>();

        if (model.Questions.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ankette en az bir soru bulunmalıdır."
            );
        }

        if (
            model.StartDate.HasValue &&
            model.EndDate.HasValue &&
            model.EndDate.Value <= model.StartDate.Value
        )
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "Bitiş tarihi başlangıç tarihinden sonra olmalıdır."
            );
        }


        // =================================================
        // DİNAMİK AKIŞ KURALLARINI DOĞRULA
        // =================================================

        for (
            var i = 0;
            i < model.Questions.Count;
            i++
        )
        {
            var question =
                model.Questions[i];

            var currentOrder =
                i + 1;

            if (
                question.DependsOnQuestionOrder.HasValue
            )
            {
                var parentOrder =
                    question.DependsOnQuestionOrder.Value;

                if (
                    parentOrder < 1 ||
                    parentOrder >= currentOrder
                )
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Soru {currentOrder} yalnızca kendisinden önceki bir soruya bağlanabilir."
                    );
                }

                if (!question.ConditionOperator.HasValue)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Soru {currentOrder} için bir koşul türü seçmelisiniz."
                    );
                }
                else if (
                    question.ConditionOperator !=
                        BranchConditionOperator.Answered &&
                    question.ConditionOperator !=
                        BranchConditionOperator.NotAnswered &&
                    string.IsNullOrWhiteSpace(
                        question.ShowWhenAnswerEquals
                    )
                )
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Soru {currentOrder} için koşul değeri boş bırakılamaz."
                    );
                }
            }
            else
            {
                question.ConditionOperator =
                    null;

                question.ShowWhenAnswerEquals =
                    null;
            }
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user =
            await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var surveyCode =
            await GenerateUniqueSurveyCodeAsync();

        var survey =
            new Survey
            {
                Title =
                    model.Title.Trim(),

                Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim(),

                Code =
                    surveyCode,

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow,

                StartDate =
                    model.StartDate!.Value,

                EndDate =
                    model.EndDate!.Value,

                CreatedByUserId =
                    user.Id
            };


        for (
            var i = 0;
            i < model.Questions.Count;
            i++
        )
        {
            var questionModel =
                model.Questions[i];

            if (
                string.IsNullOrWhiteSpace(
                    questionModel.Text
                )
            )
            {
                continue;
            }

            var question =
                new Question
                {
                    Text =
                        questionModel.Text.Trim(),

                    Type =
                        questionModel.Type,

                    IsRequired =
                        questionModel.IsRequired,

                    Order =
                        i + 1,

                    RatingMaxValue =
                        questionModel.Type == QuestionType.Rating
                            ? questionModel.RatingMaxValue ?? 5
                            : null,

                    DependsOnQuestionOrder =
                        questionModel.DependsOnQuestionOrder,

                    ConditionOperator =
                        questionModel.DependsOnQuestionOrder.HasValue
                            ? questionModel.ConditionOperator
                            : null,

                    ShowWhenAnswerEquals =
                        questionModel.DependsOnQuestionOrder.HasValue &&
                        questionModel.ConditionOperator !=
                            BranchConditionOperator.Answered &&
                        questionModel.ConditionOperator !=
                            BranchConditionOperator.NotAnswered
                            ? questionModel.ShowWhenAnswerEquals?.Trim()
                            : null
                };


            if (
                questionModel.Type == QuestionType.SingleChoice ||
                questionModel.Type == QuestionType.MultipleChoice
            )
            {
                var validOptions =
                    (
                        questionModel.Options ??
                        new List<string>()
                    )
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                        x.Trim())
                    .ToList();

                if (validOptions.Count < 2)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"\"{question.Text}\" sorusu için en az iki seçenek eklemelisiniz."
                    );

                    return View(model);
                }

                for (
                    var optionIndex = 0;
                    optionIndex < validOptions.Count;
                    optionIndex++
                )
                {
                    question.Options.Add(
                        new QuestionOption
                        {
                            Text =
                                validOptions[optionIndex],

                            Order =
                                optionIndex + 1
                        }
                    );
                }
            }

            else if (
                questionModel.Type == QuestionType.Likert
            )
            {
                var likertOptions =
                    new[]
                    {
                        "Kesinlikle Katılıyorum",
                        "Katılıyorum",
                        "Kararsızım",
                        "Katılmıyorum",
                        "Kesinlikle Katılmıyorum"
                    };

                for (
                    var optionIndex = 0;
                    optionIndex < likertOptions.Length;
                    optionIndex++
                )
                {
                    question.Options.Add(
                        new QuestionOption
                        {
                            Text =
                                likertOptions[optionIndex],

                            Order =
                                optionIndex + 1
                        }
                    );
                }
            }

            else if (
                questionModel.Type == QuestionType.YesNo
            )
            {
                question.Options.Add(
                    new QuestionOption
                    {
                        Text = "Evet",
                        Order = 1
                    }
                );

                question.Options.Add(
                    new QuestionOption
                    {
                        Text = "Hayır",
                        Order = 2
                    }
                );
            }

            survey.Questions.Add(
                question
            );
        }


        if (survey.Questions.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "En az bir geçerli soru eklemelisiniz."
            );

            return View(model);
        }

        _context.Surveys.Add(
            survey
        );

        await _context.SaveChangesAsync();


        // =================================================
        // YENİ AKTİF ANKET BİLDİRİMİ
        // =================================================

        await _notificationService
            .CreateSurveyPublishedNotificationAsync(
                survey,
                user.Id
            );


        await _systemLogService.LogAsync(
            "Anket Oluşturuldu",
            $"{user.FullName}, \"{survey.Title}\" dinamik anketini oluşturdu. Kod: {survey.Code}.",
            "Survey",
            user
        );

        return RedirectToAction(
            nameof(Created),
            new
            {
                id = survey.Id
            }
        );
    }



    // =====================================================
    // ANKET DÜZENLE - GET
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Edit(
        int id)
    {
        var survey =
            await _context.Surveys
                .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x =>
                    x.Id == id
                );

        if (survey is null)
        {
            return NotFound();
        }

        var model =
            new CreateSurveyViewModel
            {
                Title =
                    survey.Title,

                Description =
                    survey.Description ?? string.Empty,

                StartDate =
                    survey.StartDate,

                EndDate =
                    survey.EndDate,

                Questions =
                    survey.Questions
                        .OrderBy(x => x.Order)
                        .Select(question =>
                            new CreateQuestionViewModel
                            {
                                Text =
                                    question.Text,

                                Type =
                                    question.Type,

                                IsRequired =
                                    question.IsRequired,

                                Order =
                                    question.Order,

                                RatingMaxValue =
                                    question.RatingMaxValue ?? 5,

                                DependsOnQuestionOrder =
                                    question.DependsOnQuestionOrder,

                                ConditionOperator =
                                    question.ConditionOperator,

                                ShowWhenAnswerEquals =
                                    question.ShowWhenAnswerEquals,

                                Options =
                                    question.Options
                                        .OrderBy(x => x.Order)
                                        .Select(x => x.Text)
                                        .ToList()
                            }
                        )
                        .ToList()
            };

        ViewBag.SurveyId =
            survey.Id;

        ViewBag.SurveyCode =
            survey.Code;

        ViewBag.HasResponses =
            await _context.SurveyResponses
                .AnyAsync(x =>
                    x.SurveyId == survey.Id
                );

        return View(model);
    }


    // =====================================================
    // ANKET DÜZENLE - POST
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CreateSurveyViewModel model)
    {
        model.Questions ??=
            new List<CreateQuestionViewModel>();

        var survey =
            await _context.Surveys
                .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x =>
                    x.Id == id
                );

        if (survey is null)
        {
            return NotFound();
        }

        ViewBag.SurveyId =
            survey.Id;

        ViewBag.SurveyCode =
            survey.Code;

        var hasResponses =
            await _context.SurveyResponses
                .AnyAsync(x =>
                    x.SurveyId == survey.Id
                );

        ViewBag.HasResponses =
            hasResponses;


        if (model.Questions.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ankette en az bir soru bulunmalıdır."
            );
        }

        if (
            model.StartDate.HasValue &&
            model.EndDate.HasValue &&
            model.EndDate.Value <= model.StartDate.Value
        )
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "Bitiş tarihi başlangıç tarihinden sonra olmalıdır."
            );
        }


        // =================================================
        // DİNAMİK AKIŞ KURALLARI
        // =================================================

        for (
            var i = 0;
            i < model.Questions.Count;
            i++
        )
        {
            var question =
                model.Questions[i];

            var currentOrder =
                i + 1;

            question.Order =
                currentOrder;

            if (
                question.DependsOnQuestionOrder.HasValue
            )
            {
                var parentOrder =
                    question.DependsOnQuestionOrder.Value;

                if (
                    parentOrder < 1 ||
                    parentOrder >= currentOrder
                )
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Soru {currentOrder} yalnızca kendisinden önceki bir soruya bağlanabilir."
                    );
                }

                if (!question.ConditionOperator.HasValue)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Soru {currentOrder} için koşul türünü seçmelisiniz."
                    );
                }
                else if (
                    question.ConditionOperator !=
                        BranchConditionOperator.Answered &&
                    question.ConditionOperator !=
                        BranchConditionOperator.NotAnswered &&
                    string.IsNullOrWhiteSpace(
                        question.ShowWhenAnswerEquals
                    )
                )
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Soru {currentOrder} için koşul değeri boş bırakılamaz."
                    );
                }
            }
            else
            {
                question.ConditionOperator =
                    null;

                question.ShowWhenAnswerEquals =
                    null;
            }
        }


        // Daha önce cevap toplanmışsa soru yapısını değiştirmiyoruz.
        // Metin, zorunluluk ve koşullar düzenlenebilir.
        if (hasResponses)
        {
            var existingQuestions =
                survey.Questions
                    .OrderBy(x => x.Order)
                    .ToList();

            if (
                existingQuestions.Count !=
                model.Questions.Count
            )
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Bu ankette cevap bulunduğu için soru ekleme veya silme yapılamaz."
                );
            }
            else
            {
                for (
                    var i = 0;
                    i < existingQuestions.Count;
                    i++
                )
                {
                    if (
                        existingQuestions[i].Type !=
                        model.Questions[i].Type
                    )
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            $"Soru {i + 1} için soru tipi değiştirilemez; ankette daha önce cevap bulunmaktadır."
                        );
                    }
                }
            }
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var user =
            await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }


        survey.Title =
            model.Title.Trim();

        survey.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

        survey.StartDate =
            model.StartDate!.Value;

        survey.EndDate =
            model.EndDate!.Value;


        // =================================================
        // CEVAP VARSA: MEVCUT SORULARI YERİNDE GÜNCELLE
        // =================================================

        if (hasResponses)
        {
            var existingQuestions =
                survey.Questions
                    .OrderBy(x => x.Order)
                    .ToList();

            for (
                var i = 0;
                i < existingQuestions.Count;
                i++
            )
            {
                var existing =
                    existingQuestions[i];

                var incoming =
                    model.Questions[i];

                existing.Text =
                    incoming.Text.Trim();

                existing.IsRequired =
                    incoming.IsRequired;

                existing.RatingMaxValue =
                    incoming.Type == QuestionType.Rating
                        ? incoming.RatingMaxValue ?? 5
                        : null;

                existing.DependsOnQuestionOrder =
                    incoming.DependsOnQuestionOrder;

                existing.ConditionOperator =
                    incoming.DependsOnQuestionOrder.HasValue
                        ? incoming.ConditionOperator
                        : null;

                existing.ShowWhenAnswerEquals =
                    incoming.DependsOnQuestionOrder.HasValue &&
                    incoming.ConditionOperator !=
                        BranchConditionOperator.Answered &&
                    incoming.ConditionOperator !=
                        BranchConditionOperator.NotAnswered
                        ? incoming.ShowWhenAnswerEquals?.Trim()
                        : null;


                // Seçenek metinlerini, ID'leri bozmadan güncelle.
                if (
                    existing.Type == QuestionType.SingleChoice ||
                    existing.Type == QuestionType.MultipleChoice
                )
                {
                    var existingOptions =
                        existing.Options
                            .OrderBy(x => x.Order)
                            .ToList();

                    var incomingOptions =
                        (incoming.Options ?? new List<string>())
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .Select(x =>
                                x.Trim())
                            .ToList();

                    if (
                        existingOptions.Count !=
                        incomingOptions.Count
                    )
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            $"Soru {i + 1} için seçenek sayısı değiştirilemez; ankette cevap bulunmaktadır."
                        );

                        return View(model);
                    }

                    for (
                        var optionIndex = 0;
                        optionIndex < existingOptions.Count;
                        optionIndex++
                    )
                    {
                        existingOptions[optionIndex].Text =
                            incomingOptions[optionIndex];
                    }
                }
            }
        }

        // =================================================
        // CEVAP YOKSA: SORULARI YENİDEN OLUŞTUR
        // =================================================

        else
        {
            if (survey.Questions.Count > 0)
            {
                _context.Questions.RemoveRange(
                    survey.Questions
                );

                await _context.SaveChangesAsync();
            }

            survey.Questions =
                new List<Question>();

            for (
                var i = 0;
                i < model.Questions.Count;
                i++
            )
            {
                var questionModel =
                    model.Questions[i];

                if (
                    string.IsNullOrWhiteSpace(
                        questionModel.Text
                    )
                )
                {
                    continue;
                }

                var question =
                    new Question
                    {
                        Text =
                            questionModel.Text.Trim(),

                        Type =
                            questionModel.Type,

                        IsRequired =
                            questionModel.IsRequired,

                        Order =
                            i + 1,

                        RatingMaxValue =
                            questionModel.Type == QuestionType.Rating
                                ? questionModel.RatingMaxValue ?? 5
                                : null,

                        DependsOnQuestionOrder =
                            questionModel.DependsOnQuestionOrder,

                        ConditionOperator =
                            questionModel.DependsOnQuestionOrder.HasValue
                                ? questionModel.ConditionOperator
                                : null,

                        ShowWhenAnswerEquals =
                            questionModel.DependsOnQuestionOrder.HasValue &&
                            questionModel.ConditionOperator !=
                                BranchConditionOperator.Answered &&
                            questionModel.ConditionOperator !=
                                BranchConditionOperator.NotAnswered
                                ? questionModel.ShowWhenAnswerEquals?.Trim()
                                : null
                    };


                if (
                    questionModel.Type == QuestionType.SingleChoice ||
                    questionModel.Type == QuestionType.MultipleChoice
                )
                {
                    var validOptions =
                        (questionModel.Options ?? new List<string>())
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .Select(x =>
                                x.Trim())
                            .ToList();

                    if (validOptions.Count < 2)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            $"\"{question.Text}\" sorusu için en az iki seçenek bulunmalıdır."
                        );

                        return View(model);
                    }

                    for (
                        var optionIndex = 0;
                        optionIndex < validOptions.Count;
                        optionIndex++
                    )
                    {
                        question.Options.Add(
                            new QuestionOption
                            {
                                Text =
                                    validOptions[optionIndex],

                                Order =
                                    optionIndex + 1
                            }
                        );
                    }
                }

                else if (
                    questionModel.Type == QuestionType.Likert
                )
                {
                    var likertOptions =
                        new[]
                        {
                            "Kesinlikle Katılıyorum",
                            "Katılıyorum",
                            "Kararsızım",
                            "Katılmıyorum",
                            "Kesinlikle Katılmıyorum"
                        };

                    for (
                        var optionIndex = 0;
                        optionIndex < likertOptions.Length;
                        optionIndex++
                    )
                    {
                        question.Options.Add(
                            new QuestionOption
                            {
                                Text =
                                    likertOptions[optionIndex],

                                Order =
                                    optionIndex + 1
                            }
                        );
                    }
                }

                else if (
                    questionModel.Type == QuestionType.YesNo
                )
                {
                    question.Options.Add(
                        new QuestionOption
                        {
                            Text = "Evet",
                            Order = 1
                        }
                    );

                    question.Options.Add(
                        new QuestionOption
                        {
                            Text = "Hayır",
                            Order = 2
                        }
                    );
                }

                survey.Questions.Add(
                    question
                );
            }
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        await _context.SaveChangesAsync();


        await _systemLogService.LogAsync(
            "Anket Düzenlendi",
            $"{user.FullName}, \"{survey.Title}\" anketini düzenledi. Kod: {survey.Code}.",
            "Survey",
            user
        );


        TempData["SurveyActionSuccess"] =
            "Anket başarıyla güncellendi.";


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =====================================================
    // ANKET OLUŞTURULDU
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Created(
        int id)
    {
        var survey =
            await _context.Surveys
                .FirstOrDefaultAsync(x =>
                    x.Id == id
                );

        if (survey is null)
        {
            return NotFound();
        }

        return View(survey);
    }


    // =====================================================
    // ANKET YÖNETİM LİSTESİ
    // SADECE YÖNETİCİ
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var surveys =
            await _context.Surveys
                .OrderByDescending(x =>
                    x.CreatedAt)
                .ToListAsync();

        return View(surveys);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(int id)
    {
        var source = await _context.Surveys
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id);

        var user = await _userManager.GetUserAsync(User);
        if (source is null || user is null) return NotFound();

        var copy = new Survey
        {
            Title = $"{source.Title} (Kopya)",
            Description = source.Description,
            Code = await GenerateUniqueSurveyCodeAsync(),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(7),
            CreatedByUserId = user.Id
        };

        foreach (var question in source.Questions.OrderBy(x => x.Order))
        {
            var questionCopy = new Question
            {
                Text = question.Text,
                Type = question.Type,
                IsRequired = question.IsRequired,
                Order = question.Order,
                RatingMaxValue = question.RatingMaxValue,
                DependsOnQuestionOrder = question.DependsOnQuestionOrder,
                ConditionOperator = question.ConditionOperator,
                ShowWhenAnswerEquals = question.ShowWhenAnswerEquals
            };
            foreach (var option in question.Options.OrderBy(x => x.Order))
                questionCopy.Options.Add(new QuestionOption { Text = option.Text, Order = option.Order });
            copy.Questions.Add(questionCopy);
        }

        _context.Surveys.Add(copy);
        await _context.SaveChangesAsync();
        await _systemLogService.LogAsync("Anket Kopyalandı", $"{user.FullName}, '{source.Title}' anketini kopyaladı.", "Survey", user);
        TempData["SurveyActionSuccess"] = "Anket, soruları ve seçenekleriyle birlikte kopyalandı.";
        return RedirectToAction(nameof(Edit), new { id = copy.Id });
    }


    // =====================================================
    // SONUÇLAR - ANKET LİSTESİ
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> ResultsIndex()
    {
        var surveys =
            await _context.Surveys
                .OrderByDescending(x =>
                    x.CreatedAt)
                .ToListAsync();

        var surveyIds =
            surveys
                .Select(x => x.Id)
                .ToList();

        var responseCounts =
            await _context.SurveyResponses
                .Where(x =>
                    surveyIds.Contains(x.SurveyId))
                .GroupBy(x =>
                    x.SurveyId)
                .Select(g =>
                    new
                    {
                        SurveyId = g.Key,
                        Count = g.Count()
                    })
                .ToDictionaryAsync(
                    x => x.SurveyId,
                    x => x.Count
                );

        var model =
            new ResultsIndexViewModel
            {
                Surveys =
                    surveys.Select(survey =>
                        new SurveyResultListItemViewModel
                        {
                            Id =
                                survey.Id,

                            Title =
                                survey.Title,

                            Code =
                                survey.Code,

                            IsActive =
                                survey.IsActive,

                            StartDate =
                                survey.StartDate,

                            EndDate =
                                survey.EndDate,

                            ResponseCount =
                                responseCounts.TryGetValue(
                                    survey.Id,
                                    out var count
                                )
                                    ? count
                                    : 0
                        })
                    .ToList()
            };

        return View(model);
    }


    // =====================================================
    // TEK ANKETİN SONUÇLARI
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Results(
        int id)
    {
        var survey =
            await _context.Surveys
                .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x =>
                    x.Id == id
                );

        if (survey is null)
        {
            return NotFound();
        }

        var responses =
            await _context.SurveyResponses
                .Include(x => x.Answers)
                .Where(x =>
                    x.SurveyId == survey.Id)
                .OrderByDescending(x =>
                    x.SubmittedAt)
                .ToListAsync();

        var allAnswers =
            responses
                .SelectMany(x => x.Answers)
                .ToList();

        var model =
            new SurveyResultsViewModel
            {
                SurveyId =
                    survey.Id,

                Title =
                    survey.Title,

                Code =
                    survey.Code,

                StartDate =
                    survey.StartDate,

                EndDate =
                    survey.EndDate,

                TotalResponses =
                    responses.Count,

                MemberResponses = responses.Count(x => x.UserId != null),
                AnonymousResponses = responses.Count(x => x.UserId == null),
                EligibleUserCount = await _userManager.Users.CountAsync(x => x.IsActive)
            };


        foreach (
            var question in survey.Questions
                .OrderBy(x => x.Order))
        {
            var questionAnswers =
                allAnswers
                    .Where(x =>
                        x.QuestionId == question.Id)
                    .ToList();

            var questionResult =
                new QuestionResultViewModel
                {
                    QuestionId =
                        question.Id,

                    Text =
                        question.Text,

                    Type =
                        question.Type,

                    TotalAnswers =
                        questionAnswers.Count
                };


            if (
                question.Type == QuestionType.SingleChoice ||
                question.Type == QuestionType.MultipleChoice ||
                question.Type == QuestionType.Likert ||
                question.Type == QuestionType.YesNo
            )
            {
                var selectedIds =
                    new List<int>();

                foreach (
                    var answer in questionAnswers)
                {
                    if (
                        string.IsNullOrWhiteSpace(
                            answer.SelectedOptionIds
                        )
                    )
                    {
                        continue;
                    }

                    foreach (
                        var item in answer.SelectedOptionIds.Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries |
                            StringSplitOptions.TrimEntries
                        ))
                    {
                        if (
                            int.TryParse(
                                item,
                                out var optionId
                            )
                        )
                        {
                            selectedIds.Add(
                                optionId
                            );
                        }
                    }
                }

                var answeredCount =
                    questionAnswers.Count(x =>
                        !string.IsNullOrWhiteSpace(
                            x.SelectedOptionIds
                        )
                    );

                foreach (
                    var option in question.Options
                        .OrderBy(x => x.Order))
                {
                    var count =
                        selectedIds.Count(x =>
                            x == option.Id
                        );

                    var percentage =
                        answeredCount > 0
                            ? Math.Round(
                                count * 100.0 / answeredCount,
                                1
                            )
                            : 0;

                    questionResult.Options.Add(
                        new OptionResultViewModel
                        {
                            OptionId =
                                option.Id,

                            Text =
                                option.Text,

                            Count =
                                count,

                            Percentage =
                                percentage
                        }
                    );
                }
            }

            else if (
                question.Type == QuestionType.Rating
            )
            {
                var ratings =
                    questionAnswers
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.Value
                            ))
                        .Select(x =>
                        {
                            var ok =
                                int.TryParse(
                                    x.Value,
                                    out var value
                                );

                            return new
                            {
                                ok,
                                value
                            };
                        })
                        .Where(x =>
                            x.ok)
                        .Select(x =>
                            x.value)
                        .ToList();

                if (ratings.Count > 0)
                {
                    questionResult.AverageValue =
                        Math.Round(
                            ratings.Average(),
                            2
                        );
                }

                var maxValue =
                    question.RatingMaxValue ?? 5;

                for (
                    var rating = 1;
                    rating <= maxValue;
                    rating++
                )
                {
                    var count =
                        ratings.Count(x =>
                            x == rating
                        );

                    var percentage =
                        ratings.Count > 0
                            ? Math.Round(
                                count * 100.0 / ratings.Count,
                                1
                            )
                            : 0;

                    questionResult.Options.Add(
                        new OptionResultViewModel
                        {
                            OptionId =
                                rating,

                            Text =
                                rating.ToString(),

                            Count =
                                count,

                            Percentage =
                                percentage
                        }
                    );
                }
            }

            else if (
                question.Type == QuestionType.Number
            )
            {
                var numbers =
                    new List<decimal>();

                foreach (
                    var answer in questionAnswers)
                {
                    if (
                        string.IsNullOrWhiteSpace(
                            answer.Value
                        )
                    )
                    {
                        continue;
                    }

                    if (
                        decimal.TryParse(
                            answer.Value,
                            NumberStyles.Any,
                            CultureInfo.CurrentCulture,
                            out var value
                        )
                        ||
                        decimal.TryParse(
                            answer.Value,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out value
                        )
                    )
                    {
                        numbers.Add(
                            value
                        );
                    }
                }

                if (numbers.Count > 0)
                {
                    questionResult.AverageValue =
                        Math.Round(
                            (double)numbers.Average(),
                            2
                        );

                    questionResult.MinimumValue =
                        (double)numbers.Min();

                    questionResult.MaximumValue =
                        (double)numbers.Max();
                }

                questionResult.TextAnswers =
                    questionAnswers
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.Value
                            ))
                        .Select(x =>
                            x.Value!)
                        .ToList();
            }

            else if (
                question.Type == QuestionType.ShortText ||
                question.Type == QuestionType.LongText
            )
            {
                questionResult.TextAnswers =
                    questionAnswers
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.Value
                            ))
                        .Select(x =>
                            x.Value!)
                        .ToList();
            }

            model.Questions.Add(
                questionResult
            );
        }

        var ratingAverages =
            model.Questions
                .Where(x =>
                    x.Type == QuestionType.Rating &&
                    x.AverageValue.HasValue
                )
                .Select(x =>
                    x.AverageValue!.Value)
                .ToList();

        if (ratingAverages.Count > 0)
        {
            model.OverallRatingAverage =
                Math.Round(
                    ratingAverages.Average(),
                    2
                );
        }

        model.ParticipationRate = model.EligibleUserCount > 0
            ? Math.Round(model.MemberResponses * 100.0 / model.EligibleUserCount, 1)
            : 0;
        model.MostCommonAnswer = model.Questions
            .SelectMany(x => x.Options)
            .OrderByDescending(x => x.Count)
            .FirstOrDefault(x => x.Count > 0)?.Text ?? "Henüz belirlenmedi";

        return View(model);
    }


    // =====================================================
    // TEK ANKET SİL
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var survey = await _context.Surveys
            .FirstOrDefaultAsync(x => x.Id == id);

        if (survey is null)
        {
            TempData["SurveyActionError"] = "Silinecek anket bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var surveyTitle = survey.Title;

        await DeleteSurveyGraphAsync(id);

        await _systemLogService.LogAsync(
            "Anket Silindi",
            $"{user.FullName}, \"{surveyTitle}\" anketini kalıcı olarak sildi.",
            "Survey",
            user
        );

        TempData["SurveyActionSuccess"] =
            $"\"{surveyTitle}\" anketi silindi.";

        return RedirectToAction(nameof(Index));
    }


    // =====================================================
    // SEÇİLİ ANKETLERİ SİL
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected(List<int>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            TempData["SurveyActionError"] =
                "Silmek için en az bir anket seçmelisiniz.";

            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var distinctIds = ids
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var surveys = await _context.Surveys
            .Where(x => distinctIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Title
            })
            .ToListAsync();

        if (surveys.Count == 0)
        {
            TempData["SurveyActionError"] =
                "Seçilen anketler bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        foreach (var survey in surveys)
        {
            await DeleteSurveyGraphAsync(survey.Id);
        }

        await _systemLogService.LogAsync(
            "Toplu Anket Silme",
            $"{user.FullName}, {surveys.Count} anketi toplu olarak sildi.",
            "Survey",
            user
        );

        TempData["SurveyActionSuccess"] =
            $"{surveys.Count} anket başarıyla silindi.";

        return RedirectToAction(nameof(Index));
    }


    // =====================================================
    // ANKETİN İLİŞKİLİ VERİLERİNİ GÜVENLİ SİL
    // =====================================================

    private async Task DeleteSurveyGraphAsync(int surveyId)
    {
        var responseIds = await _context.SurveyResponses
            .Where(x => x.SurveyId == surveyId)
            .Select(x => x.Id)
            .ToListAsync();

        if (responseIds.Count > 0)
        {
            var answers = await _context.Answers
                .Where(x => responseIds.Contains(x.SurveyResponseId))
                .ToListAsync();

            if (answers.Count > 0)
            {
                _context.Answers.RemoveRange(answers);
            }

            var responses = await _context.SurveyResponses
                .Where(x => x.SurveyId == surveyId)
                .ToListAsync();

            _context.SurveyResponses.RemoveRange(responses);
        }

        var questionIds = await _context.Questions
            .Where(x => x.SurveyId == surveyId)
            .Select(x => x.Id)
            .ToListAsync();

        if (questionIds.Count > 0)
        {
            var options = await _context.QuestionOptions
                .Where(x => questionIds.Contains(x.QuestionId))
                .ToListAsync();

            if (options.Count > 0)
            {
                _context.QuestionOptions.RemoveRange(options);
            }

            var questions = await _context.Questions
                .Where(x => x.SurveyId == surveyId)
                .ToListAsync();

            _context.Questions.RemoveRange(questions);
        }

        var survey = await _context.Surveys
            .FirstOrDefaultAsync(x => x.Id == surveyId);

        if (survey is not null)
        {
            _context.Surveys.Remove(survey);
        }

        await _context.SaveChangesAsync();
    }


    // =====================================================
    // ANKETİ DURDUR / TEKRAR AKTİF ET
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(
        int id)
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var survey =
            await _context.Surveys
                .FirstOrDefaultAsync(x =>
                    x.Id == id
                );

        if (survey is null)
        {
            return NotFound();
        }

        if (!survey.IsActive)
        {
            if (
                survey.StartDate == default ||
                survey.EndDate == default
            )
            {
                TempData["SurveyActionError"] =
                    "Yayın tarihleri belirlenmemiş bir anket tekrar aktifleştirilemez.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            if (DateTime.Now > survey.EndDate)
            {
                TempData["SurveyActionError"] =
                    "Bitiş tarihi geçmiş bir anket tekrar aktifleştirilemez.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        survey.IsActive =
            !survey.IsActive;

        await _context.SaveChangesAsync();

        await _systemLogService.LogAsync(
            survey.IsActive
                ? "Anket Aktifleştirildi"
                : "Anket Durduruldu",
            survey.IsActive
                ? $"{user.FullName}, \"{survey.Title}\" anketini tekrar aktif etti."
                : $"{user.FullName}, \"{survey.Title}\" anketini durdurdu.",
            "Survey",
            user
        );

        TempData["SurveyActionSuccess"] =
            survey.IsActive
                ? "Anket tekrar aktif edildi."
                : "Anket durduruldu.";

        return RedirectToAction(
            nameof(Index)
        );
    }


    // =====================================================
    // BENZERSİZ 6 HANELİ RANDOM KOD
    // =====================================================

    private async Task<string>
        GenerateUniqueSurveyCodeAsync()
    {
        const int minimumCode =
            100000;

        const int maximumCode =
            1000000;

        string code;

        do
        {
            code =
                Random.Shared
                    .Next(
                        minimumCode,
                        maximumCode
                    )
                    .ToString();
        }
        while (
            await _context.Surveys
                .AnyAsync(x =>
                    x.Code == code)
        );

        return code;
    }


    // =====================================================
    // ANKET CEVAPLAMA MODELİNİ HAZIRLA
    // =====================================================

    private static TakeSurveyViewModel
        BuildTakeSurveyViewModel(
            Survey survey,
            List<SurveyAnswerInputViewModel>? answers = null)
    {
        var model =
            new TakeSurveyViewModel
            {
                SurveyId =
                    survey.Id,

                Code =
                    survey.Code,

                Title =
                    survey.Title,

                Description =
                    survey.Description
            };

        foreach (
            var question in survey.Questions
                .OrderBy(x => x.Order))
        {
            model.Questions.Add(
                new TakeSurveyQuestionViewModel
                {
                    Id =
                        question.Id,

                    Text =
                        question.Text,

                    Type =
                        question.Type,

                    IsRequired =
                        question.IsRequired,

                    Order =
                        question.Order,

                    RatingMaxValue =
                        question.RatingMaxValue,

                    DependsOnQuestionOrder =
                        question.DependsOnQuestionOrder,

                    ConditionOperator =
                        question.ConditionOperator,

                    ShowWhenAnswerEquals =
                        question.ShowWhenAnswerEquals,

                    Options =
                        question.Options
                            .OrderBy(x =>
                                x.Order)
                            .Select(x =>
                                new TakeSurveyOptionViewModel
                                {
                                    Id =
                                        x.Id,

                                    Text =
                                        x.Text,

                                    Order =
                                        x.Order
                                })
                            .ToList()
                }
            );
        }

        if (
            answers is not null &&
            answers.Count > 0
        )
        {
            model.Answers =
                model.Questions
                    .Select(
                        question =>
                        {
                            var existing =
                                answers.FirstOrDefault(
                                    x =>
                                        x.QuestionId ==
                                        question.Id
                                );

                            return existing ??
                                new SurveyAnswerInputViewModel
                                {
                                    QuestionId =
                                        question.Id
                                };
                        }
                    )
                    .ToList();
        }
        else
        {
            model.Answers =
                model.Questions
                    .Select(x =>
                        new SurveyAnswerInputViewModel
                        {
                            QuestionId =
                                x.Id
                        })
                    .ToList();
        }

        return model;
    }


    // =====================================================
    // DİNAMİK SORU GÖRÜNÜRLÜĞÜ
    // =====================================================

    private static bool IsQuestionVisible(
        Question question,
        Survey survey,
        IReadOnlyCollection<SurveyAnswerInputViewModel> answers,
        HashSet<int>? visitedOrders = null)
    {
        if (
            !question.DependsOnQuestionOrder.HasValue ||
            !question.ConditionOperator.HasValue
        )
        {
            return true;
        }

        visitedOrders ??=
            new HashSet<int>();

        if (
            !visitedOrders.Add(
                question.Order
            )
        )
        {
            return false;
        }

        var parent =
            survey.Questions
                .FirstOrDefault(x =>
                    x.Order ==
                    question.DependsOnQuestionOrder.Value
                );

        if (parent is null)
        {
            return false;
        }

        if (
            !IsQuestionVisible(
                parent,
                survey,
                answers,
                visitedOrders
            )
        )
        {
            return false;
        }

        var parentAnswer =
            answers.FirstOrDefault(x =>
                x.QuestionId ==
                parent.Id
            );

        return AnswerMatchesCondition(
            parent,
            parentAnswer,
            question.ConditionOperator.Value,
            question.ShowWhenAnswerEquals
        );
    }


    private static bool AnswerMatchesCondition(
        Question parentQuestion,
        SurveyAnswerInputViewModel? answer,
        BranchConditionOperator conditionOperator,
        string? conditionValue)
    {
        var answerValues =
            GetAnswerValues(
                parentQuestion,
                answer
            );

        var hasAnswer =
            answerValues.Count > 0;

        if (
            conditionOperator ==
            BranchConditionOperator.Answered
        )
        {
            return hasAnswer;
        }

        if (
            conditionOperator ==
            BranchConditionOperator.NotAnswered
        )
        {
            return !hasAnswer;
        }

        if (!hasAnswer)
        {
            return false;
        }

        var expected =
            conditionValue?.Trim() ?? string.Empty;


        // SAYISAL KARŞILAŞTIRMALAR
        if (
            conditionOperator ==
                BranchConditionOperator.LessThan ||
            conditionOperator ==
                BranchConditionOperator.LessThanOrEqual ||
            conditionOperator ==
                BranchConditionOperator.GreaterThan ||
            conditionOperator ==
                BranchConditionOperator.GreaterThanOrEqual
        )
        {
            if (
                !TryParseDecimal(
                    expected,
                    out var expectedNumber
                )
            )
            {
                return false;
            }

            return answerValues.Any(
                value =>
                {
                    if (
                        !TryParseDecimal(
                            value,
                            out var actualNumber
                        )
                    )
                    {
                        return false;
                    }

                    return conditionOperator switch
                    {
                        BranchConditionOperator.LessThan =>
                            actualNumber < expectedNumber,

                        BranchConditionOperator.LessThanOrEqual =>
                            actualNumber <= expectedNumber,

                        BranchConditionOperator.GreaterThan =>
                            actualNumber > expectedNumber,

                        BranchConditionOperator.GreaterThanOrEqual =>
                            actualNumber >= expectedNumber,

                        _ => false
                    };
                }
            );
        }


        if (
            conditionOperator ==
            BranchConditionOperator.Contains
        )
        {
            return answerValues.Any(
                value =>
                    value.Contains(
                        expected,
                        StringComparison.CurrentCultureIgnoreCase
                    )
            );
        }


        if (
            conditionOperator ==
            BranchConditionOperator.NotEquals
        )
        {
            return answerValues.All(
                value =>
                    !string.Equals(
                        value,
                        expected,
                        StringComparison.CurrentCultureIgnoreCase
                    )
            );
        }


        // Varsayılan: Equals
        return answerValues.Any(
            value =>
                string.Equals(
                    value,
                    expected,
                    StringComparison.CurrentCultureIgnoreCase
                )
        );
    }


    private static List<string> GetAnswerValues(
        Question question,
        SurveyAnswerInputViewModel? answer)
    {
        if (answer is null)
        {
            return new List<string>();
        }

        if (
            question.Type == QuestionType.SingleChoice ||
            question.Type == QuestionType.MultipleChoice ||
            question.Type == QuestionType.Likert ||
            question.Type == QuestionType.YesNo
        )
        {
            if (
                answer.SelectedOptionIds is null ||
                answer.SelectedOptionIds.Count == 0
            )
            {
                return new List<string>();
            }

            return question.Options
                .Where(x =>
                    answer.SelectedOptionIds.Contains(
                        x.Id
                    ))
                .Select(x =>
                    x.Text.Trim()
                )
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x)
                )
                .ToList();
        }

        if (
            string.IsNullOrWhiteSpace(
                answer.Value
            )
        )
        {
            return new List<string>();
        }

        return new List<string>
        {
            answer.Value.Trim()
        };
    }


    private static bool TryParseDecimal(
        string value,
        out decimal result)
    {
        return
            decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out result
            )
            ||
            decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out result
            );
    }


}
