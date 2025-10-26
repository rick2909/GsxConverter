using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Gsx.Bridge;

public class Program
{
    public record ActionRequest(string action, object? payload);

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddCors(o => o.AddPolicy("dev", p => p
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()));

        var app = builder.Build();

        app.UseCors("dev");

        app.MapGet("/api/health", () => Results.Ok(new { ok = true }));

        app.MapPost("/api/action", async (HttpContext ctx, ILoggerFactory lf) =>
        {
            var logger = lf.CreateLogger("Action");
            var req = await JsonSerializer.DeserializeAsync<ActionRequest>(ctx.Request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (req is null || string.IsNullOrWhiteSpace(req.action))
            {
                return Results.BadRequest(new { error = "action required" });
            }

            // TODO: Hook to SimConnect/WASM: pushback, fuel, catering, etc.
            logger.LogInformation("Action received: {Action} Payload: {Payload}", req.action, JsonSerializer.Serialize(req.payload));
            return Results.Ok(new { accepted = true });
        });

        // TODO: Replace with SimConnect queries
        app.MapGet("/api/icao", () => Results.Ok(new { icao = "EHAM" }));
        app.MapGet("/api/parking", () => Results.Ok(new[]
        {
            new { name = "B15", type = 9, hasJetway = true },
            new { name = "B16", type = 8, hasJetway = false }
        }));

        app.Run("http://localhost:8787");
    }
}
