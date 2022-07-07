using PixNET.Services.Pix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixNET.Model
{
    public class BaseRequest
    {

        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string optional_id { get; set; }
        public int banco { get; set; }
        public PixAmbiente ambiente { get; set; }
        public string certificate { get; set; }
        public string password { get; set; }

    }

    public class BasePayload { }
}
