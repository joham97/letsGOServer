using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEndLogic {
    class Config {
        public const string DATABASE_NAME = "gamedata";
        public const int SESSION_DURATION = 180;
        public const int SESSION_KEY_LENGTH = 256;
        public const bool CREATE_NEW_INITIAL_DATABASE = false;
    }
}
