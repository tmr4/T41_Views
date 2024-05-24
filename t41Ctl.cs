using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using Microsoft.UI.Dispatching;

using T41_Radio;
using T41_UI.Views;

namespace T41_Control;

public class ASData {
  public long TxRxFreq;
  public int xmtMode;
  public int mode;
  public int currentBand;

  protected bool error = false;
  protected string cmd = "";

  public ASData(byte[] byt) {
    cmd = Encoding.Default.GetString(byt);
    FetchValues();
  }

  // sprintf(cmd, "AS%011ld%d%d%d;",
  //   TxRxFreq                        // freq in Hz (%011d) at index 2
  //   currentBand,                    // current band (%d) at index 13
  //   xmtMode,                        // transmission mode (%d) at index 14
  //   bands[currentBand].mode,        // demodulation mode (%d)  at index 15
  private bool FetchValues() {
    error |= !FetchFreq(cmd, out TxRxFreq);
    error |= !FetchInt(cmd, 13, out currentBand);
    error |= !FetchInt(cmd, 14, out xmtMode);
    error |= !FetchInt(cmd, 15, out mode);
    return error;
  }

  public static bool FetchFreq(byte[] byt, out long result) {
    return long.TryParse(Encoding.Default.GetString(byt, 2, 11), out result);
  }
  public static bool FetchLong(byte[] byt, int start, int len, out long result) {
    return long.TryParse(Encoding.Default.GetString(byt, start, len), out result);
  }
  public static bool FetchInt(byte[] byt, int start, int len, out int result) {
    return int.TryParse(Encoding.Default.GetString(byt, start, len), out result);
  }
  public static bool FetchInt(byte[] byt, int start, out int result) {
    return int.TryParse(Encoding.Default.GetString(byt, start, 1), out result);
  }
  public static bool FetchFreq(string cmd, out long result) {
    return long.TryParse(cmd.Substring(2, 11), out result);
  }
  public static bool FetchLong(string cmd, int start, int len, out long result) {
    return long.TryParse(cmd.Substring(start, len), out result);
  }
  public static bool FetchInt(string cmd, int start, int len, out int result) {
    return int.TryParse(cmd.Substring(start, len), out result);
  }
  public static bool FetchInt(string cmd, int start, out int result) {
    return int.TryParse(cmd.Substring(start, 1), out result);
  }
}

public class IFData : ASData {
  public int audioVolume;
  public int NCOFreq;
  public int currentNoiseFloor;
  public int liveNoiseFloorFlag;
  public int xrState;
  public int activeVFO;
  public int centerTuneActive;
  public int ftIndex;
  public int tuneIndex;
  public int AGCMode;
  public int spectrumZoom;
  public long inactiveVFOFreq;
  //public int splitVFO;

  public IFData(byte[] byt) : base(byt) {
    //cmd = Encoding.Default.GetString(byt);
    FetchValues();
  }

  // extract data
  // sprintf(cmd, "IF%011ld%d%d%d%03d%+06ld%04d%d%d%d%d%d%d%d%ld%011d;",
  //   // active VFO Freq = TxRxFreq, centerFreq = TxRxFreq - NCOFreq
  //   //  *** TODO: we only need 8 digits for first field for T41, consider using other 3 for something ***
  //   TxRxFreq,                       // freq in Hz (%011d) at index 2
  //   currentBand,                    // current band (%d) at index 13
  //   xmtMode,                        // transmission mode (%d) at index 14
  //   bands[currentBand].mode,        // demodulation mode (%d)  at index 15
  //   audioVolume,                    // audio volume (%03d) at index 16
  //   NCOFreq,                        // NCO freq (%+06ld) at index 19
  //   currentNoiseFloor[currentBand], // noise floor (%04d) at index 25 *** TODO: verify need for +- or number of digits ***
  //   liveNoiseFloorFlag,             // set noise floor active/inactive 1/0 (%d) at index 29
  //   !xrState,                       // RX/TX (1/0) (%d) at index 30
  //   activeVFO,                      // VFO A/B (0/1) (%d) at index 31
  //   mouseCenterTuneActive ? 1 : 0,  // fine or center tune enabled (0/1) (%d) at index 32
  //   ftIndex,                        // fine tune index (%d) at index 33
  //   tuneIndex,                      // center tune index (%d) at index 34
  //   AGCMode,                        // AGC mode (%d) at index 35
  //   spectrumZoom,                   // spectrum zoom (%ld) at index 36
  //   activeVFO == 0 ? currentFreqB : currentFreqA // inactive VFO freq in Hz (%011d) at index 37
  private bool FetchValues() {
    error |= !FetchInt(cmd, 16, 3, out audioVolume);
    error |= !FetchInt(cmd, 25, out currentNoiseFloor);
    error |= !FetchInt(cmd, 29, out liveNoiseFloorFlag);
    error |= !FetchInt(cmd, 31, out activeVFO);
    error |= !FetchInt(cmd, 32, out centerTuneActive);
    error |= !FetchInt(cmd, 33, out ftIndex);
    error |= !FetchInt(cmd, 34, out tuneIndex);
    error |= !FetchInt(cmd, 35, out AGCMode);
    error |= !FetchLong(cmd, 37, 11, out inactiveVFOFreq);
    return error;
  }
}

// impliments a subset of computer control commands as shown in Appendix 21 of the Kenwood TS-2000 User Manual
// (https://www.qrzcq.com/pub/RADIO_MANUALS/KENWOOD/KENWOOD--TS-2000-User-Manual.pdf)
// plus additional commands to enable control of other T41-EP features
public class T41Control {
  private MainPage mainPage;
  private T41 t41;
  private DispatcherQueue dispatcherQueue;

  // serial port data
  private bool connectionStarted = false;
  private string selectedPort = "";
  private SerialPort? serialPort = null;

  public string[] Ports { get; set; }

  public T41Control(T41 parent, MainPage mp, DispatcherQueue queue) {
    t41 = parent;
    mainPage = mp;
    dispatcherQueue = queue;

    //serialPort = new SerialPort();
    Ports = SerialPort.GetPortNames();
    t41.PropertyChanged += T41PropertyChangedHandler;
  }

  private void T41PropertyChangedHandler(object? sender, PropertyChangedEventArgs e) {
    switch(e.PropertyName) {
      case "ActiveVFO":
        SendCmd("FT" + t41.ActiveVFO.ToString("D1"));
        break;

      case "AGCMode":
        SendCmd("GT" + t41.AGCMode.ToString("D1")); // note that Kenwood is a D3
        break;

      case "AudioVolume":
        SendCmd("VO" + t41.AudioVolume.ToString("D3"));
        break;

      case "CenterFreq":
        SendCmd("FC" + t41.CenterFreq.ToString("D11"));
        break;

      case "CenterTuneActive":
        // off=0, on=1
        SendCmd("FS" + (t41.CenterTuneActive ? "0" : "1"));
        break;

      case "CurrentBand":
        SendCmd(t41.BandChangeUp ? "BU" : "BD");
        break;

      case "CurrentDemod":
        SendCmd("MD" + t41.CurrentDemod.ToString("D1"));
        break;

      case "CurrentFreqA":
        SendCmd("FA" + t41.CurrentFreqA.ToString("D11"));
        break;

      case "CurrentFreqB":
        SendCmd("FB" + t41.CurrentFreqB.ToString("D11"));
        break;

      case "CurrentNF":
        SendCmd("NF" + t41.CurrentNF.ToString("D4"));
        break;

      case "DataFlag":
        SendCmd(t41.DataFlag ? "DS" : "DP");
        break;

      case "FreqIndex":
        //SetFreqInc(FreqIndex);
        SendCmd("FI0" + t41.FreqIndex.ToString("D1"));
        break;

      case "FtIndex":
        //SetFtInc(FtIndex);
        SendCmd("FI1" + t41.FtIndex.ToString("D1"));
        break;

      case "LiveNoiseFloorFlag":
        SendCmd("NG" + t41.LiveNoiseFloorFlag.ToString("D1"));
        break;

      case "TransmitPowerLevel":
        SendCmd("PC" + t41.TransmitPowerLevel.ToString("D3"));
        break;

      case "XmtMode":
        SendCmd("ME" + t41.XmtMode.ToString("D1"));
        break;

      //case "":
      //  //SendCmd("" + t41.);
      //  break;

      default:
        break;
    }
  }

  //private void GetSerialPortNames() {
  //  Ports = SerialPort.GetPortNames();
  //}

  public bool Connect(string port) {
    bool result = false;

    try {
      if (!connectionStarted && port != selectedPort) {
        // try to connect to selected port
        serialPort = new SerialPort();
        serialPort.PortName = port;

        // we have a USB serial port communicating at native USB speeds
        // these aren't used
        serialPort.BaudRate = 19200;
        serialPort.Parity = Parity.None;
        serialPort.DataBits = 8;
        serialPort.DtrEnable = false;
        serialPort.RtsEnable = false;

        try {
          serialPort.Open();
        }

        //catch (Exception ex) {
        catch (Exception) {
          connectionStarted = false;
          selectedPort = "";
          return result;
        }

        Thread.Sleep(30);

        if (serialPort.IsOpen) {
          connectionStarted = true;
          selectedPort = port;
          result = true;

          // set T41 time
          SetTime();

          serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

          // get current state of T41
          SendCmd("IF");
        } else {
          connectionStarted = false;
          selectedPort = "";
        }
      }
      return result;
    }

    //catch (Exception ex) {
    catch (Exception) {
      connectionStarted = false;
      selectedPort = "";
      return result;
    }
  }


  public void SetTime() { //long time) {
    // *** TODO: the T41 time will lag from this by the serial processing time, add a second for now ***
    SendCmd("TM" + (DateTimeOffset.Now.ToUnixTimeSeconds() - 7 * 60 * 60 + 1).ToString("D11"));
  }

  private void SendCmd(string cmd) {
    if (serialPort != null && serialPort.IsOpen) {
      serialPort.Write(cmd + ";");
    }
  }

  // Serial port DataReceived events are handled in a secondary thread
  // (https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.datareceived?view=net-8.0)
  // this says to "post change requests back using Invoke, which will do the work on the proper thread"
  // I couldn't get Invoke or events to work as it seems with winui 3 these remain on the current thread.
  // Using the main UI thread dispatcher for changes to bound properties does work.
  // (amazing how difficult it is to find information on this online)
  private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
    SerialPort sp = (SerialPort)sender;
    int len = sp.BytesToRead;

    if(len > 0) {
      byte[] byt = new byte[len];

      sp.Read(byt, 0, len);

      // *** TODO: below I handle the possibility that there might be more than one command in the buffer
      //           for buffers smaller than the spectrum data streams.  There is a possibility that buffer
      //           length alone will not be sufficient to distinguish between these.  For example, what if
      //           a command arrives in the PC buffer at the same time as the audio spectrum.  Currently,
      //           both of these would be ignored.  Probably not the preferred outcome.  This means the
      //           spectrum streams will need identified and searched if command lengths don't match expected. ***
      if(len < 270) {
        int first, last;
        byte[] cmd = new byte[len];

        // we might have more than one command
        byt.CopyTo(cmd, 0);
        do {
          first = Array.IndexOf(cmd, (byte)';');
          last = Array.LastIndexOf(cmd, (byte)';');

          // execute command
          switch((char)cmd[0]) {
            case 'A':
              // process auto state
              if((char)cmd[1] == 'S' && (char)cmd[16] == ';') {
                ASData asData = new ASData(byt);
                dispatcherQueue.TryEnqueue(() => { t41.SetAsState(asData); });
              }
              break;

            case 'F':
              if((char)cmd[1] == 'A' && (char)cmd[13] == ';') {
                long freq;
                if(ASData.FetchFreq(byt, out freq)) {
                  dispatcherQueue.TryEnqueue(() => { t41.SetFreqA(freq); });
                } else {
                  // try again
                  // *** TODO: probably need some time out here for when radio stops providing valid data ***
                  SendCmd("FA");
                }
              } else if((char)cmd[1] == 'B' && (char)cmd[13] == ';') {
                long freq;
                if(ASData.FetchFreq(byt, out freq)) {
                  dispatcherQueue.TryEnqueue(() => { t41.SetFreqB(freq); });
                } else {
                  // try again
                  // *** TODO: probably need some time out here for when radio stops providing valid data ***
                  SendCmd("FB");
                }
              }

              break;

            case 'I':
              if((char)cmd[1] == 'F' && (char)cmd[48] == ';') {
                // transceiver status
                  // *** Warning: this is not the Kenwood implimentation ***
                IFData ifData = new IFData(byt);
                dispatcherQueue.TryEnqueue(() => { t41.SetState(ifData); });
              }
              break;

            case 'N':
              if((char)cmd[1] == 'F' && (char)cmd[6] == ';') {

              }
              break;

            case 'S':
              if((char)cmd[1] == 'M' && (char)cmd[8] == ';') {
                int result;

                // process s-meter data
                int.TryParse(Encoding.Default.GetString(cmd, 3, 5), out result);
                if((char)cmd[2] == '0') {
                  // dBm value * 10
                  dispatcherQueue.TryEnqueue(() => { t41.SetDbm(result); });
                } else if((char)cmd[2] == '2') {
                  // s-meter
                  dispatcherQueue.TryEnqueue(() => { t41.SetSmeter(result); });
                }
              }
              break;

            default:
              break;
          }

          if(first != last) {
            Array.Copy(cmd, first + 1, cmd, 0, last - first);
            Array.Fill(cmd, (byte)0, last - first, first + 1);
          }
        } while (first != last);
      } else {
        // got spectrum data or some unknown/corrupted data
        // *** TODO: need a way to check for multiple commands in a byte buffer when the data can be a ';' (59) ***
        if(t41.DataFlag) {
          // try to interpret received data as the freqency or audio spectrum data stream
          if(len == 518) {
            // validate command and get offset
            if((char)byt[0] == 'F' && (char)byt[1] == 'D' && (char)byt[517] == ';') {
              int offset = 0; // 255 - max value of data sent
              ASData.FetchInt(byt, 2, 3, out offset);
              dispatcherQueue.TryEnqueue(() => { mainPage.UpdateFreqSpec(byt, offset); });
            }
          } else if(len == 270) {
            dispatcherQueue.TryEnqueue(() => { mainPage.UpdateAudioSpec(byt); });
          }
        }
      }
    }
  }

  //public void SetBand(int change) {
  //  SendCmd(change > 0 ? "BU" : "BD");
  //}
  //public void SetMode(int mode) {
  //  SendCmd("ME" + mode.ToString("D1"));
  //}
  //public void SetDemod(int demod) {
  //  SendCmd("MD" + demod.ToString("D1"));
  //}
  //public void SetCenterFreq(long freq) {
  //  SendCmd("FC" + freq.ToString("D11"));
  //}
  //public void SetFreqA(long freq) {
  //  SendCmd("FA" + freq.ToString("D11"));
  //}
  //public void SetFreqB(long freq) {
  //  SendCmd("FB" + freq.ToString("D11"));
  //}
  //public void SetActiveVFO(int activeVFO) {
  //  SendCmd("FT" + activeVFO.ToString("D1"));
  //}
  //public void SetFreqInc(int index) {
  //  SendCmd("FI0" + index.ToString("D1"));
  //}
  //public void SetFtInc(int index) {
  //  SendCmd("FI1" + index.ToString("D1"));
  //}
  //public void SetCenterTuneActive(bool flag) {
  //  // off=0, on=1
  //  SendCmd("FS" + flag.ToString("D1"));
  //}

  //public void GetFreqA() {
  //  SendCmd("FA");
  //  //SendCmd("IF");
  //}
  //public void GetFreqB() {
  //  SendCmd("FB");
  //}
  //public void GetState() {
  //  SendCmd("IF");
  //}
}
