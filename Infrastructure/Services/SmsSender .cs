using Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SmsSender : ISmsSender
    {
        public Task SendSmsAsync(string phoneNumber, string message)
        {
            Console.WriteLine($"[SMS to:{phoneNumber}] {message}");
            return Task.CompletedTask;
        }
    }

}
