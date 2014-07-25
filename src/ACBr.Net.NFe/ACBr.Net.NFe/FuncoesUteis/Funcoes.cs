using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Xml;
using System.Net.Mail;
using System.Web;

namespace FuncoesUteis
{
    public static class Funcoes
    {
        
        public static bool ValidaCNPJCPF(string valor)
        {
            if (String.IsNullOrEmpty(valor))
            {
                return false;
            }

            string valor_como_int = RemoveMascara(valor);

            bool isCpf = (valor_como_int.Length == 11);
            bool isCNPJ = (valor_como_int.Length == 14);

            if (!isCpf && !isCNPJ)
            {
                return false;
            }

            if (isCpf)
            {
                return ValidaCPF(valor_como_int);
            }

            if (isCNPJ)
            {
                return ValidaCNPJ(valor_como_int);
            }

            return true;
        }

        public static bool ValidaCPF(string vrCPF)
        {
            string valor = vrCPF.Replace(".", "");
            valor = valor.Replace("-", "");
            if (valor.Length != 11)
            {
                return false;
            }

            bool igual = true;
            for (int i = 1; i < 11 && igual; i++)
            {
                if (valor[i] != valor[0])
                {
                    igual = false;
                }
            }

            if (igual || valor == "12345678909")
            {
                return false;
            }

            int[] numeros = new int[11];
            for (int i = 0; i < 11; i++)
            {
                numeros[i] = int.Parse(valor[i].ToString());
            }

            int soma = 0;
            for (int i = 0; i < 9; i++)
            {
                soma += (10 - i) * numeros[i];
            }

            int resultado = soma % 11;
            if (resultado == 1 || resultado == 0)
            {
                if (numeros[9] != 0)
                {
                    return false;
                }
            }
            else if (numeros[9] != 11 - resultado)
            {
                return false;
            }

            soma = 0;
            for (int i = 0; i < 10; i++)
            {
                soma += (11 - i) * numeros[i];
            }

            resultado = soma % 11;

            if (resultado == 1 || resultado == 0)
            {
                if (numeros[10] != 0)
                {
                    return false;
                }
            }
            else
            {
                if (numeros[10] != 11 - resultado)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ValidaCNPJ(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma;
            int resto;
            string digito;
            string tempCnpj;

            cnpj = cnpj.Trim();
            cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");

            if (cnpj.Length != 14)
            {
                return false;
            }
            tempCnpj = cnpj.Substring(0, 12);

            soma = 0;
            for (int i = 0; i < 12; i++)
            {
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];
            }
            resto = (soma % 11);
            if (resto < 2)
            {
                resto = 0;
            }
            else
            {
                resto = (11 - resto);
            }

            digito = resto.ToString();

            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = (soma % 11);
            if (resto < 2)
            {
                resto = 0;
            }
            else
            {
                resto = (11 - resto);
            }

            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }

        public static string EmptyToNull(string Valor)
        {
            if (String.IsNullOrEmpty(Valor))
            {
                return null;
            }

            if (Valor.Trim() == "")
            {
                return null;
            }

            return Valor;
        }

        public static string RemoveMascara(string Valor)
        {

            string valor_como_int = "";
            for (var i = 0; i < Valor.Length; i++)
            {
                int output = 0;
                if (int.TryParse(Valor[i].ToString(), out output))
                {
                    valor_como_int += Valor[i].ToString();
                }
            }

            return valor_como_int;
        }

        /// <summary>
        /// Converte uma string para um Nullable&lt;decimal&gt;
        /// </summary>
        /// <param name="strDecimal">String a ser convertida</param>
        /// <param name="nullDec">Variável de saída do tipo Nullable&lt;decimal&gt;</param>
        /// <returns>
        ///     True - Caso tenha algum valor na string e conseguir converter o número
        ///     False - Caso a conversão do string para decimal quebre
        /// </returns>
        public static bool TextToDecimalNullable(string strDecimal, out Nullable<decimal> nullDec)
        {
            nullDec = null;
            if (!String.IsNullOrEmpty(strDecimal))
            {
                decimal dec = 0;
                if (!Decimal.TryParse(strDecimal, out dec))
                {
                    return false;
                }

                nullDec = dec;
            }

            return true;
        }

        public static string Encrypt(string toEncrypt, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            System.Configuration.AppSettingsReader settingsReader =
                                                new AppSettingsReader();
            // Get the key from config file

            string key = (string)settingsReader.GetValue("SecurityKey",
                                                             typeof(String));
            //System.Windows.Forms.MessageBox.Show(key);
            //If hashing use get hashcode regards to your key
            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                //Always release the resources and flush data
                // of the Cryptographic service provide. Best Practice

                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            byte[] resultArray =
              cTransform.TransformFinalBlock(toEncryptArray, 0,
              toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor
            tdes.Clear();
            //Return the encrypted data into unreadable string format
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string Decrypt(string cipherString, bool useHashing)
        {
            byte[] keyArray;
            //get the byte code of the string

            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

            System.Configuration.AppSettingsReader settingsReader =
                                                new AppSettingsReader();
            //Get your key from config file to open the lock!
            string key = (string)settingsReader.GetValue("SecurityKey",
                                                         typeof(String));

            if (useHashing)
            {
                //if hashing was used get the hash code with regards to your key
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                //release any resource held by the MD5CryptoServiceProvider

                hashmd5.Clear();
            }
            else
            {
                //if hashing was not implemented get the byte code of the key
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes. 
            //We choose ECB(Electronic code Book)

            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(
                                 toEncryptArray, 0, toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor                
            tdes.Clear();
            //return the Clear decrypted TEXT
            return UTF8Encoding.UTF8.GetString(resultArray);
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

        //Retorna a data do primeiro dia de uma data corrente.
        public static DateTime PrimeiraDataDoMes(DateTime dataCorrente)
        {
            return new DateTime(dataCorrente.Year, dataCorrente.Month, 1);
        }

        public static string RemoverAcentos(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return String.Empty;
            else
            {
                texto = texto.Replace("´", "").Replace("`", "").Replace("^", "").Replace("~", "");
                texto = texto.Replace("º", "").Replace("ª", "").Replace("°", "").Replace("¨", "");
                byte[] bytes = System.Text.Encoding.GetEncoding("iso-8859-8").GetBytes(texto);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
        }

        //Retorna a data do último dia de uma data corrente.
        public static DateTime UltimaDataDoMes(DateTime dataCorrente)
        {
            return new DateTime(dataCorrente.Year, dataCorrente.Month, DateTime.DaysInMonth(dataCorrente.Year, dataCorrente.Month));
        }

        public static string GetDescription(this Enum value)
        {
            if (value == null)
            {
                return "";
            }
            FieldInfo field = value.GetType().GetField(value.ToString());

            DescriptionAttribute attribute
                    = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                        as DescriptionAttribute;

            return attribute == null ? value.ToString() : attribute.Description;
        }

        /// <summary>
        /// from http://www.wackylabs.net/2006/06/getting-the-xmlenumattribute-value-for-an-enum-field/
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string ConvertEnumToString(Enum e)
        {
            // Get the Type of the enum
            Type t = e.GetType();

            // Get the FieldInfo for the member field with the enums name
            FieldInfo info = t.GetField(e.ToString("G"));

            // Check to see if the XmlEnumAttribute is defined on this field
            if (!info.IsDefined(typeof(XmlEnumAttribute), false))
            {
                // If no XmlEnumAttribute then return the string version of the enum.
                return e.ToString("G");
            }

            // Get the XmlEnumAttribute
            object[] o = info.GetCustomAttributes(typeof(XmlEnumAttribute), false);
            XmlEnumAttribute att = (XmlEnumAttribute)o[0];
            return att.Name;
        }

        public static string RemoveNameSpaceFromXml(string xml)
        {
            xml = xml.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", "");
            xml = xml.Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "");
            return xml;
        }

        public static void RemoveNameSpaceFromFile(string FileName)
        {
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load(FileName);
            XmlDoc.LoadXml(XmlDoc.OuterXml.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", ""));
            XmlDoc.LoadXml(XmlDoc.OuterXml.Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", ""));
            XmlDoc.Save(FileName);
        }
        
    }
}
