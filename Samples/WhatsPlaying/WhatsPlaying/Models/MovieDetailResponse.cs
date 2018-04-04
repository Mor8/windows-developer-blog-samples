using Newtonsoft.Json;
using System;
using Windows.UI.Xaml.Media.Imaging;

namespace WhatsPlaying.Models
{
    public class MovieDetailResponse : MovieResponseBase
    {
        [JsonProperty("backdrop_path")]
        public string BackdropPath { get; set; }

        [JsonProperty("budget")]
        public decimal Budget { get; set; }

        [JsonProperty("genres")]
        public Genre[] Genres { get; set; }

        [JsonProperty("homepage")]
        public Uri HomePage { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("revenue")]
        public decimal Revenue { get; set; }

        [JsonProperty("runtime")]
        public int RuntimeInMinutes { get; set; }

        [JsonProperty("tagline")]
        public string Tagline { get; set; }
    }
}
