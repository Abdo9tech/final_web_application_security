using System.Collections.Generic;
using System.Threading.Tasks;
using Project_DEPI.Models;
namespace Project_DEPI.Services;
public interface ISmartSearchService
{
    Task<IEnumerable<RoomDto>> SearchAsync(string query, System.Guid userId);
    Task<IReadOnlyList<string>> GetUserHistoryAsync(System.Guid userId);
    Task<IReadOnlyList<string>> GetGlobalTopAsync();
}
