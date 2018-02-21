using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Text.Core;
using Microsoft.ProjectOxford.Text.Sentiment;
using Microsoft.Toolkit.Uwp.Services.Twitter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace SafeTweet
{
    public class ViewModel : INotifyPropertyChanged
    {
        private const string PositiveEmoji = "\U0001F600";
        private const string NegativeEmoji = "\U0001F620";
        private const string NegativeSentimentMessage = "Your tweet sounds too negative. Consider revising to a more neutral or positive tone before sending.";
        private const string AngryEmotionMessage = "You seem a bit angry. Perhaps you should wait for the steam to clear before sending.";
        private const string SadEmotionMessage = "You shouldn't tweet when you're sad. Maybe wait until you feel better before sending.";

        private readonly CognitiveServices _cognitiveService;
        private readonly StringBuilder _output;

        private readonly Dictionary<string, string> NegativeEmotions 
            = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase) { { "anger", AngryEmotionMessage }, { "sadness", SadEmotionMessage } };

        private readonly MediaCapture _mediaCapture;

        private string _tweet;

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel(AppSettings settings, MediaCapture mediaCapture)
        {
            _cognitiveService = new CognitiveServices(settings);
            _mediaCapture = mediaCapture;
            _output = new StringBuilder();

            this.Settings = settings;
        }

        public string Output
        {
            get { return _output.ToString(); }
            set
            {
                _output.AppendLine(value);
                this.NotifyPropertyChanged();
            }
        }

        public string Tweet
        {
            get { return _tweet; }
            set
            {
                _tweet = value;
                this.NotifyPropertyChanged();
            }
        }

        private AppSettings Settings { get; set; }

        public void ClearMessages()
        {
            _output.Clear();
            this.NotifyPropertyChanged("State");
        }

        public void CancelTweet()
        {
            this.Output = "Smart move, cancelling tweet for now.";
        }

        public async Task<bool> ValidateAsync(Func<ValidationResponse, Task<bool>> getConfirmation)
        {
            this.ClearMessages();
            var snapshot = await this.TakeSnapshotAsync();
            this.Output = "Validating tweet...";

            var result = await this.CheckSentimentAsync();
            var confirm = await this.ProcessValidationResponseAsync(result, getConfirmation);
            if (!confirm)
            {
                return false;
            }

            result = await this.CheckEmotionAsync(snapshot);
            confirm = await this.ProcessValidationResponseAsync(result, getConfirmation);
            if (!confirm)
            {
                return false;
            }
    
            return true;
        }

        private async Task<bool> ProcessValidationResponseAsync(ValidationResponse response, Func<ValidationResponse, Task<bool>> getConfirmation)
        {
            if (response.HasError)
            {
                this.Output = response.Message;
                return await getConfirmation(response);
            }

            return true;
        }

        public async Task<bool> SendTweetAsync()
        {
            this.Output = "Tweeting...";

            TwitterService.Instance.Initialize(this.Settings.Twitter.ConsumerKey,
                                               this.Settings.Twitter.ConsumerSecret,
                                               this.Settings.Twitter.CallbackUrl);

            var loginSuccessful = await TwitterService.Instance.LoginAsync();
            if (!loginSuccessful)
            {
                this.Output = "Could not login to Twitter.";
                return false;
            }

            var success = await TwitterService.Instance.TweetStatusAsync(this.Tweet);
            this.Output = success ? "Tweet sent." : "Tweet failed.";

            return success;
        }

        private async Task<ValidationResponse> CheckSentimentAsync()
        {
            this.Output = "Evaluating tweet sentiment...";

            var sentiment = await _cognitiveService.GetSentimentAnalysisAsync(this.Tweet);
            var isOK = sentiment >= this.Settings.Text.NegativeSentimentThreshold;

            this.Output = $"Your text sentiment score is {sentiment:P2}! {(isOK ? PositiveEmoji : NegativeEmoji)}";

            var result = new ValidationResponse
            {
                HasError = !isOK,
                Message = isOK ? string.Empty : NegativeSentimentMessage
            };

            return result;
        }

        public async Task<ValidationResponse> CheckEmotionAsync(Stream image)
        {
            this.Output = "Evaluating sender's emotional state...";

            var emotion = await _cognitiveService.GetEmotionAnalysisAsync(image);
            var face = emotion.FirstOrDefault();

            if (face == null)
            {
                return new ValidationResponse
                {
                    HasError = true,
                    Message = "Couldn't detect a face or there was another issue with the Emotion API."
                };
            }

            var primary = face.Scores.ToRankedList().First();
            this.Output = $"Emotional state is {primary.Value:P2} {primary.Key}.";

            var isOK = !NegativeEmotions.ContainsKey(primary.Key);

            var result = new ValidationResponse
            {
                HasError = !isOK,
                Message = isOK ? string.Empty : NegativeEmotions[primary.Key]
            };

            return result;
        }

        public async Task<Stream> TakeSnapshotAsync()
        {
            var encoding = ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8);
            var capture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(encoding);
            var photo = await capture.CaptureAsync();
            var frame = photo.Frame;

            await capture.FinishAsync();

            return await this.WriteToStreamAsync(frame);
        }

        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task<Stream> WriteToStreamAsync(CapturedFrame frame)
        {
            using (var outputStream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputStream);
                encoder.IsThumbnailGenerated = false;
                encoder.SetSoftwareBitmap(frame.SoftwareBitmap);

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                await outputStream.FlushAsync();

                var ms = new MemoryStream();
                await outputStream.AsStream().CopyToAsync(ms);
                ms.Position = 0;

                return ms;
            }
        }
    }
}
