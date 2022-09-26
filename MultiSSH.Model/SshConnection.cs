using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MultiSSH.Model
{
    public class SshConnection : IDisposable
    {
        private readonly string _server;
        private readonly string _username;
        private readonly string _password_token;
        private readonly SshClient _client = null;

        // Bloody terrible - but it's okay for personal use!
        private const string FILE_PATH = "passwords.txt";
        private static Dictionary<string, string> _passwords = new Dictionary<string, string>();
        
        static SshConnection()
        {
            var lines = File.ReadAllLines(FILE_PATH, Encoding.UTF8);
            foreach (var line in lines)
            {
                var parts = line.Split();
                _passwords[parts[0]] = parts[1];
            }
        }

        public SshConnection(string server, string username, string password_token)
        {
            this._server = server;
            this._username = username;
            this._password_token = password_token;

            try
            {
                _client = new SshClient(_server, _username, GetPassword(_password_token));
            }
            catch (Exception e)
            {
                Error += Environment.NewLine;
                Error += e.Message;
            }

            IsDisposed = false;
        }

        public bool IsConnected => _client != null ? _client.IsConnected : false;

        public void Connect()
        {
            try
            {
                _client.Connect();
            }
            catch (Exception e)
            {
                Error += Environment.NewLine;
                Error += e.Message;
            }
        }

        public string Error { get; private set; }

        public bool IsDisposed { get; private set; }

        public string Command { get; set; }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            _client.Disconnect();
            _client.Dispose();
        }

        public string Run()
        {
            if (IsDisposed)
                throw new InvalidOperationException("Client already disposed! Server: " + _server);

            if (_client.IsConnected)
            {
                using (var command = _client.CreateCommand(Command))
                {
                    try
                    {
                        return command.Execute();
                    }
                    catch (Exception e)
                    {
                        Error += Environment.NewLine;
                        Error += e.Message;
                    }
                }
            }

            return Error;
        }

        public string Run(string command)
        {
            Command = command;
            return Run();
        }

        private string GetPassword(string password_token)
        {
            return _passwords[password_token];
        }
    }
}
