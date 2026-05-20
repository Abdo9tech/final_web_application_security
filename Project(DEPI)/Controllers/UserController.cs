using BookifyHotel.Data;
using BookifyHotel.Model;
using DAL.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLL.Services;

namespace Project_DEPI_.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly BookingService _bookingService;
        private readonly UserRoleService _userRoleService;
        private readonly ReservationCartService _reservationCartService;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly BookifyHotelDbContext _context; // أضف هذا

        public UserController(
            UserService userService,
            UserRoleService userRoleService,
            ReservationCartService reservationCartService,
            BookingService bookingService,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            BookifyHotelDbContext context) // أضف هذا
        {
            _userService = userService;
            _reservationCartService = reservationCartService;
            _bookingService = bookingService;
            _userRoleService = userRoleService;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context; // أضف هذا
        }


        #region Admin Dashboard  CRUD Operations 
        public IActionResult Index()
        {
            // Get all users with their roles
            var users = _userService.GetAll();

            // Create ViewModel to pass user data with roles
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                // Get user roles
                var userRoles = _userRoleService.GetUserRoles(user.Id);
                var roleNames = userRoles.Select(ur => ur.Role?.Name).Where(r => r != null).Cast<string>().ToList();

                var userViewModel = new UserViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roleNames,
                    RoleNames = string.Join(", ", roleNames)
                };

                userViewModels.Add(userViewModel);
            }

            return View(userViewModels);
        }
















        public IActionResult Details(int id)
        {
            // نسخة أبسط بدون العلاقات
            var user = _userService.GetById(id);

            if (user == null)
            {
                return NotFound();
            }

            // إحصائيات منفصلة
            ViewBag.BookingsCount = _bookingService.GetUserBookingsCount(id);
            ViewBag.ReservationsCount = _reservationCartService.GetUserReservationsCount(id);

            return View(user);
        }







        public IActionResult Delete(int id)
        {
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }
            _userService.Delete(id); 
            return RedirectToAction(nameof(Index));
        }









        // GET: User/Edit/5
        [Authorize(Roles = "Admin")] // ✅ فقط Admin يستطيع الوصول
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // ✅ التحقق من أن المستخدم الحالي هو Admin
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Login");
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                if (!isAdmin)
                {
                    TempData["Error"] = "You don't have permission to edit users.";
                    return RedirectToAction("Index");
                }

                var user = _userService.GetById(id);

                if (user == null)
                {
                    return NotFound();
                }

                // ✅ منع المستخدم العادي من تعديل نفسه (إلا إذا كان Admin)
                if (user.IdentityUserId == currentUser.Id && !isAdmin)
                {
                    TempData["Error"] = "You cannot edit your own profile from admin panel.";
                    return RedirectToAction("Index");
                }

                // احصل على أدوار المستخدم من Identity
                if (!string.IsNullOrEmpty(user.IdentityUserId))
                {
                    var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
                    if (identityUser != null)
                    {
                        var roles = await _userManager.GetRolesAsync(identityUser);
                        ViewBag.UserRole = roles.FirstOrDefault() ?? "User";
                        ViewBag.CurrentUserRole = ViewBag.UserRole;
                    }
                }

                ViewBag.BookingsCount = _bookingService.GetUserBookingsCount(id);
                ViewBag.ReservationsCount = _reservationCartService.GetUserReservationsCount(id);
                ViewBag.ActiveBookingsCount = _bookingService.GetActiveUserBookingsCount(id);

                // ✅ معرفة إذا كان المستخدم الحالي يحاول تعديل نفسه
                ViewBag.IsEditingSelf = (user.IdentityUserId == currentUser.Id);
                ViewBag.CurrentUserName = currentUser.UserName;

                var model = new EditUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    IdentityUserId = user.IdentityUserId
                };

                return View(model);
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while loading user for editing.";
                return RedirectToAction("Index");
            }
        }

        // POST: User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // ✅ فقط Admin يستطيع التعديل
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ التحقق من أن المستخدم الحالي هو Admin
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return RedirectToAction("Login", "Login");
                    }

                    var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                    if (!isAdmin)
                    {
                        TempData["Error"] = "You don't have permission to edit users.";
                        return RedirectToAction("Index");
                    }

                    // الحصول على الـ UserProfile الحالي
                    var userProfile = _userService.GetById(model.Id);
                    if (userProfile == null)
                    {
                        return NotFound();
                    }

                    // ✅ منع Admin من تعديل نفسه (يجب أن يعدل من صفحة Profile الخاصة به)
                    if (userProfile.IdentityUserId == currentUser.Id)
                    {
                        TempData["Warning"] = "Please edit your own profile from your account settings, not from admin panel.";
                        return RedirectToAction("Index", "Profile");
                    }

                    // ✅ التحقق: لا يمكن تحويل Admin آخر إلى User (مستوى أقل)
                    var targetIdentityUser = await _userManager.FindByIdAsync(userProfile.IdentityUserId);
                    if (targetIdentityUser != null)
                    {
                        var targetUserRoles = await _userManager.GetRolesAsync(targetIdentityUser);
                        var isTargetUserAdmin = targetUserRoles.Contains("Admin");

                        if (isTargetUserAdmin && model.UserRole == "User")
                        {
                            TempData["Error"] = "You cannot downgrade another Administrator to User.";
                            return View(model);
                        }
                    }

                    // ✅ تحديث بيانات UserProfile
                    userProfile.FullName = model.FullName;
                    userProfile.Email = model.Email;
                    userProfile.PhoneNumber = model.PhoneNumber;

                    _userService.Update(userProfile);

                    // ✅ تحديث بيانات Identity User
                    if (!string.IsNullOrEmpty(userProfile.IdentityUserId))
                    {
                        var identityUser = await _userManager.FindByIdAsync(userProfile.IdentityUserId);
                        if (identityUser != null)
                        {
                            // تحديث البيانات الأساسية
                            identityUser.UserName = model.Email;
                            identityUser.Email = model.Email;
                            identityUser.PhoneNumber = model.PhoneNumber;

                            await _userManager.UpdateAsync(identityUser);

                            // ✅ تحديث كلمة المرور إذا تم إدخال واحدة جديدة
                            if (!string.IsNullOrEmpty(model.NewPassword))
                            {
                                var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                                var result = await _userManager.ResetPasswordAsync(identityUser, token, model.NewPassword);

                                if (!result.Succeeded)
                                {
                                    foreach (var error in result.Errors)
                                    {
                                        ModelState.AddModelError("", error.Description);
                                    }
                                    return View(model);
                                }
                            }

                            // ✅ تحديث الدور إذا تغير
                            var currentRoles = await _userManager.GetRolesAsync(identityUser);
                            var currentRole = currentRoles.FirstOrDefault();

                            if (!string.IsNullOrEmpty(model.UserRole) && currentRole != model.UserRole)
                            {
                                // تأكيد تغيير الدور الخطير
                                if (currentRole == "Admin" && model.UserRole != "Admin")
                                {
                                    if (!await ConfirmRoleChange("downgrade"))
                                    {
                                        TempData["Warning"] = "Role change cancelled.";
                                        return View(model);
                                    }
                                }
                                else if (model.UserRole == "Admin")
                                {
                                    if (!await ConfirmRoleChange("promote"))
                                    {
                                        TempData["Warning"] = "Role change cancelled.";
                                        return View(model);
                                    }
                                }

                                // إزالة الأدوار القديمة
                                await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);

                                // إضافة الدور الجديد
                                await _userManager.AddToRoleAsync(identityUser, model.UserRole);
                            }
                        }
                    }

                    TempData["Success"] = "✅ User updated successfully!";
                    return RedirectToAction("Details", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"❌ Error updating user: {ex.Message}");
                    return View(model);
                }
            }

            return View(model);
        }

        // ✅ دالة تأكيد تغيير الدور
        private Task<bool> ConfirmRoleChange(string actionType)
        {
            // في التطبيق الحقيقي، يمكن استخدام JavaScript confirm
            // هنا نستخدم TempData لعرض رسالة تأكيد في الـ View
            TempData["NeedConfirmation"] = actionType;
            return Task.FromResult(false); // سيتولى الـ View عملية التأكيد
        }















        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. التحقق من وجود البريد الإلكتروني
                        var existingUser = await _userManager.FindByEmailAsync(model.Email);
                        if (existingUser != null)
                        {
                            ModelState.AddModelError("Email", "Email already exists!");
                            return View(model);
                        }

                        // 2. إنشاء المستخدم في AspNetUsers
                        var identityUser = new AppUser
                        {
                            UserName = model.Email,
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            EmailConfirmed = true
                        };

                        var result = await _userManager.CreateAsync(identityUser, model.Password);

                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                            return View(model);
                        }

                        // 3. تأكد من وجود الـ Role في Identity
                        if (!await _roleManager.RoleExistsAsync(model.UserRole))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(model.UserRole));
                        }

                        // 4. تعيين الدور في Identity
                        await _userManager.AddToRoleAsync(identityUser, model.UserRole);

                        // 5. الحصول على الـ Role من Identity (لاحظ: RoleId أصبح string)
                        var role = await _roleManager.FindByNameAsync(model.UserRole);

                        // 6. إنشاء UserProfile
                        var userProfile = new UserProfile
                        {
                            IdentityUserId = identityUser.Id,
                            FullName = model.FullName,
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            Address = "",
                            City = "",
                            Country = "",
                            CreatedDate = DateTime.Now,
                            ProfilePhotoPath = ""
                        };

                        _userService.Create(userProfile);

                        // 7. إنشاء العلاقة في User_Roles
                        var userRoleEntity = new User_Role
                        {
                            UserProfileId = userProfile.Id,
                            RoleId = role?.Id ?? "User" // ✅ RoleId أصبح string
                        };

                        _userRoleService.Create(userRoleEntity);

                        // 8. تأكيد العملية
                        await transaction.CommitAsync();

                        TempData["Success"] = $"✅ User '{model.FullName}' created successfully as {model.UserRole}!";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"❌ Error creating user: {ex.Message}");
                        return View(model);
                    }
                }
            }

            return View(model);
        }

        #endregion




    }
}
