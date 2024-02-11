﻿using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
namespace EXMLENew
{
    public partial class Create : Form
    {
        private string host, port, user, pass, ssl, db, raw, kpass;

        private bool serverExist(string command)
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(command))
                {
                    try
                    {
                        connection.Open();
                        return true;
                    }
                    catch 
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
            
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            user = textBox3.Text;
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            pass = textBox4.Text;
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            db = textBox6.Text;
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            ssl = textBox5.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            raw = "Host=" + host + ";Port=" + port + ";Database=" + db + ";Username=" + user + ";Password=" + pass + ";SslMode=" + ssl + ";";
            if (!serverExist(raw))
            {
               var mess = MessageBox.Show("Server failed to connect! Do you still want to continue?", "CONNECTION ERROR", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (mess == DialogResult.Yes)
                {
                    continues();
                }
            } else
            {
                continues();
            }
            
        }
        private void continues() {
            string encryptedData = Encrypt(raw, kpass, 128); 
            MessageBox.Show("Your key is:" + encryptedData + Environment.NewLine + "Keep this key safe as it holds your credentials. In a moment, notepad will open with your key so that you can store it elsewhere. DO NOT MODIFY THE TEXT FILE AS THIS SAVES YOUR KEY!", "IMPORTANT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            File.WriteAllText("safeKey.txt", encryptedData);
            Process.Start("notepad.exe", "safeKey.txt");
            this.Close();
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
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                Array.Resize(ref hash, keySize / 8);
                return hash;
            }
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            kpass = textBox7.Text;
        }

        static string Decrypt(string encryptedData, string key, int keySize)
        {
            using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
            {
                tripleDES.Key = GenerateValidKey(key, keySize);
                tripleDES.Mode = CipherMode.ECB;

                using (ICryptoTransform decryptor = tripleDES.CreateDecryptor())
                {
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }



        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("This is your password to decrypt the login key so that you can allow for the program to remember credentials for server.");
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            port = textBox2.Text;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            host = textBox1.Text;
        }

        public Create()
        {
            InitializeComponent();
        }

        private void Create_Load(object sender, EventArgs e)
        {

        }
        


        


    }
}
