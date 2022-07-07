using PixNET.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixNET.Services.Pix.Bancos
{
    public interface IBanco
    {

        void SetPayload(BasePayload payload);
        void SetCredentials(Credentials credentials);
        void SetCertificateFile(string certificateFile, string password);
        void SetCertificateFile(byte[] certificate, string password);
        Task GetAccessTokenAsync(bool force = false);
        void GetAccessToken(bool force = false);
        void SetNomeRazaoSocial(string nome);
        void SetCidade(string cidade);
        PixPayload CreateCobranca();
        Task<PixPayload> CreateCobrancaAsync();
        Task<PixPayload> ConsultaCobrancaAsync(string txid);
        Task<List<Model.Pix>> ConsultaPixRecebidosAsync();
    }
}
