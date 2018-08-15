using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.UI.Xaml;

namespace FloAssistant
{
    class LiveboxServer
    {
        internal static readonly string LogFile = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "LiveboxServer.log");
        private readonly string USERNAME = "admin"; //login pour accéder à la console d'administration de la livebox

        private string PASSWORD = null; //password pour accéder à la console d'administration de la livebox.
        private readonly string ORIGIN = "http://192.168.1.1";
        private DB Db;

        public LiveboxServer(DB db)
        {
            Db = db;
            loadCode();
        }

        private void loadCode()
        {
            // Read the file and display it line by line.  
            string[] files = File.ReadAllLines(@"Code.txt");
            foreach (string line in files)
            {
                string[] lineSplitted = line.Split('=');
             
                if (lineSplitted[0] == "LIVEBOXPASSWORD")
                {
                    PASSWORD = lineSplitted[1];
                }

            }

        }

        public async Task<List<ResultMobileState>> DetectConnectedDevices()
        {
            Log("Start : DetectConnectedDevices");
              
            string token;
            string httpResponseBody;
            bool mobileExistInDB;
            
            string macAddress = null;
            string name = null;
            string owner = null;
            string state = null;
            bool stateChanged = false;
            
            List<ResultMobileState> resultMobileStateList = new List<ResultMobileState>();
            /*
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            */
            try
            {/*
                token = await GetToken(string.Format(ORIGIN, USERNAME, PASSWORD));
                //Log("Token : " + token);

                //On récupére les appareils connectés
                Uri uri = new Uri(ORIGIN + "/sysbus/Devices:get");
                StringContent content = new StringContent("{\"parameters\":{}}", Encoding.UTF8, "application/json");
                content.Headers.Add("X-Context", token);
                httpResponse = await httpClient.PostAsync(uri, content);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                //Log(httpResponseBody);

                //On désérialise en JSON
                GetDevice item1 = Newtonsoft.Json.JsonConvert.DeserializeObject<GetDevice>(httpResponseBody);
                //Log("Result : " + item1.result.Status);
                
                foreach (var devicesFromLivebox in item1.result.Status)
                {
                    if (devicesFromLivebox.DeviceType == "Mobile")
                    {
                        mobileExistInDB = false;

                        //Log(devicesFromLivebox.Name);
                        //Log(devicesFromLivebox.LastConnection.ToString());
                        //Log(DateTime.UtcNow.ToString());
                        //Log((DateTime.Now - devicesFromLivebox.LastConnection).TotalMinutes.ToString());

                        foreach (var mobileFromDB in Db.RetrieveMobiles())
                        {
                            if (mobileFromDB.MACAddress == devicesFromLivebox.Key)
                            {
                                mobileExistInDB = true;
                                macAddress = devicesFromLivebox.Key;
                                name = devicesFromLivebox.Name; //Nom du téléphone enregistré dans la Livebox
                                owner = mobileFromDB.Owner; //Prénom de la personne enregistré dans la DB
                                
                                //Mobile Online
                                if ((DateTime.UtcNow - devicesFromLivebox.LastConnection).TotalSeconds < 5)
                                {
                                    //Le mobile vient de se connecter au Wifi
                                    if (mobileFromDB.LastState == "Offline")
                                    {
                                        Log(macAddress + " (" + owner + ") vient de se connecter");
                                        stateChanged = true;
                                        state = "Online";
                                    }
                                    else //Le mobile était déjà connecté au Wifi
                                    {
                                        Log(macAddress + " (" + owner + ") est connecté");
                                        stateChanged = false;
                                        state = "Online";
                                    }
                                }
                                //Mobile Offline
                                else
                                {
                                    //Le mobile vient de se déconnecter au Wifi
                                    if (mobileFromDB.LastState == "Online")
                                    {
                                        Log(macAddress + " (" + owner + ") vient de se déconnecter");
                                        stateChanged = true;
                                        state = "Offline";
                                    }
                                    else
                                    {
                                        Log(macAddress + " (" + owner + ") est déconnecté");
                                        stateChanged = false;
                                        state = "Offline";
                                    }
                                }
                                Db.UpdateMobile(devicesFromLivebox.Key, devicesFromLivebox.Name, devicesFromLivebox.LastConnection, state); 
                            }
                        }
                        if (mobileExistInDB == false)
                        {
                            Db.AddMobile(devicesFromLivebox.Key, devicesFromLivebox.Name, devicesFromLivebox.LastConnection, state);
                        }
                        resultMobileStateList.Add(new ResultMobileState() { MACAddress = macAddress, Name = name, Owner = owner, State = state, StateChanged = stateChanged });
                    }
                }*/
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw;
            }
            Log("End : DetectConnectedDevices");
            return resultMobileStateList;
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

        public class ResultMobileState
        {
            public string MACAddress;
            public string Name;
            public string Owner;
            public string State;
            public bool StateChanged;
        }
        
        private async Task<string> GetToken(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            var uri = new Uri(string.Format(ORIGIN + "/authenticate?username={0}&password={1}",USERNAME,PASSWORD));
            string token;
            try
            {
                httpResponse = await httpClient.PostAsync(uri, new StringContent("", Encoding.UTF8, "application/json"));
                httpResponse.EnsureSuccessStatusCode();
                string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                //On désérialise les données en JSON
                LoginData item = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginData>(httpResponseBody);
                token = item.data.contextID;
            }
            catch (Exception ex)
            {
                token = null;
                Log(ex.ToString());
                throw;
            }
            return token;
        }

        public static void Log(string logMessage, FileStream logFile1 = null)
        {
            // Append text to an existing file named "WriteLines.txt".
            Debug.WriteLine(DateTime.Now.ToString() + " " + logMessage);
            using (StreamWriter outputFile = File.AppendText(LogFile))
            {
                outputFile.WriteLine(DateTime.Now.ToString() + " " + logMessage);
                outputFile.Dispose();
            }
        }
    }
}
