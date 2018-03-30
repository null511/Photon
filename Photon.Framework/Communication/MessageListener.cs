﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Photon.Framework.Communication
{
    /// <summary>
    /// Listens for incomming TCP message connections, and manages
    /// a collection of <see cref="MessageHost"/> instances.
    /// </summary>
    public class MessageListener
    {
        private readonly MessageProcessor processor;
        private readonly List<MessageHost> hostList;
        private readonly object startStopLock;
        private TcpListener listener;
        private bool isListening;


        public MessageListener(MessageProcessor processor)
        {
            this.processor = processor;

            hostList = new List<MessageHost>();
            startStopLock = new object();
        }

        public void Listen(IPAddress address, int port)
        {
            lock (startStopLock) {
                if (isListening) throw new Exception("Host is already listening!");
                isListening = true;
            }

            listener = new TcpListener(address, port);
            listener.Start();

            listener.BeginAcceptTcpClient(ConnectionReceived, new object());
        }

        public async Task Stop()
        {
            lock (startStopLock) {
                if (!isListening) return;
                isListening = false;
            }

            listener.Stop();

            var tasks = hostList.Select(x => x.StopAsync());
            await Task.WhenAll(tasks.ToArray());
        }

        private void ConnectionReceived(IAsyncResult result)
        {
            if (!isListening) return;

            TcpClient client;
            try {
                client = listener.EndAcceptTcpClient(result);
            }
            catch {
                // TODO: Exception Handling
                throw;
            }
            finally {
                listener.BeginAcceptTcpClient(ConnectionReceived, new object());
            }

            var host = new MessageHost(client, processor);
            host.Stopped += Host_Stopped;
            hostList.Add(host);
        }

        private void Host_Stopped(object sender, EventArgs e)
        {
            var host = (MessageHost)sender;
            hostList.Remove(host);
            host.Dispose();
        }
    }
}
