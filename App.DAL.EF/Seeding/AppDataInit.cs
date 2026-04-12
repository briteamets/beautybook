using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }

    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
        EnsureAdminManagementSchema(context);
    }

    private static void EnsureAdminManagementSchema(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw("""
            ALTER TABLE "AspNetUsers"
                ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();

            ALTER TABLE "AspNetUsers"
                ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;

            ALTER TABLE "ServiceCategories"
                ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;

            CREATE TABLE IF NOT EXISTS "ServiceTypes" (
                "Id" uuid NOT NULL,
                "ServiceCategoryId" uuid NOT NULL,
                "Name" jsonb NOT NULL,
                "Description" jsonb NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                CONSTRAINT "PK_ServiceTypes" PRIMARY KEY ("Id")
            );

            ALTER TABLE "Services"
                ADD COLUMN IF NOT EXISTS "ServiceTypeId" uuid NULL;

            ALTER TABLE "Bookings"
                ADD COLUMN IF NOT EXISTS "CancelledByAdminId" uuid NULL;

            ALTER TABLE "Bookings"
                ADD COLUMN IF NOT EXISTS "CancellationReason" character varying(2000) NULL;

            CREATE INDEX IF NOT EXISTS "IX_ServiceTypes_ServiceCategoryId"
                ON "ServiceTypes" ("ServiceCategoryId");

            CREATE INDEX IF NOT EXISTS "IX_Services_ServiceTypeId"
                ON "Services" ("ServiceTypeId");

            CREATE INDEX IF NOT EXISTS "IX_Bookings_CancelledByAdminId"
                ON "Bookings" ("CancelledByAdminId");

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_ServiceTypes_ServiceCategories_ServiceCategoryId'
                ) THEN
                    ALTER TABLE "ServiceTypes"
                        ADD CONSTRAINT "FK_ServiceTypes_ServiceCategories_ServiceCategoryId"
                        FOREIGN KEY ("ServiceCategoryId")
                        REFERENCES "ServiceCategories" ("Id")
                        ON DELETE RESTRICT;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_Services_ServiceTypes_ServiceTypeId'
                ) THEN
                    ALTER TABLE "Services"
                        ADD CONSTRAINT "FK_Services_ServiceTypes_ServiceTypeId"
                        FOREIGN KEY ("ServiceTypeId")
                        REFERENCES "ServiceTypes" ("Id")
                        ON DELETE SET NULL;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_Bookings_AspNetUsers_CancelledByAdminId'
                ) THEN
                    ALTER TABLE "Bookings"
                        ADD CONSTRAINT "FK_Bookings_AspNetUsers_CancelledByAdminId"
                        FOREIGN KEY ("CancelledByAdminId")
                        REFERENCES "AspNetUsers" ("Id")
                        ON DELETE SET NULL;
                END IF;
            END $$;
            """);
    }


    public static void SeedAppData(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var tomorrow = today.AddDays(1);

        var providerUser = context.Users.FirstOrDefault(u => u.Email == "provider@beautybook.com");
        if (providerUser == null)
        {
            throw new ApplicationException("BeautyBook service provider user was not found for seed data.");
        }

        var clientUser = context.Users.FirstOrDefault(u => u.Email == "user@beautybook.com");
        if (clientUser == null)
        {
            throw new ApplicationException("BeautyBook client user was not found for seed data.");
        }

        var suspendedProviderUser = context.Users.AsTracking().FirstOrDefault(u => u.Email == "suspended.provider@beautybook.com");

        var nailsCategory = context.ServiceCategories
            .AsEnumerable()
            .FirstOrDefault(c => c.Name.Translate("en") == "Nails");
        if (nailsCategory == null)
        {
            nailsCategory = new ServiceCategory
            {
                Name = new LangStr("Nails", "en"),
                Description = new LangStr("Nail treatments and manicure services", "en")
                {
                    ["et"] = "Kuunte hooldus ja manikuuri teenused"
                },
                IsActive = true
            };
            nailsCategory.Name["et"] = "Küüned";
            context.ServiceCategories.Add(nailsCategory);
        }

        var hairCategory = context.ServiceCategories
            .AsEnumerable()
            .FirstOrDefault(c => c.Name.Translate("en") == "Hair");
        if (hairCategory == null)
        {
            hairCategory = new ServiceCategory
            {
                Name = new LangStr("Hair", "en"),
                Description = new LangStr("Haircuts, coloring, and styling", "en")
                {
                    ["et"] = "Juukselõikus, värvimine ja soengud"
                },
                IsActive = true
            };
            hairCategory.Name["et"] = "Juuksed";
            context.ServiceCategories.Add(hairCategory);
        }

        context.SaveChanges();

        ServiceType EnsureServiceType(ServiceCategory category, string en, string et, string descriptionEn, string descriptionEt)
        {
            var serviceType = context.ServiceTypes
                .AsEnumerable()
                .FirstOrDefault(t => t.ServiceCategoryId == category.Id && t.Name.Translate("en") == en);

            if (serviceType != null) return serviceType;

            serviceType = new ServiceType
            {
                ServiceCategoryId = category.Id,
                Name = new LangStr(en, "en")
                {
                    ["et"] = et
                },
                Description = new LangStr(descriptionEn, "en")
                {
                    ["et"] = descriptionEt
                },
                IsActive = true
            };

            context.ServiceTypes.Add(serviceType);
            context.SaveChanges();
            return serviceType;
        }

        var manicureType = EnsureServiceType(nailsCategory!, "Classic Manicure", "Klassikaline maniküür", "Cuticle care, shaping, and polish.", "Küünenaha hooldus, vormimine ja lakk.");
        var gelType = EnsureServiceType(nailsCategory!, "Gel Nails", "Geelküüned", "Gel nail service.", "Geelküünte teenus.");
        var stylingType = EnsureServiceType(hairCategory!, "Hair Styling", "Juuste stiliseerimine", "Wash, blow-dry, and styling finish.", "Pesu, föönisoeng ja viimistlus.");
        _ = EnsureServiceType(hairCategory!, "Haircut", "Juukselõikus", "Haircut and finish.", "Juukselõikus ja viimistlus.");

        var provider = context.BeautyProviders.AsTracking()
            .FirstOrDefault(p => p.UserId == providerUser.Id || p.ProfileName == "BeautyBook Studio");
        if (provider == null)
        {
            provider = new BeautyProvider
            {
                UserId = providerUser.Id,
                ProfileName = "BeautyBook Studio",
                ProfilePictureUrl = "https://images.example.com/providers/beautybook-studio.jpg",
                Bio = "Independent beauty provider offering nails and hair services.",
                Latitude = 59.4370,
                Longitude = 24.7536,
                Address = "Maakri 19/1, Tallinn",
                From = now
            };
            context.BeautyProviders.Add(provider);
            context.SaveChanges();
        }
        else if (provider.UserId != providerUser.Id)
        {
            provider.UserId = providerUser.Id;
            context.SaveChanges();
        }

        if (!context.Services
                .AsEnumerable()
                .Any(s => s.BeautyProviderId == provider.Id && s.Name.Translate("en") == "Classic Manicure"))
        {
            context.Services.Add(new Service
            {
                BeautyProviderId = provider.Id,
                ServiceCategoryId = nailsCategory!.Id,
                ServiceTypeId = manicureType.Id,
                Name = new LangStr("Classic Manicure", "en")
                {
                    ["et"] = "Klassikaline maniküür"
                },
                Description = new LangStr("Cuticle care, shaping, and polish.", "en")
                {
                    ["et"] = "Küünenaha hooldus, vormimine ja lakk."
                },
                Price = 35m,
                DurationMinutes = 60,
                IsActive = true,
                From = now
            });
        }

        if (!context.Services
                .AsEnumerable()
                .Any(s => s.BeautyProviderId == provider.Id && s.Name.Translate("en") == "Hair Styling"))
        {
            context.Services.Add(new Service
            {
                BeautyProviderId = provider.Id,
                ServiceCategoryId = hairCategory!.Id,
                ServiceTypeId = stylingType.Id,
                Name = new LangStr("Hair Styling", "en")
                {
                    ["et"] = "Juuste stiliseerimine"
                },
                Description = new LangStr("Wash, blow-dry, and styling finish.", "en")
                {
                    ["et"] = "Pesu, föönisoeng ja viimistlus."
                },
                Price = 45m,
                DurationMinutes = 75,
                IsActive = true,
                From = now
            });
        }

        context.SaveChanges();

        var manicureService = context.Services
            .AsEnumerable()
            .First(s => s.BeautyProviderId == provider.Id && s.Name.Translate("en") == "Classic Manicure");
        if (manicureService.ServiceTypeId == null)
        {
            manicureService.ServiceTypeId = manicureType.Id;
            context.Services.Update(manicureService);
        }

        var stylingService = context.Services
            .AsEnumerable()
            .First(s => s.BeautyProviderId == provider.Id && s.Name.Translate("en") == "Hair Styling");
        if (stylingService.ServiceTypeId == null)
        {
            stylingService.ServiceTypeId = stylingType.Id;
            context.Services.Update(stylingService);
        }

        context.SaveChanges();

        if (suspendedProviderUser != null)
        {
            suspendedProviderUser.IsActive = false;
            var suspendedProvider = context.BeautyProviders.AsTracking()
                .FirstOrDefault(p => p.UserId == suspendedProviderUser.Id || p.ProfileName == "Suspended Beauty Provider");

            if (suspendedProvider == null)
            {
                suspendedProvider = new BeautyProvider
                {
                    UserId = suspendedProviderUser.Id,
                    ProfileName = "Suspended Beauty Provider",
                    Bio = "Suspended provider profile.",
                    Latitude = 59.4370,
                    Longitude = 24.7536,
                    Address = "Tallinn",
                    From = now.AddDays(-30),
                    To = now
                };
                context.BeautyProviders.Add(suspendedProvider);
                context.SaveChanges();
            }
            else
            {
                suspendedProvider.To ??= now;
                suspendedProvider.UserId = suspendedProviderUser.Id;
                context.SaveChanges();
            }

            if (!context.Services
                    .AsEnumerable()
                    .Any(s => s.BeautyProviderId == suspendedProvider.Id && s.Name.Translate("en") == "Gel Nails"))
            {
                context.Services.Add(new Service
                {
                    BeautyProviderId = suspendedProvider.Id,
                    ServiceCategoryId = nailsCategory!.Id,
                    ServiceTypeId = gelType.Id,
                    Name = new LangStr("Gel Nails", "en")
                    {
                        ["et"] = "Geelküüned"
                    },
                    Description = new LangStr("Gel nail service.", "en")
                    {
                        ["et"] = "Geelküünte teenus."
                    },
                    Price = 50m,
                    DurationMinutes = 90,
                    IsActive = true,
                    From = now.AddDays(-30)
                });
                context.SaveChanges();
            }
        }

        var slots = new[]
        {
            new { Date = today, Start = new TimeOnly(10, 0), End = new TimeOnly(11, 0) },
            new { Date = today, Start = new TimeOnly(13, 0), End = new TimeOnly(14, 0) },
            new { Date = tomorrow, Start = new TimeOnly(11, 0), End = new TimeOnly(12, 30) }
        };

        foreach (var slot in slots)
        {
            var exists = context.Availabilities.Any(a =>
                a.BeautyProviderId == provider.Id &&
                a.Date == slot.Date &&
                a.StartTime == slot.Start &&
                a.EndTime == slot.End);

            if (exists) continue;

            context.Availabilities.Add(new Availability
            {
                BeautyProviderId = provider.Id,
                Date = slot.Date,
                StartTime = slot.Start,
                EndTime = slot.End,
                IsBooked = false,
                From = now
            });
        }

        context.SaveChanges();

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var isWorkingDay = day is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
            var schedule = context.ProviderSchedules.FirstOrDefault(s =>
                s.ProviderId == provider.Id &&
                s.DayOfWeek == day);

            if (schedule == null)
            {
                context.ProviderSchedules.Add(new ProviderSchedule
                {
                    ProviderId = provider.Id,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    IsWorkingDay = isWorkingDay
                });
            }
        }

        if (!context.ProviderBreaks.Any(b =>
                b.ProviderId == provider.Id &&
                b.DayOfWeek == DayOfWeek.Monday &&
                b.BreakStart == new TimeSpan(12, 0, 0)))
        {
            context.ProviderBreaks.Add(new ProviderBreak
            {
                ProviderId = provider.Id,
                DayOfWeek = DayOfWeek.Monday,
                BreakStart = new TimeSpan(12, 0, 0),
                BreakEnd = new TimeSpan(13, 0, 0)
            });
        }

        context.SaveChanges();

        var salon = context.Salons
            .AsEnumerable()
            .FirstOrDefault(s => s.Name.Translate("en") == "BeautyBook Salon");
        if (salon == null)
        {
            salon = new Salon
            {
                Name = new LangStr("BeautyBook Salon", "en")
                {
                    ["et"] = "BeautyBook Ilusalong"
                },
                Description = new LangStr("Boutique salon in central Tallinn.", "en")
                {
                    ["et"] = "Butiik-ilusalong Tallinna kesklinnas."
                },
                Address = "Tartu mnt 25, Tallinn",
                Latitude = 59.4339,
                Longitude = 24.7616,
                ProfilePictureUrl = "https://images.example.com/salons/beautybook-salon.jpg",
                From = now
            };
            context.Salons.Add(salon);
            context.SaveChanges();
        }

        var salonStaffExists = context.SalonStaff.Any(ss =>
            ss.SalonId == salon.Id &&
            ss.BeautyProviderId == provider.Id);

        if (!salonStaffExists)
        {
            context.SalonStaff.Add(new SalonStaff
            {
                SalonId = salon.Id,
                BeautyProviderId = provider.Id,
                Role = SalonStaff.RoleOwner,
                From = now
            });
            context.SaveChanges();
        }

        var bookingSeed = new[]
        {
            new { Service = manicureService, Date = today, Start = new TimeOnly(10, 0), End = new TimeOnly(11, 0), Status = Booking.StatusPending },
            new { Service = stylingService, Date = today, Start = new TimeOnly(13, 0), End = new TimeOnly(14, 15), Status = Booking.StatusConfirmed },
            new { Service = manicureService, Date = tomorrow, Start = new TimeOnly(11, 0), End = new TimeOnly(12, 0), Status = Booking.StatusCancelled },
            new { Service = stylingService, Date = today.AddDays(-1), Start = new TimeOnly(9, 0), End = new TimeOnly(10, 15), Status = Booking.StatusCompleted },
            new { Service = manicureService, Date = today.AddDays(2), Start = new TimeOnly(15, 0), End = new TimeOnly(16, 0), Status = Booking.StatusConfirmed }
        };

        foreach (var item in bookingSeed)
        {
            var exists = context.Bookings.Any(b =>
                b.UserId == clientUser.Id &&
                b.ServiceId == item.Service.Id &&
                b.BookingDate == item.Date &&
                b.StartTime == item.Start.ToTimeSpan());

            if (exists) continue;

            var booking = new Booking
            {
                UserId = clientUser.Id,
                ServiceId = item.Service.Id,
                SalonId = salon.Id,
                BookedAt = now.AddHours(-Array.IndexOf(bookingSeed, item) - 1),
                AppointmentDateTime = item.Date.ToDateTime(item.Start, DateTimeKind.Utc),
                BookingDate = item.Date,
                StartTime = item.Start.ToTimeSpan(),
                EndTime = item.End.ToTimeSpan(),
                Status = item.Status,
                From = now.AddHours(-Array.IndexOf(bookingSeed, item) - 1),
                CancellationReason = item.Status == Booking.StatusCancelled ? "Client requested cancellation." : null
            };
            context.Bookings.Add(booking);
            context.SaveChanges();

            if (item.Status == Booking.StatusCancelled)
            {
                context.BookingHistories.Add(new BookingHistory
                {
                    BookingId = booking.Id,
                    OldStatus = Booking.StatusConfirmed,
                    NewStatus = Booking.StatusCancelled,
                    UserId = clientUser.Id,
                    ChangedAt = booking.BookedAt.AddMinutes(30),
                    Reason = booking.CancellationReason,
                    From = booking.BookedAt.AddMinutes(30)
                });
                context.SaveChanges();
            }
        }
    }
    
     public static void SeedIdentity(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        foreach (var roleName in InitialData.Roles)
        {
            var role = roleManager.FindByNameAsync(roleName).Result;

            if (role != null) continue;

            role = new AppRole()
            {
                Name = roleName,
            };

            var result = roleManager.CreateAsync(role).Result;
            if (!result.Succeeded)
            {
                throw new ApplicationException("Role creation failed!");
            }
        }


        foreach (var userInfo in InitialData.Users)
        {
            var user = userManager.FindByEmailAsync(userInfo.email).Result;
            if (user == null)
            {
                user = new AppUser()
                {
                    Email = userInfo.email,
                    UserName = userInfo.email,
                    EmailConfirmed = true
                };
                var result = userManager.CreateAsync(user, userInfo.password).Result;
                if (!result.Succeeded)
                {
                    throw new ApplicationException("User creation failed!");
                }
            }

            foreach (var role in userInfo.roles)
            {
                if (userManager.IsInRoleAsync(user, role).Result)
                {
                    Console.WriteLine($"User {user.UserName} already in role {role}");
                    continue;
                }
                
                var roleResult = userManager.AddToRoleAsync(user, role).Result;
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }
                else
                {
                    Console.WriteLine($"User {user.UserName} added to role {role}");
                }
            }
        }
    }

}
