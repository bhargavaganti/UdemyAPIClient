using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StudentWantsToKnow
{
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int HWND_TOPMOST = -1;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static string UDEMY_TOKEN = "<Use what Udemy gives you>";
        static string INSTRUCTOR_NAME = "Scott Duffy"; // associated with the token

        static void Main(string[] args)
        {
            while (true) {
                // check Udemy every 10 minutes
                int sleeptime = 1000 * 60 * 10;

                Console.WriteLine("Checking questions");
                // replace these Udemy course IDs with the courses you want to check
                GetQuestions(802578, "AZ-300");
                GetQuestions(802576, "AZ-103");
                GetQuestions(802574, "AZ-203");
                GetQuestions(2394982, "AZ-900");
                GetQuestions(2278883, "AZ-301");
                GetQuestions(1403814, "Serverless");

                Console.WriteLine("Checking messages");
                GetMessages();

                Console.WriteLine("Thats all! Sleeping at " + DateTime.Now.ToLocalTime());
                System.Threading.Thread.Sleep(sleeptime);
                
                // repeat, forever
                //Console.In.ReadLine();
            }
        }

        static void GetMessages()
        {
            Console.WriteLine();

            // Grab the first unanswered question, num_replies = 0
            string URI = "https://www.udemy.com/instructor-api/v1/message-threads/?status=unreplied,not_automated&fields%5Bmessage_thread%5D=@all&fields%5Bmessage%5D=@all";

            string HtmlResult = "";
            using (WebClient webclient = new WebClient())
            {
                webclient.Headers[HttpRequestHeader.Authorization] = "Bearer " + UDEMY_TOKEN;
                HtmlResult = webclient.DownloadString(URI);
            }
            JObject o = JObject.Parse(HtmlResult);

            // if none, exit
            if (!o["results"].HasValues)
            {
                return;
            }
            int qs = 0;
            foreach (JToken item in o["results"].Children())
            {
                if (DateTime.Parse(item["created"].Value<string>()) < DateTime.Now.AddDays(-3)) continue;
                qs++;
                Console.WriteLine("Name: " + item["last_message"]["user"]["title"].Value<string>());
                Console.WriteLine("Message: " + item["last_message"]["content"].Value<string>());
                string message_id = item["id"].Value<string>();

                Console.WriteLine();

                Console.SetIn(new StreamReader(Console.OpenStandardInput(),
                               Console.InputEncoding,
                               false,
                               bufferSize: 1024));
                string TheAnswer = Console.ReadLine();

                if (TheAnswer == "")
                {
                    Console.WriteLine("Skipping");
                    continue;
                }

                string ImSure = "N";

                while (ImSure != "Y")
                {
                    Console.WriteLine();
                    Console.WriteLine("Are you sure?");
                    ImSure = Console.In.ReadLine();
                }

                // Post the answer
                URI = "https://www.udemy.com/instructor-api/v1/message-threads/" + message_id + "/messages/";

                HtmlResult = "";
                string myParameters = "{\"content\":" + JsonConvert.ToString(TheAnswer) + "}";
                using (WebClient webclient = new WebClient())
                {
                    webclient.Headers[HttpRequestHeader.Authorization] = "Bearer " + UDEMY_TOKEN;
                    webclient.Headers[HttpRequestHeader.ContentType] = "application/json;charset=utf-8";
                    try
                    {
                        HtmlResult = webclient.UploadString(URI, myParameters);
                        o = JObject.Parse(HtmlResult);
                        Console.WriteLine("Confirmed");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception:" + ex.Message);
                    }
                    //HtmlResult = webclient.DownloadString(URI);
                }
                Console.WriteLine();

                if (qs > 4) break;

            }

        }

        static void GetQuestions(int courseid, string coursename)
        {
            Console.WriteLine();
            Console.WriteLine(coursename);

            // Grab the first unanswered question, num_replies = 0
            string URI = "https://www.udemy.com/instructor-api/v1/courses/"+ courseid.ToString() + "/questions/?fields%5Bquestion%5D=@all&ordering=-created";

            string HtmlResult = "";
            using (WebClient webclient = new WebClient())
            {
                webclient.Headers[HttpRequestHeader.Authorization] = "Bearer " + UDEMY_TOKEN;
                HtmlResult = webclient.DownloadString(URI);
            }
            JObject o = JObject.Parse(HtmlResult);

            // if none, sleep for 1 hour
            if (!o["results"].HasValues)
            {
                return;
            }
            JToken item2 = null;
            foreach (JToken item in o["results"].Children())
            {
                //Console.WriteLine("Title: " + item["title"].Value<string>());
                //Console.WriteLine("Body: " + item["body"].Value<string>());
                int alreadyanswered = 0;
                if (item["replies"].Count() > 0)
                {
                    foreach (JToken reply in item["replies"].Children())
                    {
                        //Console.WriteLine(reply["user"]["title"].Value<string>());
                        if (reply["user"]["title"].Value<string>() == "Scott Duffy") alreadyanswered = 1 ;
                    }
                }
                // if (Int32.Parse(item["num_replies"].Value<string>()) > 0) continue;
                if (alreadyanswered > 0) {
                    //Console.WriteLine("Already answered");
                    //Console.WriteLine();
                    continue;
                }
                if (DateTime.Parse(item["created"].Value<string>()) < DateTime.Now.AddDays(-3)) continue;
                //Console.WriteLine(item["num_replies"].Value<string>() + "," + item["created"].Value<string>());
                item2 = item;
                break;
            }

            if (item2 is null)
            {
                return;
            }

            // Prompt myself to answer it

            IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
            ShowWindow(hWnd, SW_SHOW);

            Console.WriteLine("Course: " + item2["course"]["title"].Value<string>());
            if (item2["related_lecture_title"].HasValues) {
                Console.WriteLine("Lecture: " + item2["related_lecture_title"].Value<string>());
            }
            Console.WriteLine("Title: " + item2["title"].Value<string>());
            Console.WriteLine("Body: " + item2["body"].Value<string>());
            string question_id = item2["id"].Value<string>();

            // replies
            if (item2["replies"].Count() > 0)
            {
                foreach (JToken reply in item2["replies"].Children())
                {
                    Console.WriteLine("Replied to already by : " + reply["user"]["title"].Value<string>());
                }
            }

            Console.WriteLine();

            Console.SetIn(new StreamReader(Console.OpenStandardInput(),
                           Console.InputEncoding,
                           false,
                           bufferSize: 1024));
            string TheAnswer = Console.ReadLine();

            string ImSure = "N";

            while (ImSure != "Y")
            {
                Console.WriteLine();
                Console.WriteLine("Are you sure?");
                ImSure = Console.In.ReadLine();
            }

            // Post the answer
            URI = "https://www.udemy.com/instructor-api/v1/courses/" + courseid.ToString() + "/questions/" + question_id + "/replies/";

            HtmlResult = "";
            string myParameters = "{\"body\":" + JsonConvert.ToString(TheAnswer) + "}";
            using (WebClient webclient = new WebClient())
            {
                webclient.Headers[HttpRequestHeader.Authorization] = "Bearer " + UDEMY_TOKEN;
                webclient.Headers[HttpRequestHeader.ContentType] = "application/json;charset=utf-8";
                try
                {
                    HtmlResult = webclient.UploadString(URI, myParameters);
                    o = JObject.Parse(HtmlResult);
                    Console.WriteLine("Confirmed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception:" + ex.Message);
                }
                //HtmlResult = webclient.DownloadString(URI);
            }
            Console.WriteLine();

        }
    }
}
