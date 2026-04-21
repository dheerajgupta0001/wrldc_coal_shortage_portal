using CoalShortagePortal.Core.Interfaces.Sms;
using CoalShortagePortal.Infrastructure.Services;
using CoalShortagePortal.Infrastructure.Services.Email;
using CoalShortagePortal.Infrastructure.Services.ReportingData;
using CoalShortagePortal.Infrastructure.Services.Sms;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CoalShortagePortal.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // add email settings from app config
            EmailConfiguration emailConfig = new EmailConfiguration();
            configuration.Bind("EmailSettings", emailConfig);
            services.AddSingleton(emailConfig);

            // add sms settings from app config
            SmsConfiguration smsConfig = new SmsConfiguration();
            configuration.Bind("SmsSettings", smsConfig);
            services.AddSingleton(smsConfig);

            // Add this line with your other service registrations
            services.AddSingleton<IOtpRateLimitService, OtpRateLimitService>();
            // Singleton because the in-memory dictionary must persist across requests

            // Add Infra services
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<ISmsSender, SmsSender>();
            services.AddTransient<ReportingDataService>();

            services.AddDNTCaptcha(options =>
            {
                // options.UseSessionStorageProvider(); // -> It doesn't rely on the server or client's times. Also it's the safest one.
                // options.UseMemoryCacheStorageProvider(); // -> It relies on the server's times. It's safer than the CookieStorageProvider.
                options.UseCookieStorageProvider() // -> It relies on the server and client's times. It's ideal for scalability, because it doesn't save anything in the server's memory.
                .ShowThousandsSeparators(false)
                .WithEncryptionKey(Guid.NewGuid().ToString());
                // options.UseDistributedCacheStorageProvider(); // --> It's ideal for scalability using `services.AddStackExchangeRedisCache()` for instance.
                // options.UseDistributedSerializationProvider();
            });

            return services;
        }
    }
}
