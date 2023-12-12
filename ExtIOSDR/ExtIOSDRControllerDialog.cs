using SDRSharp.Common;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace SDRSharp.ExtIOSDR
{
    public partial class ExtIOSDRControllerDialog : Form
    {
        private readonly ExtIOSDRIO _owner;
        private bool _initialized;

        public ExtIOSDRControllerDialog(ExtIOSDRIO owner)
        {
            InitializeComponent();

            _owner = owner;
            SetDLLs();

            _initialized = true;
        }

        private bool Initialized
        {
            get
            {
                return _initialized /*&& _owner.Device != null*/;
            }
        }

        private void SetDLLs()
        {
            var dlls = Directory.GetFiles(".", "ExtIO_*.dll");
            dllComboBox.Items.Clear();
            dllComboBox.Items.AddRange(dlls);
            dllComboBox.SelectedIndex = dllComboBox.FindString(_owner.LibraryInUse);
            dllConfigButton.Enabled = _owner.HasDLLSettingGUI();
        }

        public void ConfigureGUI()
        {
            if (!Initialized)
                return;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void DLLRefreshButton_Click(object sender, EventArgs e)
        {
            if (!Initialized)
                return;
            SetDLLs();
        }

        private void DLLConfigButton_Click(object sender, EventArgs e)
        {
            if (!Initialized)
                return;
            _owner.ShowDLLSettingGUI(this);
        }

        private void ExtIOSDRControllerDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }


        private void dllComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Initialized)
                return;

            hwNameLabel.Text = "";
            hwModelLabel.Text = "";
            var dll = (string) dllComboBox.SelectedItem;
            if (dll != null)
            {
                try
                {
                    _owner.Stop();
                    _owner.HideDLLSettingGUI();
                    _owner.UseLibrary(dll);
                    _owner.SaveSettings();
                    dllConfigButton.Enabled = _owner.HasDLLSettingGUI();
                    hwNameLabel.Text = ExtIO.HWName;
                    hwModelLabel.Text = ExtIO.HWModel;
                }
                catch (Exception ex)
                {
                    dllComboBox.SelectedIndex = -1;
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        public ISharpControl Control
        {
            get;
            set;
        }
    }
}
