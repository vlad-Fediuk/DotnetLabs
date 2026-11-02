using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Shared.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public HasPermissionAttribute(string permission) : base(permission)
    {
        Permission = permission;
    }
}
