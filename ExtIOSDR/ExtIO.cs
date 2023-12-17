/* ExtIO DLL C# Wrapper for SDR# 
 * -----------------------------
 * 
 * Written by Ian Gilmour (MM6DOS) and Youssef Touil (CN8???)
 * Extended by Jiri Wichern (PG8W)
 * 
 * THIS CODE IS PLACED IN PUBLIC DOMAIN.
 * 
 * 
 * - Provide callback for SamplesAvailable(Complex *samples, int len)
 * - Call UseLibrary("xx_extio.dll")
 * - InitHW() will be called and callback address provided to DLL
 * - Call OpenHW() -> StartHW()
 * - Audio samples will arrive from SamplesAvailable event 
 * 
 * Other events are available.  See ExtIO_StatusEvent enums
 *               
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices;
using SDRSharp.Radio.PortAudio;
using SDRSharp.Radio;
using System.Windows.Forms;

namespace SDRSharp.ExtIOSDR
{
    public delegate void SampleRateChangedDelegate(int newSamplerate);
    public delegate void TuneChangedDelegate(long newTune);
    public delegate void ModeChangedDelegate(char mode);
    //public delegate void IFLimitsChangedDelegate(long low, long high);
    public delegate void FiltChangedDelegate(int loCut, int hiCut, int pitch);
    public delegate void LOFrequencyChangedDelegate(int frequency);
    public delegate void LOFrequencyChangeAcceptedDelegate();
    public delegate void ProhibitLOChangesDelegate();

    public unsafe static class ExtIO
    {
        #region ExtIO Enums

        public enum HWTypes
        {
            Sdr14 = 1,      /* Special case by WinRad ? */
            Aud16BInt = 3, /* 16 Bit integer audio samples */
            Soundcard = 4, /* Soundcard based device */
            Aud24BInt = 5, /* 24 Bit integer audio samples */
            Aud32BInt = 6, /* 32 Bit integer audio samples */
            Aud32BFloat = 7 /* 32 Bit float audio samples */
        }

        public enum StatusEvent
        {
            SrChange = 100, /* Sample rate has changed by hardware */
            LOChange = 101, /* LO has changed by hardware */
            ProhibLO = 102, /* Prohibit LO changes */
            LOChangeOk = 103, /* LO change accepted */
            TuneChange = 105, /* Tune freq changed by hardware */
            ModeChange = 106, /* Demodulator changed by hardware */
            RsqStart = 107, /* Request to start */
            RsqStop = 108, /* Request to stop */
            FiltersChange = 109 /* Filters have been changed by hardware */
        }

        #endregion

        #region Win32 Native Methods

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        #endregion

        #region Events

        /* This delegate is called when samples arrive from the DLL */
        /* Place your own hook here */

        public static SamplesAvailableDelegate SamplesAvailable;
        public static event SampleRateChangedDelegate SampleRateChanged;
        public static event TuneChangedDelegate TuneChanged;
        public static event ModeChangedDelegate ModeChanged;
        //public static event IFLimitsChangedDelegate IFLimitsChanged;
        public static event FiltChangedDelegate FiltersChanged;
        public static event LOFrequencyChangedDelegate LOFreqChanged;
        public static event LOFrequencyChangeAcceptedDelegate LOFreqChangedAccepted;
        public static event ProhibitLOChangesDelegate ProhibitLOChanged;

        #endregion

        #region ExtIO Callback

        /* Note: The calling convention seems to differ for the callback!? */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ExtIOManagedCallbackDelegate(int a, int b, float c, byte* data);

        #endregion ExtIO_Callback

        #region Entry point delegates

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int InitHWDelegate(StringBuilder name, StringBuilder model, out int type);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int OpenHWDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int StartHWDelegate(int freq);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void StopHWDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void CloseHWDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetHWLODelegate(int freq);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetHWLODelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetHWSRDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetTuneDelegate(long tune);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate long GetTuneDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate char GetModeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetModeDelegate(char mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate long SetIFLimitsDelegate(long lowfreq, long highfreq);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetFiltersDelegate(int loCut, int hiCut, int pitch);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GetFiltersDelegate(int* loCut, int* hiCut, int* pitch);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetFreqRangesDelegate(int idx, long* freq_low, long* freq_high);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetAGCDelegate(int idx);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetAGCsDelegate(int idx, char* text);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetAGCIdxDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetAttenuatorDelegate(int idx);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetAttenuatorsDelegate(int idx, float* attenuation);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetAttenuatorIdxDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetSrateDelegate(int idx);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetSratesDelegate(int idx, double* sampleRate);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetSrateIdxDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetStatusDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ShowGUIDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void HideGUIDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SwitchGUIDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetSettingDelegate(int idx, char* value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetSettingDelegate(int idx, char* description, char* value);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetCallbackDelegate(ExtIOManagedCallbackDelegate callbackAddr);

        #endregion

        #region Private fields

        private static InitHWDelegate _initHW;
        private static OpenHWDelegate _openHW;
        private static StartHWDelegate _startHW;
        private static StopHWDelegate _stopHW;
        private static CloseHWDelegate _closeHW;
        private static SetHWLODelegate _setHWLO;
        private static GetHWLODelegate _getHWLO;
        private static GetHWSRDelegate _getHWSR;
        private static SetTuneDelegate _setTune;
        private static GetTuneDelegate _getTune;
        private static GetModeDelegate _getMode;
        private static SetModeDelegate _setMode;
        private static SetIFLimitsDelegate _setIFLimits;
        private static SetFiltersDelegate _setFilters;
        private static GetFiltersDelegate _getFilters;
        private static GetFreqRangesDelegate _getFreqRanges;
        private static SetAGCDelegate _setAGC;
        private static GetAGCsDelegate _getAGCs;
        private static GetAGCIdxDelegate _getAGCIdx;
        private static SetAttenuatorDelegate _setAttenuator;
        private static GetAttenuatorsDelegate _getAttenuators;
        private static GetAttenuatorIdxDelegate _getAttenuatorIdx;
        private static SetSrateDelegate _setSrate;
        private static GetSratesDelegate _getSrates;
        private static GetSrateIdxDelegate _getSrateIdx;
        private static GetStatusDelegate _getStatus;
        private static ShowGUIDelegate _showGUI;
        private static HideGUIDelegate _hideGUI;
        private static SwitchGUIDelegate _switchGUI;
        private static SetSettingDelegate _setSetting;
        private static GetSettingDelegate _getSetting;
        private static SetCallbackDelegate _setCallback;

        private static IntPtr _dllHandle;
        private static HWTypes _hwType;
        private static string _name;
        private static string _model;
        private static UnsafeBuffer _iqBuffer;
        private static Complex* _iqPtr;
        private static int _sampleCount;
        private static bool _isHWStarted;
        private static string _dllName;
        private static readonly Dictionary<string, IntPtr> _handles = new Dictionary<string, IntPtr>();

        private static readonly ExtIOManagedCallbackDelegate _callbackInst = ExtIOCallback;

        #endregion

        #region Initialisation

        static ExtIO()
        {
            GCHandle.Alloc(_callbackInst);
        }

        public static void UseLibrary(string fileName)
        {
            _dllName = fileName;

            if (_handles.ContainsKey(_dllName))
            {
                _dllHandle = _handles[_dllName];
            }
            else
            {
                _dllHandle = LoadLibrary(_dllName);
            }

            if (_dllHandle == IntPtr.Zero)
                throw new Exception("Unable to load ExtIO library");

            _initHW = null;
            _openHW = null;
            _startHW = null;
            _stopHW = null;
            _closeHW = null;
            _setHWLO = null;
            _getHWLO = null;
            _getHWSR = null;
            _setTune = null;
            _getTune = null;
            _getMode = null;
            _setMode = null;
            _setIFLimits = null;
            _setFilters = null;
            _getFilters = null;
            _getFreqRanges = null;
            _setAGC = null;
            _getAGCs = null;
            _getAGCIdx = null;
            _setAttenuator = null;
            _getAttenuators = null;
            _getAttenuatorIdx = null;
            _setSrate = null;
            _getSrates = null;
            _getSrateIdx = null;
            _getStatus = null;
            _showGUI = null;
            _hideGUI = null;
            _switchGUI = null;
            _setCallback = null;

            IntPtr pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "InitHW");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _initHW = (InitHWDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(InitHWDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "OpenHW");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _openHW = (OpenHWDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(OpenHWDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "StartHW");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _startHW = (StartHWDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(StartHWDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "StopHW");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _stopHW = (StopHWDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(StopHWDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "CloseHW");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _closeHW = (CloseHWDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(CloseHWDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "SetCallback");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setCallback = (SetCallbackDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetCallbackDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "SetHWLO");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setHWLO = (SetHWLODelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetHWLODelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetHWLO");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getHWLO = (GetHWLODelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetHWLODelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetHWSR");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getHWSR = (GetHWSRDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetHWSRDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "TuneChanged");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setTune = (SetTuneDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetTuneDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetTune");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getTune = (GetTuneDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetTuneDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetMode");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getMode = (GetModeDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetModeDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "ModeChanged");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setMode = (SetModeDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetModeDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "IFLimitsChanged");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
               _setIFLimits = (SetIFLimitsDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetIFLimitsDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "FiltersChanged");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setFilters = (SetFiltersDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetFiltersDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetFilters");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getFilters = (GetFiltersDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetFiltersDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetFreqRanges");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getFreqRanges = (GetFreqRangesDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetFreqRangesDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "_ExtIoGetAGCs");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getAGCs = (GetAGCsDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetAGCsDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "_ExtIoGetActualAGCidx");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getAGCIdx = (GetAGCIdxDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetAGCIdxDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "_ExtIoSetAGC");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setAGC = (SetAGCDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetAGCDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetAttenuators");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getAttenuators = (GetAttenuatorsDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetAttenuatorsDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetActualAttIdx");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getAttenuatorIdx = (GetAttenuatorIdxDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetAttenuatorIdxDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "SetAttenuator");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setAttenuator = (SetAttenuatorDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetAttenuatorDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "ExtIoGetSrates");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getSrates = (GetSratesDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetSratesDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "ExtIoGetActualSrateIdx");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getSrateIdx = (GetSrateIdxDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetSrateIdxDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "ExtIoSetSrate");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setSrate = (SetSrateDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetSrateDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetStatus");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getStatus = (GetStatusDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetStatusDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "ShowGUI");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _showGUI = (ShowGUIDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(ShowGUIDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "HideGUI");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _hideGUI = (HideGUIDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(HideGUIDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "SwitchGUI");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _switchGUI = (SwitchGUIDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SwitchGUIDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "SetSetting");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _setSetting = (SetSettingDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(SetSettingDelegate));

            pAddressOfFunctionToCall = GetProcAddress(_dllHandle, "GetSetting");
            if (pAddressOfFunctionToCall != IntPtr.Zero)
                _getSetting = (GetSettingDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GetSettingDelegate));

            if (_initHW == null || _openHW == null || _startHW == null || _setHWLO == null ||
               _getStatus == null || _setCallback == null || _stopHW == null || _closeHW == null)
            {
                //FreeLibrary(_dllHandle);
                _dllHandle = IntPtr.Zero;
                throw new ApplicationException("ExtIO DLL is not valid");
            }

            var name = new StringBuilder(256);
            var model = new StringBuilder(256);
            int type;

            var result = _initHW(name, model, out type);

            _name = name.ToString();
            _model = model.ToString();

            if (result < 1)
            {
                //FreeLibrary(_dllHandle);
                _dllHandle = IntPtr.Zero;
                throw new ApplicationException("InitHW() returned " + result);
            }

            _hwType = (HWTypes)type;

            /* Give the library the managed callback address */
            _setCallback(_callbackInst);
        }

        #endregion

        #region ExtIO Methods / Properties

        public static HWTypes HWType
        {
            get { return _hwType; }
        }

        public static bool IsHardwareStarted
        {
            get
            {
                return _isHWStarted;
            }
        }

        public static bool IsHardwareOpen
        {
            get
            {
                return _dllHandle != IntPtr.Zero;
            }
        }

        public static string DllName
        {
            get
            {
                return _dllName;
            }
        }

        public static string HWName
        {
            get
            {
                if (_dllHandle != IntPtr.Zero)
                    return _name;
                return string.Empty;
            }
        }

        public static string HWModel
        {
            get
            {
                if (_dllHandle != IntPtr.Zero)
                    return _model;
                return string.Empty;
            }
        }

        public static int GetHWSR()
        {
            if (_dllHandle != IntPtr.Zero && _getHWSR != null)
                return _getHWSR();
            return 0;
        }

        public static long GetTune()
        {
            if (_dllHandle != IntPtr.Zero && _getTune != null)
                return _getTune();
            return 0;
        }

        public static char GetMode()
        {
            if (_dllHandle != IntPtr.Zero && _getMode != null)
                return _getMode();
            return '\0';
        }

        public static void SetMode(char mode)
        {
            if (_dllHandle != IntPtr.Zero && _setMode != null)
                _setMode(mode);
        }

        public static int GetFreqRanges(int idx, out long freqLow, out long freqHigh)
        {
            freqLow = 0;
            freqHigh = 0;
            if (_dllHandle != IntPtr.Zero && _getFreqRanges != null)
            {
                fixed(long* fl = &freqLow, fh = &freqHigh)
                {
                    return _getFreqRanges(idx, fl, fh);
                }
            }
            return -1;
        }

        public static void GetFilters(int* loCut, int* hiCut, int* pitch)
        {
            if (_dllHandle != IntPtr.Zero && _getFilters != null)
                _getFilters(loCut, hiCut, pitch);
        }

        public static int GetAGCs(int idx, out string name)
        {
            name = "";
            if (_dllHandle != IntPtr.Zero && _getAGCs != null)
            {
                char[] a = new char[16];
                fixed (char* p = a)
                {
                    int ret = _getAGCs(idx, p);
                    name = new string(a);
                    MessageBox.Show(ret.ToString(), "test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ret;
                }
            }
            return -1;
        }

        public static int GetAGCIdx()
        {
            if (_dllHandle != IntPtr.Zero && _getAGCIdx != null)
                return _getAGCIdx();
            return -1;
        }

        public static void SetAGC(int idx)
        {
            if (_dllHandle != IntPtr.Zero & _setAGC != null)
                _setAGC(idx);
        }

        public static int GetAttenuators(int idx, out float attenuation)
        {
            attenuation = 0.0f;
            if (_dllHandle != IntPtr.Zero && _getAttenuators != null)
            {
                fixed (float* a = &attenuation)
                {
                    return _getAttenuators(idx, a);
                }
            }
            return -1;
        }

        public static int GetAttenuatorIdx()
        {
            if (_dllHandle != IntPtr.Zero && _getAttenuatorIdx != null)
                return _getAttenuatorIdx();
            return -1;
        }

        public static bool SetAttenuator(int idx)
        {
            if (_dllHandle != IntPtr.Zero & _setAttenuator != null)
            {
                int ret = _setAttenuator(idx);
                if (ret != 0)
                    MessageBox.Show("Failed to set attenuation", "Error " + ret.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ret == 0;
            }
            MessageBox.Show("Can't set attenuation", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        public static int GetSrates(int idx, out double srate)
        {
            srate = 0.0f;
            if (_dllHandle != IntPtr.Zero && _getSrates != null)
            {
                fixed (double* a = &srate)
                {
                    return _getSrates(idx, a);
                }
            }
            return -1;
        }

        public static int GetSrateIdx()
        {
            if (_dllHandle != IntPtr.Zero && _getSrateIdx != null)
                return _getSrateIdx();
            return -1;
        }

        public static bool SetSrate(int idx)
        {
            if (_dllHandle != IntPtr.Zero & _setSrate != null)
            {
                int ret = _setSrate(idx);
                if(ret != 0)
                    MessageBox.Show("Failed to set sample rate", "Error " + ret.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ret == 0;
            }
            MessageBox.Show("Can't set sample rate", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        public static int GetHWLO()
        {
            int result = 0;
            if (_dllHandle != IntPtr.Zero && _getHWLO != null)
                result = _getHWLO();
            if (result < 0)
                result = 0;
            return result;
        }

        public static void SetHWLO(int freq)
        {
            if (_dllHandle != IntPtr.Zero & _setHWLO != null)
                _setHWLO(freq);
        }

        public static void GetSetting(int idx, string description, out string value)
        {
            value = "";
            if (_dllHandle != IntPtr.Zero && _getSetting != null)
            {
                char[] a1 = description.ToCharArray();
                char[] a2 = new char[16];
                fixed (char* p1 = a1, p2 = a2)
                {
                    _getSetting(idx, p1, p2);
                    value = new string(a2);
                }
            }
        }

        public static void SetSetting(int idx, char* value)
        {
            if (_dllHandle != IntPtr.Zero & _setSetting != null)
                _setSetting(idx, value);
        }

        public static bool HasGUI()
        {
            return _dllHandle != IntPtr.Zero && _showGUI != null;
        }

        public static bool ShowGUI(IWin32Window parent)
        {
            if (HasGUI())
            {
                _showGUI();
                return true;
            }
            else return false;
        }

        public static void HideGUI()
        {
            if (_dllHandle != IntPtr.Zero && _hideGUI != null)
                _hideGUI();
        }

        public static void SwitchGUI()
        {
            if (_dllHandle != IntPtr.Zero && _switchGUI != null)
                _switchGUI();
        }

        public static void StartHW(int freq)
        {
            if (_dllHandle == IntPtr.Zero || _startHW == null)
                return;

            _iqBuffer = null;
            _iqPtr = null;

            int result = _startHW(freq);
            if (result < 0)
                throw new Exception("ExtIO StartHW() returned " + result);

            _isHWStarted = true;
            _sampleCount = result;

            /* Allocate the sample buffers */
            /* We must do it here since we do not know the size until the hardware is started! */
            _iqBuffer = UnsafeBuffer.Create(_sampleCount, sizeof(Complex));
            _iqPtr = (Complex*)_iqBuffer;
        }

        public static int OpenHW()
        {
            if (_dllHandle != IntPtr.Zero && !_isHWStarted)
                return _openHW();
            return 0;
        }

        public static void StopHW()
        {
            if (_dllHandle != IntPtr.Zero && _isHWStarted)
            {
                _stopHW();
                _isHWStarted = false;
            }
        }

        public static void CloseHW()
        {
            if (_dllHandle != IntPtr.Zero && _closeHW != null)
            {
                _closeHW();
                _isHWStarted = false;
                _dllHandle = IntPtr.Zero;
            }
        }

        #endregion

        #region ExtIO Callback

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void ExtIOCallback(int count, int status, float iqOffs, byte* dataPtr)
        {
            /* Non-negative count means samples are ready. */           
            /* Negative count means status change */
            if (count >= 0 && _isHWStarted)
            {
                /* Buffers cannot be allocated until AFTER the hardware is started because size is unknown
                 * Therefore the callback could be called before buffers are allocated
                 */
                if (_iqPtr == null)
                {
                    return;
                }
                
                /* Convert samples to double */

                var len = _iqBuffer.Length;

                /* 16 bit integer samples */
                if (_hwType == HWTypes.Aud16BInt || _hwType == HWTypes.Sdr14)
                {
                    const float scale = 1.0f / 32767.0f;
                    var input = (Int16*) dataPtr;
                    for (var i = 0; i < len; i++)
                    {
                        _iqPtr[i].Imag = *input++ * scale;
                        _iqPtr[i].Real = *input++ * scale;
                    }
                }

                /* 24 bit integer samples */
                else if (_hwType == HWTypes.Aud24BInt)
                {
                    const float scale = 1.0f / 8388607.0f;
                    var input = (Int24*) dataPtr;
                    for (var i = 0; i < len; i++)
                    {
                        _iqPtr[i].Imag = *input++ * scale;
                        _iqPtr[i].Real = *input++ * scale;
                    }
                }

                /* 32 bit integer samples */
                else if (_hwType == HWTypes.Aud32BInt)
                {
                    const float scale = 1.0f / 2147483647.0f;
                    var input = (Int32*) dataPtr;
                    for (var i = 0; i < len; i++)
                    {
                        _iqPtr[i].Imag = *input++ * scale;
                        _iqPtr[i].Real = *input++ * scale;
                    }
                }

                /* 32 bit float samples */
                else if (_hwType == HWTypes.Aud32BFloat)
                {
                    var input = (float*) dataPtr;
                    for (var i = 0; i < len; i++)
                    {
                        _iqPtr[i].Imag = *input++;
                        _iqPtr[i].Real = *input++;
                    }
                }

                if (SamplesAvailable != null)
                {
                    SamplesAvailable(null, _iqPtr, len);
                }
            }
            else
            {
                /* Handle ExtIO status events. */
                /* Only the interesting ones for now */
                switch ((StatusEvent) status)
                {
                    case StatusEvent.LOChange:
                        if (LOFreqChanged != null)
                            LOFreqChanged(GetHWLO());
                        break;

                    case StatusEvent.LOChangeOk:
                        if (LOFreqChangedAccepted != null)
                            LOFreqChangedAccepted();
                        break;

                    case StatusEvent.SrChange:
                        if (SampleRateChanged != null)
                            SampleRateChanged(GetHWSR());
                        break;

                    case StatusEvent.TuneChange:
                        if (TuneChanged != null)
                            TuneChanged(GetTune());
                        break;

                    case StatusEvent.ModeChange:
                        if (ModeChanged != null)
                            ModeChanged(GetMode());
                        break;

                    case StatusEvent.FiltersChange:
                        if (FiltersChanged != null)
                        {
                            int loCut = 0;
                            int hiCut = 0;
                            int pitch = 0;
                            GetFilters(&loCut, &hiCut, &pitch);

                            FiltersChanged(loCut, hiCut, pitch);
                        }
                        break;

                    case StatusEvent.ProhibLO:
                        if (ProhibitLOChanged != null)
                            ProhibitLOChanged();
                        break;
                }
            }
        }
    
        #endregion
    }
}
