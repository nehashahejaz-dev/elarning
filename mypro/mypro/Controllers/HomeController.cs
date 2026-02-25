using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mypro.Models;
using System.Diagnostics;
using System.Security.Claims;
namespace mypro.Controllers
{
    public class HomeController : Controller
    {
        EleraningContext db = new EleraningContext();

        public IActionResult Index()
        {
            var categories = db.Categories.Take(6).ToList();

            // 2. Top 4 Featured Courses fetch karna (with Instructor info)
            var courses = db.Courses
                            .Include(c => c.Instructor)
                            .OrderByDescending(c => c.CourseId) // Naye courses pehle
                            .Take(4)
                            .ToList();

            // Data ko View pe bhejna
            ViewBag.Categories = categories;
            ViewBag.Courses = courses;
            var topStudents = db.StudentAttempts
         .Include(sa => sa.User) // User ka naam lene ke liye
         .GroupBy(sa => new { sa.UserId, sa.User.FullName }) // Student wise group karna
         .Select(group => new {
             FullName = group.Key.FullName,
             TotalScore = group.Sum(sa => sa.ScoreObtained ?? 0), // Total marks ka sum
             TotalPassed = group.Count(sa => sa.IsPassed == true) // Kitne quizzes pass kiye
         })
         .OrderByDescending(x => x.TotalScore) // Jis ke marks zyada wo upar
         .Take(2) // Top 2 students
         .ToList();

            ViewBag.TopStudents = topStudents;
            return View();
        }
           
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // 1. Database se user ko email ke zariye dhonndain (Include Role zaroori hai)
            var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                // 2. BCrypt ke zariye password verify karein
                bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (isValid)
                {
                    // 3. Cookies mein data save karne ke liye Claims banayein
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName), // Role name yahan kaam ayega
                new Claim("UserId", user.UserId.ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    // 4. Role based Redirect
                    if (user.Role.RoleName == "Admin" || user.Role.RoleName == "Teacher")
                    {
                        return RedirectToAction("Index", "Admin"); // Admin Panel
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home"); // User Panel
                    }
                }
            }

            // Agar login fail ho jaye
            ViewBag.Error = "Invalid email or password";
            return View();
        }
        public IActionResult Register()
        {
            var studentRoleId = db.Roles
                          .Where(r => r.RoleName == "Student")
                          .Select(r => r.RoleId)
                          .FirstOrDefault();

            ViewBag.StudentId = studentRoleId;
            return View();
        }
        [HttpPost]
        public IActionResult Register(User user)
        {


            // validation error ho to same view ke sath data wapas
            if (ModelState.IsValid)
            {
                var isEmailExist = db.Users.Any(u => u.Email == user.Email);

                if (isEmailExist)
                {
                    // 2. Agar email mil jaye, toh custom error message dikhayein
                    ModelState.AddModelError("Email", "Email already exists. Please use another one.");

                    // Dropdown ya ViewBag dobara bharein (agar zaroorat ho)
                    ViewBag.StudentId = user.RoleId;
                    return View(user);
                }



                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(user);
        

        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "home");
        }
        public IActionResult course(int? categoryId, string searchTerm, string level, string priceType)
        {

            var query = db.Courses
         .Include(c => c.Category)
         .Include(c => c.Instructor)
         .AsQueryable();

            // 1. Category Filter
            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId);
            }

            // 2. Search Logic
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Title.Contains(searchTerm) || c.Description.Contains(searchTerm));
            }

            // 3. Price Filter (Decimal handling)
            if (priceType == "Free")
            {
                query = query.Where(c => c.Price == 0);
            }
            else if (priceType == "Paid")
            {
                query = query.Where(c => c.Price > 0);
            }

            // Dropdowns aur selection state ke liye
            ViewBag.Categories = db.Categories.ToList();
            ViewBag.SelectedCat = categoryId;
            ViewBag.SelectedPrice = priceType;

            return View(query.ToList());
        }
        public IActionResult detail(int id)
        {
            var course = db.Courses
         .Include(c => c.Category)
         .Include(c => c.Instructor)
         .Include(c => c.Sections) // Sections dikhane ke liye
         .FirstOrDefault(c => c.CourseId == id);

            if (course == null) return NotFound();
            return View(course);
        }
    }
}
