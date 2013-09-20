#if JABBR_SERVER
namespace Smuxi.Engine.JabbR
{
    public class ClientMessage
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public string Room { get; set; }
    }
}
#endif