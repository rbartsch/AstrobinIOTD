using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Tweetinvi;
using Newtonsoft.Json.Linq;
using Tweetinvi.Parameters;
using Tweetinvi.Models;

namespace AstrobinIOTD {
    class Program {
        static readonly string astrobinApiKey = "your_key_here";
        static readonly string astrobinApiSecret = "your_key_here";

        static readonly string twitterConsumerKey = "your_key_here";
        static readonly string twitterConsumerSecret = "your_key_here";
        static readonly string twitterAccessToken = "your_key_here";
        static readonly string twitterAccessTokenSecret = "your_key_here";

        static readonly string lastIotdIdPath = "last_iotd_id.txt";

        static void Main(string[] args) {
            Uri iotdUri = new Uri("https://www.astrobin.com/api/v1/imageoftheday/?limit=1&api_key=" + astrobinApiKey + "&api_secret=" + astrobinApiSecret + "&format=json");
            ImageOfTheDay iotd = null;
            Image image = null;
            byte[] rawImage = null;

            try {
                using (WebClient wc = new WebClient()) {
                    Console.WriteLine("Fetching JSON");

                    JObject jsonIotd = JObject.Parse(wc.DownloadString(iotdUri));
                    IList<JToken> iotdResult = jsonIotd["objects"].Children().ToList();
                    iotd = iotdResult[0].ToObject<ImageOfTheDay>();

                    Uri imageUri = new Uri("https://www.astrobin.com" + iotd.Image + "?&api_key=" + astrobinApiKey + "&api_secret=" + astrobinApiSecret + "&format=json");
                    JObject jsonImage = JObject.Parse(wc.DownloadString(imageUri));
                    image = jsonImage.ToObject<Image>();

                    if (!File.Exists(lastIotdIdPath)) {
                        File.Create(lastIotdIdPath).Close();
                    }
                    string lastIotdId = File.ReadAllText(lastIotdIdPath);
                    if(lastIotdId == image.Id) {
                        Console.WriteLine("Latest IOTD is still the same as the previous one, no new IOTD to post.");
                        return;
                    }

                    Console.WriteLine("Downloading image");
                    rawImage = wc.DownloadData(image.Url_Hd);

                    if(rawImage == null) {
                        throw new NullReferenceException("Raw image wasn't downloaded");
                    }

                    //File.WriteAllBytes("image.png", rawImage);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
                Console.WriteLine("Fetching from Astrobin failed. Terminating tweet");
                Environment.Exit(-1);
            }

            Console.WriteLine("Authenticating");

            Auth.SetUserCredentials(twitterConsumerKey, twitterConsumerSecret, twitterAccessToken, twitterAccessTokenSecret);

            Console.WriteLine("Uploading image");
            var media = Upload.UploadImage(rawImage);
            
            string tweetText = string.Format("{0}: \"{1}\" by {2}. https://www.astrobin.com/{3} #astronomy #astrophotography", iotd.Date, image.Title, image.User, image.Id);

            Console.WriteLine("Tweeting");

            var tweet = Tweet.PublishTweet(tweetText, new PublishTweetOptionalParameters {
                Medias = new List<IMedia> { media }
            });

            File.WriteAllText(lastIotdIdPath, image.Id);
        }
    }
}
