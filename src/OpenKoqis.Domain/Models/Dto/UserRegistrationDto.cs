namespace OpenKoqis.Domain.Models.Dto;

public record UserRegistrationDto
{
    // Используется как логин/уникальный идентификатор
    public required string Nickname { get; set; }

    // Пароль в открытом виде для хеширования
    public required string Password { get; set; }

    // Используется для поля FullName в модели User
    public required string FullName { get; set; }
}
public record UserLoginDto
{
    // Используется для поиска пользователя в БД (соответствует полю Nickname в User)
    public required string Nickname { get; set; }

    // Пароль, введенный пользователем (будет хешироваться и сравниваться с PasswordHash)
    public required string Password { get; set; }
}