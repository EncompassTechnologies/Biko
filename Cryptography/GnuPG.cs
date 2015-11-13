using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Globalization;
using Microsoft.Win32;
using System.Collections;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Communications.Cryptography.OpenPGP
{
    public enum OutputTypes
    {
        AsciiArmor,
        Binary
    };

    public class GnuPG : IDisposable
    {
        private string _passphrase;
        private string _recipient;
        private string _homePath;
        private string _binaryPath;
        private OutputTypes _outputType;
        private int _timeout = 10000;
        private Process _proc;
        private Stream _outputStream;
        private Stream _errorStream;
        private const string GPG_EXECUTABLE_V1 = "gpg.exe";
        private const string GPG_EXECUTABLE_V2 = "gpg2.exe";
        private const string GPG_REGISTRY_KEY_UNINSTALL_V1 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GnuPG";
        private const string GPG_REGISTRY_KEY_UNINSTALL_V2 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GPG4Win";
        private const string GPG_REGISTRY_VALUE_INSTALL_LOCATION = "InstallLocation";
        private const string GPG_REGISTRY_VALUE_DISPLAYVERSION = "DisplayVersion";
        private const string GPG_COMMON_INSTALLATION_PATH = @"C:\Program Files\GNU\GnuPG";

        private enum ActionTypes
        {

            Encrypt,

            Decrypt,

            Sign,

            Verify
        };

        public GnuPG()
        {
            SetDefaults();
        }

        public GnuPG(string homePath, string binaryPath)
        {
            _homePath = homePath;
            _binaryPath = binaryPath;
            SetDefaults();
        }

        public GnuPG(string homePath)
        {
            _homePath = homePath;
            SetDefaults();
        }

        public int Timeout
        {
            get
            {
                return (_timeout);
            }
            set
            {
                _timeout = value;
            }
        }

        public string Recipient
        {
            get
            {
                return _recipient;
            }
            set
            {
                _recipient = value;
            }
        }

        public string Passphrase
        {
            get
            {
                return _passphrase;
            }
            set
            {
                _passphrase = value;
            }
        }

        public OutputTypes OutputType
        {
            get
            {
                return _outputType;
            }
            set
            {
                _outputType = value;
            }
        }

        public string HomePath
        {
            get
            {
                return _homePath;
            }
            set
            {
                _homePath = value;
            }
        }

        public string BinaryPath
        {
            get
            {
                return _binaryPath;
            }
            set
            {
                _binaryPath = value;
            }
        }

        public void Encrypt(Stream inputStream, Stream outputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("Argument inputStream can not be null.");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("Argument outputStream can not be null.");
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException("Argument inputStream must be readable.");
            }

            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Argument outputStream must be writable.");
            }

            ExecuteGPG(ActionTypes.Encrypt, inputStream, outputStream);
        }

        public void Decrypt(Stream inputStream, Stream outputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("Argument inputStream can not be null.");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("Argument outputStream can not be null.");
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException("Argument inputStream must be readable.");
            }

            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Argument outputStream must be writable.");
            }

            ExecuteGPG(ActionTypes.Decrypt, inputStream, outputStream);
        }

        public void Sign(Stream inputStream, Stream outputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("Argument inputStream can not be null.");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("Argument outputStream can not be null.");
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException("Argument inputStream must be readable.");
            }

            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Argument outputStream must be writable.");
            }

            ExecuteGPG(ActionTypes.Sign, inputStream, outputStream);
        }

        public void Verify(Stream inputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("Argument inputStream can not be null.");
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException("Argument inputStream must be readable.");
            }

            ExecuteGPG(ActionTypes.Verify, inputStream, new MemoryStream());
        }

        public GnuPGKeyCollection GetSecretKeys()
        {
            return new GnuPGKeyCollection(GetCommand("--list-secret-keys"));
        }

        public GnuPGKeyCollection GetKeys()
        {
            return new GnuPGKeyCollection(GetCommand("--list-keys"));
        }

        private StreamReader GetCommand(string command)
        {
            StringBuilder options = new StringBuilder();

            if (_homePath != null && _homePath.Length != 0)
            {
                options.Append(String.Format(CultureInfo.InvariantCulture, "--homedir \"{0}\" ", _homePath));
            }

            options.Append(command);

            string gpgPath = GetGnuPGPath();
            ProcessStartInfo procInfo = new ProcessStartInfo(gpgPath, options.ToString());

            procInfo.CreateNoWindow = true;
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardInput = true;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardError = true;

            MemoryStream outputStream = new MemoryStream();

            try
            {
                _proc = Process.Start(procInfo);
                _proc.StandardInput.Flush();

                if (!_proc.WaitForExit(Timeout))
                {
                    throw new GnuPGException("A time out event occurred while executing the GPG program.");
                }

                if (_proc.ExitCode != 0)
                {
                    throw new GnuPGException(_proc.StandardError.ReadToEnd());
                }

                CopyStream(_proc.StandardOutput.BaseStream, outputStream);
            }
            catch (Exception exp)
            {
                throw new GnuPGException(String.Format("An error occurred while trying to execute command {0}.", command, exp));
            }
            finally
            {
                Dispose(true);
            }

            StreamReader reader = new StreamReader(outputStream);
            reader.BaseStream.Position = 0;
            return reader;
        }

        private string GetCmdLineSwitches(ActionTypes action)
        {
            StringBuilder options = new StringBuilder();

            if (_homePath != null && _homePath.Length != 0)
            {
                options.Append(String.Format(CultureInfo.InvariantCulture, "--homedir \"{0}\" ", _homePath));
            }

            options.Append("--passphrase-fd 0 ");
            options.Append("--no-verbose --batch ");
            options.Append("--trust-model always ");

            switch (action)
            {
                case ActionTypes.Encrypt:
                    if (_recipient == null && action == ActionTypes.Encrypt)
                    {
                        throw new GnuPGException("A Recipient is required before encrypting data.  Please specify a valid recipient using the Recipient property on the GnuPG object.");
                    }

                    if (_outputType == OutputTypes.AsciiArmor)
                    {
                        options.Append("--armor ");
                    }

                    options.Append(String.Format(CultureInfo.InvariantCulture, "--recipient \"{0}\" --encrypt", _recipient));
                    break;
                case ActionTypes.Decrypt:
                    options.Append("--decrypt ");
                    break;
                case ActionTypes.Sign:
                    options.Append("--sign ");
                    break;
                case ActionTypes.Verify:
                    options.Append("--verify ");
                    break;
            }

            return options.ToString();
        }

        private void ExecuteGPG(ActionTypes action, Stream inputStream, Stream outputStream)
        {
            string gpgErrorText = string.Empty;
            string gpgPath = GetGnuPGPath();
            ProcessStartInfo procInfo = new ProcessStartInfo(gpgPath, GetCmdLineSwitches(action));

            procInfo.CreateNoWindow = true;
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardInput = true;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardError = true;

            try
            {
                _proc = Process.Start(procInfo);
                _proc.StandardInput.WriteLine(_passphrase);
                _proc.StandardInput.Flush();
                _outputStream = outputStream;
                _errorStream = new MemoryStream();

                ThreadStart outputEntry = new ThreadStart(AsyncOutputReader);
                Thread outputThread = new Thread(outputEntry);
                outputThread.Name = "GnuPG Output Thread";
                outputThread.Start();
                ThreadStart errorEntry = new ThreadStart(AsyncErrorReader);
                Thread errorThread = new Thread(errorEntry);
                errorThread.Name = "GnuPG Error Thread";
                errorThread.Start();

                CopyStream(inputStream, _proc.StandardInput.BaseStream);

                _proc.StandardInput.Flush();
                _proc.StandardInput.Close();

                if (!_proc.WaitForExit(_timeout))
                {
                    throw new GnuPGException("A time out event occurred while executing the GPG program.");
                }

                if (!outputThread.Join(_timeout / 2))
                {
                    outputThread.Abort();
                }

                if (!errorThread.Join(_timeout / 2))
                {
                    errorThread.Abort();
                }

                if (_proc.ExitCode != 0)
                {
                    StreamReader rerror = new StreamReader(_errorStream);
                    _errorStream.Position = 0;
                    gpgErrorText = rerror.ReadToEnd();
                }

            }
            catch (Exception exp)
            {
                throw new GnuPGException(String.Format(CultureInfo.InvariantCulture, "An error occurred while trying to {0} data using GnuPG.  GPG.EXE command switches used: {1}", action.ToString(), procInfo.Arguments), exp);
            }
            finally
            {
                Dispose();
            }

            if (gpgErrorText.IndexOf("bad passphrase") != -1)
            {
                throw new GnuPGBadPassphraseException(gpgErrorText);
            }

            if (gpgErrorText.Length > 0)
            {
                throw new GnuPGException(gpgErrorText);
            }
        }

        private string GetGnuPGPath()
        {
            if (!String.IsNullOrEmpty(_binaryPath))
            {
                if (!File.Exists(_binaryPath))
                {
                    throw new GnuPGException(String.Format("binary path to GnuPG executable invalid or file permissions do not allow access: {0}", _binaryPath));
                }

                return _binaryPath;
            }

            string pathv1 = "";
            RegistryKey hKeyLM_1 = Registry.LocalMachine;

            try
            {
                hKeyLM_1 = hKeyLM_1.OpenSubKey(GPG_REGISTRY_KEY_UNINSTALL_V1);
                pathv1 = (string)hKeyLM_1.GetValue(GPG_REGISTRY_VALUE_INSTALL_LOCATION);
                Path.Combine(pathv1, GPG_EXECUTABLE_V1);
            }
            finally
            {
                if (hKeyLM_1 != null)
                {
                    hKeyLM_1.Close();
                }
            }

            string pathv2 = "";
            RegistryKey hKeyLM_2 = Registry.LocalMachine;

            try
            {
                hKeyLM_2 = hKeyLM_2.OpenSubKey(GPG_REGISTRY_KEY_UNINSTALL_V2);
                pathv2 = (string)hKeyLM_2.GetValue(GPG_REGISTRY_VALUE_INSTALL_LOCATION);
                Path.Combine(pathv2, GPG_EXECUTABLE_V2);
            }
            finally
            {
                if (hKeyLM_2 != null)
                {
                    hKeyLM_2.Close();
                }
            }

            if (File.Exists(pathv2))
            {
                return pathv2;
            }
            else if (File.Exists(pathv1))
            {
                return pathv1;
            }
            else if (File.Exists(GPG_COMMON_INSTALLATION_PATH))
            {
                return GPG_COMMON_INSTALLATION_PATH;
            }
            else
            {
                throw new GnuPGException("cannot find a valid GPG.EXE or GPG2.EXE file path - set the property 'BinaryPath' to specify a hard path to the executable or verify file permissions are correct.");
            }
        }

        private void CopyStream(Stream input, Stream output)
        {
            if (_asyncWorker != null && _asyncWorker.CancellationPending)
            {
                return;
            }

            const int BUFFER_SIZE = 4096;
            byte[] bytes = new byte[BUFFER_SIZE];
            int i;

            while ((i = input.Read(bytes, 0, bytes.Length)) != 0)
            {
                if (_asyncWorker != null && _asyncWorker.CancellationPending)
                {
                    break;
                }

                output.Write(bytes, 0, i);
            }
        }

        private void SetDefaults()
        {
            _outputType = OutputTypes.AsciiArmor;
        }

        private void AsyncOutputReader()
        {
            Stream input = _proc.StandardOutput.BaseStream;
            Stream output = _outputStream;

            const int BUFFER_SIZE = 4096;
            byte[] bytes = new byte[BUFFER_SIZE];
            int i;

            while ((i = input.Read(bytes, 0, bytes.Length)) != 0)
            {
                output.Write(bytes, 0, i);
            }
        }

        private void AsyncErrorReader()
        {
            Stream input = _proc.StandardError.BaseStream;
            Stream output = _errorStream;

            const int BUFFER_SIZE = 4096;
            byte[] bytes = new byte[BUFFER_SIZE];
            int i;

            while ((i = input.Read(bytes, 0, bytes.Length)) != 0)
            {
                output.Write(bytes, 0, i);
            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_proc != null)
                {
                    _proc.StandardInput.Close();
                    _proc.StandardOutput.Close();
                    _proc.StandardError.Close();
                    _proc.Close();
                }
            }

            if (_proc != null)
            {
                _proc.Dispose();
                _proc = null;
            }
        }

        ~GnuPG()
        {
            Dispose(false);
        }

        private BackgroundWorker _asyncWorker;

        private Exception _asyncException;

        bool _asyncCancelled;

        public bool IsBusy
        {
            get
            {
                return _asyncWorker == null ? false : _asyncWorker.IsBusy;
            }
        }

        public bool IsAsyncCancelled
        {
            get
            {
                return _asyncCancelled;
            }
        }

        public void CancelAsync()
        {
            if (_asyncWorker != null && !_asyncWorker.CancellationPending && _asyncWorker.IsBusy)
            {
                _asyncCancelled = true;
                _asyncWorker.CancelAsync();
            }
        }

        private void CreateAsyncWorker()
        {
            if (_asyncWorker != null)
            {
                _asyncWorker.Dispose();
            }

            _asyncException = null;
            _asyncWorker = null;
            _asyncCancelled = false;
            _asyncWorker = new BackgroundWorker();
        }

        public event EventHandler<EncryptAsyncCompletedEventArgs> EncryptAsyncCompleted;

        public void EncryptAsync(Stream inputStream, Stream outputStream)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The GnuPG object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            CreateAsyncWorker();
            _asyncWorker.WorkerSupportsCancellation = true;
            _asyncWorker.DoWork += new DoWorkEventHandler(EncryptAsync_DoWork);
            _asyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(EncryptAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = inputStream;
            args[1] = outputStream;
            _asyncWorker.RunWorkerAsync(args);
        }

        private void EncryptAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                Encrypt((Stream)args[0], (Stream)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void EncryptAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (EncryptAsyncCompleted != null)
            {
                EncryptAsyncCompleted(this, new EncryptAsyncCompletedEventArgs(_asyncException, _asyncCancelled));
            }
        }

        public event EventHandler<DecryptAsyncCompletedEventArgs> DecryptAsyncCompleted;

        public void DecryptAsync(Stream inputStream, Stream outputStream)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The GnuPG object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            CreateAsyncWorker();
            _asyncWorker.WorkerSupportsCancellation = true;
            _asyncWorker.DoWork += new DoWorkEventHandler(DecryptAsync_DoWork);
            _asyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DecryptAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = inputStream;
            args[1] = outputStream;
            _asyncWorker.RunWorkerAsync(args);
        }

        private void DecryptAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                Decrypt((Stream)args[0], (Stream)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void DecryptAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (DecryptAsyncCompleted != null)
            {
                DecryptAsyncCompleted(this, new DecryptAsyncCompletedEventArgs(_asyncException, _asyncCancelled));
            }
        }

        public event EventHandler<SignAsyncCompletedEventArgs> SignAsyncCompleted;

        public void SignAsync(Stream inputStream, Stream outputStream)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The GnuPG object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            CreateAsyncWorker();
            _asyncWorker.WorkerSupportsCancellation = true;
            _asyncWorker.DoWork += new DoWorkEventHandler(SignAsync_DoWork);
            _asyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SignAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = inputStream;
            args[1] = outputStream;
            _asyncWorker.RunWorkerAsync(args);
        }

        private void SignAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                Sign((Stream)args[0], (Stream)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void SignAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (SignAsyncCompleted != null)
            {
                SignAsyncCompleted(this, new SignAsyncCompletedEventArgs(_asyncException, _asyncCancelled));
            }
        }
    }
}
