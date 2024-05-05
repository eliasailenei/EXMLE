using Microsoft.VisualBasic.ApplicationServices;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace EXMLENew
{
    public partial class XML : Form
    {
        public string serverCreds { get; set; }
        public string loginUsername { get; set; }
        public string loginHashedPassword { get; set; }
        public string key { get; set; }
        public string keyPass { get; set; }
        bool isOnline;
        string username,applicationLine, currentRelease, currentLanguage, osUsername, osPassword, domainLine, diskNumber, currentVersion, accPass, encKey;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.CheckedChanged -= checkBox1_CheckedChanged; 

            if (!isOnline)
            {
                MessageBox.Show("You just enabled Online Mode! This means that PortableISO will fetch the most latest data when applying the image. Click again to disable.");
                isOnline = true;
            }
            else
            {
                MessageBox.Show("Program will now only read from XML!");
                isOnline = false;
            }

            checkBox1.Checked = isOnline;

            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string location = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select the folder where you want to save the XML file. Note: It's best to create the XML when you have mounted the image.";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                location = Path.Combine(dialog.SelectedPath);
            }
            else
            {
                MessageBox.Show("We will save in " + location);
            }

            if (File.Exists(Path.Combine(location, "config.xml")))
            {
                MessageBox.Show("A previous XML file has been detected! You need to delete it to continue, please back it up and then Press OK to delete it.");
                File.Delete(Path.Combine(location, "config.xml"));
            }

            using (XmlWriter writer = XmlWriter.Create(Path.Combine(location, "config.xml"), new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true })) // writing and reading a file
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("EXMLE");
                writer.WriteStartElement("setupData");
                writer.WriteElementString("IsOnline", isOnline.ToString().ToLower() ?? error());
                writer.WriteElementString("Username", username ?? error());
                writer.WriteElementString("Password", accPass ?? error());
                writer.WriteElementString("LoginKey", key ?? error());
                writer.WriteElementString("KeyPass", encKey ?? error());
                writer.WriteEndElement();
                if (!isOnline)
                {
                    writer.WriteStartElement("niniteOptions");
                    writer.WriteElementString("ApplicationLine", applicationLine ?? error());
                    writer.WriteEndElement();
                    writer.WriteStartElement("OSDownload");
                    writer.WriteElementString("CurrentVersion", currentVersion ?? error());
                    writer.WriteElementString("CurrentRelease", currentRelease ?? error());
                    writer.WriteElementString("CurrentLanguage", currentLanguage ?? error());
                    writer.WriteEndElement();
                    writer.WriteStartElement("OSConfig");
                    writer.WriteElementString("OSUsername", osUsername ?? error());
                    writer.WriteElementString("OSPassword", osPassword ?? error());
                    writer.WriteElementString("DiskNumber", diskNumber ?? error());
                    writer.WriteElementString("IsUsingDomain", isUsingDomain.ToString().ToLower() ?? error());
                    writer.WriteElementString("DomainLine", domainLine ?? error());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();

                MessageBox.Show("File made!");
            }


        }


        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show(
                    "Key: " + (key ?? error()) + Environment.NewLine +
                    "Key Pass: " + (encKey ?? error()) + Environment.NewLine +
                    "isOnline: " + (isOnline.ToString() ?? error()) + Environment.NewLine +
                    "Username: " + (username ?? error()) + Environment.NewLine +
                    "Application Line: " + (applicationLine ?? error()) + Environment.NewLine +
                    "Current Release: " + (currentRelease?.ToString() ?? error()) + Environment.NewLine +
                    "Current Language: " + (currentLanguage?.ToString() ?? error()) + Environment.NewLine +
                    "OS Username: " + (osUsername ?? error()) + Environment.NewLine +
                    "OS Password: " + (osPassword ?? error()) + Environment.NewLine +
                    "Domain Line: " + (domainLine ?? error()) + Environment.NewLine +
                    "Disk Number: " + (diskNumber?.ToString() ?? error()) + Environment.NewLine +
                    "Current Version: " + (currentVersion ?? error()) + Environment.NewLine +
                    "Account Password: " + (accPass ?? error())
                );
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException)
                {
                    MessageBox.Show("Null!");
                }
                else
                {
                    MessageBox.Show("Unknown error occurred: " + ex.Message);
                }
            }
        }
        private string error()
        {
            MessageBox.Show("One or more things were null or empty. Please make sure you populate all of your data before exporting it!");
            this.Close();
            Environment.Exit(0);
            return "error";
        }
        bool isUsingDomain;
        public XML()
        {
            InitializeComponent();
        }
        
        private void XML_Load(object sender, EventArgs e)
        {
            username = loginUsername;
            collectNinite();
            collectOSSel();
            collectOSSetup();
            accPass = BCrypt.Net.BCrypt.HashPassword(loginHashedPassword); // hashing
            // to find password, the key is the key used to define the servercreds e.g key=1122 pass=hello now key=5464 pass =1122
            encKey = Encrypt(keyPass,"unlock", 128);
        }
        static byte[] GenerateValidKey(string key, int keySize)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                    Array.Resize(ref hash, keySize / 8);
                    return hash;
                }
            }
            catch
            {
                MessageBox.Show("Credentials invalid! Try again later!");
                return null;
            }

        }
        static string Encrypt(string data, string key, int keySize)
        {
            using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
            {
                tripleDES.Key = GenerateValidKey(key, keySize);
                tripleDES.Mode = CipherMode.ECB;

                using (ICryptoTransform encryptor = tripleDES.CreateEncryptor())
                {
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }
        private void collectOSSetup()
        {
            try
            {
                object result = sqlCC("select p.DiskNo from setuppref p join users u on p.id = u.id where u.username = '" + username + "'"); // cross table sql

                if (result != null)
                {
                    string input = result.ToString();
                    if (!string.IsNullOrEmpty(input))
                    {
                        try
                        {
                            diskNumber = input;
                        }
                        catch (FormatException)
                        {
                            diskNumber = "-1";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            try
            {
                object result = sqlCC("select p.DomainCommand from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");// cross table sql

                if (result != null)
                {
                    string inputs = result.ToString();

                    if (!string.IsNullOrEmpty(inputs))
                    {
                        domainLine = inputs;
                        isUsingDomain = true;
                    } else
                    {
                        MessageBox.Show("Empty!");
                    }
                }
            }
            catch { }

            try
            {
                object result = sqlCC("select p.OSUser from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");// cross table sql

                if (result != null)
                {
                    string inputsss = result.ToString();

                    if (!string.IsNullOrEmpty(inputsss))
                    {
                        osUsername = inputsss;
                    }
                }
            }
            catch { }

            try
            {
                object result = sqlCC("select p.OSPassword from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");// cross table sql

                if (result != null)
                {
                    string inputssss = result.ToString();

                    if (!string.IsNullOrEmpty(inputssss))
                    {
                        osPassword = inputssss;
                    }
                }
            }
            catch { }


        }
        private void collectOSSel()
        {
            try
            {
                object result = sqlCC("select p.osversion from preference p join users u on p.id = u.id where u.username = '" + username + "'");// cross table sql

                if (result != null)
                {
                    string input = result.ToString();
                    if (!string.IsNullOrEmpty(input))
                    {
                        try
                        {
                           
                           currentVersion = input;
                        }
                        catch (FormatException)
                        {
                            currentVersion = "-1";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            try
            {
                object result = sqlCC("select p.osrelease from preference p join users u on p.id = u.id where u.username = '" + username + "'");// cross table sql

                if (result != null)
                {
                    string inputs = result.ToString();

                    if (!string.IsNullOrEmpty(inputs))
                    {
                        currentRelease = inputs;
                    }
                }
            }
            catch { }
            try
            {
                object result = sqlCC("select p.oslanguage from preference p join users u on p.id = u.id where u.username = '" + username + "'");// cross table sql

                if (result != null)
                {
                    string inputsss = result.ToString();

                    if (!string.IsNullOrEmpty(inputsss))
                    {
                        currentLanguage = inputsss;
                    }
                }
            }
            catch { }
            
        }

        private void collectNinite()
        {
            try
            {
                object input = sqlCC("select p.niniteoptions from preference p join users u on p.id = u.id where u.username = '" + username + "'");// cross table sql
                if (input != null)
                {
                    string result = input.ToString();
                    applicationLine = result;
                }
            } catch { }
        }

        private object sqlCC(string query)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(serverCreds))
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
    }
}
