using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTweet
{
    public class AppSettings
    {
        public TextSettings Text { get; set; }

        public CognitiveSettings Emotion { get; set; }

        public TwitterSettings Twitter { get; set; }
    }

    public class TextSettings : CognitiveSettings
    {
        public float NegativeSentimentThreshold { get; set; }
    }

    public class CognitiveSettings 
    {
        public string EndPoint { get; set; }

        public string Key1 { get; set; }

        public string Key2 { get; set; }
    }

    public class TwitterSettings
    {
        public string ConsumerKey { get; set; }

        public string ConsumerSecret { get; set; }

        public string CallbackUrl { get; set; }
    }
}
