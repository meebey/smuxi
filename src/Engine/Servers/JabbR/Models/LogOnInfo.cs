#if JABBR_SERVER
using System.Collections.Generic;

namespace Smuxi.Engine.JabbR
{
    public class LogOnInfo
    {
        public string UserId { get; set; }
        public IEnumerable<Room> Rooms { get; set; }

        public LogOnInfo()
        {
            Rooms = new List<Room>();
        }
    }
}
#endif