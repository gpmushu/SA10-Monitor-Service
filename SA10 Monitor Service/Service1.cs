using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net;
using System.IO;
using Newtonsoft.Json;


namespace SA10_Monitor_Service
{
    public partial class Service1 : ServiceBase
    {
        //Initialize timers
        Timer hourTimer = new Timer();
        Timer dayTimer = new Timer();
        DateTime uploadTime = DateTime.Today.AddHours(1);

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Run the SA10 Monitoring Utility on start and on hourTimer elapse
            RunUtility();
            hourTimer.Elapsed += new ElapsedEventHandler(OnHourElapsed);
            //Reset hourTimer
            hourTimer.Interval = 1000 * 60 * 60;
            hourTimer.Enabled = true;
           
            //Run httpPost on dayTimer elapse and reset timer
            dayTimer.Elapsed += new ElapsedEventHandler(OnDayElapsed);
            //Reset dayTimer
            double nextTime = uploadTime.Subtract(DateTime.Now).TotalMilliseconds;
            if (nextTime < 0) nextTime += new TimeSpan(24, 0, 0).TotalMilliseconds;
            dayTimer.Interval = nextTime;
            dayTimer.Enabled = true;
        }

        //OnStop event runs SA10 Monitoring Utility
        protected override void OnStop()
        {
            RunUtility();
        }

        //hourTimer elapse event runs SA10 Monitoring Utility
        private void OnHourElapsed(object source, ElapsedEventArgs e)
        {
            RunUtility();
        }

        //dayTimer elapse event runs httpPost and FileCleanup
        private void OnDayElapsed(object source, ElapsedEventArgs e)
        {
            HttpPost();
            FileCleanup();
        }
        
        //Method to run SA10 Monitoring Utility
        public void RunUtility()
        {
            Process.Start("T:\\SA10 Monitoring Utility\\SA10 Monitoring Utility.exe");
        }

        //Method to post to SQL Database
        public void HttpPost()
        {
            //Iterate through log files
            foreach (string file in Directory.EnumerateFiles("T:\\SA10 Monitoring Utility\\", "*.log"))
            {
                //Iterate through lines in each file
                foreach (string line in File.ReadLines(file))
                {
                    using (WebClient client = new WebClient())
                    {
                        //Post line to SQL Database in JSON format
                        client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                        client.UploadString("https://sa10monitorwebapi.azurewebsites.net/api/database", line);
                    }
                }
                //Rename file to .bak, if .bak file exists, use .bak1
                try
                {
                    File.Move(file, file + ".bak");
                }
                catch (IOException)
                {
                    File.Move(file, file + ".bak1");
                }
            }
        }

        //Method to delete log files older than 90 days
        public void FileCleanup()
        {
            foreach (string file in Directory.EnumerateFiles("T:\\SA10 Monitoring Utility\\", "*.bak"))
            {
                FileInfo info = new FileInfo(file);
                if (info.CreationTime < DateTime.Now.AddMonths(-3))
                    File.Delete(file);
            }
        }
    }
}
