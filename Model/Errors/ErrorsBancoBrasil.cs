using System.Collections.Generic;

namespace PixNET.Model.Errors.BancoBrasil
{
    public class Errors
    {
        public List<Error> erros { get; set; }
        public class Error
        {
            public string mensagem { get; set; }
        }
    }

    
}
