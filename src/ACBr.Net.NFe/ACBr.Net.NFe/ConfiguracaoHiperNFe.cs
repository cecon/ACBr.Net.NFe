using ACBr.Net.NFe.TiposBasicos;


namespace ACBr.Net.NFe
{
    public class ConfiguracaoHiperNFe
    {
        private ConfigEmail _configEmail;
        private string _NumCertificado;
        private TAmb _Ambiente;
        private TUf _UF;
        private TCodUfIBGE _CodUF;
        private string _PastaEmitidas;
        private string _PastaLog;
        private string _LogoDANFE;
        private string _versao;
        private TNFeInfNFeEmit _Emitente;
        private string _AssuntoEmail;        
        private string _MensagemEmail;
        private string _site;
        private int _casasDecimaisQtd;
        private int _casasDecimaisValorUnitario;


        public int CasasDecimaisQtd
        {
            get { return _casasDecimaisQtd; }
            set { _casasDecimaisQtd = value; }
        }       

        public int CasasDecimaisValorUnitario
        {
            get { return _casasDecimaisValorUnitario; }
            set { _casasDecimaisValorUnitario = value; }
        }

        public string Site
        {
            get { return _site; }
            set { _site = value; }
        }

        public string MensagemEmail
        {
            get { return _MensagemEmail; }
            set { _MensagemEmail = value; }
        }

        public string AssuntoEmail
        {
            get { return _AssuntoEmail; }
            set { _AssuntoEmail = value; }
        }        

        public string LogoDanfe
        {
            get { return _LogoDANFE; }
            set { _LogoDANFE = value; }
        }

        public string NumCertificado
        {
            get { return _NumCertificado; }
            set
            {
                _NumCertificado = value;
                //_Certificado = CertificadoDigital.SelecionarCertificado(NumCertificado);
            }
        }

        public string PastaEmitidas
        {
            get { return _PastaEmitidas; }
            set { _PastaEmitidas = value; }
        }

        public string PastaLog
        {
            get { return _PastaLog; }
            set { _PastaLog = value; }
        }

        public string Versao
        {
            get { return _versao; }
            set { _versao = value; }
        }

        public TAmb Ambiente
        {
            get { return _Ambiente; }
            set { _Ambiente = value; }
        }

        public TCodUfIBGE CodUF
        {
            get { return _CodUF; }
        }

        public TUf UF
        {
            get { return _UF; }
            set
            {
                _CodUF = BuscaCodUF(value);
                _UF = value;
            }
        }        

        public TNFeInfNFeEmit Emitente
        {
            get
            {
                if (_Emitente == null)
                {
                    _Emitente = new TNFeInfNFeEmit();
                }
                return _Emitente;
            }
            set { _Emitente = value; }
        }

        private TCodUfIBGE BuscaCodUF(TUf UF)
        {
            switch (UF)
            {
                case TUf.RJ:
                    return TCodUfIBGE.RioDeJaneiro;
                case TUf.SP:
                    return TCodUfIBGE.SaoPaulo;
                case TUf.AC:
                    return TCodUfIBGE.Acre;
                case TUf.AL:
                    return TCodUfIBGE.Alagoas;
                case TUf.AM:
                    return TCodUfIBGE.Amazonas;
                case TUf.AP:
                    return TCodUfIBGE.Amapa;
                case TUf.BA:
                    return TCodUfIBGE.Bahia;
                case TUf.CE:
                    return TCodUfIBGE.Ceara;
                case TUf.DF:
                    return TCodUfIBGE.DistritoFederal;
                case TUf.ES:
                    return TCodUfIBGE.EspiritoSanto;
                case TUf.GO:
                    return TCodUfIBGE.Goias;
                case TUf.MA:
                    return TCodUfIBGE.Maranhao;
                case TUf.MG:
                    return TCodUfIBGE.MinasGerais;
                case TUf.MS:
                    return TCodUfIBGE.MatoGrossoDoSul;
                case TUf.MT:
                    return TCodUfIBGE.MatoGrosso;
                case TUf.PA:
                    return TCodUfIBGE.Para;
                case TUf.PB:
                    return TCodUfIBGE.Paraiba;
                case TUf.PE:
                    return TCodUfIBGE.Pernambuco;
                case TUf.PI:
                    return TCodUfIBGE.Piaui;
                case TUf.PR:
                    return TCodUfIBGE.Parana;
                case TUf.RN:
                    return TCodUfIBGE.RioGrandeDoNorte;
                case TUf.RO:
                    return TCodUfIBGE.Rondonia;
                case TUf.RR:
                    return TCodUfIBGE.Roraima;
                case TUf.RS:
                    return TCodUfIBGE.RioGrandeDoSul;
                case TUf.SC:
                    return TCodUfIBGE.SantaCatarina;
                case TUf.SE:
                    return TCodUfIBGE.Sergipe;
                case TUf.TO:
                    return TCodUfIBGE.Tocantis;
                default:
                    return TCodUfIBGE.RioDeJaneiro;
            }
        }

        public ConfigEmail ConfiguracaoMail
        {
            get
            {
                if (_configEmail == null)
                {
                    _configEmail = new ConfigEmail();
                }
                return _configEmail;
            }
            set { _configEmail = value; }
        }

    }
}
