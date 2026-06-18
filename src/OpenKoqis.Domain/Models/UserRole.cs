namespace OpenKoqis.Domain.Models
{
    // Базовый record для всех ролей. Обязательно abstract.
    public abstract record UserRole
    {
        // Уникальный строковый идентификатор, который будет использоваться в JWT Claims.
        public abstract string Name { get; }

        // Список прав, которыми обладает данная роль.
        // Это можно использовать для более сложной авторизации, кроме иерархии.
        public virtual List<string> Permissions => new List<string>();

        // Метод для сравнения ролей (аналог скриншота "сравнение").
        // Проверяет, обладает ли текущая роль правами, как минимум, сравниваемой роли.
        public abstract bool HasPermissionsOf(UserRole otherRole);

        // Статический метод для парсинга строки из Claims обратно в объект Record.
        public static UserRole Parse(string roleName)
        {
            return roleName switch
            {
                "Admin" => AdminRole.Instance,
                "SalesManager" => SalesManagerRole.Instance,
                "Guest" => GuestRole.Instance,
                _ => throw new ArgumentException($"Unknown role: {roleName}")
            };
        }
    }

    // --- Конкретные Роли (Singletons для сравнения) ---

    public record AdminRole : UserRole
    {
        public static readonly AdminRole Instance = new();
        public override string Name => "Admin";

        // Admin обладает правами всех остальных ролей
        public override bool HasPermissionsOf(UserRole otherRole) => true;
    }

    public record SalesManagerRole : UserRole
    {
        public static readonly SalesManagerRole Instance = new();
        public override string Name => "SalesManager";

        // SalesManager обладает правами Guest
        public override bool HasPermissionsOf(UserRole otherRole)
        {
            if (otherRole is AdminRole) return false;
            return true; // Обладает правами всех, кроме Admin
        }
    }

    public record GuestRole : UserRole
    {
        public static readonly GuestRole Instance = new();
        public override string Name => "Guest";

        // Guest обладает правами только самого себя
        public override bool HasPermissionsOf(UserRole otherRole)
        {
            return otherRole is GuestRole;
        }
    }
}