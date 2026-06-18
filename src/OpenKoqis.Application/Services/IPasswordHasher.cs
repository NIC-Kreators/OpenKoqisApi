using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKoqis.Application.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);

        bool VerifyPassword(string providedPassword, string hashedPassword);
    }
}
