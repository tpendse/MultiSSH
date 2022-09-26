using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MultiSSH.View
{
    public class MainViewModel : BaseViewModel
    {
        private const string SERVER_LIST_FILENAME = "servers.txt";
        private const int ASTERISK_COUNT = 40;

        private string _outputServerResponse = string.Empty;
        private bool _processing = false;
        private int _processingCount = 0;

        private static Mutex _processingMutex = new Mutex();
        private static Mutex _outputLogMutex = new Mutex();

        public MainViewModel()
        {
            Servers = new ObservableCollection<ServerConnectionDelegate>();

            GoCommand = new RelayCommand(OnGoRequested);
            RefreshCommand = new RelayCommand(OnRefreshRequested);
            SaveToFileCommand = new RelayCommand(OnSaveToFileRequested);

            UpdateServers(SERVER_LIST_FILENAME);

            CommandString = Properties.Settings.Default.SavedCommand;
        }

        public ObservableCollection<ServerConnectionDelegate> Servers { get; }

        private ServerConnectionDelegate _selectedServer = null;
        public ServerConnectionDelegate SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (_selectedServer != null) _selectedServer.IsSelected = false;
                _selectedServer = value;
                if (_selectedServer != null) _selectedServer.IsSelected = true;
                NotifyPropertyChanged(nameof(SelectedServer));
            }
        }

        private int ProcessingCount
        {
            get => _processingCount;
            set
            {
                _processingMutex.WaitOne();
                {
                    _processingCount = value;
                    if (_processingCount <= 0)
                        Processing = false;
                    else
                        if (!Processing)
                            Processing = true;
                }
                _processingMutex.ReleaseMutex();
            }
        }

        public bool Processing
        {
            get => _processing;
            set
            {
                if (_processing == value) return;
                _processing = value;
                NotifyPropertyChanged(nameof(Processing));
            }
        }

        public ICommand GoCommand { get; }
        public ICommand SaveToFileCommand { get; }
        public ICommand RefreshCommand { get; }

        private string _commandString = string.Empty;
        public string CommandString
        {
            get => _commandString;
            set
            {
                if (_commandString == value) return;
                _commandString = value;
                NotifyPropertyChanged(nameof(CommandString));

                Properties.Settings.Default["SavedCommand"] = _commandString;
                Properties.Settings.Default.Save();
            }
        }

        public string ServerConfig { get; set; }

        public string OutputServerResponse
        {
            get => _outputServerResponse;
            private set
            {
                if (_outputServerResponse == value) return;
                _outputServerResponse = value;
                NotifyPropertyChanged(nameof(OutputServerResponse));
            }
        }

        private void OnGoRequested(object parameter)
        {
            OutputServerResponse = string.Empty;

            Processing = true;

            List<Task> tasks = new List<Task>();
            foreach(var server in Servers)
            {
                server.ServerResponse = string.Empty;

                var task = Task.Run(() =>
                {
                    server.InProgress = true;
                    {
                        server.Connect();
                        server.Run(CommandString);
                    }
                    server.InProgress = false;
                });

                tasks.Add(task);
            }
        }

        private void OnRefreshRequested(object obj)
        {
            UpdateServers(SERVER_LIST_FILENAME);
        }

        private void OnSaveToFileRequested(object obj)
        {
            var dialog = new SaveFileDialog()
            {
                RestoreDirectory = true,
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("COMMAND: " + CommandString + Environment.NewLine);

                foreach(var server in Servers)
                {
                    sb.AppendLine(server.GetSaveContent());
                    sb.AppendLine(Environment.NewLine);
                }
                File.WriteAllText(dialog.FileName, sb.ToString());
            }
        }

        private void UpdateServers(string server_filename)
        {
            while(Servers.Count > 0)
                DisposeDelegate(Servers[0]);

            foreach(var line in File.ReadAllLines(server_filename).Where(line => ! line.StartsWith("#")))
                Servers.Add(CreateServerConnection(line));
        }

        private void DisposeDelegate(ServerConnectionDelegate server_delegate)
        {
            server_delegate.NotifyConnected          -= OnServerConnectedNotified;
            server_delegate.NotifyServerResponse     -= OnServerResponseNotified;
            server_delegate.NotifyConfigParseError   -= OnServerConfigParseErrorNotified;
            server_delegate.NotifyOperationCompleted -= OnServerOperationCompleteNotified;

            server_delegate.Dispose();

            Servers.Remove(server_delegate);
        }

        private ServerConnectionDelegate CreateServerConnection(string config)
        {
            var server_delegate = new ServerConnectionDelegate(config);

            server_delegate.NotifyConnected          += OnServerConnectedNotified;
            server_delegate.NotifyServerResponse     += OnServerResponseNotified;
            server_delegate.NotifyConfigParseError   += OnServerConfigParseErrorNotified;
            server_delegate.NotifyOperationCompleted += OnServerOperationCompleteNotified;

            return server_delegate;
        }

        private List<string> ParseAndGetServerStrings(string serverConfig)
        {
            return serverConfig
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .Where(l => l.Length > 0)
                .ToList();
        }

        private void OnServerConnectedNotified(object sender, EventArgs e) => ProcessingCount++;

        private void OnServerResponseNotified(object sender, string response)
        {
            _outputLogMutex.WaitOne();
            {
                var sb = new StringBuilder();
                sb.AppendLine(new string('*', ASTERISK_COUNT));
                sb.AppendLine("SERVER: " + (sender as ServerConnectionDelegate).Server);
                //sb.AppendLine(new string('~', ASTERISK_COUNT));
                sb.AppendLine(response);
                sb.AppendLine(new string('*', ASTERISK_COUNT));
                OutputServerResponse += Environment.NewLine + sb.ToString();
            }
            _outputLogMutex.ReleaseMutex();
        }

        private void OnServerConfigParseErrorNotified(object sender, string message)
        {
            var sb = new StringBuilder();
            sb.AppendLine(new string('*', ASTERISK_COUNT));
            sb.AppendLine(string.Format("Could not parse '{0}'", (sender as ServerConnectionDelegate).Config));
            sb.AppendLine("Error: " + message);
            sb.AppendLine(new string('*', ASTERISK_COUNT));

            ProcessingCount--;

            Console.WriteLine(sb.ToString());
        }

        private void OnServerOperationCompleteNotified(object sender, EventArgs e)
        {
            ProcessingCount--;
            var server = sender as ServerConnectionDelegate;
            server?.Dispose();
        }
    }
}
