using System;

namespace uMarket.Models.Sellers
{
    public class PendingRequestDto
    {
        public Guid RequestId { get; set; }
        public string FromUserId { get; set; } = string.Empty;
        public string? FromFullName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UpdateSellerRequest
    {
        public bool IsAvailable { get; set; }
        public string? CurrentDescription { get; set; }
    }
}
