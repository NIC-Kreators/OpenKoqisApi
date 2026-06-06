using System.Security.Claims;
using SmartBin.Domain.Models; // Для доступа к UserRole, AdminRole и т.д.
using System;
using System.Linq;

namespace SmartBin.Api.Extensions
{
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Checks whether the current authenticated user (ClaimsPrincipal) 
        /// possesses the permissions required for the specified role.
        /// </summary>
        /// <param name="principal">The current user object extracted from the JWT token.</param>
        /// <param name="requiredRole">The minimum role required for access (e.g., AdminRole.Instance).</param>
        /// <returns>True if the user possesses the required permissions.</returns>
        public static bool ValidateToken(this ClaimsPrincipal principal, UserRole requiredRole)
        {
            // 1. Checking if user is authenthicated.
            if (principal == null || !principal.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            // 2. Find  Claim with type Role. 
            var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            if (roleClaim == null)
            {
                return false;
            }

            // 3. Parsing string type back to record UserRole object.
            UserRole userRole;
            try
            {
                userRole = UserRole.Parse(roleClaim.Value);
            }
            catch (ArgumentException)
            {
                // Token carries invalid role.
                return false;
            }
            catch (Exception)
            {
                return false;
            }

            // 4. Executing the rights hierarchy check.
            return userRole.HasPermissionsOf(requiredRole);
        }
    }
}