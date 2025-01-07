using VideoTranslator.Controllers;

namespace VideoTranslator
{
    public partial class MainForm : Form
    {
        private TextBox _apiKeyTextBox;
        private Button _saveApiKeyButton;
        private Button _selectFileButton;
        public TextBox FilePathTextBox;
        private Button _processButton;
        private ProgressBar _progressBar;
        private RichTextBox _logWindow;
        private Label _fileSizeLabel;
        private Label _processingRateLabel;
        private Label _startTimeLabel;
        private Label _elapsedTimeLabel;
        private Label _estimatedCompletionTimeLabel;
        private readonly MainController _controller;

        public MainForm()
        {
            _controller = new MainController(this);
            InitializeComponents();
            LoadSavedApiKey();
            ValidateProcessButton();
            ApplyDarkTheme();
        }

        private void InitializeComponents()
        {
            this.Text = "Video to SRT Processor";
            this.Size = new System.Drawing.Size(600, 420);
            this.MinimumSize = new System.Drawing.Size(600, 420);

            var apiKeyLabel = new Label { Text = "OpenAI API Key:", Top = 20, Left = 10, Width = 120 };
            _apiKeyTextBox = new TextBox { Top = 20, Left = 140, Width = 250 };
            _apiKeyTextBox.TextChanged += (sender, e) => ValidateProcessButton();

            _saveApiKeyButton = new Button { Text = "Save", Top = 20, Left = 400, Width = 70 };
            _saveApiKeyButton.Click += (sender, e) => _controller.SaveApiKey(_apiKeyTextBox.Text);

            var fileLabel = new Label { Text = "Selected File:", Top = 60, Left = 10, Width = 120 };
            FilePathTextBox = new TextBox { Top = 60, Left = 140, Width = 250, ReadOnly = true };
            FilePathTextBox.TextChanged += (sender, e) => ValidateProcessButton();

            _selectFileButton = new Button { Text = "Browse", Top = 60, Left = 400, Width = 70 };
            _selectFileButton.Click += (sender, e) => _controller.SelectFile();

            _processButton = new Button { Text = "Process", Top = 100, Left = 10, Width = 100, Enabled = false };
            _processButton.Click += (sender, e) => _controller.ProcessSelectedFile();

            _progressBar = new ProgressBar
            {
                Top = 140, Left = 20, Width = (this.Width - 50), Height = 40, Visible = true, ForeColor = Color.RoyalBlue,
                BackColor = Color.FromArgb(50, 50, 50), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                
            };

            _fileSizeLabel = new Label { Text = "File Size: 0 MB", Top = 180, Left = 10, Width = 300 };
            _processingRateLabel = new Label { Text = "Processing Rate: 0.00 MB/s", Top = 210, Left = 10, Width = 300 };
            _startTimeLabel = new Label { Text = "Start Time: Not Started", Top = 240, Left = 10, Width = 300 };
            _elapsedTimeLabel = new Label { Text = "Elapsed Time: 0:00:00", Top = 270, Left = 10, Width = 300 };
            _estimatedCompletionTimeLabel = new Label
                { Text = "Estimated Completion: Not Available", Top = 300, Left = 10, Width = 300 };

            _logWindow = new RichTextBox
            {
                Top = 330,
                Left = 10,
                Width = 550,
                Height = 80,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                ReadOnly = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            this.Controls.Add(apiKeyLabel);
            this.Controls.Add(_apiKeyTextBox);
            this.Controls.Add(_saveApiKeyButton);
            this.Controls.Add(fileLabel);
            this.Controls.Add(FilePathTextBox);
            this.Controls.Add(_selectFileButton);
            this.Controls.Add(_processButton);
            this.Controls.Add(_progressBar);
            this.Controls.Add(_fileSizeLabel);
            this.Controls.Add(_processingRateLabel);
            this.Controls.Add(_startTimeLabel);
            this.Controls.Add(_elapsedTimeLabel);
            this.Controls.Add(_estimatedCompletionTimeLabel);
            this.Controls.Add(_logWindow);


            this.Resize += MainForm_Resize;
        }

        private void ApplyDarkTheme()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            foreach (Control control in this.Controls)
            {
                if (control is Label label)
                {
                    label.ForeColor = Color.FromArgb(150, 150, 150);
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = Color.FromArgb(50, 50, 50);
                    textBox.ForeColor = Color.FromArgb(150, 150, 150);
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is Button button)
                {
                    button.BackColor = Color.FromArgb(70, 70, 70);
                    button.ForeColor = Color.FromArgb(150, 150, 150);
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
                }
                else if (control is RichTextBox richTextBox)
                {
                    richTextBox.BackColor = Color.FromArgb(20, 20, 20);
                    richTextBox.ForeColor = Color.FromArgb(150, 150, 150);
                    richTextBox.BorderStyle = BorderStyle.FixedSingle;
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Ensure the top of the log window stays fixed while resizing
            _logWindow.Top = 330;
            _logWindow.Height = this.ClientSize.Height - _logWindow.Top - 10; // Adjust height dynamically
        }


        private void LoadSavedApiKey()
        {
            string savedApiKey = FileManager.LoadApiKey();
            if (!string.IsNullOrEmpty(savedApiKey))
            {
                _apiKeyTextBox.Text = savedApiKey;
                AppendToLog("API Key loaded from file.");
            }
        }

        private void ValidateProcessButton()
        {
            string[] validExtensions = { ".mp4", ".mkv", ".avi" };
            bool isApiKeyValid = !string.IsNullOrWhiteSpace(_apiKeyTextBox.Text);
            bool isFilePathValid = !string.IsNullOrWhiteSpace(FilePathTextBox.Text) &&
                                   validExtensions.Any(ext =>
                                       FilePathTextBox.Text.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            _processButton.Enabled = isApiKeyValid && isFilePathValid;
        }

        public void UpdateProgress(int progress)
        {
            _progressBar.Value = progress;
        }

        public void AppendToLog(string message, bool isError = false)
        {
            if (_logWindow.InvokeRequired)
            {
                _logWindow.Invoke(new Action(() => AppendToLog(message, isError)));
                return;
            }

            if (isError)
            {
                _logWindow.SelectionStart = _logWindow.Text.Length;
                _logWindow.SelectionLength = 0;
                _logWindow.SelectionColor = Color.Red;
            }
            else
            {
                _logWindow.SelectionStart = _logWindow.Text.Length;
                _logWindow.SelectionLength = 0;
                _logWindow.SelectionColor = Color.FromArgb(150, 150, 150);
            }

            _logWindow.AppendText(message + Environment.NewLine);
            _logWindow.ScrollToCaret();
        }

        public void UpdateFileSize(double sizeInMB)
        {
            _fileSizeLabel.Text = $"File Size: {sizeInMB:0.00} MB";
        }

        public void UpdateProcessingRate(double rateInMBps)
        {
            _processingRateLabel.Text = $"Processing Rate: {rateInMBps:0.00} MB/s";
        }

        public void UpdateStartTime(DateTime startTime)
        {
            _startTimeLabel.Text = $@"Start Time: {startTime:HH\:mm\:ss}";
        }

        public void UpdateElapsedTime(TimeSpan elapsedTime)
        {
            _elapsedTimeLabel.Text = $@"Elapsed Time: {elapsedTime:hh\:mm\:ss}";
        }

        public void UpdateEstimatedCompletionTime(DateTime estimatedTime)
        {
            _estimatedCompletionTimeLabel.Text = $@"Estimated Completion: {estimatedTime:HH\:mm\:ss}";
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowError(string message)
        {
            AppendToLog(message, isError: true);
        }
    }
}