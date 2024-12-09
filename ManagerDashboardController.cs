using FinalProject_PRMS_ASPNetEntityFrameworkMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Services.Description;

namespace FinalProject_PRMS_ASPNetEntityFrameworkMVC.Controllers
{
    public class ManagerDashboardController : Controller
    {

        private readonly PRMS_DBEntities _context;
        public ManagerDashboardController()
        {
            _context = new PRMS_DBEntities(); 
        }

        public ManagerDashboardController(PRMS_DBEntities context)
        {
            _context = context;
        }


        // GET: ManagerDashboard
        public async Task<ActionResult> Index()
        {            
            int? managerId = Session["ManagerId"] as int?;

            if (managerId == null)
            {
                // Exibe uma mensagem se não houver ManagerId na sessão
                TempData["ErrorMessage"] = "Você precisa estar logado como Manager para acessar o painel.";
                // Se o ManagerId não estiver na sessão, redireciona para a página de login
                return RedirectToAction("Login", "Home");
            }

            var properties = await _context.Properties
                .Where(p => p.ManagerId == managerId)
                .ToListAsync();

            var totalHouses = await _context.Properties
                .Where(p => p.ManagerId == managerId && p.PropertyType == "House")
                .CountAsync();

            var totalApartments = await _context.Properties
                .Where(p => p.ManagerId == managerId && p.PropertyType == "Apartment")
                .CountAsync();

            ViewBag.TotalHouses = totalHouses;
            ViewBag.TotalApartments = totalApartments;
            ViewBag.TotalProperties = totalHouses + totalApartments;

            
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

        // GET: ManagerDashboard/Houses
        public ActionResult HousesList()
        {
            var managerId = Session["ManagerId"] as int?;
            
            if (!managerId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }
            
            var buildings = _context.Properties
             .Where(b => b.ManagerId == managerId.Value && (b.PropertyType == "House" || b.PropertyType == "Townhouse"))
             .ToList();

            return View(buildings);
        }


        // GET: ManagerDashboard/Apartments/Condos/Buildings
        public ActionResult ApartmentsList()
        {
            var managerId = Session["ManagerId"] as int?;

            if (!managerId.HasValue)
            {                
                return RedirectToAction("Login", "Account");
            }
                        
            var buildings = _context.Properties
             .Where(b => b.ManagerId == managerId.Value && (b.PropertyType == "Apartment" || b.PropertyType == "Condo" || b.PropertyType == "Buildings"))
             .ToList();

            return View(buildings);
        }


        // GET: ManagerDashboard/EditApartment
        public ActionResult EditApartment(int id)
        {            
            var allowedTypes = new[] { "Apartment", "Condo", "Building" };
            var apartment = _context.Properties
                .FirstOrDefault(p => p.PropertyId == id && allowedTypes.Contains(p.PropertyType));
           
            if (apartment == null)
            {
                return HttpNotFound();
            }

            return View(apartment);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditApartment(Property property)
        {
            if (ModelState.IsValid)
            {               
                var existingProperty = _context.Properties.Find(property.PropertyId);

                if (existingProperty != null &&
                    (existingProperty.PropertyType == "Apartment" ||
                     existingProperty.PropertyType == "Condo" ||
                     existingProperty.PropertyType == "Building"))

                {
                    existingProperty.Address = property.Address;
                    existingProperty.City = property.City;
                    existingProperty.IsActive = property.IsActive;
                    existingProperty.StatusId = property.StatusId;

                    _context.SaveChanges();

                    return RedirectToAction("ApartmentsList");
                }
            }

            return View(property);
        }

        // GET: ManagerDashboard/EditHouse
        public ActionResult EditHouse(int id)
        {            
            var allowedHouseTypes = new[] { "House", "Townhouse" };
            
            var property = _context.Properties
                .Include("House") 
                .FirstOrDefault(p => p.PropertyId == id && allowedHouseTypes.Contains(p.PropertyType));

            if (property != null && property.House != null)
            {                
                return View(property);
            }

            return HttpNotFound();
        }

        // POST: ManagerDashboard/EditHouse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditHouse(int id, Property property)
        {
            if (ModelState.IsValid)
            {               
                var existingProperty = _context.Properties
                    .Include(p => p.House)
                    .FirstOrDefault(p => p.PropertyId == id);

                if (existingProperty != null && existingProperty.House != null)
                {                    
                    existingProperty.Address = property.Address;
                    existingProperty.City = property.City;
                    existingProperty.IsActive = property.IsActive;
                    existingProperty.House.LotSize = property.House.LotSize;
                   
                    _context.SaveChanges();

                    return RedirectToAction("Index"); // Redireciona para a lista de propriedades
                }
            }

            return View(property); // Retorna para a view caso haja um erro
        }


        // GET: ManagerDashboard/DeleteHouse
        public ActionResult DeleteHouse(int id)
        {            
            var property = _context.Properties
                .Include("House") // Carrega a relação com House
                .FirstOrDefault(p => p.PropertyId == id);

            if (property != null)
            {
                return View(property); 
            }
           
            _context.Houses.Remove(property.House);

            _context.Properties.Remove(property);

            _context.SaveChanges();

            return RedirectToAction("Index"); 
        }

        // POST: ManagerDashboard/DeleteHouse
        [HttpPost, ActionName("DeleteHouse")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var property = _context.Properties
                .FirstOrDefault(p => p.PropertyId == id);

            if (property != null)
            {                
                if (property.House != null)
                {                    
                    _context.Houses.Remove(property.House);
                }
                _context.Properties.Remove(property);
               
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return HttpNotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmedHouses(int id)
        {            
            var property = _context.Properties.Include(p => p.House).FirstOrDefault(p => p.PropertyId == id);

            if (property == null)
            {
                return HttpNotFound();
            }
           
            if (property.House != null)
            {
                _context.Houses.Remove(property.House);
            }
           
            _context.Properties.Remove(property);
            
            _context.SaveChanges();
           
            return RedirectToAction("Index");
        }


        public ActionResult CreateHouse()
        {            
            var model = new Property
            {
                PropertyType = "House", 
                StatusId = 6, 
                OwnerId = 22,  
                ManagerId = 23, 
                IsActive = true, 
                Image = null,
                PetsAllowed = true,
                Garage = true
            };

            return View(model);
        }

        //CREATE APARTMENT
        public ActionResult CreateApartment()
        {            
            var model = new Property
            {
                PropertyType = "Apartment", 
                StatusId = 6, 
                OwnerId = 22,  
                ManagerId = 23, 
                IsActive = true, 
                Image = null,
                PetsAllowed = true,  
                Garage = true,      
               
            };

            return View(model);
        }

        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateApartment(Property model)
        {
            if (ModelState.IsValid)
            {
                model.ManagerId = (int)Session["ManagerId"];
                model.OwnerId = (int)Session["OwnerId"];
                
                _context.Properties.Add(model);
                _context.SaveChanges();
                
                return RedirectToAction("ApartmentsList", "ManagerDashboard");
            }
            
            return View(model);
        }


        // STATUS PROPERTIES

        public async Task<ActionResult> StatusProperties()
        {
            int? managerId = Session["ManagerId"] as int?;

            if (managerId == null)
            {
                TempData["ErrorMessage"] = "Você precisa estar logado como Manager para acessar as propriedades.";
                return RedirectToAction("Login", "Home");
            }

            var properties = await _context.Properties
                .Where(p => p.ManagerId == managerId)
                .ToListAsync();

            return View(properties);
        }

        public ActionResult Schedules()
        {            
            var managerId = Session["ManagerId"];
            if (managerId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "ManagerId not found in session.");
            }

            int managerIdInt = (int)managerId;
            
            if (_context == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Database context is not initialized.");
            }
           
            var schedules = _context.Appointments
                .Include("Property")
                .Include("User") 
                .Where(a => a.ManagerId == managerIdInt)
                .ToList();

            return View(schedules);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateSchedule(int appointmentId, DateTime? newDate, string action)
        {            
            var appointment = _context.Appointments.Find(appointmentId);

            if (appointment == null)
            {
                return HttpNotFound($"Appointment with ID {appointmentId} not found.");
            }
            
            if (action == "Accept")
            {
                appointment.StatusId = 11;
                appointment.Status = _context.Status.Find(11);
                appointment.Message = "The appointment is scheduled and confirmed."; 
                appointment.AppointmentDate = newDate ?? appointment.AppointmentDate; 
            }
            else if (action == "Deny")
            {
                appointment.StatusId = 13; 
                appointment.Status = _context.Status.Find(13); 
                appointment.Message = "The appointment has been cancelled."; 
            }
            
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Schedule updated successfully!";
            return RedirectToAction("Schedules");
        }


        // MESSAGES
        public async Task<ActionResult> Messages()
        {           
            int? userId = Session["UserId"] as int?;
            if (userId == null)
            {
                var userIdCookie = Request.Cookies["UserId"];
                if (userIdCookie != null && int.TryParse(userIdCookie.Value, out int cookieUserId))
                {
                    userId = cookieUserId;
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "UserId not found.");
                }
            }
            
            if (_context == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Database context is not initialized.");
            }
            
            var messages = await _context.Messages
                
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.MessageDateTime)
                .ToListAsync();
            
            return View(messages);
        }

    }

}