using FinalProject_PRMS_ASPNetEntityFrameworkMVC.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace FinalProject_PRMS_ASPNetEntityFrameworkMVC.Controllers
{
    public class TenantDashboardController : Controller
    {

        private PRMS_DBEntities db = new PRMS_DBEntities();

        // GET: TenantDashboard
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Settings()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult SearchProperties(int? bedrooms, int? bathrooms, string petsAllowed, string city, decimal? minRent, decimal? maxRent, string propertyType)
        {           
            using (var db = new PRMS_DBEntities())
            {
                var properties = db.Properties.AsQueryable(); 

                if (bedrooms.HasValue)
                    properties = properties.Where(p => p.Bedrooms == bedrooms);

                if (bathrooms.HasValue)
                    properties = properties.Where(p => p.Bathrooms == bathrooms);

                if (!string.IsNullOrEmpty(petsAllowed))
                    properties = properties.Where(p => p.PetsAllowed.ToString().ToLower() == petsAllowed.ToLower());

                if (!string.IsNullOrEmpty(city))
                    properties = properties.Where(p => p.City.Contains(city));

                if (minRent.HasValue)
                    properties = properties.Where(p => p.RentAmount >= minRent);

                if (maxRent.HasValue)
                    properties = properties.Where(p => p.RentAmount <= maxRent);

                if (!string.IsNullOrEmpty(propertyType))
                    properties = properties.Where(p => p.PropertyType == propertyType);
                
                var result = properties.ToList();
                
                return View(result);
            }
        }

        private List<Property> GetProperties()
        {
            return new List<Property>
            {
            new Property { PropertyId = 1, PropertyType = "Apartment", Description = "Cozy apartment", City = "Montreal", RentAmount = 1200, Bedrooms = 2, Bathrooms = 1, PetsAllowed = true, ImageBase64 = "" },
            new Property { PropertyId = 2, PropertyType = "Condo", Description = "Modern condo", City = "Montreal", RentAmount = 1500, Bedrooms = 3, Bathrooms = 2, PetsAllowed = false, ImageBase64 = "" },
            
            };
        }

        public ActionResult GetImage(int id)
        {
            using (var db = new PRMS_DBEntities())
            {
                var properties = db.Properties.ToList();
                
                foreach (var property in properties)
                {
                    if (property.Image != null)
                    {
                        property.ImageBase64 = Convert.ToBase64String(property.Image);
                        Debug.WriteLine($"Property {property.PropertyId} ImageBase64: {property.ImageBase64.Substring(0, 50)}"); 
                    }
                    else
                    {
                        Debug.WriteLine($"Property {property.PropertyId} has no image.");
                    }
                }

                return View(properties);
            }
        }

        public ActionResult Details(int id)
        {
            using (var db = new PRMS_DBEntities())
            {
                var property = db.Properties.FirstOrDefault(p => p.PropertyId == id);

                if (property == null)
                {
                    return HttpNotFound();
                }

                return View(property);
            }
        }
        public ActionResult SendMessage(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            ViewBag.PropertyId = id;
            return View();
        }
        public ActionResult RequestAppointment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            ViewBag.PropertyId = id;
            return View();
        }
        public ActionResult SendMessage(int id)
        {
            ViewBag.PropertyId = id;
            return View();
        }

        public ActionResult RequestAppointment(int id)
        {
            ViewBag.PropertyId = id;
            return View();
        }

        [HttpGet]
        public ActionResult MakeAppointment(int id)
        {
            var property = db.Properties.Find(id);
            if (property == null)
            {
                return HttpNotFound();
            }

            ViewBag.PropertyId = id;
            return View();
        }

        [HttpPost]
        public ActionResult MakeAppointment(int id, DateTime appointmentDate, string message)
        {            
            var tenantId = (int)Session["TenantId"];  
            var property = db.Properties.Find(id);

            if (property == null)
            {
                return HttpNotFound();
            }

            var managerId = property.ManagerId;
            var manager = db.Users.Find(managerId);

            if (manager == null)
            {
                ModelState.AddModelError("", "The manager for this property does not exist in the system.");
                return View(); 
            }

            
            var appointment = new Appointment
            {
                TenantId = tenantId,
                ManagerId = managerId, 
                PropertyId = id,
                AppointmentDate = appointmentDate,
                StatusId = 22, 
                Message = message
            };

            db.Appointments.Add(appointment);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Appointment scheduled successfully!";
            return RedirectToAction("Details", new { id = id });
        }


        [HttpGet]
        public ActionResult SendMessageM(int id)
        {
            var property = db.Properties.Find(id);
            if (property == null)
            {
                return HttpNotFound();
            }

            ViewBag.PropertyId = id;
            return View();
        }


        [HttpPost]
        public ActionResult SendMessageM(int id, string contentMessage)
        {           
            var tenantId = (int)Session["TenantId"];
            
            var property = db.Properties.Find(id);
            if (property == null)
            {
                return HttpNotFound();
            }
            
            var managerId = property.ManagerId;
           
            var manager = db.Users.Find(managerId);
            if (manager == null)
            {
                ModelState.AddModelError("", "The manager for this property does not exist.");
                return View();
            }
           
            var message = new Message
            {
                SenderId = tenantId,      
                ReceiverId = managerId,     
                Content_Message = contentMessage, 
                MessageDateTime = DateTime.Now,  
                StatusId = 2  
            };
                        
            db.Messages.Add(message);
            db.SaveChanges();
           
            TempData["SuccessMessage"] = "Message sent successfully!";

            return RedirectToAction("Details", new { id = id });
        }

    }

}

