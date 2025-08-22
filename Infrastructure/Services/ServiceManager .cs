using Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ServiceManager :  IServiceManager
    {
        public IEmailSend EmailSender { get; }
        public ISmsSender SmsSender { get; }

        public ISaticiProfiliService SaticiProfiliService { get; }

        public IUrunService UrunService { get; set; }

        public IDosyaKaydetService DosyaKaydetService { get; }
        public IYorumServices YorumService { get;  }

        public IPuanService PuanService { get;  }

        public ServiceManager(
            IEmailSend emailSender,
            ISmsSender smsSender,
            ISaticiProfiliService saticiProfiliService,
            IUrunService urunService,
            IDosyaKaydetService dosyaKaydetService,
            IYorumServices yorumService,
            IPuanService puanService)
        {
            EmailSender = emailSender;
            SmsSender = smsSender;
            SaticiProfiliService = saticiProfiliService;
            UrunService = urunService;
            DosyaKaydetService = dosyaKaydetService;
            YorumService = yorumService;
            PuanService = puanService;
        }

        // Diğer servisler burada eklenebilir...
    }
}