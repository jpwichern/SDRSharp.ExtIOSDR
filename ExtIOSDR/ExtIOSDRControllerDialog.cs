using SDRSharp.Common;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

/*
    Written by Jiri Wichern PG8W. 
*/

namespace SDRSharp.ExtIOSDR
{
    public interface IExtIOSDRControllerDialogFacilitator
    {
        void UseLibrary(string dll);
        void StopLibrary();
        String LibraryInUse { get; }
        void SaveSettings();
        bool HasDLLSettingGUI();
        void ShowDLLSettingGUI(IWin32Window parent);
        void HideDLLSettingGUI();
        long MinimumTunableFrequency { get; }
        long MaximumTunableFrequency { get; }
        float[] GetAttenuators();
        int AttenuatorIdx { get; set; }
        double[] GetSamplerates();
        int SamplerateIdx { get; set; }
    }

    public partial class ExtIOSDRControllerDialog : Form
    {
        private readonly IExtIOSDRControllerDialogFacilitator _owner;
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
        }

        private static String floatStringconv(float f)
        {
            return f.ToString();
        }

        private static String doubleStringconv(double d)
        {
            return d.ToString();
        }

        public void OpeningLibrary()
        {
            if (!Initialized)
                return;

            dllConfigButton.Enabled = _owner.HasDLLSettingGUI();
            hwNameLabel.Text = ExtIO.HWName;
            hwModelLabel.Text = ExtIO.HWModel;

            long min = _owner.MinimumTunableFrequency;
            long max = _owner.MaximumTunableFrequency;
            freqRangeLabel.Text = min.ToString() + " - " + max.ToString() + " Hz";
            attenuatorsComboBox.Items.Clear();
            float[] attenuators = _owner.GetAttenuators();
            if (attenuators.Length > 0)
            {
                attenuationLabel.Visible = true;
                attenuatorsComboBox.Visible = true;
                attenuatorsComboBox.Items.AddRange(Array.ConvertAll(attenuators, new Converter<float, String>(floatStringconv)));
                attenuatorsComboBox.SelectedIndex = _owner.AttenuatorIdx;
            }
            sampleratesComboBox.Items.Clear();
            double[] samplerates = _owner.GetSamplerates();
            if (samplerates.Length > 0)
            {
                samplerateLabel.Visible = true;
                sampleratesComboBox.Visible = true;
                sampleratesComboBox.Items.AddRange(Array.ConvertAll(samplerates, new Converter<double, String>(doubleStringconv)));
                sampleratesComboBox.SelectedIndex = _owner.SamplerateIdx;
            }
        }

        public void ClosingLibrary()
        {
            _owner.HideDLLSettingGUI();
            dllConfigButton.Enabled = false;
            hwNameLabel.Text = "";
            hwModelLabel.Text = "";
            freqRangeLabel.Text = "";
            attenuationLabel.Visible = false;
            attenuatorsComboBox.Visible = false;
            samplerateLabel.Visible = false;
            sampleratesComboBox.Visible = false;
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


        private void DLLComboBox_SelectedIndexChanged(object sender, EventArgs e)
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
                    //Close previous library
                    _owner.StopLibrary();
                    //Open new library
                    _owner.UseLibrary(dll);
                    //Save settings
                    _owner.SaveSettings();
                }
                catch (Exception ex)
                {
                    dllComboBox.SelectedIndex = -1;
                    MessageBox.Show(this, ex.Message, "Error selecting ExtIO SDR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AttenuatorsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Initialized)
                return;

            var attenuationIndex = attenuatorsComboBox.SelectedIndex;
            if(attenuationIndex >= 0)
                _owner.AttenuatorIdx = attenuationIndex;
            attenuatorsComboBox.SelectedIndex = _owner.AttenuatorIdx;
        }

        private void attenuatorsComboBox_Click(object sender, EventArgs e)
        {
            if(attenuatorsComboBox.SelectedIndex == -1)
                attenuatorsComboBox.SelectedIndex = _owner.AttenuatorIdx;
        }

        private void SampleratesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Initialized)
                return;

            var samplerateIndex = sampleratesComboBox.SelectedIndex;
            _owner.SamplerateIdx = samplerateIndex;
            sampleratesComboBox.SelectedIndex = _owner.SamplerateIdx;
        }

        public ISharpControl Control
        {
            get;
            set;
        }


    }
}
