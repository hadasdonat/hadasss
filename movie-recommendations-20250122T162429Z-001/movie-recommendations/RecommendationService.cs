using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

public class GenreRequest
{
    public List<string> Genres { get; set; }
}

// Define the Movie class
public class Movie
{
    public string Title { get; set; } = string.Empty;
    public int MovieId { get; set; }  // Changed to MovieId to match TMDB's format
    public double Popularity { get; set; } = 0.0;  // For rating, using 'popularity'
    public List<int> Genres { get; set; } = new List<int>();  // Empty list of integers
}

// TMDB Genre Response class
public class TmdbGenreResponse
{
    public List<Genre> Genres { get; set; }
}

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// TMDB Movie Response class
public class TmdbMovieResponse
{
    public List<TmdbMovie> Results { get; set; } = new List<TmdbMovie>();
}

public class TmdbMovie
{
    public string Title { get; set; } = string.Empty;  // Initialize to avoid null reference
    public int Id { get; set; }
    public double Popularity { get; set; }
    public List<int> Genres { get; set; } = new List<int>();  // Empty list of integers
}

// Movie Details class for fetching IMDb ID
public class MovieDetails
{
    public string ImdbId { get; set; }
}
public class EmotionAnalysisResult
{
    public Dictionary<string, double> Emotions { get; set; }
    public string Gender { get; set; }
}


// Mock RecommendationService
public class RecommendationService
{
    private const string TMDB_API_KEY = "b211488ad89ea4836cbd7e78a56aa76f";
    private const string TMDB_BASE_URL = "https://api.themoviedb.org/3";
    private List<string> savedGenres = new();

    public Task SaveGenres(List<string> genres)
    {
        savedGenres = genres ?? new List<string>(); // שמירת ז'אנרים או רשימה ריקה
        Console.WriteLine("Genres saved: " + string.Join(", ", savedGenres));
        return Task.CompletedTask;
    }


    public List<string> GetSavedGenres()
    {
        return savedGenres ?? new List<string>(); // החזרת הז'אנרים או רשימה ריקה
    }


    public async Task<object> GetRecommendedMovies(string imagePath, List<string> genres = null)
    {
        // אם אין ז'אנרים, המשך רק לפי רגשות
        genres ??= new List<string>();

        // Analyze emotions from the image using Python script
        var analysisResults = await AnalyzeEmotions(imagePath);
        if (analysisResults == null || !analysisResults.Emotions.Any())
        {
            return new { message = "Failed to analyze emotions from the image." };
        }

        // המלצות לפי רגשות בלבד אם אין ז'אנרים
        if (genres.Any())
        {
            return await GetRecommendationWithGenres(analysisResults.Emotions, analysisResults.Gender, genres);
        }
        else
        {
            Console.WriteLine("No genres selected. Recommending movies based on emotions only.");
            return await GetRecommendationWithoutGenres(analysisResults.Emotions, analysisResults.Gender);
        }
    }

    //gets dictionary of emotions and amount from image
     public async Task<EmotionAnalysisResult> AnalyzeEmotions(string imagePath)
{
    try
    {
        var pythonScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "emotion_detection.py");

        Console.WriteLine($"{pythonScript} {imagePath}");
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"{pythonScript} {imagePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string errorOutput = await process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(errorOutput))
        {
            Console.WriteLine($"Python Error: {errorOutput}");
        }

        // Assuming the output is properly formatted JSON
        string jsonOutput = output.Trim();
        Console.WriteLine($"Python Output: {jsonOutput}");

        var analysisResult = new EmotionAnalysisResult();

        // Deserialize Emotions
        try
        {
            var emotions = JsonDocument.Parse(jsonOutput).RootElement.GetProperty("emotions");
            analysisResult.Emotions = JsonSerializer.Deserialize<Dictionary<string, double>>(emotions.GetRawText());
            Console.WriteLine("Emotions successfully deserialized:");
            foreach (var emotion in analysisResult.Emotions)
            {
                Console.WriteLine($"{emotion.Key}: {emotion.Value}%");
            }
        }
        catch (Exception emotionEx)
        {
            Console.WriteLine($"Error deserializing emotions: {emotionEx.Message}");
            analysisResult.Emotions = new Dictionary<string, double>();
        }

        // Deserialize Gender
        try
        {
            var gender = JsonDocument.Parse(jsonOutput).RootElement.GetProperty("gender").GetString();
            analysisResult.Gender = gender;
            Console.WriteLine($"Gender successfully deserialized: {analysisResult.Gender}");
        }
        catch (Exception genderEx)
        {
            Console.WriteLine($"Error deserializing gender: {genderEx.Message}");
            analysisResult.Gender = "Unknown";
        }

        return analysisResult;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error analyzing emotions: {ex.Message}");
        return null;
    }
}


    private List<int> MapGenresToIds(List<string> genres)
    {
        var genreMap = new Dictionary<string, int>
        {
            { "Action", 28 },
            { "Drama", 18 },
            { "Comedy", 35 },
            { "Horror", 27 },
            { "Thriller", 53 },
            { "Romance", 10749 },
            { "Fantasy", 14 },
            { "Biography", 99 }
        };

        return genres.Select(genre => genreMap.GetValueOrDefault(genre, 0))
                     .Where(id => id > 0)
                     .ToList();
    }

    private List<int> GetGenresFromEmotions(Dictionary<string, double> emotions)
    {
        // מיפוי רגשות לז'אנרים
        var emotionToGenre = new Dictionary<string, int>
        {
            { "happy", 35 },   // Comedy
            { "sad", 18 },     // Drama
            { "angry", 28 },   // Action
            { "neutral", 14 }, // Fantasy
                { "surprise", 27 },// Horror
            { "fear", 53 },    // Thriller
            { "love", 10749 }, // Romance
            { "pride", 99 }    // Biography
        };

        // הפקת ז'אנרים על בסיס שלושת הרגשות המובילים
        return emotions.OrderByDescending(e => e.Value)
                       .Take(3)
                       .Select(e => emotionToGenre.GetValueOrDefault(e.Key))
                       .Where(id => id > 0)
                       .Distinct()
                       .ToList();
    }

    private async Task<object> GetRecommendationWithoutGenres(Dictionary<string, double> emotions, string gender)
    {
        var genresFromEmotions = GetGenresFromEmotions(emotions);//get the matching genres for the analyzed emotions

        if (!genresFromEmotions.Any())
        {
            return new { message = "No genres found for the given emotions." };
        }

        var movies = await FetchMovieForGenres(genresFromEmotions);//get the movies based on genres from image emotions
        if (movies == null)
        {
            return new { message = "No movies found." };
        }

        var finalMovies = FilterAndPrioritizeMovies(movies, gender, genresFromEmotions);
        return finalMovies;

    }

    private async Task<object> GetRecommendationWithGenres(Dictionary<string, double> emotions, string gender, List<string> selectedGenres)
    {
        var genresFromEmotions = GetGenresFromEmotions(emotions);

        // מיפוי שמות הז'אנרים ל-IDs
        var manualGenreIds = MapGenresToIds(selectedGenres);

        // שילוב ז'אנרים מהרגשות ומהבחירה הידנית
        var combinedGenreIds = genresFromEmotions.Concat(manualGenreIds).Distinct().ToList();

        if (!combinedGenreIds.Any())
        {
            return new { message = "No genres found for the given emotions and manual selection." };
        }

        var movies = await FetchMovieForGenres(combinedGenreIds);
        if (movies == null)
        {
            return new { message = "No movies found." };
        }

        var finalMovies = FilterAndPrioritizeMovies(movies, gender, genresFromEmotions);
        return finalMovies;

    }

    private async Task<List<Movie>> FetchMovieForGenres(List<int> genres)
    {
        using var client = new HttpClient();
        var genreIds = string.Join(",", genres);
        var response = await client.GetAsync($"{TMDB_BASE_URL}/discover/movie?api_key={TMDB_API_KEY}&with_genres={genreIds}");

        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync();
        // Deserialize the response into a dictionary
        var movieData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

        if (movieData == null || !movieData.ContainsKey("results"))
        {
            Console.WriteLine("Debug: No movie data or results found in the API response.");
            return new List<Movie>();
        }

        var results = movieData["results"].EnumerateArray();

        var movies = new List<Movie>();

        foreach (var movieResponse in results)
        {
            // Manually deserialize each movie
            var tmdbMovie = new TmdbMovie
            {
                Title = movieResponse.GetProperty("title").GetString(),  // Manually get the title
                Id = movieResponse.GetProperty("id").GetInt32(),         // Manually get the movie ID
                Popularity = movieResponse.GetProperty("popularity").GetDouble(),  // Manually get popularity
                Genres = movieResponse.GetProperty("genre_ids").EnumerateArray()
                                      .Select(genre => genre.GetInt32())  // Manually parse genre IDs
                                      .ToList()  // Convert to list of integers
            };

            // Debug: Print the details of the deserialized movie
            Console.WriteLine($"Debug: Movie Title: {tmdbMovie.Title}");
            Console.WriteLine($"       Movie ID: {tmdbMovie.Id}");
            Console.WriteLine($"       Popularity (Rating): {tmdbMovie.Popularity}");
            Console.WriteLine($"       Genres: {string.Join(", ", tmdbMovie.Genres)}");  // Print the genre IDs

            movies.Add(new Movie
            {
                Title = tmdbMovie.Title,
                MovieId = tmdbMovie.Id,  // Movie ID from API
                Popularity = tmdbMovie.Popularity,  // Popularity as rating
                Genres = tmdbMovie.Genres  // Handle genre list
            });
        }

        return movies ?? new List<Movie>();
    }

    // New function to filter and prioritize movies based on gender and emotion analysis
    public object FilterAndPrioritizeMovies(List<Movie> movies, string gender, List<int> combinedGenres)
    {
        // Determine gender preference
        var genderPreference = gender.ToLower() == "Man" ? "action" : "romance";
        var genderGenreId = genderPreference == "action" ? 28 : 10749;

        Console.WriteLine($"Debug: Gender Preference - {genderPreference} (Genre ID: {genderGenreId})");

        // Prioritize movies based on scoring
        var prioritizedMovies = movies.Select(movie =>
        {
            // Check if the movie's genres include the preferred gender genre
            bool matchesGenderPreference = movie.Genres.Contains(genderGenreId);

            // Calculate gender score (higher for matching the gender-preferred genre)
            int genderScore = matchesGenderPreference ? 10 : 0;

            // Calculate genre score based on the user's selected genres (topGeneres)
            int genreScore = combinedGenres
                             .Where(id => movie.Genres.Contains(id))
                             .Sum(id => 5); // Add 5 points for each matching genre

            // Aggregate the final score: combine movie popularity, gender score, and genre score
            double finalScore = movie.Popularity + genreScore + genderScore;

            // Debugging: Print details of the movie and its score breakdown
            Console.WriteLine($"Debug: Movie - {movie.Title}, Popularity: {movie.Popularity}, GenderScore: {genderScore}, GenreScore: {genreScore}, FinalScore: {finalScore}");

            return new
            {
                movie.Title,
                movie.MovieId,
                movie.Popularity,
                movie.Genres,
                FinalScore = finalScore
            };
        });
        // Debug: Print all prioritized movies
        Console.WriteLine("Debug: Prioritized Movies:");
        foreach (var movie in prioritizedMovies)
        {
            Console.WriteLine($"       {movie.Title} - FinalScore: {movie.FinalScore}");
        }
        //choose the top 2 movies from the reccomended based on score
        var topMovies = prioritizedMovies.OrderByDescending(m => m.FinalScore)
                                  .Take(2)
                                  .Select(m => new
                                  {
                                      message = "Movies recommended successfully.",
                                      title = m.Title,
                                      link = $"https://www.themoviedb.org/movie/{m.MovieId}-{m.Title.ToLower().Replace(" ", "-")}"
                                  })
                                  .ToList();
        Console.WriteLine($"Debug: Top Movies - {string.Join(", ", topMovies.Select(t => t.title))}");

        // If no movies are found, return an appropriate message
        if (!topMovies.Any())
        {
            return new { message = "No suitable movies found based on the analysis." };
        }

        // Return the top 2 movies
        Console.WriteLine("Debug: Returning top movies.");
        return topMovies;

    }



}
