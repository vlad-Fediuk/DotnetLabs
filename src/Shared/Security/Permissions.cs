namespace ECommerce.Shared.Security;

public static class Permissions
{
    public const string ClaimType = "permission";

    public static class Catalog
    {
        public const string View = "catalog:view";
        public const string Manage = "catalog:manage";
    }

    public static class Orders
    {
        public const string Create = "orders:create";
        public const string ViewOwn = "orders:view-own";
        public const string ViewAll = "orders:view-all";
        public const string ManageStatus = "orders:manage-status";
    }

    public static class Users
    {
        public const string View = "users:view";
        public const string ManageRoles = "users:manage-roles";
    }

    public static class System
    {
        public const string ManageCache = "system:manage-cache";
    }

    public static readonly IReadOnlyList<string> All = new[]
    {
        Catalog.View,
        Catalog.Manage,
        Orders.Create,
        Orders.ViewOwn,
        Orders.ViewAll,
        Orders.ManageStatus,
        Users.View,
        Users.ManageRoles,
        System.ManageCache
    };
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string CatalogManager = "CatalogManager";
    public const string OrderOperator = "OrderOperator";
    public const string Support = "Support";
    public const string User = "User";

    public static readonly IReadOnlyList<string> All = new[]
    {
        User,
        Admin,
        Manager,
        CatalogManager,
        OrderOperator,
        Support
    };
}

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Matrix =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Admin] = Permissions.All,
            [RoleNames.Manager] = new[]
            {
                Permissions.Catalog.View,
                Permissions.Catalog.Manage,
                Permissions.Orders.Create,
                Permissions.Orders.ViewOwn,
                Permissions.Orders.ViewAll,
                Permissions.Orders.ManageStatus,
                Permissions.Users.View
            },
            [RoleNames.CatalogManager] = new[]
            {
                Permissions.Catalog.View,
                Permissions.Catalog.Manage,
                Permissions.Orders.Create,
                Permissions.Orders.ViewOwn
            },
            [RoleNames.OrderOperator] = new[]
            {
                Permissions.Catalog.View,
                Permissions.Orders.Create,
                Permissions.Orders.ViewOwn,
                Permissions.Orders.ViewAll,
                Permissions.Orders.ManageStatus
            },
            [RoleNames.Support] = new[]
            {
                Permissions.Catalog.View,
                Permissions.Orders.ViewOwn,
                Permissions.Orders.ViewAll,
                Permissions.Users.View
            },
            [RoleNames.User] = new[]
            {
                Permissions.Catalog.View,
                Permissions.Orders.Create,
                Permissions.Orders.ViewOwn
            }
        };

    public static IReadOnlyList<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            if (Matrix.TryGetValue(role, out var rolePerms))
            {
                foreach (var perm in rolePerms)
                {
                    permissions.Add(perm);
                }
            }
        }

        return permissions.ToList();
    }

    public static bool HasPermission(IEnumerable<string> roles, string permission)
    {
        foreach (var role in roles)
        {
            if (Matrix.TryGetValue(role, out var rolePerms) &&
                rolePerms.Contains(permission, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
