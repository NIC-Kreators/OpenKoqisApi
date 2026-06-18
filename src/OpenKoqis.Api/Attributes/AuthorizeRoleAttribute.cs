using Microsoft.AspNetCore.Authorization;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRoleAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "MinimumRole_";
    public UserRole RequiredRole { get; }

    public AuthorizeRoleAttribute(Type roleType)
    {
        if (!typeof(UserRole).IsAssignableFrom(roleType))
        {
            throw new ArgumentException($"Type {roleType.Name} must inherit from OpenKoqis.Domain.Models.UserRole.");
        }

        // We cannot save the Role instance itself, but we can save its Name,
        // which will be used in the policy.

        // Get the static field Instance to access the role name (e.g., "Admin")
        var roleInstance = roleType.GetField("Instance")?.GetValue(null) as UserRole;

        RequiredRole = roleInstance ?? throw new InvalidOperationException($"Type {roleType.Name} must contain a static field 'Instance'.");

        // Key step: Setting the policy name
        // For example: Policy = "MinimumRole_Admin"
        Policy = $"{PolicyPrefix}{roleInstance.Name}";
    }
}
