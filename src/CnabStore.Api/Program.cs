using CnabStore.Api.Application;
using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=cnabstore;Username=cnabuser;Password=cnabpass";

// EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Application layer (services and parsers)
builder.Services.AddApplicationServices();

// Minimal API extras
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply database migrations on startup (for simplicity in the challenge)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Root endpoint serving the HTML page
app.MapGet("/", async context =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "Web", "wwwroot", "index.html");

    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("index.html not found.");
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(filePath);
});

// File upload endpoint for CNAB
app.MapPost("/api/cnab/upload", async (HttpRequest request,
                                       ICnabImportService importService,
                                       CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Form content type expected (multipart/form-data).");
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file");

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("CNAB file is required.");
    }

    await using var stream = file.OpenReadStream();
    var result = await importService.ImportAsync(stream, cancellationToken);

    // Return detailed information about imported and failed lines
    return Results.Ok(result);
});

// Store summary endpoint (list of imported operations by store + total balance)
app.MapGet("/api/stores/summary", async (
        AppDbContext db,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
{
    // Basic guards and defaults
    if (page <= 0)
    {
        page = 1;
    }

    if (pageSize <= 0)
    {
        pageSize = 10;
    }
    else if (pageSize > 100)
    {
        pageSize = 100; // hard limit to avoid insane page sizes
    }

    var totalItems = await db.Stores.CountAsync(cancellationToken);

    var totalPages = totalItems == 0
        ? 0
        : (int)Math.Ceiling(totalItems / (double)pageSize);

    var skip = (page - 1) * pageSize;

    var query = db.Stores
        .OrderBy(s => s.Name)
        .Select(s => new StoreSummaryDto(
            s.Id,
            s.Name,
            s.OwnerName,
            s.Transactions.Sum(t => t.Value)
        ));

    var items = await query.Skip(skip)
                           .Take(pageSize)
                           .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        page,
        pageSize,
        totalItems,
        totalPages,
        items
    });
})
.WithName("GetStoreSummary");

app.Run();
