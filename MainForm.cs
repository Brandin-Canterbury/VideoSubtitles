using VideoTranslator.Controllers;
using VideoTranslator.Enums;
using VideoTranslator.Utilities;

namespace VideoTranslator
{
    public partial class MainForm : Form
    {
        public TextBox ApiKeyTextBox;
        private Button _saveApiKeyButton;
        private Button _selectFileButton;
        public TextBox FilePathTextBox;
        private Button _processButton;
        private ProgressBar _mainProgressBar;
        private ProgressBar _secondaryProgressBar;
        private Button _cancelButton;
        private RichTextBox _logWindow;
        private Label _fileSizeLabel;
        private Label _processingRateLabel;
        private Label _startTimeLabel;
        private Label _elapsedTimeLabel;
        private Label _estimatedCompletionTimeLabel;
        
        
        private readonly MainFormController _controller;

        public MainForm()
        {
            InitializeComponents();
            ValidateControls();
            ApplyDarkTheme();
            _controller = new MainFormController(this);
        }

        private void InitializeComponents()
        {
            Text = "Video to Subtitle Creator";
            Size = new Size(600, 600);
            MinimumSize = new Size(600, 600);

            CreateApiKeyControls();
            CreateFileControls();
            CreateProcessButton();
            CreateProgressBars();
            CreateCancelButton();
            CreateLogWindow();
            
            Resize += MainForm_Resize;
            
            HandleCreated += OnHandleCreated;





            // _fileSizeLabel = new Label { Text = "File Size: 0 MB", Top = 180, Left = 10, Width = 300 };
            // _processingRateLabel = new Label { Text = "Processing Rate: 0.00 MB/s", Top = 210, Left = 10, Width = 300 };
            // _startTimeLabel = new Label { Text = "Start Time: Not Started", Top = 240, Left = 10, Width = 300 };
            // _elapsedTimeLabel = new Label { Text = "Elapsed Time: 0:00:00", Top = 270, Left = 10, Width = 300 };
            // _estimatedCompletionTimeLabel = new Label
            //     { Text = "Estimated Completion: Not Available", Top = 300, Left = 10, Width = 300 };
            //
            //
            //
            // Controls.Add(_fileSizeLabel);
            // Controls.Add(_processingRateLabel);
            // Controls.Add(_startTimeLabel);
            // Controls.Add(_elapsedTimeLabel);
            // Controls.Add(_estimatedCompletionTimeLabel);


        }

        private void OnHandleCreated(object? sender, EventArgs e)
        {
            _controller.GetApiKey();
        }

        #region Controls

        private void CreateApiKeyControls()
        {
            var apiKeyLabel = new Label { Text = "OpenAI API Key:", Top = 20, Left = 10, Width = 120 };
            Controls.Add(apiKeyLabel);
            
            ApiKeyTextBox = new TextBox { Top = 20, Left = 140, Width = 250 };
            ApiKeyTextBox.TextChanged += (sender, e) => ValidateControls();
            Controls.Add(ApiKeyTextBox);
            
            _saveApiKeyButton = new Button { Text = "Save", Top = 20, Left = 400, Width = 70 };
            _saveApiKeyButton.Click += (sender, e) =>
            {
                ValidateControls();
                _controller.SaveApiKey(ApiKeyTextBox.Text);
            };
            Controls.Add(_saveApiKeyButton);
        }

        private void CreateFileControls()
        {
            
            var fileLabel = new Label { Text = "Selected File:", Top = 60, Left = 10, Width = 120 };
            Controls.Add(fileLabel);
            
            FilePathTextBox = new TextBox { Top = 60, Left = 140, Width = 250, ReadOnly = true };
            FilePathTextBox.TextChanged += (sender, e) => ValidateControls();
            Controls.Add(FilePathTextBox);

            _selectFileButton = new Button { Text = "Browse", Top = 60, Left = 400, Width = 70 };
            _selectFileButton.Click += (sender, e) => _controller.SelectFile();
            Controls.Add(_selectFileButton);
        }

        private void CreateProcessButton()
        {
            _processButton = new Button { Text = "Process", Top = 100, Left = 10, Width = 100, Enabled = false };
            _processButton.Click += (sender, e) => _controller.ProcessSelectedFile();
            Controls.Add(_processButton);
        }

        private void CreateProgressBars()
        {
            
            _mainProgressBar = new ProgressBar
            {
                Top = 140, Left = 20, Width = (Width - 50), Height = 40, Visible = true, ForeColor = Color.RoyalBlue,
                BackColor = Color.FromArgb(50, 50, 50), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            Controls.Add(_mainProgressBar);
            
            _secondaryProgressBar = new ProgressBar
            {
                Top = 200, Left = 20, Width = (Width - 50), Height = 40, Visible = true, ForeColor = Color.RoyalBlue,
                BackColor = Color.FromArgb(50, 50, 50), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                
            };
            Controls.Add(_secondaryProgressBar);
        }

        private void CreateCancelButton()
        {
            
            _cancelButton = new Button { Text = "Cancel", Top = 260, Left = 10, Width = 100 };
            _cancelButton.Click += (sender, e) => _controller.CancelAllActions();
            Controls.Add(_cancelButton);
        }
        
        private void CreateLogWindow()
        {
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
            Controls.Add(_logWindow);
        }

        #endregion

        private void ApplyDarkTheme()
        {
            BackColor = Color.FromArgb(30, 30, 30);
            foreach (Control control in Controls)
            {
                switch (control)
                {
                    case Label label:
                        label.ForeColor = Color.FromArgb(150, 150, 150);
                        break;
                    case TextBox textBox:
                        textBox.BackColor = Color.FromArgb(50, 50, 50);
                        textBox.ForeColor = Color.FromArgb(150, 150, 150);
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case Button button:
                        button.BackColor = Color.FromArgb(70, 70, 70);
                        button.ForeColor = Color.FromArgb(150, 150, 150);
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
                        break;
                    case RichTextBox richTextBox:
                        richTextBox.BackColor = Color.FromArgb(20, 20, 20);
                        richTextBox.ForeColor = Color.FromArgb(150, 150, 150);
                        richTextBox.BorderStyle = BorderStyle.FixedSingle;
                        break;
                }
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Ensure the top of the log window stays fixed while resizing
            _logWindow.Top = 330;
            _logWindow.Height = ClientSize.Height - _logWindow.Top - 10; // Adjust height dynamically
        }

        private void ValidateControls()
        {
            string[] validExtensions = { ".mp4", ".mkv", ".avi" };
            var isApiKeyValid = !string.IsNullOrWhiteSpace(ApiKeyTextBox.Text);
            var isApiKeySaved = ApiKeyTextBox.Text == FileManager.LoadApiKey();
            var isFilePathValid = !string.IsNullOrWhiteSpace(FilePathTextBox.Text) &&
                                  validExtensions.Any(ext =>
                                      FilePathTextBox.Text.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            _processButton.Enabled = isApiKeyValid && isFilePathValid && isApiKeySaved;
            _saveApiKeyButton.Enabled = isApiKeyValid;
        }

        public void UpdateMainProgress(int progress)
        {
            _mainProgressBar.Value = progress;
        }
        
        public void UpdateSecondaryProgress(int progress)
        {
            _secondaryProgressBar.Value = progress;
        }

        public void AppendToLog(string message, LogType logType)
        {
           
            // if (_logWindow.InvokeRequired)
            // {
            //     _logWindow.Invoke(() => AppendToLog(message, logType));
            //     return;
            // }
            
            switch (logType)
            {
                case LogType.Error:
                    _logWindow.SelectionStart = _logWindow.Text.Length;
                    _logWindow.SelectionLength = 0;
                    _logWindow.SelectionColor = Color.Red;
                    break;
                case LogType.Success:
                    _logWindow.SelectionStart = _logWindow.Text.Length;
                    _logWindow.SelectionLength = 0;
                    _logWindow.SelectionColor = Color.Green;
                    break;
                case LogType.Default:
                    _logWindow.SelectionStart = _logWindow.Text.Length;
                    _logWindow.SelectionLength = 0;
                    _logWindow.SelectionColor = Color.FromArgb(150, 150, 150);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
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
       
    }
}