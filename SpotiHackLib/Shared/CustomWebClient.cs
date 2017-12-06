using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SpotiHackLib.Shared
{
     public class CustomWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 60 * 60 * 1000; // 5min
            return w;
        }
    }
}
