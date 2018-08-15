using System;
using System.Collections;
using System.IO;
using System.Net;
//using System.Net.Cache;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;

namespace FloAssistant
{
    class LiveboxAdapter
    {
        internal static readonly string LogFile = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "LiveboxAdapter.log");
        
        public string UserName { get; set; }
        public string Password { get; set; }

        private static string UrlLogin = "/authenticate?username={0}&password={1}";

        public string Origin => "http://192.168.1.1";
        public DB Db;
        public MainPage gui;
        //private LoginResult _loginResult;

        public LiveboxAdapter(string userName, string password, DB db)
        {
            UserName = userName;
            Password = password;
            Db = db;
        }

        // JSON : Login //
        public class LoginData
        {
            public string status;
            public ContextID data;
        }

        public class ContextID
        {
            public String contextID;
        }
        // JSON : Login //

        public class GetDevice
        {
            public GetDeviceStatus result;            
        }
        public class GetDeviceStatus
        {
            public List<GetDeviceStatusName> Status;            
        }
        public class GetDeviceStatusName
        {
            public string Name;
            public string DeviceType;
            public string Key;
            public DateTime LastConnection;
        }

        public void LoginAsync(DB db)
        {
            //if (_cookieJar != null)
                //throw new InvalidOperationException("Already logged in");

            // Login and get status
              //GetConnectedDevices();
        }

        




        public static void Log(string logMessage, FileStream logFile1 = null)
        {
            // Append text to an existing file named "WriteLines.txt".
            //FileStream logFile1 = System.IO.File.AppendText(LogFile);
            Debug.WriteLine(DateTime.Now.ToString() + " " + logMessage);
            using (StreamWriter outputFile = File.AppendText(LogFile))
            {

                outputFile.WriteLine(DateTime.Now.ToString() + " " + logMessage);
                outputFile.Dispose();
            }
        }
    }
}
