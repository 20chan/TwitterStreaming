using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Fiddler;
using TwitterStreaming.Properties;
using Newtonsoft.Json.Linq;

namespace TwitterStreaming
{
    class Program
    {
        const string ID_CLASS_NAME = "js-username-field";
        const string PW_CLASS_NAME = "js-password-field";
        const string LOGIN_CLASS_NAME = "submit";

        static ChromeDriver driver;
        static HashSet<long> idSet = new HashSet<long>();

        static void Main(string[] args)
        {
            Init();
            Login();
            Console.Read();
        }

        static void Init()
        {
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            var options = new ChromeOptions()
            {
                Proxy = new OpenQA.Selenium.Proxy()
                {
                    HttpProxy = "localhost:8880",
                    SslProxy = "localhost:8880"
                }
            };
            driver = new ChromeDriver(service, options);

            FiddlerApplication.BeforeResponse += FiddlerApplication_BeforeResponse;
            FiddlerApplication.Startup(8880, false, true);
        }

        static void FiddlerApplication_BeforeResponse(Fiddler.Session oSession)
        {
            if (oSession.RequestMethod == "GET" &&
                oSession.fullUrl.Contains("home_timeline.json"))
            {
                try
                {
                    Console.WriteLine("Check!");
                    var json = JArray.Parse(oSession.GetResponseBodyAsString());
                    foreach (var t in json)
                    {
                        var id = (long)t["id"];
                        if (idSet.Contains(id))
                            continue;

                        Console.WriteLine(t["full_text"]);

                        // 임시방편으로 Set을 사용함
                        idSet.Add((long)t["id"]);
                    }
                }
                catch (Exception ex) {
                    var body = oSession.GetResponseBodyAsString();
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static void Login()
        {
            driver.Url = "https://tweetdeck.twitter.com";
            WaitUntilLoadOne(By.TagName("a")).Click();

            WaitUntilLoadOne(By.ClassName(ID_CLASS_NAME))
                .SendKeys(Settings.Default.ID);
            driver.FindElementByClassName(PW_CLASS_NAME)
                .SendKeys(Settings.Default.PW);
            driver.FindElementsByClassName(LOGIN_CLASS_NAME)[1]
                .Click();
        }

        static ReadOnlyCollection<IWebElement> WaitUntilLoad(By option)
        {
            while (true)
            {
                var elems = driver.FindElements(option);
                if (elems.Count > 0)
                    return elems;

                Thread.Sleep(100);
            }
        }
        static IWebElement WaitUntilLoadOne(By option)
            => WaitUntilLoad(option)[0];
    }
}
