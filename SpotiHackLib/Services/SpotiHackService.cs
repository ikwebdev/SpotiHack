using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using SpotiHackLib.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotiHackLib
{
    public class SpotiHackService
    {
        private AutorizationCodeAuth _auth;
        private string UserID = String.Empty;
        private string PlaylistID = String.Empty; // "7ydOkN0ppweUlsiMGQlFjH" //dev 3ExiQTtIAHceYJYXfI5ysH //prod
        private DateTime AfterDate = DateTime.MinValue;

        public SpotiHackService(string userID, string playlistID, DateTime afterDate)
        {
            UserID = userID;
            PlaylistID = playlistID;
            AfterDate = afterDate;
        }
        public void Run()
        {
            _auth = new AutorizationCodeAuth()
            {
                //Your client Id
                ClientId = "29876bdcb6384fc287e37ae01873058c",
                //Set this to localhost if you want to use the built-in HTTP Server
                RedirectUri = "http://localhost:7777/",
                //How many permissions we need?
                Scope = Scope.UserReadPrivate,
            };
            //This will be called, if the user cancled/accept the auth-request
            _auth.OnResponseReceivedEvent += Auth_OnResponseReceivedEvent;
            //a local HTTP Server will be started (Needed for the response)
            _auth.StartHttpServer(7777);
            //This will open the spotify auth-page. The user can decline/accept the request
            _auth.DoAuth();
        }

        private void Auth_OnResponseReceivedEvent(AutorizationCodeAuthResponse response)
        {
            _auth.StopHttpServer();

            Token token = _auth.ExchangeAuthCode(response.Code, "9fde3d021e9f424e8740f1ab3f755bf6");

            var spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken
            };

            var tracks = spotify.GetPlaylistTracks(UserID, PlaylistID);
            var tracksList = new List<TrackModel>();

            tracks.Items = tracks.Items.Where(da => da.AddedAt >= AfterDate).ToList();

            if (tracks.Total >= 100)
            {
                for (int i = 100; i <= tracks.Total + 100; i += 100)
                {
                    tracks.Items.ForEach(track => Console.WriteLine(track.Track.Artists.FirstOrDefault().Name + " - " + track.Track.Name));
                    tracks.Items.ForEach(track => tracksList.Add(new TrackModel()
                    {
                        Artist = track.Track.Artists.FirstOrDefault().Name,
                        Album = track.Track.Album.Name,
                        Name = track.Track.Name,
                        Images = track.Track.Album.Images
                    }));
                    tracks = spotify.GetPlaylistTracks(UserID, PlaylistID, "", 100, i);
                    tracks.Items = tracks.Items.OrderByDescending(d => d.AddedAt).Where(da => da.AddedAt >= AfterDate).ToList();
                }
            }
            else
            {
                tracks.Items.ForEach(track => Console.WriteLine(track.Track.Artists.FirstOrDefault().Name + " " + track.Track.Name));
                tracks.Items.ForEach(track => tracksList.Add(new TrackModel()
                {
                    Artist = track.Track.Artists.FirstOrDefault().Name,
                    Album = track.Track.Album.Name,
                    Name = track.Track.Name,
                    Images = track.Track.Album.Images
                }));
            }

            Parallel.ForEach(tracksList, (track) =>
            {
                new YoutubeSearchService().YoutubeSearchTrack(track).Wait();
            });

            Console.WriteLine("Gotovo bratishka at " + DateTime.Now.TimeOfDay);
            Console.ReadKey();
            //With the token object, you can now make API calls
        }
    }    
}
