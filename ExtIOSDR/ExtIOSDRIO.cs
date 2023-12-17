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
    public delegate void RunningStateChangedDelegate(bool running);
    public delegate void LibraryChangedDelegate(bool open);

    public class ExtIOSDRIO : IExtIOSDRControllerDialogFacilitator, IFrontendController, IDisposable, ISampleRateChangeSource, IControlAwareObject, IFloatingConfigDialogProvider, IIQStreamController, ITunableSource /*, IFrontendOffset, ISpectrumProvider*/
    {
        private const string _displayName = "ExtIOSDR";
        //private bool _showingLibrarySettingGUI = false;
        private bool _opened = false;
        public event RunningStateChangedDelegate RunningStateChanged;
        public event LibraryChangedDelegate LibraryChanged;
        public event EventHandler SampleRateChanged;
        //public event EventHandler TuneChanged;
        //public event EventHandler LOFrequencyChanged;
        //public event EventHandler LOFrequencyChangedAccepted;
        private long _minFrequency = 0;
        private long _maxFrequency = 0;
        private long _editableMinFrequency = 0;
        private long _editableMaxFrequency = 0;

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

        public bool FrequenciesEditable { get; private set; } = false;

        private bool _restartable = false;
        public bool Restartable {
            get
            {
                return _restartable;
            }
            set
            {
                if(_restartable != value)
                {
                    _restartable = value;
                    SaveSettings();
                }
            }
        }

        private bool _running = false;
        private bool Running
        {
            get
            {
                return _running;
            }
            set
            {
                if(value != _running)
                {
                    _running = value;
                    RunningStateChanged?.Invoke(_running);
                }
            }
        }

        private ExtIOSDRControllerDialog GUI { get; }

        public void Open()
        {
            if (LibraryInUse.Length == 0 || _opened) return;

            ExtIO.UseLibrary(LibraryInUse);
            ExtIO.OpenHW();
            GetFrequencyRange();
            GetAGCs();
            ExtIO.SampleRateChanged += ExtIO_SampleRateChanged;
            ///ExtIO.TuneChanged += ExtIO_TuneChanged;
            //ExtIO.LOFreqChanged += ExtIO_LOFrequencyChanged;
            //ExtIO.LOFreqChangedAccepted += ExtIO_LOFrequencyChangedAccepted;
            LibraryChanged?.Invoke(true);
            _opened = true;
        }

        public void Close()
        {
            if (!_opened) return;

            LibraryChanged?.Invoke(false);
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
            Running = true;
        }

        public void Stop()
        {
            if (!_opened) return;

            Running = false;
            ExtIO.StopHW();
            ExtIO.SamplesAvailable = null;
        }

        private void Restart()
        {
            if (!_opened || !Restartable) return;

            /*SamplesAvailableDelegate saveCallback = null;
            if (Running)
            {
                saveCallback = ExtIO.SamplesAvailable;
                Stop();
            }
            Close();
            Open();
            if (saveCallback != null) Start(saveCallback);*/

            if (Running)
            {
                if (SampleRateChanged != null) {
                    MessageBox.Show("Restart", "Samplerate changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SampleRateChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }


        private bool IsSoundCardBased
        {
            get { return ExtIO.HWType == ExtIO.HWTypes.Soundcard; }
        }

        void IFloatingConfigDialogProvider.ShowSettingGUI(IWin32Window parent)
        {
            if (GUI.IsDisposed)
                return;

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
            _restartable = Utils.GetBooleanSetting("ExtIORestartable", false);
            _editableMinFrequency = Utils.GetLongSetting("ExtIOEditableMinFrequency", 0);
            _editableMaxFrequency = Utils.GetLongSetting("ExtIOEditableMaxFrequency", 0);
        }

        public void SaveSettings()
        {
            Utils.SaveSetting("ExtIODLLSelected", LibraryInUse);
            Utils.SaveSetting("ExtIORestartable", _restartable);
            Utils.SaveSetting("ExtIOEditableMinFrequency", _editableMinFrequency);
            Utils.SaveSetting("ExtIOEditableMaxFrequency", _editableMaxFrequency);
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
            //SampleRateChanged?.Invoke(this, EventArgs.Empty);
            if(SampleRateChanged != null)
            {
                MessageBox.Show("To: " + newSamplerate.ToString(), "Samplerate changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SampleRateChanged.Invoke(this, EventArgs.Empty);
            }
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
            set
            {
                if (FrequenciesEditable && _editableMinFrequency != value)
                {
                    _minFrequency = value;
                    _editableMinFrequency = value;
                    SaveSettings();
                    Restart();
                }

            }
        }

        public long MaximumTunableFrequency
        {
            get
            {
                return _maxFrequency;
            }
            set
            {
                if (FrequenciesEditable && _editableMaxFrequency != value)
                {
                    _maxFrequency = value;
                    _editableMaxFrequency = value;
                    SaveSettings();
                    Restart();
                }

            }
        }

        private void GetFrequencyRange()
        {
            long freqLow = 0;
            long freqHigh = 0;
            int idx = 0;
            while (ExtIO.GetFreqRanges(idx, out freqLow, out freqHigh) == 0) {
                //TODO
                String info = idx.ToString() + "|" + freqLow.ToString() + "|" + freqHigh.ToString();
                MessageBox.Show(info, "FRange test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                idx++;
            }
            if (idx == 0)
            {
                _minFrequency = _editableMinFrequency;
                _maxFrequency = _editableMaxFrequency;
                FrequenciesEditable = true;
                return;
            }

            _minFrequency = freqLow;
            _maxFrequency = freqHigh;
            FrequenciesEditable = false;
            MessageBox.Show("done", "test", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GetAGCs()
        {
            //TODO
            int idx = 0;
            String name;
            while (ExtIO.GetAGCs(idx, out name) == 0)
            {
                MessageBox.Show(idx.ToString() + ": " + name, "AGC test", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                bool success = ExtIO.SetSrate(value);
                if (success)
                    Restart();
                //else
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

        void IExtIOSDRControllerDialogFacilitator.UseLibrary(String dll)
        {
            LibraryInUse = dll;
            Open();
        }

        void IExtIOSDRControllerDialogFacilitator.StopLibrary()
        {
            Stop();
            Close();
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
