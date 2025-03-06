using System.Collections.Generic;

namespace PixNET.Model.Errors.Sicredi
{
    public class Errors
    {
        public string title { get; set; }
        public string detail { get; set; }
        public List<Violacao> violacoes { get; set; }

        public class Violacao
        { 
            public string propriedade { get; set; }
            public string razao { get; set; }
            public string valor { get; set; }
        }
    }
}
