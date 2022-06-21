using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MusicSystem.Areas.Identity.Data;
using MusicSystem.Data;
using MusicSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly MusicSystemContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<MusicSystemUser> _userManager;

        public AdminController(MusicSystemContext Context, RoleManager<IdentityRole> RoleManager, UserManager<MusicSystemUser> UserManager)
        {
            this._context = Context;
            this._roleManager = RoleManager;
            this._userManager = UserManager;
        }

        // Role Management

        [Authorize(Policy = "AdminAccess")]
        public IActionResult RoleIndex()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        [Authorize(Policy = "AdminAccess")]
        public IActionResult RoleCreate()
        {
            return View(new IdentityRole());
        }

        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> CreateRole(IdentityRole role)
        {
            await _roleManager.CreateAsync(role);
            return RedirectToAction("RoleIndex");
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteRole(string name)
        {
            var Cuser = await _userManager.GetUserAsync(User);
            if (name == Cuser.UserName)
                return RedirectToAction("RoleIndex");
            var role = await _roleManager.FindByNameAsync(name);
            await _roleManager.DeleteAsync(role);
            return RedirectToAction("RoleIndex");
        }

        // User Management

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UserIndex(string SearchString, string Email)
        {
            var users = from s in _context.MusicSystemUsers select s;

            if (!String.IsNullOrEmpty(SearchString))
                users = users.Where(s => s.Email.Contains(SearchString));

            IQueryable<String> TypeQuery = from s in _context.MusicSystemUsers orderby s.Email select s.Email;

            IEnumerable<SelectListItem> items = new SelectList(await TypeQuery.Distinct().ToListAsync());
            ViewBag.Email = items;
            if (!String.IsNullOrEmpty(Email))
                users = users.Where(s => s.Email.Equals(Email));

            return View(await users.ToListAsync());
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UserDetail(string? id)
        {
            if (id == null)
                return NotFound();
            var user = await _context.MusicSystemUsers.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UserEdit(string? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.MusicSystemUsers.FindAsync(id);
            if (user == null)
                return NotFound();
            string[] list = {"Member", "Admin"};
            IEnumerable<SelectListItem> roles = new SelectList(list);
            ViewBag.Roles = roles;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UserEdit(string id,[Bind("Email,UserRole")] MusicSystemUser user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.MusicSystemUsers.Where(a => a.Id == id).First().UserName = user.Email;
                    _context.MusicSystemUsers.Where(a => a.Id == id).First().Email = user.Email;
                    _context.MusicSystemUsers.Where(a => a.Id == id).First().UserRole = user.UserRole;
                    _context.MusicSystemUsers.Where(a => a.Id == id).First().NormalizedEmail = user.Email.ToUpper();
                    _context.MusicSystemUsers.Where(a => a.Id == id).First().NormalizedUserName = user.Email.ToUpper();
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MusicSystemUsers.Any(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(UserIndex));
            }
            return View("UserIndex");
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UserDelete(string id)
        {
            if (id == null)
                return NotFound();
            var user = await _context.MusicSystemUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
                return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("UserDelete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.MusicSystemUsers.FindAsync(id);
            _context.MusicSystemUsers.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("UserIndex");
        }

        // Song Management
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> SongIndex(string SearchString, string ArtistName)
        {
            var songs = from s in _context.Song
                        select s;
            if (!String.IsNullOrEmpty(SearchString))
                songs = songs.Where(s => s.songName.Contains(SearchString));
            IQueryable<String> TypeQuery = from s in _context.Song orderby s.artistName select s.artistName;
            IEnumerable<SelectListItem> items = new SelectList(await TypeQuery.Distinct().ToListAsync());
            ViewBag.ArtistName = items;
            if (!String.IsNullOrEmpty(ArtistName))
                songs = songs.Where(s => s.artistName.Equals(ArtistName));
            return View(await songs.ToListAsync());
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> SongDetail(int? id)
        {
            if (id == null)
                return NotFound();
            var song = await _context.Song.FirstOrDefaultAsync(m => m.songId == id);
            if (song == null)
                return NotFound();
            return View(song);
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> SongEdit(int? id)
        {
            if (id == null)
                return NotFound();
            var song = await _context.Song.FindAsync(id);
            if (song == null)
                return NotFound();
            return View(song);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> SongEdit(int id, [Bind("songId,songName,duration,albumName,releasedDate,songCost")] Song song)
        {
            if (id != song.songId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    song.artistName = _context.Song.Where(x => x.songId == id).FirstOrDefault().artistName;
                    var songOld = await _context.Song.AsNoTracking().FirstOrDefaultAsync(x => x.songId == id);
                    song.songDownload = songOld.songDownload;
                    song.totalEarning = songOld.totalEarning;
                    _context.Update(song);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Song.Any(e => e.songId == id))
                        return NotFound();
                    else
                        throw;
                    
                }
                return RedirectToAction("SongIndex");
            }
            return View(song);
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> SongDelete(int? id)
        {
            if (id == null)
                return NotFound();

            var song = await _context.Song.FirstOrDefaultAsync(m => m.songId == id);
            if (song == null)
                return NotFound();

            return View(song);
        }

        [HttpPost, ActionName("SongDelete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var song = await _context.Song.FindAsync(id);
            _context.Song.Remove(song);
            await _context.SaveChangesAsync();
            return RedirectToAction("SongIndex");
        }

        // Artist Management

        [Authorize(Policy = "AdminAccess")]
        public IActionResult ArtistIndex()
        {
            var artists = _context.Artist;
            return View(artists);
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> ApproveArtist(string id)
        {
            _context.Artist.Where(a => a.UserID == id).First().ArtistStatus = "Verified";    
            await _context.SaveChangesAsync();
            var user = await _userManager.FindByIdAsync(id);
            user.ArtistStatus = "Verified";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ArtistIndex));
        }

        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> RemoveArtist(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            user.ArtistStatus = "None";
            await _context.SaveChangesAsync();
            _context.Artist.Remove(_context.Artist.Where(a => a.UserID == id).First());
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ArtistIndex));
        }
    }
}
