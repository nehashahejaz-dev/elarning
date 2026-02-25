using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mypro.Models;
using System.Data;
using Microsoft.AspNetCore.Authorization; // Required for Restriction
using Microsoft.AspNetCore.Authentication; // Required for Logout
using Microsoft.AspNetCore.Authentication.Cookies;

namespace mypro.Controllers
{
    // Sirf logged-in users hi is controller ko access kar saken ge
    [Authorize]
    public class AdminController : Controller
    {
        EleraningContext db = new EleraningContext();

      
          public IActionResult Index()
        {
            // 1. Top Summary Counts
            ViewBag.StudentCount = db.Users.Count(u => u.Role.RoleName == "Student");
            ViewBag.CourseCount = db.Courses.Count();
            ViewBag.QuizCount = db.Quizzes.Count();
            ViewBag.TeacherCount = db.Users.Count(u => u.Role.RoleName == "Teacher");

            // 2. Dynamic Graph Data: Course Name vs Student Count
            // Hum Enrollments table se data group karenge (Agar Enrollment table hai)
            // Agar enrollment table nahi hai, toh ye logic aapke schema ke mutabiq thora change hoga
            var courseStats = db.Courses
                .Select(c => new {
                    CourseName = c.Title,
                    StudentCount = db.Enrollments.Count(e => e.CourseId == c.CourseId)
                }).ToList();

            // Data ko separate lists mein convert karein taake JavaScript ko pass kar saken
            ViewBag.ChartLabels = courseStats.Select(x => x.CourseName).ToList();
            ViewBag.ChartData = courseStats.Select(x => x.StudentCount).ToList();

            return View();
        }
        

        // --- ADMIN ONLY SECTION ---
        // Sirf Admin hi Role aur User manage kar sakta hai
        [Authorize(Roles = "Admin")]
        public IActionResult AddRole() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult AddRole(Role role)
        {
            if (!ModelState.IsValid) return View(role);
            db.Roles.Add(role);
            db.SaveChanges();
            return RedirectToAction("Rolelist");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Rolelist() => View(db.Roles.ToList());

        [Authorize(Roles = "Admin")]
        public IActionResult teacherregister()
        {
            ViewBag.RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult teacherregister(User user)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName");
                return View(user);
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            db.Users.Add(user);
            db.SaveChanges();
            return RedirectToAction("Userlist");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Userlist() => View(db.Users.Include(u => u.Role).ToList());

        // --- SHARED SECTION (Admin & Teacher both can access) ---

        public IActionResult AddCategory() => View();

        [HttpPost]
        public IActionResult Addcategory(Category cate)
        {
            if (!ModelState.IsValid) return View(cate);
            db.Categories.Add(cate);
            db.SaveChanges();
            return RedirectToAction("Categorylist");
        }

        public IActionResult Categorylist() => View(db.Categories.ToList());

        public IActionResult AddCourse()
        {
            var teachers = db.Users.Where(u => u.Role.RoleName == "Teacher").ToList();
            ViewBag.InstructorList = new SelectList(teachers, "UserId", "FullName");
            ViewBag.CategoryList = new SelectList(db.Categories.ToList(), "CategoryId", "CategoryName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddCourse(Course course, IFormFile courseImage)
        {
            if (ModelState.IsValid)
            {
                if (courseImage != null && courseImage.Length > 0)
                {
                    string folder = "wwwroot/uploads/courses/";
                    string fileName = Guid.NewGuid().ToString() + "_" + courseImage.FileName;
                    string serverPath = Path.Combine(Directory.GetCurrentDirectory(), folder, fileName);
                    using (var stream = new FileStream(serverPath, FileMode.Create))
                    {
                        await courseImage.CopyToAsync(stream);
                    }
                    course.ThumbnailUrl = "/uploads/courses/" + fileName;
                }
                db.Courses.Add(course);
                await db.SaveChangesAsync();
                return RedirectToAction("courselist");
            }
            return View(course);
        }

        public IActionResult courselist()
        {
            var courses = db.Courses.Include(c => c.Category).Include(c => c.Instructor).ToList();
            return View(courses);
        }

        // --- LOGOUT LOGIC ---
        [Route("Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login","home");
        }

        // Note: Baki Edit/Delete methods par bhi isi tarah [Authorize] laga rahega.
    }
}