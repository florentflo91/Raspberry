using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloAssistant.DBModel
{
    class Mobile
    {
        //The Id property is marked as the Primary Key
        //[SQLite.Net.Attributes.PrimaryKey, SQLite.Net.Attributes.AutoIncrement]
        public string MACAddress { get; set; }
        public string Name { get; set; }
        public DateTime LastConnectionDate { get; set; }
        public string Owner { get; set; }
        public string LastState { get; set; }
        // public List<Mobile> Mobile { get; set; }

        public Mobile()
        {
            //empty constructor
        }
        //public ListMobile(string macAddress, string name)
        //{
        //      Status;
        // }
        public Mobile(string macAddress, string name, DateTime lastConnectionDate, string lastState)
        {
            MACAddress = macAddress;
            Name = name;
            LastConnectionDate = lastConnectionDate;
            LastState = lastState;
        }

        public Mobile(string macAddress, string name, DateTime lastConnectionDate,  string lastState, string owner)
        {
            MACAddress = macAddress;
            Name = name;
            LastConnectionDate = lastConnectionDate;
            Owner = owner;
            LastState = lastState;
        }
    }
}

