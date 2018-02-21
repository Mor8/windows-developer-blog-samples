using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Text.Core;
using Microsoft.ProjectOxford.Text.Sentiment;

namespace SafeTweet
{
    public class CognitiveServices
    {
        private AppSettings _settings;

        public CognitiveServices(AppSettings settings)
        {
            _settings = settings;
        }

        private AppSettings Settings
        {
            get
            {
                return _settings;
            }
        }
        
        public async Task<float> GetSentimentAnalysisAsync(string text)
        {
            var client = new SentimentClient(this.Settings.Text.Key1)
            {
                Url = this.Settings.Text.EndPoint
            };

            var document = new SentimentDocument
            {
                Id = Guid.NewGuid().ToString(),
                Text = text,
                Language = "en"
            };

            var request = new SentimentRequest
            {
                Documents = new List<IDocument> { document }
            };

            var response = await client.GetSentimentAsync(request);

            //  Only one document was sent, therefore only one result should be returned.
            var result = response.Documents.FirstOrDefault();
            if (result == null)
            {
                throw new ApplicationException("Text Analysis Failed.");
            }

            return result.Score;
        }

        public async Task<IEnumerable<Emotion>> GetEmotionAnalysisAsync(Stream image)
        {
            using (var client = new EmotionServiceClient(this.Settings.Emotion.Key1,
                                                         this.Settings.Emotion.EndPoint))
            {
                try
                {
                    var results = await client.RecognizeAsync(image);

                    return results;
                }
                catch (ClientException ce)
                {
                    throw new ApplicationException("Emotion Analysis Failed.", ce);
                }
            }
        }
    }
}
