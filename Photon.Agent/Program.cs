﻿using log4net;
using log4net.Config;
using Photon.Agent.Internal;
using System;
using System.ServiceProcess;

namespace Photon.Agent
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));


        public static int Main(string[] args)
        {
            XmlConfigurator.Configure();

            try {
                Arguments.Parse(args);
            }
            catch (Exception error) {
                Log.Fatal("Failed to parse arguments!", error);
                return 1;
            }

            try {
                if (Arguments.RunAsConsole) {
                    return RunAsConsole(args);
                }
                else {
                    ServiceBase.Run(new [] {
                        new AgentService(),
                    });
                }
            }
            catch (Exception error) {
                Log.Fatal("Unhandled Exception!", error);
                return 1;
            }

            return 0;
        }

        private static int RunAsConsole(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Photon Agent");
            Console.ResetColor();

            PhotonAgent server = null;
            try {
                server = new PhotonAgent();
                server.Start();

                Console.ReadKey(true);
            }
            finally {
                server?.Dispose();
            }


            return 0;
        }
    }
}