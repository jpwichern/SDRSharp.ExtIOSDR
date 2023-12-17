using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SDRSharp.Common;
using SDRSharp.Radio;

/*
    Written by Jiri Wichern PG8W.
*/

namespace SDRSharp.ExtIOSDR
{
    public class ExtIOSDRIO : IExtIOSDRControllerDialogFacilitator, IFrontendController, IDisposable, ISampleRateChangeSource, IControlAwareObject, IFloatingConfigDialogProvider, IIQStreamController, ITunableSource /*, IFrontendOffset, ISpectrumProvider*/
    {
        private const string _displayName = "ExtIOSDR";
        //private bool _showingLibrarySettingGUI = false;
        private bool _opened = false;
        public event EventHandler SampleRateChanged;
        //public event EventHandler TuneChanged;
        //public event EventHandler LOFrequencyChanged;
        //public event EventHandler LOFrequencyChangedAccepted;
        private long _minFrequency = 0;
        private long _maxFrequency = 0;

        public ExtIOSDRIO()
        {
            LoadSettings();
            GUI = new ExtIOSDRControllerDialog(this);
        }

        ~ExtIOSDRIO()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
            if (GUI != null)
            {
                GUI.Close();
                GUI.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        public String LibraryInUse { get; private set; }

        private ExtIOSDRControllerDialog GUI { get; }

        public void Open()
        {
            if (LibraryInUse.Length == 0 || _opened) return;

            ExtIO.UseLibrary(LibraryInUse);
            ExtIO.OpenHW();
            GetFrequencyRange();
            ExtIO.SampleRateChanged += ExtIO_SampleRateChanged;
            ///ExtIO.TuneChanged += ExtIO_TuneChanged;
            //ExtIO.LOFreqChanged += ExtIO_LOFrequencyChanged;
            //ExtIO.LOFreqChangedAccepted += ExtIO_LOFrequencyChangedAccepted;
            GUI.OpeningLibrary();
            _opened = true;
        }

        public void Close()
        {
            if (!_opened) return;

            GUI.ClosingLibrary();
            ExtIO.SampleRateChanged -= ExtIO_SampleRateChanged;
            //ExtIO.TuneChanged -= ExtIO_TuneChanged;
            //ExtIO.LOFreqChanged -= ExtIO_LOFrequencyChanged;
            //ExtIO.LOFreqChangedAccepted += ExtIO_LOFrequencyChangedAccepted;
            ExtIO.CloseHW();
            _minFrequency = 0;
            _maxFrequency = 0;
            _opened = false;
        }

        public double Samplerate
        {
            get { return ExtIO.GetHWSR(); }
        }

        public void Start(SamplesAvailableDelegate callback)
        {
            if (!_opened) return;

            ExtIO.SamplesAvailable = callback;
            ExtIO.StartHW(ExtIO.GetHWLO());
        }

        public void Stop()
        {
            if (!_opened) return;

            ExtIO.StopHW();
            ExtIO.SamplesAvailable = null;
        }



        private bool IsSoundCardBased
        {
            get { return ExtIO.HWType == ExtIO.HWTypes.Soundcard; }
        }

        void IFloatingConfigDialogProvider.ShowSettingGUI(IWin32Window parent)
        {
            if (this.GUI.IsDisposed)
                return;
            GUI.OpeningLibrary();
            GUI.Show();
            GUI.Activate();
        }

        void IFloatingConfigDialogProvider.HideSettingGUI()
        {
            if (GUI.IsDisposed)
                return;
            GUI.Hide();
        }

        public void LoadSettings()
        {
            LibraryInUse = Utils.GetStringSetting("ExtIODLLSelected", null);
        }

        public void SaveSettings()
        {
            Utils.SaveSetting("ExtIODLLSelected", LibraryInUse);
        }

        void IControlAwareObject.SetControl(object control)
        {
            GUI.Control = (ISharpControl)control;
        }

        /*private void ExtIOSDRDevice_SampleRateChanged(object sender, EventArgs e)
        {
            SampleRateChanged?.Invoke(this, EventArgs.Empty);
        }*/

        private void ExtIO_SampleRateChanged(int newSamplerate)
        {
            SampleRateChanged?.Invoke(this, EventArgs.Empty);
        }

        /*private void ExtIO_TuneChanged(long tune)
        {
            TuneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ExtIO_LOFrequencyChanged(int newFreq)
        {
            LOFrequencyChanged?.Invoke(this, EventArgs.Empty);
        }*/

        long ITunableSource.Frequency
        {
            get
            {
                return ExtIO.GetHWLO();
            }
            set
            {
                unchecked
                {
                    ExtIO.SetHWLO(unchecked ((int) value));
                }
            }
        }

        bool ITunableSource.CanTune
        {
            get
            {
                return true;
            }
        }

        public long MinimumTunableFrequency
        {
            get
            {
                return _minFrequency;
            }
        }

        public long MaximumTunableFrequency
        {
            get
            {
                return _maxFrequency;
            }
        }

        private void GetFrequencyRange()
        {
            long freqLow = 0;
            long freqHigh = 0;
            int idx = 0;
            while (ExtIO.GetFreqRanges(idx, out freqLow, out freqHigh) == 0) {
                String info = idx.ToString() + "|" + freqLow.ToString() + "|" + freqHigh.ToString();
                MessageBox.Show(info, "FRange test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                idx++;
            }
            //MessageBox.Show("done", "test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _minFrequency = 0;
            _maxFrequency = 1000000000;

            idx = 0;
            String name;
            while (ExtIO.GetAGCs(idx, out name) == 0)
            {
                MessageBox.Show(idx.ToString() + ": " + name, "AGC test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                idx++;
            }
        }

        float[] IExtIOSDRControllerDialogFacilitator.GetAttenuators()
        {
            int idx = 0;
            List<float> attenuators = new List<float>();
            float attn = 0.0f;
            while (ExtIO.GetAttenuators(idx, out attn) == 0)
            {
                attenuators.Add(attn);
                idx++;
            }

            return attenuators.ToArray();
        }

        int IExtIOSDRControllerDialogFacilitator.AttenuatorIdx
        {
            get
            {
                return ExtIO.GetAttenuatorIdx();
            }
            set
            {
                /*bool success =*/ ExtIO.SetAttenuator(value);
                //if(!success)
                //    MessageBox.Show("Failed to set attenuation", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        double[] IExtIOSDRControllerDialogFacilitator.GetSamplerates()
        {
            int idx = 0;
            List<double> rates = new List<double>();
            double rate = 0.0f;
            while (ExtIO.GetSrates(idx, out rate) == 0)
            {
                rates.Add(rate);
                idx++;
            }

            return rates.ToArray();
        }

        int IExtIOSDRControllerDialogFacilitator.SamplerateIdx
        {
            get
            {
                return ExtIO.GetSrateIdx();
            }
            set
            {
                /*bool success =*/ ExtIO.SetSrate(value);
                //if (!success)
                //    MessageBox.Show("Failed to set sample rate", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*private void ExtIO_LOFrequencyChangedAccepted()
        {
            LOFrequencyChangedAccepted?.Invoke(this, EventArgs.Empty);
        }*/

        /*public int Offset
        {
            get
            {
                return ExtIO.GetHWOffset();
            }
            set
            {
                unchecked
                {
                    ExtIO.SetHWOffset(unchecked((int)value));
                }
            }
        }*/

        void IExtIOSDRControllerDialogFacilitator.StopLibrary()
        {
            Stop();
            Close();
        }

        void IExtIOSDRControllerDialogFacilitator.UseLibrary(String dll)
        {
            LibraryInUse = dll;
            Open();
        }

        bool IExtIOSDRControllerDialogFacilitator.HasDLLSettingGUI()
        {
            return ExtIO.HasGUI();
        }

        void IExtIOSDRControllerDialogFacilitator.ShowDLLSettingGUI(IWin32Window parent)
        {
            if (!_opened /*|| _showingLibrarySettingGUI*/) { return; }
            /*_showingLibrarySettingGUI =*/ ExtIO.ShowGUI(parent);
        }

        void IExtIOSDRControllerDialogFacilitator.HideDLLSettingGUI()
        {
            if (!_opened) { return; }
            ExtIO.HideGUI();
            //_showingLibrarySettingGUI = false;
        }
    }
}
