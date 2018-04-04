using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsPlaying.Models
{
    public class AppSettings
    {
        public MovieApiSettings MovieApi { get; set; }
    }

    public class MovieApiSettings
    {
        public string BaseApiPath { get; set; }

        public string ApiKey { get; set; }
    }
}
