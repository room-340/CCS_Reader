namespace CCS_Reader
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comBox1 = new System.Windows.Forms.ListBox();
            this.comLabel = new System.Windows.Forms.Label();
            this.comBox2 = new System.Windows.Forms.ListBox();
            this.comBox3 = new System.Windows.Forms.ListBox();
            this.comBox4 = new System.Windows.Forms.ListBox();
            this.readButton = new System.Windows.Forms.Button();
            this.fileButton = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.saveBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // comBox1
            // 
            this.comBox1.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comBox1.FormattingEnabled = true;
            this.comBox1.ItemHeight = 15;
            this.comBox1.Location = new System.Drawing.Point(12, 35);
            this.comBox1.Name = "comBox1";
            this.comBox1.Size = new System.Drawing.Size(120, 79);
            this.comBox1.TabIndex = 0;
            this.comBox1.Tag = "0";
            this.comBox1.SelectedIndexChanged += new System.EventHandler(this.comBox_Click);
            // 
            // comLabel
            // 
            this.comLabel.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comLabel.Location = new System.Drawing.Point(12, 9);
            this.comLabel.Name = "comLabel";
            this.comLabel.Size = new System.Drawing.Size(182, 23);
            this.comLabel.TabIndex = 1;
            this.comLabel.Text = "Выберите СОМ порты";
            // 
            // comBox2
            // 
            this.comBox2.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comBox2.FormattingEnabled = true;
            this.comBox2.ItemHeight = 15;
            this.comBox2.Location = new System.Drawing.Point(138, 35);
            this.comBox2.Name = "comBox2";
            this.comBox2.Size = new System.Drawing.Size(120, 79);
            this.comBox2.TabIndex = 4;
            this.comBox2.Tag = "1";
            this.comBox2.SelectedIndexChanged += new System.EventHandler(this.comBox_Click);
            // 
            // comBox3
            // 
            this.comBox3.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comBox3.FormattingEnabled = true;
            this.comBox3.ItemHeight = 15;
            this.comBox3.Location = new System.Drawing.Point(264, 35);
            this.comBox3.Name = "comBox3";
            this.comBox3.Size = new System.Drawing.Size(120, 79);
            this.comBox3.TabIndex = 5;
            this.comBox3.Tag = "2";
            this.comBox3.SelectedIndexChanged += new System.EventHandler(this.comBox_Click);
            // 
            // comBox4
            // 
            this.comBox4.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comBox4.FormattingEnabled = true;
            this.comBox4.ItemHeight = 15;
            this.comBox4.Location = new System.Drawing.Point(388, 35);
            this.comBox4.Name = "comBox4";
            this.comBox4.Size = new System.Drawing.Size(120, 79);
            this.comBox4.TabIndex = 6;
            this.comBox4.Tag = "3";
            this.comBox4.SelectedIndexChanged += new System.EventHandler(this.comBox_Click);
            // 
            // readButton
            // 
            this.readButton.Enabled = false;
            this.readButton.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.readButton.Location = new System.Drawing.Point(12, 121);
            this.readButton.Name = "readButton";
            this.readButton.Size = new System.Drawing.Size(204, 43);
            this.readButton.TabIndex = 7;
            this.readButton.Tag = "0";
            this.readButton.Text = "Начать считывание";
            this.readButton.UseVisualStyleBackColor = true;
            this.readButton.Click += new System.EventHandler(this.readButton_Click);
            // 
            // fileButton
            // 
            this.fileButton.Font = new System.Drawing.Font("Times New Roman", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.fileButton.Location = new System.Drawing.Point(302, 120);
            this.fileButton.Name = "fileButton";
            this.fileButton.Size = new System.Drawing.Size(204, 49);
            this.fileButton.TabIndex = 8;
            this.fileButton.Text = "Выбрать файл для сохранения";
            this.fileButton.UseVisualStyleBackColor = true;
            this.fileButton.Click += new System.EventHandler(this.fileButton_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.Title = "Выберете папку и имя фалов для сохранения";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(302, 207);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(204, 23);
            this.progressBar.TabIndex = 9;
            // 
            // saveBox
            // 
            this.saveBox.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.saveBox.Location = new System.Drawing.Point(302, 175);
            this.saveBox.Name = "saveBox";
            this.saveBox.ReadOnly = true;
            this.saveBox.Size = new System.Drawing.Size(204, 26);
            this.saveBox.TabIndex = 10;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(518, 232);
            this.Controls.Add(this.saveBox);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.fileButton);
            this.Controls.Add(this.readButton);
            this.Controls.Add(this.comBox4);
            this.Controls.Add(this.comBox3);
            this.Controls.Add(this.comBox2);
            this.Controls.Add(this.comLabel);
            this.Controls.Add(this.comBox1);
            this.Name = "Form1";
            this.Text = "CCS Reader";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox comBox1;
        private System.Windows.Forms.Label comLabel;
        private System.Windows.Forms.ListBox comBox2;
        private System.Windows.Forms.ListBox comBox3;
        private System.Windows.Forms.ListBox comBox4;
        private System.Windows.Forms.Button readButton;
        private System.Windows.Forms.Button fileButton;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox saveBox;
    }
}

