using System;
using System.Collections.Generic;
using System.Net;

/*
 * Subdomain support (dbarchive.ehsawyer.uk) 
 * scan js and css for resources?
 * alphabetical output
 * multi-threading
*/

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
            "#"
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

        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.White;
            while (true)
            {
                Console.Write("Base URL: ");
                baseUrl = Console.ReadLine();

                Console.WriteLine("Validating base URL...");

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
               
                if (getHTML("https://" + baseUrl) == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Bad base URL...\n");
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

                Console.WriteLine("Validated!");

                Console.WriteLine("Processing: " + baseUrl);
                Console.WriteLine("Please wait, this may take a while! Working...\n");

                searchForUrl(baseUrl);

                Console.WriteLine("----------");
                Console.WriteLine("SUMMARY OF");
                Console.WriteLine(baseUrl);
                Console.WriteLine("----------");

                Console.WriteLine("Pages / Page Locations");
                Console.ForegroundColor = ConsoleColor.Cyan;
                foreach (string curUrl in urls)
                    Console.WriteLine("    " + curUrl);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("-");

                Console.WriteLine("Resources");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                foreach (string curResource in resources)
                    Console.WriteLine("    " + curResource);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("-");

                Console.WriteLine("Externals");
                Console.ForegroundColor = ConsoleColor.Magenta;
                foreach (string curExt in externals)
                    Console.WriteLine("    " + curExt);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("-");

                break;
            }
            Console.WriteLine("\nPress any key to end program...");
            Console.ReadKey();
        }

        static string[] sort(string[] toSort)
        {
            return null; // alphabetically sort
        }

        static string format(string url, bool primary)
        {
            if (url == "")
            {
                return "";
            }

            bool localResource = false;
            if (url.Contains(baseUrl))
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

        static void addUrl(string url)
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
                        if (format(url, false) == ext)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Dupe Found, Abandoning: " + url + "\n");
                            Console.ForegroundColor = ConsoleColor.White;
                            return;
                        }
                    }

                    externals.Add(format(url, false));

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
                    if (format(url.Replace(baseUrl, ""), false) == curUrl)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Dupe Found, Abandoning: " + url + "\n");
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                }

                urls.Add(format(url.Replace(baseUrl, ""), false));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Adding Page / Page Location: " + url + "\n");
                Console.ForegroundColor = ConsoleColor.White;

                if (!url.Contains("#"))
                {
                    searchForUrl(baseUrl + "/" + format(url, false));
                }
            }
            else
            {
                foreach (string curResrc in resources)
                {
                    if (format(url.Replace(baseUrl, ""), false) == curResrc)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Dupe Found, Abandoning: " + url + "\n");
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                }

                resources.Add(format(url.Replace(baseUrl, ""), false));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Adding Resource: " + url + "\n");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static void searchForUrl(string url)
        {
            string html = getHTML("https://" + url);
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

                content = format(content, true);

                addUrl(content);

                html = html.Remove(hrefPos, contentEnd - hrefPos);
            }
        }

        static string getHTML(string url)
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
                        Console.WriteLine("Invalid: " + url);
                        Console.ForegroundColor = ConsoleColor.White;
                        return null;
                    }
                }
            }
        }
    }
}
