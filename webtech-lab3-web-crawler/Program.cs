using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;

namespace webtech_lab3_web_crawler
{
    public class Program
    {
        static string seed; //the initial url
        static Stack<Link> stack = new Stack<Link>(); //the links to vist
        static List<Link> visited = new List<Link>(); //the links that have been visited
        static List<String> disallowed = new List<string>(); //items disallowed by robots.txt
        static List<String> robotsDownloaded = new List<string>(); //which robots.txt files have been checked
        const string CRAWLERFILE = "crawl.txt";

        static void Main(string[] args)
        {
            seed = "http://www.dcs.bbk.ac.uk/~martin/sewn/ls3";
            stack.Push(new Link(seed, seed, seed));

            while(stack.Count > 0)
            {
                CrawlLinks();
            };

            WriteCrawlers();
            Console.WriteLine("Finished");
            Console.ReadLine();
        }

        static void CrawlLinks()
        {
            //takes the next page from the stack and extracts all of the links, adds non visted & allowed pages to the stack       
            Link page = stack.Pop(); //get the next page to view

            //page may have been viewed already (duplicate links on the same page), if not crawl if allowed
            if(!LinkVisted(page) && CanVisit(page))
            {
                Console.WriteLine(page.linkString);
                visited.Add(page);
                try
                {
                    String html = new WebClient().DownloadString(page.linkString); //download the HTML
                    MatchCollection regexLinks = Regex.Matches(html, @"((<a.*?>.*?</a>)|(<A.*?>.*?</A))", RegexOptions.Singleline); //get the ahrefs

                    //put all the unvisited links onto the stack
                    for(int i = 0; i < regexLinks.Count; i++)
                    {
                        Link link = new Link(ReturnPageLink(regexLinks[i].Value), page.path, page.linkString);
                        if (link.linkString != null) { stack.Push(link); }
                    } 
                }
                catch(WebException ex)
                {
                    //maybe not found or server error
                }
 
            }//if the link hasn't already been visited

        }//CrawlLinks()

        static String ReturnPageLink(string aLink)
        {
            //returns the HTML page from an a link, e.g. <a href="martin.htm">Start crawling here!</a>

            string[] split = aLink.ToLower().Replace("'", "\"").Split('"');

            if (split.Length > 2 && split[0].Contains("<a") && split[0].Contains("href") && !split[0].Contains("mailto:"))
            {
                //always return a fully qualified link
                if (split[1].ToLower().Contains("http:"))
                {
                    return split[1];
                }
                else if (split[1].ToLower().Contains("mailto:"))
                {
                    return null;
                }
                else
                {
                    if (split[1].Contains(seed))
                    {
                        return split[1];
                    }
                    else
                    {
                        split[1] = split[1].Replace(@"../../", ""); //remove the relative path notation
                        if (split[1].Substring(0, 1) == ".") { split[1] = split[1].Substring(1); } //remove leading dots, these denote a relative url
                        if (split[1].Substring(0, 1) == @"/") { split[1] = split[1].Substring(1); } //remove leading dash, these denote a relative url
                        return seed + @"/" + split[1];
                    }
                    //return split[1].Contains(seed) ? split[1] : seed + @"/" + split[1];
                }
            }
            else
            {
                return null;
            }

        }//ReturnPage

        static Boolean LinkVisted(Link link)
        {
            return (from lnk in visited
                          where link.linkString == lnk.linkString
                          select lnk).Any();
        }

        static Boolean CanVisit(Link link)
        {
            //takes a link and determines whether we can visit it, based on the same domain rule and by obeying robots.txt        
          
            if(link.linkString.Contains(seed) && !isDisallowed(link)) //check whether this link is from the correct domain
            {
                List<String> robots = new List<string>();

                //get a list of all the disallowed links from robots.txt, if it hasn't been visited
                if (!RobotsChecked(link.robots))
                {
                    try
                    {
                        robots = Regex.Split(new WebClient().DownloadString(link.robots), "\r\n").ToList();
                        robotsDownloaded.Add(link.robots);
                    }
                    catch(System.Net.WebException ex) //may not be found
                    {
                        return true; //if no robots.txt then assume we can visit
                    }

                    disallowed.AddRange((from line in robots
                                      where line.Contains("Disallow")
                                      select link.path + @"/" + line.Replace(@"Disallow:","").Replace(" ","").Substring(1)).ToList());
                }

                //check whether this passed in link is in the disallowed list
                var exists = (from lnk in disallowed
                              where link.linkString.Contains(lnk)
                              select lnk).Any();

                //Console.WriteLine(!exists + " - " + link.linkString);
                return !exists; 
            }
            else
            {
                //Console.WriteLine("false - " + link.linkString);
                return false; //a different domain or is disallowed
            }

        }//CanVisit

        static Boolean RobotsChecked(string robotFile)
        {
            return (from file in robotsDownloaded
                    where file == robotFile
                    select file).Any();
        }

        static Boolean isDisallowed(Link link)
        {
            return (from lnk in disallowed
                    where lnk == link.path
                    select lnk).Any();
        }

        static void WriteCrawlers()
        {
            //writes to the crawlers.txt file. Extracts all the parents and then prints the children as nodes

            List<String> parents = new List<string>(); //the parent URLs that have been written to the text file

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(CRAWLERFILE))
            {
                for (int i = 0; i < visited.Count; i++)
                {
                    if (!parents.Exists(el => el == visited[i].parent))
                    {
                        file.WriteLine(visited[i].parent);

                        var children = (from link in visited
                                        where link.parent == visited[i].parent
                                        select link.linkString).ToList();

                        for(int n = 0; n < children.Count; n++)
                        {
                            file.WriteLine("--" + children[n]);
                        }

                        parents.Add(visited[i].parent); //add to the list so we don't print again

                    }//if this parents hasn't been written yet

                }//for each url visited

            }//write to text file

        }//WriteCrawlers
    }
}
