using System;

/*
    Written by Jiri Wichern PG8W.
    Used RTLSDR plugin dialog box as a template.
*/

namespace SDRSharp.ExtIOSDR
{
    partial class ExtIOSDRControllerDialog
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
            this.closeButton = new System.Windows.Forms.Button();
            this.dllComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dllRefreshButton = new System.Windows.Forms.Button();
            this.dllConfigButton = new System.Windows.Forms.Button();
            this.hwNameLabel = new System.Windows.Forms.Label();
            this.hwModelLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // closeButton
            // 
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(245, 377);
            this.closeButton.Margin = new System.Windows.Forms.Padding(4);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(100, 28);
            this.closeButton.TabIndex = 8;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // dllComboBox
            // 
            this.dllComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dllComboBox.FormattingEnabled = true;
            this.dllComboBox.Location = new System.Drawing.Point(16, 32);
            this.dllComboBox.Margin = new System.Windows.Forms.Padding(4);
            this.dllComboBox.Name = "dllComboBox";
            this.dllComboBox.Size = new System.Drawing.Size(328, 24);
            this.dllComboBox.TabIndex = 0;
            this.dllComboBox.SelectedIndexChanged += new System.EventHandler(this.dllComboBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 11);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 16);
            this.label1.TabIndex = 20;
            this.label1.Text = "ExtIO DLL";
            // 
            // dllRefreshButton
            // 
            this.dllRefreshButton.Location = new System.Drawing.Point(16, 59);
            this.dllRefreshButton.Margin = new System.Windows.Forms.Padding(4);
            this.dllRefreshButton.Name = "dllRefreshButton";
            this.dllRefreshButton.Size = new System.Drawing.Size(100, 28);
            this.dllRefreshButton.TabIndex = 8;
            this.dllRefreshButton.Text = "Refresh";
            this.dllRefreshButton.UseVisualStyleBackColor = true;
            this.dllRefreshButton.Click += new System.EventHandler(this.DLLRefreshButton_Click);
            // 
            // dllConfigButton
            // 
            this.dllConfigButton.Location = new System.Drawing.Point(245, 59);
            this.dllConfigButton.Margin = new System.Windows.Forms.Padding(4);
            this.dllConfigButton.Name = "dllConfigButton";
            this.dllConfigButton.Size = new System.Drawing.Size(100, 28);
            this.dllConfigButton.TabIndex = 8;
            this.dllConfigButton.Text = "Configure";
            this.dllConfigButton.UseVisualStyleBackColor = true;
            this.dllConfigButton.Click += new System.EventHandler(this.DLLConfigButton_Click);
            // 
            // hwNameLabel
            // 
            this.hwNameLabel.AutoSize = true;
            this.hwNameLabel.Location = new System.Drawing.Point(16, 100);
            this.hwNameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.hwNameLabel.Name = "hwNameLabel";
            this.hwNameLabel.Size = new System.Drawing.Size(65, 16);
            this.hwNameLabel.TabIndex = 20;
            this.hwNameLabel.Text = "";
            // 
            // hwModelLabel
            // 
            this.hwModelLabel.AutoSize = true;
            this.hwModelLabel.Location = new System.Drawing.Point(16, 132);
            this.hwModelLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.hwModelLabel.Name = "hwNameLabel";
            this.hwModelLabel.Size = new System.Drawing.Size(65, 16);
            this.hwModelLabel.TabIndex = 20;
            this.hwModelLabel.Text = "";
            // 
            // ExtIOSDRControllerDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(361, 421);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dllComboBox);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.dllRefreshButton);
            this.Controls.Add(this.dllConfigButton);
            this.Controls.Add(this.hwNameLabel);
            this.Controls.Add(this.hwModelLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExtIOSDRControllerDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Ext IO Controller";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ExtIOSDRControllerDialog_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.ComboBox dllComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button dllRefreshButton;
        private System.Windows.Forms.Button dllConfigButton;
        private System.Windows.Forms.Label hwNameLabel;
        private System.Windows.Forms.Label hwModelLabel;
    }
}

