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
        event RunningStateChangedDelegate RunningStateChanged;
        event LibraryChangedDelegate LibraryChanged;
        void UseLibrary(string dll);
        void StopLibrary();
        String LibraryInUse { get; }
        bool FrequenciesEditable { get; }
        bool Restartable { get; set; }
        void SaveSettings();
        bool HasDLLSettingGUI();
        void ShowDLLSettingGUI(IWin32Window parent);
        void HideDLLSettingGUI();
        long MinimumTunableFrequency { get; set; }
        long MaximumTunableFrequency { get; set; }
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
            _owner.RunningStateChanged += ExtIOSDR_RunningStateChanged;
            _owner.LibraryChanged += ExtIOSDR_LibraryChanged;
            SetDLLs();
            RestartCheckBox.Checked = _owner.Restartable;
            _initialized = true;
        }

        private void ExtIOSDR_RunningStateChanged(bool running)
        {

        }

        private void ExtIOSDR_LibraryChanged(bool open)
        {
            if (!Initialized)
                return;

            if (open) OpeningLibrary();
            else ClosingLibrary();
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

        private void OpeningLibrary()
        {
            dllConfigButton.Enabled = _owner.HasDLLSettingGUI();
            hwNameLabel.Text = ExtIO.HWName;
            hwModelLabel.Text = ExtIO.HWModel;

            UpdateFreqRange();
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

        private void ClosingLibrary()
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

        private void UpdateFreqRange()
        {
            
            if (_owner.FrequenciesEditable)
            {
                freqRangeLabel.Text = "Range: ";
                freqRangeLabel2.Visible = true;
                minFreqRangeTextBox.Visible = true;
                minFreqRangeTextBox.Text = _owner.MinimumTunableFrequency.ToString();
                maxFreqRangeTextBox.Visible = true;
                maxFreqRangeTextBox.Text = _owner.MaximumTunableFrequency.ToString();
            }
            else
            {
                freqRangeLabel2.Visible = false;
                minFreqRangeTextBox.Visible = false;
                maxFreqRangeTextBox.Visible = false;
                freqRangeLabel.Text = "Range: " + _owner.MinimumTunableFrequency.ToString() + " - " + _owner.MaximumTunableFrequency.ToString() + " Hz";
            }

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
            if (samplerateIndex >= 0)
                _owner.SamplerateIdx = samplerateIndex;
            sampleratesComboBox.SelectedIndex = _owner.SamplerateIdx;
        }

        public ISharpControl Control
        {
            get;
            set;
        }

        private void restartCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!Initialized)
                return;

            _owner.Restartable = RestartCheckBox.Checked;
        }

        private void minFreqRangeTextBox_TextChanged(object sender, EventArgs e)
        {
            long newMin = 0;
            try
            {
                newMin = long.Parse(minFreqRangeTextBox.Text);
            }
            catch(Exception)
            {
                return;
            }

            if (newMin != _owner.MinimumTunableFrequency)
            {
                _owner.MinimumTunableFrequency = newMin;
            }
        }

        private void maxFreqRangeTextBox_TextChanged(object sender, EventArgs e)
        {
            long newMax = 0;
            try
            {
                newMax = long.Parse(maxFreqRangeTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            if (newMax != _owner.MaximumTunableFrequency)
            {
                _owner.MaximumTunableFrequency = newMax;
            }
        }
    }
}
