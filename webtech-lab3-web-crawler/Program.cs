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
        static string root; //the root url
        static Stack<String> stack = new Stack<string>(); //the links to vist
        static List<String> visted = new List<string>(); //the links that have been visited
        static List<String> disallowed = new List<string>(); //the links disallowed by robots.txt

        static void Main(string[] args)
        {
            seed = "http://www.dcs.bbk.ac.uk/~martin/sewn/ls3";

            stack.Push(seed);

            while(stack.Count > 0)
            {
                CrawlLinks();
            };
            
            Console.ReadLine();
        }

        static void CrawlLinks()
        {
            //takes the next page from the stack and extracts all of the links, adds non visted & allowed pages to the stack
         
            string page = stack.Pop(); //get the next page to view

            //page may have been viewed already (duplicate links on the same page), if not crawl if allowed
            if(!LinkVisted(page) && CanVisit(page))
            {
                Console.WriteLine(page);
                String html = new WebClient().DownloadString(page); //download the HTML
                MatchCollection regexLinks = Regex.Matches(html, @"(<a.*?>.*?</a>)", RegexOptions.Singleline); //get the ahrefs

                //put all the unvisited links onto the stack
                for(int i = 0; i < regexLinks.Count; i++)
                {
                    String link = ReturnPageLink(regexLinks[i].Value);
                    if (link != null) { stack.Push(link); }
                }  

            }//if the link hasn't already been visited

        }//CrawlLinks()

        static String ReturnPageLink(string aLink)
        {
            //returns the HTML page from an a link, e.g. <a href="martin.htm">Start crawling here!</a>
           
            string[] split = aLink.Split('"');

            if(split.Length > 2 && split[0] == "<a href=")
            {
                //always return a fully qualified link
                return split[1].Contains(seed) ? split[1] : seed + @"/" + split[1];
            }
            else
            {
                return null;
            }

        }//ReturnPage

        static Boolean LinkVisted(string lnk)
        {
            return (from link in visted
                          where link == lnk
                          select link).Any();
        }

        static Boolean CanVisit(string link)
        {
            //takes a link and determines whether we can visit it, based on the same domain rule
            //and by obeying the robots.txt        


            //check whether this link is from the correct domain
            if(link.Contains(seed))
            {
                //get a list of all the disallowed links
                List<String> robots = Regex.Split(new WebClient().DownloadString(link + @"/robots.txt"), "\r\n").ToList();

                var disallowed = (from line in robots
                                  where line.Contains("Disallow")
                                  select link + @"/" + line.Replace(@"Disallow:","").Replace(" ","").Substring(1)).ToList();

                //check whether this passed in link is in the disallowed list
                var exists = (from lnk in disallowed
                              where link.Contains(lnk)
                              select lnk).Any();

                Console.WriteLine(exists + " - " + link);
                return !exists; 
            }
            else
            {
                Console.WriteLine("false - " + link);
                return false; //a different domain
            }

        }
    }
}
