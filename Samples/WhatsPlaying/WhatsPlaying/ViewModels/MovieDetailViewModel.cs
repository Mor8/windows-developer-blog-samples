using WhatsPlaying.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WhatsPlaying.ViewModels
{
    public class MovieDetailViewModel : ViewModelBase
    {
        private Movie _movie;

        public Movie Movie
        {
            get
            {
                return _movie;
            }
            set
            {
                _movie = value;
                this.NotifyPropertyChanged();
            }
        }
    }
}
