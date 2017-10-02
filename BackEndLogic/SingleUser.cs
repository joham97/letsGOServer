using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEndLogic {
    class SingleUser {
        public string id;
        public string name;
        public string atp;
        public string wp;
        public string rank;

        public SingleUser(string id, string name, string atp, string wp, string rank) {
            this.id = id;
            this.name = name;
            this.atp = atp;
            this.wp = wp;
            this.rank = rank;
        }
    }
}
