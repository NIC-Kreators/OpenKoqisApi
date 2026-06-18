using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKoqis.Application.Services
{
    public interface IPasswordHasher
    {
        // Создает хеш пароля для сохранения в базе данных
        string HashPassword(string password);

        // Сравнивает предоставленный пароль с сохраненным хешем
        bool VerifyPassword(string providedPassword, string hashedPassword);
    }
}
