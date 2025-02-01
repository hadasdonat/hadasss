using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// רישום Singleton של RecommendationService
builder.Services.AddSingleton<RecommendationService>();

var app = builder.Build();

// מוודא שהשרת מגיש קבצים סטטיים מה-wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// מגדיר את index.html כברירת מחדל
app.MapFallbackToFile("index.html");

// Endpoint לעיבוד ז'אנרים
app.MapPost("/movies/genres", async ([FromServices] RecommendationService recommender, [FromBody] GenreRequest genreRequest) =>
{
    if (genreRequest == null || genreRequest.Genres == null)
    {
        return Results.BadRequest("Genres data is required.");
    }

    Console.WriteLine("Debug: Genres received in /movies/genres: " + string.Join(", ", genreRequest.Genres));

    if (!genreRequest.Genres.Any())
    {
        Console.WriteLine("Debug: No genres selected. Proceeding without genres.");
    }
    else if (genreRequest.Genres.Count > 5)
    {
        Console.WriteLine("Debug: More than 5 genres selected. Returning BadRequest.");
        return Results.BadRequest("You can select up to 5 genres only.");
    }

    await recommender.SaveGenres(genreRequest.Genres);
    Console.WriteLine("Debug: Genres saved successfully.");

    return Results.Ok(new { Message = "Genres processed successfully." });
});

// Endpoint לקבלת המלצות סרטים
app.MapPost("/movies/recommendations", async ([FromServices] RecommendationService recommender, [FromForm] IFormFile image) =>
{
    if (image == null || image.Length == 0)
    {
        Console.WriteLine("❌ Debug: No image uploaded.");
        return Results.BadRequest("Image is required.");
    }

    try
    {
        var filePath = Path.GetTempFileName();
        Console.WriteLine($"📡 Debug: Image saved temporarily at {filePath}");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        var genres = recommender.GetSavedGenres();
        Console.WriteLine($"📌 Debug: Genres used - {string.Join(", ", genres)}");

        if (genres == null || !genres.Any())
        {
            Console.WriteLine("⚠ Debug: No genres provided, proceeding with emotion-based recommendations only.");
        }

        var recommendedMovies = await recommender.GetRecommendedMovies(filePath, genres ?? new List<string>());

        File.Delete(filePath);
        Console.WriteLine($"✅ Debug: Temporary image file deleted.");
        Console.WriteLine($"📡 Debug: Recommended movies - {JsonSerializer.Serialize(recommendedMovies)}");

        return Results.Ok(recommendedMovies);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Debug: Exception occurred - {ex.Message}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/movies/emotions", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var image = form.Files["image"];

    if (image == null || image.Length == 0)
    {
        Console.WriteLine("Debug: No image uploaded.");
        return Results.BadRequest("Image is required.");
    }

    try
    {
        var filePath = Path.GetTempFileName();
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        var recommender = new RecommendationService();
        var analysisResult = await recommender.AnalyzeEmotions(filePath);

        File.Delete(filePath);
        Console.WriteLine("Debug: Temporary image file deleted.");

        return Results.Ok(new { emotions = analysisResult });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Debug: Exception occurred - {ex.Message}");
        return Results.Problem(ex.Message);
    }
});


app.Run();
