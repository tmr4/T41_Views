using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using T41_Control;
using Microsoft.UI.Dispatching;
using T41_UI.Views;
using Microsoft.UI.Xaml;

namespace T41_Radio;

public class T41 : INotifyPropertyChanged {
  public event PropertyChangedEventHandler? PropertyChanged;
  private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  // Supported modes and bands
  // {SSB_MODE, CW_MODE, DATA_MODE}
  // {DEMOD_USB, DEMOD_LSB, DEMOD_AM, DEMOD_NFM, DEMOD_PSK31, DEMOD_PSK31_WAV, DEMOD_FT8, DEMOD_FT8_WAV, DEMOD_SAM}
  // {BAND_80M, BAND_40M, BAND_20M, BAND_17M, BAND_15M, BAND_12M, BAND_10M }

  private MainPage mainPage;
  private T41Control t41Ctl;

  private bool resetCenterFreqFlag = false;
  //private bool initializedFlag = false;

  // T41 data
  private int activeVFO = 0; // VFO A
  private bool centerTuneActive = false;
  private int freqIndex = 6;
  private int ftIndex = 3;
  private int freqIncrement = 100000;
  private int ftIncrement = 500;
  //private long TxRxFreq = 7048000;
  private long centerFreq = 7048000;
  private long currentFreqA = 7048000;
  private long currentFreqB = 7030000;
  private int audioVolume = 30;
  private int currentDemod = 1; // DEMOD_LSB
  private int xmtMode = 0; // SSB_MODE;
  private int currentBand = 1; // BAND_40M;
  //private int currentBandA = 1; //BAND_40M;
  //private int currentBandB = 1; //BAND_40M;
  private int currentNF = 0;
  private int agcMode = 1;
  private int liveNoiseFloorFlag = 0;
  private int transmitPowerLevel = 1;
  //private int fLoCut = -200;
  //private int fHiCut = -3000;
  private bool dataFlag = false;
  private int ncoFreq = 0;

  public T41(MainPage mp) {
    t41Ctl = new T41Control(this, mp, DispatcherQueue.GetForCurrentThread());
    mainPage = mp;

    DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
    timer.Tick += (s, e) => NotifyPropertyChanged(nameof(Clock));
    timer.Start();
  }

  private int ValidateAGC(int band) {
    if(band > 4) return 0;
    if(band < 0) return 4;
    return band;
  }
  public int AGCMode {
    get { return agcMode; }
    set { agcMode = ValidateAGC(value); NotifyPropertyChanged(); }
  }
  public int CurrentNF {
    get { return currentNF; }
    set { currentNF = value; NotifyPropertyChanged(); }
  }
  public int LiveNoiseFloorFlag {
    get { return liveNoiseFloorFlag; }
    set { liveNoiseFloorFlag = value; NotifyPropertyChanged(); }
  }
  public bool DataFlag { // true = T41 flagged to send spectrum data periodically
    get { return dataFlag; }
    set { dataFlag = value; NotifyPropertyChanged(); }
  }
  public int FLoCut { get; set; } = -200;
  public int FHiCut { get; set; } = -3000;
  // *** TODO: need to account for mode in bandwidth bar display

  private double dbm = 0;
  private int smeter = 2;
  public int SMeter {
    get { return smeter; }
    set { smeter = value; NotifyPropertyChanged(); }
  }
  public string Dbm {
    get { return dbm.ToString("F1"); }
    set { NotifyPropertyChanged(); }
  }

  //private DateTime time = new DateTime();
  //public string timeString = "";
  public string Clock => "PST: " + DateTime.Now.ToString("H:mm:ss");


  public bool CenterTuneActive {
    get { return centerTuneActive; }
    set { centerTuneActive = value; NotifyPropertyChanged(); }
  }

  int[] selectFreq = { 10, 50, 100, 250, 1000, 10000, 100000, 1000000 };
  private int ValidateFreqIndex(int index) {
    if (index > 7) return 0;
    if (index < 0) return 7;
    return index;
  }
  public int FreqIndex {
    get { return freqIndex; }
    set {
      freqIndex = ValidateFreqIndex(value);
      NotifyPropertyChanged();
      FreqIncrement = selectFreq[freqIndex];
    }
  }
  public int FreqIncrement {
    get { return freqIncrement; }
    set { freqIncrement = value; NotifyPropertyChanged(); }
  }

  private int[] selectFT = { 10, 50, 250, 500 };
  private int ValidateFtIndex(int index) {
    if (index > 3)return 0;
    if (index < 0) return 3;
    return index;
  }
  public int FtIndex {
    get { return ftIndex; }
    set {
      ftIndex = ValidateFtIndex(value);
      NotifyPropertyChanged();
      FtIncrement = selectFT[ftIndex];
    }
  }
  public int FtIncrement {
    get { return ftIncrement; }
    set { ftIncrement = value; NotifyPropertyChanged(); }
  }

  private int ValidateActiveVFO(int vfo) {
    if(vfo < 0) return 1;
    if(vfo > 1) return 0;
    return vfo;
  }
  public int ActiveVFO {
    get { return activeVFO; }
    set {
      //activeVFO = value == 1 ? 0 : 1; // this toggles activeVFO
      activeVFO = ValidateActiveVFO(value);

      NCOFreq = 0;
      if(activeVFO == 1) {
        //centerFreq = TxRxFreq = currentFreqB;
        centerFreq = currentFreqB;
      } else {
        //centerFreq = TxRxFreq = currentFreqA;
        centerFreq = currentFreqA;
      }

      resetCenterFreqFlag = true; // flag callback that a reset is needed
      NotifyPropertyChanged();
    }
  }

  public long CurrentFreqA {
    get { return currentFreqA; }
    set { currentFreqA = value; NotifyPropertyChanged(); }
  }
  public long CurrentFreqB {
    get { return currentFreqB; }
    set { currentFreqB = value; NotifyPropertyChanged(); }
  }
  public int NCOFreq {
    get { return ncoFreq; }
    set {
      ncoFreq = value;
      NotifyPropertyChanged();
      NotifyPropertyChanged(nameof(BwLeft));
      NotifyPropertyChanged(nameof(BwLine));
    }
  }
  public long CenterFreq {
    get { return centerFreq; }
    set { centerFreq = value; NotifyPropertyChanged(); }
  }

  public int AudioVolume {
    get { return audioVolume; }
    set { audioVolume = value; NotifyPropertyChanged(); }
  }

  public bool BandChangeUp { get; private set; }
  private int ValidateBand(int band) {
    if(band > 6) return 0;
    if(band < 0) return 6;
    return band;
  }
  public int CurrentBand {
    get { return currentBand; }
    set {
      BandChangeUp = value > currentBand;
      currentBand = ValidateBand(value);
      resetCenterFreqFlag = true; // flag callback that a reset is needed
      NotifyPropertyChanged(); }
  }

  private int ValidateMode(int mode) {
    if(mode > 2) return 0;
    if(mode < 0) return 2;
    return mode;
  }
  public int XmtMode {
    get { return xmtMode; }
    set { xmtMode = ValidateMode(value); NotifyPropertyChanged(); }
  }

  private int ValidateDemod(int demod) {
    if(demod > 8) return 0;
    if(demod < 0) return 8;
    return demod;
  }
  public int CurrentDemod {
    get { return currentDemod; }
    set {
      currentDemod = ValidateDemod(value);
      NotifyPropertyChanged();
      NotifyPropertyChanged(nameof(BwLeft));
      NotifyPropertyChanged(nameof(BwValueLeft));
      NotifyPropertyChanged(nameof(BwValueRight));
    }
  }

  private int ValidatePwr(int pwr) {
    if(pwr < 1) return 1;
    if(pwr > 20) return 20;
    return pwr;
  }
  public int TransmitPowerLevel {
    get { return transmitPowerLevel; }
    set { transmitPowerLevel = ValidatePwr(value); NotifyPropertyChanged(); }
  }

  public void ResetTuning() {
    CenterFreq += NCOFreq;
    NCOFreq = 0;

    //DrawBandwidthBar();
  }

  // zero out 1000s part of VFO A frequency
  public void ZeroFreqA() {
    if (activeVFO == 0) {
      //int delta = currentFreqA % 1000;
      CurrentFreqA -= currentFreqA % 1000;
      CenterFreq = currentFreqA;
    }
  }
  public void IncFreqA(int change) {
    if (activeVFO == 0) {
      if(centerTuneActive) {
        CurrentFreqA += FreqIncrement * change;
        CenterFreq = CurrentFreqA;
      } else {
        NCOFreq += FtIncrement * change;
        CurrentFreqA = CenterFreq + NCOFreq;
      }
    }
  }

  // zero out 1000s part of VFO B frequency
  public void ZeroFreqB() {
    if (activeVFO == 1) {
      //int delta = currentFreqB % 1000;
      CurrentFreqB -= currentFreqB % 1000;
      CenterFreq = currentFreqB;
    }
  }
  public void IncFreqB(int change) {
    if (activeVFO == 1) {
      if(centerTuneActive) {
        CurrentFreqB += FreqIncrement * change;
        CenterFreq = CurrentFreqB;
      } else {
        NCOFreq += FtIncrement * change;
        CurrentFreqB = CenterFreq + NCOFreq;
      }
    }
  }

  // Update properties from IFData
  // using properties to keep both the display and T41 updated results
  // in some churn here; it's reduced by only updating changed values
  // and on startup by setting default values the same as on the T41
  public void SetState(IFData data) {
    if(activeVFO != data.activeVFO) {
      ActiveVFO = data.activeVFO;
    }
    if (activeVFO == 0) {
      if(currentFreqA != data.TxRxFreq) {
        CurrentFreqA = CenterFreq = data.TxRxFreq;
        CurrentFreqB = data.inactiveVFOFreq;
      }
    } else {
      if(currentFreqB != data.TxRxFreq) {
        CurrentFreqB = CenterFreq = data.TxRxFreq;
        CurrentFreqA = data.inactiveVFOFreq;
      }
    }
    NCOFreq = 0;
    if(currentBand != data.currentBand) {
      CurrentBand = data.currentBand;
    }
    if(xmtMode != data.xmtMode) {
      XmtMode = data.xmtMode;
    }
    if(currentDemod != data.mode) {
      CurrentDemod = data.mode;
    }
    if(liveNoiseFloorFlag != data.liveNoiseFloorFlag) {
      LiveNoiseFloorFlag = data.liveNoiseFloorFlag;
    }
    if(currentNF != data.currentNoiseFloor) {
      // the current noise floor property isn't used directly on screen
      // so we can just update the private field.  This prevents sending
      // this back to the T41 via t41Ctl.
      currentNF = data.currentNoiseFloor;
    }
    if(agcMode != data.AGCMode) {
      AGCMode = data.AGCMode;
    }
    mainPage.SetActiveVFO(activeVFO);
  }

  public void SetFreqA(long freq) {
    CurrentFreqA = freq;
    if(resetCenterFreqFlag) {
      CenterFreq = freq;
      NCOFreq = 0;
      resetCenterFreqFlag = false;
    }
  }
  public void SetFreqB(long freq) {
    CurrentFreqB = freq;
    if(resetCenterFreqFlag) {
      CenterFreq = freq;
      NCOFreq = 0;
      resetCenterFreqFlag = false;
    }
  }

  public void SetAsState(ASData data) {
    //TxRxFreq = data.TxRxFreq;
    // AS command doesn't change the center frequency
    // use SetFreq to handle events that need to set it
    if (activeVFO == 0) {
      if(currentFreqA != data.TxRxFreq) {
        SetFreqA(data.TxRxFreq);
      }
    } else {
      if(currentFreqA != data.TxRxFreq) {
        SetFreqA(data.TxRxFreq);
      }
    }
    if(currentBand != data.currentBand) {
      CurrentBand = data.currentBand;
    }
    if(xmtMode != data.xmtMode) {
      XmtMode = data.xmtMode;
    }
    if(currentDemod != data.mode) {
      CurrentDemod = data.mode;
    }
  }

  public void SetDbm(int value) {
    dbm = ((double)value) / 10.0;
    //dbm = (double)smeter;
    NotifyPropertyChanged(nameof(Dbm));
    //NotifyPropertyChanged("SMeter");
  }
  public void SetSmeter(int value) {
    SMeter = (int)(value * 1.5);
    //smeter = (int)(value * 1.5);
    //smeter = value;
    //dbm = (double)smeter;
    //NotifyPropertyChanged("Dbm");
    //NotifyPropertyChanged("SMeter");
  }

  // properties related to bandwidth bar
  public double BwLeft {
    get {
      double factor = 0; // USB and data modes
      switch(currentDemod) {
        case 1:
          factor = -1;
          break;
        case 2:
        case 3:
        case 8:
          factor = -0.5;
          break;
        default:
          break;
      }
      return ((double)(Math.Abs(FHiCut) * factor + (double)ncoFreq) / 96000.0 + 0.5) * 768.0;
    }
  }
  public double BwWidth {
    get { return (double)Math.Abs(FHiCut) / 96000.0 * 768.0; }
  }
  public double BwLine {
    get { return ((double)ncoFreq / 96000.0 + 0.5 ) * 768.0; }
  }
  public string BwValueLeft {
    get {
      int filter = Math.Abs(FLoCut); // USB and data modes
      switch(currentDemod) {
        case 1:
          filter = FHiCut;
          break;
        case 2:
        case 3:
        case 8:
          filter = Math.Abs(FHiCut) * 2;
          break;
        default:
          break;
      }
      return ((double)filter / 1000.0).ToString("F1") + "kHz";
    }
  }
  public string BwValueRight {
    get {
      int filter = Math.Abs(FHiCut); // USB and data modes
      switch(currentDemod) {
        case 1:
          filter = FLoCut;
          break;
        case 2:
        case 3:
        case 8:
          //filter = Math.Abs(FHiCut) * 2;
          return "";
          //break;
        default:
          break;
      }
      return ((double)filter / 1000.0).ToString("F1") + "kHz";
    }
  }

  public string[] GetPorts() {
    return t41Ctl.Ports;
  }

  public bool Connect(string item) {
    return t41Ctl.Connect(item);
  }
}
