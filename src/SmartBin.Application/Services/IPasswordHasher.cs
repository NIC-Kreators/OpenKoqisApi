using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBin.Application.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);

        bool VerifyPassword(string providedPassword, string hashedPassword);
    }
}
