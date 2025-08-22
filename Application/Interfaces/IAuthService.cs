using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Succeeded, string? Error)> RegisterSaticiAsync(RegisterSaticiDto dto);
        Task<(bool Succeeded, string? Error)> RegisterAliciAsync(RegisterAliciDto dto);
        Task<(bool Succeeded, string? Token, string? Error)> LoginAsync(LoginDto dto);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task<bool> ConfirmPhoneAsync(string userId, string token);
    }
}
