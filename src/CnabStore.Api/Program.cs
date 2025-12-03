using CnabStore.Api.Application;
using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration)
                                      .Enrich.FromLogContext()
                                      .CreateLogger();

builder.Host.UseSerilog();

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
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Applying database migrations...");
    db.Database.Migrate();
    logger.LogInformation("Database migrations applied successfully.");
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
                                       ILoggerFactory loggerFactory,
                                       CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("CnabUpload");
    logger.LogInformation("Received CNAB upload request.");

    if (!request.HasFormContentType)
    {
        logger.LogWarning("Upload rejected: invalid content type {ContentType}.", request.ContentType);
        return Results.BadRequest("Form content type expected (multipart/form-data).");
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file");

    if (file is null || file.Length == 0)
    {
        logger.LogWarning("No file uploaded or file is empty.");
        return Results.BadRequest("CNAB file is required.");
    }

    await using var stream = file.OpenReadStream();
    var result = await importService.ImportAsync(stream, cancellationToken);

    // Return detailed information about imported and failed lines
    logger.LogInformation("Import finished for file {FileName}. TotalLines={TotalLines}, Imported={Imported}, Failed={Failed}.",
                          file.FileName,
                          result.TotalLines,
                          result.ImportedCount,
                          result.FailedCount);

    return Results.Ok(result);
});

// Store summary endpoint (list of imported operations by store + total balance)
app.MapGet("/api/stores/summary", async (AppDbContext db,
                                         int page,
                                         int pageSize,
                                         ILoggerFactory loggerFactory,
                                         CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("StoreSummary");

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

    logger.LogInformation("Fetching store summary. Page={Page}, PageSize={PageSize}.", page, pageSize);


    var totalItems = await db.Stores.CountAsync(cancellationToken);

    var totalPages = totalItems == 0
        ? 0
        : (int)Math.Ceiling(totalItems / (double)pageSize);

    var skip = (page - 1) * pageSize;

    var query = db.Stores.OrderBy(s => s.Name)
                         .Select(s => new StoreSummaryDto(s.Id,
                                                          s.Name,
                                                          s.OwnerName,
                                                          s.Transactions.Sum(t => t.Value)));

    var items = await query.Skip(skip)
                           .Take(pageSize)
                           .ToListAsync(cancellationToken);

    logger.LogInformation("Store summary fetched. TotalItems={TotalItems}, TotalPages={TotalPages}, ReturnedItems={ReturnedItems}.",
                          totalItems,
                          totalPages,
                          items.Count);

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
