using WhatsPlaying.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;

namespace WhatsPlaying.Utilities
{
    public class MovieApiManager
    {
        private const string DiscoverApi = "/discover/movie?";
        private const string MovieApi = "/movie/{0}?";
        private const string ConfigurationApi = "/configuration?";
        private const string GenreApi = "/genre/list?";

        private const string ApiKeyQueryString = "api_key={0}";
        private const string DateRangeQueryString = "primary_release_date.gte={0}&primary_release_date.lte={1}&sort_by=popularity.desc";
        private const string PopularMoviesQueryString = "primary_release_year={0}&vote_count.gte=250&sort_by=popularity.desc&page=1";
        private const string HighRatedMoviesQueryString = "primary_release_year={0}&vote_count.gte=250&sort_by=vote_average.desc&page=1";
        private const string DateFormat = "yyyy-MM-dd";

        private IDictionary<string, string> _cache = new Dictionary<string, string>();
        private AppSettings _settings;

        private static readonly MovieApiManager _instance = new MovieApiManager();

        private MovieApiManager()
        {
            _settings = ((App)App.Current).Settings;
        }

        public static MovieApiManager GetCurrent()
        {
            return _instance;
        }

        private string BaseApi
        {
            get
            {
                return _settings.MovieApi.BaseApiPath;
            }
        }

        private string ApiKey
        {
            get
            {
                return _settings.MovieApi.ApiKey;
            }
        }

        public async Task<ImageConfiguration> GetImageConfigurationAsync()
        {
            const string key = "image-config";

            if (_cache.ContainsKey(key))
            {
                var json = _cache[key];
                return JsonConvert.DeserializeObject<ImageConfiguration>(json);
            }

            var request = $"{BaseApi}{ConfigurationApi}{string.Format(ApiKeyQueryString, ApiKey)}";
            var result = await this.CallWebService(request);

            var data = this.ParseImageConfiguration(result);

            _cache.Add(key, JsonConvert.SerializeObject(data));
            return data;
        }

        public async Task<IEnumerable<Genre>> GetGenresAsync()
        {
            const string key = "genres";
            if (_cache.ContainsKey(key))
            {
                var json = _cache[key];
                return JsonConvert.DeserializeObject<Genre[]>(json);
            }

            var request = $"{BaseApi}{GenreApi}{string.Format(ApiKeyQueryString, ApiKey)}";
            var result = await this.CallWebService(request);            
            var data = this.ParseGenres(result);

            _cache.Add(key, JsonConvert.SerializeObject(data));
            return data;
        }

        public async Task<IEnumerable<Movie>> GetMoviesPlayingNowAsync()
        {
            var key = "current";
            var request = $"{BaseApi}{string.Format(MovieApi, "now_playing")}&{string.Format(ApiKeyQueryString, ApiKey)}";

            return await this.ProcessRequestAsync(key, request);
        }

        public async Task<IEnumerable<Movie>> GetMoviesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var key = "movies-" + startDate.GetHashCode().ToString() + endDate.GetHashCode().ToString();
            var request = $"{BaseApi}{DiscoverApi}{string.Format(ApiKeyQueryString, ApiKey)}&{string.Format(DateRangeQueryString, startDate.ToString(DateFormat), endDate.ToString(DateFormat))}";

            return await this.ProcessRequestAsync(key, request);
        }

        public async Task<IEnumerable<Movie>> GetPopularMoviesByYearAsync(int year)
        {
            var key = "popular-" + year.ToString();
            var request = $"{BaseApi}{DiscoverApi}{string.Format(ApiKeyQueryString, ApiKey)}&{string.Format(PopularMoviesQueryString, year)}";

            return await this.ProcessRequestAsync(key, request);
        }

        public async Task<IEnumerable<Movie>> GetHighRatedMoviesByYearAsync(int year)
        {
            var key = "rated-" + year.ToString();
            var request = $"{BaseApi}{DiscoverApi}{string.Format(ApiKeyQueryString, ApiKey)}&{string.Format(HighRatedMoviesQueryString, year)}";

            return await this.ProcessRequestAsync(key, request);
        }

        public async Task<Movie> GetMovieDetails(int id)
        {
            var config = await this.GetImageConfigurationAsync();
            var genres = await this.GetGenresAsync();

            var key = $"details-{id}";
            if (_cache.ContainsKey(key))
            {
                var json = _cache[key];
                var cached = JsonConvert.DeserializeObject<MovieDetailResponse>(json);

                return this.Map(cached, config.SecureBaseUrl, genres);
            }

            var request = $"{BaseApi}{string.Format(MovieApi, id)}{string.Format(ApiKeyQueryString, ApiKey)}";
            var result = await this.CallWebService(request);
            var data = JsonConvert.DeserializeObject<MovieDetailResponse>(result);
           
            _cache.Add(key, result);

            return this.Map(data, config.SecureBaseUrl, genres);
        }

        private async Task<IEnumerable<Movie>> ProcessRequestAsync(string cacheKey, string request)
        {
            var config = await this.GetImageConfigurationAsync();
            var genres = await this.GetGenresAsync();

            if (_cache.ContainsKey(cacheKey))
            {
                var json = _cache[cacheKey];
                var cached = JsonConvert.DeserializeObject<DiscoverMovieResponse[]>(json);

                return this.Map(cached, config.SecureBaseUrl, genres);
            }

            var result = await this.CallWebService(request);

            var data = this.ParseMovies(result);

            _cache.TryAdd(cacheKey, JsonConvert.SerializeObject(data));

            return this.Map(data, config.SecureBaseUrl, genres);
        }

        private async Task<string> CallWebService(string request)
        {
            using (var client = new HttpClient())
            {
                var requestUri = new Uri(request);

                return await client.GetStringAsync(requestUri);
            }
        }

        private IEnumerable<Movie> Map(IEnumerable<DiscoverMovieResponse> source, string imageBasePath, IEnumerable<Genre> genres)
        {
            if (source == null)
            {
                return null;
            }

            const string posterImageSize = "w154";

            var mapped = source.Select(m =>
            {
                return new Movie
                {
                    Id = m.Id,
                    Popularity = m.Popularity,
                    PosterPath = $"{imageBasePath}{posterImageSize}{m.PosterPath}",
                    ReleaseDate = m.ReleaseDate,
                    Title = m.Title,
                    VoteAverage = m.VoteAverage,
                    VoteCount = m.VoteCount,
                    Genres = genres.Where(g => m.GenresIds.Contains(g.Id))
                };
            });

            return mapped;
        }

        private Movie Map(MovieDetailResponse source, string imageBasePath, IEnumerable<Genre> genres)
        {
            if (source == null)
            {
                return null;
            }

            const string posterImageSize = "w342";
            const string backdropImageSize = "w1280";

            var mapped = new Movie
                {
                    Id = source.Id,
                    BackdropPath = $"{imageBasePath}{backdropImageSize}{source.BackdropPath}",
                    Budget = source.Budget,
                    Genres = source.Genres,
                    HomePage = source.HomePage,
                    Overview = source.Overview,
                    Popularity = source.Popularity,
                    PosterPath = $"{imageBasePath}{posterImageSize}{source.PosterPath}",
                    ReleaseDate = source.ReleaseDate,
                    Revenue = source.Revenue,
                    RuntimeInMinutes = source.RuntimeInMinutes,
                    Tagline = source.Tagline,
                    Title = source.Title,
                    VoteAverage = source.VoteAverage,
                    VoteCount = source.VoteCount
                };

            return mapped;
        }

        private List<DiscoverMovieResponse> ParseMovies(string json)
        {
            var j = JObject.Parse(json);
            var movies = j["results"];
            var results = new List<DiscoverMovieResponse>();
            foreach(var m in movies)
            {
                var movie = m.ToObject<DiscoverMovieResponse>();
                results.Add(movie);
            }

            return results.OrderBy(m => m.ReleaseDate).ToList();
        }

        private ImageConfiguration ParseImageConfiguration(string json)
        {
            var j = JObject.Parse(json);
            var images = j["images"];

            var config = images.ToObject<ImageConfiguration>();
            return config;
        }

        private IEnumerable<Genre> ParseGenres(string json)
        {
            var j = JObject.Parse(json);
            var genres = j["genres"];

            return  genres.ToObject<Genre[]>();
        }
    }
}
