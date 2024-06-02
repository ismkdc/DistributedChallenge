using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/", (CreateReportRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("{TraceId}, {Title}, {Expression}", request.TraceId, request.Title, request.Expression);
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);

    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        var errors = validationResults
            .GroupBy(e => e.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage ?? string.Empty).ToArray()
            );

        return Results.ValidationProblem(errors);
    }

    if (!Guid.TryParse(request.TraceId, out var traceId))
    {
        logger.LogWarning("TraceId must be a valid GUID.");
        return Results.BadRequest(new { error = "TraceId must be a valid GUID." });
    }

    Random rnd = new();
    // Gelen talepteki bilgilere göre rapor talebini benzersiz bir veri modeli ile damgalamak istiyoruz
    var refDocId = new ReferenceDocumentId
    {
        Head = rnd.Next(1000, 1100),
        Source = rnd.Next(1, 10),
        Stamp = Guid.NewGuid(),
    };

    logger.LogInformation("Created Referenced Document Id: {RefDocumentId}", refDocId.ToString());

    // Bu sistem kendi için rapor hazırlama işini başlatıyor şeklinde düşünelim.
    // Request üzerinden gelen Expression içeriğinin de Gen AI tarzı bir API ile bu sistemde 
    // anlamlı ve işletilebilir bir ifadeye dönüştürüldüğünü düşünelim.
    // Şu an için test amaçlı sabit bir response döndürmesi yeterli

    var response = new CreateReportResponse
    {
        Status = StatusCode.Success,
        DocumentId = refDocId.ToString(),
        Explanation = "Rapor talebi başarılı bir şekilde alındı"
    };
    return Results.Json(response);
})
.WithName("CreateReportRequest")
.WithOpenApi();

app.Run();

class CreateReportRequest
{
    [Required]
    public string? TraceId { get; set; }

    [Required]
    [StringLength(30, MinimumLength = 20, ErrorMessage = "Title length must be between 20 and 30 characters.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Expression must be filled.")]
    [StringLength(100, MinimumLength = 30, ErrorMessage = "Expression length must be between 30 and 100 characters.")]
    public string? Expression { get; set; }
}


enum StatusCode
{
    Success = 1,
    ReportReady = 200,
    InvalidExpression = 400,
    Fail = 500
}

struct ReferenceDocumentId
{
    public int Head { get; set; }
    public int Source { get; set; }
    public Guid Stamp { get; set; }
    public override readonly string ToString()
    {
        return $"{Head}-{Source}-{Stamp}";
    }
}

class CreateReportResponse
{
    public StatusCode Status { get; set; }

    [JsonIgnore]
    public ReferenceDocumentId ReferenceDocumentId { get; set; }

    public string? DocumentId { get; set; }
    public string? Explanation { get; set; }
}