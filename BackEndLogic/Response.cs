using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEndLogic {
    public class Response {
        public bool success { get; set; }
        public string message { get; set; }
        public Dictionary<string, object> data { get; set; }

        public Response() {
            this.success = true;
            this.message = "";
            this.data = new Dictionary<string, object>();
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this);
        }
    }
}
