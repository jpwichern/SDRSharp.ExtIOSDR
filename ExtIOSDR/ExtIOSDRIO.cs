﻿using System;
using System.Windows.Forms;
using System.ComponentModel;

using SDRSharp.Common;
using SDRSharp.Radio;

namespace SDRSharp.ExtIOSDR
{
    public unsafe class ExtIOSDRIO : IFrontendController, IDisposable, ISampleRateChangeSource, IControlAwareObject, IFloatingConfigDialogProvider /*, ISpectrumProvider, ITunableSource, IIQStreamController*/
    {
        private const string _displayName = "USRP";
        private readonly ExtIOSDRControllerDialog _gui;
        private string _filename = null;
        private int _lastDLLSelected = -1;
        private bool _showingDLLSettingGUI = false;
        public event EventHandler SampleRateChanged;

        public ExtIOSDRIO()
        {
            LoadSettings();
            _gui = new ExtIOSDRControllerDialog(this);
        }

        ~ExtIOSDRIO()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
            if (_gui != null)
            {
                _gui.Close();
                _gui.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        public void UseLibrary(string filename)
        {
            _filename = filename;
            ExtIO.UseLibrary(filename);
            ExtIO.OpenHW();
        }

        public ExtIOSDRControllerDialog GUI
        {
            get
            {
                return _gui;
            }
        }

        public int LastDLLSelected => _lastDLLSelected;

        public void Open()
        {
            ExtIO.UseLibrary(_filename);
        }

        public void Start(SamplesAvailableDelegate callback)
        {
            ExtIO.SamplesAvailable = callback;
            ExtIO.StartHW(ExtIO.GetHWLO());
        }

        public void Stop()
        {
            ExtIO.StopHW();
            ExtIO.SamplesAvailable = null;
        }

        public void Close()
        {
            if(_showingDLLSettingGUI) HideDLLSettingGUI();
            //ExtIO.CloseHW();
        }

        public bool IsSoundCardBased
        {
            get { return ExtIO.HWType == ExtIO.HWTypes.Soundcard; }
        }

        public string SoundCardHint
        {
            get { return string.Empty; }
        }

        public void ShowSettingGUI(IWin32Window parent)
        {
            if (this._gui.IsDisposed)
                return;
            _gui.Show();
            _gui.Activate();
        }

        public void HideSettingGUI()
        {
            if (_gui.IsDisposed)
                return;
            _gui.Hide();
        }

        public void LoadSettings()
        {
            _lastDLLSelected = Utils.GetIntSetting("ExtIODLLSelected", -1);
        }

        public void SaveSettings()
        {
            Utils.SaveSetting("ExtIODLLSelected", _lastDLLSelected);
        }

        public void SetControl(object control)
        {
            this._gui.Control = (ISharpControl)control;
        }

        private void ExtIOSDRDevice_SampleRateChanged(object sender, EventArgs e)
        {
            SampleRateChanged?.Invoke(this, EventArgs.Empty);
        }

        public double Samplerate
        {
            get { return ExtIO.GetHWSR(); }
        }

        public long Frequency
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

        /*bool ITunableSource.CanTune
        {
            get
            {
                return true;
            }
        }

        long ITunableSource.MinimumTunableFrequency
        {
            get
            {
                return ExtIO.MinFrequency;
            }
        }
        long ITunableSource.MaximumTunableFrequency
        {
            get
            {
                return ExtIO.MaxFrequency;
            }
        }*/

        public bool HasDLLSettingGUI()
        {
            return ExtIO.HasGUI();
        }

        public void ShowDLLSettingGUI(IWin32Window parent)
        {
            if (_showingDLLSettingGUI) { return; }
            _showingDLLSettingGUI = ExtIO.ShowGUI(parent);
        }

        public void HideDLLSettingGUI()
        {
            ExtIO.HideGUI();
            _showingDLLSettingGUI = false;
        }
    }
}