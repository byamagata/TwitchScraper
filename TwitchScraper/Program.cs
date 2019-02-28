using static System.Console;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TwitchScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebDriver driver = new ChromeDriver();
            driver.Url = "https://www.twitch.tv/directory";
            driver.Navigate();
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            


            List<string> viewers = new List<string>();
            List<string> gameNames = new List<string>();
            string firstViewer = "//*[@id=\"root\"]/div/div[2]/div/main/div/div[3]/div/div/div/div[3]/div/div/div[1]/div[2]/div/div/div/div[1]/div/a/div[2]/p";
            string firstName = "//*[@id=\"root\"]/div/div[2]/div/main/div/div[3]/div/div/div/div[3]/div/div/div[1]/div[2]/div/div/div/div[1]/div/a/div[2]/div/div/h3";
            bool isFound = false;
            int count = 0;
            int viewerCount = 2;
            const int MAX_NUMS = 100;
            int i = 0;
            while (i < MAX_NUMS)
            {
                try
                {
                    if (driver.FindElement(By.XPath(firstViewer)) != null)
                    {
                        var num = driver.FindElement(By.XPath(firstViewer));
                        var name = driver.FindElement(By.XPath(firstName));
                        viewers.Add(num.Text);
                        gameNames.Add(name.Text);
                        firstViewer = firstViewer.Replace($"div[1]/div[{viewerCount++}]", $"div[1]/div[{viewerCount--}]");
                        firstName = firstName.Replace($"div[1]/div[{viewerCount++}]", $"div[1]/div[{viewerCount}]");
                        js.ExecuteScript("arguments[0].scrollIntoView();", num);
                        i++;
                    }
                }
                catch(Exception e)
                {

                }
                count++;
            }

            int rank = 1;
            for(int j = 0; j < 100; j++)
            {
                WriteLine(gameNames[j] + " Rank: " + rank.ToString() + ": " + viewers[j]);
                rank++;
            }


            Regex r = new Regex("([0-9.]+K|[0-9.]+)");
            List<double> actualViewers = new List<double>();
            for (int x = 0; x < 100; x++)
            {
                Match m = r.Match(viewers[x]);
                if (m.Value.Contains("K"))
                {
                    string number = m.Value.Remove(m.Value.Length - 1);
                    actualViewers.Add(double.Parse(number) * 1000);
                }
                else
                {
                    actualViewers.Add(double.Parse(m.Value));
                }
            }

            int sum = 0;
            for (int x = 0; x < actualViewers.Count; x++)
            {
                sum += (int)actualViewers[x];
            }
            int avg = sum / actualViewers.Count;

            double runningSum = 0.0;
            for (int x = 0; x < actualViewers.Count; x++)
            {
                runningSum += Math.Pow(actualViewers[x] - avg, 2);
            }

            double almost = runningSum / actualViewers.Count;
            double stdDev = Math.Sqrt(almost);
            WriteLine("Standard Deviation: " + stdDev.ToString());
            double posStd = avg + stdDev;
            double negStd = avg - stdDev;
            if(negStd <= 0)
            {
                negStd = 0;
            }
            WriteLine("Lower Bound - Upper Bound");
            WriteLine(negStd.ToString() + " - " + posStd.ToString());

            //Games that fall within one std devitaion have less than the upper bound of viewers
            WriteLine("\n\nGames with viewers within the Standard Deviation:");
            for (int x = 0; x < actualViewers.Count; x++)
            {
                if(actualViewers[x] <= posStd)
                {
                    WriteLine(gameNames[x] + " " + viewers[x]);
                }
            }

            //now need to find the streamers on every single game page...
            //navigate from directory page to every games page
            // structure is https://twitch.tv/directory/game/NAME%20SEPARATED

            Dictionary<int, string> gameFollowers = new Dictionary<int, string>();
            string gameUrls = "https://twitch.tv/directory/game/";

            for (int x = 0; x < actualViewers.Count; x++)
            {
                if(actualViewers[x] <= posStd)
                {
                    string spaceSep = gameNames[x].Replace(" ", "%20");
                    spaceSep = spaceSep.Replace(":", "%3A");
                    string actualURL = gameUrls + spaceSep;

                    driver.Url = actualURL;
                    driver.Navigate();

                    bool found = false;
                    while (!found)
                    {
                        try
                        {
                            var followCount = driver.FindElement(By.XPath("//*[@id=\"root\"]/div/div[2]/div/main/div[1]/div[3]/div/div/div[1]/div[1]/div[2]/div/div[2]/div[1]/p"));
                            found = true;
                            gameFollowers[x] = followCount.Text;
                            WriteLine(gameNames[x] + " - Followers: " + gameFollowers[x]);
                            js.ExecuteScript("window.scrollTo(0, 400px)");

                        }
                        catch (Exception e) { }
                    }
                }
            }
        }
    }
}
