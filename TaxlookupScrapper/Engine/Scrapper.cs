using ScrapeEngine.Root;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ScrapeEngine;
using OpenQA.Selenium;
using System.Text.RegularExpressions;

namespace TaxlookupScrapper.Engine
{
    class Scrapper : BaseSelenium, IScrape
    {
        private string _propertyName { get; set; }
        private string _jurisdication { get; set; }
        private string _year { get; set; }
        private Action<string> _action { get; set; }

        public Scrapper(string propertyName, string jurisdication = "vestal", string year = "2016", Action<string> action = null)
        {
            _propertyName = propertyName;
            _jurisdication = jurisdication;
            _year = year;
            _action = action;
        }

        Item item;
        int skip = 0;

        public void GetDetails(HtmlNode node)
        {
            //skip++;
            //if (skip < 16) return;
            var tds = node.SelectNodes(".//td");
            if (tds == null)
                throw new Exception("Parsing error, please contact the author at vstanbol@gmail.com");

            var ownerName = tds.FirstOrDefault();
            var link = Helper.GetUrlFromNode(ownerName.SelectSingleNode(".//a"));

            var propertyAddress = tds.ElementAt(1)?.InnerText;
            if (string.IsNullOrEmpty(link)) return;

            item = new Item()
            {
                Owner = ownerName?.InnerText,
                PropertyAddress = propertyAddress
            };

            var url = $"http://www.taxlookup.net/{link}".Replace("&amp;", "&");
            var html = Browser.HttpWebRequestGet(url);
            try
            {
                GetLandDetails(html);
            }
            catch(Exception ex)
            {
                _action($"error occured{ex.ToString()} {counter.ToString()}");
            }
            _action($"Completed {counter.ToString()}");

            try
            {
                item.Owner = item.Owner.Replace("&amp;", "&");
                SaveLineCommaDelimitedAs(item, filename);

            }
            catch
            {
                _action("finished");
                _action("error during saving document!");
            }
        }

        Regex ownerReg = new Regex("<b>Owner.*", RegexOptions.Singleline);
        private void GetLandDetails(string html)
        {
            var doc = Helper.GetDocument(html);
            var boxOwner = Helper.GetSingleNode(doc, "//div[@id='propertyinfobox1']");
            var box1 = Helper.GetSingleNode(doc, "//div[@id='propertydetailsbox1']");
            var box2 = Helper.GetSingleNode(doc, "//div[@id='ctl00_bodyplaceholder_levydetailspanel']");
            var box3 = Helper.GetSingleNode(doc, "//div[@id='propertydetailsbox4']");
            var bolds = Helper.GetCollectionSubNode(box1, ".//b");
            var totalTax = Helper.GetCollectionSubNode(box2, ".//b")?.FirstOrDefault(x => x.InnerText.Contains("Total Tax:"))?.InnerText;

            //var ownerInfos = boxOwner.SelectSingleNode(".//b[text()='Owner:']").SelectNodes(".//br");

            var LandAssessment = box1.SelectSingleNode(".//b[text()='Land Assessment:']")?.SelectSingleNode(".//following-sibling::text()");
            var TotalAssessment = box1.SelectSingleNode(".//b[text()='Total Assessment:']")?.SelectSingleNode(".//following-sibling::text()");
            var StarSavings = box1.SelectSingleNode(".//b/span[text()='Star Savings:']")?.ParentNode.SelectSingleNode(".//following-sibling::text()");

            var Exemptions = box3?.SelectSingleNode(".//b[text()='Exemptions:']")?.ParentNode?.SelectNodes(".//div/div");
            //.SelectSingleNode(".//following-sibling::text()");

            string exemptions = "";
            if(Exemptions!=null)
            {
                exemptions = string.Join(" ", Exemptions.Select(x => x.InnerText));
            }

            string ownerHtml = boxOwner?.InnerHtml != null ? ownerReg.Match(boxOwner.InnerHtml).Value : "";
            string ownerinfo = "";// Helper.RemoveHTMLTags(ownerHtml).Replace("Owner:", string.Empty);
            if (!string.IsNullOrEmpty(ownerHtml))
            {
                ownerinfo = ownerHtml?.Replace("<b>Owner:</b><br>", string.Empty);
                ownerinfo = Regex.Replace(ownerinfo, "<.[^>]*>", " | ");
            }


            item.TotalTax = totalTax.Replace("Total Tax:", string.Empty);
            item.OwnerInfo = ownerinfo;
            item.LandAssessment = LandAssessment != null ? LandAssessment.InnerText : "";
            item.TotalAssessment = TotalAssessment != null ? TotalAssessment.InnerText : "";
            item.StarSavings = StarSavings != null ? StarSavings.InnerText : "";
            item.Exemptions = exemptions;
        }

        class Item
        {
            public string PropertyAddress { get; set; } //from GetDetails
            public string Owner { get; set; }          //from GetDetails
            public string OwnerInfo { get; set; }          //from GetDetails
            public string LandAssessment { get; set; }
            public string TotalAssessment { get; set; }
            public string StarSavings { get; set; }
            public string Exemptions { get; set; }
            public string TotalTax { get; set; }
        }

        public void GetLinks(string html)
        {
            var doc = Helper.GetDocument(html);
            var main = Helper.GetSingleNode(doc, "//table[@id='ctl00_bodyplaceholder_resultsgrid']");
            var nodes = Helper.GetCollectionSubNode(main, ".//tr[contains(@class,'recordsrow')]");
            foreach (var node in nodes)
            {
                GetDetails(node);
            }
        }

        public void GetPages()
        {
            filename = $"{_jurisdication}_{_propertyName}_{DateTime.Now.ToShortDateString().Replace("/", "-")}.csv";
            GenerateHeaders();
            var url =
                $"http://www.taxlookup.net/search.aspx?jurisdiction={_jurisdication}&type=lastname&lastname={_propertyName}&year={_year}";

            _driver.Navigate().GoToUrl(url);
            _driver.FindElement(By.Id("ctl00_bodyplaceholder_lastname")).SendKeys(_propertyName);
            _driver.FindElement(By.Name("ctl00$bodyplaceholder$lastnamesubmit")).Click();
            System.Threading.Thread.Sleep(1500);
            var html = _driver.PageSource;
            //var html = Browser.HttpWebRequestGet(url);
            GetLinks(html);
            _action("finished");
        }

        // run only once
        private void GenerateHeaders()
        {
            var header = new Item
            {
                Owner = "Owner",
                Exemptions = "Exemptions",
                LandAssessment = "Land Assessment",
                OwnerInfo = "Owner Info",
                PropertyAddress = "Property Address",
                StarSavings = "Star Savings",
                TotalAssessment = "Total Assessment",
                TotalTax = "Total Tax",
            };
            SaveLineCommaDelimitedAs(header, filename);
        }
    }
}
