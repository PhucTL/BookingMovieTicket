using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.Constants;
using BookingMovieTicket_Repository;
using BookingMovieTicket_Repository.Basic;
using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Repository.Repositories;
using BookingMovieTicket_API.Filters;
using BookingMovieTicket_API.Hubs;
using BookingMovieTicket_Service.Authentication;
using BookingMovieTicket_Service.Authentication.Email;
using BookingMovieTicket_Service.Authentication.JWT;
using BookingMovieTicket_Service.Authentication.OTP;
using BookingMovieTicket_Service.BackgroundJobs;
using BookingMovieTicket_Service.Booking;
using BookingMovieTicket_Service.Realtime;
using BookingMovieTicket_Service.Redis;
using BookingMovieTicket_Service.Showtime;
using BookingMovieTicket_Service.Venue;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Cấu hình DbContext kết nối PostgreSQL từ appsettings.json
builder.Services.AddDbContext<BookingMovieTicketSystemDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// 2. Đăng ký UnitOfWork và Repositories vào DI Container
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthenRepository, AuthenRepository>();
builder.Services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped(typeof(GenericRepository<>));

// 3. Đăng ký MemoryCache, Redis & các Services
builder.Services.AddMemoryCache();

// Cấu hình Redis Connection & RedisService
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                            ?? builder.Configuration["Redis:ConnectionString"]
                            ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConnectionString);
    config.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(config);
});
builder.Services.AddScoped<IRedisService, RedisService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthenService, AuthenService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IVenueService, VenueService>();

// Đăng ký SignalR kèm Redis Backplane để scale-out phân tán
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("BookingMovieTicket_SignalR");
    });
builder.Services.AddScoped<ISeatNotificationService, SeatNotificationService>();

// 3.5. Cấu hình Hangfire Background Jobs sử dụng PostgreSQL Storage
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(defaultConnectionString));
});

builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
    options.WorkerCount = Environment.ProcessorCount * 2;
});

builder.Services.AddScoped<ISeatExpirationJob, SeatExpirationJob>();

// 4. Cấu hình Authentication với JWT Bearer Token
var jwtKey = builder.Configuration["Jwt:Key"] ?? "BookingMovieTicket_SuperSecretKey_2026_Secure_Key_!@#$%";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BookingMovieTicketAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BookingMovieTicketClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                var token = authHeader.Trim();
                while (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring(7).Trim();
                }
                token = token.Trim('"').Trim('\'');
                context.Token = token;
            }
            else
            {
                // Hỗ trợ WebSocket của SignalR lấy Token từ query string ?access_token=...
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var redisService = context.HttpContext.RequestServices.GetRequiredService<IRedisService>();
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.Principal?.FindFirst("userId")?.Value
                         ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(jti))
            {
                var activeJti = await redisService.GetUserSessionAsync(userId);
                // Nếu Redis đang có active session và JTI không khớp => Đã đăng nhập ở nơi khác => Đá ra
                if (!string.IsNullOrEmpty(activeJti) && activeJti != jti)
                {
                    context.Fail("Tài khoản của bạn đã được đăng nhập ở thiết bị khác. Phiên đăng nhập này đã bị kết thúc.");
                }
            }
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"\n[JWT AUTH FAILED] Lỗi xác thực token: {context.Exception.Message}\n");
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            // Trả về JSON thông báo lỗi khi bị đá tài khoản hoặc xác thực thất bại
            if (context.AuthenticateFailure != null)
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";

                var message = context.AuthenticateFailure.Message;
                var apiResponse = ApiResponse<string>.ErrorResult(message);
                var json = System.Text.Json.JsonSerializer.Serialize(apiResponse);
                await context.Response.WriteAsync(json);
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy => policy.RequireRole(RoleConstants.Staff, "1"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole(RoleConstants.User, "0"));
});

builder.Services.AddControllers();

// 5. Cấu hình Swagger kèm nút Authorize Bearer Token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BookingMovieTicket API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Dán trực tiếp chuỗi JWT Token vào đây",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingMovieTicket API v1");
    });
}

app.UseHttpsRedirection();

// Kích hoạt Authentication trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Cấu hình Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "BookingMovieTicket - Background Jobs"
});

// Đăng ký Recurring Job tự động quét và giải phóng các ghế giữ hết hạn mỗi phút
RecurringJob.AddOrUpdate<ISeatExpirationJob>(
    "release-expired-seats",
    job => job.ReleaseExpiredSeatsAsync(),
    Cron.Minutely()
);

app.MapControllers();
app.MapHub<SeatHub>("/hubs/seats");

app.Run();
