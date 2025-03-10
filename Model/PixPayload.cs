﻿using System;
using System.Collections.Generic;

namespace PixNET.Model
{
    public class PixPayload : BasePayload
    {
        /// <summary>
        /// Status da transação
        /// </summary>
        /// <example>ATIVA ou CONCLUIDA ou REMOVIDA_PELO_USUARIO_RECEBEDOR ou REMOVIDA_PELO_PSP</example>
        public string? status { get; set; }
        public Calendario calendario { get; set; }
        public Valor valor { get; set; }
        public string? chave { get; set; }
        public string? solicitacaoPagador { get; set; }
        public string? location { get; set; }
        public string? txid { get; set; }
        public int revisao { get; set; }
        public string? textoImagemQRcode { get; set; }
        public string? pixCopiaECola { get; set; }
        public Devedor devedor { get; set; }
        public List<InfoAdicionais> infoAdicionais { get; set; }
        public List<OperacoesPix> pix { get; set; }
    }

    public class Devedor
    {
        /// <summary>
        /// Retorna o nome do devedor, tanto pessoa física, quanto pessoa jurídica
        /// </summary>
        /// <example>José Fulano</example>
        public string nome { get; set; }
        /// <summary>
        /// Retorna o CPF do devedor, para pessoa física
        /// </summary>
        /// <example>11111111111</example>
        public string? cpf { get; set; }
        /// <summary>
        /// Retorna o CNPJ do devedor, para pessoa jurídica
        /// </summary>
        /// <example>11111111111111</example>
        public string? cnpj { get; set; }
        /// <summary>
        /// Retorna o Logradouro do devedor
        /// </summary>
        /// <example>11111111111111</example>
        public string? logradouro { get; set; }
        /// <summary>
        /// Retorna a cidade do devedor
        /// </summary>
        /// <example>Uberlandia</example>
        public string? cidade { get; set; }
        /// <summary>
        /// Retorna o UF do devedor
        /// </summary>
        /// <example>MG</example>
        public string? uf { get; set; }
    }

    public class OperacoesPix
    {
        public string endToEndId { get; set; }
        public string txid { get; set; }
        public string valor { get; set; }
        public DateTime horario { get; set; }
        public Pagador pagador { get; set; }
        public string infoPagador { get; set; }
        public string chave { get; set; }

        public List<PixDevolucao> devolucoes { get; set; }
    }

    public class PixWebhook
    {
        public OperacoesPix[] pix { get; set; }
    }

    public class Pagador
    {
        public string cpf { get; set; }
        public string cnpj { get; set; }
        public string nome { get; set; }

    }

    public class InfoAdicionais
    {
        public InfoAdicionais(string nome, string valor)
        {
            this.nome = nome;
            this.valor = valor;
        }
        public string? nome { get; set; }
        public string? valor { get; set; }
    }

    public class Valor
    {
        public string? original { get; set; }
        public Multa? multa { get; set; }
        public Juros? juros { get; set; }
        public Desconto? desconto { get; set; }
    }

    public class Desconto
    {
        public ModalidadeDesconto modalidade { get; set; }
        public List<DescontoItem> descontoDataFixa { get; set; } = [];

    }

    public class Multa
    {
        public ModalidadeMulta modalidade { get; set; }
        public decimal valorPerc { get; set; }
    }

    public class Juros
    {
        public ModalidadeJuros modalidade { get; set; }
        public decimal valorPerc { get; set; }
    }

    public class DescontoItem
    {
        public DateTime data { get; set; }
        public decimal valorPerc { get; set; }
    }

    public class Calendario
    {
        public int expiracao { get; set; }
        public DateTime? criacao { get; set; }

        private DateTime? m_DataDeVencimento;
        public DateTime? dataDeVencimento {
            get
            {
                if (m_DataDeVencimento is not null)
                    return ((DateTime)m_DataDeVencimento).Date;

                return m_DataDeVencimento;
            }
            set
            {
                if (value is not null)
                    m_DataDeVencimento = ((DateTime)value).Date;
            } 
        }
        public int? validadeAposVencimento { get; set; } = 60;
    }

    public class PixConfig
    {
        public string? clientId { get; set; }
        public string? clientSecret { get; set; }
        public string? developerKey { get; set; }
        public string? chave { get; set; }
        public ProvedorToken banco { get; set; }
    }

    public class Endpoint
    {
        public string? AuthorizationToken { get; set; }
        public string? Pix { get; set; }
        public string? Webhook { get; set; }
    }

    public class PixRecebidosPayload : BasePayload
    {
        public DateTime inicio { get; set; }
        public DateTime fim { get; set; }
        //public int paginaAtual { get; set; }
    }
    public class PixRecebidosRequest : BaseRequest
    {
        public DateTime inicio { get; set; }
        public DateTime fim { get; set; }
        //public int paginaAtual { get; set; }
        public PixRecebidosParametrosPaginacao? paginacao { get; set; }

    }

    public class PixRecebidos
    {
        public PixRecebidosParametros parametros { get; set; }
        public List<Pix> pix { get; set; }
    }

    public class CobsRecebidos
    {
        public PixRecebidosParametros parametros { get; set; }
        public List<PixPayload> cobs { get; set; }
    }

    public class Pix
    {
        public string endToEndId { get; set; }
        public string txid { get; set; }
        public decimal valor { get; set; }
        public DateTime horario { get; set; }
        public string infoPagador { get; set; }
        public Pagador pagador { get; set; }
        public List<PixDevolucao> devolucoes { get; set; }
    }

    public class PixRecebidosParametros
    {
        public DateTime inicio { get; set; }
        public DateTime fim { get; set; }
        public PixRecebidosParametrosPaginacao paginacao { get; set; }
    }

    public class PixRecebidosParametrosPaginacao
    {
        public int paginaAtual { get; set; }
        public int itensPorPagina { get; set; }
        public int quantidadeDePaginas { get; set; }
        public int quantidadeTotalDeItens { get; set; }
    }

    public class PixError
    {
        public List<Erros> erros { get; set; }
    }

    public class PixDevolucao
    {
        public string id { get; set; }
        public string rtrId { get; set; }
        public string valor { get; set; }
        public Horario horario { get; set; }
        public string status { get; set; }
    }
    public class Horario
    {
        public DateTime solicitacao { get; set; }
    }


    public class Erros
    {
        public string codigo { get; set; }
        public string versao { get; set; }
        public string mensagem { get; set; }
        public string ocorrencia { get; set; }
    }

    public class PixDevolucaoPayload : BasePayload
    {
        public string e2eid { get; set; }
        public string id { get; set; }
        public decimal valor { get; set; }
    }

    public enum ModalidadeMulta
    {
        FIXO = 1,
        PERCENTUAL = 2
    }

    public enum ModalidadeDesconto
    {
        FIXO_ATE_A_DATA = 1,
        PERCENTUAL_ATE_A_DATA = 2,
        VALOR_ATENCIPACAO_DIA_CORRIDO = 3,
        VALOR_ATENCIPACAO_DIA_UTIL = 4,
        PERCENTUAL_ATENCIPACAO_DIA_CORRIDO = 5,
        PERCENTUAL_ATENCIPACAO_DIA_UTIL = 6

    }

    public enum ModalidadeJuros
    {
        VALOR_DIAS_CORRIDOS = 1,
        PERCENTUAL_AO_DIA_DIAS_CORRIDOS  = 2,
        PARCENTUAL_AO_MES_DIAS_CORRIDOS  = 3,
        PERCENTUAL_AO_ANO_DIAS_CORRIDOS = 4,
        VALOR_DIAS_UTEIS = 5,
        PERCENTUAL_AO_DIA_DIAS_UTEIS = 6,
        PARCENTUAL_AO_MES_DIAS_UTEIS = 7,
        PERCENTUAL_AO_ANO_DIAS_UTEIS = 8

    }
}
