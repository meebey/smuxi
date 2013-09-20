#if JABBR_SERVER
using System;

namespace Smuxi.Engine.JabbR
{
    public class Message
    {
        public bool HtmlEncoded { get; set; }
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTimeOffset When { get; set; }
        public User User { get; set; }
    }
}
#endif