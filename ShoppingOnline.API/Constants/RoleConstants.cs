namespace ShoppingOnline.API.Constants
{
    public static class RoleConstants
    {
        public static class Names
        {
            public const string Admin = "Admin";
            public const string ProductManager = "Product Manager";
            public const string OrderManager = "Order Manager";
            public const string Account = "Account";
            public const string Shipper = "Shipper";
            public const string Customer = "Customer";
        }

        public static class Descriptions
        {
            public const string Admin = "Quản trị hệ thống";
            public const string ProductManager = "Quản lý sản phẩm";
            public const string OrderManager = "Quản lý đơn hàng";
            public const string Account = "Kế toán";
            public const string Shipper = "Nhân viên giao hàng";
            public const string Customer = "Khách hàng";
        }

        public static class Permissions
        {
            public static readonly string[] AdminPermissions = { "all" };
            public static readonly string[] ProductManagerPermissions = { "products.*", "categories.*" };
            public static readonly string[] OrderManagerPermissions = { "orders.*", "payments.*" };
            public static readonly string[] AccountPermissions = { "reports.*", "payments.view" };
            public static readonly string[] ShipperPermissions = { "shipping.*", "orders.view" };
            public static readonly string[] CustomerPermissions = { "products.view", "orders.create", "reviews.*" };
        }
    }
}
