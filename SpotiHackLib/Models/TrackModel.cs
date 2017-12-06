using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotiHackLib
{
    public class TrackModel
    {
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Name { get; set; }
        public List<Image> Images { get; set; }
    }
}
