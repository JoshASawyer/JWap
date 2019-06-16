using System;
using System.Collections.Generic;
using System.Net;

//using System.Threading;
/*
 * scan js and css for resources
 * 
 * multi-threading
 * 
 * support for local files
*/

namespace Site_Mapper
{
    class Program
    {
        // List of extentions that represent webpages
        static readonly string[] 
            EXTS = {
            ".htm",
            ".html",
            ".php",
            ".asp",
            ".aspx",
            "#"
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

        // Main function
        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.White;
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

                Console.WriteLine("Validated!");

                Console.WriteLine("Processing: " + baseUrl);
                Console.WriteLine("Please wait, this may take a while! Working...\n");

                // Start the crawling process
                SearchForUrl(baseUrl);

                // Display results
                Console.WriteLine("----------");
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

        // Function to sort string array
        static string[] Sort(string[] toSort)
        {
            Array.Sort(toSort, StringComparer.InvariantCulture);
            return toSort;
        }

        // Function to format results
        static string Format(string url, bool primary)
        {
            if (url == "")
            {
                return "";
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

            if (primary)
            {
                Console.WriteLine("Formatting: " + url);
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
                        if (Format(url, false) == ext)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine("Dupe Found, Abandoning: " + url + "\n");
                            Console.ForegroundColor = ConsoleColor.White;

                            return;
                        }
                    }

                    externals.Add(Format(url, false));

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Adding External: " + url + "\n");
                    Console.ForegroundColor = ConsoleColor.White;

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
                    if (Format(url.Replace(baseUrl, ""), false) == curUrl)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Dupe Found, Abandoning: " + url + "\n");
                        Console.ForegroundColor = ConsoleColor.White;

                        return;
                    }
                }

                if (url.IndexOf(baseUrl) == 0 || !url.Contains(baseUrl))
                {
                    urls.Add(Format(url.Replace(baseUrl, ""), false));
                }
                else
                {
                    urls.Add(Format(url, false));
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Adding Page / Page Location: " + url + "\n");
                Console.ForegroundColor = ConsoleColor.White;

                if (!url.Contains("#"))
                {
                    if (url.Contains(baseUrl))
                    {
                        SearchForUrl(Format(url, false));
                    }
                    else
                    {
                        SearchForUrl(Format(baseUrl + url, false));
                    }
                }
            }
            else
            {
                foreach (string curResrc in resources)
                {
                    if (Format(url.Replace(baseUrl, ""), false) == curResrc)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Dupe Found, Abandoning: " + url + "\n");
                        Console.ForegroundColor = ConsoleColor.White;

                        return;
                    }
                }

                if (!url.Contains(".") && !url.Contains(":"))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Not A Resource, Abandoning: " + url + "\n");
                    Console.ForegroundColor = ConsoleColor.White;

                    return;
                }

                if (url.IndexOf(baseUrl) == 0 || !url.Contains(baseUrl))
                {
                    resources.Add(Format(url.Replace(baseUrl, ""), false));
                }
                else
                {
                    resources.Add(Format(url, false));
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Adding Resource: " + url + "\n");
                Console.ForegroundColor = ConsoleColor.White;
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

                Console.WriteLine("Found: " + content);

                content = Format(content, true);

                AddUrl(content);

                html = html.Remove(hrefPos, contentEnd - hrefPos);
            }
        }

        // Funtion to get the raw text of a page
        static string GetHtml(string url)
        {
            Console.WriteLine("Getting: " + url);

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
                        return new WebClient().DownloadString(url.Replace("https://", "www."));
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid: " + url + "\n");
                        Console.ForegroundColor = ConsoleColor.White;
                        return null;
                    }
                }
            }
        }
    }
}
