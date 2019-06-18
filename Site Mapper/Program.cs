using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

/*
 * fix formatting
 * write output to logs/joshsawyerdev.txt : logs/ehsawyeruk.txt
*/

/*
 * 1 thread - grab data
 * 1 thread - process
 * 
 * once 1st thread grabbed all data, move to process
 */

namespace Site_Mapper
{
    class Program
    {
        static readonly string[]
            // List of extentions that represent webpages
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

            // List of domain extensions
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
        
        // Defining lists
        static List<string>
            urls = new List<string>(),
            resources = new List<string>(),
            externals = new List<string>();

        // Core url, e.g. (domain.ext/)
        static string baseUrl;

        static bool processComplete = false;
        static int seconds = 0;

        // Main function
        static void Main()
        {
            CLI();
        }

        // Function for the command line interface
        static void CLI()
        {
            while (true)
            {
                // Get base URL input
                Console.Write("Base URL: ");
                baseUrl = Console.ReadLine();

                Console.WriteLine("Validating base URL...");

                // Validating the base URL

                baseUrl = baseUrl.Replace("https://", "");
                baseUrl = baseUrl.Replace("http://", "");

                baseUrl = baseUrl.Replace("https:\\\\", "");
                baseUrl = baseUrl.Replace("http:\\\\", "");

                baseUrl = baseUrl.Replace("www.", "");

                if (baseUrl[baseUrl.Length - 1] == '/')
                {
                    baseUrl = baseUrl.Remove(baseUrl.Length - 1);
                }
                if (baseUrl[0] == '/')
                {
                    baseUrl = baseUrl.Remove(0);
                }

                if (GetHtml("https://" + baseUrl) == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Bad base URL...\n");
                    Console.ForegroundColor = ConsoleColor.White;

                    continue;
                }

                Console.WriteLine("Validated!\n");

                Console.WriteLine("Processing: " + baseUrl);
                Console.WriteLine("Please wait, this may take a while!\n");

                Console.Write("Crawling site: 0s");

                // Starts the process
                Start(baseUrl);

                // Abort 2nd thread
                processComplete = true;

                // Display results
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
            Console.WriteLine("\nPress any key to end program...");
            Console.ReadKey();
        }
        static void Start(string url)
        {
            // Start timer thread
            ThreadStart timerThrStrt = new ThreadStart(timerThrInit);
            Thread timerThr = new Thread(timerThrStrt);
            timerThr.Start();

            // Start the crawling process
            SearchForUrl(url);

            // Abort 2nd thread
            processComplete = true;
        }

        // 2nd thread function
        static void timerThrInit()
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

        // Function to format results
        static string Format(string url)
        {
            if (url == "")
            {
                return "";
            }

            //url = url.ToLower();

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

        // Function to search a page for links
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

                content = Format(content);

                AddUrl(content);

                html = html.Remove(hrefPos, contentEnd - hrefPos);
            }
        }

        // Function to validate and add a URL, resource or external
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
                    foreach (string ext in externals)
                    {
                        if (Format(url) == ext ||
                            url == ext)
                        {
                            return;
                        }
                    }

                    externals.Add(Format(url));

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
                foreach (string curUrl in urls)
                {
                    if (Format(url.Replace(baseUrl, "")) == curUrl ||
                        url == curUrl)
                    {
                        return;
                    }
                }

                if (url.IndexOf(baseUrl) == 0 || !url.Contains(baseUrl))
                {
                    urls.Add(Format(url.Replace(baseUrl, "")));
                }
                else
                {
                    urls.Add(Format(url));
                }

                if (!url.Contains("#"))
                {
                    if (url.Contains(baseUrl) && url.IndexOf(baseUrl) == 0)
                    {
                        SearchForUrl(Format(url));
                    }
                    else
                    {
                        SearchForUrl(Format(baseUrl + url));
                    }
                }
            }
            else
            {
                foreach (string curResrc in resources)
                {
                    if (Format(url.Replace(baseUrl, "")) == curResrc ||
                        url == curResrc)
                    {
                        return;
                    }
                }

                if (!url.Contains(".") && !url.Contains(":"))
                {
                    return;
                }

                if (url.IndexOf(baseUrl) == 0 || !url.Contains(baseUrl))
                {
                    resources.Add(Format(url.Replace(baseUrl, "")));
                }
                else
                {
                    resources.Add(Format(url));
                }
            }
        }

        // Funtion to get the raw text of a page
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
                        return null;
                    }
                }
            }
        }

        // Removes newlines
        static string RNL(string content)
        {
            while (true)
            {
                int nlIndex = content.IndexOf('\n');
                if (nlIndex < 0)
                {
                    break;
                }

                content = content.Remove(nlIndex, 1);
            }
            return content;
        }

        // Function to sort string array
        static string[] Sort(string[] toSort)
        {
            Array.Sort(toSort, StringComparer.InvariantCulture);
            return toSort;
        }
    }
}
