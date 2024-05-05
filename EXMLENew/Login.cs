using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using Microsoft.VisualBasic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using BCrypt;
using Npgsql;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;


namespace EXMLENew
{
    public partial class Login : Form
    {
        string user, pass, keyPass, key, serverCreds;
        bool isSuper;
        public bool isShut { get; set; }
        public Login()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.FirstUse)
            {
                Properties.Settings.Default.FirstUse = false;
                Properties.Settings.Default.Save();
                this.Hide();
                Setup st = new Setup();
                st.ShowDialog();
                this.Show();
            }
            if (isShut)
            {
                this.Close();
            }
            if (File.Exists("safeKey.txt") && !string.IsNullOrEmpty(File.ReadAllText("safeKey.txt")))
            {
                key = File.ReadAllText("safeKey.txt"); // writing and reading from a file
            }
            else
            {
                MessageBox.Show("No key found! Please import or create one!", "CRITICAL ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button1.Enabled = false;
            }
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            pass = BCrypt.Net.BCrypt.HashPassword(textBox2.Text); // hashing
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (keyPass != null)
            {
                string pureOut = Decrypt(key, keyPass, 128);
                if (pureOut == "errPass")
                {
                    MessageBox.Show("Failed to decrypt! Is your key or password correct?", "DECRYPTION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    serverCreds = pureOut;
                }
            }
            else
            {
                MessageBox.Show("Decryption key required to retrieve credentials.", "DECRYPTION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            string enteredPassword = textBox2.Text;
            string res = sqlCC("SELECT Password FROM Users WHERE Username = '" + user + "'");
            try
            {
                if (res != null && BCrypt.Net.BCrypt.Verify(enteredPassword, res))// hashing verify
                {
                    string isSuperUser = sqlCC("select username from users where issuper = true and username = '" + user + "'");
                    if (isSuperUser == user)
                    {
                        isSuper = true;
                    }
                    else
                    {
                        isSuper = false;
                    }
                    Dashboard dashboard = new Dashboard();
                    dashboard.username = user;
                    dashboard.isSuper = isSuper;
                    dashboard.serverCreds = serverCreds;
                    dashboard.key = key;
                    dashboard.keyPass = keyPass;
                    dashboard.loginHashedPassword = enteredPassword;
                    dashboard.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Invalid username or password");
                }
            }
            catch
            {
                MessageBox.Show("Can't validate password/ internal server error. Try again!");
            }
        }
        private string sqlCC(string query)// user made algo
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(serverCreds))
            {
                try
                {
                    connection.Open();
                    using (NpgsqlCommand selectCommand = new NpgsqlCommand(query, connection))
                    {
                        return selectCommand.ExecuteScalar() as string;
                    }
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            Create create = new Create();
            create.Show();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            keyPass = textBox3.Text;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Setup setup = new Setup();
            setup.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string userKey = Interaction.InputBox("Please paste your key here. If you need one, Click Create Key.", "Key Import");
            if (userKey != null)
            {
                keyPass = userKey;
                button1.Enabled = true;
                File.WriteAllText("safeKey.txt", keyPass);
                this.Close();
            }


        }
        static string Decrypt(string encryptedData, string key, int keySize)
        {
            using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
            {
                tripleDES.Key = GenerateValidKey(key, keySize);
                tripleDES.Mode = CipherMode.ECB;
                try
                {
                    using (ICryptoTransform decryptor = tripleDES.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
                catch
                {
                    return "errPass";
                }

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
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            user = textBox1.Text;
        }
    }
}
