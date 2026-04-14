var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<FileService>(); // shared across requests

// For authentification
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.Cookie.HttpOnly = true;   // JS can't read the cookie
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

var app = builder.Build();

app.UseWebSockets();
app.UseSession();

// Middleware: Auth session check
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    // Always allow: login page, auth API, and static assets for the login page
    bool isPublic = path.StartsWithSegments("/login.html")
                    || path.StartsWithSegments("/login.js")
                    || path.StartsWithSegments("/api/auth")
                    || path.StartsWithSegments("/styles.css");
    if (isPublic)
    {
        await next(context); return;
    }

    bool authenticated = context.Session.GetString("auth") == "1";

    if (!authenticated)
    {
        if (path.StartsWithSegments("/api") || path.StartsWithSegments("/ws"))
        {
            context.Response.StatusCode = 401;
        }
        else
        {
            context.Response.Redirect("/login.html");
        }
        return;
    }

    await next(context);
});

app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles();   // makes / serve index.html
app.UseStaticFiles();    // serves everything else in wwwroot

// WebSocket watcher — not a controller, lives here
app.Map("/ws/watch", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest) {
        context.Response.StatusCode = 400; return;
    }
    var ws = await context.WebSockets.AcceptWebSocketAsync();
    var rootPath = builder.Configuration["RootPath"]!;
    var handler = new FileWatcherHandler(ws, rootPath);
    await handler.HandleAsync();
});

app.Run();