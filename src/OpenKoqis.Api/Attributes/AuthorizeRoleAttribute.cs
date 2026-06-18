using Microsoft.AspNetCore.Authorization;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Attributes;

// Наследуемся от базового атрибута AuthorizeAttribute
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRoleAttribute : AuthorizeAttribute
{
    // 💡 Константа для имени политики
    private const string PolicyPrefix = "MinimumRole_";

    // 💡 Свойство для хранения требуемой роли
    public UserRole RequiredRole { get; }

    public AuthorizeRoleAttribute(Type roleType)
    {
        if (!typeof(UserRole).IsAssignableFrom(roleType))
        {
            throw new ArgumentException($"Тип {roleType.Name} должен наследоваться от OpenKoqis.Domain.Models.UserRole.");
        }

        // Мы не можем сохранить сам экземпляр Role, но можем сохранить его Name, 
        // который будет использоваться в политике.

        // Получаем статическое поле Instance для доступа к имени роли (например, "Admin")
        var roleInstance = roleType.GetField("Instance")?.GetValue(null) as UserRole;

        RequiredRole = roleInstance ?? throw new InvalidOperationException($"Тип {roleType.Name} должен содержать статическое поле 'Instance'.");

        // Ключевой шаг: Установка имени политики
        // Например: Policy = "MinimumRole_Admin"
        Policy = $"{PolicyPrefix}{roleInstance.Name}";
    }
}