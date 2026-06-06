using Microsoft.AspNetCore.Authorization;
using SmartBin.Domain.Models; 
using System;

namespace SmartBin.Api.Attributes
{
    // inherit from the base attribute AuthorizeAttribute
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        // Constant for policy name
        private const string PolicyPrefix = "MinimumRole_";

        // Proprety for storing the required role
        public UserRole RequiredRole { get; }

        public AuthorizeRoleAttribute(Type roleType)
        {
            if (!typeof(UserRole).IsAssignableFrom(roleType))
            {
                throw new ArgumentException($"Тип {roleType.Name} должен наследоваться от SmartBin.Domain.Models.UserRole.");
            }
            
            // Getting static field Instance to access the role name (for example, "Admin")
            var roleInstance = roleType.GetField("Instance")?.GetValue(null) as UserRole;

            if (roleInstance == null)
            {
                throw new InvalidOperationException($"Тип {roleType.Name} должен содержать статическое поле 'Instance'.");
            }

            this.RequiredRole = roleInstance;

            // Setting policy name
            this.Policy = $"{PolicyPrefix}{roleInstance.Name}";
        }
    }
}