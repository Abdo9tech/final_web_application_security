namespace Project_DEPI.ViewModels
{
    public class DashboardBookingViewModel
    {
        public int BookingId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int RoomNumber { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }
}
