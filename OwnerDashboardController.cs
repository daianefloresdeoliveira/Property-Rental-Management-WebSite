using FinalProject_PRMS_ASPNetEntityFrameworkMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace FinalProject_PRMS_ASPNetEntityFrameworkMVC.Controllers
{
    public class OwnerDashboardController : Controller
    {
        private readonly PRMS_DBEntities _context;
        public OwnerDashboardController()
        {
            _context = new PRMS_DBEntities(); 
        }

        public OwnerDashboardController(PRMS_DBEntities context)
        {
            _context = context;
        }
        
        public async Task<ActionResult> Index()
        {            
            int? ownerId = Session["OwnerId"] as int?;

            if (ownerId == null)
            {
                // Exibe uma mensagem se não houver OwnerId na sessão
                TempData["ErrorMessage"] = "Você precisa estar logado para acessar o painel.";
                // Se o OwnerId não estiver na sessão, redireciona para a página de login
                return RedirectToAction("Login", "Home");
            }
           
            var properties = await _context.Properties
                .Where(p => p.OwnerId == ownerId)
                .ToListAsync();

            ViewBag.TotalProperties = properties.Count;
            ViewBag.TotalManagers = await _context.Users
                .CountAsync(u => u.Role.RoleName == "Manager");
            ViewBag.TotalTenants = await _context.Users
                .CountAsync(u => u.Role.RoleName == "Tenants");

            return View(properties);
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        public async Task<ActionResult> Properties()
        {
            int? ownerId = Session["OwnerId"] as int?;

            if (ownerId == null)
            {
                return RedirectToAction("Login", "Home");
            }
            
            int totalProperties = await _context.Properties
                .Where(p => p.OwnerId == ownerId)
                .CountAsync();
          
            int totalManagers = await _context.Users
                .Where(u => u.Role.RoleName == "Manager")
                .CountAsync();
            
            int totalTenants = await _context.Users
                .Where(u => u.Role.RoleName == "Tenants")
                .CountAsync();
            
            ViewBag.TotalProperties = totalProperties;
            ViewBag.TotalManagers = totalManagers;
            ViewBag.TotalTenants = totalTenants;

            var properties = await _context.Properties
                .Where(p => p.OwnerId == ownerId)
                .ToListAsync();

            return View(properties); 
        }
       
        public ActionResult CreateProperty()
        {
            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateProperty([Bind(Include = "PropertyType,Address,City,State,ZipCode,StatusId,Description,RentAmount,PetsAllowed,Bedrooms,Bathrooms,Garage,IsActive")] Property property)
        {            
            var ownerId = Session["OwnerId"] as int?;
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Home");
            }
                       
            property.OwnerId = ownerId.Value;
            property.ManagerId = 2; 

            if (ModelState.IsValid)
            {
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
                return RedirectToAction("Properties", "OwnerDashboard");
            }

            return View(property);
        }
        
        public async Task<ActionResult> EditProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return HttpNotFound();
            }
            return View(property);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProperty(Property property)
        {
            if (ModelState.IsValid)
            {
                _context.Entry(property).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return RedirectToAction("Properties");
            }
            return View(property);
        }

        //[HttpPost]
        // DELETE PROPERTY
        public async Task<ActionResult> DeleteProperty(int id)
        {           
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null)
            {
                return HttpNotFound();
            }
            
            if (property.ManagerId != null)
            {                
                var manager = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == property.ManagerId);

                if (manager != null)
                {                    
                    ViewBag.Message = $"This property is associated with the manager {manager.FirstName} {manager.LastName}. Please change the manager before deleting the property..";
                }
            }

            return View(property);
        }


        public async Task<ActionResult> IndexManagers()
        {            
            int? ownerId = Session["OwnerId"] as int?;
            Debug.WriteLine("OwnerId: " + ownerId);

            if (ownerId == null)
            {
                Debug.WriteLine("OwnerId não encontrado na sessão.");
                return RedirectToAction("Login", "Home");
            }
            
            var managerIds = await _context.Properties
                .Where(p => p.OwnerId == ownerId) 
                .Select(p => p.ManagerId)  
                .ToListAsync();
           
            var managers = await _context.Users
                .Where(u => managerIds.Contains(u.UserId) && u.RoleId == 2)  
                .ToListAsync();

            return View("CRUD_Managers/Index", managers);  

        }

        // GET: Create Manager
        public ActionResult CreateManager()
        {
            return View();
        }

        // POST: Create Manager
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateManager(User user)
        {
            if (ModelState.IsValid)
            {                
                user.RoleId = 2;
               
                int? userId = Session["UserId"] as int?;
                if (userId == null)
                {
                    return RedirectToAction("Login", "Home");
                }
                user.UserId = userId.Value;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("IndexManagers");
            }
            return View(user);
        }

        // GET: Edit Manager
        public async Task<ActionResult> EditManager(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return HttpNotFound(); 
            }
            return View(user); 
        }


        // POST: Edit Manager
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditManager(int id, [Bind(Include = "UserId,Email,PhoneNumber,FirstName,LastName,IsActive")] User user)
        {
            if (ModelState.IsValid)
            {               
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    return HttpNotFound();
                }
               
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.IsActive = user.IsActive;
                
                _context.Entry(existingUser).State = EntityState.Modified;
                
                await _context.SaveChangesAsync();
                
                return RedirectToAction("IndexManagers");
            }
           
            return View(user);
        }
       
        public async Task<ActionResult> DeleteManager(int id)
        {
            var user = await _context.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return HttpNotFound();
            }
            
            if (user.Role != null && user.Role.RoleName == "Manager")
            {                
                var dependencies = _context.Properties
                    .Where(e => e.ManagerId == user.UserId)
                    .ToList();

                if (dependencies.Any())
                {                    
                    ViewBag.Message = "This user is a manager in other properties. Please change the manager before deleting them.";
                }
            }

            return View(user);
        }
        
        [HttpPost, ActionName("DeleteManager")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {            
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return HttpNotFound();
            }
            
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("IndexManagers");
        }

        //GET Detaisl MANAGER

        public async Task<ActionResult> DetailsManager(int id)                //// REVISAR
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return HttpNotFound(); 
            }
            return View(user);  
        }
                                           ////REVISAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DetailsManager(int id, [Bind(Include = "UserId,Email,PhoneNumber,FirstName,LastName,IsActive, Password")] User user)
        {
            if (ModelState.IsValid)
            {               
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    return HttpNotFound();
                }
                
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.IsActive = user.IsActive;
               
                _context.Entry(existingUser).State = EntityState.Modified;
                
                await _context.SaveChangesAsync();

                return RedirectToAction("IndexManagers");
            }

            return View(user);
        }


    }
}
