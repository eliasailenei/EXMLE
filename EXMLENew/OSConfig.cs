using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using Npgsql;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace EXMLENew
{
    public partial class OSConfig : Form
    {
        public string serverConfig { get; set; }
        public string username { get; set; }
        DataTable Windows;
        bool notAdded = true;
        bool firstTime;
        string rel, lang;
        int ver;
        public OSConfig()
        {
            InitializeComponent();
        }

        private void OSConfig_Load(object sender, EventArgs e)
        {
            getLatest();
            checkIfEmpty();    
            Windows = new DataTable();
            Windows.Columns.Add("Version");
            Windows.Columns.Add("Release");

            int[] versions = { 10, 11 };

            foreach (int item in versions)
            {
                populateRelease(item);
            }
        }
        private object sqlCC(string query)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(serverConfig))
            {
                try
                {
                    connection.Open();
                    using (NpgsqlCommand selectCommand = new NpgsqlCommand(query, connection))
                    {
                        return selectCommand.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
        }
        private void checkIfEmpty()
        {
            firstTime = true;
            bool notOSEmpty = false;
            try
            {
                object result = sqlCC("select p.osversion from preference p join users u on p.id = u.id where u.username = '" + username + "'"); // cross table sql

                if (result != null)
                {
                    string input = result.ToString();

                    if (!string.IsNullOrEmpty(input))
                    {
                        try
                        {
                            firstTime = false;
                            notOSEmpty = true;
                            ver = int.Parse(input);
                            textBox1.Text = ver.ToString();
                        }
                        catch (FormatException)
                        {
                            firstTime = false;
                            notOSEmpty = true;
                            ver = 0;
                            textBox1.Text = ver.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                object result = sqlCC("select p.osrelease from preference p join users u on p.id = u.id where u.username = '" + username + "'"); // cross table sql

                if (result != null)
                {
                    string inputs = result.ToString();

                    if (!string.IsNullOrEmpty(inputs))
                    {
                        firstTime = false;
                        notOSEmpty = true;
                        rel = inputs;
                        textBox2.Text = rel;
                    }
                }
            }
            catch { }
            try
            {
                object result = sqlCC("select p.oslanguage from preference p join users u on p.id = u.id where u.username = '" + username + "'"); // cross table sql

                if (result != null)
                {
                    string inputsss = result.ToString();

                    if (!string.IsNullOrEmpty(inputsss))
                    {
                        firstTime = false;
                        notOSEmpty = true;
                        lang = inputsss;
                        textBox3.Text = lang;
                    }
                }
            }
            catch { }
            try
            {
                object input = sqlCC("select p.niniteoptions from preference p join users u on p.id = u.id where u.username = '" + username + "'"); // cross table sql
                if (input != null)
                {
                    firstTime = false;
                }
                else if (input == null)
                {
                    if (!notOSEmpty)
                    {
                        firstTime = true;
                        Console.WriteLine("This is users first time");
                    }
                    
                }
            } catch { }
        }





        private void getLatest()
        {
            if (Directory.Exists("MSWISO"))
            {
                Directory.Delete("MSWISO", true);
            }
            Directory.CreateDirectory("MSWISO");
            using (var client = new WebClient())
            {
                client.DownloadFile("https://github.com/eliasailenei/MSWISO/releases/download/Release/Stable.zip", "MSWISO.zip");
            }
            ZipFile.ExtractToDirectory("MSWISO.zip", "MSWISO"); // writing and reading a file
            File.Delete("MSWISO.zip");
        }
        public void populateRelease(int ver)
        {
            Process infoGain = new Process();
            infoGain.StartInfo.FileName = "MSWISO\\MSWISO.exe";
            infoGain.StartInfo.Arguments = "--ESDMode=True --WinVer=Windows_" + ver + " --Release=";
            infoGain.StartInfo.UseShellExecute = false;
            infoGain.StartInfo.CreateNoWindow = true;
            infoGain.Start();
            infoGain.WaitForExit();
            getInput(ver);
        }
        public void getInput(int ver)
        {
            string[] allEntries = File.ReadAllLines("output.txt"); // writing and reading output
            foreach (string release in allEntries)
            {
                DataRow releaseRow = Windows.NewRow();
                releaseRow["Version"] = ver;
                releaseRow["Release"] = release;
                Windows.Rows.Add(releaseRow);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                rel = listBox1.SelectedItem.ToString();
                textBox2.Text = rel;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " due to empty space being clicked. Error is ignored as it is not critical.");
            }
            if (notAdded)
            {
                notAdded = false;
                string[] languageArray = { "Arabic", "Bulgarian", "Czech", "Danish", "German", "Greek", "English", "Spanish", "Estonian", "Finnish", "French", "Hebrew", "Croatian", "Hungarian", "Italian", "Japanese", "Korean", "Lithuanian", "Latvian", "Dutch", "Norwegian", "Polish", "Romanian", "Russian", "Slovak", "Slovenian", "Serbian", "Swedish", "Thai", "Turkish", "Ukrainian" };
                listBox2.Items.Clear();
                listBox2.Items.AddRange(languageArray);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ver = 10;
            textBox1.Text = ver.ToString();
            listBox1.Items.Clear();
            foreach (DataRow row in Windows.Rows)
            {
                if (row["Version"].ToString() == "10")
                {
                    listBox1.Items.Add(row["Release"].ToString());
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ver = 11;
            textBox1.Text = ver.ToString();
            listBox1.Items.Clear();
            foreach (DataRow row in Windows.Rows)
            {
                if (row["Version"].ToString() == "11")
                {
                    listBox1.Items.Add(row["Release"].ToString());
                }
            }
        }
        static string argsFormat(string input)
        {
            return input.Replace(' ', '_');
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string command = "";
            // insertion is done wrong, rewrite is required here
            if (firstTime)
            {
                command = $"insert into preference (id, osversion, osrelease, oslanguage) select u.id, '{ver}', '{rel}', '{lang}' from users u where u.username = '{username}'";
                firstTime = false ;
            }
            else if (!firstTime)
            {
                command = $"update preference set osversion = '{ver}', osrelease = '{rel}', oslanguage = '{lang}' where id = (select id from users where username = '{username}')";
            }
            sqlCC(command);
            MessageBox.Show("Update done!");
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            lang = listBox2.SelectedItem.ToString();
            textBox3.Text = lang;
        }
    }
}
