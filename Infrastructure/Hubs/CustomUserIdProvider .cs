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
        public string GetUserId(HubConnectionContext connection)
        {
            // JWT Token içindeki NameIdentifier (genelde userId veya email olur)
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

    }
}