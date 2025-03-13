using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Entities;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Cores;
using BloodCenter.Service.Utils.Auth;
using BloodCenter.Service.Utils.Interface;
using BloodCenter.Service.Utils.Mapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using BloodCenter.Service.Utils.Backgrounds.Interface;
using BloodCenter.Service.Utils.Backgrounds;
using Quartz.Simpl;
using Quartz.Spi;
using Quartz;
using BloodCenter.Service.Utils.Redis.Cache;
using MassTransit;
using BloodCenter.Service.Utils.Consumer;


var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection("Jwt");



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var redisSettings = builder.Configuration.GetSection("Redis");
string redisHost = redisSettings["Host"];
int redisPort = int.Parse(redisSettings["Port"]);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{redisHost}:{redisPort}";
    options.InstanceName = "Auth_";
});


builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter the Bearer token: `Bearer YOUR_GENERATED_JWT_TOKEN`",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
            ValidAudience = jwtSettings.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.GetValue<string>("Key"))),
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });
builder.Services.AddDbContext<BloodCenterContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UpdateCacheConsumer>();
    x.AddEntityFrameworkOutbox<BloodCenterContext>(cfg =>
    {
        cfg.QueryDelay = TimeSpan.FromSeconds(1);
        cfg.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
        cfg.UsePostgres();
        cfg.UseBusOutbox();
        Console.WriteLine(" Outbox has been configured!");
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("update-cache-queue", configurator =>
        {
            configurator.UseEntityFrameworkOutbox<BloodCenterContext>(context);
            configurator.ConfigureConsumer<UpdateCacheConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});




builder.Services.AddIdentity<Account, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
    .AddEntityFrameworkStores<BloodCenterContext>()
    .AddDefaultTokenProviders();
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("UpdateActivity");

    q.AddJob<QuartzJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("UpdateActivityTrigger")
        .WithCronSchedule("0 0 0 * * ?", x => x.InTimeZone(TimeZoneInfo.Local))
    );
});
builder.Services.AddSingleton<IJobFactory, MicrosoftDependencyInjectionJobFactory>();
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddSingleton(provider => provider.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);
builder.Services.AddScoped<IAuth, Auth>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwt, Jwt>();
builder.Services.AddScoped<IAdmin, AdminService>();
builder.Services.AddScoped<IHospital, HospitalService>();
builder.Services.AddScoped<IAuthRedisCacheService, AuthRedisCacheService>();
builder.Services.AddScoped<IDonor, DonorService>();
builder.Services.AddScoped<IQuartzWorker, QuartzWorker>();
builder.Services.AddTransient<QuartzJob>();
builder.Services.AddSingleton<QuartzStartProgram>();
var app = builder.Build();
app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
