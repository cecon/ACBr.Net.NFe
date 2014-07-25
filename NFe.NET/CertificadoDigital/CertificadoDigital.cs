using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Security.Cryptography.X509Certificates;

public static class CertificadoDigital
{
	
	public static XmlDocument Assinar(XmlDocument docXML, string pUri, X509Certificate2 pCertificado)
	{
		try {
			// Load the certificate from the certificate store.
			X509Certificate2 cert = pCertificado;

			// Create a new XML document.
			XmlDocument doc = new XmlDocument();

			// Format the document to ignore white spaces.
			doc.PreserveWhitespace = false;

			// Load the passed XML file using it's name.
			doc = docXML;

			// Create a SignedXml object.
			SignedXml signedXml = new SignedXml(doc);

			// Add the key to the SignedXml document.
			signedXml.SigningKey = cert.PrivateKey;

			// Create a reference to be signed.
			Reference reference = new Reference();
			// pega o uri que deve ser assinada
			XmlAttributeCollection _Uri = doc.GetElementsByTagName(pUri).Item(0).Attributes;
			foreach (XmlAttribute _atributo in _Uri) {
				if (_atributo.Name == "Id") {
					reference.Uri = "#" + _atributo.InnerText;
				}
			}

			// Add an enveloped transformation to the reference.
			XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
			reference.AddTransform(env);

			XmlDsigC14NTransform c14 = new XmlDsigC14NTransform();
			reference.AddTransform(c14);

			// Add the reference to the SignedXml object.
			signedXml.AddReference(reference);

			// Create a new KeyInfo object.
			KeyInfo keyInfo = new KeyInfo();

			// Load the certificate into a KeyInfoX509Data object
			// and add it to the KeyInfo object.
			keyInfo.AddClause(new KeyInfoX509Data(cert));

			// Add the KeyInfo object to the SignedXml object.
			signedXml.KeyInfo = keyInfo;

			// Compute the signature.
			signedXml.ComputeSignature();

			// Get the XML representation of the signature and save
			// it to an XmlElement object.
			XmlElement xmlDigitalSignature = signedXml.GetXml();

			// Append the element to the XML document.
			doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));


			if (doc.FirstChild is XmlDeclaration) {
				doc.RemoveChild(doc.FirstChild);
			}

			return doc;
		} catch (Exception ex) {
			throw new Exception("Erro ao efetuar assinatura digital, detalhes: " + ex.Message);
		}
	}

	//BUSCA CERTIFICADOS INSTALADOS SE INFORMADO UMA SERIE BUSCA A MESMA
	//SE NÃO ABRE CAIXA DE DIALOGOS DE CERTIFICADO
	public static X509Certificate2 SelecionarCertificado(string CerSerie)
	{
		X509Certificate2 certificate = new X509Certificate2();
		try {
			X509Certificate2Collection certificatesSel = null;
			X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
			store.Open(OpenFlags.OpenExistingOnly);
			X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, true).Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, true);
			if ((string.IsNullOrEmpty(CerSerie))) {
				certificatesSel = X509Certificate2UI.SelectFromCollection(certificates, "Certificados Digitais", "Selecione o Certificado Digital para uso no aplicativo", X509SelectionFlag.SingleSelection);
				if ((certificatesSel.Count == 0)) {
					certificate.Reset();
					throw new Exception("Nenhum certificado digital foi selecionado ou o certificado selecionado está com problemas.");
				} else {
					certificate = certificatesSel[0];
				}
			} else {
				certificatesSel = certificates.Find(X509FindType.FindBySerialNumber, CerSerie, true);
				if ((certificatesSel.Count == 0)) {
					certificate.Reset();
					throw new Exception("Certificado digital não encontrado");
				} else {
					certificate = certificatesSel[0];
				}
			}
			store.Close();
			return certificate;
		} catch (Exception exception) {
			throw new Exception(exception.Message);			
		}
	}

    public static X509Certificate2 SelecionarCertificado(string Caminho, string Senha)
    {
        if (!File.Exists(Caminho))
        {
            throw new Exception("Arquivo do Certificado digital não encontrado"); 
        }

        X509Certificate2 cert = new X509Certificate2(Caminho, Senha, X509KeyStorageFlags.MachineKeySet);
        return cert;
    }
}