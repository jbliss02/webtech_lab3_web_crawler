using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webtech_lab3_web_crawler
{
    public class Links
    {
    }

    public class Link
    {
        public Link(string linkString, string startingUrl) { this.linkString = linkString; this.originatingPath = startingUrl; }
        public string linkString { get; set; }

        public string originatingPath { get; set; }

        public string path
        {
            get
            {

                List<String> split = linkString.Split('/').ToList();

                if (split[split.Count - 1].Contains("."))
                {
                    string newLink = "";
                    for (int i = 0; i < split.Count - 1; i++)
                    {
                        newLink += split[i] + "/";
                    }
                    return newLink;

                }
                else
                {
                    return linkString;
                }
            }
        }

        public string robots
        {
            get
            {
                return path.Substring(path.Length - 1) == @"/" ? path + @"robots.txt" : path + @"/robots.txt";
            }
        }

    }

}
