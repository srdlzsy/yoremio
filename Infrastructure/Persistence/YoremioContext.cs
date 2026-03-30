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
        public DbSet<Yorum> Yorumlar { get; set; } = null!;
        public DbSet<Puan> Puanlar { get; set; } = null!;

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
                .OnDelete(DeleteBehavior.Cascade);

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

            builder.Entity<UrunFavori>()
                .HasIndex(f => new { f.KullaniciId, f.UrunId })
                .IsUnique();

            builder.Entity<Talep>()
                .Property(t => t.Durum)
                .HasMaxLength(20);

            builder.Entity<TalepTeklif>()
                .Property(t => t.Durum)
                .HasMaxLength(20);

            builder.Entity<TalepTeklif>()
                .HasIndex(t => new { t.TalepId, t.SaticiId })
                .IsUnique();

            builder.Entity<Urun>()
                  .HasIndex(u => u.SaticiId);

            builder.Entity<Urun>()
                .HasIndex(u => u.KategoriId);





        }

    }
}
