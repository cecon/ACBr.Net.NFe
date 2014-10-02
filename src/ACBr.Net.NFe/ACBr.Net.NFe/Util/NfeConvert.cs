using System;
using CrystalDecisions.Shared;

namespace ACBr.Net.NFe.Util
{
    public class NfeConvert
    {
        #region PDF

        public static string ToPDF(TNfeProc nota, ConfiguracaoHiperNFe config)
        {
            try
            {
                var arquivoPDF = nota.NFe.NomeArquivo.Replace(".xml", ".pdf");
                var danfe = Print.DANFE(nota, config);
                danfe.ExportToDisk(ExportFormatType.PortableDocFormat, arquivoPDF);
                return arquivoPDF;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar o arquivo do DANFE: " + ex.Message);
            }
        }

        public static string ToPDFCartaCorrecao(TNfeProc nota, CartaCorrecao.TProcEvento evento, ConfiguracaoHiperNFe config)
        {
            try
            {
                var arquivoPDF = evento.NomeArquivo.Replace(".xml", ".pdf");
                var danfe = Print.CartaCorrecao(nota, evento, config);
                danfe.ExportToDisk(ExportFormatType.PortableDocFormat, arquivoPDF);
                return arquivoPDF;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar o arquivo do DANFE: " + ex.Message);
            }
        }

        #endregion PDF
    }
}
