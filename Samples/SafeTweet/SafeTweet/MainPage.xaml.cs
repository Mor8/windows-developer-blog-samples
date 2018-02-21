using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace SafeTweet
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ViewModel _vm;
        private MediaCapture _mediaCapture;
        private bool _isPreviewing;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await this.InitializePreviewAsync();

            var settings = ((App)Application.Current).Settings;
            _vm = new ViewModel(settings, _mediaCapture);

            this.DataContext = _vm;
        }

        private async Task InitializePreviewAsync()
        {
            _mediaCapture = new MediaCapture();

            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video
            };

            await _mediaCapture.InitializeAsync(settings);


            var props = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);
            var max = props.Cast<VideoEncodingProperties>().OrderByDescending(p => p.Width).First();

            await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, max);

            try
            {
                this.WebcamPreview.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();

                _isPreviewing = true;
            }
            catch (FileLoadException fle)
            {
                Debug.WriteLine(fle.Message);
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += MediaCapture_CaptureDeviceExclusiveControlStatusChangedAsync;
            }
        }

        private async void MediaCapture_CaptureDeviceExclusiveControlStatusChangedAsync(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                _vm.Output = "The camera preview can't be displayed because another app has exclusive access.";
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !_isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await InitializePreviewAsync();
                });
            }
        }

        private async void OnTweet(object sender, RoutedEventArgs e)
        {
            var canTweet = await _vm.ValidateAsync(async (response) => 
            {
                var message = $"{response.Message}\n\nDo you want to tweet anyway?";
                return await this.ShowConfirmationDialogAsync(message);
            });

            if (!canTweet)
            {
                _vm.Output = "Cancelling tweet for now.";
                return;
            }

            await _vm.SendTweetAsync();
        }

        private async Task<bool> ShowConfirmationDialogAsync(string message)
        {
            var confirmationDialog = new MessageDialog(message, "Please confirm");

            confirmationDialog.Commands.Add(new UICommand("Yes"));
            confirmationDialog.Commands.Add(new UICommand("No"));
            confirmationDialog.DefaultCommandIndex = 1;
            confirmationDialog.CancelCommandIndex = 1;

            var result = await confirmationDialog.ShowAsync();
            return result.Label.Equals("Yes");
        }
    }
}
