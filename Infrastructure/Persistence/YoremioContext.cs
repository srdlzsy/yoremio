using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence
{
    public class YoremioContext : IdentityDbContext<ApplicationUser>
    {
        public YoremioContext(DbContextOptions<YoremioContext> options)
            : base(options)
        {
        }

        // DbSet alanlarını null-forgiving ile işaretliyoruz; EF Core bunları runtime'da doldurur.
        public DbSet<SaticiProfili> SaticiProfilleri { get; set; } = null!;
        public DbSet<AliciProfili> AliciProfilleri { get; set; } = null!;
        public DbSet<Urun> Urunler { get; set; } = null!;
        public DbSet<Kategori> Kategoriler { get; set; } = null!;
        public DbSet<UrunResim> UrunResimler { get; set; } = null!;
        public DbSet<UrunVideo> UrunVideolar { get; set; } = null!;
        public DbSet<UrunFavori> UrunFavoriler { get; set; } = null!;
        public DbSet<Talep> Talepler { get; set; } = null!;
        public DbSet<TalepTeklif> TalepTeklifler { get; set; } = null!;
        public DbSet<TalepEslesme> TalepEslesmeleri { get; set; } = null!;
        public DbSet<SaticiTakip> SaticiTakipleri { get; set; } = null!;
        public DbSet<FiyatAlarmi> FiyatAlarmlari { get; set; } = null!;
        public DbSet<StokAlarmi> StokAlarmlari { get; set; } = null!;
        public DbSet<OrtakAlim> OrtakAlimlar { get; set; } = null!;
        public DbSet<OrtakAlimKatilim> OrtakAlimKatilimlari { get; set; } = null!;
        public DbSet<PlanliGonderim> PlanliGonderimler { get; set; } = null!;
        public DbSet<Yorum> Yorumlar { get; set; } = null!;
        public DbSet<Puan> Puanlar { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<Bildirim> Bildirimler { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Kullanici → SaticiProfili (1:1)
            builder.Entity<ApplicationUser>()
                .HasOne(k => k.SaticiProfili)
                .WithOne(p => p.Kullanici)
                .HasForeignKey<SaticiProfili>(p => p.KullaniciId);

            // Kullanici → AliciProfili (1:1)
            builder.Entity<ApplicationUser>()
                .HasOne(k => k.AliciProfili)
                .WithOne(p => p.Kullanici)
                .HasForeignKey<AliciProfili>(p => p.KullaniciId);

            // Urun → Kategori (N:1)
            builder.Entity<Urun>()
                .HasOne(u => u.Kategori)
                .WithMany(k => k.Urunler)
                .HasForeignKey(u => u.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);

            // urun → Yorum (1:N)
            builder.Entity<Urun>()
                .HasMany(u => u.Yorumlar)
                .WithOne(y => y.Urun)
                .HasForeignKey(y => y.UrunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Yorum → Kullanici (N:1)
            builder.Entity<Yorum>()
                .HasOne(y => y.YorumYapanKullanici)
                .WithMany()
                .HasForeignKey(y => y.YorumYapanKullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // Urun → SaticiProfili (N:1)
            builder.Entity<Urun>()
                .HasOne(u => u.Satici)
                .WithMany(s => s.Urunler)
                .HasForeignKey(u => u.SaticiId)
                .OnDelete(DeleteBehavior.Cascade);

            // Urun → UrunResim (1:N)
            builder.Entity<UrunResim>()
                .HasOne(r => r.Urun)
                .WithMany(u => u.Resimler)
                .HasForeignKey(r => r.UrunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Urun → UrunVideo (1:N)
            builder.Entity<UrunVideo>()
                .HasOne(v => v.Urun)
                .WithMany(u => u.Videolar)
                .HasForeignKey(v => v.UrunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Urun → Puan (1:N)
            builder.Entity<Urun>()
                .HasMany(u => u.Puanlar)
                .WithOne(p => p.Urun)
                .HasForeignKey(p => p.UrunId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UrunFavori>()
                .HasOne(f => f.Urun)
                .WithMany(u => u.Favoriler)
                .HasForeignKey(f => f.UrunId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UrunFavori>()
                .HasOne(f => f.Kullanici)
                .WithMany()
                .HasForeignKey(f => f.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Talep>()
                .HasOne(t => t.Alici)
                .WithMany()
                .HasForeignKey(t => t.AliciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Talep>()
                .HasOne(t => t.Urun)
                .WithMany()
                .HasForeignKey(t => t.UrunId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Talep>()
                .HasOne(t => t.Kategori)
                .WithMany()
                .HasForeignKey(t => t.KategoriId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<TalepEslesme>()
                .HasOne(e => e.Talep)
                .WithMany(t => t.Eslesmeler)
                .HasForeignKey(e => e.TalepId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TalepEslesme>()
                .HasOne(e => e.Satici)
                .WithMany()
                .HasForeignKey(e => e.SaticiId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TalepEslesme>()
                .Property(e => e.Sebep)
                .HasMaxLength(250);

            builder.Entity<TalepEslesme>()
                .HasIndex(e => new { e.TalepId, e.SaticiId })
                .IsUnique();

            builder.Entity<SaticiTakip>()
                .HasOne(t => t.Alici)
                .WithMany()
                .HasForeignKey(t => t.AliciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SaticiTakip>()
                .HasOne(t => t.Satici)
                .WithMany()
                .HasForeignKey(t => t.SaticiId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SaticiTakip>()
                .HasIndex(t => new { t.AliciId, t.SaticiId })
                .IsUnique();

            builder.Entity<FiyatAlarmi>()
                .HasOne(a => a.Alici)
                .WithMany()
                .HasForeignKey(a => a.AliciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FiyatAlarmi>()
                .HasOne(a => a.Urun)
                .WithMany()
                .HasForeignKey(a => a.UrunId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FiyatAlarmi>()
                .HasIndex(a => new { a.AliciId, a.UrunId })
                .IsUnique();

            builder.Entity<StokAlarmi>()
                .HasOne(a => a.Alici)
                .WithMany()
                .HasForeignKey(a => a.AliciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StokAlarmi>()
                .HasOne(a => a.Urun)
                .WithMany()
                .HasForeignKey(a => a.UrunId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StokAlarmi>()
                .HasIndex(a => new { a.AliciId, a.UrunId })
                .IsUnique();

            builder.Entity<OrtakAlim>()
                .HasOne(o => o.Satici)
                .WithMany()
                .HasForeignKey(o => o.SaticiId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrtakAlim>()
                .HasOne(o => o.Urun)
                .WithMany()
                .HasForeignKey(o => o.UrunId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<OrtakAlim>()
                .HasOne(o => o.Kategori)
                .WithMany()
                .HasForeignKey(o => o.KategoriId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<OrtakAlim>()
                .Property(o => o.Baslik)
                .HasMaxLength(150);

            builder.Entity<OrtakAlim>()
                .Property(o => o.MenseiSehir)
                .HasMaxLength(60);

            builder.Entity<OrtakAlim>()
                .Property(o => o.HedefSehir)
                .HasMaxLength(60);

            builder.Entity<OrtakAlim>()
                .Property(o => o.Birim)
                .HasMaxLength(30);

            builder.Entity<OrtakAlim>()
                .Property(o => o.Durum)
                .HasMaxLength(30);

            builder.Entity<OrtakAlim>()
                .Property(o => o.TeslimatKosullari)
                .HasMaxLength(500);

            builder.Entity<OrtakAlim>()
                .Property(o => o.IptalKosullari)
                .HasMaxLength(500);

            builder.Entity<OrtakAlimKatilim>()
                .HasOne(k => k.OrtakAlim)
                .WithMany(o => o.Katilimlar)
                .HasForeignKey(k => k.OrtakAlimId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrtakAlimKatilim>()
                .HasOne(k => k.Alici)
                .WithMany()
                .HasForeignKey(k => k.AliciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrtakAlimKatilim>()
                .HasIndex(k => new { k.OrtakAlimId, k.AliciId })
                .IsUnique();

            builder.Entity<TalepTeklif>()
                .HasOne(t => t.Talep)
                .WithMany(talep => talep.Teklifler)
                .HasForeignKey(t => t.TalepId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TalepTeklif>()
                .HasOne(t => t.Satici)
                .WithMany()
                .HasForeignKey(t => t.SaticiId)
                .OnDelete(DeleteBehavior.Cascade);

            // Puan → Kullanici (N:1)
            builder.Entity<Puan>()
                .HasOne(p => p.Kullanici)
                .WithMany()
                .HasForeignKey(p => p.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Puan>()
                .ToTable(t => t.HasCheckConstraint("CK_Puanlar_PuanDegeri", "\"PuanDegeri\" BETWEEN 1 AND 5"));

            builder.Entity<SaticiProfili>()
                .HasIndex(s => s.VergiNo)
                .IsUnique();

            builder.Entity<Kategori>()
                .HasIndex(k => k.Adi)
                .IsUnique();

            builder.Entity<Puan>()
                .HasIndex(p => new { p.KullaniciId, p.UrunId })
                .IsUnique();

            builder.Entity<ChatMessage>()
                .Property(m => m.Message)
                .HasMaxLength(1000);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatMessage>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId, m.SentAt });

            builder.Entity<ChatMessage>()
                .HasIndex(m => new { m.ReceiverId, m.SenderId, m.SentAt });

            builder.Entity<Bildirim>()
                .Property(b => b.Tur)
                .HasMaxLength(50);

            builder.Entity<Bildirim>()
                .Property(b => b.Baslik)
                .HasMaxLength(120);

            builder.Entity<Bildirim>()
                .Property(b => b.Mesaj)
                .HasMaxLength(500);

            builder.Entity<Bildirim>()
                .Property(b => b.IlgiliVarlikTuru)
                .HasMaxLength(50);

            builder.Entity<Bildirim>()
                .Property(b => b.IlgiliVarlikId)
                .HasMaxLength(100);

            builder.Entity<Bildirim>()
                .Property(b => b.AksiyonUrl)
                .HasMaxLength(300);

            builder.Entity<Bildirim>()
                .HasOne(b => b.Kullanici)
                .WithMany()
                .HasForeignKey(b => b.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Bildirim>()
                .HasIndex(b => new { b.KullaniciId, b.OkunmaTarihi, b.OlusturmaTarihi });

            builder.Entity<UrunFavori>()
                .HasIndex(f => new { f.KullaniciId, f.UrunId })
                .IsUnique();

            builder.Entity<Talep>()
                .Property(t => t.Durum)
                .HasMaxLength(20);

            builder.Entity<Talep>()
                .Property(t => t.TalepTuru)
                .HasMaxLength(20);

            builder.Entity<Talep>()
                .Property(t => t.TalepTipi)
                .HasMaxLength(20);

            builder.Entity<Talep>()
                .Property(t => t.ArananUrunAdi)
                .HasMaxLength(150);

            builder.Entity<Talep>()
                .Property(t => t.Birim)
                .HasMaxLength(30);

            builder.Entity<Talep>()
                .Property(t => t.TeslimatSehir)
                .HasMaxLength(60);

            builder.Entity<Talep>()
                .Property(t => t.TercihEdilenSehirler)
                .HasMaxLength(200);

            builder.Entity<Talep>()
                .HasIndex(t => new { t.TalepTuru, t.Durum, t.OlusturmaTarihi });

            builder.Entity<TalepTeklif>()
                .Property(t => t.Durum)
                .HasMaxLength(20);

            builder.Entity<TalepTeklif>()
                .Property(t => t.GonderimSehir)
                .HasMaxLength(60);

            builder.Entity<TalepTeklif>()
                .HasIndex(t => new { t.TalepId, t.SaticiId })
                .IsUnique();

            builder.Entity<Urun>()
                .Property(u => u.BirimMiktar)
                .HasMaxLength(50);

            builder.Entity<Urun>()
                .Property(u => u.TeslimatSecenekleri)
                .HasMaxLength(200);

            builder.Entity<Urun>()
                .Property(u => u.UretimSehir)
                .HasMaxLength(100);

            builder.Entity<Urun>()
                .Property(u => u.UretimIlce)
                .HasMaxLength(100);

            builder.Entity<Urun>()
                .Property(u => u.GonderimSehirleri)
                .HasMaxLength(500);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Urun)
                .WithMany()
                .HasForeignKey(m => m.UrunId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Talep)
                .WithMany()
                .HasForeignKey(m => m.TalepId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ChatMessage>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId, m.SentAt });

            builder.Entity<PlanliGonderim>()
                .HasOne(p => p.Satici)
                .WithMany()
                .HasForeignKey(p => p.SaticiId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlanliGonderim>()
                .HasIndex(p => new { p.HedefSehir, p.AktifMi, p.GonderimTarihi });

            builder.Entity<Urun>()
                  .HasIndex(u => u.SaticiId);

            builder.Entity<Urun>()
                .HasIndex(u => u.KategoriId);





        }

    }
}
