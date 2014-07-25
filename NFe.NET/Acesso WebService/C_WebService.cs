using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Web.Services.Protocols;
using System.Xml;
using NFe.NET;
using System.IO;
using NFe.NET.TiposBasicos;
using FuncoesUteis;
using NFe.NET.DownloadNFe;

namespace NFe.NET.Webservice
{
    public class C_WebService
    {

        private static string _PastaLog;

        public C_WebService(string PastaLog)
        {
            _PastaLog = PastaLog;
            if (!Directory.Exists(_PastaLog))
            {
                Directory.CreateDirectory(_PastaLog);
            }
        }

        public struct ListaUrl
        {
            public string UrlNfeRecepcao;
            public string UrlNfeRetRecepcao;
            public string UrlNfeCancelamento;
            public string UrlNfeInutilizacao;
            public string UrlNfeConsultaProtocolo;
            public string UrlNfeStatusServico;
            public string UrlNfeConsultaCadastro;
            public string UrlRecepcaoEvento;
            public string UrlNfeDownloadNF;
        }

        public TRetEnviNFe EnviaLote2(HiperNFe Notas, int NumLote)
        {
            XmlNode strRetorno = null;
            dynamic xmldoc = new XmlDocument();

            ListaUrl listaURL = default(ListaUrl);
            listaURL = WsUrls.BuscaURL(Notas.Configuracao.CodUF, Notas.Configuracao.Ambiente);

            string nomeArquivoLote = _PastaLog + NumLote.ToString() + "-env-lot.xml";
            string nomeArquivoRetLote = _PastaLog + NumLote.ToString() + "-rec.xml";

            try
            {
                TEnviNFe LoteNFe = new TEnviNFe();
                LoteNFe.idLote = NumLote.ToString();
                LoteNFe.versao = Notas.Configuracao.Versao;
                // ERROR: Not supported in C#: ReDimStatement

                int indice = 0;
                LoteNFe.NFe = new TNFe[Notas.NotasFiscais.Count];
                foreach (TNfeProc nota in Notas.NotasFiscais)
                {
                    LoteNFe.NFe[indice] = nota.NFe;
                    indice += 1;
                }
                LoteNFe.GeraLoteNFe(nomeArquivoLote);
                xmldoc.Load(nomeArquivoLote);
                //Carrega o arquivo XML 

                Recepcao2.NfeRecepcao2 wsMsg = default(Recepcao2.NfeRecepcao2);
                Recepcao2.nfeCabecMsg cab = new Recepcao2.nfeCabecMsg();

                //UF E VERSÃO DO CABEÇALHO
                cab.cUF = PegarCodigoUF(Notas.Configuracao.CodUF);
                cab.versaoDados = LoteNFe.versao;

                //CABEÇALHO USADA PARA ENVIO DE LOTE
                wsMsg = new Recepcao2.NfeRecepcao2(listaURL.UrlNfeRecepcao);
                wsMsg.nfeCabecMsgValue = cab;

                wsMsg.Timeout = 100000;
                wsMsg.ClientCertificates.Add(Notas.Certificado);
                wsMsg.SoapVersion = SoapProtocolVersion.Soap12;

                xmldoc.Save(nomeArquivoLote);
                //RETORNO DA SEFAZ
                strRetorno = wsMsg.nfeRecepcaoLote2(xmldoc);
                TRetEnviNFe retornoEnvio = new TRetEnviNFe();
                XmlDocument retornoXML = new XmlDocument();
                retornoXML.LoadXml(strRetorno.OuterXml);
                retornoXML.Save(nomeArquivoRetLote);
                retornoEnvio = TRetEnviNFe.LoadFromFile(nomeArquivoRetLote);

                return retornoEnvio;
            }
            catch (Exception)
            {
                throw new NFe.NET.Exceptions.EnviaLote2Exception("Falha ao enviar o lote de NFe.");
            }
        }

        public TRetConsReciNFe ConsultaRecLote2(TConsReciNFe consReciNFe, TRetEnviNFe retEnvNFe, X509Certificate2 cert)
        {
            XmlNode strRetorno = null;

            C_WebService.ListaUrl listaURL = default(C_WebService.ListaUrl);
            listaURL = WsUrls.BuscaURL(retEnvNFe.cUF, retEnvNFe.tpAmb);

            string nomeArquivoPedido = _PastaLog + retEnvNFe.infRec.nRec + "-ped-rec.xml";
            string nomeArquivoRetProc = _PastaLog + retEnvNFe.infRec.nRec + "-pro-rec.xml";

            try
            {
                RetRecepcao2.NfeRetRecepcao2 wsMsg = default(RetRecepcao2.NfeRetRecepcao2);
                RetRecepcao2.nfeCabecMsg cab = new RetRecepcao2.nfeCabecMsg();

                //UF E VERSÃO DO CABEÇALHO
                cab.cUF = PegarCodigoUF(retEnvNFe.cUF);
                cab.versaoDados = retEnvNFe.versao;

                //CRIA UMA INSTANCIA DA CONEXÃO COM O WEBSERVICE
                wsMsg = new RetRecepcao2.NfeRetRecepcao2(listaURL.UrlNfeRetRecepcao);

                //ASSOCIA CABEÇALHO NFE
                wsMsg.nfeCabecMsgValue = cab;

                //DEFINE TEMPO MAXIMO DE ESPERA POR RETORNO
                wsMsg.Timeout = 100000;

                //ASSOCIA CERTIFICADO A CONEXAO WEBSERVICE
                wsMsg.ClientCertificates.Add(cert);

                //DEFINE PROTOCOLO USADO NA CONEXÃO
                //wsMsg.SoapVersion = SoapProtocolVersion.Soap12

                //CRIA UM NOVO DOCUMENTO XML
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(Funcoes.RemoveNameSpaceFromXml(consReciNFe.Serialize()));
                xmldoc.Save(nomeArquivoPedido);

                //ENVIA CONSULTA PARA SEFAZ E OBTEM RETORNO EM FORMATO STRING
                strRetorno = wsMsg.nfeRetRecepcao2(xmldoc);

                TRetConsReciNFe retornoReciNFe = new TRetConsReciNFe();
                XmlDocument retornoXML = new XmlDocument();
                retornoXML.LoadXml(strRetorno.OuterXml);
                retornoXML.Save(nomeArquivoRetProc);
                retornoReciNFe = TRetConsReciNFe.LoadFromFile(nomeArquivoRetProc);
                retornoReciNFe.XmlRetorno = retornoXML;

                return retornoReciNFe;

            }
            catch (Exception)
            {
                throw new NFe.NET.Exceptions.ConsultaRecLote2Exception("Falha ao consultar o recebimento de lote de NFe.", consReciNFe, retEnvNFe, cert);
            }
        }

        public static TRetDownloadNFe DownloadNFe(TDownloadNFe downloadNFe, TCodUfIBGE CodUF, X509Certificate2 cert)
        {
            XmlNode xmlNodeRetorno = null;
            TRetDownloadNFe retornoDownloadNFe = null;
            C_WebService.ListaUrl listaURL = WsUrls.BuscaURL(CodUF, downloadNFe.tpAmb);           

            //CABEÇALHO
            DownloadNF.nfeCabecMsg cab = new DownloadNF.nfeCabecMsg();
            cab.cUF = PegarCodigoUF(CodUF);                                    //UF DO CABEÇALHO 
            cab.versaoDados = Funcoes.ConvertEnumToString(downloadNFe.versao); //VERSÃO DO CABEÇALHO

            //CRIA UMA INSTANCIA DA CONEXÃO COM O WEBSERVICE
            DownloadNF.NfeDownloadNF wsMsg = new DownloadNF.NfeDownloadNF(listaURL.UrlNfeDownloadNF);
            wsMsg.nfeCabecMsgValue = cab;                   //ASSOCIA CABEÇALHO NFE
            wsMsg.Timeout = 100000;                         //DEFINE TEMPO MAXIMO DE ESPERA POR RETORNO
            wsMsg.ClientCertificates.Add(cert);             //ASSOCIA CERTIFICADO A CONEXAO WEBSERVICE            
            wsMsg.SoapVersion = SoapProtocolVersion.Soap12; //DEFINE PROTOCOLO USADO NA CONEXÃO

            XmlDocument dados = new XmlDocument();
            dados.LoadXml(Funcoes.RemoveNameSpaceFromXml(downloadNFe.Serialize()));

            //ENVIA REQUISIÇÂO PARA SEFAZ E OBTEM RETORNO
            xmlNodeRetorno = wsMsg.nfeDownloadNF(dados);
            retornoDownloadNFe = TRetDownloadNFe.Deserialize(xmlNodeRetorno.OuterXml);

            return retornoDownloadNFe;
        }

        public TRetConsStatServ ConsultaStatusWs(TConsStatServ consStatServ, X509Certificate2 cert)
        {
            XmlNode strRetorno = null;
            TRetConsStatServ retornoStatus = new TRetConsStatServ();
            C_WebService.ListaUrl listaURL = default(C_WebService.ListaUrl);
            listaURL = WsUrls.BuscaURL(consStatServ.cUF, consStatServ.tpAmb);

            try
            {
                StatusServico2.NfeStatusServico2 wsMsg = default(StatusServico2.NfeStatusServico2);
                StatusServico2.nfeCabecMsg cab = new StatusServico2.nfeCabecMsg();

                //UF E VERSÃO DO CABEÇALHO
                cab.cUF = PegarCodigoUF(consStatServ.cUF);
                cab.versaoDados = consStatServ.versao;

                //CRIA UMA INSTANCIA DA CONEXÃO COM O WEBSERVICE
                wsMsg = new StatusServico2.NfeStatusServico2(listaURL.UrlNfeStatusServico);

                //ASSOCIA CABEÇALHO NFE
                wsMsg.nfeCabecMsgValue = cab;

                //DEFINE TEMPO MAXIMO DE ESPERA POR RETORNO
                wsMsg.Timeout = 100000;

                //ASSOCIA CERTIFICADO A CONEXAO WEBSERVICE
                wsMsg.ClientCertificates.Add(cert);

                //DEFINE PROTOCOLO USADO NA CONEXÃO
                //wsMsg.SoapVersion = SoapProtocolVersion.Soap12

                //CRIA UM NOVO DOCUMENTO XML                
                XmlDocument retornoXML = new XmlDocument();
                string nomeArquivoEnv = _PastaLog + System.DateTime.Now.ToString("yyyyMMddTHHmmss") + " -ped-sta.xml";
                string nomeArquivoRet = _PastaLog + System.DateTime.Now.ToString("yyyyMMddTHHmmss") + " -ret-sta.xml";

                XmlDocument dados = new XmlDocument();
                dados.LoadXml(Funcoes.RemoveNameSpaceFromXml(consStatServ.Serialize()));
                dados.Save(nomeArquivoEnv);

                //ENVIA CONSULTA PARA SEFAZ E OBTEM RETORNO EM FORMATO STRING
                strRetorno = wsMsg.nfeStatusServicoNF2(dados);
                retornoXML.LoadXml(strRetorno.OuterXml);
                retornoXML.Save(nomeArquivoRet);
                retornoStatus = TRetConsStatServ.LoadFromFile(nomeArquivoRet);

                return retornoStatus;

            }
            catch (Exception ex)
            {
                throw new System.InvalidOperationException("Erro ao consultar status do serviço. Erro: " + ex.Message);
            }
        }

        private static string PegarCodigoUF(TCodUfIBGE CodUfIBGE)
        {
            switch (CodUfIBGE)
            {
                case TCodUfIBGE.RioDeJaneiro: return "33";
                case TCodUfIBGE.SaoPaulo: return "35";
                case TCodUfIBGE.Acre: return "12";
                case TCodUfIBGE.Alagoas: return "27";
                case TCodUfIBGE.Amazonas: return "13";
                case TCodUfIBGE.Amapa: return "16";
                case TCodUfIBGE.Bahia: return "29";
                case TCodUfIBGE.Ceara: return "23";
                case TCodUfIBGE.DistritoFederal: return "53";
                case TCodUfIBGE.EspiritoSanto: return "32";
                case TCodUfIBGE.Goias: return "52";
                case TCodUfIBGE.Maranhao: return "21";
                case TCodUfIBGE.MinasGerais: return "31";
                case TCodUfIBGE.MatoGrossoDoSul: return "50";
                case TCodUfIBGE.MatoGrosso: return "51";
                case TCodUfIBGE.Para: return "15";
                case TCodUfIBGE.Paraiba: return "25";
                case TCodUfIBGE.Pernambuco: return "26";
                case TCodUfIBGE.Piaui: return "22";
                case TCodUfIBGE.Parana: return "41";
                case TCodUfIBGE.RioGrandeDoNorte: return "24";
                case TCodUfIBGE.Rondonia: return "11";
                case TCodUfIBGE.Roraima: return "14";
                case TCodUfIBGE.RioGrandeDoSul: return "43";
                case TCodUfIBGE.SantaCatarina: return "42";
                case TCodUfIBGE.Sergipe: return "28";
                case TCodUfIBGE.Tocantis: return "17";
                default: return "";
            }
        }

        public List<TRetConsSitNFe> ConsultaSitNFe(HiperNFe hiperNFe, X509Certificate2 cert)
        {
            XmlNode strRetorno = null;
            C_WebService.ListaUrl listaURL = default(C_WebService.ListaUrl);
            listaURL = WsUrls.BuscaURL(hiperNFe.Configuracao.CodUF, hiperNFe.Configuracao.Ambiente);
            try
            {
                Consulta2.NfeConsulta2 wsMsg = default(Consulta2.NfeConsulta2);
                Consulta2.nfeCabecMsg cab = new Consulta2.nfeCabecMsg();

                //UF E VERSÃO DO CABEÇALHO
                cab.cUF = PegarCodigoUF(hiperNFe.Configuracao.CodUF);
                cab.versaoDados = hiperNFe.Configuracao.Versao;

                //CRIA UMA INSTANCIA DA CONEXÃO COM O WEBSERVICE
                wsMsg = new Consulta2.NfeConsulta2(listaURL.UrlNfeConsultaProtocolo);

                //ASSOCIA CABEÇALHO NFE
                wsMsg.nfeCabecMsgValue = cab;

                //DEFINE TEMPO MAXIMO DE ESPERA POR RETORNO
                wsMsg.Timeout = 100000;

                //ASSOCIA CERTIFICADO A CONEXAO WEBSERVICE
                wsMsg.ClientCertificates.Add(cert);

                //DEFINE PROTOCOLO USADO NA CONEXÃO
                wsMsg.SoapVersion = SoapProtocolVersion.Soap12;

                //CRIA UM NOVO DOCUMENTO XML                
                List<TRetConsSitNFe> listaRetorno = new List<TRetConsSitNFe>();
                foreach (TNfeProc nota in hiperNFe.NotasFiscais)
                {
                    TConsSitNFe consSitNfe = new TConsSitNFe();
                    string chaveNfe = nota.NFe.infNFe.Id.Substring(3);
                    string arquivoConsulta = _PastaLog + chaveNfe + "-ped-sit.xml";
                    string arquivoRetorno = _PastaLog + chaveNfe + "-sit.xml";
                    consSitNfe.chNFe = chaveNfe;
                    consSitNfe.tpAmb = hiperNFe.Configuracao.Ambiente;
                    //consSitNfe.versao = nota.NFe.infNFe.versao;
                    consSitNfe.versao = TVerConsSitNFe.Versao201;
                    consSitNfe.xServ = TConsSitNFeXServ.CONSULTAR;

                    XmlDocument dados = new XmlDocument();
                    dados.LoadXml(Funcoes.RemoveNameSpaceFromXml(consSitNfe.Serialize()));
                    dados.Save(arquivoConsulta);

                    strRetorno = wsMsg.nfeConsultaNF2(dados);

                    XmlDocument retornoXML = new XmlDocument();
                    retornoXML.LoadXml(strRetorno.OuterXml);
                    retornoXML.Save(arquivoRetorno);
                    TRetConsSitNFe retornoSitNFe = new TRetConsSitNFe();
                    retornoSitNFe = TRetConsSitNFe.LoadFromFile(arquivoRetorno);
                    nota.procEventoNFe = retornoSitNFe.procEventoNFe;

                    if (nota.procEventoNFe != null)
                    {
                        foreach (var item in nota.procEventoNFe)
                        {
                            string arquivoProcEvento = _PastaLog + item.evento.infEvento.tpEvento + chaveNfe + item.evento.infEvento.nSeqEvento + "-procEventoNfe.xml";
                            XmlDocument xmlEvento = new XmlDocument();
                            item.SaveToFile(arquivoProcEvento);
                            xmlEvento.LoadXml(item.Serialize());
                            item.ArquivoXML = xmlEvento;
                            item.NomeArquivo = arquivoProcEvento;
                        }
                    }

                    listaRetorno.Add(retornoSitNFe);
                }

                return listaRetorno;

            }
            catch (Exception ex)
            {
                throw new System.InvalidOperationException("Erro ao consultar a situação da NFe. Erro: " + ex.Message);
            }
        }

        public Cancelamento.TRetEnvEvento CancelaNFe(TNfeProc nota, string Justificativa, X509Certificate2 pCertificado, int numLote, int nSequencia, TAmb ambiente, out NFe.NET.Cancelamento.TProcEvento procEvento)
        {
            XmlElement strRetorno = null;

            string arqPedCanc = _PastaLog + nota.NFe.infNFe.Id.Substring(3) + "-ped-evento.xml";
            string retPedCanc = _PastaLog + nota.NFe.infNFe.Id.Substring(3) + "-eve.xml";
            C_WebService.ListaUrl listaURL = default(C_WebService.ListaUrl);
            listaURL = WsUrls.BuscaURL(nota.NFe.infNFe.ide.cUF, ambiente);

            #region evento
            Cancelamento.TEvento evento = new Cancelamento.TEvento();
            evento.versao = "1.00";

            #region infEvento
            Cancelamento.TEventoInfEvento infEvento = new Cancelamento.TEventoInfEvento();
            infEvento.tpAmb = ambiente;
            infEvento.chNFe = nota.NFe.infNFe.Id.Substring(3);
            infEvento.cOrgao = nota.NFe.infNFe.ide.cUF;
            infEvento.dhEvento = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzzz");// +"-00:00";
            infEvento.Id = "ID" + "110111" + nota.NFe.infNFe.Id.Substring(3) + nSequencia.ToString("D2");
            infEvento.ItemElementName = Cancelamento.TipoDocumentoCanc.CNPJ;
            infEvento.Item = nota.NFe.infNFe.emit.Item;
            infEvento.nSeqEvento = nSequencia.ToString();
            infEvento.verEvento = Cancelamento.TEventoInfEventoVerEvento.Item100;
            infEvento.tpEvento = Cancelamento.TEventoInfEventoTpEvento.Cancelamento;

            #region detEvento
            Cancelamento.TEventoInfEventoDetEvento detEvento = new Cancelamento.TEventoInfEventoDetEvento();
            detEvento.descEvento = Cancelamento.TEventoInfEventoDetEventoDescEvento.Cancelamento;
            detEvento.nProt = nota.protNFe.infProt.nProt;
            detEvento.versao = Cancelamento.TEventoInfEventoDetEventoVersao.Item100;
            detEvento.xJust = Justificativa;
            #endregion detEvento

            infEvento.detEvento = detEvento;
            #endregion infEvento

            evento.infEvento = infEvento;

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(Funcoes.RemoveNameSpaceFromXml(evento.Serialize()));
            xmldoc = CertificadoDigital.Assinar(xmldoc, "infEvento", pCertificado);
            xmldoc.Save(arqPedCanc);

            #endregion evento

            dynamic envEvento = GeraLoteEvento(arqPedCanc, numLote);

            try
            {
                RecepcaoEvento.nfeCabecMsg cab = new RecepcaoEvento.nfeCabecMsg();
                RecepcaoEvento.RecepcaoEvento wsMsg = default(RecepcaoEvento.RecepcaoEvento);

                // UF E VERSÃO DO CABEÇALHO
                cab.cUF = PegarCodigoUF(nota.NFe.infNFe.ide.cUF);
                cab.versaoDados = "1.00";

                //CABEÇALHO USADA PARA ENVIO DE LOTE
                wsMsg = new RecepcaoEvento.RecepcaoEvento(listaURL.UrlRecepcaoEvento);
                wsMsg.nfeCabecMsgValue = cab;
                wsMsg.Timeout = 100000;
                wsMsg.ClientCertificates.Add(pCertificado);
                wsMsg.SoapVersion = SoapProtocolVersion.Soap12;

                //RETORNO DA SEFAZ
                strRetorno = wsMsg.nfeRecepcaoEvento(envEvento);

                XmlDocument xmlRetorno = new XmlDocument();
                xmlRetorno.LoadXml(strRetorno.OuterXml);
                xmlRetorno.Save(retPedCanc);

                NFe.NET.Cancelamento.TRetEnvEvento retCancNFe = NFe.NET.Cancelamento.TRetEnvEvento.LoadFromFile(retPedCanc);

                //saída
                procEvento = new Cancelamento.TProcEvento();
                procEvento.evento = NET.Cancelamento.TEvento.Deserialize(xmldoc.OuterXml);
                procEvento.retEvento = retCancNFe.retEvento[0];
                procEvento.versao = "1.00";

                return retCancNFe;
            }
            catch (Exception ex)
            {
                throw new System.InvalidOperationException("Erro ao cancelar NFe. Erro: " + ex.Message);
            }
        }

        public CartaCorrecao.TRetEnvEvento CartaCorrecao(TNfeProc nota, string Correcao, X509Certificate2 pCertificado, int numLote, int nSequencia, TAmb ambiente, out NFe.NET.CartaCorrecao.TProcEvento procEvento)
        {
            string arqPedCorecao = _PastaLog + nota.NFe.infNFe.Id.Substring(3) + "-ped-evento.xml";//-ped-cce.xml ???
            string retPedCorrecao = _PastaLog + nota.NFe.infNFe.Id.Substring(3) + "-eve.xml";
            C_WebService.ListaUrl listaURL = default(C_WebService.ListaUrl);
            listaURL = WsUrls.BuscaURL(nota.NFe.infNFe.ide.cUF, ambiente);

            #region evento
            CartaCorrecao.TEvento evento = new CartaCorrecao.TEvento();
            evento.versao = "1.00";

            #region infEvento
            CartaCorrecao.TEventoInfEvento infEvento = new CartaCorrecao.TEventoInfEvento();
            infEvento.tpAmb = ambiente;
            infEvento.chNFe = nota.NFe.infNFe.Id.Substring(3);
            infEvento.cOrgao = PegarCodigoOrgaoUF(nota.NFe.infNFe.ide.cUF);
            infEvento.dhEvento = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzzz");
            infEvento.Id = "ID" + "110110" + nota.NFe.infNFe.Id.Substring(3) + nSequencia.ToString("D2");
            infEvento.ItemElementName = NFe.NET.CartaCorrecao.ItemChoiceTypeCC.CNPJ;
            infEvento.Item = nota.NFe.infNFe.emit.Item;
            infEvento.nSeqEvento = nSequencia.ToString();
            infEvento.verEvento = NFe.NET.CartaCorrecao.TEventoInfEventoVerEvento.Item100;
            infEvento.tpEvento = NFe.NET.CartaCorrecao.TEventoInfEventoTpEvento.Item110110;

            #region detEvento
            CartaCorrecao.TEventoInfEventoDetEvento detEvento = new CartaCorrecao.TEventoInfEventoDetEvento();
            detEvento.descEvento = NFe.NET.CartaCorrecao.TEventoInfEventoDetEventoDescEvento.CartadeCorrecao;
            detEvento.versao = NFe.NET.CartaCorrecao.TEventoInfEventoDetEventoVersao.Item100;
            detEvento.xCondUso = NET.CartaCorrecao.TEventoInfEventoDetEventoXCondUso.ACartadeCorrecaoedisciplinadapeloparagrafo1oAdoart7odoConvenioSNde15dedezembrode1970epodeserutilizadapararegularizacaodeerroocorridonaemissaodedocumentofiscaldesdequeoerronaoestejarelacionadocomIasvariaveisquedeterminamovalordoimpostotaiscomobasedecalculoaliquotadiferencadeprecoquantidadevalordaoperacaooudaprestacaoIIacorrecaodedadoscadastraisqueimpliquemudancadoremetenteoudodestinatarioIIIadatadeemissaooudesaida;
            detEvento.xCorrecao = Correcao;
            #endregion detEvento

            infEvento.detEvento = detEvento;
            #endregion infEvento

            evento.infEvento = infEvento;

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(Funcoes.RemoveNameSpaceFromXml(evento.Serialize()));
            xmldoc = CertificadoDigital.Assinar(xmldoc, "infEvento", pCertificado);
            xmldoc.Save(arqPedCorecao);

            #endregion evento

            dynamic envEvento = GeraLoteEvento(arqPedCorecao, numLote);

            try
            {
                RecepcaoEvento.nfeCabecMsg cab = new RecepcaoEvento.nfeCabecMsg();
                RecepcaoEvento.RecepcaoEvento wsMsg = default(RecepcaoEvento.RecepcaoEvento);

                // UF E VERSÃO DO CABEÇALHO
                cab.cUF = PegarCodigoUF(nota.NFe.infNFe.ide.cUF);
                cab.versaoDados = "1.00";

                //CABEÇALHO USADA PARA ENVIO DE LOTE
                wsMsg = new RecepcaoEvento.RecepcaoEvento(listaURL.UrlRecepcaoEvento);
                wsMsg.nfeCabecMsgValue = cab;
                wsMsg.Timeout = 100000;
                wsMsg.ClientCertificates.Add(pCertificado);
                wsMsg.SoapVersion = SoapProtocolVersion.Soap12;

                //RETORNO DA SEFAZ
                XmlElement strRetorno = wsMsg.nfeRecepcaoEvento(envEvento);
                XmlDocument xmlRetorno = new XmlDocument();
                xmlRetorno.LoadXml(strRetorno.OuterXml);
                xmlRetorno.Save(retPedCorrecao);

                NFe.NET.CartaCorrecao.TRetEnvEvento retCorrecaoNFe = NFe.NET.CartaCorrecao.TRetEnvEvento.LoadFromFile(retPedCorrecao);

                //saída
                procEvento = new CartaCorrecao.TProcEvento();
                procEvento.evento = NET.CartaCorrecao.TEvento.Deserialize(xmldoc.OuterXml);
                procEvento.retEvento = retCorrecaoNFe.retEvento[0];
                procEvento.versao = "1.00";

                return retCorrecaoNFe;
            }
            catch (Exception ex)
            {
                throw new System.InvalidOperationException("Erro ao corrigir NFe. Erro: " + ex.Message);
            }
        }

        private TCOrgaoIBGE PegarCodigoOrgaoUF(TCodUfIBGE codUfIBGE)
        {
            switch (codUfIBGE)
            {
                case TCodUfIBGE.RioDeJaneiro: return TCOrgaoIBGE.RioDeJaneiro;
                case TCodUfIBGE.SaoPaulo: return TCOrgaoIBGE.SaoPaulo;
                case TCodUfIBGE.Acre: return TCOrgaoIBGE.Acre;
                case TCodUfIBGE.Alagoas: return TCOrgaoIBGE.Alagoas;
                case TCodUfIBGE.Amazonas: return TCOrgaoIBGE.Amazonas;
                case TCodUfIBGE.Amapa: return TCOrgaoIBGE.Amapa;
                case TCodUfIBGE.Bahia: return TCOrgaoIBGE.Bahia;
                case TCodUfIBGE.Ceara: return TCOrgaoIBGE.Ceara;
                case TCodUfIBGE.DistritoFederal: return TCOrgaoIBGE.DistritoFederal;
                case TCodUfIBGE.EspiritoSanto: return TCOrgaoIBGE.EspiritoSanto;
                case TCodUfIBGE.Goias: return TCOrgaoIBGE.Goias;
                case TCodUfIBGE.Maranhao: return TCOrgaoIBGE.Maranhao;
                case TCodUfIBGE.MinasGerais: return TCOrgaoIBGE.MinasGerais;
                case TCodUfIBGE.MatoGrossoDoSul: return TCOrgaoIBGE.MatoGrossoDoSul;
                case TCodUfIBGE.MatoGrosso: return TCOrgaoIBGE.MatoGrosso;
                case TCodUfIBGE.Para: return TCOrgaoIBGE.Para;
                case TCodUfIBGE.Paraiba: return TCOrgaoIBGE.Paraiba;
                case TCodUfIBGE.Pernambuco: return TCOrgaoIBGE.Pernambuco;
                case TCodUfIBGE.Piaui: return TCOrgaoIBGE.Piaui;
                case TCodUfIBGE.Parana: return TCOrgaoIBGE.Parana;
                case TCodUfIBGE.RioGrandeDoNorte: return TCOrgaoIBGE.RioGrandeDoNorte;
                case TCodUfIBGE.Rondonia: return TCOrgaoIBGE.Rondonia;
                case TCodUfIBGE.Roraima: return TCOrgaoIBGE.Roraima;
                case TCodUfIBGE.RioGrandeDoSul: return TCOrgaoIBGE.RioGrandeDoSul;
                case TCodUfIBGE.SantaCatarina: return TCOrgaoIBGE.SantaCatarina;
                case TCodUfIBGE.Sergipe: return TCOrgaoIBGE.Sergipe;
                case TCodUfIBGE.Tocantis: return TCOrgaoIBGE.Tocantins;
                default: return TCOrgaoIBGE.Exterior;
            }
        }

        public TRetInutNFe InutilizaNumeracao(TInutNFe InutNFe, X509Certificate2 pCertificado, TAmb ambiente)
        {
            C_WebService.ListaUrl listaURL = default(C_WebService.ListaUrl);
            string arqRetInut = _PastaLog + InutNFe.infInut.ano + InutNFe.infInut.CNPJ + InutNFe.infInut.mod + InutNFe.infInut.serie + InutNFe.infInut.nNFIni + InutNFe.infInut.nNFFin + "-inu.xml";
            string arqPedInut = _PastaLog + InutNFe.infInut.cUF + InutNFe.infInut.ano + InutNFe.infInut.CNPJ + InutNFe.infInut.mod + InutNFe.infInut.serie + InutNFe.infInut.nNFIni + InutNFe.infInut.nNFFin + "-ped-inu.xml";

            listaURL = WsUrls.BuscaURL(InutNFe.infInut.cUF, ambiente);

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(Funcoes.RemoveNameSpaceFromXml(InutNFe.Serialize()));
            xmldoc = CertificadoDigital.Assinar(xmldoc, "infInut", pCertificado);
            xmldoc.Save(arqPedInut);

            try
            {
                Inutilizacao2.nfeCabecMsg cab = new Inutilizacao2.nfeCabecMsg();
                Inutilizacao2.NfeInutilizacao2 wsMsg = default(Inutilizacao2.NfeInutilizacao2);

                // UF E VERSÃO DO CABEÇALHO
                cab.cUF = PegarCodigoUF(InutNFe.infInut.cUF);
                cab.versaoDados = InutNFe.versao;

                //CABEÇALHO USADA PARA ENVIO DE LOTE
                wsMsg = new Inutilizacao2.NfeInutilizacao2(listaURL.UrlNfeInutilizacao);
                wsMsg.nfeCabecMsgValue = cab;
                wsMsg.Timeout = 100000;
                wsMsg.ClientCertificates.Add(pCertificado);
                wsMsg.SoapVersion = SoapProtocolVersion.Soap12;

                //RETORNO DA SEFAZ
                XmlNode strRetorno = wsMsg.nfeInutilizacaoNF2(xmldoc);
                XmlDocument xmlRetorno = new XmlDocument();
                xmlRetorno.LoadXml(strRetorno.OuterXml);
                xmlRetorno.Save(arqRetInut);

                TRetInutNFe retInutNFe = new TRetInutNFe();
                retInutNFe = TRetInutNFe.LoadFromFile(arqRetInut);

                return retInutNFe;
            }
            catch (Exception ex)
            {
                throw new System.InvalidOperationException("Erro ao inutilizar numeração. Erro: " + ex.Message);
            }

        }

        private XmlDocument GeraLoteEvento(string fileName, int idLote)
        {
            XmlDocument documentoXML = new XmlDocument();
            documentoXML.Load(fileName);
            string conteudoEvento = documentoXML.OuterXml;
            string vStringLoteNfe = string.Empty;
            vStringLoteNfe += "<?xml version=\"1.0\"?>";
            vStringLoteNfe += "<envEvento xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"1.00\">";
            vStringLoteNfe += "<idLote>" + idLote + "</idLote>";
            vStringLoteNfe += conteudoEvento;
            vStringLoteNfe += "</envEvento>";
            documentoXML.LoadXml(vStringLoteNfe);
            documentoXML.Save(fileName);
            return documentoXML;
        }

    }
}