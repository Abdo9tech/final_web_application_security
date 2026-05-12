using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookifyHotel.Model
{
    public class RoomType
    {
        public int RoomTypeId { get; set; }
        public string Name { get; set; } = string.Empty; 
        public decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public int Capacity { get; set; } = 2; // Default to 2 guests
     

        public virtual ICollection<Room> Rooms { get; set; } = null!;
        
    }
}
