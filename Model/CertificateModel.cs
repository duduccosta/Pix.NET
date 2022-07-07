using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixNET.Model
{
    public class CertificateModel
    {
        public string nomeRazaoSocial { get; set; }
        public string cpfCnpj { get; set; }
        public DateTime dataValidade { get; set; }
        public string thumbPrint { get; set; }
    }
}
