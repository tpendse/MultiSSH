using MultiSSH.Model;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace MultiSSH.View
{
    public class ServerConnectionDelegate : BaseViewModel, IDisposable
    {
        public event EventHandler NotifyConnected;
        public event EventHandler<string> NotifyServerResponse;
        public event EventHandler<string> NotifyConfigParseError;
        public event EventHandler NotifyOperationCompleted;

        private readonly SshConnection _connection = null;
        private bool _inProgress = false;

        internal ServerConnectionDelegate(string config)
        {
            Config = config;

            try
            {
                var parts = Config.Split(',');
                Server = parts[0];
                var user = parts[1];
                var password = parts[2];

                _connection = new SshConnection(Server, user, password);
            }
            catch(Exception e)
            {
                NotifyConfigParseError?.Invoke(this, e.Message);
            }
        }

        private bool _isSelected = false;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        public string GetSaveContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine("CONFIG: " + Config);
            sb.AppendLine(new string('-', 30));
            sb.AppendLine(string.IsNullOrEmpty(ServerResponse) ? "-- EMPTY --" : ServerResponse);
            return sb.ToString();
        }

        public bool InProgress
        {
            get => _inProgress;
            set
            {
                _inProgress = value;
                NotifyPropertyChanged(nameof(InProgress));
            }
        }

        public string Config { get; }

        public string Server { get; }

        public bool IsConnected => _connection.IsConnected;

        public void Connect()
        {
            if(!IsConnected)
                _connection.Connect();

            NotifyConnected?.Invoke(this, EventArgs.Empty);
        }

        public void Run(string command)
        {
            var response = _connection.Run(command);
            NotifyServerResponse?.Invoke(this, response);
            ServerResponse = response;
            NotifyOperationCompleted?.Invoke(this, EventArgs.Empty);
        }

        private string _serverResponse = string.Empty;

        public string ServerResponse
        {
            get => _serverResponse;
            set
            {
                _serverResponse = value;
                NotifyPropertyChanged(nameof(ServerResponse));
            }
        }

        public void Dispose() => _connection?.Dispose();
    }
}
