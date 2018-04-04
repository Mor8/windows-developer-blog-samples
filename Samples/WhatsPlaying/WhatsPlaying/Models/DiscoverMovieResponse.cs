using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsPlaying.Models
{
    public class DiscoverMovieResponse : MovieResponseBase
    {
        [JsonProperty("genre_ids")]
        public int[] GenresIds { get; set; }
    }
}
