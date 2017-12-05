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

            //  new Program().youtubeSearch().Wait();
            //DownloadAudio();
           // return;

            //Create the auth object
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
          //  auth.StopHttpServer();
         //   Console.WriteLine("Too long, didnt respond, exiting now...");
        }

        private static void auth_OnResponseReceivedEvent(AutorizationCodeAuthResponse response)
        {
            //Stop the HTTP Server, done.
            auth.StopHttpServer();

            //NEVER DO THIS! You would need to provide the ClientSecret.
            //You would need to do it e.g via a PHP-Script.
            Token token = auth.ExchangeAuthCode(response.Code, "9fde3d021e9f424e8740f1ab3f755bf6");

            var spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken
            };

           // var tracks = spotify.GetPlaylistTracks("12101170232", "3ExiQTtIAHceYJYXfI5ysH");
            var tracks = spotify.GetPlaylistTracks("12101170232", "7ydOkN0ppweUlsiMGQlFjH");
            var tracksList = new List<string>();
            // tracks.Items.Sort(i => i.AddedAt )

            if (tracks.Total >= 100)
            {
                for (int i = 100; i <= tracks.Total + 100; i += 100)
                {
                    tracks.Items.ForEach(track => Console.WriteLine(track.Track.Artists.FirstOrDefault().Name + " - " + track.Track.Name));
                    tracks.Items.ForEach(track => tracksList.Add(track.Track.Artists.FirstOrDefault().Name + " " + track.Track.Name));
                    tracks = spotify.GetPlaylistTracks("12101170232", "7ydOkN0ppweUlsiMGQlFjH", "", 100, i);
                }
            }
            else
            {
                tracks.Items.ForEach(track => Console.WriteLine(track.Track.Artists.FirstOrDefault().Name + " " + track.Track.Name));
                tracks.Items.ForEach(track => tracksList.Add(track.Track.Artists.FirstOrDefault().Name + " " + track.Track.Name));
            }


            Parallel.ForEach(tracksList, (track) =>
            {
                new Program().youtubeSearch(track).Wait();
            });

            Console.WriteLine("Gotovo bratishka!");
            Console.ReadKey();
            //With the token object, you can now make API calls
        }

        private async Task youtubeSearch(string track)
        {
            // Create the service.
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyAiDxF6OIAwIywSVJBu8vFLO7yp0DRWAwc",
                ApplicationName = "SpotiHack"
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = track + " audio"; // Replace with your search term.

            //Special kostyl
            if (track.Contains("Solence") || track.Contains("Death Come"))
                searchListRequest.Q = track; // Replace with your search term.


            searchListRequest.MaxResults = 10;
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(String.Format("{0}", searchResult.Id.VideoId));
                        break;

                    case "youtube#channel":
                        channels.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.ChannelId));
                        break;

                    case "youtube#playlist":
                        playlists.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.PlaylistId));
                        break;
                }
            }

            DownloadAudio(videos.FirstOrDefault(), track);
        }

        private static void DownloadAudio(string videoId, string fileName)
        {
            string url = "https://youtubemp3api.com/@grab?vidID="+ videoId + "&format=mp3&streams=mp3&api=button";
            string referer = "https://youtubemp3api.com/@api/button/mp3/" + videoId;

            // Создаём объект WebClient
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("Referer", referer);
                // Выполняем запрос по адресу и получаем ответ в виде строки
                var response = webClient.DownloadData(url);
                CQ cq = System.Text.Encoding.Default.GetString(response);

                var mp3Url = cq["a.q320"].FirstOrDefault().GetAttribute("href");
   
                Console.WriteLine(mp3Url);

                var mp3File = webClient.DownloadData(mp3Url);
              
                FileStream fileStream = new FileStream(
                  $@"C:/Users/Master/Documents/SpotiHack/Downloads/{fileName}.mp3", FileMode.OpenOrCreate,
                  FileAccess.ReadWrite, FileShare.None);
                fileStream.Write(mp3File, 0, mp3File.Length);
                fileStream.Close();

            }
        }

    }
}
