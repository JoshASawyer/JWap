// Import modules used in this program
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;

/* 
 * Author: Josh Sawyer
 * Date Last Edited: 21/06/19 @ 22:51
 * 
 * Project Definition: A collection of scripts and programs used to
 * analyze websites. Currently only the Web Mapper is functional
 * and present.
*/

namespace JWap
{
    class Program
    {
        // Static read only string arrays
        static readonly string[]
            // To store the possible extensions of pages
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

            // To store possible domain name extensions
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

        // Static string lists
        static List<string>
            // To store all pages that are found
            urls = new List<string>(),
            // To store all resources that are found
            resources = new List<string>(),
            // To store all external links that are found
            externals = new List<string>();

        // Static string to store the base url, e.g. https://google.com
        static string baseUrl;

        // Static boolean to store whether the process is complete or not
        static bool processComplete = false;
        // Static string variable to store the next link to crawl
        static string SATEntryUrl = null;

        // Static string to store the path to the log file
        static string path = null;
        // Static string storing the message to log
        static string toLog = "";

        // Main method
        static void Main()
        {
            // Used to catch any errors and log them
            try
            {
                // Start the command line interface
                CLI();
            }
            // Catch any exception and store the value
            catch (Exception e)
            {
                // Used to stop all threads except for this main one
                processComplete = true;

                // Print the error to console in red and asks user to send it to the developer (me)
                Console.ForegroundColor = ConsoleColor.Red;
                // Prints the message
                Console.WriteLine("\nERROR CAUGHT:\n" + e + "\nPlease send your log file to github.com/JoshSawyer");
                // Resets console colour
                Console.ForegroundColor = ConsoleColor.White;

                // Log the error
                Log(e + " | ERROR THROWN");
                // Save the logs to the log file
                Log();

                // Exit the program
                Exit();
                // Ensure that no more code runs by returning out of the Main method
                return;
            }
        }

        // Command Line Interface method
        static void CLI()
        {
            // Change CLI text colour to white
            Console.ForegroundColor = ConsoleColor.White;
            // While loop for the getting of a valid base URL
            while (true)
            {
                // Ask for input
                Console.Write("Base URL: ");
                baseUrl = Console.ReadLine();

                Console.WriteLine("Validating base URL...");

                // Validate base URL
                baseUrl = Format(baseUrl, true);
                // Try to get the HTML, if cannot get new input
                if (GetHtml("https://" + baseUrl) == null)
                {
                    // Print out a red error message saying "Bad base URL..."
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Bad base URL...\n");
                    // Reset console colour
                    Console.ForegroundColor = ConsoleColor.White;

                    // Go back to beginning of while loop
                    continue;
                }
                // Break out of while loop if input is valid
                break;
            }
            // Now that base URL is got, construct the path to the log file
            path = GetFileName(baseUrl);

            // If the logs/ folder doesn't exist, create it
            if (!Directory.Exists("logs/"))
            {
                // Creates logs/ directory
                Directory.CreateDirectory("logs/");
            }
            // If file for current base URL already exists, delete it
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // Notify user of successful validation
            Console.WriteLine("Validated!\n");

            // Print the notification of the process starting
            Console.WriteLine("Processing: " + baseUrl);
            Console.WriteLine("Please wait, this may take a while!\n");

            // Calls StartProcess() on the base URL, starting the mapper
            StartProcess(baseUrl);

            // After it is done processComplete becomes true to tell the timer thread that processing has finished
            processComplete = true;

            // Write title for the output
            Console.WriteLine("\n----------");
            Console.WriteLine(baseUrl);
            Console.WriteLine("----------\n");

            // Print a list of pages found in cyan
            Console.WriteLine("Pages / Page Locations");
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (string curUrl in Sort(urls.ToArray()))
                Console.WriteLine("    " + curUrl);

            // Reset console colour and print divider ("-")
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("-");

            // Print a list of resources found in dark cyan
            Console.WriteLine("Resources");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            foreach (string curResource in Sort(resources.ToArray()))
                Console.WriteLine("    " + curResource);

            // Reset console colour and print divider ("-")
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("-");

            // Print a list of external links found in magenta
            Console.WriteLine("Externals");
            Console.ForegroundColor = ConsoleColor.Magenta;
            foreach (string curExt in Sort(externals.ToArray()))
                Console.WriteLine("    " + curExt);

            // Reset console colour and print divider ("-")
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("-");

            // Log results to logs/--------.txt
            Log();
            // Save the map to a local file
            SaveMap();
            // Exit the program peacefully
            Exit();
        }

        // Method to start threads and processing
        static void StartProcess(string url)
        {
            // Starts the timer thread (see the TimerThrInit() function)
            ThreadStart timerThrStrt = new ThreadStart(TimerThrInit);
            Thread timerThr = new Thread(timerThrStrt);
            timerThr.Start();

            // Starts the secondary thread (used for processing, like the main thread)
            ThreadStart SATStart = new ThreadStart(SAThrInit);
            Thread SAThr = new Thread(SATStart);
            SAThr.Start();

            // Start processing on the base URL
            SearchForUrl(url);
        }

        // (MULTI-THREADING) Timer thread initiation method
        static void TimerThrInit()
        {
            // Integer to store how many seconds have passed
            int seconds = 0;
            // Until the processing is done infinitely do this
            while (true)
            {
                // If processComplete is true return out of the thread function
                if (processComplete)
                {
                    // Aborts the current thread
                    Thread.CurrentThread.Abort();
                    // Makes sure that thread isn't running by exiting out of function
                    return;
                }

                // Rewrite the line to show current amount of seconds
                Console.Write("\rCrawling site... (" + seconds + "s)");

                // Pause the thread for 1 second
                Thread.Sleep(1000);
                // Seconds goes up by 1
                seconds++;
            }
        }

        // (MULTI-THREADING) Secondary processing thread initiation method
        static void SAThrInit()
        {
            // Run infinately, until program closes
            while (true)
            {
                // If processComplete is true return out of the thread function
                if (processComplete)
                {
                    // Aborts the current thread
                    Thread.CurrentThread.Abort();
                    // Makes sure that thread isn't running by exiting out of function
                    return;
                }
                // If no knew url for this thread, keep waiting
                if (SATEntryUrl == null)
                {
                    continue;
                }

                // When the main thread provides this thread with a URL, start processing it
                SearchForUrl(SATEntryUrl);
                // After it is done set this variable to null to tell main thread to send another URL
                SATEntryUrl = null;
            }
        }

        // Method for formatting
        static string Format(string url, bool init)
        {
            // If URL is nothing, return nothing
            if (url == ""
                || url == null)
            {
                return "";
            }

            // Convert the URL to lower case
            url = url.ToLower();

            // Remove any protocol tags
            url = url.Replace("https://", "");
            url = url.Replace("http://", "");

            // Remove any protocol tags (with inverted slashes)
            url = url.Replace("https:\\\\", "");
            url = url.Replace("http:\\\\", "");

            // Remove www.
            url = url.Replace("www.", "");

            // If the init parameter is true do this
            if (init)
            {
                // If a '/' is present at the end or start of the string, remove it 
                if (url[url.Length - 1] == '/')
                {
                    url = url.Remove(url.Length - 1);
                }
                if (url[0] == '/')
                {
                    url = url.Remove(0);
                }
                // Return the new, formatted URL
                return url;
            }

            // localResource is automatically false
            bool localResource = false;
            // If the URL contains the base URL and it is positioned at the start, localResource = true
            if (url.Contains(baseUrl) && url.IndexOf(baseUrl) == 0)
            {
                // Remove the base URL and document that this url is a local resource
                url = url.Replace(baseUrl, "");
                localResource = true;
            }

            // If, at this point URL is nothing return nothing
            if (url == "")
            {
                return "";
            }
            // If '/' is the last character, remove it
            if (url[url.Length - 1] == '/')
            {
                url = url.Remove(url.Length - 1, 1);
            }

            // If, at this point URL is nothing return nothing
            if (url == "")
            {
                return "";
            }
            // If '/' is the first character, remove it
            if (url[0] == '/')
            {
                url = url.Remove(0, 1);
            }

            // If the URL is a local resource return the baseUrl followed by the resource, e.g. website.com/styles.css
            if (localResource)
            {
                return baseUrl + '/' + url;
            }
            // Else just return the URL plain
            else
            {
                return url;
            }
        }

        // Method to search a URL for other pages, resources and external links
        static void SearchForUrl(string url)
        {
            // Get the raw HTML
            string html = GetHtml("https://" + url);
            // If there is no HTML to get 
            if (html == null)
            {
                return;
            }

            // Look through the file, documenting new links until no src's of href's are present
            while (true)
            {
                // Set linkTagPos to the position of the first href=
                int linkTagPos = html.IndexOf("href=");
                // Store the tag of the href=, e.g. ' would be documented for href='test', " for href="test"
                char tag = html[linkTagPos + 5];
                // Store the index where the content starts
                int contentStart = linkTagPos + 6;

                // If no href is present, look for src's
                if (linkTagPos < 0)
                {
                    // Set linkTagPos to the position of the first src=
                    linkTagPos = html.IndexOf("src=");
                    // Store the tag of the src=, e.g. ' would be documented for src='test', " for src="test"
                    tag = html[linkTagPos + 4];
                    // Store the index where the content starts
                    contentStart = linkTagPos + 5;

                    // If no href of src is present, file is done, fully break out of loop
                    if (linkTagPos < 0)
                    {
                        break;
                    }
                }

                // If the grabbed content is nothing, href="" or src="", then break
                if (html[contentStart] == tag)
                {
                    break;
                }

                // Find the end of the content
                int contentEnd = -1;
                for (int i = contentStart; i < html.Length - 1; i++)
                {
                    if (html[i] == tag)
                    {
                        // When finding the first instance of the tag after the label (href or src), document its position
                        contentEnd = i;
                        break;
                    }
                }

                // Use positions to get a substring of the page containing the content of the label
                string content = html.Substring(contentStart, contentEnd - contentStart);

                // If the content seems to be JavaScript (JS) then break out of loop
                if (content.Contains("<script>")
                    || content.Contains("</script>")
                    || content.Contains("{")
                    || content.Contains("}"))
                {
                    return;
                }

                // Format the content (see function Format())
                content = Format(content, false);

                // Give the content/URL to the function AddUrl() for further processing and adding
                AddUrl(content);

                // Remove the content just processed
                html = html.Remove(linkTagPos, contentEnd - linkTagPos);
            }
        }

        // Method to process and add found URL's
        static void AddUrl(string url)
        {
            // If url is nothing, return
            if (url == ""
                || url == null)
            {
                return;
            }

            // For each domain extention, check if it is present
            foreach (string tld in TLDS)
            {
                // If the URL contains an extention but not the baseUrl do this
                if (url.Contains(tld) && !url.Contains(baseUrl))
                {
                    // Look through externals list, if it is already present, it is a dupe, log and return
                    foreach (string ext in externals.ToArray())
                    {
                        // If the URL or the formatted URL is already present do this
                        if (Format(url, false) == ext
                            || url == ext)
                        {
                            // Log and return
                            Log(url + " | ABANDONING DUPE");
                            return;
                        }
                    }

                    // Log the addition of the current URL as an external
                    Log(url + " | ADDING AS EXTERNAL");

                    // Add to the externals array
                    externals.Add(Format(url, false));

                    // Exit out of function, url has been processed
                    return;
                }
            }

            // Check if the URL is a page
            bool isPage = false;
            foreach (string ext in EXTS)
            {
                // If the URL contains a valid extension
                if (url.Contains(ext))
                {
                    // Document that the current URL is a page
                    isPage = true;
                    break;
                }
            }

            // Check if the URL is a page without an extention or a resource
            for (int i = url.Length - 1; i >= 0; i--)
            {
                // If it contains an extension (valid ones have already been checked), break, URL is not a page
                if (url[i] == '.' && !isPage)
                {
                    break;
                }
                // If URL has the structure of a redirected page, e.g. website.com/mypage/, its a page,
                else if (url[i] == '/'
                    || url[i] == '\\')
                {
                    // Document that the current URL is a page
                    isPage = true;
                    break;
                }
            }

            // If the current URL is a page do this
            if (isPage)
            {
                // Check for duplicate
                foreach (string curUrl in urls.ToArray())
                {
                    // If the current URL is a duplicate do this
                    if (Format(url.Replace(baseUrl, ""), false) == curUrl
                        || url == curUrl)
                    {
                        // Log and return
                        Log(url + " | ABANDONING DUPE");
                        return;
                    }
                }

                // Otherwise, it is a page and not a duplicate, log and add
                Log(url + " | ADDING AS PAGE");

                // If the base URL is present at the start or the url doesn't contain the base URL
                if (url.IndexOf(baseUrl) == 0
                    || !url.Contains(baseUrl))
                {
                    // Format and add without the base url
                    urls.Add(Format(url.Replace(baseUrl, ""), false));
                }
                // Otherwise
                else
                {
                    // Format and add as is
                    urls.Add(Format(url, false));
                }

                // Find the position of the first '/'
                int firstSlashPos = -1;
                for (int i = url.Length - 1; i >= 0; i--)
                {
                    // When found do this
                    if (url[i] == '/')
                    {
                        // Store the position and move on
                        firstSlashPos = i;
                        break;
                    }
                }

                // If firstSlashPos is found then strippedUrl becomes the url without the last segment
                string strippedUrl = null;
                // Otherwise strippedUrl stays null
                if (firstSlashPos != -1)
                {
                    strippedUrl = url.Substring(0, url.Length - firstSlashPos);
                }

                // If the page is not a page location or strippedUrl is not null
                if (!url.Contains("#")
                    || strippedUrl != null)
                {
                    // If strippedUrl has a value
                    if (strippedUrl != null)
                    {
                        // Check for duplicate
                        foreach (string page in urls.ToArray())
                        {
                            // If the formatted or plain version of strippedUrl is already stored as a page, return
                            if (Format(strippedUrl, false) == page
                                || strippedUrl == page)
                            {
                                return;
                            }
                        }
                    }

                    // If the current URL contains the base URL at the start do this
                    if (url.Contains(baseUrl) && url.IndexOf(baseUrl) == 0)
                    {
                        // If secondary processing thread is resting, set to work on new URL
                        if (SATEntryUrl == null)
                        {
                            // Give 2nd thread new URL
                            SATEntryUrl = Format(url, false);
                        }
                        // Otherwise
                        else
                        {
                            // Process it on this thread
                            SearchForUrl(Format(url, false));
                        }
                    }
                    // If URL doesn't contain the base URL at the start
                    // do the same as above, only with different formatting
                    else
                    {
                        // If secondary processing thread is resting, set to work on new URL
                        if (SATEntryUrl == null)
                        {
                            // Give 2nd thread new URL
                            SATEntryUrl = Format(baseUrl + url, false);
                        }
                        else
                        {
                            // Process it on this thread
                            SearchForUrl(Format(baseUrl + url, false));
                        }
                    }
                }
            }
            // If current URL is a resource do this
            else
            {
                // Check for duplicate
                foreach (string curResrc in resources.ToArray())
                {
                    // If formatted URL or plain URL is already documented, ignore it
                    if (Format(url.Replace(baseUrl, ""), false) == curResrc
                        || url == curResrc)
                    {
                        // Log and return
                        Log(url + " | ABANDONING DUPE");
                        return;
                    }
                }

                // If the URL doesn't contain '.' or ':' return
                if (!url.Contains(".") && !url.Contains(":"))
                {
                    return;
                }

                // Log the adding of the current URL as a resource
                Log(url + " | ADDING AS RESOURCE");

                // If the base URL is present at the start or the url doesn't contain the base URL
                if (url.IndexOf(baseUrl) == 0
                    || !url.Contains(baseUrl))
                {
                    // Format and add without the base url
                    resources.Add(Format(url.Replace(baseUrl, ""), false));
                }
                else
                {
                    // Format and add as is
                    resources.Add(Format(url, false));
                }
            }
        }

        // (POLYMORPHISM) Method to write Log()'ed messages to the log file
        static void Log()
        {
            // Notify the user of logging status
            Console.WriteLine("Finished, Logging...");
            // Log the logs
            File.WriteAllText(path, toLog);
            // Notify the user of logging status
            Console.WriteLine("Complete!");
        }

        // (POLYMORPHISM) Method to save messages to write to log file
        static void Log(string msg)
        {
            // Add msg variable to the toLog variable
            toLog += msg + "\n\n";
            return;
        }

        // Method to save the map into a local file
        static void SaveMap()
        {
            // Declare string variable to add to
            string textToSave = "";

            // Add the title
            textToSave += "----------\n";
            textToSave += baseUrl + "\n";
            textToSave += "----------\n\n";

            // Add the pages 
            textToSave += "Pages / Page Locations\n";
            foreach (string curUrl in Sort(urls.ToArray()))
                textToSave += "    " + curUrl + "\n";

            // Add divider
            textToSave += "-\n";

            // Add the resources 
            textToSave += "Resources\n";
            foreach (string curResource in Sort(resources.ToArray()))
                textToSave += "    " + curResource + "\n";

            // Add divider
            textToSave += "-\n";

            // Add the external links 
            textToSave += "Externals\n";
            foreach (string curExt in Sort(externals.ToArray()))
                textToSave += "    " + curExt + "\n";

            // Notify the user where to look for the log in the map file
            textToSave += "\nLook in '" + path + "' for logs.";

            // Write to the map file
            File.WriteAllText("map_" + path.Replace("logs/", ""), textToSave);
        }

        // Method to get the html of a given URL
        static string GetHtml(string url)
        {
            try
            {
                // Try returning the URL as is
                return new WebClient().DownloadString(url);
            }
            catch
            {
                try
                {
                    // Try returning the URL as http not https
                    return new WebClient().DownloadString(url.Replace("https", "http"));
                }
                catch
                {
                    try
                    {
                        // Try returning the URL as http://www., not https
                        return new WebClient().DownloadString(url.Replace("https://", "http://www."));
                    }
                    catch
                    {
                        // If page is unreachable, log and return null
                        Log(url + " | ABANDONING INVALID LINK");
                        return null;
                    }
                }
            }
        }

        // Method to sort array into alphabetical order
        static string[] Sort(string[] toSort)
        {
            // Sort array
            for (int i = 0; i < toSort.Length - 1; i++)
            {
                // Variable to store whether an iteration ran without changing the array
                bool cleanRun = true;

                // Go through the array
                for (int t = 0; t < toSort.Length - 1; t++)
                {
                    // If the second result is before the first result alphabetically do this
                    if (string.Compare(toSort[t], toSort[t + 1]) > 0)
                    {
                        // Switch toSort[t] with toSort[t + 1]
                        string temp = toSort[t];
                        toSort[t] = toSort[t + 1];
                        toSort[t + 1] = temp;

                        // If a swap has been made, not a clean run, cleanRun = false
                        cleanRun = false;
                    }
                }

                // If a clean run has been made, no more iterations needed, exit
                if (cleanRun)
                {
                    break;
                }
            }

            // Return new array
            return toSort;
        }

        // Method to give the log path of a given URL
        static string GetFileName(string url)
        {
            // Remove all '.''s
            while (url.IndexOf('.') >= 0)
            {
                url = url.Remove(url.IndexOf('.'), 1);
            }

            // Return the path
            return "logs/" + url + ".txt";
        }

        // Method to download the site and resources
        static void Download()
        {

        }

        // Method to exit the program peacefully
        static void Exit()
        {
            // Prompt user to enter any key to end the program
            Console.WriteLine("\nPress any key to end program...");
            // Read next key input
            Console.ReadKey();
            // Exit the program (OBSOLETE: Was used to force close threads, not needed as threads close automatically)
            Environment.Exit(1);
        }
    }
}
