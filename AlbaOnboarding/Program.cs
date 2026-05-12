using AlbaOnboarding.Data;
using AlbaOnboarding.Models;
using AlbaOnboarding.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// RIGHT: This tells the app to use MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Make sure you use UseMySql here!
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
// Email Service
builder.Services.AddScoped<EmailService>();

// this part is changed
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Home/Error";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
});

var app = builder.Build();

// this part is changed
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Seed roles
    string[] roles = { "Admin", "HR", "Employee" };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    // Seed Admin
    if (await userManager.FindByEmailAsync("admin@alba.com") == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "admin@alba.com",
            Email = "admin@alba.com",
            FullName = "ALBA Admin",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, "Admin@123");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Seed HR
    if (await userManager.FindByEmailAsync("hr@alba.com") == null)
    {
        var hr = new ApplicationUser
        {
            UserName = "hr@alba.com",
            Email = "hr@alba.com",
            FullName = "Test HR",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(hr, "Hr@123");
        await userManager.AddToRoleAsync(hr, "HR");
    }

    // Seed Employee
    if (await userManager.FindByEmailAsync("emp@alba.com") == null)
    {
        var emp = new ApplicationUser
        {
            UserName = "emp@alba.com",
            Email = "emp@alba.com",
            FullName = "Test Employee",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(emp, "Emp@123");
        await userManager.AddToRoleAsync(emp, "Employee");
    }

    // Seed checklist items
    if (!db.ChecklistItems.Any())
    {
        db.ChecklistItems.AddRange(
            // Pre-Employment
            new ChecklistItem { Title = "PRF Form", Description = "Pre-Employment", IsMandatory = false, DisplayOrder = 1 },
            new ChecklistItem { Title = "Advertisement", Description = "Pre-Employment", IsMandatory = false, DisplayOrder = 2 },
            new ChecklistItem { Title = "Memorandum", Description = "Pre-Employment", IsMandatory = false, DisplayOrder = 3 },
            new ChecklistItem { Title = "NRCGC Approval", Description = "Pre-Employment", IsMandatory = false, DisplayOrder = 4 },
            new ChecklistItem { Title = "Recruitment Clearance", Description = "Pre-Employment", IsMandatory = false, DisplayOrder = 5 },
            new ChecklistItem { Title = "HR Medical Report & Questionnaire", Description = "Pre-Employment", IsMandatory = true, DisplayOrder = 6 },
            new ChecklistItem { Title = "Security Clearance (CID/VISA)", Description = "Pre-Employment", IsMandatory = true, DisplayOrder = 7 },
            new ChecklistItem { Title = "LMRA Fitness Form (Expatriates only)", Description = "Pre-Employment", IsMandatory = false, DisplayOrder = 8 },

            // Recruitment Documentation
            new ChecklistItem { Title = "CV", Description = "Recruitment Documentation", IsMandatory = true, DisplayOrder = 9 },
            new ChecklistItem { Title = "Test Assessment", Description = "Recruitment Documentation", IsMandatory = false, DisplayOrder = 10 },
            new ChecklistItem { Title = "Interview Sheets", Description = "Recruitment Documentation", IsMandatory = true, DisplayOrder = 11 },
            new ChecklistItem { Title = "Matrix Report", Description = "Recruitment Documentation", IsMandatory = false, DisplayOrder = 12 },
            new ChecklistItem { Title = "Qualification", Description = "Recruitment Documentation", IsMandatory = true, DisplayOrder = 13 },
            new ChecklistItem { Title = "Experience", Description = "Recruitment Documentation", IsMandatory = true, DisplayOrder = 14 },
            new ChecklistItem { Title = "Offer Acceptance Email", Description = "Recruitment Documentation", IsMandatory = true, DisplayOrder = 15 },

            // Personal Identification
            new ChecklistItem { Title = "ID Card", Description = "Personal Identification", IsMandatory = true, DisplayOrder = 16 },
            new ChecklistItem { Title = "ID Card Data Sheet", Description = "Personal Identification", IsMandatory = true, DisplayOrder = 17 },
            new ChecklistItem { Title = "Passport", Description = "Personal Identification", IsMandatory = true, DisplayOrder = 18 },
            new ChecklistItem { Title = "Marriage Certificate", Description = "Personal Identification", IsMandatory = false, DisplayOrder = 19 },
            new ChecklistItem { Title = "Spouse & Children ID Card & Passport", Description = "Personal Identification", IsMandatory = false, DisplayOrder = 20 },

            // SIO & Payroll Processing
            new ChecklistItem { Title = "SIO Registration", Description = "SIO & Payroll Processing", IsMandatory = true, DisplayOrder = 21 },
            new ChecklistItem { Title = "PAF Form - Engagement", Description = "SIO & Payroll Processing", IsMandatory = true, DisplayOrder = 22 },
            new ChecklistItem { Title = "PAF Audit Report", Description = "SIO & Payroll Processing", IsMandatory = true, DisplayOrder = 23 },
            new ChecklistItem { Title = "Bank Transfer Letter", Description = "SIO & Payroll Processing", IsMandatory = true, DisplayOrder = 24 },

            // Administrative Documents
            new ChecklistItem { Title = "Offer (Schedule 1)", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 25 },
            new ChecklistItem { Title = "Employment Contract", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 26 },
            new ChecklistItem { Title = "Application for Employment", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 27 },
            new ChecklistItem { Title = "Disclosure of Interest", Description = "Administrative Documents", IsMandatory = false, DisplayOrder = 28 },
            new ChecklistItem { Title = "Relative in ALBA / Action in SAP", Description = "Administrative Documents", IsMandatory = false, DisplayOrder = 29 },
            new ChecklistItem { Title = "Safety Accountability", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 30 },
            new ChecklistItem { Title = "PPE Size Form", Description = "Administrative Documents", IsMandatory = false, DisplayOrder = 31 },
            new ChecklistItem { Title = "Induction Checklist", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 32 },
            new ChecklistItem { Title = "Departmental Safety Checklist", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 33 },
            new ChecklistItem { Title = "On-Boarding Inductions", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 34 },
            new ChecklistItem { Title = "Access and Security", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 35 },
            new ChecklistItem { Title = "Safety & Security Induction", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 36 },
            new ChecklistItem { Title = "ID Badge", Description = "Administrative Documents", IsMandatory = true, DisplayOrder = 37 },
            new ChecklistItem { Title = "Access Door", Description = "Administrative Documents", IsMandatory = false, DisplayOrder = 38 },

            // IT and Equipment Setup
            new ChecklistItem { Title = "Computer Setup", Description = "IT and Equipment Setup", IsMandatory = true, DisplayOrder = 39 },
            new ChecklistItem { Title = "System Access and Permissions", Description = "IT and Equipment Setup", IsMandatory = true, DisplayOrder = 40 },
            new ChecklistItem { Title = "Email Account", Description = "IT and Equipment Setup", IsMandatory = true, DisplayOrder = 41 },
            new ChecklistItem { Title = "Access to Shared Drive", Description = "IT and Equipment Setup", IsMandatory = true, DisplayOrder = 42 },
            new ChecklistItem { Title = "Extension Number", Description = "IT and Equipment Setup", IsMandatory = false, DisplayOrder = 43 },
            new ChecklistItem { Title = "Mobile Phone", Description = "IT and Equipment Setup", IsMandatory = false, DisplayOrder = 44 },
            new ChecklistItem { Title = "Software Licenses (SAP)", Description = "IT and Equipment Setup", IsMandatory = false, DisplayOrder = 45 },
            new ChecklistItem { Title = "Workstation and Office Supplies", Description = "IT and Equipment Setup", IsMandatory = false, DisplayOrder = 46 },
            new ChecklistItem { Title = "Office Desk Setup", Description = "IT and Equipment Setup", IsMandatory = false, DisplayOrder = 47 },

            // Others
            new ChecklistItem { Title = "Office Supplies", Description = "Others", IsMandatory = false, DisplayOrder = 48 },
            new ChecklistItem { Title = "Personal Protective Equipment (PPE)", Description = "Others", IsMandatory = false, DisplayOrder = 49 },
            new ChecklistItem { Title = "Locker", Description = "Others", IsMandatory = false, DisplayOrder = 50 },
            new ChecklistItem { Title = "Business Cards", Description = "Others", IsMandatory = false, DisplayOrder = 51 }
        );
        await db.SaveChangesAsync();
    }

    // Assign test employee to test HR
    var hrUser = await userManager.FindByEmailAsync("hr@alba.com");
    var empUser = await userManager.FindByEmailAsync("emp@alba.com");
    if (hrUser != null && empUser != null && string.IsNullOrEmpty(empUser.AssignedHRId))
    {
        empUser.AssignedHRId = hrUser.Id;
        await userManager.UpdateAsync(empUser);
    }
    // Seed additional demo employees for presentation
    var demoEmployees = new[]
    {
    ("sara@alba.com", "Sara Ahmed", "Engineering"),
    ("khalid@alba.com", "Khalid Al-Mansoori", "Finance"),
    ("fatema@alba.com", "Fatema Hassan", "HR"),
    ("mohammed@alba.com", "Mohammed Al-Khalifa", "Operations")
};

    foreach (var (email, name, dept) in demoEmployees)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var emp = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                Department = dept,
                JobTitle = "New Joiner",
                EmailConfirmed = true,
                OnboardingStatus = OnboardingStatus.NotStarted
            };
            await userManager.CreateAsync(emp, "Demo@123");
            await userManager.AddToRoleAsync(emp, "Employee");

            // Assign to HR
            if (hrUser != null)
            {
                emp.AssignedHRId = hrUser.Id;
                await userManager.UpdateAsync(emp);
            }
        }
        // Demo employee for end-to-end flow
        if (await userManager.FindByEmailAsync("newemployee@alba.com") == null)
        {
            var demoEmp = new ApplicationUser
            {
                UserName = "newemployee@alba.com",
                Email = "newemployee@alba.com",
                FullName = "New Demo Employee",
                EmailConfirmed = true,
                OnboardingStatus = OnboardingStatus.NotStarted
            };
            await userManager.CreateAsync(demoEmp, "Demo@123");
            await userManager.AddToRoleAsync(demoEmp, "Employee");

            if (hrUser != null)
            {
                demoEmp.AssignedHRId = hrUser.Id;
                await userManager.UpdateAsync(demoEmp);
            }
        }
    }
}

app.Run();