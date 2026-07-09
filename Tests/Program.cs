using Application.DTOs;
using API.Controllers;
using Domain.Constants;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

if (args.Any(a => string.Equals(a, "--chat-e2e", StringComparison.OrdinalIgnoreCase)))
{
    await ChatE2eRunner.RunAsync();
    return;
}

var tests = new (string Name, Action Test)[]
{
    ("Alici kaydı geçerli model kabul edilmeli", RegisterAliciDto_Should_BeValid),
    ("Satıcı kaydı zorunlu alanları yakalamalı", RegisterSaticiDto_Should_RequireFields),
    ("Email dogrulama DTO'su email ve kod istemeli", ConfirmVerificationDtos_Should_RequireEmailAndCode),
    ("Puan DTO aralık kontrolü yapmalı", PuanEkleDto_Should_ValidateRange),
    ("Ürün DTO dosya türünü doğrulamalı", UrunEkleDto_Should_RejectInvalidImageType),
    ("Rol sabitleri beklenen değerleri taşımalı", ApplicationRoles_Should_MatchExpectedValues),
    ("Ürün yazma endpointleri satıcı rolü istemeli", UrunController_WriteEndpoints_Should_RequireSellerRole),
    ("Ürün favori ve öneri endpointleri alıcı rolü istemeli", UrunController_FavoriteEndpoints_Should_RequireBuyerRole),
    ("Talep akışı endpointleri doğru rolleri istemeli", TalepController_Endpoints_Should_RequireExpectedRoles),
    ("Yorum yazma endpointleri alıcı rolü istemeli", YorumController_WriteEndpoints_Should_RequireBuyerRole),
    ("Puan ekleme endpointi alıcı rolü istemeli", PuanController_PuanEkle_Should_RequireBuyerRole),
    ("Profil endpointi satıcı rolü istemeli", ProfilController_Should_RequireSellerRole),
    ("Auth dogrulama endpointleri anonim POST olmali", AuthController_VerificationEndpoints_Should_BeAnonymousPost),
    ("Dashboard endpointleri beklenen rolleri istemeli", DashboardController_Should_RequireExpectedRoles),
    ("Kategori yazma endpointleri admin rolü istemeli", KategoriController_WriteEndpoints_Should_RequireAdminRole),
    ("ChatHub iki parametreli güvenli SendMessage metodu sunmalı", ChatHub_Should_ExposeSecureSendMethod),
    ("ChatHub okundu bilgisini desteklemeli", ChatHub_Should_ExposeReadReceiptMethod),
    ("Chat API korumalı geçmiş endpointleri sunmalı", ChatController_Should_RequireAuthAndExposeHistory),
    ("Bildirim API korumali okuma ve okundu endpointleri sunmali", BildirimController_Should_RequireAuthAndExposeNotificationEndpoints)
};

var failed = new List<string>();

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"[OK] {name}");
    }
    catch (Exception ex)
    {
        failed.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[FAIL] {name}");
    }
}

if (failed.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Başarısız testler:");
    foreach (var failure in failed)
    {
        Console.WriteLine($" - {failure}");
    }

    Environment.ExitCode = 1;
    return;
}

Console.WriteLine();
Console.WriteLine("Tüm testler geçti.");

static void RegisterAliciDto_Should_BeValid()
{
    var dto = new RegisterAliciDto
    {
        Email = "test@example.com",
        Password = "Aa!12345"
    };

    AssertValid(dto);
}

static void RegisterSaticiDto_Should_RequireFields()
{
    var dto = new RegisterSaticiDto
    {
        Email = "gecersiz",
        Password = "123",
        PhoneNumber = "",
        MagazaAdi = "ab",
        VergiNo = ""
    };

    var results = Validate(dto);

    AssertContains(results, "Geçerli bir email adresi giriniz.");
    AssertContains(results, "Telefon numarası boş olamaz.");
    AssertContains(results, "Şifre en az 8 karakter olmalı.");
    AssertContains(results, "Mağaza adı en az 3 karakter olmalıdır.");
    AssertContains(results, "Vergi numarası zorunludur.");
}

static void ConfirmVerificationDtos_Should_RequireEmailAndCode()
{
    var emailDto = new ConfirmEmailDto
    {
        Email = "gecersiz",
        Code = ""
    };

    var emailResults = Validate(emailDto);

    AssertContains(emailResults, "Gecerli bir email adresi giriniz.");
    AssertContains(emailResults, "Dogrulama kodu bos olamaz.");
}

static void AuthController_VerificationEndpoints_Should_BeAnonymousPost()
{
    AssertHttpMethod(typeof(AuthController), nameof(AuthController.ConfirmEmailCode), typeof(HttpPostAttribute));

    var emailMethod = typeof(AuthController).GetMethod(nameof(AuthController.ConfirmEmailCode));

    if (emailMethod?.GetCustomAttribute<AllowAnonymousAttribute>() == null)
        throw new InvalidOperationException("AuthController.ConfirmEmailCode AllowAnonymous olmalidir.");

    if (emailMethod?.GetParameters().SingleOrDefault()?.ParameterType != typeof(ConfirmEmailDto))
        throw new InvalidOperationException("AuthController.ConfirmEmailCode sadece ConfirmEmailDto almalidir.");

}

static void PuanEkleDto_Should_ValidateRange()
{
    var dto = new PuanEkleDto
    {
        UrunId = 0,
        PuanDegeri = 6
    };

    var results = Validate(dto);

    AssertContains(results, "Geçerli bir ürün seçilmelidir.");
    AssertContains(results, "Puan değeri 1 ile 5 arasında olmalıdır.");
}

static void UrunEkleDto_Should_RejectInvalidImageType()
{
    var dto = new UrunEkleDto
    {
        Adi = "Deneme ürün",
        Fiyat = 10,
        StokMiktari = 2,
        KategoriId = 1,
        Resimler =
        {
            CreateFormFile("not-image.txt", "text/plain", 128)
        }
    };

    var results = Validate(dto);
    AssertContains(results, "Yalnızca resim dosyaları yükleyebilirsiniz.");
}

static void ApplicationRoles_Should_MatchExpectedValues()
{
    if (ApplicationRoles.Admin != "ADMIN" || ApplicationRoles.Satici != "SATICI" || ApplicationRoles.Alici != "ALICI")
        throw new InvalidOperationException("Rol sabitleri beklenen değerlerle eşleşmiyor.");
}

static void UrunController_WriteEndpoints_Should_RequireSellerRole()
{
    AssertMethodRole(typeof(UrunController), nameof(UrunController.UrunEkle), ApplicationRoles.Satici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.GetMyProducts), ApplicationRoles.Satici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.UrunGuncelle), ApplicationRoles.Satici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.UrunDurumuGuncelle), ApplicationRoles.Satici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.UrunSil), ApplicationRoles.Satici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.UrunResimSil), ApplicationRoles.Satici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.UrunVideoSil), ApplicationRoles.Satici);
}

static void UrunController_FavoriteEndpoints_Should_RequireBuyerRole()
{
    AssertMethodRole(typeof(UrunController), nameof(UrunController.GetRecommended), ApplicationRoles.Alici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.GetFavorilerim), ApplicationRoles.Alici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.FavoriyeEkle), ApplicationRoles.Alici);
    AssertMethodRole(typeof(UrunController), nameof(UrunController.FavoridenCikar), ApplicationRoles.Alici);
}

static void YorumController_WriteEndpoints_Should_RequireBuyerRole()
{
    AssertMethodRole(typeof(YorumController), nameof(YorumController.YorumEkle), ApplicationRoles.Alici);
    AssertMethodRole(typeof(YorumController), nameof(YorumController.YorumGuncelle), ApplicationRoles.Alici);
    AssertMethodRole(typeof(YorumController), nameof(YorumController.YorumSil), ApplicationRoles.Alici);
}

static void PuanController_PuanEkle_Should_RequireBuyerRole()
{
    AssertMethodRole(typeof(PuanController), nameof(PuanController.PuanEkle), ApplicationRoles.Alici);
}

static void ProfilController_Should_RequireSellerRole()
{
    var authorize = typeof(ProfilController).GetCustomAttribute<AuthorizeAttribute>();
    if (authorize == null)
        throw new InvalidOperationException("ProfilController üzerinde Authorize attribute bulunamadı.");

    if (!string.Equals(authorize.Roles, ApplicationRoles.Satici, StringComparison.Ordinal))
        throw new InvalidOperationException($"ProfilController için beklenen rol {ApplicationRoles.Satici}, mevcut: {authorize.Roles}");

    var trustMethod = typeof(ProfilController).GetMethod(nameof(ProfilController.GetSaticiGuvenSkoru));
    if (trustMethod == null)
        throw new InvalidOperationException("ProfilController.GetSaticiGuvenSkoru bulunamadı.");

    var allowAnonymous = trustMethod.GetCustomAttribute<AllowAnonymousAttribute>();
    if (allowAnonymous == null)
        throw new InvalidOperationException("ProfilController.GetSaticiGuvenSkoru endpointi AllowAnonymous olmalıdır.");
}

static void KategoriController_WriteEndpoints_Should_RequireAdminRole()
{
    AssertMethodRole(typeof(KategoriController), nameof(KategoriController.Create), ApplicationRoles.Admin);
    AssertMethodRole(typeof(KategoriController), nameof(KategoriController.Update), ApplicationRoles.Admin);
    AssertMethodRole(typeof(KategoriController), nameof(KategoriController.Delete), ApplicationRoles.Admin);
}

static void DashboardController_Should_RequireExpectedRoles()
{
    var authorize = typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>();
    if (authorize == null)
        throw new InvalidOperationException("DashboardController uzerinde Authorize attribute bulunamadi.");

    AssertHttpMethod(typeof(DashboardController), nameof(DashboardController.GetSummary), typeof(HttpGetAttribute));
    AssertMethodRole(typeof(DashboardController), nameof(DashboardController.GetSaticiDashboard), ApplicationRoles.Satici);
    AssertMethodRole(typeof(DashboardController), nameof(DashboardController.GetAdminDashboard), ApplicationRoles.Admin);
}

static void TalepController_Endpoints_Should_RequireExpectedRoles()
{
    AssertMethodRole(typeof(TalepController), nameof(TalepController.TalepOlustur), ApplicationRoles.Alici);
    AssertMethodRole(typeof(TalepController), nameof(TalepController.GetBenimTaleplerim), ApplicationRoles.Alici);
    AssertMethodRole(typeof(TalepController), nameof(TalepController.GetSaticiTalepleri), ApplicationRoles.Satici);
    AssertMethodRole(typeof(TalepController), nameof(TalepController.TeklifVer), ApplicationRoles.Satici);
    AssertMethodRole(typeof(TalepController), nameof(TalepController.TeklifKabulEt), ApplicationRoles.Alici);
}

static void ChatHub_Should_ExposeSecureSendMethod()
{
    var method = typeof(ChatHub)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .FirstOrDefault(m =>
            m.Name == nameof(ChatHub.SendMessage) &&
            m.GetParameters().Length == 2 &&
            m.GetParameters()[0].ParameterType == typeof(string) &&
            m.GetParameters()[1].ParameterType == typeof(string));

    if (method == null)
        throw new InvalidOperationException("ChatHub üzerinde SendMessage(toUserId, message) metodu bulunamadı.");
}

static void ChatHub_Should_ExposeReadReceiptMethod()
{
    var method = typeof(ChatHub)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .FirstOrDefault(m =>
            m.Name == nameof(ChatHub.MarkConversationRead) &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(string));

    if (method == null)
        throw new InvalidOperationException("ChatHub üzerinde MarkConversationRead(otherUserId) metodu bulunamadı.");
}

static void ChatController_Should_RequireAuthAndExposeHistory()
{
    var authorize = typeof(ChatController).GetCustomAttribute<AuthorizeAttribute>();
    if (authorize == null)
        throw new InvalidOperationException("ChatController üzerinde Authorize attribute bulunamadı.");

    AssertHttpMethod(typeof(ChatController), nameof(ChatController.GetConversations), typeof(HttpGetAttribute));
    AssertHttpMethod(typeof(ChatController), nameof(ChatController.GetMessages), typeof(HttpGetAttribute));
    AssertHttpMethod(typeof(ChatController), nameof(ChatController.SendMessage), typeof(HttpPostAttribute));
    AssertHttpMethod(typeof(ChatController), nameof(ChatController.MarkConversationRead), typeof(HttpPostAttribute));
}

static void BildirimController_Should_RequireAuthAndExposeNotificationEndpoints()
{
    var authorize = typeof(BildirimController).GetCustomAttribute<AuthorizeAttribute>();
    if (authorize == null)
        throw new InvalidOperationException("BildirimController uzerinde Authorize attribute bulunamadi.");

    AssertHttpMethod(typeof(BildirimController), nameof(BildirimController.GetBildirimler), typeof(HttpGetAttribute));
    AssertHttpMethod(typeof(BildirimController), nameof(BildirimController.GetOkunmamisSayisi), typeof(HttpGetAttribute));
    AssertHttpMethod(typeof(BildirimController), nameof(BildirimController.OkunduIsaretle), typeof(HttpPostAttribute));
    AssertHttpMethod(typeof(BildirimController), nameof(BildirimController.TumunuOkunduIsaretle), typeof(HttpPostAttribute));
}

static List<string> Validate(object instance)
{
    var context = new ValidationContext(instance);
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
    return results.SelectMany(result => result.ErrorMessage is null ? [] : new[] { result.ErrorMessage }).ToList();
}

static void AssertValid(object instance)
{
    var results = Validate(instance);
    if (results.Count > 0)
        throw new InvalidOperationException(string.Join(" | ", results));
}

static void AssertContains(List<string> results, string expected)
{
    if (!results.Contains(expected))
        throw new InvalidOperationException($"Beklenen hata bulunamadı: {expected}");
}

static IFormFile CreateFormFile(string fileName, string contentType, int length)
{
    var bytes = Encoding.UTF8.GetBytes(new string('a', length));
    var stream = new MemoryStream(bytes);

    return new FormFile(stream, 0, bytes.Length, "file", fileName)
    {
        Headers = new HeaderDictionary(),
        ContentType = contentType
    };
}

static void AssertMethodRole(Type controllerType, string methodName, string expectedRole)
{
    var method = controllerType.GetMethod(methodName);
    if (method == null)
        throw new InvalidOperationException($"{controllerType.Name}.{methodName} bulunamadı.");

    var authorize = method.GetCustomAttribute<AuthorizeAttribute>();
    if (authorize == null)
        throw new InvalidOperationException($"{controllerType.Name}.{methodName} üzerinde Authorize attribute bulunamadı.");

    if (!string.Equals(authorize.Roles, expectedRole, StringComparison.Ordinal))
        throw new InvalidOperationException($"{controllerType.Name}.{methodName} için beklenen rol {expectedRole}, mevcut: {authorize.Roles}");
}

static void AssertHttpMethod(Type controllerType, string methodName, Type httpAttributeType)
{
    var method = controllerType.GetMethod(methodName);
    if (method == null)
        throw new InvalidOperationException($"{controllerType.Name}.{methodName} bulunamadı.");

    if (!method.GetCustomAttributes().Any(attribute => attribute.GetType() == httpAttributeType))
        throw new InvalidOperationException($"{controllerType.Name}.{methodName} üzerinde {httpAttributeType.Name} bulunamadı.");
}
