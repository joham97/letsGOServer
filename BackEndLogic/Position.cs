using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEndLogic {
    class Position {
        public int koordX;
        public int koordY;
        public int status;

        public Position(int koordX, int koordY, int status) {
            this.koordX = koordX;
            this.koordY = koordY;
            this.status = status;
        }
    }
}
