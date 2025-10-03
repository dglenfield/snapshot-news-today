namespace NewsScraper.Models.PerplexityApi.Requests;

/// <summary>
/// User location to refine search results based on geography. For best accuracy, it is recommended to 
/// provide as many fields as possible including city and region.
/// </summary>
internal class UserLocation
{
    /// <summary>
    /// The latitude of the user's location.
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// The longitude of the user's location.
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// The two letter ISO country code of the user's location.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// The region/state/province of the user's location (e.g., 'California', 'Ontario', 'Île-de-France').
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// The city name of the user's location (e.g., 'San Francisco', 'New York City', 'Paris').
    /// </summary>
    public string? City { get; set; }
}
