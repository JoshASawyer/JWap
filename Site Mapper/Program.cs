using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;

namespace Site_Mapper
{
    class Program
    {
        static readonly string[]
            EXTS = {
            ".htm",
            ".html",
            ".php",
            ".asp",
            ".aspx",
            "#",
            ".shtml",
            ".shtm"
            },

            TLDS = {
            ".com",
            ".uk",
            ".co.uk",
            ".org",
            ".net",
            ".edu",
            ".ac.uk",
            ".dev",
            ".io",
            ".online",
            ".org.uk",
            ".ai",
            ".biz",
            ".gov",
            ".gov.uk",
            ".ovh"
            };

        static List<string>
            urls = new List<string>(),
            resources = new List<string>(),
            externals = new List<string>();

        static string baseUrl;

        static bool processComplete = false;
        static int seconds = 0;
        static string SATEntryUrl = null;

        static string path = null;
        static string toLog = "";

        static void Main()
        {
            CLI();
        }

        static void CLI()
        {
            Console.ForegroundColor = ConsoleColor.White;
            while (true)
            {
                Console.Write("Base URL: ");
                baseUrl = Console.ReadLine();

                Console.WriteLine("Validating base URL...");

                baseUrl = Format(baseUrl, true);

                if (GetHtml("https://" + baseUrl) == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Bad base URL...\n");
                    Console.ForegroundColor = ConsoleColor.White;

                    continue;
                }
                path = GetFileName(baseUrl);

                if (!Directory.Exists("logs/"))
                {
                    Directory.CreateDirectory("logs/");
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                Console.WriteLine("Validated!\n");

                Console.WriteLine("Processing: " + baseUrl);
                Console.WriteLine("Please wait, this may take a while!\n");

                Console.Write("Crawling site: 0s");

                StartProcess(baseUrl);

                processComplete = true;

                Console.WriteLine("\n----------");
                Console.WriteLine(baseUrl);
                Console.WriteLine("----------\n");

                Console.WriteLine("Pages / Page Locations");
                Console.ForegroundColor = ConsoleColor.Cyan;
                foreach (string curUrl in Sort(urls.ToArray()))
                    Console.WriteLine("    " + curUrl);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("-");

                Console.WriteLine("Resources");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                foreach (string curResource in Sort(resources.ToArray()))
                    Console.WriteLine("    " + curResource);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("-");

                Console.WriteLine("Externals");
                Console.ForegroundColor = ConsoleColor.Magenta;
                foreach (string curExt in Sort(externals.ToArray()))
                    Console.WriteLine("    " + curExt);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("-");

                break;
            }

            Log(null);
            SaveMap();
            Exit();
        }

        static void StartProcess(string url)
        {
            ThreadStart timerThrStrt = new ThreadStart(TimerThrInit);
            Thread timerThr = new Thread(timerThrStrt);
            timerThr.Start();

            ThreadStart SATStart = new ThreadStart(SAThrInit);
            Thread SAThr = new Thread(SATStart);
            SAThr.Start();

            SearchForUrl(url);

            processComplete = true;
        }

        static void TimerThrInit()
        {
            while (true)
            {
                if (processComplete)
                {
                    return;
                }

                seconds++;
                Console.Write("\rCrawling site... (" + seconds + "s)");

                Thread.Sleep(1000);
            }
        }

        static void SAThrInit()
        {
            while (true)
            {
                if (SATEntryUrl == null)
                {
                    continue;
                }

                SearchForUrl(SATEntryUrl);
                SATEntryUrl = null;
            }
        }

        static string Format(string url, bool init)
        {
            if (url == "")
            {
                return "";
            }

            url = url.ToLower();

            if (init)
            {
                url = url.Replace("https://", "");
                url = url.Replace("http://", "");

                url = url.Replace("https:\\\\", "");
                url = url.Replace("http:\\\\", "");

                url = url.Replace("www.", "");

                if (url[url.Length - 1] == '/')
                {
                    url = url.Remove(url.Length - 1);
                }
                if (url[0] == '/')
                {
                    url = url.Remove(0);
                }
                return url;
            }

            bool localResource = false;
            if (url.Contains(baseUrl) && url.IndexOf(baseUrl) == 0)
            {
                url = url.Replace(baseUrl, "");
                localResource = true;
            }
            url = url.Replace("https://", "");
            url = url.Replace("http://", "");

            url = url.Replace("https:\\\\", "");
            url = url.Replace("http:\\\\", "");

            url = url.Replace("www.", "");

            if (url == "")
            {
                return "";
            }
            if (url[url.Length - 1] == '/')
            {
                url = url.Remove(url.Length - 1, 1);
            }

            if (url == "")
            {
                return "";
            }
            if (url[0] == '/')
            {
                url = url.Remove(0, 1);
            }
            
            if (localResource)
            {
                return baseUrl + '/' + url;
            }
            else
            {
                return url;
            }
        }

        static void SearchForUrl(string url)
        {
            string html = GetHtml("https://" + url);
            if (html == null)
            {
                return;
            }

            while (true)
            {
                int hrefPos = html.IndexOf("href=");
                char tag = html[hrefPos + 5];
                int contentStart = hrefPos + 6;

                if (hrefPos < 0)
                {
                    hrefPos = html.IndexOf("src=");
                    tag = html[hrefPos + 4];
                    contentStart = hrefPos + 5;

                    if (hrefPos < 0)
                    {
                        break;
                    }
                }

                if (html[contentStart] == tag)
                {
                    break;
                }

                int contentEnd = -1;
                for (int i = contentStart; i < html.Length - 1; i++)
                {
                    if (html[i] == tag)
                    {
                        contentEnd = i;
                        break;
                    }
                }

                string content = html.Substring(contentStart, contentEnd - contentStart);

                if (content.Contains("<script>") || content.Contains("</script>")
                    || content.Contains("{") || content.Contains("}"))
                {
                    return;
                }

                content = Format(content, false);

                AddUrl(content);

                html = html.Remove(hrefPos, contentEnd - hrefPos);
            }
        }

        static void AddUrl(string url)
        {
            if (url == "")
            {
                return;
            }

            foreach (string tld in TLDS)
            {
                if (url.Contains(tld) && !url.Contains(baseUrl))
                {
                    foreach (string ext in externals.ToArray())
                    {
                        if (Format(url, false) == ext ||
                            url == ext)
                        {
                            Log(url + " | ABANDONING DUPE");
                            return;
                        }
                    }

                    Log(url + " | ADDING AS EXTERNAL");

                    externals.Add(Format(url, false));

                    return;
                }
            }

            bool isPage = false;
            foreach (string ext in EXTS)
            {
                if (url.Contains(ext))
                {
                    isPage = true;
                    break;
                }
            }

            for (int i = url.Length - 1; i >= 0; i--)
            {
                if (url[i] == '.' && !isPage)
                {
                    break;
                }
                else if (url[i] == '/' || url[i] == '\\')
                {
                    isPage = true;
                    break;
                }
            }

            if (isPage)
            {
                foreach (string curUrl in urls.ToArray())
                {
                    if (Format(url.Replace(baseUrl, ""), false) == curUrl ||
                        url == curUrl)
                    {
                        Log(url + " | ABANDONING DUPE");
                        return;
                    }
                }

                Log(url + " | ADDING AS PAGE");

                if (url.IndexOf(baseUrl) == 0 || !url.Contains(baseUrl))
                {
                    urls.Add(Format(url.Replace(baseUrl, ""), false));
                }
                else
                {
                    urls.Add(Format(url, false));
                }

                if (!url.Contains("#"))
                {
                    if (url.Contains(baseUrl) && url.IndexOf(baseUrl) == 0)
                    {
                        if (SATEntryUrl == null)
                        {
                            SATEntryUrl = Format(url, false);
                        }
                        else
                        {
                            SearchForUrl(Format(url, false));
                        }
                    }
                    else
                    {
                        if (SATEntryUrl == null)
                        {
                            SATEntryUrl = Format(baseUrl + url, false);
                        }
                        else
                        {
                            SearchForUrl(Format(baseUrl + url, false));
                        }
                    }
                }
            }
            else
            {
                foreach (string curResrc in resources.ToArray())
                {
                    if (Format(url.Replace(baseUrl, ""), false) == curResrc ||
                        url == curResrc)
                    {
                        Log(url + " | ABANDONING DUPE");
                        return;
                    }
                }

                if (!url.Contains(".") && !url.Contains(":"))
                {
                    return;
                }

                Log(url + " | ADDING AS RESOURCE");
                
                if (url.IndexOf(baseUrl) == 0 || !url.Contains(baseUrl))
                {
                    resources.Add(Format(url.Replace(baseUrl, ""), false));
                }
                else
                {
                    resources.Add(Format(url, false));
                }
            }
        }

        static void Log(string msg)
        {
            if (msg != null)
            {
                toLog += msg + "\n\n";
                return;
            }
            Console.WriteLine("Finished, Logging...");
            File.WriteAllTextAsync(path, toLog);
            Console.WriteLine("Complete!");
        }

        static void SaveMap()
        {
            string textToSave = "";

            textToSave += "----------\n";
            textToSave += baseUrl + "\n";
            textToSave += "----------\n\n";

            textToSave += "Pages / Page Locations\n";
            foreach (string curUrl in Sort(urls.ToArray()))
                textToSave += "    " + curUrl + "\n";

            textToSave += "-\n";

            textToSave += "Resources\n";
            foreach (string curResource in Sort(resources.ToArray()))
                textToSave += "    " + curResource + "\n";

            textToSave += "-\n";

            textToSave += "Externals\n";
            foreach (string curExt in Sort(externals.ToArray()))
                textToSave += "    " + curExt + "\n";

            textToSave += "\nLook in '" + path + "' for logs.";

            File.WriteAllText("map_" + path.Replace("logs/", ""), textToSave);
        }

        static string GetHtml(string url)
        {
            try
            {
                return new WebClient().DownloadString(url);
            }
            catch
            {
                try
                {
                    return new WebClient().DownloadString(url.Replace("https", "http"));
                }
                catch
                {
                    try
                    {
                        return new WebClient().DownloadString(url.Replace("https://", "http://www."));
                    }
                    catch
                    {
                        Log(url + " | ABANDONING INVALID LINK");
                        return null;
                    }
                }
            }
        }

        static string[] Sort(string[] toSort)
        {
            Array.Sort(toSort, StringComparer.InvariantCulture);
            return toSort;
        }

        static string GetFileName(string url)
        {
            while (url.IndexOf('.') >= 0)
            {
                url = url.Remove(url.IndexOf('.'), 1);
            }

            int sIndex = url.IndexOf("/");
            if (sIndex >= 0)
            {
                url = url.Remove(sIndex, url.Length - sIndex);
            }

            return "logs/" + url + ".txt";
        }

        static void Exit()
        {
            Console.WriteLine("\nPress any key to end program...");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }
}
