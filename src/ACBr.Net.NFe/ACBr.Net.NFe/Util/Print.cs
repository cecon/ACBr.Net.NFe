using System;
using System.Data;
using System.IO;
using System.Xml.Linq;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using FuncoesUteis;

namespace ACBr.Net.NFe.Util
{
    public class Print
    {
        private const string Ns = "{http://www.portalfiscal.inf.br/nfe}";        
        public static ReportDocument DANFE(TNfeProc nota, ConfiguracaoHiperNFe config)
        {
            var doc = XDocument.Load(nota.NFe.NomeArquivo);

            #region Validations            
            foreach (var det in doc.Descendants(Ns + "det"))
            {
                var imposto = det.Element(Ns + "imposto");
                /*Cria toda a parte do ICMS*/
                XmlImpostoCreator.Icms(imposto);
                XmlImpostoCreator.Ipi(imposto);

                if (det.Element(Ns + "infAdProd") == null)
                {
                    det.Add(new XElement(Ns + "infAdProd", ""));
                }

                var xElement = det.Element(Ns + "prod");
                if (xElement == null || xElement.Element(Ns + "vDesc") != null) continue;
                var element = det.Element(Ns + "prod");
                if (element != null) element.Add(new XElement(Ns + "vDesc", "0.00"));
            }

            #endregion Validations

            var dataSet = new DataSet();
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

            var danfe = new DANFE.DANFE();
            danfe.Load(Path.Combine(Environment.CurrentDirectory, "DANFE.rpt"), OpenReportMethod.OpenReportByDefault);
            var imageds = new DANFE.ImageDataSet();
            imageds.Images.AddImagesRow(Logo.ToBytes(config), Barras.ToArray(nota.NFe.infNFe.Id.Substring(3)), config.Site, config.CasasDecimaisQtd, config.CasasDecimaisValorUnitario);
            danfe.SetDataSource(dataSet);
            danfe.Database.Tables["Images"].SetDataSource(imageds);
            return danfe;
        }
        public static ReportDocument CartaCorrecao(TNfeProc nfe, CartaCorrecao.TProcEvento evento, ConfiguracaoHiperNFe config)
        {
            var doc = XDocument.Load(evento.NomeArquivo);
            var dataSet = new DataSet();
            dataSet.ReadXml(doc.CreateReader());

            var cartaCorrecao = new ImpressaoCartaCorrecao.CartaCorrecao();
            cartaCorrecao.Load(Path.Combine(Environment.CurrentDirectory, "CartaCorrecao.rpt"), OpenReportMethod.OpenReportByDefault);
            var imageds = new ImpressaoCartaCorrecao.ImageDataSet();
            imageds.Images.AddImagesRow(Logo.ToBytes(config), Barras.ToArray(evento.retEvento.infEvento.chNFe), config.Site);


            var row = ((ImpressaoCartaCorrecao.ImageDataSet.CartaCorrecaoRow)(imageds.CartaCorrecao.NewRow()));

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
    }
}
