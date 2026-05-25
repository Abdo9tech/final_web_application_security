using System;

namespace BookifyHotel.Model
{
    public class SearchHistory
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Query { get; set; }
        public DateTime SearchDate { get; set; }
    }
}
