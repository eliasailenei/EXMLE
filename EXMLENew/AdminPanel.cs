using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace EXMLENew
{
    public partial class AdminPanel : Form
    {
        public string serverCreds { get; set; }
        public string currentUser { get; set; }
        DataTable userTable;
        DataTable prefTable;
        DataTable setupTable;
        public AdminPanel()
        {
            InitializeComponent();
        }

        private void AdminPanel_Load(object sender, EventArgs e)
        {
            userTable = popData($"SELECT * FROM users"); // single table SQL 
           dataGridView1.DataSource = userTable;
            prefTable = popData($"SELECT * FROM preference");// single table SQL 
            dataGridView2.DataSource = prefTable;
            setupTable = popData($"SELECT * FROM setuppref");// single table SQL 
            dataGridView3.DataSource = setupTable;
        }
        private void refreshUserBase()
        {
            userTable.Clear();
            userTable = popData($"SELECT * FROM users");// single table SQL 
            dataGridView1.DataSource = userTable;
        }
        private void refreshPrefBase()
        {
            prefTable.Clear();
            prefTable = popData($"SELECT * FROM preference");// single table SQL 
            dataGridView2.DataSource = prefTable;
        }
        private void refreshSetupBase()
        {
            setupTable.Clear();
            setupTable = popData($"SELECT * FROM setuppref");// single table SQL 
            dataGridView3.DataSource = setupTable;
        }
        private DataTable popData(string commands)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(serverCreds))
            {
                connection.Open();

                string query = commands;

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
        }
        private bool doesUserExist(string username)
        {
            if (sqlCC("SELECT username FROM users WHERE username = '" + username + "'") != null) // single table SQL 
            {
                return true;
            } else
            {
                return false;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string user = Interaction.InputBox("Please enter the username you want to change the password for.", "Change Password");
            bool invalid = (user == "Admin" || user == currentUser);
            if (user != null)
            {
                if (doesUserExist(user))
                {
                    string pass = Interaction.InputBox("Please enter the password.", "Change Password");
                    if (pass != null)
                    {
                        string command = $"UPDATE users SET password = '{BCrypt.Net.BCrypt.HashPassword(pass)}' WHERE username = '{user}'";// single table SQL 
                        sqlCC(command);
                        if (invalid)
                        {
                            MessageBox.Show("Password changed! Please log back in!");
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
                    } else
                    {
                        MessageBox.Show("No changes made");
                    }
                }
                else
                {
                    MessageBox.Show("Check username, it was not found!");
                }
            }
            else
            {
                MessageBox.Show("Check username, it was invalid!");
            }
            refreshUserBase();
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

        private void button3_Click(object sender, EventArgs e)
        {
            string user = Interaction.InputBox("Please enter the username you want to remove.", "Remove User");
            bool invalid = (user == "Admin" || user == currentUser);
            if (user != null && invalid == false) {
                if (doesUserExist(user))
                {
                    var res = MessageBox.Show("Are you sure you want to remove " + user + "? This cannot be undone.", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        sqlCC("DELETE FROM setuppref WHERE id = (SELECT id FROM users WHERE username = '" + user + "')"); // cross reference SQL
                        sqlCC("DELETE FROM preference WHERE id = (SELECT id FROM users WHERE username = '" + user + "')");// cross reference SQL 
                        sqlCC("DELETE FROM users WHERE username = '" + user + "'"); // single table sql

                    }
                }
                else
                {
                    MessageBox.Show("Check username, it was not found!");
                }
            } else
            {
                MessageBox.Show("Check username, it was invalid or Admin (you cannot remove Admin or yourself whilst logged in)!");
            }
            refreshUserBase();// recursive algo
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string passEnc = BCrypt.Net.BCrypt.HashPassword("changeme"); // hashing
            string acUser = "placehold";
            string acSuper = "false";
            string user = Interaction.InputBox("Please enter the username you want to add.", "Create User");
            if (user != null)
            {
                acUser = user;
            }
            string isSuper = Interaction.InputBox("Please enter True if you want the user to be an Admin (case sensitive).", "Create User");
            if (isSuper == "True")
            {
                acSuper = "true";
            } else
            {
                acSuper="false";
            }
            if (acUser != "placehold") {
                string userCreate = $"INSERT INTO users (username, password , issuper) VALUES ('{acUser}','{passEnc}',{acSuper})"; // single table sql
                sqlCC(userCreate);
                MessageBox.Show("User was created! Credentials:" + Environment.NewLine + "Username:" + acUser + Environment.NewLine + "Password:changeme" + Environment.NewLine + "Please ask " + acUser + " to change their password immediately!");
            } else
            {
                MessageBox.Show("No user was made");
            }
            refreshUserBase() ;
        }

        private void button5_Click(object sender, EventArgs e)
        {
           var warning= MessageBox.Show("Are you sure you want to wipe Preference? This cannot be undone!", "DATA WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (warning == DialogResult.Yes)
            {
                sqlCC("DELETE FROM preference");// single table sql
            } else
            {
                MessageBox.Show("No changes made!");
            }
            refreshPrefBase() ;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var warning = MessageBox.Show("Are you sure you want to wipe Setup Preference? This cannot be undone!", "DATA WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (warning == DialogResult.Yes)
            {
                sqlCC("DELETE FROM setuppref");// single table sql
            }
            else
            {
                MessageBox.Show("No changes made!");
            }
            refreshSetupBase() ;// recursive algo
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string user = Interaction.InputBox("Please enter the username you want to remove its user data.", "Remove User Date");
            if (user != null)
            {
                if (doesUserExist(user))
                {
                    var res = MessageBox.Show("Are you sure you want to remove " + user + "'s data? This cannot be undone.", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        sqlCC("DELETE FROM preference WHERE id = (SELECT id FROM users WHERE username = '" + user + "')");// cross reference SQL
                    }
                }
                else
                {
                    MessageBox.Show("Check username, it was not found!");
                }
            }
            else
            {
                MessageBox.Show("Check username, it was invalid or Admin (you cannot remove Admin or yourself whilst logged in)!");
            }
            refreshPrefBase(); // recusive algo
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string user = Interaction.InputBox("Please enter the username you want to remove its user data.", "Remove User Date");
            if (user != null)
            {
                if (doesUserExist(user))
                {
                    var res = MessageBox.Show("Are you sure you want to remove " + user + "'s data? This cannot be undone.", "WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        sqlCC("DELETE FROM setuppref WHERE id = (SELECT id FROM users WHERE username = '" + user + "')");// cross reference SQL
                    }
                }
                else
                {
                    MessageBox.Show("Check username, it was not found!");
                }
            }
            else
            {
                MessageBox.Show("Check username, it was invalid or Admin (you cannot remove Admin or yourself whilst logged in)!");
            }
            refreshSetupBase(); // recursive algo
        }
    }
}
