using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EVA.Data;
using EVA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Dynamic;
using Newtonsoft.Json;
using EVA.Services;
using System.Diagnostics;

namespace EVA.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Attendee")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISMSSender _smsSender;
        static readonly object key = new object();
        //Allbookings is used as the in-memory copy of all bookings
        //This purpose is to eliminate database calls for everytime a user filters the bookings by the Event
        private List<BookingViewModel> AllBookings = new List<BookingViewModel>();
        public BookingsController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ISMSSender smsSender)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _smsSender = smsSender;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            await LoadBookingsAsync();
            return View(AllBookings);
        }

        private async Task LoadBookingsAsync()
        {
            var bookings = new List<Booking>();
            try
            {
                bool getAll = false;
                if (_httpContextAccessor.HttpContext.User.IsInRole("Admin") || _httpContextAccessor.HttpContext.User.IsInRole("SuperAdmin"))
                {
                    bookings = await _context.Booking.ToListAsync();
                    getAll = true;
                }
                else
                {
                    var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    bookings = await _context.Booking.Where(x => x.UserId == userId).ToListAsync();
                }
                bookings.ForEach((x) =>
                {
                    lock (key)
                    {
                        var bvm = new BookingViewModel
                        {
                            UserId = x.UserId,
                            BookingId = x.BookingId,
                            EventId = x.EventId,
                            Status = x.BookingStatus
                        };
                        //run this code only if its an admin user needing all the user details. do not run for one user viewing his own things.
                        //The concern here is that too many records in data holding specifically the 'UserProfilePic' byte[] variable might cause unnnecessary memory expenses
                        if (getAll)
                        {
                            var userObj = _context.Users.Find(x.UserId);
                            bvm.UserName = $"{userObj?.FirstName} {userObj?.LastName}";
                            bvm.PhoneNumber = $"{userObj?.PhoneNumber}";
                            bvm.Email = $"{userObj?.Email}";
                            bvm.UserProfilePic = userObj?.ProfilePicture;
                            bvm.UserProfilePic_ = Convert.ToBase64String(userObj?.ProfilePicture);
                        }
                        bvm.Event = _context.Event.Find(x.EventId);
                        
                        AllBookings.Add(bvm);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Filter the bookings by event Id
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns>Json List of bookings per event, to overwrite the UI</returns>
        //public async Task<List<BookingViewModel>> GetBookingsByEvent(int eventId)
        [HttpGet]
        public async Task<JsonResult> GetBookingsByEvent(int id)
        {
            var selectedBookings = new List<BookingViewModel>();
            //'new' up this object to avoid null-object-exceptions at the serialization phase
            var bookings = await _context.Booking.Where(x => x.EventId == id).ToListAsync();
            bookings.ForEach((x) =>
            {
                lock (key)
                {
                    var bvm = new BookingViewModel
                    {
                        UserId = x.UserId,
                        BookingId = x.BookingId,
                        EventId = x.EventId,
                        Status = x.BookingStatus
                    };
                        var userObj = _context.Users.Find(x.UserId);
                        bvm.UserName = $"{userObj?.FirstName} {userObj?.LastName}";
                        bvm.PhoneNumber = $"{userObj?.PhoneNumber}";
                        bvm.Email = $"{userObj?.Email}";
                        bvm.UserProfilePic = userObj?.ProfilePicture;
                        bvm.UserProfilePic_ = Convert.ToBase64String(userObj?.ProfilePicture);
                        bvm.Event = _context.Event.Find(x.EventId);
                    selectedBookings.Add(bvm);
                }
            });
            return new JsonResult(JsonConvert.SerializeObject(selectedBookings));
        }
        /// <summary>
        /// Retrieves a list of events, but only those for which bookings have been made, to avoid clutter and unneccesary links.
        /// </summary>
        /// <returns>All the events for which bookings have been made</returns>
        [HttpGet]
        public JsonResult GetEvents()
        {
            try
            {
               var eventIds = _context.Booking.Select(x => x.EventId).Distinct().ToList();
               var events = from evnt in _context.Event.ToList()
                            where eventIds.Contains(evnt.EventId)
                            select evnt;
                return new JsonResult(JsonConvert.SerializeObject(events));
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return new JsonResult(e.Message);
            }
            //return new JsonResult(JsonConvert.SerializeObject(events));
        }
        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Booking
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
        /*
        // GET: Bookings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,EventId")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                booking.UserId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                //when a booking is first made, set its status to Pending to be approved by an administrator
                booking.BookingStatus = Status.Pending;
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(booking);
        }
        */

            /// <summary>
            /// Create a booking from the Attendee side
            /// </summary>
            /// <param name="id">event id for which user wants to book</param>
            /// <returns>JsonResult indicating success or failure with appropriate message</returns>
        [HttpPost]
        public async Task<JsonResult> Create(int id)
        {
            try
            {
                var booking = new Booking();
                booking.UserId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                //check to ensure a user has not already made a booking for the same event
                if(await UserHasBookingForEventAsync(booking.UserId, id) != true)
                {
                    if(await EventIsFullAsync(id) != true)
                    { 
                        booking.BookingStatus = Status.Pending;
                        booking.EventId = id;
            
                        _context.Add(booking);
                        await _context.SaveChangesAsync();

                        dynamic jObj = new ExpandoObject();
                        jObj.message = $"Your booking was made and is now PENDING. \nYou will be notified via email or sms once the booking has been confirmed or denied. Thank you. \nBooking number #{booking.BookingId}";
                        int count = await _context.Booking.Where(x => x.EventId == id).CountAsync();
                        jObj.bookingcount = count;
                        var full = await EventIsFullAsync(id);
                        jObj.fullybooked = full ? "YES" : "NO";
                        var json = JsonConvert.SerializeObject(jObj);
                        return new JsonResult(json);
                    }
                    else
                    {
                        dynamic jObj = new ExpandoObject();
                        jObj.message = $"Sorry. Seats for this event are all full.";
                        var json = JsonConvert.SerializeObject(jObj);
                        return new JsonResult(json);
                    }
                }
                else
                {

                    dynamic jObj = new ExpandoObject();
                    jObj.message = $"You have already made a booking for this event. Please check your bookings tab.";
                    var json = JsonConvert.SerializeObject(jObj);
                    return new JsonResult(json);
                }
            }
            catch(Exception e)
            {
                dynamic jObj = new ExpandoObject();
                jObj.message = $"An error occured while making the booking";
                var json = JsonConvert.SerializeObject(jObj);
                return new JsonResult(json);
            }
        }

        private async Task<bool> EventIsFullAsync(int id)
        {
            var ev = await _context.Event.FindAsync(id);
            var evm = MapModelToViewModel(ev);
            evm.BookingsMade = GetBookingsMadeForEvent(id);
            return evm.IsFull;
        }

        /// <summary>
        /// Author: Tshepo Blom, 22 Mar 2021
        /// Check for double booking on the same event by the same
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="eventId"></param>
        /// <returns>true if booking already exists, false if user does not have any bookings for that event</returns>
        public async Task<bool> UserHasBookingForEventAsync(string userId,int eventId)
        {
            return await _context.Booking.Where(x => x.EventId == eventId && x.UserId == userId).AnyAsync();
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Booking.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }

        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,UserId,EventId")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Booking
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Booking.FindAsync(id);
            _context.Booking.Remove(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Booking.Any(e => e.BookingId == id);
        }

        //Approve Booking, only available to administrators
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> ApproveBooking(int id)
        {
            dynamic result = new ExpandoObject();
            try
            {
                var booking = await _context.Booking.FindAsync(id);
                var user = await _context.Users.FindAsync(booking?.UserId);
                var evnt = await _context.Event.FindAsync(booking?.EventId);
                if (booking?.BookingStatus == Status.Approved)
                {
                    result.outcome = "This booking has already been approved!";
                    return new JsonResult(JsonConvert.SerializeObject(result));
                }
                else 
                { 
                    booking.BookingStatus = Status.Approved;
                    await _context.SaveChangesAsync();
                    var smsSent = await NotifyUserAsync(
                        $"Hi {user?.FirstName} {user?.LastName}, your booking #{id} has been CONFIRMED for the event on {evnt?.EventStartDateTime.Date.ToShortDateString()}. Please present this SMS as proof on arrival at the door.", 
                        $"{user?.PhoneNumber}");

                    return new JsonResult(JsonConvert.SerializeObject(result));
                }
            }
            catch (Exception)
            {
                result.outcome = "Pending";
                return new JsonResult(JsonConvert.SerializeObject(result));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> DeclineBooking(int id)
        {
            dynamic result = new ExpandoObject();
            try
            {
                var booking = await _context.Booking.FindAsync(id);
                var user = await _context.Users.FindAsync(booking?.UserId);
                var evnt = await _context.Event.FindAsync(booking?.EventId);

                if (booking?.BookingStatus == Status.Declined)
                {
                    result.outcome = "This booking has already been declined!";
                    return new JsonResult(JsonConvert.SerializeObject(result));
                }
                else
                {
                    booking.BookingStatus = Status.Declined;
                    await _context.SaveChangesAsync();
               

                    var smsSent = await NotifyUserAsync(
                        $"Hi {user?.FirstName} {user?.LastName}, your booking #{id} has been DECLINED for the event on {evnt?.EventStartDateTime.Date.ToShortDateString()}. Please try booking for another day.", 
                        $"{user?.PhoneNumber}");

                    result.outcome = "Declined";
                    return new JsonResult(JsonConvert.SerializeObject(result));
                }
            }
            catch (Exception)
            {
                result.outcome = "Pending";
                return new JsonResult(JsonConvert.SerializeObject(result));
            }
        }

        /// <summary>
        /// Check whether a user can book for the week they want to. 
        /// Users must skip one week, they cannot make bookings for two consecutive weeks
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="bookingId"></param>
        /// <returns></returns>
        public async Task<bool> CanBookForThisWeek(string userId)
        {
            //WILL RETURN TO FINISH THIS METHOD LAST
            var userObj = await _context.Users.FindAsync(userId);
            var userBookings = await _context.Booking.Where(x => x.UserId == userId).OrderBy(x => x.EventId).ToListAsync();
            var bookingEventId = await _context.Booking.Select(x => x.EventId).ToListAsync();
            var bookedEvents = await _context.Event.Where(ev => bookingEventId.Contains(ev.EventId)).ToListAsync();
            return true;
        }
        private EventViewModel MapModelToViewModel(Event _event)
        {
            return new EventViewModel
            {
                Title = _event.Title,
                EventId = _event.EventId,
                EventStartDateTime = _event.EventStartDateTime,
                EventEndDateTime = _event.EventEndDateTime,
                SeatLimit = _event.SeatLimit,
                Description = _event.Description,
                Location = _event.Location,
                Repeats = _event.Repeats
            };
        }
        private int GetBookingsMadeForEvent(int id)
        {
            return _context.Booking.Where(x => x.EventId == id).ToList().Count;
        }

        /// <summary>
        /// Send SMS to user who has made the booking
        /// </summary>
        /// <param name="bookingId">Id of the current booking to be approved or declined</param>
        /// <returns></returns>
        private async Task<bool> NotifyUserAsync(string message, string phone)
        {
            return await _smsSender.SendSmsAsync(message, phone);                         
        }
    }
}
