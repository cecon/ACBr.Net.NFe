using System.IO;

namespace ACBr.Net.NFe.Util
{
    public class Barras
    {
        public static byte[] ToArray(string chNFe)
        {
            using (var ms = new MemoryStream())
            {
                using (var barra = (new Zen.Barcode.Code128BarcodeDraw(Zen.Barcode.Code128Checksum.Instance)).Draw(chNFe, 10))
                {
                    barra.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }
}
