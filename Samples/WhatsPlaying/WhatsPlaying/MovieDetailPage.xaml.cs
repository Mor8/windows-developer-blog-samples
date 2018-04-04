using WhatsPlaying.Models;
using WhatsPlaying.Utilities;
using WhatsPlaying.ViewModels;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WhatsPlaying
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MovieDetailPage : Page
    {
        private MovieDetailViewModel _vm;

        public MovieDetailPage()
        {
            this.InitializeComponent();
            this.InitializeBackgroundEffect(this.BackgroundHost);

            _vm = new MovieDetailViewModel();

            this.DataContext = _vm;
        }

        private void InitializeBackgroundEffect(UIElement host)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(host);
            var compositor = hostVisual.Compositor;

            var effect = new GaussianBlurEffect
            {
                BlurAmount = 3.0f,                
                BorderMode = EffectBorderMode.Hard,
                Source = new ArithmeticCompositeEffect
                {
                    MultiplyAmount = 0,
                    Source1Amount = 0.5f,
                    Source2Amount = 0.5f,
                    Source1 = new CompositionEffectSourceParameter("backdropBrush"),
                    Source2 = new ColorSourceEffect
                    {
                        Color = Color.FromArgb(255, 245, 245, 245)
                    }
                }
            };

            var effectFactory = compositor.CreateEffectFactory(effect);
            var backdropBrush = compositor.CreateBackdropBrush();
            var effectBrush = effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("backdropBrush", backdropBrush);
            
            var effectVisual = compositor.CreateSpriteVisual();
            effectVisual.Brush = effectBrush;
            
            ElementCompositionPreview.SetElementChildVisual(host, effectVisual);

            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);

            effectVisual.StartAnimation("Size", bindSizeAnimation);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var movie = e.Parameter as Movie;
            _vm.Movie = movie;

            // Start the connected animation prepared in the list view.
            var connectedAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("poster");
            if (connectedAnimation != null)
            {
                connectedAnimation.TryStart(this.PosterImage);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Prepare the connected animation before returning to the list view.
            ConnectedAnimationService.
                GetForCurrentView().
                PrepareToAnimate("fromDetailPoster", this.PosterImage);
        }
    }
}
