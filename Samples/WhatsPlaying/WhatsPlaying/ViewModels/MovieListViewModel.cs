using WhatsPlaying.Models;
using WhatsPlaying.Utilities;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsPlaying.ViewModels
{
    public class MovieListViewModel : ViewModelBase
    {
        private MovieApiManager _api;
        private string _results = string.Empty;
        private IEnumerable<Movie> _movies;

        public MovieListViewModel()
        {
            _api = MovieApiManager.GetCurrent();

            this.Movies = new List<Movie>();
        }

        public IEnumerable<Movie> Movies
        {
            get
            {
                return _movies;
            }
            set
            {
                _movies = value;
                this.NotifyPropertyChanged();
            }
        }

        public async Task LoadMoviesAsync(string option)
        {
            IEnumerable<Movie> movies;
            var config = await _api.GetImageConfigurationAsync();
            var genres = await _api.GetGenresAsync();

            switch (option.ToLowerInvariant())
            {
                case "movies_now":
                    movies = await this.GetMoviesPlayingNow();
                    break;
                case "movies_month":
                    movies = await this.GetMoviesForCurrentMonth();
                    break;
                case "movies_popular":
                    movies = await this.GetPopularMoviesLastYear();
                    break;
                case "movies_rated":
                    movies = await this.GetHighRatedMoviesLastYear();
                    break;
                default:
                    movies = null;
                    break;
            }

            this.Movies = movies;
        }

        public void SortByTitle()
        {
            _movies = _movies.OrderBy(m => m.Title);
            this.NotifyPropertyChanged("Movies");
        }

        public void SortByReleaseDate()
        {
            _movies = _movies.OrderByDescending(m => m.ReleaseDate);
            this.NotifyPropertyChanged("Movies");
        }

        public async Task<Movie> GetMovieDetails(int movieId)
        {
            var movieDetail = await _api.GetMovieDetails(movieId);

            // Preload poster image.
            var posterImage = await ImageCache.Instance.GetFromCacheAsync(new Uri(movieDetail.PosterPath));
            movieDetail.PosterImage = posterImage;

            return movieDetail;
        }

        private async Task<IEnumerable<Movie>> GetMoviesPlayingNow()
        {
            var movies = await _api.GetMoviesPlayingNowAsync();

            return movies.OrderBy(m => m.ReleaseDate);
        }

        private async Task<IEnumerable<Movie>> GetMoviesForCurrentMonth()
        {
            var today = DateTime.Now;

            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            var movies = await _api.GetMoviesByDateRangeAsync(startOfMonth, endOfMonth);

            return movies;
        }

        private async Task<IEnumerable<Movie>> GetPopularMoviesLastYear()
        {
            var today = DateTime.Now;

            var lastYear = today.Year - 1;

            var movies = await _api.GetPopularMoviesByYearAsync(lastYear);

            return movies.OrderByDescending(m => m.Popularity);
        }

        private async Task<IEnumerable<Movie>> GetHighRatedMoviesLastYear()
        {
            var today = DateTime.Now;

            var lastYear = today.Year - 1;

            var movies = await _api.GetHighRatedMoviesByYearAsync(lastYear);

            return movies.OrderByDescending(m => m.VoteAverage);
        }
    }
}
