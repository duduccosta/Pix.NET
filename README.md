# Seja bem vindo ao PIX.NET!

Esta biblioteca foi desenvolvida com o intuito de auxiliar na implementação de PIX em aplicações .NET.
Baseada em .NET Standard para que possa funcionar com qualquer versão do .NET.

# Dependências
 1. Newtonsoft (13.0.1) - Biblioteca para trabalhar com JSON.
# Bancos Homologados
 - [x] Banco do Brasil (Homologação e Produção) 
 - [x] Santander (Homologação e Produção) 
 - [x] Itaú (Homologação e Produção)  
 - [x] Sicoob (Homologação e Produção)   
 - [ ] Bradesco (Em Testes) 

# Como Utilizar
A primeira coisa que deve-se atentar, é sobre a classe ***Model.ProvedorToken***.
Esta classe é do tipo **enum**, representando os bancos disponíveis com seus respectivos códigos:

> - NONE = 0,
> - SICOOB = 756,
> - Santander = 33,
> - BancoBrasil = 1,
> - Itau = 341,
> - Bradesco = 237

## 1. Para iniciar o fluxo do código, precisamos definir qual banco será utilizado:

```csharp
ProvedorToken psp = (ProvedorToken)Enum.Parse(typeof(ProvedorToken), "33");
```

## 2. Com o banco definido, precisamos instanciar o objeto **PixPayload**:

```csharp
PixPayload payload = new PixPayload
{
    calendario = new Calendario
    {
        expiracao = 3600
    },
    chave = "suachavepix@email.com",
    valor = new Valor
    {
        original = "1122.43"
    },
    txid = "TESTE000000000000000000000000000001",
    solicitacaoPagador = "Descrição da transação",
    devedor = new Devedor {
	    nome = "Fulano de tal",
	    cpf = "00000000000" //cnpj
    },
    infoAdicionais = new List<InfoAdicionais>
    {
	    new InfoAdicionais 
	    {
		    nome = "Info 1",
		    valor = "Valor info 1"
	    }
	}
};
```
> 📝 **NOTE:** Vale ressaltar que a classe **Devedor()** aceita tanto o atributo ***cpf*** quanto ***cnpj***.

## 3. Definir o objeto de credenciais:

```csharp
Credentials credentials = new Credentials
{
    clientId = "xxxxxxxxxxxxxx",
    clientSecret = "yyyyyyyyyyyy",
    developerKey = "zzzzzzzzzzzzzzzzzzz"
};
```
> 📝 **NOTE:** O Atributo developerKey deve ser setado para os bancos:
> - Banco do Brasil

## 4. Definir o certificado digital (obrigatório em ambiente de produção):

```csharp
byte[] cer = System.IO.File.ReadAllBytes("Path do arquivo PFX");
string password = "password";
```

## 5. Iniciando a classe PIX:

## 5.1. Criando uma cobrança imediata

Com async/await:
```csharp
using PIX pix = new PIX(payload, credentials, psp, cer, password, PixAmbiente.Homologacao);
pix.SetNomeRazaoSocial("Nome da empresa proprietária da chave");
pix.SetCidade("Cidade Sede");
PixPayload cob = await pix.CreateCobrancaAsync();
```
Sem async:
```csharp
using PIX pix = new PIX(payload, credentials, psp, cer, password, PixAmbiente.Homologacao);
pix.SetNomeRazaoSocial("Nome da empresa proprietária da chave");
pix.SetCidade("Cidade Sede");
PixPayload cob = pix.CreateCobranca();
```

## 5.2. Consultando uma cobrança imediata:
Com async/await:
```csharp
PixPayload cob = await pix.ConsultaCobrancaAsync("txid");
```
Sem async/await:
```csharp
PixPayload cob = pix.ConsultaCobranca("txid");
```

## 5.3. Listando cobranças:
Com async/await:
```csharp
PixRecebidosPayload payload = new PixRecebidosPayload
 {
     fim = DateTime.Now.Date,
     inicio = DateTime.Now.Date,
 };

 using PIX pix = new PIX(payload, credentials, psp, cer, request.password, request.ambiente);
 var lista = await pix.ConsultaPixRecebidosAsync();
```
Sem async/await:
```csharp
PixRecebidosPayload payload = new PixRecebidosPayload
 {
     fim = DateTime.Now.Date,
     inicio = DateTime.Now.Date,
 };

 using PIX pix = new PIX(payload, credentials, psp, cer, request.password, request.ambiente);
 var lista = pix.ConsultaPixRecebidos();
```

## Créditos
- Autor: Eduardo Carvalho Costa
- Email: eduardoccosta@outlook.com
- Analista de sistemas: [Quality Systems](https://qualitysys.com.br)
> :exclamation: **NOTE:** Dúvidas somente pelo GitHub

## Licença
MIT
