using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("dev/verification")]
    public sealed class DevVerificationController : ControllerBase
    {
        private readonly IVerificationOutbox _verificationOutbox;
        private readonly IWebHostEnvironment _environment;

        public DevVerificationController(
            IVerificationOutbox verificationOutbox,
            IWebHostEnvironment environment)
        {
            _verificationOutbox = verificationOutbox;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            return Content(RenderPage(), "text/html; charset=utf-8");
        }

        [HttpGet("messages")]
        public ActionResult<ApiResponse<IReadOnlyCollection<VerificationOutboxMessageDto>>> GetMessages()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            return Ok(ApiResponse<IReadOnlyCollection<VerificationOutboxMessageDto>>.Ok(
                _verificationOutbox.GetMessages(),
                "Mock dogrulama mesajlari getirildi.",
                HttpContext.TraceIdentifier));
        }

        [HttpDelete("messages")]
        public IActionResult ClearMessages()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            _verificationOutbox.Clear();
            return Ok(ApiResponse<object>.Ok(null, "Mock dogrulama kutusu temizlendi.", HttpContext.TraceIdentifier));
        }

        private static string RenderPage()
        {
            return """
<!doctype html>
<html lang="tr">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Yoremio Dogrulama Kutusu</title>
  <style>
    :root { color-scheme: light; font-family: Inter, Segoe UI, Arial, sans-serif; }
    body { margin: 0; background: #f6f7f9; color: #17202a; }
    header { padding: 24px; background: #0f766e; color: white; }
    main { max-width: 1080px; margin: 0 auto; padding: 24px; }
    .toolbar { display: flex; gap: 12px; align-items: center; justify-content: space-between; margin-bottom: 16px; }
    button { border: 0; border-radius: 6px; padding: 10px 14px; background: #0f766e; color: white; cursor: pointer; font-weight: 600; }
    button.secondary { background: #334155; }
    .empty { padding: 24px; border: 1px dashed #94a3b8; border-radius: 8px; background: white; }
    .grid { display: grid; gap: 12px; }
    article { border: 1px solid #d8dee8; border-radius: 8px; background: white; padding: 16px; }
    .meta { display: flex; flex-wrap: wrap; gap: 8px; color: #475569; font-size: 13px; margin-bottom: 10px; }
    .badge { border-radius: 999px; background: #dff7f3; color: #0f766e; padding: 4px 8px; font-weight: 700; text-transform: uppercase; }
    .body { white-space: pre-wrap; word-break: break-word; line-height: 1.5; }
    a { color: #0f766e; font-weight: 700; }
  </style>
</head>
<body>
  <header>
    <h1>Yoremio Dogrulama Kutusu</h1>
    <p>Development ortaminda mock email dogrulama mesajlari burada gorunur.</p>
  </header>
  <main>
    <div class="toolbar">
      <strong id="count">Yukleniyor...</strong>
      <div>
        <button onclick="loadMessages()">Yenile</button>
        <button class="secondary" onclick="clearMessages()">Temizle</button>
      </div>
    </div>
    <section id="messages" class="grid"></section>
  </main>
  <script>
    async function loadMessages() {
      const response = await fetch('/dev/verification/messages');
      const payload = await response.json();
      const messages = payload.data || [];
      document.getElementById('count').textContent = `${messages.length} mesaj`;
      const container = document.getElementById('messages');
      if (messages.length === 0) {
        container.innerHTML = '<div class="empty">Henuz mock dogrulama mesaji yok. Bir satici kaydi olusturun.</div>';
        return;
      }
      container.innerHTML = messages.map(message => `
        <article>
          <div class="meta">
            <span class="badge">${escapeHtml(message.channel)}</span>
            <span>${escapeHtml(message.to)}</span>
            <span>${new Date(message.createdAtUtc).toLocaleString()}</span>
          </div>
          ${message.subject ? `<h2>${escapeHtml(message.subject)}</h2>` : ''}
          <div class="body">${linkify(message.body)}</div>
        </article>
      `).join('');
    }

    async function clearMessages() {
      await fetch('/dev/verification/messages', { method: 'DELETE' });
      await loadMessages();
    }

    function escapeHtml(value) {
      return String(value || '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
    }

    function linkify(value) {
      return escapeHtml(value).replace(/https?:\/\/[^\s<']+/g, url => `<a href="${url}">${url}</a>`);
    }

    loadMessages();
    setInterval(loadMessages, 5000);
  </script>
</body>
</html>
""";
        }
    }
}
