namespace uMarket.Server.Models.Sellers
{
    public class SellerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? CurrentDescription { get; set; }
        public bool IsAvailable { get; set; }
    }
}
