using Microsoft.VisualBasic;
using Npgsql;
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

namespace EXMLENew
{
    public partial class Dashboard : Form
    {
        public string username { get; set; }
        public bool isSuper { get; set; }
        public string serverCreds { get; set; }
        public string loginHashedPassword { get; set; }
        public string key { get; set; }
        public string keyPass { get; set; }

        public Dashboard()
        {
            InitializeComponent();
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            label1.Text = "Welcome " + username + "!";
            if (!isSuper)
            {
                button4.Visible = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            AdminPanel adminPanel = new AdminPanel();
            adminPanel.serverCreds = serverCreds;
            adminPanel.currentUser = username;
            adminPanel.Show();
        }
        private string sqlCC(string query)
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

        private void button8_Click(object sender, EventArgs e)
        {
            string pass = Interaction.InputBox("Please enter the password.", "Change Password");
            if (pass == "")
            {
                MessageBox.Show("No changes made");
            }
            else if (pass != null)
            {
                string command = $"UPDATE users SET password = '{BCrypt.Net.BCrypt.HashPassword(pass)}' WHERE username = '{username}'";
                sqlCC(command);
                MessageBox.Show("Password changed! Please log back in!");
                closeForm();
            }
            else
            {
                MessageBox.Show("No changes made");
            }
        }
        private void closeForm()
        {
            List<Form> formsToClose = new List<Form>();
            foreach (Form form in Application.OpenForms)
            {
                formsToClose.Add(form);
            }
            foreach (Form form in formsToClose)
            {
                form.Close();
            }
        }
        private void Dashboard_FormClosed(object sender, FormClosedEventArgs e)
        {
            closeForm();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            NiniteConfig ninite = new NiniteConfig();
            ninite.serverConfig = serverCreds;
            ninite.username = username;
            ninite.Show();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            OSConfig os = new OSConfig();
            os.serverConfig = serverCreds;
            os.username = username;
            os.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SetupOS os = new SetupOS();
            os.serverConfig = serverCreds;
            os.username = username;
            os.Show();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            XML xML = new XML();
            xML.loginUsername = username;
            xML.loginHashedPassword = loginHashedPassword;
            xML.serverCreds = serverCreds;
            xML.key = key;
            xML.keyPass = keyPass;
            xML.Show();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            var res = MessageBox.Show("Due to limitations, you must use iVentoy to PXE boot. Do you want to download it?", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                Process.Start("https://www.iventoy.com/");
            }
        }
    }
}
