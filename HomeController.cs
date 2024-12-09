using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FinalProject_PRMS_ASPNetEntityFrameworkMVC.Models;

namespace FinalProject_PRMS_ASPNetEntityFrameworkMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly PRMS_DBEntities _context;
        
        public HomeController()
        {
            _context = new PRMS_DBEntities(); 
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        // Ação POST para validar o login
        [HttpPost]

        public async Task<ActionResult> ValidateLogin(string email, string password)
        {           
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewData["ErrorMessage"] = "Email or password cannot be empty!";
                return View("Login");
            }
           
            var user = await _context.Users
                .Include(u => u.Role)  
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password && u.IsActive);

            if (user == null)
            {
                ViewData["ErrorMessage"] = "Invalid email or password!";
                return View("Login");
            }

            Session["UserName"] = user.FirstName;

            if (user.Role != null)
            {
                if (user.Role.RoleName == "Owner")
                {
                    Session["OwnerId"] = user.UserId; 

                    
                    HttpCookie userCookie = new HttpCookie("UserId", user.UserId.ToString());
                    userCookie.Expires = DateTime.Now.AddHours(1); 
                    Response.Cookies.Add(userCookie);

                    return RedirectToAction("Index", "OwnerDashboard");
                }
                else if (user.Role.RoleName == "Manager")
                {
                    Session["ManagerId"] = user.UserId; 

                    HttpCookie userCookie = new HttpCookie("UserId", user.UserId.ToString());
                    userCookie.Expires = DateTime.Now.AddHours(1); 
                    Response.Cookies.Add(userCookie);

                    return RedirectToAction("Index", "ManagerDashboard");
                }
                else if (user.Role.RoleName == "Tenants")
                {
                    Session["TenantId"] = user.UserId; 
                    
                    HttpCookie userCookie = new HttpCookie("UserId", user.UserId.ToString());
                    userCookie.Expires = DateTime.Now.AddHours(1); 
                    Response.Cookies.Add(userCookie);

                    return RedirectToAction("Index", "TenantDashboard");
                }
            }

            ViewData["ErrorMessage"] = "Invalid role or your account is not active!";
            return View("Login");
        }
    }
}