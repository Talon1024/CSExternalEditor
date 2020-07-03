using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
namespace ExternalEditor
{
    enum TextType
    {
        UNKNOWN,
        ACS,
        DIALOGUE // Dialogue
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

        ~MainForm()
        {
            string tempDir = GetTempDirectory();
            Directory.Delete(tempDir, true);
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

        string GetTempDirectory()
        {
            string tempDir = Path.GetTempPath();
            char sep = Path.DirectorySeparatorChar;
            if (string.IsNullOrEmpty(tempDir))
            {
                tempDir = "/tmp"; // Unix folder for temporary files
            }
            return $"{tempDir}{sep}UDB";
        }

        string GetFilePath(string wadName, string mapName, TextType textType = TextType.UNKNOWN)
        {
            string[] extensions = {
                "txt",
                "acs",
                "dlg",
            };
            char sep = Path.DirectorySeparatorChar;
            string editDirPath = GetTempDirectory();
            if (!Directory.Exists(editDirPath))
            {
                Directory.CreateDirectory(editDirPath);
            }
            string editFileName = "{0}.{1}.{2}";
            string extension = extensions[(int)textType];
            string fileName = string.Format(editFileName, wadName, mapName, extension);
            return $"{editDirPath}{sep}{fileName}";
        }

        ProcessStartInfo GetEditor(string filePath)
        {
            // Get EDITOR environment variable
            string editCommand = Environment.GetEnvironmentVariable("EDITOR");
            // To be replaced later
            string editor;
            string arguments = "";
            // Find quote marks, if they exist
            char quoteMark = (char) 0;
            if(editCommand.StartsWith("\""))
            {
                quoteMark = '"';
            }
            else if(editCommand.StartsWith("'"))
            {
                quoteMark = '\'';
            }
            int quoteEnd = 0;
            if(quoteMark > 0)
            {
                quoteEnd = editCommand.IndexOf(quoteMark, 1);
                // Text between quotes
                editor = editCommand.Substring(1, quoteEnd - 2);
                // Text after quotes
                arguments = editCommand.Substring(quoteEnd + 1);
            }
            else
            {
                char[] whitespace = {' ', '\t', '\n'};
                string[] editorInfo = editCommand.Split(whitespace, 2);
                editor = editorInfo[0];
                if(editorInfo.Length > 1)
                {
                    arguments = editorInfo[1];
                }
            }
            arguments += $" {filePath}";
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = editor,
                Arguments = arguments
            };
            return startInfo;
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
            ProcessStartInfo editorStartInfo = GetEditor(filePath);
            Process editorProcess = new Process
            {
                EnableRaisingEvents = true, // So that the host app can do stuff with the text once the user is done editing it
                StartInfo = editorStartInfo
            };
            editorProcess.Start();
            editorRunning = !editorProcess.HasExited;
            if(editorRunning)
            {
                editorProcess.Exited += EditorProcess_Exited;
            }
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
            // Also, perform any actions configured to run after the editor is
            // closed, like compile the ACS script(s) if the editor was editing
            // an ACS script.
            textBox.Text = script;
        }

        public static void Main() {
            Application.Run(new MainForm());
        }
    }
}
