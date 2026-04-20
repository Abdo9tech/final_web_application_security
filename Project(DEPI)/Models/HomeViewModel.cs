using BookifyHotel.Model;

namespace Project_DEPI_.Models
{
    public class HomeViewModel
    {
        public IEnumerable<Room> FeaturedRooms { get; set; } = new List<Room>();
    }
}
