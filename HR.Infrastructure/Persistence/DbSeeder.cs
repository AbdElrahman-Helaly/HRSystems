using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await db.Departments.AnyAsync(ct))
        {
            db.Departments.AddRange(
                new Department { Name = "Human Resources" },
                new Department { Name = "IT" },
                new Department { Name = "Finance" }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Nationalities.AnyAsync(ct))
        {
            db.Nationalities.AddRange(
                new Nationality { Name = "Egyptian", NameAr = "مصري" },
                new Nationality { Name = "Saudi", NameAr = "سعودي" },
                new Nationality { Name = "Emirati", NameAr = "إماراتي" },
                new Nationality { Name = "Jordanian", NameAr = "أردني" },
                new Nationality { Name = "Lebanese", NameAr = "لبناني" },
                new Nationality { Name = "Syrian", NameAr = "سوري" },
                new Nationality { Name = "Iraqi", NameAr = "عراقي" },
                new Nationality { Name = "Kuwaiti", NameAr = "كويتي" },
                new Nationality { Name = "Qatari", NameAr = "قطري" },
                new Nationality { Name = "Bahraini", NameAr = "بحريني" },
                new Nationality { Name = "Omani", NameAr = "عماني" },
                new Nationality { Name = "Yemeni", NameAr = "يمني" },
                new Nationality { Name = "Palestinian", NameAr = "فلسطيني" },
                new Nationality { Name = "Sudanese", NameAr = "سوداني" },
                new Nationality { Name = "Moroccan", NameAr = "مغربي" },
                new Nationality { Name = "Tunisian", NameAr = "تونسي" },
                new Nationality { Name = "Algerian", NameAr = "جزائري" },
                new Nationality { Name = "Libyan", NameAr = "ليبي" },
                new Nationality { Name = "Other", NameAr = "أخرى" }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Governorates.AnyAsync(ct))
        {
            db.Governorates.AddRange(
                new Governorate { Name = "Cairo", NameAr = "القاهرة" },
                new Governorate { Name = "Alexandria", NameAr = "الإسكندرية" },
                new Governorate { Name = "Giza", NameAr = "الجيزة" },
                new Governorate { Name = "Qalyubia", NameAr = "القليوبية" },
                new Governorate { Name = "Dakahlia", NameAr = "الدقهلية" },
                new Governorate { Name = "Sharqia", NameAr = "الشرقية" },
                new Governorate { Name = "Monufia", NameAr = "المنوفية" },
                new Governorate { Name = "Gharbia", NameAr = "الغربية" },
                new Governorate { Name = "Beheira", NameAr = "البحيرة" },
                new Governorate { Name = "Kafr El Sheikh", NameAr = "كفر الشيخ" }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Cities.AnyAsync(ct))
        {
            var cairo = await db.Governorates.FirstOrDefaultAsync(g => g.Name == "Cairo", ct);
            var alexandria = await db.Governorates.FirstOrDefaultAsync(g => g.Name == "Alexandria", ct);
            var giza = await db.Governorates.FirstOrDefaultAsync(g => g.Name == "Giza", ct);

            if (cairo != null)
            {
                db.Cities.AddRange(
                    new City { GovernorateId = cairo.Id, Name = "Nasr City", NameAr = "مدينة نصر" },
                    new City { GovernorateId = cairo.Id, Name = "Maadi", NameAr = "المعادي" },
                    new City { GovernorateId = cairo.Id, Name = "Zamalek", NameAr = "الزمالك" },
                    new City { GovernorateId = cairo.Id, Name = "Heliopolis", NameAr = "مصر الجديدة" },
                    new City { GovernorateId = cairo.Id, Name = "New Cairo", NameAr = "القاهرة الجديدة" }
                );
            }

            if (alexandria != null)
            {
                db.Cities.AddRange(
                    new City { GovernorateId = alexandria.Id, Name = "Smouha", NameAr = "سموحة" },
                    new City { GovernorateId = alexandria.Id, Name = "Sidi Gaber", NameAr = "سيدي جابر" },
                    new City { GovernorateId = alexandria.Id, Name = "Stanley", NameAr = "ستانلي" }
                );
            }

            if (giza != null)
            {
                db.Cities.AddRange(
                    new City { GovernorateId = giza.Id, Name = "6th October", NameAr = "السادس من أكتوبر" },
                    new City { GovernorateId = giza.Id, Name = "Sheikh Zayed", NameAr = "الشيخ زايد" },
                    new City { GovernorateId = giza.Id, Name = "Dokki", NameAr = "الدقي" }
                );
            }

            await db.SaveChangesAsync(ct);
        }

        if (!await db.Branches.AnyAsync(ct))
        {
            db.Branches.AddRange(
                new Branch { Name = "Main Branch", NameAr = "الفرع الرئيسي" },
                new Branch { Name = "Cairo Branch", NameAr = "فرع القاهرة" },
                new Branch { Name = "Alexandria Branch", NameAr = "فرع الإسكندرية" },
                new Branch { Name = "Giza Branch", NameAr = "فرع الجيزة" }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.JobTitles.AnyAsync(ct))
        {
            db.JobTitles.AddRange(
                new JobTitle { Name = "Manager", NameAr = "مدير", JobLevel = 1, IsManagerRole = true },
                new JobTitle { Name = "Senior Developer", NameAr = "مطور أول", JobLevel = 2, IsManagerRole = false },
                new JobTitle { Name = "Developer", NameAr = "مطور", JobLevel = 3, IsManagerRole = false },
                new JobTitle { Name = "HR Specialist", NameAr = "أخصائي موارد بشرية", JobLevel = 3, IsManagerRole = false },
                new JobTitle { Name = "Accountant", NameAr = "محاسب", JobLevel = 3, IsManagerRole = false },
                new JobTitle { Name = "Administrative Assistant", NameAr = "مساعد إداري", JobLevel = 4, IsManagerRole = false }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.MaritalStatuses.AnyAsync(ct))
        {
            db.MaritalStatuses.AddRange(
                new MaritalStatus { Name = "Single", NameAr = "أعزب" },
                new MaritalStatus { Name = "Married", NameAr = "متزوج" },
                new MaritalStatus { Name = "Divorced", NameAr = "مطلق" },
                new MaritalStatus { Name = "Widowed", NameAr = "أرمل" }
            );
            await db.SaveChangesAsync(ct);
        }

        // Seed EmploymentModes (upsert missing rows even if table already has data)
        var requiredEmploymentModes = new[]
        {
            new EmploymentMode { Name = "Full Time", NameAr = "دوام كامل", IsActive = true },
            new EmploymentMode { Name = "Part Time", NameAr = "دوام جزئي", IsActive = true },
            new EmploymentMode { Name = "Shift", NameAr = "شيفت", IsActive = true }
        };

        foreach (var mode in requiredEmploymentModes)
        {
            var exists = await db.EmploymentModes.AnyAsync(
                e => e.Name.ToLower() == mode.Name.ToLower(),
                ct);

            if (!exists)
                db.EmploymentModes.Add(mode);
        }

        await db.SaveChangesAsync(ct);
    }
}


