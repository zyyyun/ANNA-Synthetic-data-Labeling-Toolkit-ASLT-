namespace ASLTv1.Forms
{
    partial class OnboardingForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelBody;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.CheckBox chkDoNotShowAgain;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelBody = new System.Windows.Forms.Label();
            this.labelProgress = new System.Windows.Forms.Label();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.chkDoNotShowAgain = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();

            // labelTitle
            this.labelTitle.Location = new System.Drawing.Point(20, 20);
            this.labelTitle.Size = new System.Drawing.Size(500, 40);
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.labelTitle.Text = "";
            this.labelTitle.AutoSize = false;
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // labelBody
            this.labelBody.Location = new System.Drawing.Point(20, 70);
            this.labelBody.Size = new System.Drawing.Size(500, 220);
            this.labelBody.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.labelBody.Text = "";
            this.labelBody.AutoSize = false;
            this.labelBody.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // labelProgress
            this.labelProgress.Location = new System.Drawing.Point(20, 305);
            this.labelProgress.Size = new System.Drawing.Size(80, 24);
            this.labelProgress.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.labelProgress.Text = "1 / 3";
            this.labelProgress.AutoSize = false;
            this.labelProgress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // chkDoNotShowAgain
            this.chkDoNotShowAgain.Location = new System.Drawing.Point(20, 345);
            this.chkDoNotShowAgain.Size = new System.Drawing.Size(200, 26);
            this.chkDoNotShowAgain.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkDoNotShowAgain.Text = "다시 보지 않기";

            // btnPrev
            this.btnPrev.Location = new System.Drawing.Point(310, 342);
            this.btnPrev.Size = new System.Drawing.Size(100, 32);
            this.btnPrev.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPrev.Text = "이전";
            this.btnPrev.UseVisualStyleBackColor = true;
            this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);

            // btnNext
            this.btnNext.Location = new System.Drawing.Point(420, 342);
            this.btnNext.Size = new System.Drawing.Size(100, 32);
            this.btnNext.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnNext.Text = "다음";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);

            // OnboardingForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 390);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.labelBody);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.chkDoNotShowAgain);
            this.Controls.Add(this.btnPrev);
            this.Controls.Add(this.btnNext);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Text = "AOLT 시작 가이드";
            this.AcceptButton = this.btnNext;
            this.ResumeLayout(false);
        }
    }
}
