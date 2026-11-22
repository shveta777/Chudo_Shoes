using Chudo_obuv.ScaffoldedModels;
namespace Chudo_obuv.Statics
{
    public static class CurrentSession
    {
        internal const int AdministratorRoleId = 1;
        internal const int ManagerRoleId = 2;
        internal const int CustomerRoleId = 3;

        public static User CurrentUser { get; set; }

        internal static bool CanManageOrders(User user)
        {
            return user?.RoleId == AdministratorRoleId || user?.RoleId == ManagerRoleId;
        }
    }
}
