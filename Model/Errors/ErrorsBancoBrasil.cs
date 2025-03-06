using System.Collections.Generic;

namespace PixNET.Model.Errors.BancoBrasil
{
    public class Errors
    {
        public List<Error> erros { get; set; }
        public List<Error> errors { get; set; }
        public string error { get; set; }
        public string message { get; set; }

        public class Error
        {
            public string mensagem { get; set; }
        }
    }

    public class Error
    {
        public string error { get; set; }
        public string message { get; set; }
    }

}
