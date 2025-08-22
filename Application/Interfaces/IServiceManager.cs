using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IServiceManager
    {
        IEmailSend EmailSender { get; }
        ISmsSender SmsSender { get; }
        ISaticiProfiliService SaticiProfiliService { get; }
        IUrunService UrunService { get; }
        IDosyaKaydetService DosyaKaydetService { get; }

        IYorumServices YorumService { get; }

        IPuanService PuanService { get; }

        // Diğer servisler eklenebilir:
        // IProductService ProductService { get; }
        // IOrderService OrderService { get; }
    }
}
