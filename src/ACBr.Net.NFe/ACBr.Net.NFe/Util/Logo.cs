using System.IO;
using System.Reflection;

namespace ACBr.Net.NFe.Util
{
    public class Logo
    {
        public static byte[] ToBytes(ConfiguracaoHiperNFe config)
        {
            if (File.Exists(config.LogoDanfe))
            {
                return File.ReadAllBytes(config.LogoDanfe);
            }
            var path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            var caminhoImagem = Path.GetDirectoryName(path) + "/Resources/Imagens/logo.jpg";
            return File.ReadAllBytes(caminhoImagem);
        }
    }
}
