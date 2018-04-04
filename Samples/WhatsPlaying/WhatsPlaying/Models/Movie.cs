using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;

namespace WhatsPlaying.Models
{
    public class Movie
    {
        public int Id { get; set; }

        public string BackdropPath { get; set; }

        public decimal Budget { get; set; }

        public IEnumerable<Genre> Genres { get; set; }

        public Uri HomePage { get; set; }

        public string Overview { get; set; }

        public decimal Popularity { get; set; }

        public BitmapImage PosterImage { get; set; }

        public string PosterPath { get; set; }

        public DateTime ReleaseDate { get; set; }

        public decimal Revenue { get; set; }

        public int RuntimeInMinutes { get; set; }

        public string Tagline { get; set; }

        public string Title { get; set; }

        public double VoteAverage { get; set; }

        public int VoteCount { get; set; }
       
        public string DeliminatedGenres
        {
            get
            {
                return string.Join(", ", this.Genres.Select(g => g.Name));
            }
        }

        public int FiveStarVotingAverage
        {
            get
            {
                return (int)Math.Round(this.VoteAverage / 2);
            }
        }

        public double VotingAverageAsPercent
        {
            get
            {

                return this.VoteAverage * 10;
            }
        }

        public string RatingCaption
        {
            get
            {
                return $"({this.VoteAverage / 2:0.0})";
            }
        }

        public string FormattedReleaseDate
        {
            get
            {
                return ReleaseDate.ToString("yyyy-MM-dd");
            }
        }

        public string RuntimeFormatted
        {
            get
            {
                return $"{this.RuntimeInMinutes / 60}h {this.RuntimeInMinutes % 60}m";
            }
        }
    }
}
