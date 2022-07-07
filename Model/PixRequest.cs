using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixNET.Model
{
    public class PixRequest : BaseRequest
    {
        public string txid { get; set; }
        public double valor { get; set; }
        public Devedor devedor { get; set; }
        public int expiracao { get; set; }
        public string descricao { get; set; }
        public string chave { get; set; }
        public int banco { get; set; }
        public string cidade { get; set; }
        public string nomeRazaoSocial { get; set; }
        public List<InfoAdicionais> infoAdicionais { get; set; }
    }
}
