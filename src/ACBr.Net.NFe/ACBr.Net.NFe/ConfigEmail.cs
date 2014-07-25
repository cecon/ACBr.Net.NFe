public class ConfigEmail
{
    public string SMTPServer { get; set; }
    public int Port { get; set; }
    public bool HabilitaSSL { get; set; }
    public bool UsarCredenciaisDefault { get; set; }
    public bool CorpoHtml { get; set; }
    public string Usuario { get; set; }
    public string Senha { get; set; }
    public string Remetente { get; set; }
}
