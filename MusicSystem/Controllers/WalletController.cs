using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MusicSystem.Areas.Identity.Data;
using MusicSystem.Data;
using System.Threading.Tasks;

namespace MusicSystem.Controllers
{
    public class WalletController : Controller
    {
        private readonly MusicSystemContext _context;
        private readonly UserManager<MusicSystemUser> _userManager;

        public WalletController(MusicSystemContext context, UserManager<MusicSystemUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Deposit()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Balance = user.AccountBalance;
            return View();
        }

        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Withdraw()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Balance = user.AccountBalance;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> TopUp(decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            user.AccountBalance = user.AccountBalance + amount;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Deposit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Withdraw(decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user.AccountBalance - amount > 0)
            {
                user.AccountBalance = user.AccountBalance - amount;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Withdraw));
            }
            ViewBag.Balance = user.AccountBalance;
            ViewBag.Error = "Insufficient Funds";
            return View("Withdraw");
        }
    }
}
