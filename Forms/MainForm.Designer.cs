namespace ASLTv1.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Top Header Panel
            this.panelHeader = new System.Windows.Forms.Panel();
            this.labelTitle = new System.Windows.Forms.Label();
            this.btnSelectFolder = new System.Windows.Forms.Button();
            this.btnExportJson = new System.Windows.Forms.Button();
            this.btnDeleteJson = new System.Windows.Forms.Button();
            this.labelBoxCount = new System.Windows.Forms.Label();
            this.labelCurrentJsonFile = new System.Windows.Forms.Label();
            this.btnShowGuide = new System.Windows.Forms.Button();
            this.btnAbout = new System.Windows.Forms.Button();
            this.btnMinimize = new System.Windows.Forms.Button();
            this.btnMaximize = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();

            // Main Container
            this.panelMainContainer = new System.Windows.Forms.Panel();

            // Left Sidebar (Vertical Icon Toolbar)
            this.panelLeftSidebar = new System.Windows.Forms.Panel();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();

            // Center Video Area
            this.panelCenter = new System.Windows.Forms.Panel();
            this.pictureBoxVideo = new ASLTv1.Forms.FastPictureBox();

            // Video Controls (Bottom)
            this.panelVideoControls = new System.Windows.Forms.Panel();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnRewind = new System.Windows.Forms.Button();
            this.btnForward = new System.Windows.Forms.Button();
            this.labelTimeInfo = new System.Windows.Forms.Label();
            this.btnEntry = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnToggleSubtitle = new System.Windows.Forms.Button();
            this.panelTimeline = new System.Windows.Forms.Panel();

            // Right Sidebar (Info Panel)
            this.panelRightSidebar = new System.Windows.Forms.Panel();
            this.groupBoxObjectInfo = new System.Windows.Forms.GroupBox();
            this.labelObjectLabel = new System.Windows.Forms.Label();
            this.labelPrevWaypoint = new System.Windows.Forms.Label();
            this.labelNextWaypoint = new System.Windows.Forms.Label();
            this.groupBoxPersonWaypoint = new System.Windows.Forms.GroupBox();
            this.groupBoxVehicleWaypoint = new System.Windows.Forms.GroupBox();
            this.groupBoxEventWaypoint = new System.Windows.Forms.GroupBox();
            this.labelWaypointTime = new System.Windows.Forms.Label();
            this.groupBoxLabels = new System.Windows.Forms.GroupBox();
            this.btnLabelPerson = new System.Windows.Forms.Button();
            this.btnLabelVehicle = new System.Windows.Forms.Button();
            this.btnLabelEvent = new System.Windows.Forms.Button();
            this.panelBboxList = new System.Windows.Forms.Panel();
            this.btnDeleteLabel = new System.Windows.Forms.Button();
            this.btnExportJsonInLabels = new System.Windows.Forms.Button();
            this.labelModifyBox = new System.Windows.Forms.Label();
            this.comboBoxPerson = new System.Windows.Forms.ComboBox();
            this.comboBoxVehicle = new System.Windows.Forms.ComboBox();
            this.comboBoxEvent = new System.Windows.Forms.ComboBox();

            // Timer
            this.timerPlayback = new System.Windows.Forms.Timer(this.components);

            // ToolTip (USAB-01)
            this.toolTipMain = new System.Windows.Forms.ToolTip(this.components);
            this.toolTipMain.AutoPopDelay = 5000;
            this.toolTipMain.InitialDelay = 500;
            this.toolTipMain.ReshowDelay = 100;

            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).BeginInit();
            this.SuspendLayout();

            //
            // panelHeader
            //
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.panelHeader.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 50;
            this.panelHeader.Controls.Add(this.labelTitle);
            this.panelHeader.Controls.Add(this.btnSelectFolder);
            this.panelHeader.Controls.Add(this.btnExportJson);
            this.panelHeader.Controls.Add(this.btnDeleteJson);
            this.panelHeader.Controls.Add(this.labelBoxCount);
            this.panelHeader.Controls.Add(this.labelCurrentJsonFile);
            this.panelHeader.Controls.Add(this.btnShowGuide);
            this.panelHeader.Controls.Add(this.btnAbout);
            this.panelHeader.Controls.Add(this.btnMinimize);
            this.panelHeader.Controls.Add(this.btnMaximize);
            this.panelHeader.Controls.Add(this.btnClose);

            // Title (축약 표기 — 긴 제목이 버튼 가림 이슈로 단축)
            this.labelTitle.Text = "ASLT(v1.0)";
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.labelTitle.Location = new System.Drawing.Point(15, 12);
            this.labelTitle.Size = new System.Drawing.Size(150, 25);

            // File Select Button
            this.btnSelectFolder.Text = "파일 선택";
            this.btnSelectFolder.Location = new System.Drawing.Point(170, 10);
            this.btnSelectFolder.Size = new System.Drawing.Size(90, 30);
            this.btnSelectFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectFolder.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnSelectFolder.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnSelectFolder.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(63, 63, 70);
            this.btnSelectFolder.Click += new System.EventHandler(this.btnSelectFolder_Click);

            // Export JSON Button
            this.btnExportJson.Text = "JSON 저장";
            this.btnExportJson.Location = new System.Drawing.Point(270, 10);
            this.btnExportJson.Size = new System.Drawing.Size(90, 30);
            this.btnExportJson.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportJson.BackColor = System.Drawing.Color.FromArgb(78, 201, 176);
            this.btnExportJson.ForeColor = System.Drawing.Color.White;
            this.btnExportJson.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnExportJson.FlatAppearance.BorderSize = 0;
            this.btnExportJson.Click += new System.EventHandler(this.btnExportJson_Click);

            // Delete JSON Button
            this.btnDeleteJson.Text = "JSON 삭제";
            this.btnDeleteJson.Location = new System.Drawing.Point(365, 10);
            this.btnDeleteJson.Size = new System.Drawing.Size(90, 30);
            this.btnDeleteJson.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteJson.BackColor = System.Drawing.Color.FromArgb(199, 54, 54);
            this.btnDeleteJson.ForeColor = System.Drawing.Color.White;
            this.btnDeleteJson.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDeleteJson.FlatAppearance.BorderSize = 0;
            this.btnDeleteJson.Click += new System.EventHandler(this.btnDeleteJson_Click);

            // Box Count Label
            this.labelBoxCount.Text = "박스 개수:";
            this.labelBoxCount.Location = new System.Drawing.Point(460, 15);
            this.labelBoxCount.Size = new System.Drawing.Size(80, 25);
            this.labelBoxCount.ForeColor = System.Drawing.Color.FromArgb(157, 157, 157);
            this.labelBoxCount.Visible = false;

            // Current JSON File Label
            this.labelCurrentJsonFile.Text = "";
            this.labelCurrentJsonFile.Location = new System.Drawing.Point(465, 15);
            this.labelCurrentJsonFile.Size = new System.Drawing.Size(800, 20);
            this.labelCurrentJsonFile.ForeColor = System.Drawing.Color.FromArgb(157, 157, 157);
            this.labelCurrentJsonFile.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelCurrentJsonFile.AutoEllipsis = true;

            // Show Guide Button
            this.btnShowGuide.Text = "가이드 켜기";
            this.btnShowGuide.Location = new System.Drawing.Point(1295, 8);
            this.btnShowGuide.Size = new System.Drawing.Size(80, 35);
            this.btnShowGuide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnShowGuide.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnShowGuide.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnShowGuide.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(63, 63, 70);
            this.btnShowGuide.FlatAppearance.BorderSize = 1;
            this.btnShowGuide.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnShowGuide.Click += new System.EventHandler(this.btnShowGuide_Click);

            // About Button (NEW)
            this.btnAbout.Text = "정보";
            this.btnAbout.Location = new System.Drawing.Point(1380, 8);
            this.btnAbout.Size = new System.Drawing.Size(50, 35);
            this.btnAbout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAbout.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnAbout.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnAbout.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(63, 63, 70);
            this.btnAbout.FlatAppearance.BorderSize = 1;
            this.btnAbout.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);

            // Window Control Buttons (Right side)
            this.btnMinimize.Text = "−";
            this.btnMinimize.Location = new System.Drawing.Point(1440, 8);
            this.btnMinimize.Size = new System.Drawing.Size(35, 35);
            this.btnMinimize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMinimize.FlatAppearance.BorderSize = 0;
            this.btnMinimize.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnMinimize.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.btnMinimize.Click += new System.EventHandler(this.btnMinimize_Click);

            this.btnMaximize.Text = "□";
            this.btnMaximize.Location = new System.Drawing.Point(1480, 8);
            this.btnMaximize.Size = new System.Drawing.Size(35, 35);
            this.btnMaximize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMaximize.FlatAppearance.BorderSize = 0;
            this.btnMaximize.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnMaximize.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.btnMaximize.Click += new System.EventHandler(this.btnMaximize_Click);

            this.btnClose.Text = "✕";
            this.btnClose.Location = new System.Drawing.Point(1520, 8);
            this.btnClose.Size = new System.Drawing.Size(35, 35);
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            //
            // panelMainContainer
            //
            this.panelMainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMainContainer.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.panelMainContainer.Controls.Add(this.panelCenter);
            this.panelMainContainer.Controls.Add(this.panelLeftSidebar);
            this.panelMainContainer.Controls.Add(this.panelRightSidebar);

            //
            // panelLeftSidebar (Vertical Icon Toolbar)
            //
            this.panelLeftSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeftSidebar.Width = 60;
            this.panelLeftSidebar.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.panelLeftSidebar.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.panelLeftSidebar.Padding = new System.Windows.Forms.Padding(8);

            int iconBtnY = 10;
            int iconBtnSize = 44;
            int iconBtnSpacing = 10;

            // Select All Icon
            this.btnSelectAll.Location = new System.Drawing.Point(8, iconBtnY);
            this.btnSelectAll.Size = new System.Drawing.Size(iconBtnSize, iconBtnSize);
            this.btnSelectAll.Text = "☐";
            this.btnSelectAll.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.btnSelectAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectAll.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
            this.btnSelectAll.ForeColor = System.Drawing.Color.White;
            this.btnSelectAll.FlatAppearance.BorderSize = 0;
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
            iconBtnY += iconBtnSize + iconBtnSpacing;

            // Edit
            this.btnEdit.Location = new System.Drawing.Point(8, iconBtnY);
            this.btnEdit.Size = new System.Drawing.Size(iconBtnSize, iconBtnSize);
            this.btnEdit.Text = "✏️";
            this.btnEdit.Font = new System.Drawing.Font("Segoe UI", 16F);
            this.btnEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEdit.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnEdit.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnEdit.FlatAppearance.BorderSize = 0;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            iconBtnY += iconBtnSize + iconBtnSpacing;

            this.panelLeftSidebar.Controls.Add(this.btnSelectAll);
            this.panelLeftSidebar.Controls.Add(this.btnEdit);

            //
            // panelCenter (Video Area + Controls)
            //
            this.panelCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCenter.Padding = new System.Windows.Forms.Padding(16);
            this.panelCenter.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.panelCenter.Controls.Add(this.pictureBoxVideo);
            this.panelCenter.Controls.Add(this.panelVideoControls);

            //
            // pictureBoxVideo
            //
            this.pictureBoxVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxVideo.BackColor = System.Drawing.Color.Black;
            this.pictureBoxVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxVideo.TabStop = true;
            this.pictureBoxVideo.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxVideo_Paint);
            this.pictureBoxVideo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxVideo_MouseDown);
            this.pictureBoxVideo.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxVideo_MouseMove);
            this.pictureBoxVideo.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxVideo_MouseUp);
            this.pictureBoxVideo.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pictureBoxVideo_MouseDoubleClick);

            //
            // labelSubtitleTimestamp
            //
            this.labelSubtitleTimestamp = new System.Windows.Forms.Label();
            this.labelSubtitleTimestamp.AutoSize = false;
            this.labelSubtitleTimestamp.Size = new System.Drawing.Size(200, 30);
            this.labelSubtitleTimestamp.BackColor = System.Drawing.Color.FromArgb(180, 0, 0, 0);
            this.labelSubtitleTimestamp.ForeColor = System.Drawing.Color.White;
            this.labelSubtitleTimestamp.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            this.labelSubtitleTimestamp.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelSubtitleTimestamp.Text = "";
            this.labelSubtitleTimestamp.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.pictureBoxVideo.Controls.Add(this.labelSubtitleTimestamp);

            //
            // labelLoading (RELI-06: 영상 로드 중 표시)
            //
            this.labelLoading = new System.Windows.Forms.Label();
            this.labelLoading.Text = "영상 로드 중...";
            this.labelLoading.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.labelLoading.ForeColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.labelLoading.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            this.labelLoading.AutoSize = true;
            this.labelLoading.Padding = new System.Windows.Forms.Padding(20, 12, 20, 12);
            this.labelLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelLoading.Visible = false;
            this.pictureBoxVideo.Controls.Add(this.labelLoading);

            this.pictureBoxVideo.Resize += new System.EventHandler(this.pictureBoxVideo_Resize);

            //
            // panelVideoControls (Bottom control bar)
            //
            this.panelVideoControls.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelVideoControls.Height = 150;
            this.panelVideoControls.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.panelVideoControls.Padding = new System.Windows.Forms.Padding(12);
            this.panelVideoControls.Controls.Add(this.groupBoxObjectInfo);
            this.panelVideoControls.Controls.Add(this.btnPlay);
            this.panelVideoControls.Controls.Add(this.btnRewind);
            this.panelVideoControls.Controls.Add(this.btnForward);
            this.panelVideoControls.Controls.Add(this.labelTimeInfo);
            this.panelVideoControls.Controls.Add(this.btnEntry);
            this.panelVideoControls.Controls.Add(this.btnExit);
            this.panelVideoControls.Controls.Add(this.btnToggleSubtitle);
            this.panelVideoControls.Controls.Add(this.panelTimeline);
            this.panelVideoControls.Resize += new System.EventHandler(this.panelVideoControls_Resize);

            // Playback buttons
            this.btnPlay.Text = "▶";
            this.btnPlay.Location = new System.Drawing.Point(82, 16);
            this.btnPlay.Size = new System.Drawing.Size(40, 40);
            this.btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlay.FlatAppearance.BorderSize = 0;
            this.btnPlay.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.btnPlay.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnPlay.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);

            this.btnRewind.Text = "⏪";
            this.btnRewind.Location = new System.Drawing.Point(42, 16);
            this.btnRewind.Size = new System.Drawing.Size(40, 40);
            this.btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRewind.FlatAppearance.BorderSize = 0;
            this.btnRewind.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.btnRewind.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnRewind.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.btnRewind.Click += new System.EventHandler(this.btnRewind_Click);

            this.btnForward.Text = "⏩";
            this.btnForward.Location = new System.Drawing.Point(118, 16);
            this.btnForward.Size = new System.Drawing.Size(40, 40);
            this.btnForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnForward.FlatAppearance.BorderSize = 0;
            this.btnForward.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.btnForward.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnForward.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);

            // Time Info
            this.labelTimeInfo.Text = "00:00:00 / 00:00:00 1.0x";
            this.labelTimeInfo.Location = new System.Drawing.Point(160, 24);
            this.labelTimeInfo.Size = new System.Drawing.Size(200, 25);
            this.labelTimeInfo.ForeColor = System.Drawing.Color.FromArgb(157, 157, 157);
            this.labelTimeInfo.Font = new System.Drawing.Font("Consolas", 10F);

            // Entry/Exit buttons
            this.btnEntry.Text = "Entry";
            this.btnEntry.Location = new System.Drawing.Point(480, 16);
            this.btnEntry.Size = new System.Drawing.Size(110, 40);
            this.btnEntry.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
            this.btnEntry.ForeColor = System.Drawing.Color.White;
            this.btnEntry.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEntry.FlatAppearance.BorderSize = 0;
            this.btnEntry.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnEntry.Click += new System.EventHandler(this.btnEntry_Click);

            this.btnExit.Text = "Exit";
            this.btnExit.Location = new System.Drawing.Point(600, 16);
            this.btnExit.Size = new System.Drawing.Size(110, 40);
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);

            // btnToggleSubtitle
            this.btnToggleSubtitle.Text = "자막 열기";
            this.btnToggleSubtitle.Location = new System.Drawing.Point(370, 16);
            this.btnToggleSubtitle.Size = new System.Drawing.Size(100, 40);
            this.btnToggleSubtitle.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnToggleSubtitle.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.btnToggleSubtitle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleSubtitle.FlatAppearance.BorderSize = 0;
            this.btnToggleSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnToggleSubtitle.TabStop = false;
            this.btnToggleSubtitle.Click += new System.EventHandler(this.btnToggleSubtitle_Click);

            //
            // panelTimeline
            //
            this.panelTimeline.Location = new System.Drawing.Point(70, 70);
            this.panelTimeline.Size = new System.Drawing.Size(1000, 60);
            this.panelTimeline.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.panelTimeline.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.panelTimeline.Paint += new System.Windows.Forms.PaintEventHandler(this.panelTimeline_Paint);
            this.panelTimeline.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelTimeline_MouseDown);
            this.panelTimeline.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panelTimeline_MouseMove);
            this.panelTimeline.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panelTimeline_MouseUp);

            //
            // panelRightSidebar (Info Panel)
            //
            this.panelRightSidebar.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelRightSidebar.Width = 320;
            this.panelRightSidebar.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.panelRightSidebar.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.panelRightSidebar.Padding = new System.Windows.Forms.Padding(16);
            this.panelRightSidebar.AutoScroll = true;
            this.panelRightSidebar.TabStop = false;

            int rightY = 0;

            //
            // groupBoxObjectInfo (anchored top-right within panelVideoControls)
            //
            this.groupBoxObjectInfo.Text = "Object Info";
            this.groupBoxObjectInfo.Size = new System.Drawing.Size(280, 100);
            this.groupBoxObjectInfo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.groupBoxObjectInfo.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.groupBoxObjectInfo.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.groupBoxObjectInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.groupBoxObjectInfo.TabStop = false;
            this.groupBoxObjectInfo.Controls.Add(this.labelObjectLabel);

            this.labelObjectLabel.Text = "Label: -";
            this.labelObjectLabel.Location = new System.Drawing.Point(8, 18);
            this.labelObjectLabel.Size = new System.Drawing.Size(264, 78);
            this.labelObjectLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.labelObjectLabel.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.labelObjectLabel.AutoSize = false;

            this.labelPrevWaypoint.Text = "Previous Waypoint: -";
            this.labelPrevWaypoint.Location = new System.Drawing.Point(8, 38);
            this.labelPrevWaypoint.Size = new System.Drawing.Size(264, 20);
            this.labelPrevWaypoint.Font = new System.Drawing.Font("Segoe UI", 7F);
            this.labelPrevWaypoint.ForeColor = System.Drawing.Color.FromArgb(157, 157, 157);
            this.labelPrevWaypoint.AutoSize = false;
            this.labelPrevWaypoint.Visible = false;

            this.labelNextWaypoint.Text = "Next Waypoint: -";
            this.labelNextWaypoint.Location = new System.Drawing.Point(8, 58);
            this.labelNextWaypoint.Size = new System.Drawing.Size(264, 20);
            this.labelNextWaypoint.Font = new System.Drawing.Font("Segoe UI", 7F);
            this.labelNextWaypoint.ForeColor = System.Drawing.Color.FromArgb(157, 157, 157);
            this.labelNextWaypoint.AutoSize = false;
            this.labelNextWaypoint.Visible = false;

            //
            // groupBoxPersonWaypoint (Person - red theme, dark)
            //
            this.groupBoxPersonWaypoint.Text = "■ Person Waypoint";
            this.groupBoxPersonWaypoint.Location = new System.Drawing.Point(12, 0);
            this.groupBoxPersonWaypoint.Size = new System.Drawing.Size(280, 250);
            this.groupBoxPersonWaypoint.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupBoxPersonWaypoint.ForeColor = System.Drawing.Color.FromArgb(255, 107, 107);
            this.groupBoxPersonWaypoint.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.groupBoxPersonWaypoint.TabStop = false;

            this.listViewPersonWaypoints = new System.Windows.Forms.ListView();
            this.listViewPersonWaypoints.Location = new System.Drawing.Point(12, 25);
            this.listViewPersonWaypoints.Size = new System.Drawing.Size(260, 220);
            this.listViewPersonWaypoints.View = System.Windows.Forms.View.Details;
            this.listViewPersonWaypoints.FullRowSelect = true;
            this.listViewPersonWaypoints.TabStop = false;
            this.listViewPersonWaypoints.BackColor = System.Drawing.Color.FromArgb(60, 30, 30);
            this.listViewPersonWaypoints.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.listViewPersonWaypoints.Columns.Add("Entry", 80);
            this.listViewPersonWaypoints.Columns.Add("Exit", 80);
            this.listViewPersonWaypoints.Columns.Add("객체", 95);
            this.listViewPersonWaypoints.Click += new System.EventHandler(this.listViewPersonWaypoints_Click);
            this.listViewPersonWaypoints.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listViewWaypoints_MouseDown);

            this.groupBoxPersonWaypoint.Controls.Add(this.listViewPersonWaypoints);

            rightY += 270;

            //
            // groupBoxVehicleWaypoint (Vehicle - blue theme, dark)
            //
            this.groupBoxVehicleWaypoint.Text = "■ Vehicle Waypoint";
            this.groupBoxVehicleWaypoint.Location = new System.Drawing.Point(12, rightY);
            this.groupBoxVehicleWaypoint.Size = new System.Drawing.Size(280, 250);
            this.groupBoxVehicleWaypoint.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupBoxVehicleWaypoint.ForeColor = System.Drawing.Color.FromArgb(107, 158, 255);
            this.groupBoxVehicleWaypoint.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.groupBoxVehicleWaypoint.TabStop = false;

            this.listViewVehicleWaypoints = new System.Windows.Forms.ListView();
            this.listViewVehicleWaypoints.Location = new System.Drawing.Point(12, 25);
            this.listViewVehicleWaypoints.Size = new System.Drawing.Size(260, 220);
            this.listViewVehicleWaypoints.View = System.Windows.Forms.View.Details;
            this.listViewVehicleWaypoints.FullRowSelect = true;
            this.listViewVehicleWaypoints.TabStop = false;
            this.listViewVehicleWaypoints.BackColor = System.Drawing.Color.FromArgb(30, 30, 60);
            this.listViewVehicleWaypoints.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.listViewVehicleWaypoints.Columns.Add("Entry", 80);
            this.listViewVehicleWaypoints.Columns.Add("Exit", 80);
            this.listViewVehicleWaypoints.Columns.Add("객체", 95);
            this.listViewVehicleWaypoints.Click += new System.EventHandler(this.listViewVehicleWaypoints_Click);
            this.listViewVehicleWaypoints.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listViewWaypoints_MouseDown);

            this.groupBoxVehicleWaypoint.Controls.Add(this.listViewVehicleWaypoints);

            rightY += 270;

            //
            // groupBoxEventWaypoint (Event - green theme, dark)
            //
            this.groupBoxEventWaypoint.Text = "■ Event Waypoint";
            this.groupBoxEventWaypoint.Location = new System.Drawing.Point(12, rightY);
            this.groupBoxEventWaypoint.Size = new System.Drawing.Size(280, 250);
            this.groupBoxEventWaypoint.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupBoxEventWaypoint.ForeColor = System.Drawing.Color.FromArgb(107, 255, 107);
            this.groupBoxEventWaypoint.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.groupBoxEventWaypoint.TabStop = false;

            this.listViewEventWaypoints = new System.Windows.Forms.ListView();
            this.listViewEventWaypoints.Location = new System.Drawing.Point(12, 25);
            this.listViewEventWaypoints.Size = new System.Drawing.Size(260, 220);
            this.listViewEventWaypoints.View = System.Windows.Forms.View.Details;
            this.listViewEventWaypoints.FullRowSelect = true;
            this.listViewEventWaypoints.TabStop = false;
            this.listViewEventWaypoints.BackColor = System.Drawing.Color.FromArgb(30, 60, 30);
            this.listViewEventWaypoints.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.listViewEventWaypoints.Columns.Add("Event", 80);
            this.listViewEventWaypoints.Columns.Add("timestamp", 95);
            this.listViewEventWaypoints.Columns.Add("객체(P/V)", 80);
            this.listViewEventWaypoints.Click += new System.EventHandler(this.listViewEventWaypoints_Click);
            this.listViewEventWaypoints.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listViewWaypoints_MouseDown);
            this.listViewEventWaypoints.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listViewEventWaypoints_MouseUp);

            this.groupBoxEventWaypoint.Controls.Add(this.listViewEventWaypoints);

            rightY += 270;

            //
            // btnDeleteEventWaypoint (Waypoint delete button)
            //
            this.btnDeleteEventWaypoint = new System.Windows.Forms.Button();
            this.btnDeleteEventWaypoint.Text = "선택한 Waypoint 삭제";
            this.btnDeleteEventWaypoint.Location = new System.Drawing.Point(12, rightY);
            this.btnDeleteEventWaypoint.Size = new System.Drawing.Size(280, 35);
            this.btnDeleteEventWaypoint.TabStop = false;
            this.btnDeleteEventWaypoint.BackColor = System.Drawing.Color.FromArgb(199, 54, 54);
            this.btnDeleteEventWaypoint.ForeColor = System.Drawing.Color.White;
            this.btnDeleteEventWaypoint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDeleteEventWaypoint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteEventWaypoint.FlatAppearance.BorderSize = 0;
            this.btnDeleteEventWaypoint.Click += new System.EventHandler(this.btnDeleteSelectedWaypoint_Click);

            rightY += 50;

            //
            // groupBoxLabels
            //
            this.groupBoxLabels.Text = "Labels";
            this.groupBoxLabels.Location = new System.Drawing.Point(12, rightY);
            this.groupBoxLabels.Size = new System.Drawing.Size(288, 260);
            this.groupBoxLabels.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupBoxLabels.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
            this.groupBoxLabels.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.groupBoxLabels.TabStop = false;

            // Label type selection buttons
            this.btnLabelPerson.Text = "person";
            this.btnLabelPerson.Location = new System.Drawing.Point(12, 25);
            this.btnLabelPerson.Size = new System.Drawing.Size(78, 35);
            this.btnLabelPerson.BackColor = System.Drawing.Color.FromArgb(60, 30, 30);
            this.btnLabelPerson.ForeColor = System.Drawing.Color.FromArgb(255, 107, 107);
            this.btnLabelPerson.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLabelPerson.FlatAppearance.BorderSize = 2;
            this.btnLabelPerson.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 107, 107);
            this.btnLabelPerson.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnLabelPerson.TabStop = false;
            this.btnLabelPerson.Click += new System.EventHandler(this.btnLabelPerson_Click);

            this.btnLabelVehicle.Text = "vehicle";
            this.btnLabelVehicle.Location = new System.Drawing.Point(95, 25);
            this.btnLabelVehicle.Size = new System.Drawing.Size(78, 35);
            this.btnLabelVehicle.BackColor = System.Drawing.Color.FromArgb(30, 30, 60);
            this.btnLabelVehicle.ForeColor = System.Drawing.Color.FromArgb(107, 158, 255);
            this.btnLabelVehicle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLabelVehicle.FlatAppearance.BorderSize = 2;
            this.btnLabelVehicle.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(107, 158, 255);
            this.btnLabelVehicle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnLabelVehicle.TabStop = false;
            this.btnLabelVehicle.Click += new System.EventHandler(this.btnLabelVehicle_Click);

            this.btnLabelEvent.Text = "event";
            this.btnLabelEvent.Location = new System.Drawing.Point(178, 25);
            this.btnLabelEvent.Size = new System.Drawing.Size(78, 35);
            this.btnLabelEvent.BackColor = System.Drawing.Color.FromArgb(30, 60, 30);
            this.btnLabelEvent.ForeColor = System.Drawing.Color.FromArgb(107, 255, 107);
            this.btnLabelEvent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLabelEvent.FlatAppearance.BorderSize = 2;
            this.btnLabelEvent.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(107, 255, 107);
            this.btnLabelEvent.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnLabelEvent.TabStop = false;
            this.btnLabelEvent.Click += new System.EventHandler(this.btnLabelEvent_Click);

            // Person list toggle label
            this.labelPersonList = new System.Windows.Forms.Label();
            this.labelPersonList.Text = "> person";
            this.labelPersonList.Location = new System.Drawing.Point(8, 70);
            this.labelPersonList.Size = new System.Drawing.Size(270, 30);
            this.labelPersonList.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.labelPersonList.BackColor = System.Drawing.Color.FromArgb(60, 30, 30);
            this.labelPersonList.ForeColor = System.Drawing.Color.FromArgb(255, 107, 107);
            this.labelPersonList.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelPersonList.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.labelPersonList.Cursor = System.Windows.Forms.Cursors.Hand;
            this.labelPersonList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelPersonList.Click += new System.EventHandler(this.TogglePersonPanel);

            // Person list panel
            this.panelPersonList = new System.Windows.Forms.Panel();
            this.panelPersonList.Location = new System.Drawing.Point(8, 100);
            this.panelPersonList.Size = new System.Drawing.Size(270, 100);
            this.panelPersonList.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.panelPersonList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPersonList.AutoScroll = true;
            this.panelPersonList.MaximumSize = new System.Drawing.Size(270, 100);
            this.panelPersonList.TabStop = false;
            this.panelPersonList.Visible = false;

            // Vehicle list toggle label
            this.labelVehicleList = new System.Windows.Forms.Label();
            this.labelVehicleList.Text = "> vehicle";
            this.labelVehicleList.Location = new System.Drawing.Point(8, 100);
            this.labelVehicleList.Size = new System.Drawing.Size(270, 30);
            this.labelVehicleList.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.labelVehicleList.BackColor = System.Drawing.Color.FromArgb(30, 30, 60);
            this.labelVehicleList.ForeColor = System.Drawing.Color.FromArgb(107, 158, 255);
            this.labelVehicleList.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelVehicleList.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.labelVehicleList.Cursor = System.Windows.Forms.Cursors.Hand;
            this.labelVehicleList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelVehicleList.Click += new System.EventHandler(this.ToggleVehiclePanel);

            // Vehicle list panel
            this.panelVehicleList = new System.Windows.Forms.Panel();
            this.panelVehicleList.Location = new System.Drawing.Point(8, 130);
            this.panelVehicleList.Size = new System.Drawing.Size(270, 100);
            this.panelVehicleList.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.panelVehicleList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelVehicleList.AutoScroll = true;
            this.panelVehicleList.MaximumSize = new System.Drawing.Size(270, 100);
            this.panelVehicleList.TabStop = false;
            this.panelVehicleList.Visible = false;

            // Event list toggle label
            this.labelEventList = new System.Windows.Forms.Label();
            this.labelEventList.Text = "> event";
            this.labelEventList.Location = new System.Drawing.Point(8, 130);
            this.labelEventList.Size = new System.Drawing.Size(270, 30);
            this.labelEventList.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.labelEventList.BackColor = System.Drawing.Color.FromArgb(30, 60, 30);
            this.labelEventList.ForeColor = System.Drawing.Color.FromArgb(107, 255, 107);
            this.labelEventList.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelEventList.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.labelEventList.Cursor = System.Windows.Forms.Cursors.Hand;
            this.labelEventList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelEventList.Click += new System.EventHandler(this.ToggleEventPanel);

            // Event list panel
            this.panelEventList = new System.Windows.Forms.Panel();
            this.panelEventList.Location = new System.Drawing.Point(8, 160);
            this.panelEventList.Size = new System.Drawing.Size(270, 100);
            this.panelEventList.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.panelEventList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelEventList.AutoScroll = true;
            this.panelEventList.MaximumSize = new System.Drawing.Size(270, 100);
            this.panelEventList.TabStop = false;
            this.panelEventList.Visible = false;

            // panelBboxList references panelPersonList for compatibility
            this.panelBboxList = this.panelPersonList;

            // Delete label button
            this.btnDeleteLabel.Text = "선택한 Bbox 삭제";
            this.btnDeleteLabel.Location = new System.Drawing.Point(8, 165);
            this.btnDeleteLabel.Size = new System.Drawing.Size(270, 35);
            this.btnDeleteLabel.BackColor = System.Drawing.Color.FromArgb(199, 54, 54);
            this.btnDeleteLabel.ForeColor = System.Drawing.Color.White;
            this.btnDeleteLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteLabel.FlatAppearance.BorderSize = 0;
            this.btnDeleteLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDeleteLabel.TabStop = false;
            this.btnDeleteLabel.Click += new System.EventHandler(this.btnDeleteLabel_Click);

            // Export JSON in Labels button
            this.btnExportJsonInLabels.Text = "JSON 저장";
            this.btnExportJsonInLabels.Location = new System.Drawing.Point(8, 207);
            this.btnExportJsonInLabels.Size = new System.Drawing.Size(270, 35);
            this.btnExportJsonInLabels.BackColor = System.Drawing.Color.FromArgb(78, 201, 176);
            this.btnExportJsonInLabels.ForeColor = System.Drawing.Color.White;
            this.btnExportJsonInLabels.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportJsonInLabels.FlatAppearance.BorderSize = 0;
            this.btnExportJsonInLabels.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnExportJsonInLabels.TabStop = false;
            this.btnExportJsonInLabels.Click += new System.EventHandler(this.btnExportJson_Click);

            this.groupBoxLabels.Controls.Add(this.btnLabelPerson);
            this.groupBoxLabels.Controls.Add(this.btnLabelVehicle);
            this.groupBoxLabels.Controls.Add(this.btnLabelEvent);
            this.groupBoxLabels.Controls.Add(this.labelPersonList);
            this.groupBoxLabels.Controls.Add(this.panelPersonList);
            this.groupBoxLabels.Controls.Add(this.labelVehicleList);
            this.groupBoxLabels.Controls.Add(this.panelVehicleList);
            this.groupBoxLabels.Controls.Add(this.labelEventList);
            this.groupBoxLabels.Controls.Add(this.panelEventList);
            this.groupBoxLabels.Controls.Add(this.btnDeleteLabel);
            this.groupBoxLabels.Controls.Add(this.btnExportJsonInLabels);

            rightY += 275;

            this.panelRightSidebar.Controls.Add(this.groupBoxPersonWaypoint);
            this.panelRightSidebar.Controls.Add(this.groupBoxVehicleWaypoint);
            this.panelRightSidebar.Controls.Add(this.groupBoxEventWaypoint);
            this.panelRightSidebar.Controls.Add(this.btnDeleteEventWaypoint);
            this.panelRightSidebar.Controls.Add(this.groupBoxLabels);

            //
            // timerPlayback
            //
            this.timerPlayback.Interval = 33; // ~30 FPS
            this.timerPlayback.Tick += new System.EventHandler(this.timerPlayback_Tick);

            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.Controls.Add(this.panelMainContainer);
            this.Controls.Add(this.panelHeader);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.Text = "ASLT v1.0 - ANNA Synthetic data Labeling Toolkit";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.Load += new System.EventHandler(this.MainForm_Load);

            // ToolTip bindings (USAB-01)
            this.toolTipMain.SetToolTip(this.btnSelectFolder, "영상 파일 선택 (비디오 열기)");
            this.toolTipMain.SetToolTip(this.btnExportJson, "JSON 저장 (Ctrl+S)");
            this.toolTipMain.SetToolTip(this.btnDeleteJson, "JSON 삭제");
            this.toolTipMain.SetToolTip(this.btnShowGuide, "시작 가이드 다시 보기");
            this.toolTipMain.SetToolTip(this.btnAbout, "프로그램 정보 및 단축키");
            this.toolTipMain.SetToolTip(this.btnMinimize, "최소화");
            this.toolTipMain.SetToolTip(this.btnMaximize, "최대화/복원");
            this.toolTipMain.SetToolTip(this.btnClose, "닫기");
            this.toolTipMain.SetToolTip(this.btnSelectAll, "전체 선택");
            this.toolTipMain.SetToolTip(this.btnEdit, "편집 모드");
            this.toolTipMain.SetToolTip(this.btnPlay, "재생/정지 (Space)");
            this.toolTipMain.SetToolTip(this.btnRewind, "되감기");
            this.toolTipMain.SetToolTip(this.btnForward, "앞으로");
            this.toolTipMain.SetToolTip(this.btnEntry, "Entry 마커 (E)");
            this.toolTipMain.SetToolTip(this.btnExit, "Exit 마커 (X)");
            this.toolTipMain.SetToolTip(this.btnToggleSubtitle, "자막 토글 (C)");
            this.toolTipMain.SetToolTip(this.btnLabelPerson, "Person 라벨 (F1)");
            this.toolTipMain.SetToolTip(this.btnLabelVehicle, "Vehicle 라벨 (F2)");
            this.toolTipMain.SetToolTip(this.btnLabelEvent, "Event 라벨 (F3)");
            this.toolTipMain.SetToolTip(this.btnDeleteLabel, "선택 라벨 삭제 (Delete)");
            this.toolTipMain.SetToolTip(this.btnExportJsonInLabels, "라벨 데이터 JSON 저장");
            this.toolTipMain.SetToolTip(this.btnDeleteEventWaypoint, "선택한 Waypoint 삭제");

            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        // Header
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.Button btnExportJson;
        private System.Windows.Forms.Button btnDeleteJson;
        private System.Windows.Forms.Label labelBoxCount;
        private System.Windows.Forms.Label labelCurrentJsonFile;
        private System.Windows.Forms.Button btnShowGuide;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Button btnMinimize;
        private System.Windows.Forms.Button btnMaximize;
        private System.Windows.Forms.Button btnClose;

        // Main Container
        private System.Windows.Forms.Panel panelMainContainer;

        // Left Sidebar
        private System.Windows.Forms.Panel panelLeftSidebar;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnEdit;

        // Center Video
        private System.Windows.Forms.Panel panelCenter;
        private ASLTv1.Forms.FastPictureBox pictureBoxVideo;

        // Video Controls
        private System.Windows.Forms.Panel panelVideoControls;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.Button btnForward;
        private System.Windows.Forms.Label labelTimeInfo;
        private System.Windows.Forms.Label labelSubtitleTimestamp;
        private System.Windows.Forms.Label labelLoading;
        private System.Windows.Forms.Button btnEntry;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnToggleSubtitle;
        private System.Windows.Forms.Panel panelTimeline;

        // Right Sidebar
        private System.Windows.Forms.Panel panelRightSidebar;
        private System.Windows.Forms.GroupBox groupBoxObjectInfo;
        private System.Windows.Forms.Label labelObjectLabel;
        private System.Windows.Forms.Label labelPrevWaypoint;
        private System.Windows.Forms.Label labelNextWaypoint;
        private System.Windows.Forms.GroupBox groupBoxPersonWaypoint;
        private System.Windows.Forms.GroupBox groupBoxVehicleWaypoint;
        private System.Windows.Forms.GroupBox groupBoxEventWaypoint;
        private System.Windows.Forms.Label labelWaypointTime;
        private System.Windows.Forms.ListView listViewPersonWaypoints;
        private System.Windows.Forms.ListView listViewVehicleWaypoints;
        private System.Windows.Forms.ListView listViewEventWaypoints;
        private System.Windows.Forms.Button btnDeleteEventWaypoint;
        private System.Windows.Forms.GroupBox groupBoxLabels;
        private System.Windows.Forms.Button btnLabelPerson;
        private System.Windows.Forms.Button btnLabelVehicle;
        private System.Windows.Forms.Button btnLabelEvent;
        private System.Windows.Forms.Label labelPersonList;
        private System.Windows.Forms.Panel panelPersonList;
        private System.Windows.Forms.Label labelVehicleList;
        private System.Windows.Forms.Panel panelVehicleList;
        private System.Windows.Forms.Label labelEventList;
        private System.Windows.Forms.Panel panelEventList;
        private System.Windows.Forms.Panel panelBboxList;
        private System.Windows.Forms.Button btnDeleteLabel;
        private System.Windows.Forms.Button btnExportJsonInLabels;
        private System.Windows.Forms.Label labelModifyBox;
        private System.Windows.Forms.ComboBox comboBoxPerson;
        private System.Windows.Forms.ComboBox comboBoxVehicle;
        private System.Windows.Forms.ComboBox comboBoxEvent;

        // Timer
        private System.Windows.Forms.Timer timerPlayback;

        // ToolTip (USAB-01)
        private System.Windows.Forms.ToolTip toolTipMain;
    }
}
