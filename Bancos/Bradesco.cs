﻿using PixNET.Model;

namespace PixNET.Services.Pix.Bancos
{
    public class Bradesco : Banco
    {
        public Bradesco(PixAmbiente? ambiente)
        {
            endpoint = new Endpoint
            {
                AuthorizationToken = ambiente == PixAmbiente.Producao ? "https://qrpix.bradesco.com.br/auth/server/oauth/token" : "https://qrpix-h.bradesco.com.br/auth/server/oauth/token",
                Pix = ambiente == PixAmbiente.Producao ? "https://qrpix.bradesco.com.br/" : "https://qrpix-h.bradesco.com.br/"
            };
        }
    }
}
