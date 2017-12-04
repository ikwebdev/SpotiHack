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

namespace SpotiHack
{
    class Program
    {
        static AutorizationCodeAuth auth;
        static void Main(string[] args)
        {
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
            Console.WriteLine("Too long, didnt respond, exiting now...");
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
            var tracks = spotify.GetPlaylistTracks("cevig", "004GONyTxoKCxA7lYdkuyu");

            // tracks.Items.Sort(i => i.AddedAt )

            if (tracks.Total >= 100)
            {
                for (int i = 100; i <= tracks.Total + 100; i += 100)
                {
                    tracks.Items.ForEach(track => Console.WriteLine(track.Track.Artists.FirstOrDefault().Name + " - " + track.Track.Name));
                    tracks = spotify.GetPlaylistTracks("cevig", "004GONyTxoKCxA7lYdkuyu", "", 100, i);
                }
            }

            Console.ReadKey();
            //With the token object, you can now make API calls
        }

    }
}
