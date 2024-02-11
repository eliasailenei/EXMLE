using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Mime;


namespace EXMLENew
{
    public partial class NiniteConfig : Form
    {
        DataTable dataBase;
        public string[] toInstall;
        public int amountOfProgs;
        public bool firstTime, pngFirstTime;
        public int toInstallPointer;
        public string serverConfig { get; set; }
        public string username { get; set; }    
        public NiniteConfig()
        {
            InitializeComponent();
        }

        private async void NiniteConfig_Load(object sender, EventArgs e)
        {
           
            label2.BeginInvoke(new Action(() => label2.Visible = true));
            checkIfEmpty();
            // reason for no pass retrival, there was no data to retrive as it was added wrong
            try
            {
                listBox1.Items.AddRange(toInstall);
            } catch
            {
                Console.WriteLine("No prev data");
            }
                

            
            await getLatest();
            await getSource();
            await popData();
            if (pngFirstTime)
            {
                await pngDownload();
            } else
            {
                await pngAlreadyExist();
            }
            await popList();
            label2.BeginInvoke(new Action(() => label2.Visible = false));
            dataBase = new DataTable();
            if (amountOfProgs == 0)
            {
                MessageBox.Show("It looks like something went wrong, please try on a different network or ensure NiniteForCMD is not corrupted", "CRITICAL ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (Directory.Exists("ICONS"))
                {
                    Directory.Delete("NiniteForCMD", true);
                    Directory.Delete("ICONS", true);
                }
            }
        }
        private void checkIfEmpty()
        {
            bool notNiniteEmpty = false;
            string input = sqlCC("select p.niniteoptions from preference p join users u on p.id = u.id where u.username = '" + username + "'");
            if ( input != null)
            {
                firstTime = false;
                notNiniteEmpty = true;
                seperate(input);
            }
            else if (input == null)
            {
                firstTime = true;
                Console.WriteLine("This is users first time");
            }
            else
            {
                firstTime= true;
            }
            input = sqlCC("select p.osrelease from preference p join users u on p.id = u.id where u.username = '" + username + "'");
            if (input != null)
            {
                firstTime = false;
            }
            else if (input == null)
            {
                if (!notNiniteEmpty)
                {
                    firstTime = true;
                    Console.WriteLine("User hasn't configured OS.");
                }
               
            }
        }
        public void seperate(string inputs)
        {
            MatchCollection matches = Regex.Matches(inputs, "'(.*?)'");
            string[] extractedValues = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                extractedValues[i] = matches[i].Groups[1].Value;
            }
            toInstall = extractedValues;
        }
        private string sqlCC(string query)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(serverConfig))
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
        private async Task getLatest()
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists("NiniteForCMD"))
                {
                    pngFirstTime = true;
                    Directory.CreateDirectory("NiniteForCMD");
                    try
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile("https://github.com/eliasailenei/NiniteForCMD/releases/download/Release/Program.zip", "NiniteForCMD.zip");
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("It looks like your ISP or network administrator has blocked Ninite. Please use a proxy / VPN or connect to a different network. You can also request for a whitelist. Here is complier error: " + e, "NETWORK ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                    ZipFile.ExtractToDirectory("NiniteForCMD.zip", "NiniteForCMD");
                    File.Delete("NiniteForCMD.zip");
                } else
                {
                    pngFirstTime= false;
                }
                //This would get the latest version of required tool.
            });
        }
        private async Task getSource()
        {
            await Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "NiniteForCMD\\NiniteForCMD.exe";
                process.StartInfo.Arguments = "EXPORT ALL";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            });
        }
        private async Task popData()
        {
            await Task.Run(() =>
            {
                dataBase = new DataTable();
                dataBase.Columns.Add("Title");
                dataBase.Columns.Add("Value");
                dataBase.Columns.Add("SRC");
                string[] source = File.ReadAllLines("EXPORT.txt");
                foreach (string item in source)
                {
                    DataRow newRow = dataBase.NewRow();
                    string[] parts = item.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        amountOfProgs++;
                        newRow["Value"] = parts[0].Trim();
                        newRow["SRC"] = parts[1].Trim();
                        newRow["Title"] = parts[2].Trim();
                        dataBase.Rows.Add(newRow);
                    }
                }
            });
        }
        private async Task popList()
        {
            await Task.Run(() =>
            {
                int pointer = 0;
                foreach (DataRow row in dataBase.Rows)
                {
                    int imageIndex = pointer;
                    listView1.BeginInvoke(new Action(() => listView1.Items.Add(row["Title"].ToString(), imageIndex)));
                    pointer++;
                }
                listView1.BeginInvoke(new Action(() => listView1.LargeImageList = imageList1));
            });
        }
        private async Task pngAlreadyExist()
        {
            await Task.Run(() =>
            {
                imageList1 = new ImageList();
                imageList1.ColorDepth = ColorDepth.Depth32Bit;
                imageList1.ImageSize = new Size(30, 30);
                int point =0;
                foreach (DataRow row in dataBase.Rows)
                {
                    string icon = row["SRC"].ToString();
                    if (!string.IsNullOrEmpty(icon))
                    {
                        string png = $"ICONS/ICON_{point}.png";

                       
                            Image image = Image.FromFile(png);
                            imageList1.Images.Add(image);
                        
                        point++;
                    }
                }
            });
        }
        private async Task pngDownload()
        {
            await Task.Run(() =>
            {
                imageList1 = new ImageList();
                imageList1.ColorDepth = ColorDepth.Depth32Bit;
                imageList1.ImageSize = new Size(30, 30);
                int point = 0;

                try
                {
                    if (Directory.Exists("ICONS"))
                    {
                        Directory.Delete("ICONS", true);
                    }

                    Directory.CreateDirectory("ICONS");
                    foreach (DataRow row in dataBase.Rows)
                    {
                        string icon = row["SRC"].ToString();
                        if (!string.IsNullOrEmpty(icon))
                        {
                            string png = $"ICONS/ICON_{point}.png";

                            try
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.DownloadFile(icon, png);
                                }

                                Image image = Image.FromFile(png);
                                imageList1.Images.Add(image);
                            }
                            catch (Exception ex)
                            {
                                if (png != null)
                                {
                                    Console.WriteLine("Minor error: PNG not possible to install, debug code: " + ex);
                                    File.Delete(png);
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile("https://static.vecteezy.com/system/resources/thumbnails/017/178/563/small/cross-check-icon-symbol-on-transparent-background-free-png.png", png);
                                    }
                                    Image image = Image.FromFile(png);
                                    imageList1.Images.Add(image);
                                }
                            }
                            point++;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e is IOException)
                    {
                        MessageBox.Show("Please run the program from the start, another session is in use");
                    }
                    else
                    {
                        MessageBox.Show("It looks like your ISP or network administrator has blocked Ninite. Please use a proxy / VPN or connect to a different network. You can also request for a whitelist. Here is complier error: " + e, "NETWORK ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                }
            });
        }


        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                ListViewItem selItem = listView1.SelectedItems[0];
                string selected = selItem.Text;
                if (toInstall == null)
                {
                    toInstall = new string[] { selected };
                }
                else
                {
                    if (toInstall.Contains(selected))
                    {
                        toInstall = toInstall.Where(item => item != selected).ToArray();
                    }
                    else
                    {
                        toInstall = toInstall.Concat(new string[] { selected }).ToArray();
                    }
                }
                listBox1.Items.Clear();
                listBox1.Items.AddRange(toInstall.Where(item => item != null).ToArray());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string finalResult = string.Empty;
            if (toInstall != null && toInstall.Length > 0)
            {
                StringBuilder result = new StringBuilder();
                foreach (string selectedText in toInstall)
                {
                    result.Append($"'{selectedText}' + ");
                }
                finalResult = result.ToString();

                if (finalResult.EndsWith("+ "))
                {
                    finalResult = finalResult.Remove(finalResult.Length - 2);
                }
            }
            
            string escapedFinalResult = finalResult.Replace("'", "''");
            string command = string.Empty;
            if (firstTime)
            {
                command = $"insert into preference (id, niniteoptions) select u.id, '{escapedFinalResult}' from users u where u.username = '{username}'";
                firstTime = false ;
            } else if (!firstTime) {
                command = $"update preference set niniteoptions = '{escapedFinalResult}' where id in (select u.id from users u where u.username = '{username}')";
            }
            sqlCC(command);

            MessageBox.Show("Update Complete!");
        }
    }
}
