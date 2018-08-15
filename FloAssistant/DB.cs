using System;
using System.Linq;
//using DBClass.SqliteUWP.ViewModel;
using System.IO;
using System.Diagnostics;
using FloAssistant.DBModel;
using SQLite.Net;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace FloAssistant
{
    class DB
    {
        string DB_PATH;

        public DB(string dbpath)
        {
            DB_PATH = dbpath;
            Debug.WriteLine("Init CLass DB");
            if (!File.Exists(this.DB_PATH))
            {
                Debug.WriteLine("Creating Database");
                using (SQLiteConnection conn = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), this.DB_PATH))
                {
                    conn.CreateTable<Mobile>();
                }
            }
        }

        public void AddMobile(string macAddress, string name, DateTime lastConnectionDate, string lastState)
        {
            Debug.WriteLine("Add Mobile to DB : " + macAddress + " " + name + " " + lastConnectionDate.ToString() + " " + lastState);
            try
            {
                var s = new Mobile { MACAddress = macAddress, Name = name, LastConnectionDate = lastConnectionDate, LastState = lastState};
                using (SQLiteConnection conn = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), this.DB_PATH))
                {
                    conn.RunInTransaction(() =>
                    {
                        conn.Insert(s);
                    });
                }
                //conn.DeleteAll<Message> ();
            }
            catch
            {
                Debug.WriteLine("ERROR : Unable to add Message");
            }
        }

        public void UpdateMobile(string macAddress, string name, DateTime lastConnectionDate, string lastState)
        {
            //Debug.WriteLine("Update Mobile to DB : " + macAddress + " " + name + " " + lastConnectionDate.ToString() + " " + lastState);
            try
            {
                var s = new Mobile { MACAddress = macAddress, Name = name, LastConnectionDate = lastConnectionDate, LastState = lastState};               
                using (SQLiteConnection conn = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), this.DB_PATH))
                {
                     var existingMobile = conn.Query<Mobile>("update  Mobile set LastState = '" + lastState + "', LastConnectionDate = '" + lastConnectionDate + "'  where MACAddress = '" + macAddress + "'");
                }
            }
            catch
            {
                Debug.WriteLine("ERROR : Unable to update Mobile");
            }
        }

        public ObservableCollection<Mobile> RetrieveMobiles()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), this.DB_PATH))
                {
                    List<Mobile> myCollection = conn.Table<Mobile>().ToList<Mobile>();
                    ObservableCollection<Mobile> ContactsList = new ObservableCollection<Mobile>(myCollection);
                    return ContactsList;
                }
            }
            catch
            {
                Debug.WriteLine("ERROR : Unable to retrieve last Message");
                return null;
            }
        }
    }
}
