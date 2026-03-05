using Microsoft.EntityFrameworkCore;
using Render.Shared;

namespace Render.Data;

public class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DataSeeder> _logger;

    private static readonly string[] PostContents =
    [
        "Just captured this amazing moment – couldn't be more grateful for days like these. 🌅",
        "Sometimes the best adventures are the unplanned ones. Where will the road take you next?",
        "There's something magical about the golden hour light. This photo basically took itself.",
        "Spending the weekend exploring new places and clearing my head. Highly recommend it.",
        "Every picture tells a story. This one is one of my favorites so far.",
        "Life is too short for bad vibes. Surround yourself with beauty and good people. ✨",
        "Woke up early just to catch this. Totally worth losing the sleep.",
        "The world is full of stunning places – you just have to go look for them.",
        "Sharing a little slice of my day with you all. Hope it brightens yours too! 🌻",
        "No filter needed when the scene is already this breathtaking.",
        "Found this hidden gem on my walk today. Nature never disappoints.",
        "Sometimes you have to stop and appreciate how beautiful the ordinary can be.",
        "Here's to new beginnings and fresh perspectives. Feeling inspired today.",
        "A moment of peace in a busy world. Take a breath and enjoy the small things.",
        "Can't get enough of these views. Every angle tells a different story.",
        "This is my happy place. Where's yours? Drop it in the comments! 💬",
        "Captured this right before the light faded. Timing is everything.",
        "The colors today were absolutely unreal. Nature is the best artist.",
        "Just a reminder to slow down and notice the beauty around you.",
        "Adventures big and small – each one leaves a mark on the soul.",
        "Started the day with this view. It's going to be a good one. ☀️",
        "The best moments are the ones you didn't plan for.",
        "Feeling grateful for every opportunity to create and explore.",
        "This scene stopped me in my tracks. Had to share it with you.",
        "Perspective changes everything. Take a step back and look around.",
        "Moments like this remind me why I love what I do.",
        "Chasing light, chasing life. Never going to stop. 📸",
        "There's a whole world out there waiting to be seen.",
        "Another day, another beautiful scene to be thankful for.",
        "The quieter you become, the more you can hear.",
        "This one has been sitting in my drafts for too long. Finally sharing it!",
        "Sometimes the journey is more beautiful than the destination.",
        "Grateful for moments that take your breath away.",
        "A picture is worth a thousand words – but this one might need more.",
        "Slowing down and soaking it all in. Life is good. 🙌",
        "Not every day is perfect, but there's always something beautiful to find.",
        "The world looks different when you take a moment to really look at it.",
        "Sharing this because it made me smile and I hope it does the same for you.",
        "Some days the sky just decides to put on a show.",
        "Went exploring and found exactly what I needed – peace and quiet.",
        "The best camera is the one you have with you. Today I used mine well.",
        "Everything looks better with the right light.",
        "Took the long way home today. No regrets.",
        "Little moments of joy are what make life worth living.",
        "Wishing everyone a day as lovely as this view. 💙",
        "This is what weekends are for – getting outside and breathing fresh air.",
        "Sometimes simple is the most beautiful thing of all.",
        "Grateful for eyes that notice beauty and a camera to capture it.",
        "Life is better when you look up from your screen every once in a while.",
        "Another memory added to the collection. Keep exploring, keep creating. 🌍"
    ];

    public DataSeeder(AppDbContext context, IHttpClientFactory httpClientFactory, ILogger<DataSeeder> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<int> SeedPostsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.ToListAsync(cancellationToken);
        if (users.Count == 0)
            throw new InvalidOperationException("No users found. Create at least one user before seeding posts.");

        var client = _httpClientFactory.CreateClient("picsum");
        int created = 0;

        for (int i = 0; i < count; i++)
        {
            try
            {
                var imageBytes = await FetchImageAsync(client, i, cancellationToken);
                var user = users[i % users.Count];
                var content = PostContents[i % PostContents.Length];
                var daysAgo = Random.Shared.Next(0, 90);
                var hoursAgo = Random.Shared.Next(0, 24);

                var post = new Post
                {
                    Content = content,
                    Image = imageBytes,
                    LikesCount = Random.Shared.Next(0, 500000),
                    CreatedAt = DateTime.Now.AddDays(-daysAgo).AddHours(-hoursAgo),
                    UserId = user.Id
                };

                _context.Posts.Add(post);
                created++;

                if (created % 10 == 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Seeded {Count}/{Total} posts...", created, count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed post {Index}, skipping.", i);
            }
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeding complete. {Created} posts created.", created);
        return created;
    }

    private static async Task<byte[]> FetchImageAsync(HttpClient client, int seed, CancellationToken cancellationToken)
    {
        // picsum.photos returns a deterministic random image for a given seed
        var response = await client.GetAsync($"seed/{seed}/400/400", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
