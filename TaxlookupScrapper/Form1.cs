using ScrapeEngine.Root;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaxlookupScrapper.Engine;

namespace TaxlookupScrapper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            PrepareYearsSelectList();
            PrepareCounties();
            InitializeComponent();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Please enter Property Name search text!");
                return;
            }
            var searchWord = textBox1.Text;
            var jurisdication = comboBoxCounty.SelectedItem.ToString();
            var year = comboBox1.SelectedItem.ToString();
            this.buttonStart.Enabled = false;
            RunOnBackgroundWorker(new Scrapper(searchWord, jurisdication, year, UpdateLabel));
        }

        private IScrape scrape { get; set; }

        private void RunOnBackgroundWorker(IScrape scrape)
        {
            //  this.Invoke(new Action<IScrape>(UpdateLabel), scrape);
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerAsync(scrape);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            scrape = (IScrape)e.Argument;
            StartScrapping(scrape);
        }

        private void StartScrapping(IScrape scrape)
        {
            scrape.GetPages();
        }

        private void UpdateLabel(string message)
        {
            if (message.Equals("finished"))
            {
                MethodInvoker invBtn = delegate
                {
                    this.buttonStart.Enabled = true;
                };

                this.Invoke(invBtn);
                EndPhantomJs();
            }
            MethodInvoker inv = delegate
            {
                if (!message.Equals("finished"))
                    this.labelMessage.Text = message;
                else
                    this.labelMessage.Text += " " + message;

            };

            this.Invoke(inv);
        }

        public void EndPhantomJs()
        {
            try
            {
                foreach (Process proc in Process.GetProcessesByName("phantomjs"))
                {
                    proc.Kill();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private List<string> _yearsSelectList { get; set; } = new List<string>();
        private List<string> _Counties { get; set; } = new List<string>();
        private void PrepareYearsSelectList()
        {
            var currentYear = DateTime.Now.Year;

            for (int i = currentYear; i > 2004; i--)
            {
                _yearsSelectList.Add(i.ToString());
            }
        }

        private void PrepareCounties()
        {
            try
            {
                _Counties = System.IO.File.ReadAllLines(@"counties.txt").ToList();
            }
            catch
            {
                throw new Exception("Counties file not found.");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = _yearsSelectList;
            comboBox1.SelectedIndex = 1;
            comboBoxCounty.DataSource = _Counties;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(@"chrome.exe", "http:\\scrapman.io");
            }
            catch
            {
                Process.Start(@"iexplore.exe", "http:\\scrapman.io");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            EndPhantomJs();
        }
    }
}
