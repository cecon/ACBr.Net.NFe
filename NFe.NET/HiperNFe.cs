using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using FuncoesUteis;
using NFe.NET;
using NFe.NET.DANFE;
using NFe.NET.ImpressaoCartaCorrecao;
using NFe.NET.TiposBasicos;
using NFe.NET.Webservice;
using NFe.NET.DownloadNFe;

public class HiperNFe
{
    private X509Certificate2 _Certificado;
    private List<TNfeProc> _NotasFiscais;
    private static List<string> _Erros;
    private ConfiguracaoHiperNFe _Configuracao;

    public HiperNFe()
    {

    }

    public static List<string> Erros
    {
        get { return HiperNFe._Erros; }
        set { HiperNFe._Erros = value; }
    }

    public ConfiguracaoHiperNFe Configuracao
    {
        get
        {
            if (_Configuracao == null)
            {
                _Configuracao = new ConfiguracaoHiperNFe();
            }
            return _Configuracao;
        }
        set { _Configuracao = value; }
    }

    public List<TNfeProc> NotasFiscais
    {
        get
        {
            if (_NotasFiscais == null)
            {
                _NotasFiscais = new List<TNfeProc>();
            }
            return _NotasFiscais;
        }
        set { _NotasFiscais = value; }
    }

    public X509Certificate2 Certificado
    {
        get
        {
            if (_Certificado == null)
            {
                _Certificado = CertificadoDigital.SelecionarCertificado(Configuracao.NumCertificado);
            }
            return _Certificado;
        }
        set { _Certificado = value; }
    }

    public TRetConsReciNFe ConsultarLote(TConsReciNFe consRecibo, TRetEnviNFe retEnvio)
    {
        C_WebService ws = new C_WebService(Configuracao.PastaLog);
        TRetConsReciNFe reciboNFe = default(TRetConsReciNFe);
        reciboNFe = ws.ConsultaRecLote2(consRecibo, retEnvio, Certificado);
        if (reciboNFe.protNFe != null)
        {
            foreach (var item in reciboNFe.protNFe)
            {
                TNfeProc nota = NotasFiscais.FirstOrDefault(n => n.NFe.infNFe.Id == "NFe" + item.infProt.chNFe);
                nota.protNFe = item;

                if (item.infProt.cStat == "100")
                {
                    SalvarNFe(ref nota);
                }
            }
        }
        return reciboNFe;
    }

    public TRetConsReciNFe Enviar(int NumeroLote)
    {
        C_WebService ws = new C_WebService(Configuracao.PastaLog);
        TRetEnviNFe retEnvio = ws.EnviaLote2(this, NumeroLote);
        TConsReciNFe consRecibo = new TConsReciNFe();
        consRecibo.nRec = retEnvio.infRec.nRec;
        consRecibo.tpAmb = retEnvio.tpAmb;
        consRecibo.versao = retEnvio.versao;
        System.Threading.Thread.Sleep(5000);
        try
        {
            return ConsultarLote(consRecibo, retEnvio);
        }
        catch (Exception)
        {
            System.Threading.Thread.Sleep(3000);
            return ConsultarLote(consRecibo, retEnvio);
        }
    }

    public TRetConsStatServ StatusServico()
    {
        if (!Directory.Exists(Configuracao.PastaLog))
        {
            Directory.CreateDirectory(Configuracao.PastaLog);
        }

        C_WebService.ListaUrl listaURL = default(C_WebService.ListaUrl);
        listaURL = WsUrls.BuscaURL(Configuracao.CodUF, Configuracao.Ambiente);
        C_WebService ws = new C_WebService(Configuracao.PastaLog);
        TConsStatServ consStatus = new TConsStatServ();

        consStatus.tpAmb = Configuracao.Ambiente;
        consStatus.versao = Configuracao.Versao;
        consStatus.cUF = Configuracao.CodUF;

        try
        {
            return ws.ConsultaStatusWs(consStatus, this.Certificado);
        }
        catch (Exception)
        {
            throw new Exception("Não foi possível consultar o status do servidor da Receita Federal. Favor tentar novamente.");
        }
    }

    public static TRetDownloadNFe DownloadNFe(TDownloadNFe downloadNFe, string CodUF, X509Certificate2 cert)
    {
        return C_WebService.DownloadNFe(downloadNFe, (TCodUfIBGE)Enum.Parse(typeof(TCodUfIBGE), CodUF), cert);
    }

    public List<TRetConsSitNFe> ConsultaSituacao()
    {
        if (!Directory.Exists(Configuracao.PastaLog))
        {
            Directory.CreateDirectory(Configuracao.PastaLog);
        }

        C_WebService ws = new C_WebService(Configuracao.PastaLog);
        List<TRetConsSitNFe> retorno = new List<TRetConsSitNFe>();
        retorno = ws.ConsultaSitNFe(this, Certificado);
        return retorno;
    }

    public NFe.NET.CartaCorrecao.TRetEnvEvento CartaCorrecao(string Correcao, int numLote, int nSequencia, out NFe.NET.CartaCorrecao.TProcEvento procCorrecao)
    {
        if (NotasFiscais.Count > 1)
        {
            throw new InvalidOperationException("Só é possível corrigir uma nota por vez.");
        }
        TNfeProc nota = NotasFiscais[0];
        Correcao = Funcoes.RemoverAcentos(Correcao.Trim());
        C_WebService ws = new C_WebService(Configuracao.PastaLog);
        NFe.NET.CartaCorrecao.TRetEnvEvento retCorrecao = ws.CartaCorrecao(nota, Correcao, Certificado, numLote, nSequencia, Configuracao.Ambiente, out procCorrecao);
        if (retCorrecao.retEvento[0].infEvento.cStat == "135")
        {
            SalvarEventoCorrecao(ref procCorrecao, nota.NFe.infNFe.ide.dEmi);
        }
        return retCorrecao;
    }

    public NFe.NET.Cancelamento.TRetEnvEvento Cancelar(string Justificativa, int numLote, int nSequencia, out NFe.NET.Cancelamento.TProcEvento procCancelamento)
    {
        if (NotasFiscais.Count > 1)
        {
            throw new InvalidOperationException("Só é possível cancelar uma nota por vez.");
        }
        TNfeProc nota = NotasFiscais[0];
        C_WebService ws = new C_WebService(Configuracao.PastaLog);
        Justificativa = Funcoes.RemoverAcentos(Justificativa.Trim());
        NFe.NET.Cancelamento.TRetEnvEvento retCanc = ws.CancelaNFe(nota, Justificativa, Certificado, numLote, nSequencia, Configuracao.Ambiente, out procCancelamento);
        if (retCanc.retEvento[0].infEvento.cStat == "135")
        {
            SalvarEventoCancelamento(ref procCancelamento, nota.NFe.infNFe.ide.dEmi);
        }
        return retCanc;
    }

    #region Inutilização

    public TRetInutNFe Inutilizar(int Inicio, int Final, int Serie, string Justificativa)
    {
        if (Configuracao == null)
        {
            return null;
        }

        if (String.IsNullOrEmpty(Configuracao.CodUF.ToString()))
        {
            return null;
        }

        if (String.IsNullOrEmpty(Configuracao.Versao))
        {
            return null;
        }

        TInutNFe Inutilizacao = new TInutNFe();

        Inutilizacao.infInut.ano = DateTime.Now.Year.ToString();
        Inutilizacao.infInut.CNPJ = Configuracao.Emitente.Item;
        Inutilizacao.infInut.mod = TMod.NotaFiscalEletronica;
        Inutilizacao.infInut.serie = Serie.ToString();
        Inutilizacao.infInut.nNFIni = Inicio.ToString();
        Inutilizacao.infInut.nNFFin = Final.ToString();


        string chave = Configuracao.CodUF + Inutilizacao.infInut.ano + Inutilizacao.infInut.CNPJ + Inutilizacao.infInut.mod + string.Format("{0:000}", Convert.ToInt32(Inutilizacao.infInut.serie)) + string.Format("{0:000000000}", Convert.ToInt32(Inutilizacao.infInut.nNFIni)) + string.Format("{0:000000000}", Convert.ToInt32(Inutilizacao.infInut.nNFFin));
        Inutilizacao.infInut.Id = "ID" + chave;
        Inutilizacao.infInut.xServ = TInutNFeInfInutXServ.INUTILIZAR;
        Inutilizacao.infInut.xJust = Justificativa;

        return Inutilizar(Inutilizacao);
    }

    private TRetInutNFe Inutilizar(TInutNFe Inutilizacao)
    {
        C_WebService ws = new C_WebService(Configuracao.PastaLog);
        return ws.InutilizaNumeracao(Inutilizacao, Certificado, Configuracao.Ambiente);
    }

    #endregion Inutilização

    #region Geração de Arquivo

    public void GerarArquivoNFe()
    {
        foreach (TNfeProc nota in NotasFiscais)
        {
            if ((nota.NFe.infNFe.emit == null) && (Configuracao.Emitente != null))
            {
                nota.NFe.infNFe.emit = Configuracao.Emitente;
            }

            System.DateTime dtEmissao = Convert.ToDateTime(nota.NFe.infNFe.ide.dEmi);
            string _codUF = nota.NFe.infNFe.ide.cUF.GetHashCode().ToString();
            string _dEmi = dtEmissao.ToString("yyMM");
            string _cnpj = removeFormatacao(nota.NFe.infNFe.emit.Item);
            string _mod = nota.NFe.infNFe.ide.mod.GetHashCode().ToString();
            string _serie = string.Format("{0:000}", Int32.Parse(nota.NFe.infNFe.ide.serie));
            string _numNF = string.Format("{0:000000000}", Int32.Parse(nota.NFe.infNFe.ide.nNF));
            string _codigo = string.Format("{0:00000000}", Convert.ToInt32(nota.NFe.infNFe.ide.nNF));
            string _tpEmissao = nota.NFe.infNFe.ide.tpEmis.GetHashCode().ToString();
            string chaveNF = _codUF + _dEmi + _cnpj + _mod + _serie + _numNF + _tpEmissao + _codigo;

            #region Validations

            if (String.IsNullOrEmpty(dtEmissao.ToString()))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta Data de Emissão");
            }

            if (string.IsNullOrEmpty(_codUF))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta CodUF do emissor");
            }

            if (string.IsNullOrEmpty(_dEmi))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta mes e ano da Data de Emissão");
            }

            if (string.IsNullOrEmpty(_cnpj))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta o CNPJ do Emissor");
            }

            if (string.IsNullOrEmpty(_mod))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta o modelo na Nota Fiscal");
            }

            if (string.IsNullOrEmpty(_serie))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta a série da Nota Fiscal");
            }

            if (string.IsNullOrEmpty(_numNF))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta o número da Nota Fiscal");
            }

            if (string.IsNullOrEmpty(_codigo))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta número da Nota Fiscal");
            }

            #endregion Validations

            int _dv = modulo11(chaveNF);
            nota.NFe.infNFe.Id = "NFe" + chaveNF + _dv.ToString();
            nota.NFe.infNFe.ide.cDV = _dv.ToString();
            nota.NFe.infNFe.ide.cNF = _codigo;
            string ArquivoNFe = Configuracao.PastaLog + chaveNF + _dv.ToString() + "-nfe.xml";

            nota.NFe.ArquivoXML = saveXml(nota.Serialize(), ArquivoNFe);
            nota.NFe.NomeArquivo = ArquivoNFe;
        }
    }
    
    public void GerarArquivoNFeProc()
    {
        foreach (TNfeProc nota in NotasFiscais)
        {
            if ((nota.NFe.infNFe.emit == null) && (Configuracao.Emitente != null))
            {
                nota.NFe.infNFe.emit = Configuracao.Emitente;
            }

            System.DateTime dtEmissao = Convert.ToDateTime(nota.NFe.infNFe.ide.dEmi);
            string _codUF = nota.NFe.infNFe.ide.cUF.GetHashCode().ToString();
            string _dEmi = dtEmissao.ToString("yyMM");
            string _cnpj = removeFormatacao(nota.NFe.infNFe.emit.Item);
            string _mod = nota.NFe.infNFe.ide.mod.GetHashCode().ToString();
            string _serie = string.Format("{0:000}", Int32.Parse(nota.NFe.infNFe.ide.serie));
            string _numNF = string.Format("{0:000000000}", Int32.Parse(nota.NFe.infNFe.ide.nNF));
            string _codigo = string.Format("{0:00000000}", Convert.ToInt32(nota.NFe.infNFe.ide.nNF)).Substring(0, 8);
            string _tpEmissao = nota.NFe.infNFe.ide.tpEmis.GetHashCode().ToString();
            string chaveNF = _codUF + _dEmi + _cnpj + _mod + _serie + _numNF + _tpEmissao + _codigo;

            #region Validations

            if (String.IsNullOrEmpty(dtEmissao.ToString()))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta Data de Emissão");
            }

            if (string.IsNullOrEmpty(_codUF))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta CodUF do emissor");
            }

            if (string.IsNullOrEmpty(_dEmi))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta mes e ano da Data de Emissão");
            }

            if (string.IsNullOrEmpty(_cnpj))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta o CNPJ do Emissor");
            }

            if (string.IsNullOrEmpty(_mod))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta o modelo na Nota Fiscal");
            }

            if (string.IsNullOrEmpty(_serie))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta a série da Nota Fiscal");
            }

            if (string.IsNullOrEmpty(_numNF))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta o número da Nota Fiscal");
            }

            if (string.IsNullOrEmpty(_codigo))
            {
                throw new Exception("Chave inválida, não é possível calcular o digito verificador: Falta número da Nota Fiscal");
            }

            #endregion Validations

            int _dv = modulo11(chaveNF);
            nota.protNFe.infProt = new TProtNFeInfProt();
            nota.protNFe.infProt.nProt = "Nao autorizado";
            nota.protNFe.infProt.cStat = "000";
            nota.protNFe.infProt.chNFe = chaveNF + _dv.ToString();
            nota.NFe.infNFe.Id = "NFe" + chaveNF + _dv.ToString();
            nota.NFe.infNFe.ide.cDV = _dv.ToString();
            nota.NFe.infNFe.ide.cNF = _codigo;
            nota.NFe.infNFe.infAdic.infCpl = Funcoes.RemoverAcentos(nota.NFe.infNFe.infAdic.infCpl);
            string ArquivoNFe = Configuracao.PastaLog + chaveNF + _dv.ToString() + "-nfe.xml";

            nota.NFe.ArquivoXML = saveXml(nota.Serialize(), ArquivoNFe); ;
            nota.NFe.NomeArquivo = ArquivoNFe;
        }
    }

    #endregion Geração de Arquivo

    #region Assinatura

    public void Assinar()
    {
        foreach (TNfeProc nota in NotasFiscais)
        {
            if (Certificado == null)
            {
                throw new Exception("O Certificado Digital não está Configurado.");
            }
            nota.NFe = AssinarNFE(nota.NFe, "infNFe");
        }
    }

    public void Assinar(ref TNfeProc nota)
    {
        if (Certificado == null)
        {
            throw new Exception("O Certificado Digital não está Configurado.");
        }
        nota.NFe = AssinarNFE(nota.NFe, "infNFe");
    }

    public TNFe AssinarNFE(TNFe nota, string pUri)
    {
        XmlDocument arquivoNaoAssinado = new XmlDocument();
        arquivoNaoAssinado.LoadXml(Funcoes.RemoveNameSpaceFromXml(nota.Serialize()));

        XmlDocument auxDocXML = CertificadoDigital.Assinar(arquivoNaoAssinado, pUri, Certificado);
        auxDocXML.Save(nota.NomeArquivo);

        TNFe retorno = TNFe.LoadFromFile(nota.NomeArquivo);
        retorno.NomeArquivo = nota.NomeArquivo;
        retorno.ArquivoXML = auxDocXML;
        return retorno;
    }

    #endregion Assinatura

    #region Email

    public List<String> EnviarEmail()
    {
        if (String.IsNullOrWhiteSpace(Configuracao.MensagemEmail))
        {
            throw new Exception("Erro ao enviar o email. Motivo: Corpo de email não configurado");
        }

        List<string> lstMsgErroEmail = new List<string>();

        foreach (TNfeProc nota in NotasFiscais)
        {
            if (!String.IsNullOrEmpty(nota.Email))
            {
                try
                {
                    enviaMensagemEmail(nota, nota.Email, "", "", Configuracao.AssuntoEmail, String.Format(Configuracao.MensagemEmail, nota.NFe.infNFe.ide.nNF));
                }
                catch (Exception ex)
                {
                    lstMsgErroEmail.Add("Nota [" + nota.NFe.infNFe.ide.nNF + "] e Destinatário [" + nota.Email + "]: " + ex.Message);
                }
            }
            else
            {
                lstMsgErroEmail.Add("Nota [" + nota.NFe.infNFe.ide.nNF + "] e Destinatário [" + nota.Email + "]: Sem destinatário.");
            }
        }

        return lstMsgErroEmail;
    }

    public List<String> EnviarEmail(string Destinatario)
    {
        if (String.IsNullOrWhiteSpace(Configuracao.MensagemEmail))
        {
            throw new Exception("Erro ao enviar o email. Motivo: Corpo de email não configurado");
        }

        List<string> lstMsgErroEmail = new List<string>();

        foreach (TNfeProc nota in NotasFiscais)
        {
            if (!String.IsNullOrEmpty(Destinatario))
            {
                try
                {
                    enviaMensagemEmail(nota, Destinatario, "", "", Configuracao.AssuntoEmail, String.Format(Configuracao.MensagemEmail, nota.NFe.infNFe.ide.nNF));
                }
                catch (Exception ex)
                {
                    lstMsgErroEmail.Add("Nota [" + nota.NFe.infNFe.ide.nNF + "] e Destinatário [" + Destinatario + "]: " + ex.Message);
                }
            }
            else
            {
                lstMsgErroEmail.Add("Nota [" + nota.NFe.infNFe.ide.nNF + "] e Destinatário [" + Destinatario + "]: Sem destinatário.");
            }
        }

        return lstMsgErroEmail;
    }

    private void enviaMensagemEmail(TNfeProc Nota, string Destinatario, string bcc, string cc, string Assunto, string Corpo)
    {
        if (string.IsNullOrEmpty(Configuracao.ConfiguracaoMail.SMTPServer))
        {
            throw new Exception("É necessário preencher a propriedade ConfiguracaoEmail para enviar email");
            //return;
        }

        MailMessage mMailMessage = new MailMessage();
        mMailMessage.From = new MailAddress(Configuracao.ConfiguracaoMail.Remetente);

        string[] arrDestinatarios = Destinatario.Split(';');

        foreach (var email in arrDestinatarios)
        {
            if (!String.IsNullOrEmpty(email))
            {
                mMailMessage.To.Add(new MailAddress(email.Trim()));
            }
        }

        if ((bcc != null) & bcc != string.Empty)
        {
            mMailMessage.Bcc.Add(new MailAddress(bcc));
        }

        if ((cc != null) & cc != string.Empty)
        {
            mMailMessage.CC.Add(new MailAddress(cc));
        }

        mMailMessage.Subject = Assunto;
        mMailMessage.Body = Corpo;
        mMailMessage.IsBodyHtml = Configuracao.ConfiguracaoMail.CorpoHtml;
        mMailMessage.Priority = MailPriority.Normal;

        string servidorSMTP = Configuracao.ConfiguracaoMail.SMTPServer;
        SmtpClient mSmtpClient = new SmtpClient(servidorSMTP);

        //Configurar a conta de envio
        mSmtpClient.Port = Configuracao.ConfiguracaoMail.Port;
        mSmtpClient.EnableSsl = Configuracao.ConfiguracaoMail.HabilitaSSL;
        string Usuario = Configuracao.ConfiguracaoMail.Usuario;
        string Senha = Configuracao.ConfiguracaoMail.Senha;
        mSmtpClient.Credentials = new System.Net.NetworkCredential(Usuario, Senha);
        //mSmtpClient.UseDefaultCredentials = True
        mMailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;

        //Anexa o XML
        Attachment anexo = new Attachment(Nota.NFe.NomeArquivo);
        mMailMessage.Attachments.Add(anexo);

        //Anexa o PDF
        string arquivoPDF = GeraPDF(Nota);
        Attachment anexoPDF = new Attachment(arquivoPDF);
        mMailMessage.Attachments.Add(anexoPDF);

        //Enviar email
        try
        {
            mSmtpClient.Send(mMailMessage);
        }
        catch (Exception Ex)
        {

            throw new Exception("Erro ao enviar o email da nota: " + Nota.NFe.infNFe.ide.nNF + " para: " + Destinatario + ". Motivo: " + Ex.Message);
        }

        //File.Delete(arquivoPDF);
    }

    public void EnviarEmailCartaCorrecao(string Destinatario, NFe.NET.CartaCorrecao.TProcEvento evento)
    {
        if (String.IsNullOrWhiteSpace(Configuracao.MensagemEmail))
        {
            throw new Exception("Erro ao enviar o email. Motivo: Corpo de email não configurado");
        }

        foreach (TNfeProc nota in NotasFiscais)
        {
            enviaMensagemEmailCartaCorrecao(nota, evento, Destinatario, "", "", Configuracao.AssuntoEmail, String.Format(Configuracao.MensagemEmail, nota.NFe.infNFe.ide.nNF));
        }
    }

    private void enviaMensagemEmailCartaCorrecao(TNfeProc Nota, NFe.NET.CartaCorrecao.TProcEvento Evento, string Destinatario, string bcc, string cc, string Assunto, string Corpo)
    {
        if (string.IsNullOrEmpty(Configuracao.ConfiguracaoMail.SMTPServer))
        {
            throw new Exception("É necessário preencher a propriedade ConfiguracaoEmail para enviar email");
            //return;
        }

        MailMessage mMailMessage = new MailMessage();
        mMailMessage.From = new MailAddress(Configuracao.ConfiguracaoMail.Remetente);

        string[] arrDestinatarios = Destinatario.Split(';');

        foreach (var email in arrDestinatarios)
        {
            if (!String.IsNullOrEmpty(email))
            {
                mMailMessage.To.Add(new MailAddress(email.Trim()));
            }
        }

        if ((bcc != null) & bcc != string.Empty)
        {
            mMailMessage.Bcc.Add(new MailAddress(bcc));
        }

        if ((cc != null) & cc != string.Empty)
        {
            mMailMessage.CC.Add(new MailAddress(cc));
        }

        mMailMessage.Subject = Assunto;
        mMailMessage.Body = Corpo;
        mMailMessage.IsBodyHtml = Configuracao.ConfiguracaoMail.CorpoHtml;
        mMailMessage.Priority = MailPriority.Normal;

        string servidorSMTP = Configuracao.ConfiguracaoMail.SMTPServer;
        SmtpClient mSmtpClient = new SmtpClient(servidorSMTP);

        //Configurar a conta de envio
        mSmtpClient.Port = Configuracao.ConfiguracaoMail.Port;
        mSmtpClient.EnableSsl = Configuracao.ConfiguracaoMail.HabilitaSSL;
        string Usuario = Configuracao.ConfiguracaoMail.Usuario;
        string Senha = Configuracao.ConfiguracaoMail.Senha;
        mSmtpClient.Credentials = new System.Net.NetworkCredential(Usuario, Senha);
        //mSmtpClient.UseDefaultCredentials = True
        mMailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;

        //Anexa o XML
        Attachment anexo = new Attachment(Evento.NomeArquivo);
        mMailMessage.Attachments.Add(anexo);

        //Anexa o PDF
        string arquivoPDF = GeraPDFCartaCorrecao(Nota, Evento);
        Attachment anexoPDF = new Attachment(arquivoPDF);
        mMailMessage.Attachments.Add(anexoPDF);

        //Enviar email
        try
        {
            mSmtpClient.Send(mMailMessage);
        }
        catch (Exception Ex)
        {
            throw new Exception("Erro ao enviar o email do evento: " + Evento.retEvento.infEvento.chNFe + " para: " + Destinatario + ". Motivo: " + Ex.Message);
        }

        //File.Delete(arquivoPDF);
    }

    #endregion Email

    #region Validação

    public List<string> Validar()
    {
        List<string> ResultValidacao = new List<string>();
        foreach (TNfeProc nota in NotasFiscais)
        {
            string retorno = null;
            string AbsolutePath = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            string SchemasPath = Path.GetDirectoryName(AbsolutePath) + "\\Schemas\\nfe_v2.00.xsd";
            retorno = ValidarXML(nota.NFe.NomeArquivo, SchemasPath);
            if (!string.IsNullOrEmpty(retorno))
            {
                ResultValidacao.Add("Nota Fiscal: " + nota.NFe.infNFe.ide.nNF + " - " + retorno);
            }
        }
        return ResultValidacao;
    }

    public static string ValidarXML(string ArquivoXML, string SchemaNf)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ValidationEventHandler += new ValidationEventHandler(ValidationEventHandler);

        if (!File.Exists(ArquivoXML))
        {
            return "Arquivo da nota fiscal não encontrado.";
        }

        if (!File.Exists(SchemaNf))
        {
            return "Arquivo de Schema não encontrado.";
        }

        try
        {
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas.Add("http://www.portalfiscal.inf.br/nfe", XmlReader.Create(SchemaNf));
            using (XmlReader XmlValidatingReader = XmlReader.Create(ArquivoXML, settings))
            {
                while (XmlValidatingReader.Read())
                {
                }
            }
        }
        catch (Exception ex)
        {
            return ex.Message;

        }

        if (Erros != null)
        {
            string retorno = String.Join(String.Empty, Erros);
            Erros = null;
            return retorno;
        }

        return string.Empty;
    }

    public static void ValidationEventHandler(object sender, ValidationEventArgs args)
    {
        Erros = new List<string>();
        if (args.Severity == XmlSeverityType.Warning)
        {
            Erros.Add("Nenhum arquivo de Schema foi encontrado para efetuar a validação...");
        }
        else if (args.Severity == XmlSeverityType.Error)
        {
            Erros.Add("Ocorreu um erro durante a validação....");
        }
        // Erro na validação do schema XSD
        if ((args.Exception != null))
        {
            Erros.Add("\nErro: " + args.Exception.Message + "\nLinha " + args.Exception.LinePosition + " - Coluna "
                               + args.Exception.LineNumber + "\nSource: " + args.Exception.SourceUri);
        }
    }

    #endregion Validação

    #region Impressão

    public ReportDocument ImprimirDANFE(TNfeProc nota)
    {
        XDocument doc = XDocument.Load(nota.NFe.NomeArquivo);

        #region Validations

        string ns = "{http://www.portalfiscal.inf.br/nfe}";
        foreach (XElement det in doc.Descendants(ns + "det"))
        {
            XElement imposto = det.Element(ns + "imposto");
            XElement ICMS = imposto.Element(ns + "ICMS");

            if (ICMS.Element(ns + "ICMS00") == null)
            {
                XElement ICMS00 = new XElement(ns + "ICMS00");
                ICMS00.Add(new XElement(ns + "orig", ""));
                ICMS00.Add(new XElement(ns + "CST", ""));
                ICMS00.Add(new XElement(ns + "vBC", ""));
                ICMS00.Add(new XElement(ns + "vICMS", ""));
                ICMS00.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMS00);
            }

            if (ICMS.Element(ns + "ICMS10") == null)
            {
                XElement ICMS10 = new XElement(ns + "ICMS10");
                ICMS10.Add(new XElement(ns + "orig", ""));
                ICMS10.Add(new XElement(ns + "CST", ""));
                ICMS10.Add(new XElement(ns + "vBC", ""));
                ICMS10.Add(new XElement(ns + "vICMS", ""));
                ICMS10.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMS10);
            }

            if (ICMS.Element(ns + "ICMS20") == null)
            {
                XElement ICMS20 = new XElement(ns + "ICMS20");
                ICMS20.Add(new XElement(ns + "orig", ""));
                ICMS20.Add(new XElement(ns + "CST", ""));
                ICMS20.Add(new XElement(ns + "vBC", ""));
                ICMS20.Add(new XElement(ns + "vICMS", ""));
                ICMS20.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMS20);
            }

            if (ICMS.Element(ns + "ICMS30") == null)
            {
                XElement ICMS30 = new XElement(ns + "ICMS30");
                ICMS30.Add(new XElement(ns + "orig", ""));
                ICMS30.Add(new XElement(ns + "CST", ""));

                ICMS.Add(ICMS30);
            }

            if (ICMS.Element(ns + "ICMS40") == null)
            {
                XElement ICMS40 = new XElement(ns + "ICMS40");
                ICMS40.Add(new XElement(ns + "orig", ""));
                ICMS40.Add(new XElement(ns + "CST", ""));

                ICMS.Add(ICMS40);
            }

            if (ICMS.Element(ns + "ICMS51") == null)
            {
                XElement ICMS51 = new XElement(ns + "ICMS51");
                ICMS51.Add(new XElement(ns + "orig", ""));
                ICMS51.Add(new XElement(ns + "CST", ""));
                ICMS51.Add(new XElement(ns + "vBC", ""));
                ICMS51.Add(new XElement(ns + "vICMS", ""));
                ICMS51.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMS51);
            }

            if (ICMS.Element(ns + "ICMS60") == null)
            {
                XElement ICMS60 = new XElement(ns + "ICMS60");
                ICMS60.Add(new XElement(ns + "orig", ""));
                ICMS60.Add(new XElement(ns + "CST", ""));

                ICMS.Add(ICMS60);
            }

            if (ICMS.Element(ns + "ICMS70") == null)
            {
                XElement ICMS70 = new XElement(ns + "ICMS70");
                ICMS70.Add(new XElement(ns + "orig", ""));
                ICMS70.Add(new XElement(ns + "CST", ""));
                ICMS70.Add(new XElement(ns + "vBC", ""));
                ICMS70.Add(new XElement(ns + "vICMS", ""));
                ICMS70.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMS70);
            }

            if (ICMS.Element(ns + "ICMS90") == null)
            {
                XElement ICMS90 = new XElement(ns + "ICMS90");
                ICMS90.Add(new XElement(ns + "orig", ""));
                ICMS90.Add(new XElement(ns + "CST", ""));
                ICMS90.Add(new XElement(ns + "vBC", ""));
                ICMS90.Add(new XElement(ns + "vICMS", ""));
                ICMS90.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMS90);
            }

            if (ICMS.Element(ns + "ICMSPart") == null)
            {
                XElement ICMSPart = new XElement(ns + "ICMSPart");
                ICMSPart.Add(new XElement(ns + "orig", ""));
                ICMSPart.Add(new XElement(ns + "CST", ""));
                ICMSPart.Add(new XElement(ns + "vBC", ""));
                ICMSPart.Add(new XElement(ns + "vICMS", ""));
                ICMSPart.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMSPart);
            }

            if (ICMS.Element(ns + "ICMSSN101") == null)
            {
                XElement ICMSSN101 = new XElement(ns + "ICMSSN101");
                ICMSSN101.Add(new XElement(ns + "orig", ""));
                ICMSSN101.Add(new XElement(ns + "CSOSN", ""));

                ICMS.Add(ICMSSN101);
            }

            if (ICMS.Element(ns + "ICMSSN102") == null)
            {
                XElement ICMSSN102 = new XElement(ns + "ICMSSN102");
                ICMSSN102.Add(new XElement(ns + "orig", ""));
                ICMSSN102.Add(new XElement(ns + "CSOSN", ""));

                ICMS.Add(ICMSSN102);
            }

            if (ICMS.Element(ns + "ICMSSN201") == null)
            {
                XElement ICMSSN201 = new XElement(ns + "ICMSSN201");
                ICMSSN201.Add(new XElement(ns + "orig", ""));
                ICMSSN201.Add(new XElement(ns + "CSOSN", ""));

                ICMS.Add(ICMSSN201);
            }

            if (ICMS.Element(ns + "ICMSSN202") == null)
            {
                XElement ICMSSN202 = new XElement(ns + "ICMSSN202");
                ICMSSN202.Add(new XElement(ns + "orig", ""));
                ICMSSN202.Add(new XElement(ns + "CSOSN", ""));

                ICMS.Add(ICMSSN202);
            }

            if (ICMS.Element(ns + "ICMSSN500") == null)
            {
                XElement ICMSSN500 = new XElement(ns + "ICMSSN500");
                ICMSSN500.Add(new XElement(ns + "orig", ""));
                ICMSSN500.Add(new XElement(ns + "CSOSN", ""));

                ICMS.Add(ICMSSN500);
            }

            if (ICMS.Element(ns + "ICMSSN900") == null)
            {
                XElement ICMSSN900 = new XElement(ns + "ICMSSN900");
                ICMSSN900.Add(new XElement(ns + "orig", ""));
                ICMSSN900.Add(new XElement(ns + "CSOSN", ""));
                ICMSSN900.Add(new XElement(ns + "vBC", ""));
                ICMSSN900.Add(new XElement(ns + "vICMS", ""));
                ICMSSN900.Add(new XElement(ns + "pICMS", ""));

                ICMS.Add(ICMSSN900);
            }

            if (ICMS.Element(ns + "ICMSST") == null)
            {
                XElement ICMSST = new XElement(ns + "ICMSST");
                ICMSST.Add(new XElement(ns + "orig", ""));
                ICMSST.Add(new XElement(ns + "CST", ""));

                ICMS.Add(ICMSST);
            }

            if (ICMS.Element(ns + "ISSQN") == null)
            {
                XElement ISSQN = new XElement(ns + "ISSQN");
                ISSQN.Add(new XElement(ns + "ISSQN", ""));

                ICMS.Add(ISSQN);
            }

            XElement IPI = imposto.Element(ns + "IPI");
            if (IPI == null)
            {
                IPI = new XElement(ns + "IPI");
                imposto.Add(IPI);
            }
            if (IPI.Element(ns + "IPITrib") == null)
            {
                XElement IPITrib = new XElement(ns + "IPITrib");
                IPITrib.Add(new XElement(ns + "pIPI", "0.00"));
                IPITrib.Add(new XElement(ns + "vIPI", "0.00"));
                IPI.Add(IPITrib);
            }

            if (det.Element(ns + "infAdProd") == null)
            {
                det.Add(new XElement(ns + "infAdProd", ""));
            }

            if (det.Element(ns + "prod").Element(ns + "vDesc") == null)
            {
                det.Element(ns + "prod").Add(new XElement(ns + "vDesc", "0.00"));
            }
        }

        #endregion Validations

        DataSet dataSet = new DataSet();
        dataSet.ReadXml(doc.CreateReader());

        #region Validations

        if (dataSet.Tables["ide"].Columns["dSaiEnt"] == null) { dataSet.Tables["ide"].Columns.Add("dSaiEnt"); }
        if (dataSet.Tables["ide"].Columns["hSaiEnt"] == null) { dataSet.Tables["ide"].Columns.Add("hSaiEnt"); }

        if (dataSet.Tables["emit"].Columns["CPF"] == null) { dataSet.Tables["emit"].Columns.Add("CPF"); }
        if (dataSet.Tables["emit"].Columns["CNPJ"] == null) { dataSet.Tables["emit"].Columns.Add("CNPJ"); }
        if (dataSet.Tables["emit"].Columns["IEST"] == null) { dataSet.Tables["emit"].Columns.Add("IEST"); }

        if (dataSet.Tables["enderEmit"].Columns["xCpl"] == null) { dataSet.Tables["enderEmit"].Columns.Add("xCpl"); }
        if (dataSet.Tables["enderEmit"].Columns["CEP"] == null) { dataSet.Tables["enderEmit"].Columns.Add("CEP"); }
        if (dataSet.Tables["enderEmit"].Columns["fone"] == null) { dataSet.Tables["enderEmit"].Columns.Add("fone"); }

        if (dataSet.Tables["dest"].Columns["CPF"] == null) { dataSet.Tables["dest"].Columns.Add("CPF"); }
        if (dataSet.Tables["dest"].Columns["CNPJ"] == null) { dataSet.Tables["dest"].Columns.Add("CNPJ"); }
        if (dataSet.Tables["dest"].Columns["email"] == null) { dataSet.Tables["dest"].Columns.Add("email"); }

        if (dataSet.Tables["enderDest"].Columns["xCpl"] == null) { dataSet.Tables["enderDest"].Columns.Add("xCpl"); }
        if (dataSet.Tables["enderDest"].Columns["CEP"] == null) { dataSet.Tables["enderDest"].Columns.Add("CEP"); }
        if (dataSet.Tables["enderDest"].Columns["fone"] == null) { dataSet.Tables["enderDest"].Columns.Add("fone"); }

        if (dataSet.Tables["ISSQNTot"] == null) { dataSet.Tables.Add("ISSQNTot"); }
        if (dataSet.Tables["retTrib"] == null) { dataSet.Tables.Add("retTrib"); }

        if (dataSet.Tables["transporta"] == null) { dataSet.Tables.Add("transporta"); }
        if (dataSet.Tables["transporta"].Columns["CPF"] == null) { dataSet.Tables["transporta"].Columns.Add("CPF"); }
        if (dataSet.Tables["transporta"].Columns["CNPJ"] == null) { dataSet.Tables["transporta"].Columns.Add("CNPJ"); }
        if (dataSet.Tables["transporta"].Columns["IE"] == null) { dataSet.Tables["transporta"].Columns.Add("IE"); }
        if (dataSet.Tables["transporta"].Columns["xNome"] == null) { dataSet.Tables["transporta"].Columns.Add("xNome"); }
        if (dataSet.Tables["transporta"].Columns["xEnder"] == null) { dataSet.Tables["transporta"].Columns.Add("xEnder"); }
        if (dataSet.Tables["transporta"].Columns["xMun"] == null) { dataSet.Tables["transporta"].Columns.Add("xMun"); }
        if (dataSet.Tables["transporta"].Columns["UF"] == null) { dataSet.Tables["transporta"].Columns.Add("UF"); }

        if (dataSet.Tables["veicTransp"] == null) { dataSet.Tables.Add("veicTransp"); }
        if (dataSet.Tables["veicTransp"].Columns["transp_Id"] == null) { dataSet.Tables["veicTransp"].Columns.Add("transp_Id"); }
        if (dataSet.Tables["veicTransp"].Columns["RNTC"] == null) { dataSet.Tables["veicTransp"].Columns.Add("RNTC"); }
        if (dataSet.Tables["veicTransp"].Columns["placa"] == null) { dataSet.Tables["veicTransp"].Columns.Add("placa"); }
        if (dataSet.Tables["veicTransp"].Columns["UF"] == null) { dataSet.Tables["veicTransp"].Columns.Add("UF"); }

        if (dataSet.Tables["vol"] == null) { dataSet.Tables.Add("vol"); }
        if (dataSet.Tables["vol"].Columns["qVol"] == null) { dataSet.Tables["vol"].Columns.Add("qVol"); }
        if (dataSet.Tables["vol"].Columns["esp"] == null) { dataSet.Tables["vol"].Columns.Add("esp"); }
        if (dataSet.Tables["vol"].Columns["marca"] == null) { dataSet.Tables["vol"].Columns.Add("marca"); }
        if (dataSet.Tables["vol"].Columns["nVol"] == null) { dataSet.Tables["vol"].Columns.Add("nVol"); }
        if (dataSet.Tables["vol"].Columns["pesoL"] == null) { dataSet.Tables["vol"].Columns.Add("pesoL"); }
        if (dataSet.Tables["vol"].Columns["pesoB"] == null) { dataSet.Tables["vol"].Columns.Add("pesoB"); }

        if (dataSet.Tables["cobr"] == null) { dataSet.Tables.Add("cobr"); }
        if (dataSet.Tables["fat"] == null) { dataSet.Tables.Add("fat"); }

        if (dataSet.Tables["dup"] == null)
        {
            dataSet.Tables.Add("dup");
            dataSet.Tables["dup"].Columns.Add("nDup");
            dataSet.Tables["dup"].Columns.Add("dVenc");
            dataSet.Tables["dup"].Columns.Add("vDup");
        }

        if (dataSet.Tables["entrega"] == null)
        {
            dataSet.Tables.Add("entrega");
            dataSet.Tables["entrega"].Columns.Add("CNPJ");
            dataSet.Tables["entrega"].Columns.Add("CPF");
            dataSet.Tables["entrega"].Columns.Add("xLog");
            dataSet.Tables["entrega"].Columns.Add("nro");
            dataSet.Tables["entrega"].Columns.Add("xCpl");
            dataSet.Tables["entrega"].Columns.Add("xBairro");
            dataSet.Tables["entrega"].Columns.Add("cMun");
            dataSet.Tables["entrega"].Columns.Add("xMun");
            dataSet.Tables["entrega"].Columns.Add("UF");
        }

        if (dataSet.Tables["infProt"] == null) { dataSet.Tables.Add("infProt"); }
        if (dataSet.Tables["infProt"].Columns["nProt"] == null) { dataSet.Tables["infProt"].Columns.Add("nProt"); }
        if (dataSet.Tables["infProt"].Columns["chNFe"] == null) { dataSet.Tables["infProt"].Columns.Add("chNFe"); }
        if (dataSet.Tables["infProt"].Columns["dhRecbto"] == null) { dataSet.Tables["infProt"].Columns.Add("dhRecbto"); }
        if (dataSet.Tables["infProt"].Columns["cStat"] == null) { dataSet.Tables["infProt"].Columns.Add("cStat"); }

        if (dataSet.Tables["infAdic"] == null) { dataSet.Tables.Add("infAdic"); }
        if (dataSet.Tables["infAdic"].Columns["infCpl"] == null) { dataSet.Tables["infAdic"].Columns.Add("infCpl"); }
        if (dataSet.Tables["infAdic"].Columns["infAdFisco"] == null) { dataSet.Tables["infAdic"].Columns.Add("infAdFisco"); }

        if (dataSet.Tables["obsCont"] == null) { dataSet.Tables.Add("obsCont"); }
        if (dataSet.Tables["obsFisco"] == null) { dataSet.Tables.Add("obsFisco"); }

        #endregion Validations

        DANFE danfe = new DANFE();
        danfe.Load(Path.Combine(Environment.CurrentDirectory, "DANFE.rpt"), OpenReportMethod.OpenReportByDefault);
        NFe.NET.DANFE.ImageDataSet imageds = new NFe.NET.DANFE.ImageDataSet();
        imageds.Images.AddImagesRow(Logo(), Barras(nota.NFe.infNFe.Id.Substring(3)), Configuracao.Site, Configuracao.CasasDecimaisQtd, Configuracao.CasasDecimaisValorUnitario);
        danfe.SetDataSource(dataSet);
        danfe.Database.Tables["Images"].SetDataSource(imageds);
        return danfe;
    }

    public ReportDocument ImprimeCartaCorrecao(TNfeProc nfe, NFe.NET.CartaCorrecao.TProcEvento evento)
    {
        XDocument doc = XDocument.Load(evento.NomeArquivo);
        DataSet dataSet = new DataSet();
        dataSet.ReadXml(doc.CreateReader());

        CartaCorrecao cartaCorrecao = new CartaCorrecao();
        cartaCorrecao.Load(Path.Combine(Environment.CurrentDirectory, "CartaCorrecao.rpt"), OpenReportMethod.OpenReportByDefault);
        NFe.NET.ImpressaoCartaCorrecao.ImageDataSet imageds = new NFe.NET.ImpressaoCartaCorrecao.ImageDataSet();
        imageds.Images.AddImagesRow(Logo(), Barras(evento.retEvento.infEvento.chNFe), Configuracao.Site);


        NFe.NET.ImpressaoCartaCorrecao.ImageDataSet.CartaCorrecaoRow row = ((NFe.NET.ImpressaoCartaCorrecao.ImageDataSet.CartaCorrecaoRow)(imageds.CartaCorrecao.NewRow()));

        #region Nota Fiscal Eletrônica
        row.Modelo = Funcoes.ConvertEnumToString(nfe.NFe.infNFe.ide.mod);
        row.Serie = nfe.NFe.infNFe.ide.serie;
        row.Numero = nfe.NFe.infNFe.ide.nNF;
        row.MesAnoEmissao = nfe.NFe.infNFe.ide.dEmi.Substring(5, 2) + "/" + nfe.NFe.infNFe.ide.dEmi.Substring(2, 2);
        row.ChaveAcesso = nfe.protNFe.infProt.chNFe;
        #endregion Nota Fiscal Eletrônica

        #region Carta de Correção Eletrônica
        row.Orgao = Funcoes.ConvertEnumToString(evento.evento.infEvento.cOrgao);
        row.Ambiente = Funcoes.ConvertEnumToString(evento.evento.infEvento.tpAmb);
        row.DataHoraEvento = evento.evento.infEvento.dhEvento;
        row.Evento = Funcoes.ConvertEnumToString(evento.evento.infEvento.tpEvento);
        row.DescricaoEvento = Funcoes.ConvertEnumToString(evento.evento.infEvento.detEvento.descEvento);
        row.SequenciaEvento = evento.evento.infEvento.nSeqEvento;
        row.VersaoEvento = Funcoes.ConvertEnumToString(evento.evento.infEvento.verEvento);
        row.Status = evento.retEvento.infEvento.cStat + " - " + evento.retEvento.infEvento.xMotivo;
        row.Protocolo = evento.retEvento.infEvento.nProt;
        row.DataHoraRegistro = evento.retEvento.infEvento.dhRegEvento;
        #endregion Carta de Correção Eletrônica

        #region Emitente
        row.RazaoSocial_Emit = nfe.NFe.infNFe.emit.xNome;
        row.CNPJCPF_Emit = nfe.NFe.infNFe.emit.Item;
        row.Endereco_Emit = nfe.NFe.infNFe.emit.enderEmit.xLgr + " " + nfe.NFe.infNFe.emit.enderEmit.nro + " " + nfe.NFe.infNFe.emit.enderEmit.xCpl;
        row.Bairro_Emit = nfe.NFe.infNFe.emit.enderEmit.xBairro;
        row.CEP_Emit = nfe.NFe.infNFe.emit.enderEmit.CEP;
        row.Municipio_Emit = nfe.NFe.infNFe.emit.enderEmit.xMun;
        row.FoneFax_Emit = nfe.NFe.infNFe.emit.enderEmit.fone;
        row.Estado_Emit = nfe.NFe.infNFe.emit.enderEmit.UF.ToString();
        row.IE_Emit = nfe.NFe.infNFe.emit.IE;
        #endregion Emitente

        #region Destinatário / Remetente
        row.RazaoSocial_Dest = nfe.NFe.infNFe.dest.xNome;
        row.CNPJCPF_Dest = nfe.NFe.infNFe.dest.Item;
        row.Endereco_Dest = nfe.NFe.infNFe.dest.enderDest.xLgr + " " + nfe.NFe.infNFe.dest.enderDest.nro + " " + nfe.NFe.infNFe.dest.enderDest.xCpl;
        row.Bairro_Dest = nfe.NFe.infNFe.dest.enderDest.xBairro;
        row.CEP_Dest = nfe.NFe.infNFe.dest.enderDest.CEP;
        row.Municipio_Dest = nfe.NFe.infNFe.dest.enderDest.xMun;
        row.FoneFax_Dest = nfe.NFe.infNFe.dest.enderDest.fone;
        row.Estado_Dest = nfe.NFe.infNFe.dest.enderDest.UF.ToString();
        row.IE_Dest = nfe.NFe.infNFe.dest.IE;
        #endregion Destinatário / Remetente

        row.CondicoesUso = Funcoes.ConvertEnumToString(evento.evento.infEvento.detEvento.xCondUso);
        row.Correcao = evento.evento.infEvento.detEvento.xCorrecao;

        imageds.CartaCorrecao.AddCartaCorrecaoRow(row);
        cartaCorrecao.SetDataSource(dataSet);
        cartaCorrecao.Database.Tables["Images"].SetDataSource(imageds);
        return cartaCorrecao;
    }

    #endregion Impressão

    #region PDF

    public string GeraPDF(TNfeProc nota)
    {
        try
        {
            string arquivoPDF = nota.NFe.NomeArquivo.Replace(".xml", ".pdf");
            ReportDocument danfe = ImprimirDANFE(nota);
            danfe.ExportToDisk(ExportFormatType.PortableDocFormat, arquivoPDF);
            return arquivoPDF;
        }
        catch (Exception ex)
        {
            throw new Exception("Erro ao gerar o arquivo do DANFE: " + ex.Message);
        }
    }

    public string GeraPDFCartaCorrecao(TNfeProc Nota, NFe.NET.CartaCorrecao.TProcEvento evento)
    {
        try
        {
            string arquivoPDF = evento.NomeArquivo.Replace(".xml", ".pdf");
            ReportDocument danfe = ImprimeCartaCorrecao(Nota, evento);
            danfe.ExportToDisk(ExportFormatType.PortableDocFormat, arquivoPDF);
            return arquivoPDF;
        }
        catch (Exception ex)
        {
            throw new Exception("Erro ao gerar o arquivo do DANFE: " + ex.Message);
        }
    }

    #endregion PDF

    #region Auxiliares

    public static int modulo11(string chaveNFE)
    {
        if (chaveNFE.Length != 43)
        {
            throw new Exception("Chave inválida, não é possível calcular o digito verificador");
        }


        string baseCalculo = "4329876543298765432987654329876543298765432";
        int somaResultados = 0;

        for (int i = 0; i <= chaveNFE.Length - 1; i++)
        {
            int numNF = Convert.ToInt32(chaveNFE[i].ToString());
            int numBaseCalculo = Convert.ToInt32(baseCalculo[i].ToString());

            somaResultados += (numBaseCalculo * numNF);
        }

        int restoDivisao = (somaResultados % 11);
        int dv = 11 - restoDivisao;
        if ((dv == 0) || (dv > 9))
        {
            return 0;
        }
        else
        {
            return dv;
        }
    }

    private string removeFormatacao(string texto)
    {
        return string.Join(null, System.Text.RegularExpressions.Regex.Split(texto, "[^\\d]"));
    }

    public static MemoryStream StringXmlToStreamUTF8(string strXml)
    {
        byte[] byteArray = new byte[strXml.Length];
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
        byteArray = encoding.GetBytes(strXml);
        MemoryStream memoryStream = new MemoryStream(byteArray);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

    private byte[] Logo()
    {
        if (File.Exists(this.Configuracao.LogoDanfe))
        {
            return File.ReadAllBytes(this.Configuracao.LogoDanfe);
        }
        else
        {
            string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            string caminhoImagem = Path.GetDirectoryName(path) + "/Resources/Imagens/logo.jpg";
            return File.ReadAllBytes(caminhoImagem);
        }
    }

    private byte[] Barras(string chNFe)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (System.Drawing.Image barra = (new Zen.Barcode.Code128BarcodeDraw(Zen.Barcode.Code128Checksum.Instance)).Draw(chNFe, 10))
            {
                barra.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }

    #endregion Auxiliares

    #region Arquivos e Pastas

    #region Eventos

    public string getCurrentPathEventos(string cnpjEmpresa, string dataEmissao)
    {
        string pastaEventos = getCurrentPathNFe(cnpjEmpresa, dataEmissao) + "\\Eventos";
        if (!Directory.Exists(pastaEventos))
        {
            Directory.CreateDirectory(pastaEventos);
        }
        return pastaEventos;
    }

    public string getFileNameEventoCancelamento(NFe.NET.Cancelamento.TEventoInfEvento infEvento, string dataEmissao)
    {
        string cnpjEmpresa = infEvento.Item;
        string tipoEvento = Funcoes.ConvertEnumToString(infEvento.tpEvento);
        string chaveEvento = infEvento.chNFe;
        string seqEvento = infEvento.nSeqEvento;
        return getFileNameEvento(cnpjEmpresa, tipoEvento, chaveEvento, seqEvento, dataEmissao);
    }

    public string getFileNameEventoCorrecao(NFe.NET.CartaCorrecao.TEventoInfEvento infEvento, string dataEmissao)
    {
        string cnpjEmpresa = infEvento.Item;
        string tipoEvento = Funcoes.ConvertEnumToString(infEvento.tpEvento);
        string chaveEvento = infEvento.chNFe;
        string seqEvento = infEvento.nSeqEvento;
        return getFileNameEvento(cnpjEmpresa, tipoEvento, chaveEvento, seqEvento, dataEmissao);
    }

    public string getFileNameEvento(TEventoInfEvento infEvento, string dataEmissao, string cnpjEmitente)
    {
        string cnpjEmpresa = cnpjEmitente;
        string tipoEvento = infEvento.tpEvento;
        string chaveEvento = infEvento.chNFe;
        string seqEvento = infEvento.nSeqEvento;
        return getFileNameEvento(cnpjEmpresa, tipoEvento, chaveEvento, seqEvento, dataEmissao);
    }

    public string getFileNameEvento(string cnpjEmpresa, string tipoEvento, string chaveEvento, string seqEvento, string dataEmissao)
    {
        return getCurrentPathEventos(cnpjEmpresa, dataEmissao) + "\\" + tipoEvento + "-" + chaveEvento + "-" + seqEvento + "-procEventoNfe.xml";
    }

    public void SalvarEventoCancelamento(ref NFe.NET.Cancelamento.TProcEvento procEvento, string dataEmissao)
    {
        procEvento.NomeArquivo = getFileNameEventoCancelamento(procEvento.evento.infEvento, dataEmissao);
        procEvento.ArquivoXML = saveXml(procEvento.Serialize(), procEvento.NomeArquivo);
    }

    public void SalvarEventoCorrecao(ref NFe.NET.CartaCorrecao.TProcEvento procEvento, string dataEmissao)
    {
        procEvento.NomeArquivo = getFileNameEventoCorrecao(procEvento.evento.infEvento, dataEmissao);
        procEvento.ArquivoXML = saveXml(procEvento.Serialize(), procEvento.NomeArquivo);
    }

    public void SalvarEvento(ref TProcEvento procEvento, string dataEmissao, string cnpjEmitente)
    {
        procEvento.NomeArquivo = getFileNameEvento(procEvento.evento.infEvento, dataEmissao, cnpjEmitente);
        procEvento.ArquivoXML = saveXml(procEvento.Serialize(), procEvento.NomeArquivo);
    }

    #endregion Eventos

    #region Notas Fiscais Eletrônicas

    public string getCurrentPathNFe(string cnpjEmpresa, string dataEmissao)
    {
        string ano = dataEmissao.Substring(0, 4);
        string mes = dataEmissao.Substring(5, 2);
        string pasta = Configuracao.PastaEmitidas + cnpjEmpresa + "\\" + ano + mes;
        if (!Directory.Exists(pasta))
        {
            Directory.CreateDirectory(pasta);
        }
        return pasta;
    }

    public string getFileNameNFe(TNFeInfNFe infNFe)
    {
        string cnpjEmpresa = infNFe.emit.Item;
        string chaveNFe = infNFe.Id.Substring(3);
        string dataEmissao = infNFe.ide.dEmi;
        return getCurrentPathNFe(cnpjEmpresa, dataEmissao) + "\\" + chaveNFe + "-nfe.xml";
    }

    public void SalvarNFe(ref TNfeProc nota)
    {
        nota.NFe.NomeArquivo = getFileNameNFe(nota.NFe.infNFe);
        nota.NFe.ArquivoXML = saveXml(nota.Serialize(), nota.NFe.NomeArquivo);
    }

    #endregion Notas Fiscais Eletrônicas

    public XmlDocument saveXml(string xml, string fileName)
    {
        XmlTextWriter xtw = null;
        try
        {
            DirectoryInfo dInfo = Directory.GetParent(fileName);
            if (!dInfo.Exists)
            {
                dInfo.Create();
            }
            xtw = new XmlTextWriter(fileName, Encoding.UTF8);
            XmlDocument xd = new XmlDocument();
            xd.LoadXml(Funcoes.RemoveNameSpaceFromXml(xml));
            xd.Save(xtw);
            return xd;
        }
        finally
        {
            if ((xtw != null))
            {
                xtw.Flush();
                xtw.Close();
            }
        }
    }

    #endregion Arquivos e Pastas
}


public enum StatusNFe
{
    Inicializada,
    ArquivoGerado,
    Assinada,
    Validada,
    Emitida,
    Cancelada
}