using BookifyHotel.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PLL.Services;

namespace Project_DEPI_.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoomTypeController : Controller
    {
        private readonly RoomTypeService _roomTypeService;
        public RoomTypeController(RoomTypeService roomTypeService)
        {
            _roomTypeService = roomTypeService;
        }


        #region Admin Dashboard  CRUD Operations
        public IActionResult Index()  // Admin View Dashboard
        {
            var roomTypes = _roomTypeService.GetAll();
            return View(roomTypes);
        }
        public IActionResult Details(int id)
        {
            var roomType = _roomTypeService.GetById(id);
            if (roomType == null)
            {
                return NotFound();
            }
            return View(roomType);

        }

        public IActionResult Create()   //     /RoomType/Create 
        {
            return View(new RoomType());
        }
        [HttpPost]
        public IActionResult CreateSave(RoomType roomType)   //   
        {

            _roomTypeService.Create(roomType);
            return RedirectToAction("Index");

        }
        public IActionResult Delete(int id)
        {
            var roomType = _roomTypeService.GetById(id);
            if (roomType == null)
            {
                return NotFound();
            }
            _roomTypeService.Delete(id);
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var roomType = _roomTypeService.GetById(id);
            if (roomType == null)
            {
                return NotFound();
            }
            return View(roomType);
        }
        [HttpPost]
        public IActionResult EditSave(RoomType roomType)
        {

            _roomTypeService.Update(roomType);
            return RedirectToAction("Index");

        }
        #endregion

    }
}
