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
        bool isOffline;
        string username,applicationLine, currentRelease, currentLanguage, osUsername, osPassword, domainLine, diskNumber, currentVersion, accPass, encKey;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.CheckedChanged -= checkBox1_CheckedChanged; 

            if (!isOffline)
            {
                MessageBox.Show("Program will now only read from XML!");
                isOffline = true;
            }
            else
            {
                MessageBox.Show("You just enabled Online Mode! This means that PortableISO will fetch the most latest data when applying the image. Click again to disable.");
                isOffline = false;
            }

            checkBox1.Checked = isOffline;

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

            using (XmlWriter writer = XmlWriter.Create(Path.Combine(location, "config.xml"), new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("EXMLE");
                writer.WriteStartElement("setupData");
                writer.WriteElementString("IsOffline", isOffline.ToString().ToLower());
                writer.WriteElementString("Username", username);
                writer.WriteElementString("Password", accPass);
                writer.WriteElementString("LoginKey", key);
                writer.WriteElementString("KeyPass", encKey);
                writer.WriteEndElement();
                if (!isOffline)
                {
                    writer.WriteStartElement("niniteOptions");
                    writer.WriteElementString("ApplicationLine", applicationLine);
                    writer.WriteEndElement();
                    writer.WriteStartElement("OSDownload");
                    writer.WriteElementString("CurrentVersion", currentVersion);
                    writer.WriteElementString("CurrentRelease", currentRelease);
                    writer.WriteElementString("CurrentLanguage", currentLanguage);
                    writer.WriteEndElement();
                    writer.WriteStartElement("OSConfig");
                    writer.WriteElementString("OSUsername", osUsername);
                    writer.WriteElementString("OSPassword", osPassword);
                    writer.WriteElementString("DiskNumber", diskNumber);
                    writer.WriteElementString("IsUsingDomain", isUsingDomain.ToString().ToLower());
                    writer.WriteElementString("DomainLine", domainLine);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();

                MessageBox.Show("File made!");
            }


        }


        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
    "Key: " + key + Environment.NewLine +
    "Key Pass: " + encKey + Environment.NewLine +
    "isOffline: " + isOffline.ToString() + Environment.NewLine +
    "Username: " + username + Environment.NewLine +
    "Application Line: " + applicationLine + Environment.NewLine +
    "Current Release: " + currentRelease.ToString() + Environment.NewLine +
    "Current Language: " + currentLanguage.ToString() + Environment.NewLine +
    "OS Username: " + osUsername + Environment.NewLine +
    "OS Password: " + osPassword + Environment.NewLine +
    "Domain Line: " + domainLine + Environment.NewLine +
    "Disk Number: " + diskNumber + Environment.NewLine +
    "Current Version: " + currentVersion + Environment.NewLine +
    "Account Password: " + accPass
);


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
            accPass = BCrypt.Net.BCrypt.HashPassword(loginHashedPassword);
            // to find password, the key is the key used to define the servercreds e.g key=1122 pass=hello now key=5464 pass =1122
            encKey = Encrypt(keyPass, key, 128);
            isOffline = true;
            MessageBox.Show("By default, isOffline is set to true. Do not panic because of the UI! Click See Data to confirm");
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
                object result = sqlCC("select p.DiskNo from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");

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
                object result = sqlCC("select p.DomainCommand from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");

                if (result != null)
                {
                    string inputs = result.ToString();

                    if (!string.IsNullOrEmpty(inputs))
                    {
                        domainLine = inputs;
                        isUsingDomain = true;
                    }
                }
            }
            catch { }

            try
            {
                object result = sqlCC("select p.OSUser from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");

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
                object result = sqlCC("select p.OSPassword from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");

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
                object result = sqlCC("select p.osversion from preference p join users u on p.id = u.id where u.username = '" + username + "'");

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
                object result = sqlCC("select p.osrelease from preference p join users u on p.id = u.id where u.username = '" + username + "'");

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
                object result = sqlCC("select p.oslanguage from preference p join users u on p.id = u.id where u.username = '" + username + "'");

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
                object input = sqlCC("select p.niniteoptions from preference p join users u on p.id = u.id where u.username = '" + username + "'");
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
