#if JABBR_SERVER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Smuxi.Engine.JabbR
{
    public class User
    {
        public string Name { get; set; }
        public string Hash { get; set; }
        public bool Active { get; set; }
        public UserStatus Status { get; set; }
        public string Note { get; set; }
        public string AfkNote { get; set; }
        public bool IsAfk { get; set; }
        public string Flag { get; set; }
        public string Country { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsAdmin { get; set; }
    }
}
#endif