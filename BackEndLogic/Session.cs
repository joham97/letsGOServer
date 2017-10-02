using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEndLogic {
    class Session {
        public int userid;
        public long lastactivity;

        public Session(int userid, long lastactivity) {
            this.userid = userid;
            this.lastactivity = lastactivity;
        }
    }
}
