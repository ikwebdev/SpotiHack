using CsQuery;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SpotiHackLib.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TagLib;

namespace SpotiHackLib.Services
{
    public class YoutubeSearchService
    {
        public async Task YoutubeSearchTrack(TrackModel track, string searchTerm = null)
        {
            // Create the service.
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyAiDxF6OIAwIywSVJBu8vFLO7yp0DRWAwc",
                ApplicationName = "SpotiHack"
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = track.Artist + " " + track.Name + " audio"; // Replace with your search term.

            if (!String.IsNullOrEmpty(searchTerm))
                searchListRequest.Q = searchTerm;

            searchListRequest.MaxResults = 10;
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            var videos = new Dictionary<string, string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(searchResult.Id.VideoId, searchResult.Snippet.Title);
                        break;
                }
            }

            if (videos.FirstOrDefault().Value.ToLower().Contains(track.Artist.ToLower()) || !String.IsNullOrEmpty(searchTerm))
            {
                if (!videos.FirstOrDefault().Value.ToLower().Contains(track.Artist.ToLower()))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(DateTime.Now.TimeOfDay + " WARNING: PROBABLY WRONG TRACK =( " + track.Artist + " - " + track.Name);
                    Console.ResetColor();
                }

                DownloadAudio(videos.FirstOrDefault().Key, track);
            }
            else
                YoutubeSearchTrack(track, track.Artist + " " + track.Name).Wait();

        }

        public string GetYoutubeVideoId(string url)
        {
            var uri = new Uri(url);

            var query = HttpUtility.ParseQueryString(uri.Query);

            var videoId = string.Empty;

            if (query.AllKeys.Contains("v"))
            {
                videoId = query["v"];
            }
            else
            {
                videoId = uri.Segments.Last();
            }

            return videoId;
        }

        public void DownloadAudio(string videoId, TrackModel track)
        {
            string url = "https://youtubemp3api.com/@grab?vidID=" + videoId + "&format=mp3&streams=mp3&api=button";
            string referer = "https://youtubemp3api.com/@api/button/mp3/" + videoId;
            var fileName = CleanFileName(track.Artist + " - " + track.Name);

            try
            {
                using (var webClient = new CustomWebClient())
                {
                    webClient.Headers.Add("Referer", referer);

                    var response = webClient.DownloadData(url);
                    CQ cq = Encoding.Default.GetString(response);

                    var mp3Url = cq["a.q320"].FirstOrDefault().GetAttribute("href");

                    Console.WriteLine(DateTime.Now.TimeOfDay + " STARTED: " + fileName + " from: https://www.youtube.com/watch?v=" + videoId);

                    var mp3File = webClient.DownloadData("https:" + mp3Url);

                    Directory.CreateDirectory(@"Downloads/");

                    FileStream fileStream = new FileStream(
                      $@"Downloads/{fileName}.mp3", FileMode.OpenOrCreate,
                      FileAccess.ReadWrite, FileShare.None);
                    fileStream.Write(mp3File, 0, mp3File.Length);
                    fileStream.Close();


                    //Set tags

                    var fileForTags = TagLib.File.Create($@"Downloads/{fileName}.mp3"); // Change file path accordingly.

                    fileForTags.Tag.Title = track.Name;
                    fileForTags.Tag.Album = track.Album;
                    fileForTags.Tag.Performers = new string[] { track.Artist };

                    if (track.Images != null)
                    {
                        var thumbnail = webClient.DownloadData(track.Images.FirstOrDefault().Url);
                        Picture picture = new Picture(new ByteVector(thumbnail));
                        fileForTags.Tag.Pictures = new Picture[] { picture };
                    }

                    // Save Changes:
                    fileForTags.Save();

                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(DateTime.Now.TimeOfDay + " ERROR: when downloading or writing " + fileName);
                Console.WriteLine("Details: " + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine + e.InnerException);
                Console.ResetColor();
            }

        }

        public string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
    }
}
