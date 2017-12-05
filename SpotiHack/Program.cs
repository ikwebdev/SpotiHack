using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web; //Base Namespace
using SpotifyAPI.Web.Auth; //All Authentication-related classes
using SpotifyAPI.Web.Enums; //Enums
using SpotifyAPI.Web.Models; //Models for the JSON-responses

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YoutubeExtractor;
using System.IO;
using System.Net;
using CsQuery;

namespace SpotiHack
{
    class Program
    {
        static AutorizationCodeAuth auth;
        static void Main(string[] args)
        {
            auth = new AutorizationCodeAuth()
            {
                //Your client Id
                ClientId = "29876bdcb6384fc287e37ae01873058c",
                //Set this to localhost if you want to use the built-in HTTP Server
                RedirectUri = "http://localhost:7777/",
                //How many permissions we need?
                Scope = Scope.UserReadPrivate,
            };
            //This will be called, if the user cancled/accept the auth-request
            auth.OnResponseReceivedEvent += auth_OnResponseReceivedEvent;
            //a local HTTP Server will be started (Needed for the response)
            auth.StartHttpServer(7777);
            //This will open the spotify auth-page. The user can decline/accept the request
            auth.DoAuth();

            Thread.Sleep(60000);

        }

        private static void auth_OnResponseReceivedEvent(AutorizationCodeAuthResponse response)
        {  
            auth.StopHttpServer();

            Token token = auth.ExchangeAuthCode(response.Code, "9fde3d021e9f424e8740f1ab3f755bf6");

            var spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken
            };

            /* CONSTS */
            const string userID = "12101170232";
            const string playlistID = "3ExiQTtIAHceYJYXfI5ysH"; // "7ydOkN0ppweUlsiMGQlFjH" //dev
            var afterDate = new DateTime(2017, 11, 20);
            /**/

            var tracks = spotify.GetPlaylistTracks(userID, playlistID);
            var tracksList = new List<TrackModel>();

            

            tracks.Items = tracks.Items.Where(da => da.AddedAt >= afterDate).ToList();

            if (tracks.Total >= 100)
            {
                for (int i = 100; i <= tracks.Total + 100; i += 100)
                {
                    tracks.Items.ForEach(track => Console.WriteLine(track.Track.Artists.FirstOrDefault().Name + " - " + track.Track.Name));
                    tracks.Items.ForEach(track => tracksList.Add(new TrackModel() { Artist = track.Track.Artists.FirstOrDefault().Name, Name = track.Track.Name }));
                    tracks = spotify.GetPlaylistTracks(userID, "3ExiQTtIAHceYJYXfI5ysH", "", 100, i);
                    tracks.Items = tracks.Items.OrderByDescending(d => d.AddedAt).Where(da => da.AddedAt >= afterDate).ToList();
                }
            }
            else
            {
                tracks.Items.ForEach(track => Console.WriteLine(track.Track.Artists.FirstOrDefault().Name + " " + track.Track.Name));
                tracks.Items.ForEach(track => tracksList.Add(new TrackModel() { Artist = track.Track.Artists.FirstOrDefault().Name, Name = track.Track.Name }));
            }

            Parallel.ForEach(tracksList, (track) =>
            {
                new Program().youtubeSearch(track).Wait();
            });

            Console.WriteLine("Gotovo bratishka!");
            Console.ReadKey();
            //With the token object, you can now make API calls
        }

        private async Task youtubeSearch(TrackModel track , string searchTerm = null)
        {
            // Create the service.
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyAiDxF6OIAwIywSVJBu8vFLO7yp0DRWAwc",
                ApplicationName = "SpotiHack"
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = track.Artist + " " + track.Name + " audio"; // Replace with your search term.

            if(!String.IsNullOrEmpty(searchTerm))
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
                    Console.WriteLine("WARNING: PROBABLY WRONG TRACK =( " + track.Artist + " - " + track.Name);
                    Console.ResetColor(); 
                }

                DownloadAudio(videos.FirstOrDefault().Key, track.Artist + " - " + track.Name);
            }   
            else
                new Program().youtubeSearch(track, track.Artist + " " + track.Name).Wait();

        }

        private static void DownloadAudio(string videoId, string fileName)
        {
            string url = "https://youtubemp3api.com/@grab?vidID="+ videoId + "&format=mp3&streams=mp3&api=button";
            string referer = "https://youtubemp3api.com/@api/button/mp3/" + videoId;

            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("Referer", referer);
                
                var response = webClient.DownloadData(url);
                CQ cq = Encoding.Default.GetString(response);

                var mp3Url = cq["a.q320"].FirstOrDefault().GetAttribute("href");
   
                Console.WriteLine("STARTED: " + fileName);

                var mp3File = webClient.DownloadData(mp3Url);

                Directory.CreateDirectory(@"../../../Downloads/");

                FileStream fileStream = new FileStream(
                  $@"../../../Downloads/{fileName}.mp3", FileMode.OpenOrCreate,
                  FileAccess.ReadWrite, FileShare.None);
                fileStream.Write(mp3File, 0, mp3File.Length);
                fileStream.Close();

            }
        }

    }

    public class TrackModel
    {
        public string Artist { get; set; }
        public string Name { get; set; }
    }
}
