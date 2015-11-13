using System;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Communications.Net.Ftp
{
    public class ValidateServerCertificateEventArgs : EventArgs
    {
        private X509Certificate2 _certificate;
        private X509Chain _chain;
        private SslPolicyErrors _policyErrors;
        private bool _isCertificateValid;

        public ValidateServerCertificateEventArgs(X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            _certificate = certificate;
            _chain = chain;
            _policyErrors = policyErrors;
        }

        public X509Certificate2 Certificate
        {
            get
            {
                return _certificate;
            }
        }

        public X509Chain Chain
        {
            get
            {
                return _chain;
            }
        }

        public SslPolicyErrors PolicyErrors
        {
            get
            {
                return _policyErrors;
            }
        }

        public bool IsCertificateValid
        {
            get
            {
                return _isCertificateValid;
            }

            set
            {
                _isCertificateValid = value;
            }
        }
    }
}
