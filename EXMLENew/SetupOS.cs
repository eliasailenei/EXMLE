using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace EXMLENew
{
    public partial class SetupOS : Form
    {
        public string pcName, fullDomain,domain, userD, passDPlain,passDenc, user, passPlain, passenc;
        public int disk;
        bool firstTime;
        public string serverConfig { get; set; }
        public string username { get; set; }
        public bool isSame;

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            domain = textBox5.Text;
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            userD = textBox6.Text;
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            passDPlain = textBox7.Text;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            user = textBox3.Text;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!isSame)
            {
                isSame = true;
                user = username;
                textBox3.Enabled = false;
                textBox3.Text = user;
            } 
            else if (isSame)
            {
                isSame = false;
                user = "PortableISO";
                textBox3.Enabled = true;
                textBox3.Text = user;
            }  
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
                            firstTime = false;
                            disk = int.Parse(input);
                            textBox1.Text = disk.ToString();
                        }
                        catch (FormatException)
                        {
                            firstTime = false;
                            disk = 0;
                            textBox1.Text = disk.ToString();
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
                object result = sqlCC("select p.DomainCommand from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");

                if (result != null)
                {
                    string inputs = result.ToString();

                    if (!string.IsNullOrEmpty(inputs))
                    {
                        firstTime = false;
                        fullDomain = inputs;
                        textBox2.Text = "Premade, command is: " + fullDomain;
                        textBox5.Text = "Premade, command is: " + fullDomain;
                        textBox6.Text = "Premade, command is: " + fullDomain;
                        textBox7.Text = "Premade, command is: " + fullDomain;
                    }
                }
            }
            catch { }

            try
            {
                object result = sqlCC("select p.isUsingSameLogin from setuppref p join users u on p.id = u.id where u.username = '" + username + "'");

                if (result != null)
                {
                    string inputss = result.ToString();

                    if (!string.IsNullOrEmpty(inputss))
                    {
                        firstTime = false;
                        if (inputss == "true" || inputss == "True")
                        {
                            isSame = true;
                            textBox3.Enabled = false;
                        }
                        checkBox1.Checked = isSame;
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
                        firstTime = false;
                        user = inputsss;
                        textBox3.Text = user.ToString();
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
                        firstTime = false;
                        passenc = inputssss;
                        textBox4.Text = passenc.ToString();
                    }
                }
            }
            catch { }


        }
        private void button1_Click(object sender, EventArgs e)
        {
            passenc = Encrypt(passPlain, user, 128);
            if (firstTime)
            {
                sqlCC("INSERT INTO setuppref  (id, DiskNo, DomainCommand, isUsingSameLogin, OSUser, OSPassword) " +
                       $"SELECT u.id, '{disk}', '{domainCommandBuilder()}', '{isSame}', '{user}', '{passenc}' " +
                       $"FROM users u " +
                       $"WHERE u.username = '{username}'");
            }
            else if (!firstTime)
            {
                sqlCC($"UPDATE setuppref SET DiskNo = '{disk}' " +
                      $", DomainCommand = '{domainCommandBuilder()}' " +
                      $", isUsingSameLogin = '{isSame}' " +
                      $", OSUser = '{user}' " +
                      $", OSPassword = '{passenc}' " +
                      $"WHERE id IN (SELECT u.id FROM users u WHERE u.username = '{username}')");
            }
            MessageBox.Show("Update done!");
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
        private string domainCommandBuilder()
        {
            passDenc = Encrypt(passDPlain, userD, 128);
            return "netdom.exe join " + pcName + " /domain:" + domain + " /UserD:" + userD + " /PasswordD:" + passDenc;
        }
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            passPlain = textBox4.Text;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            pcName=textBox2.Text;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                disk = int.Parse(textBox1.Text);
            } catch {
                MessageBox.Show("Please only enter numbers");
            }
        }

        
        public SetupOS()
        {
            InitializeComponent();
        }

        private void SetupOS_Load(object sender, EventArgs e)
        {
            checkIfEmpty();
        }
    }
}
