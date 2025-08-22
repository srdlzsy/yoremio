using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
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

        public DbSet<SaticiProfili> SaticiProfilleri { get; set; }
        public DbSet<AliciProfili> AliciProfilleri { get; set; }
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<Kategori> Kategoriler { get; set; }
        public DbSet<UrunResim> UrunResimler { get; set; }
        public DbSet<UrunVideo> UrunVideolar { get; set; }
        public DbSet<Yorum> Yorumlar { get; set; }
        public DbSet<Puan> Puanlar { get; set; }

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
                .HasMany(u => u.yorumlar)
                .WithOne(y => y.Urun)
                .HasForeignKey(y => y.UrunId)
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


            builder.Entity<Puan>()
    .HasIndex(p => new { p.KullaniciId, p.UrunId })
    .IsUnique();




        }

    }
}
