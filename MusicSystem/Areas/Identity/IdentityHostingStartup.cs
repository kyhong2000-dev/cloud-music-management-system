using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicSystem.Areas.Identity.Data;
using MusicSystem.Data;

[assembly: HostingStartup(typeof(MusicSystem.Areas.Identity.IdentityHostingStartup))]
namespace MusicSystem.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<MusicSystemContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("MusicSystemContextConnection")));
                services.AddIdentity<MusicSystemUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddDefaultUI()
                    .AddEntityFrameworkStores<MusicSystemContext>()
                    .AddDefaultTokenProviders();
                services.AddScoped<IUserClaimsPrincipalFactory<MusicSystemUser>,
                    ApplicationUserClaimsPrincipalFactory
                    >();
            });
        }
    }
}