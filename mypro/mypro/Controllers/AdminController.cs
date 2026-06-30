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
        // --- DELETE ACTION (Direct GET Method) ---
        public IActionResult DeleteUser(int id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                try
                {
                    db.Users.Remove(user);
                    db.SaveChanges();
                    TempData["AlertMessage"] = "User deleted successfully!";
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                {
                    // Foreign Key constraint error message
                    TempData["AlertMessage"] = "Cannot delete this user because they are linked to other data in the system!";
                }
            }
            return RedirectToAction("Userlist");
        }

        // --- EDIT ACTIONS ---
        // 1. GET: EditUser (Form show krne k liye)
        public IActionResult EditUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            // Roles list dropdown k liye
            ViewBag.RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // 2. POST: EditUser (Data update krne k liye)
        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName", user.RoleId);
                return View(user);
            }

            // Database se existing record nikalein
            var existingUser = db.Users.Find(user.UserId);
            if (existingUser != null)
            {
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.RoleId = user.RoleId;

                // Agar user ne new password type kiya hai to use hash krein, warna purana hi rehne dein
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }

                db.SaveChanges();
            }

            return RedirectToAction("Userlist");
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
        public IActionResult teacherregister(User user)
        {
            // 1. Pehle check krein k email already database me hai ya nahi
            bool emailExists = db.Users.Any(u => u.Email == user.Email); // Agr aap k model me field ka naam 'Email' hai

            if (emailExists)
            {
                // ViewBag me message set krein
                ViewBag.ErrorMessage = "This email is already registered!";

                // Ya phir validation summary me show krne k liye custom error add krein
                ModelState.AddModelError("Email", "Email already exists.");
            }

            // 2. ModelState check krein (agr email exist krta hai to ModelState invalid ho chuka hoga agr AddModelError use kiya hai)
            if (!ModelState.IsValid || emailExists)
            {
                ViewBag.RoleList = new SelectList(db.Roles.ToList(), "RoleId", "RoleName");
                return View(user);
            }

            // 3. Agar sab sahi hai to password hash krein aur save krein
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            db.Users.Add(user);
            db.SaveChanges();

            return RedirectToAction("Userlist");
        }

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
        public IActionResult AddSection()

        {

            // Database se sare courses nikal kar dropdown ke liye bhejein

            ViewBag.CourseList = new SelectList(db.Courses.ToList(), "CourseId", "Title");

            return View();

        }



        // POST: Section/Create

        [HttpPost]



        public IActionResult AddSection(Section section)

        {

            if (ModelState.IsValid)

            {

                db.Sections.Add(section);

                db.SaveChanges();

                return RedirectToAction("Sectionlist");

            }



            // Agar error aaye to dropdown dobara bharein

            ViewBag.CourseList = new SelectList(db.Courses.ToList(), "CourseId", "Title");

            return View(section);

        }

        public IActionResult Sectionlist()

        {

            // Course aur Lessons dono ko Include kiya taake unka data mil sake

            var sections = db.Sections

                             .Include(s => s.Course)

                             .Include(s => s.Lessons)

                             .OrderBy(s => s.CourseId) // Course wise group karne ke liye

                             .ThenBy(s => s.SortOrder) // Order wise dikhane ke liye

                             .ToList();



            return View(sections);

        }

        public IActionResult Deletesection(int id)

        {

            var sec = db.Sections.Find(id);

            if (sec != null)

            {

                db.Sections.Remove(sec);

                db.SaveChanges();

            }

            // Delete karne ke baad wapas list par le jayega

            return RedirectToAction("sectionlist");

        }

        // 1. GET: Section/Edit/5

        public IActionResult EditSection(int id)

        {

            var section = db.Sections.Find(id);

            if (section == null) return NotFound();



            // Dropdown bharna taake user agar Course change karna chahe

            ViewBag.CourseList = new SelectList(db.Courses.ToList(), "CourseId", "Title", section.CourseId);



            return View(section);

        }



        // 2. POST: Section/Edit/5

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult EditSection(Section section)

        {

            if (ModelState.IsValid)

            {

                try

                {

                    db.Sections.Update(section);

                    db.SaveChanges();

                    return RedirectToAction("sectionlist");

                }

                catch (Exception ex)

                {

                    ModelState.AddModelError("", "Unable to save changes. Error: " + ex.Message);

                }

            }



            // Agar validation fail ho jaye toh dropdown dobara bharein

            ViewBag.CourseList = new SelectList(db.Courses.ToList(), "CourseId", "Title", section.CourseId);

            return View(section);

        }

        public IActionResult lessonlist()

        {

            var lessons = db.Lessons.Include(l => l.Section).ThenInclude(s => s.Course).ToList();

            return View(lessons);

        }



        // 2. INSERT (Get)

        public IActionResult Addlesson()

        {

            ViewBag.SectionList = new SelectList(db.Sections, "SectionId", "Title");

            return View();

        }



        // 2. INSERT (Post)

        [HttpPost]

        public IActionResult Addlesson(Lesson lesson)

        {

            if (ModelState.IsValid)

            {

                db.Lessons.Add(lesson);

                db.SaveChanges();

                return RedirectToAction("lessonlist");

            }

            ViewBag.SectionList = new SelectList(db.Sections, "SectionId", "Title");

            return View(lesson);

        }



        // 3. UPDATE (Get)

        public IActionResult Editlesson(int id)

        {

            var lesson = db.Lessons.Find(id);

            if (lesson == null) return NotFound();

            ViewBag.SectionList = new SelectList(db.Sections, "SectionId", "Title", lesson.SectionId);

            return View(lesson);

        }



        // 3. UPDATE (Post)

        [HttpPost]

        public IActionResult Editlesson(Lesson lesson)

        {

            if (ModelState.IsValid)

            {

                db.Lessons.Update(lesson);

                db.SaveChanges();

                return RedirectToAction("lessonlist");

            }

            ViewBag.SectionList = new SelectList(db.Sections, "SectionId", "Title", lesson.SectionId);

            return View(lesson);

        }



        // 4. DELETE

        public IActionResult Deletelesson(int id)

        {

            var lesson = db.Lessons.Find(id);

            if (lesson != null)

            {

                db.Lessons.Remove(lesson);

                db.SaveChanges();

            }

            return RedirectToAction("lessonlist");

        }

        public IActionResult quizlist()

        {

            // Course aur Questions dono include kiye taake list mein info dikha saken

            var quizzes = db.Quizzes

                            .Include(q => q.Course)

                            .Include(q => q.Questions)

                            .OrderByDescending(q => q.QuizId)

                            .ToList();



            return View(quizzes);

        }

        public IActionResult QuizDetails(int id)

        {

            var quiz = db.Quizzes

                         .Include(q => q.Course)

                         .Include(q => q.Questions)

                            .ThenInclude(ques => ques.Options)

                         .FirstOrDefault(q => q.QuizId == id);



            if (quiz == null) return NotFound();



            return View(quiz);

        }

        // Nayi Quiz banane ka GET method

        public IActionResult addquiz()

        {

            ViewBag.CourseList = new SelectList(db.Courses, "CourseId", "Title");

            return View();

        }



        [HttpPost]

        public IActionResult addquiz(Quiz quiz)

        {

            if (ModelState.IsValid)

            {

                db.Quizzes.Add(quiz);

                db.SaveChanges();

                return RedirectToAction("AddQuestion", new { quizId = quiz.QuizId });

            }

            return View(quiz);

        }



        // Question aur Options aik sath add karne ka method

        public IActionResult AddQuestion(int quizId)

        {

            ViewBag.QuizId = quizId;

            return View();

        }



        [HttpPost]

        public IActionResult AddQuestion(Question question, List<string> OptionTexts, int CorrectOptionIndex)

        {

            // 1. Sawal Save karein

            db.Questions.Add(question);

            db.SaveChanges();



            // 2. Us sawal ke 4 options loop se save karein

            for (int i = 0; i < OptionTexts.Count; i++)

            {

                var opt = new Option

                {

                    QuestionId = question.QuestionId,

                    OptionText = OptionTexts[i],

                    IsCorrect = (i == CorrectOptionIndex)

                };

                db.Options.Add(opt);

            }

            db.SaveChanges();



            return RedirectToAction("AddQuestion", new { quizId = question.QuizId });

        }

    }





}

        // Note: Baki Edit/Delete methods par bhi isi tarah [Authorize] laga rahega.
    
