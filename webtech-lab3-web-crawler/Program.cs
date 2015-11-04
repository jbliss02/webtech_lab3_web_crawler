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
        static List<Link> visted = new List<Link>(); //the links that have been visited
        static List<String> disallowed = new List<string>(); //items disallowed by robots.txt

        static void Main(string[] args)
        {
            seed = "http://www.dcs.bbk.ac.uk/~martin/sewn/ls3";
            stack.Push(new Link(seed, seed));

            while(stack.Count > 0)
            {
                CrawlLinks();
            };

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
                visted.Add(page);
                String html = new WebClient().DownloadString(page.linkString); //download the HTML
                MatchCollection regexLinks = Regex.Matches(html, @"((<a.*?>.*?</a>)|(<A.*?>.*?</A))", RegexOptions.Singleline); //get the ahrefs

                //put all the unvisited links onto the stack
                for(int i = 0; i < regexLinks.Count; i++)
                {
                    Link link = new Link(ReturnPageLink(regexLinks[i].Value), page.path);
                    if (link.linkString != null) { stack.Push(link); }
                }  

            }//if the link hasn't already been visited

        }//CrawlLinks()

        static String ReturnPageLink(string aLink)
        {
            //returns the HTML page from an a link, e.g. <a href="martin.htm">Start crawling here!</a>
           
            string[] split = aLink.ToLower().Split('"');

            if(split.Length > 2 && split[0].Contains("<a") && split[0].Contains("href"))
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
            return (from lnk in visted
                          where link.linkString == lnk.linkString
                          select lnk).Any();
        }

        static Boolean CanVisit(Link link)
        {
            //takes a link and determines whether we can visit it, based on the same domain rule and by obeying robots.txt        

            //check whether this link is from the correct domain
            if(link.linkString.Contains(seed))
            {
                List<String> robots = new List<string>();

                //get a list of all the disallowed links
                try
                {
                    robots = Regex.Split(new WebClient().DownloadString(link.robots), "\r\n").ToList();
                }
                catch(System.Net.WebException ex) //may not be found
                {
                    return false; //if no robots then assume we cannot visit
                }

                var disallowed = (from line in robots
                                  where line.Contains("Disallow")
                                  select link.path + line.Replace(@"Disallow:","").Replace(" ","").Substring(1)).ToList();

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
                return false; //a different domain
            }

        }

    }
}
