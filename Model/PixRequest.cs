using System;
using System.Collections.Generic;

namespace PixNET.Model
{
    public class PixRequest : BaseRequest
    {
        public string? txid { get; set; }
        public decimal? valor { get; set; }
        public Desconto? desconto { get; set; }
        public Multa? multa { get; set; }
        public Juros? juros { get; set; }
        public Devedor? devedor { get; set; }
        public int? expiracao { get; set; }
        public string? descricao { get; set; }
        public string? chave { get; set; }
        public string? cidade { get; set; }
        public string? nomeRazaoSocial { get; set; }
        public List<InfoAdicionais>? infoAdicionais { get; set; }
        public DateTime? dataVencimento { get; set; }
    }

    public class PixDevolucaoRequest : BaseRequest
    {
        public string e2eid { get; set; }
        public string id { get; set; }
        public decimal valor { get; set; }
    }
}
