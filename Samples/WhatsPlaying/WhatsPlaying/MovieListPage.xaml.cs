using WhatsPlaying.Models;
using WhatsPlaying.Utilities;
using WhatsPlaying.ViewModels;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WhatsPlaying
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MovieListPage : Page
    {
        private MovieListViewModel _vm;
        private static int persistedItemIndex;

        public MovieListPage()
        {
            this.InitializeComponent();

            _vm = new MovieListViewModel();

            this.DataContext = _vm;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var tag = e.Parameter as string;
            await _vm.LoadMoviesAsync(tag);

            // Only worry about the connection animation when navigating back to this page.
            if (e.NavigationMode == NavigationMode.Back)
            {
                MovieGridView.Loaded += async (obj, args) =>
                {
                    var transitions = MovieGridView.ItemContainerTransitions.ToList();
                    MovieGridView.ItemContainerTransitions.Clear();

                    var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("fromDetailPoster");
                    if (animation != null)
                    {
                        var item = MovieGridView.Items[persistedItemIndex];
                        MovieGridView.ScrollIntoView(item);

                        // Small delay to give the view time to scroll into position before animating.
                        await Task.Delay(100);

                        await MovieGridView.TryStartConnectedAnimationAsync(animation, item, "PosterImage");
                    }

                    transitions.ForEach(t => MovieGridView.ItemContainerTransitions.Add(t));
                };
            }
        }

        private async void MovieGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var movie = e.ClickedItem as Movie;
            if (movie == null)
            {
                return;
            }

            persistedItemIndex = MovieGridView.Items.IndexOf(e.ClickedItem);

            var movieDetail = await _vm.GetMovieDetails(movie.Id);

            // Prepare connected animation to tie both poster images together.
            MovieGridView.PrepareConnectedAnimation("poster", movie, "PosterImage");

            // Finally navigate to detail page and suppress default navigation transitions.
            Frame.Navigate(typeof(MovieDetailPage), movieDetail, new SuppressNavigationTransitionInfo());
        }
    }
}
