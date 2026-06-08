using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SmartBin.Domain.Models; // Для доступа к UserRole, AdminRole и т.д.
using System;
using System.Linq;

namespace SmartBin.Api.Extensions
{
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Проверяет, обладает ли текущий аутентифицированный пользователь (ClaimsPrincipal) 
        /// правами, необходимыми для требуемой роли.
        /// </summary>
        /// <param name="principal">Объект текущего пользователя, извлеченный из JWT токена.</param>
        /// <param name="requiredRole">Минимальная роль, необходимая для доступа (напр., AdminRole.Instance).</param>
        /// <returns>True, если пользователь обладает требуемыми правами.</returns>
        public static bool ValidateToken(this ClaimsPrincipal principal, UserRole requiredRole)
        {
            // 1. Проверяем, аутентифицирован ли пользователь.
            if (principal == null || !principal.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            // 2. Находим Claim с типом Role. 
            // ClaimTypes.Role — это стандартный ключ, который мы использовали в JwtService.
            var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            if (roleClaim == null)
            {
                // Токен действителен, но не содержит информацию о роли (должен содержать).
                // Считаем, что прав нет.
                return false;
            }

            // 3. Парсинг строкового значения (например, "Admin" или "SalesManager") обратно в объект record UserRole.
            UserRole userRole;
            try
            {
                // Используем статический метод Parse, определенный в SmartBin.Domain/UserRole.cs
                userRole = UserRole.Parse(roleClaim.Value);
            }
            catch (ArgumentException)
            {
                // Токен содержит неизвестную роль.
                return false;
            }
            catch (Exception)
            {
                // Общая ошибка при парсинге
                return false;
            }

            // 4. Выполнение проверки иерархии прав.
            // Используем логику, инкапсулированную в record (например, AdminRole.HasPermissionsOf(SalesManagerRole))
            return userRole.HasPermissionsOf(requiredRole);
        }
        
        public static IServiceCollection AddAuthorizationSecPolicies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthorization(options =>
            {

                options.AddPolicy("MinimumRole_Admin", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context => context.User.ValidateToken(AdminRole.Instance));
                });

                options.AddPolicy("MinimumRole_SalesManager", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context => context.User.ValidateToken(SalesManagerRole.Instance));
                });

                options.AddPolicy("MinimumRole_Guest", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context => context.User.ValidateToken(GuestRole.Instance));
                });
            });
            return services;
        }
    }
}