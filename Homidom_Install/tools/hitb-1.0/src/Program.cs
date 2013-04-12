﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace hitb
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {

                    case "--check-installed-svc":
                    case "-cis":
                        // vérifie si Homidom Server est installé en tant que service windows
                        Environment.Exit(CheckIfInstalledAsService() ? 1 : 0);
                        break;
                    case "--kill-all":
                    case "-ka":
                        // arrête tous les services et applications HoMIDom en cours d'execution (Kill)
                        Environment.Exit(killAll());
                        break;
                    case "--check-running-svc":
                    case "-crs":
                        Environment.Exit(CheckIfServiceIsRunning() ? 1 : 0);
                        break;
                    case "--stop-service":
                    case "-sps":
                        StopService("Homidom");
                        KillProcess("HomidomService");
                        Environment.Exit(0);
                        break;
                    case "--start-service":
                    case "-sts":
                        Environment.Exit(StartService("Homidom") ? 1 : 0);
                        break;
                    case "--check-running-app":
                    case "-cra":
                        Environment.Exit(IsProcessRunning("HomiAdmin") || IsProcessRunning("HomiWpf") ? 1 : 0);
                        break;
                    default:
                        break;
                }

            }
            else
            {
                ShowUsage();
            }

        }

        private static bool StartService(string serviceName, int timeoutMilliseconds = 5000)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("HoMIDoM Installer ToolBox v1.0");
            Console.WriteLine("Usage:");
            Console.WriteLine("hitb.exe <command> [<options>]\r\n");
        }

        private static bool CheckIfServiceIsRunning()
        {
            // recherche du service windows -ou- du process
            Process[] pname = Process.GetProcessesByName("HomidomService");
            ServiceController ctl = ServiceController.GetServices().Where(s => s.ServiceName.ToLower() == "homidom").FirstOrDefault();
            if (ctl != null && ctl.Status == ServiceControllerStatus.Running)
                return true;

             return (pname.Length != 0);

        }

        private static bool CheckIfInstalledAsService()
        {
            try
            {
                ServiceController ctl = ServiceController.GetServices().Where(s => s.ServiceName.ToLower() == "homidom").FirstOrDefault();
                return (ctl != null);
            }
            catch (Exception)
            {

                return false;
            }

        }

        private static void KillProcess(string processName)
        {
            try
            {
                Process[] pname = Process.GetProcessesByName(processName);
                if (pname.Length != 0)
                    pname[0].Kill();

            }
            catch (Exception)
            {

            }

        }

        private static bool IsProcessRunning(string processName)
        {
            try
            {
                Process[] pname = Process.GetProcessesByName(processName);
                return (pname.Length != 0);
            }
            catch (Exception)
            {
                return false;
            }

        }

        static int killAll()
        {
            try
            {
                KillProcess("HomiWpf");
                KillProcess("HomiAdmin");
                KillProcess("HomidomService");

                return 0;
            }
            catch (Exception)
            {

                return 5;
            }

        }

        public static void StopService(string serviceName, int timeoutMilliseconds = 5000)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch
            {
                // ...
            }
        }
    }
}
