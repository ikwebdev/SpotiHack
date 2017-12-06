using CsQuery;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotiHackLib;
using SpotiHackLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TagLib;

namespace SpotiHack
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--# SpotiHack v0.1 #--");
            Console.WriteLine("Welcome bratishka at " + DateTime.Now.TimeOfDay);

            int userInput = 0;
            do
            {
                userInput = DisplayMenu();

                switch (userInput)
                {
                    case (int)MenuItem.SpotifyPlaylist:
                        StartSpotifyPlaylist();
                        break;
                    case (int)MenuItem.YoutubeLink:
                        StartDownloadFromYoutubeLink();
                        break;
                }
                    
            } while (userInput != (int)MenuItem.Exit);

        }

        static public int DisplayMenu()
        {
            Console.WriteLine();
            Console.WriteLine((int)MenuItem.SpotifyPlaylist + ". Download spotify playlist");
            Console.WriteLine((int)MenuItem.YoutubeLink + ". Download mp3 from youtube link");

            Console.WriteLine((int)MenuItem.Exit + ". Exit");
            var result = Console.ReadLine();
            return Convert.ToInt32(result);
        }

        private static void StartSpotifyPlaylist()
        {
            Console.WriteLine("Playlist URL:");
            var playlistUrl = Console.ReadLine();

            if (!IsValidUrl(playlistUrl))
            {
                Console.WriteLine("Not valid url");
                return;
            }


            var playlistUrlSegments = new Uri(playlistUrl).Segments;

            var userID = playlistUrlSegments[2].Replace(@"/", string.Empty);
            var playlistID = playlistUrlSegments[4].Replace(@"/", string.Empty);

            Console.WriteLine("Date from:");
            var dateFrom = Console.ReadLine();

            if (!DateTime.TryParse(dateFrom, out DateTime parsedDate))
            {
                Console.WriteLine("Not valid date");
                return;
            }

            var afterDate = parsedDate;

            var spotiHack = new SpotiHackService(userID, playlistID, afterDate);
            spotiHack.Run();
        }
        private static void StartDownloadFromYoutubeLink()
        {
            Console.WriteLine("Youtube video url:");
            var videoUrl = Console.ReadLine();

            Console.WriteLine("Artist:");
            var artist = Console.ReadLine();

            Console.WriteLine("Track name:");
            var name = Console.ReadLine();

            var youtubeSearchService = new YoutubeSearchService();

            var videoId = youtubeSearchService.GetYoutubeVideoId(videoUrl);
            
            if(String.IsNullOrEmpty(videoId))
            {
                Console.WriteLine("Not valid youtube url");
                return;
            }

            new YoutubeSearchService().DownloadAudio(videoId, new TrackModel() { Artist = artist, Name = name });
        }

        private static bool IsValidUrl(string url)
        {
            Uri uriResult;
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);  
        }
    }

    public enum MenuItem
    {
       SpotifyPlaylist = 1,
       YoutubeLink = 2,
       Exit = 0
    }
}
