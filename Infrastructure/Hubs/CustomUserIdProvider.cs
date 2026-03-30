using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Hubs
{

    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // JWT token içindeki kullanıcı kimliğini SignalR tarafına taşıyoruz.
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

    }
}
