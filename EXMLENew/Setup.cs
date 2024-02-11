using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt;
using System.Windows.Forms;

namespace EXMLENew
{
    public partial class Setup : Form
    {
        public string key, keyPass, serverCreds;
        public bool dropTables;
        public Setup()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://cockroachlabs.cloud/signup");
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            keyPass = textBox2.Text;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            key = textBox1.Text;    
        }
        private void genKey()
        {
            if (keyPass != null)
            {
                string pureOut = Decrypt(key, keyPass, 128);
                if (pureOut == "errPass")
                {
                    listBox1.Items.Add("Decryption failed!");
                    MessageBox.Show("Failed to decrypt! Is your key or password correct?", "DECRYPTION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    listBox1.Items.Add("Decryption passed!");
                    serverCreds = pureOut;
                }
            }
            else
            {
                MessageBox.Show("Decryption key required to retrieve credentials.", "DECRYPTION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        private void executeCode(string code)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(serverCreds))
            {
                try
                {
                    connection.Open();
                    using (NpgsqlCommand command = new NpgsqlCommand(code, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!dropTables)
            {
                dropTables = true;
            } else if (dropTables)
            {
                dropTables = false;
            }
        }
        private void dropTable()
        {
            string[] commands = {
        "ALTER TABLE preference DROP CONSTRAINT IF EXISTS preference_id_fkey",
        "ALTER TABLE setuppref DROP CONSTRAINT IF EXISTS setuppref_id_fkey",
        "DROP TABLE IF EXISTS users",
        "DROP TABLE IF EXISTS preference",
        "DROP TABLE IF EXISTS setuppref"
    };

            foreach (string command in commands)
            {
                executeCode(command);
            }
        }

        private void createTables()
        {
            string[]  commands = new string[3];
            commands[0] = "CREATE TABLE users (" +
    "id SERIAL PRIMARY KEY," +
    "username VARCHAR(20)," +
    "password VARCHAR(60)," +
    "isSuper BOOLEAN" +
    ");" + Environment.NewLine;
            commands[1] = "CREATE TABLE preference (" +
    "id INT REFERENCES users(id)," +
    "NiniteOptions VARCHAR(256)," +
    "OSVersion INT," +
    "OSRelease VARCHAR(128)," +
    "OSLanguage VARCHAR(32)" +
    ");" + Environment.NewLine;
            commands[2] = "CREATE TABLE setuppref (" +
    "id INT REFERENCES users(id)," +
    "DiskNo INT," +
    "DomainCommand VARCHAR(128)," +
    "isUsingSameLogin BOOLEAN," +
    "OSUser VARCHAR(16)," +
    "OSPassword VARCHAR(32)" +
    ");" + Environment.NewLine;
            for (int i=0; i < 3; i++)
            {
                executeCode(commands[i]);
            }
        }

        private void Setup_Load(object sender, EventArgs e)
        {

        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Create create = new Create();
            create.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add("Decrypting");
            genKey();
            if (dropTables)
            {
                listBox1.Items.Add("Deleting tables");
                dropTable();
            }
            listBox1.Items.Add("Creating tables");
            createTables();
            listBox1.Items.Add("Making Admin User");
            string passEnc = BCrypt.Net.BCrypt.HashPassword("Admin123");
            executeCode("INSERT INTO users (username, password, isSuper) VALUES ('Admin','"+ passEnc +"',true)");
            MessageBox.Show("New user account made! Your new login to dashboard is: " + Environment.NewLine + "Username:Admin" + Environment.NewLine + "Password:Admin123" + Environment.NewLine + "Please change it after you entered your dashboard!");
        }
    }
}
