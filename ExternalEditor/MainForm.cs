using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
namespace ExternalEditor
{
    enum TextType
    {
        MISC,
        ACS,
        DIALOGUE
    }
    public class MainForm : Form
    {
        private TextBox textBox; // The text box containing the text to edit
        private Button editButton; // The button which launches the editor
        // private ListBox editorList; // The list of available editors
        private TableLayoutPanel layout;
        private string script;
        private bool editorRunning;

        public MainForm()
        {
            this.Text = "External editor";
            script = "It's time to kick gum and chew ass, and I'm all out of ass - Dick Kickem";
            var allSidesAnchorStyle = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            SuspendLayout();
            layout = new TableLayoutPanel { 
                Anchor = allSidesAnchorStyle,
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
            };
            Controls.Add(layout);
            InitializeComponents();
            ResumeLayout();
            PerformLayout();
        }

        private void InitializeComponents()
        {
            textBox = new TextBox
            {
                AcceptsReturn = false,
                AcceptsTab = false,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = script,
            };
            layout.SetColumnSpan(textBox, 2);
            layout.SetRow(textBox, 0);
            layout.SetColumn(textBox, 0);
            layout.Controls.Add(textBox);
            // Button
            editButton = new Button {
                Text = "Edit!",
            };
            layout.SetRow(editButton, 1);
            layout.SetColumn(editButton, 0);
            layout.Controls.Add(editButton);
            editButton.Click += EditButton_Click;
            /*
            editorList = new ListBox();
            layout.SetRow(editorList, 1);
            layout.SetColumn(editorList, 0);
            layout.Controls.Add(editorList);
            */
            layout.RowStyles.Add(new RowStyle
            {
                SizeType = SizeType.Percent,
                Height = 80
            });
        }

        string GetFilePath(string wadName, string mapName, TextType textType = TextType.MISC)
        {
            string editDirPath = "/tmp/UDB";
            if (!Directory.Exists(editDirPath))
            {
                Directory.CreateDirectory(editDirPath);
            }
            string editFileName = "SCRIPTS.{0}.{1}.{2}";
            string fileName = string.Format(editFileName, wadName, mapName, textType.ToString());
            return editDirPath + Path.DirectorySeparatorChar + fileName;
        }

        void EditButton_Click(object sender, EventArgs e)
        {
            if (editorRunning)
            {
                return;
            }
            // ========== Write scripts to external file ==========
            string wadName = "ROBERT.WAD";
            string mapName = "E3M7";
            string filePath = GetFilePath(wadName, mapName, TextType.ACS);
            StreamWriter writer = File.CreateText(filePath);
            writer.Write(script);
            writer.Close();
            // ========== Open file in editor ==========
            ProcessStartInfo editorStartInfo = new ProcessStartInfo { 
                FileName = "atom",
                Arguments = $"-w {filePath}"
            };
            Process editorProcess = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = editorStartInfo
            };
            editorProcess.Exited += EditorProcess_Exited;
            editorProcess.Start();
            editorRunning = !editorProcess.HasExited;
        }

        void EditorProcess_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Editor exited");
            if (!editorRunning)
            {
                return;
            }
            editorRunning = false;
            // ========== Open and read file contents ==========
            string filePath = GetFilePath("ROBERT.WAD", "E3M7", TextType.ACS);
            try
            {
                StreamReader reader = File.OpenText(filePath);
                script = reader.ReadToEnd();
                reader.Close();
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.Write(ex);
            }
            // ========== Display file contents in the text box ==========
            textBox.Text = script;
        }

        public static void Main() {
            Application.Run(new MainForm());
        }
    }
}
