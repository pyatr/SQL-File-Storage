using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FilesDataBase
{
    public partial class Form1 : Form
    {
        /*
        create table IMAGES(
        [id] int primary key identity,
        [file] varbinary(max),
        [description] nvarchar(256) not null,
        );
        
        create table AUDIO(
        [id] int primary key identity,
        [file] varbinary(max),
        [description] nvarchar(256) not null,
        );
        
        create table VIDEOS(
        [id] int primary key identity,
        [file] varbinary(max),
        [description] nvarchar(256) not null,
        );
        
        create table DOCUMENTS(
        [id] int primary key identity,
        [file] varbinary(max),
        [description] nvarchar(256) not null,
        );
        */

        public enum FileType
        {
            Image,
            Audio,
            Video,
            Document
        }

        public readonly string[] imagePermittedFormats = { "bmp", "jpg", "jpeg", "png", "gif" };
        public readonly string[] audioPermittedFormats = { "wav", "mp3", "ogg" };
        public readonly string[] videoPermittedFormats = { "mp4", "webm" };
        public readonly string[] documentPermittedFormats = { "txt", "doc", "xls", "xlsx" };

        public static readonly string dbname = "filestorage";
        public static readonly string sqlServerName = "(localdb)\\MSSQLLocalDB";
        public static readonly string dataBaseKey = "Data Source=" + sqlServerName + ";Database=" + dbname + ";Persist Security Info=False;Integrated Security=true;";
        public static readonly string masterKey = "Data Source=" + sqlServerName + ";Database=master;Persist Security Info=False;Integrated Security=true;";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] tableNames = { "Images", "Audio", "Videos", "Documents" };

            foreach (string s in tableNames)
            {
                listBox1.Items.Add(s);
                if (!TableExists(s.ToUpper()))
                {
                    string cmd = "create table " + s.ToUpper() + "(" +
                                    "[id] int primary key identity," +
                                    "[file] varbinary(max)," +
                                    "[description] nvarchar(256) not null,)";
                    SqlConnection scon = new SqlConnection(dataBaseKey);
                    scon.Open();
                    SqlCommand command = new SqlCommand(cmd, scon);
                    command.ExecuteNonQuery();
                    scon.Close();
                    Log("Создана таблица " + s.ToUpper());
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string categoryName = listBox1.GetItemText(listBox1.SelectedItem).ToUpper();
            string command = "SELECT * FROM " + categoryName;
            SqlConnection scon = new SqlConnection(dataBaseKey);
            scon.Open();
            SqlCommand cmd = new SqlCommand(command, scon);
            SqlDataReader dr = cmd.ExecuteReader();
            listBox2.Items.Clear();
            while (dr.Read())
            {
                listBox2.Items.Add(dr["description"]);
            }
            scon.Close();
        }

        bool TableExists(string tableName)
        {
            try
            {
                string cmd = "SELECT count(*) as Exist from INFORMATION_SCHEMA.TABLES where table_name = '" + tableName + "'";
                SqlConnection scon = new SqlConnection(dataBaseKey);
                scon.Open();
                SqlCommand command = new SqlCommand(cmd, scon);
                bool result = (int)command.ExecuteScalar() == 1;
                scon.Close();
                return result;
            }
            catch { return false; }
        }

        public bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    Log("Файл экспортирован: " + fileName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log("Exception caught in process: " + ex.ToString());
                return false;
            }
        }

        public Stream OpenFile(FileType type, out string filename)
        {
            int i;
            filename = "no file";
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string[] permittedFormats = null;
                switch (type)
                {
                    case FileType.Image: permittedFormats = imagePermittedFormats; break;
                    case FileType.Audio: permittedFormats = audioPermittedFormats; break;
                    case FileType.Video: permittedFormats = videoPermittedFormats; break;
                    case FileType.Document: permittedFormats = documentPermittedFormats; break;
                }
                if (permittedFormats != null)
                {
                    string formats = "|";
                    string filter = "All " + type.ToString() + " Files (";
                    for (i = 0; i < permittedFormats.Length; i++)
                    {
                        filter += permittedFormats[i];
                        formats += "*." + permittedFormats[i].ToUpper() + ";*." + permittedFormats[i].ToLower();
                        if (i < permittedFormats.Length - 1)
                        {
                            filter += ", ";
                            formats += ";";
                        }
                    }
                    filter += formats;
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = filter;
                    openFileDialog.FilterIndex = 0;
                    openFileDialog.RestoreDirectory = true;
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string[] str = openFileDialog.FileName.Split('\\');
                        filename = str.Last();
                        //MessageBox.Show(filename, "File name is " + filename, MessageBoxButtons.OK);
                        Stream fileStream = openFileDialog.OpenFile();
                        return fileStream;
                    }
                }
            }
            return null;
        }

        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static byte[] StreamToByteArray(Stream stream)
        {
            if (stream is MemoryStream)
            {
                return ((MemoryStream)stream).ToArray();
            }
            else
            {
                return ReadFully(stream);
            }
        }

        void InsertFileIntoTable(Stream file, string filename)
        {
            byte[] byteData = null;
            string tableName = null;
            string fileType = filename.Split('.').Last();
            foreach (string s in imagePermittedFormats)
                if (s == fileType)
                    tableName = "IMAGES";
            foreach (string s in audioPermittedFormats)
                if (s == fileType)
                    tableName = "AUDIO";
            foreach (string s in videoPermittedFormats)
                if (s == fileType)
                    tableName = "VIDEOS";
            foreach (string s in documentPermittedFormats)
                if (s == fileType)
                    tableName = "DOCUMENTS";

            byteData = StreamToByteArray(file);
            if (tableName != null)
            {
                using (SqlConnection sqlconnection = new SqlConnection(dataBaseKey))
                {
                    sqlconnection.Open();
                    string insertQuery = @"Insert Into " + tableName + " ([file],[description]) Values(@BinData,'" + filename + "')";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, sqlconnection);
                    SqlParameter sqlParam = insertCommand.Parameters.AddWithValue("@BinData", byteData);
                    sqlParam.DbType = DbType.Binary;
                    insertCommand.ExecuteNonQuery();
                    sqlconnection.Close();
                    Log(filename + " добавлен в таблицу " + tableName);
                }
            }
        }

        public void Log(string s)
        {
            richTextBox1.AppendText(s + '\n');
        }

        public void ChooseAndInsertFile(FileType type)
        {
            string filename;
            Stream fileStream = OpenFile(type, out filename);
            if (fileStream != null)
                InsertFileIntoTable(fileStream, filename);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChooseAndInsertFile(FileType.Image);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ChooseAndInsertFile(FileType.Audio);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ChooseAndInsertFile(FileType.Video);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ChooseAndInsertFile(FileType.Document);
        }

        private void button5_Click(object sender, EventArgs e)//Export
        {
            string path = "c:\\Users\\Public";
            string categoryName = listBox1.GetItemText(listBox1.SelectedItem);
            string fileName = listBox2.GetItemText(listBox2.SelectedItem);
            switch (categoryName)
            {
                case "Images": path += "\\Pictures"; break;
                case "Audio": path += "\\Music"; break;
                case "Videos": path += "\\Videos"; break;
                case "Documents": path += "\\Documents"; break;
            }
            path += "\\" + fileName;
            string command = "SELECT * FROM " + categoryName;
            SqlConnection scon = new SqlConnection(dataBaseKey);
            scon.Open();
            SqlCommand cmd = new SqlCommand(command, scon);
            SqlDataReader dr = cmd.ExecuteReader();
            byte[] fileBytes = null;
            while (dr.Read() && fileBytes == null)
            {
                if ((string)dr["description"] == fileName)
                {
                    fileBytes = (byte[])dr["file"];
                }
            }
            scon.Close();
            if (fileBytes != null)
                ByteArrayToFile(path, fileBytes);
        }
    }
}