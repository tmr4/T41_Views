using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Globalization;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Data;
using System.Drawing;
using System.Windows;

using T41_UI.ViewModels;
using T41_Radio;

namespace T41_UI.Views;

public sealed partial class MainPage : Page {
  public T41 t41;

  // frequency bar
  private int FREQUENCY_W_ACTIVE = 384;
  private int FREQUENCY_W_INACTIVE = 250;
  private int FREQ_FONT_ACTIVE = 72;
  private int FREQ_FONT_INACTIVE = 48;

  // spectrum sizes
  private int freqSpectrumW;
  private int freqSpectrumH;
  private const int t41FreqSpectrumH = 150;
  private int audioSpectrumW;
  private int audioSpectrumH;

  //private double sizeFactor = 1.5; //
  // *** TODO: move sample rate to T41 ***
  private long grat = 12; // 192000.0 / 8000.0 / 2.0;
  private double AudioGrat = 0.0623; // 405 / 6500;

  public int[] t41FreqData;
  public int[] t41AudioData;

  private Line[] fLines;
  private Line[] aLines;

  public MainViewModel ViewModel {
    get;
  }
  public Waterfall WaterfallInst { get {return waterfall; } }

  public MainPage()     {
    ViewModel = App.GetService<MainViewModel>();
    InitializeComponent();

    // initialize display sizes from XAML
    freqSpectrumW = (int)freqSpectrum.Width;
    freqSpectrumH = (int)freqSpectrum.Height;
    audioSpectrumW = (int)audioSpectrum.Width;
    audioSpectrumH = (int)audioSpectrum.Height;

    t41FreqData = new int[freqSpectrumW];
    t41AudioData = new int[audioSpectrumW];

    fLines = new Line[freqSpectrumW];
    aLines = new Line[audioSpectrumW];

    // *** TODO: make separate controls for these like the waterfall ***
    // initialize frequency and audio spectrums
    for(int i = 0; i < freqSpectrumW; i++) {
      fLines[i] = new Line();
      fLines[i].Stroke = new SolidColorBrush(Colors.Yellow);
      fLines[i].X1 = i+1;
      fLines[i].Y1 = freqSpectrumH - 1;
      fLines[i].X2 = i+1;
      fLines[i].Y2 = freqSpectrumH;
      freqSpectrum.Children.Add(fLines[i]);
      if(i < audioSpectrumW) {
        aLines[i] = new Line();
        aLines[i].Stroke = new SolidColorBrush(Colors.Magenta);
        aLines[i].X1 = i+1;
        aLines[i].Y1 = audioSpectrumH - 1;
        aLines[i].X2 = i+1;
        aLines[i].Y2 = audioSpectrumH;
        audioSpectrum.Children.Add(aLines[i]);
      }
    }

    t41 = new T41(this);
    SetFilters();

    this.DataContext = t41;

    ShowSpectrumFreqValues();
  }

  private void SetFilters() {
    int lowIndex = (int)((double)Math.Abs(t41.FLoCut) * AudioGrat);
    int hiIndex = (int)((double)Math.Abs(t41.FHiCut) * AudioGrat);
    aLines[lowIndex].Y1 = 0;
    aLines[lowIndex].Stroke = new SolidColorBrush(Colors.White);
    aLines[hiIndex].Y1 = 0;
    aLines[hiIndex].Stroke = new SolidColorBrush(Colors.Lime);
  }

  private void serialPort_ComboBoxOpened(object sender, object e) {
    serialPortComboBox.Items.Clear();

    foreach (string port in t41.GetPorts()) {
      serialPortComboBox.Items.Add(port);
    }
  }

  private void serialPort_ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e) {
    string? item = serialPortComboBox.SelectedItem.ToString();
    //if(serialPortComboBox.SelectedIndex != -1 && !t41.Connect(serialPortComboBox.SelectedItem.ToString())) {
    if(item != null && !t41.Connect(item)) {
      serialPortComboBox.SelectedIndex = -1;
    }
  }

  // *** multiple PointerWheelChanged events (2 in my tests) are triggered with a single notch,
  //     regardless of settings, when in debug mode, flag allows us to skip the second event ***
  private void vfoAFreq_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.IncFreqA(e.GetCurrentPoint(vfoAFreq).Properties.MouseWheelDelta > 0 ? 1 : -1);
    ShowSpectrumFreqValues();
  }

  private void vfoBFreq_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.IncFreqB(e.GetCurrentPoint(vfoBFreq).Properties.MouseWheelDelta > 0 ? 1 : -1);
    //int delta = e.GetCurrentPoint(vfoBFreq).Properties.MouseWheelDelta > 0 ? 1 : -1;
    //t41.IncFreqB(delta);
    //waterfall.testOffset += delta;
    //waterfall.TestGradient2();
    ShowSpectrumFreqValues();
  }

  private void freqSpectrum_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    if(t41.LiveNoiseFloorFlag == 1) {
      t41.CurrentNF += e.GetCurrentPoint(vfoAFreq).Properties.MouseWheelDelta > 0 ? 1 : -1;
    }
  }

  private void audioVolControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.AudioVolume += e.GetCurrentPoint(vfoAFreq).Properties.MouseWheelDelta > 0 ? 1 : -1;
  }

  private void agc_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.AGCMode += e.GetCurrentPoint(vfoAFreq).Properties.MouseWheelDelta > 0 ? 1 : -1;
    if(t41.AGCMode == 0){
      agcControl.Foreground = new SolidColorBrush(Colors.White);
    } else {
      agcControl.Foreground = new SolidColorBrush(Colors.Lime);
    }
  }

  private void ftIncrement_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.FtIndex += e.GetCurrentPoint(vfoAFreq).Properties.MouseWheelDelta > 0 ? 1 : -1;
  }

  private void freqIncrement_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.FreqIndex += e.GetCurrentPoint(vfoAFreq).Properties.MouseWheelDelta > 0 ? 1 : -1;
  }

  private void band_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.CurrentBand += e.GetCurrentPoint(band).Properties.MouseWheelDelta > 0 ? 1 : -1;
  }

  private void mode_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.XmtMode += e.GetCurrentPoint(mode).Properties.MouseWheelDelta > 0 ? 1 : -1;
  }

  private void demod_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.CurrentDemod += e.GetCurrentPoint(demod).Properties.MouseWheelDelta > 0 ? 1 : -1;
  }

  private void pwr_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
    t41.TransmitPowerLevel += e.GetCurrentPoint(pwr).Properties.MouseWheelDelta > 0 ? 1 : -1;
  }

  public void SetActiveVFO(int activate) {
    if(activate == 0) {
      // activate VFO A
      vfoBFreq.Foreground = new SolidColorBrush(Colors.White);
      vfoBFreq.FontSize = FREQ_FONT_INACTIVE;
      vfoBFreq.Width = FREQUENCY_W_INACTIVE;
      vfoBFreqButton.Width = FREQUENCY_W_INACTIVE;
      //vfoBFreqButton.IsEnabled = true;

      vfoAFreq.Foreground = new SolidColorBrush(Colors.Lime);
      vfoAFreq.FontSize = FREQ_FONT_ACTIVE;
      vfoAFreq.Width = FREQUENCY_W_ACTIVE;
      vfoAFreqButton.Width = FREQUENCY_W_ACTIVE;
      //vfoAFreqButton.IsEnabled = false;
    } else {
      // activate VFO B
      vfoAFreq.Foreground = new SolidColorBrush(Colors.White);
      vfoAFreq.FontSize = FREQ_FONT_INACTIVE;
      vfoAFreq.Width = FREQUENCY_W_INACTIVE;
      vfoAFreqButton.Width = FREQUENCY_W_INACTIVE;
      //vfoAFreqButton.IsEnabled = true;

      vfoBFreq.Foreground = new SolidColorBrush(Colors.Lime);
      vfoBFreq.FontSize = FREQ_FONT_ACTIVE;
      vfoBFreq.Width = FREQUENCY_W_ACTIVE;
      vfoBFreqButton.Width = FREQUENCY_W_ACTIVE;
      //vfoBFreqButton.IsEnabled = false;
    }

    ShowSpectrumFreqValues();
  }
  private void vfoAFreq_Click(object sender, RoutedEventArgs e) {
    if(t41.ActiveVFO == 1) {
      t41.ActiveVFO = 0;
      SetActiveVFO(0);
    }
  }
  private void vfoAFreq_RightTapped(object sender, RightTappedRoutedEventArgs e) {
    if(t41.ActiveVFO == 0) {
      // zero out 1000s part of VFO A frequency
      t41.ZeroFreqA();
    }
  }
  private void vfoBFreq_Click(object sender, RoutedEventArgs e) {
    if(t41.ActiveVFO == 0) {
      t41.ActiveVFO = 1;
      SetActiveVFO(1);
    }
  }
  private void vfoBFreq_RightTapped(object sender, RightTappedRoutedEventArgs e) {
    if(t41.ActiveVFO == 1) {
      // zero out 1000s part of VFO A frequency
      t41.ZeroFreqB();
    }
  }

  private void centerFreq_Click(object sender, RoutedEventArgs e) {
    t41.ResetTuning();
    ShowSpectrumFreqValues();
  }

  private void ftIncrement_Click(object sender, RoutedEventArgs e) {
    t41.CenterTuneActive = false;
    ftIncControl.Foreground = new SolidColorBrush(Colors.Lime);
    freqIncControl.Foreground = new SolidColorBrush(Colors.White);
  }

  private void freqIncrement_Click(object sender, RoutedEventArgs e) {
    t41.CenterTuneActive = true;
    freqIncControl.Foreground = new SolidColorBrush(Colors.Lime);
    ftIncControl.Foreground = new SolidColorBrush(Colors.White);
  }

  private void pwr_Click(object sender, RoutedEventArgs e) {
    t41.TransmitPowerLevel += 1;
  }

  private void pwr_RightTapped(object sender, RightTappedRoutedEventArgs e) {
    t41.TransmitPowerLevel -= 1;
  }

  private void nfSet_Click(object sender, RoutedEventArgs e) {
    t41.LiveNoiseFloorFlag = t41.LiveNoiseFloorFlag == 1 ? 0 : 1;
    //if(t41.LiveNoiseFloorFlag == 1) {
    //
    //} else {
    //
    //}
  }

  private void data_Click(object sender, RoutedEventArgs e) {
    if(t41.DataFlag) {
      wfStartPauseButton.Content = "Start";
      t41.DataFlag = false;
    } else {
      wfStartPauseButton.Content = "Pause";
      t41.DataFlag = true;
    }
  }

  private void ShowOperatingStats() {

  }

  public void ShowSpectrumFreqValues() {
    long tmp;

    tmp = t41.CenterFreq / 1000;
    freq3.Text = tmp.ToString("F0");

    tmp = t41.CenterFreq / 1000 - grat * 4;
    freq1.Text = tmp.ToString("F0");

    tmp = t41.CenterFreq / 1000 - grat * 2;
    freq2.Text = tmp.ToString("F0");

    tmp = t41.CenterFreq / 1000 + grat * 2;
    freq4.Text = tmp.ToString("F0");

    tmp = t41.CenterFreq / 1000 + grat * 4;
    freq5.Text = tmp.ToString("F0");
  }

  public void UpdateFreqSpec(byte[] data, int offset) {
    int count = 0;
    int adj = 0;
    int tmp;

    // interpolate data to freqSpectrumW (768 = 512 * 1.5)
    // *** TODO: check for transforms to do this ***
    for(int i = 0; i < 512; i += 2) {
      // back out the (255 - max) offset made when transfering data and
      // scale to control app frequency spectrum size
      // it's 221 in this app, thus back out an
      // additional 26 to make the two the comparable
      // frequency spectrum data starts at index 5
      //t41FreqData[2 * (i + 1) - count - 2] = data[i + 5] - offset - 26;
      //t41FreqData[2 * (i + 1) - count - 1] = ((data[i + 5] + data[i + 6]) / 2) - offset - 26;
      //t41FreqData[2 * (i + 1) - count] = data[i + 6] - offset - 26;
      //t41FreqData[2 * (i + 1) - count - 2] = (int)((double)(data[i + 5] - offset - 26) * 1.5);
      //t41FreqData[2 * (i + 1) - count - 1] = (int)((double)(((data[i + 5] + data[i + 6]) / 2) - offset - 26) * 1.5);
      //t41FreqData[2 * (i + 1) - count] = (int)((double)(data[i + 6] - offset - 26) * 1.5);
      t41FreqData[2 * (i + 1) - count - 2] = (int)((data[i + 5] - offset) * freqSpectrumH / t41FreqSpectrumH - adj);
      t41FreqData[2 * (i + 1) - count - 1] = (int)(((data[i + 5] + data[i + 6]) / 2 - offset) * freqSpectrumH / t41FreqSpectrumH - adj);
      t41FreqData[2 * (i + 1) - count] = (int)((data[i + 6] - offset) * freqSpectrumH / t41FreqSpectrumH - adj);
      count++;
    }

    // the T41 displays this spectrum after subtracting it from the fixed spectrum floor (247)
    // that's equivalent to the frequency spectrum height here
    for(int i = 0; i < freqSpectrumW - 1; i++) {
      // scale freq data for screen
      tmp = freqSpectrumH - t41FreqData[i];
      if (tmp < 0) tmp = 0;
      if (tmp > freqSpectrumH) tmp = freqSpectrumH;
      fLines[i].Y1 = tmp;

      tmp = freqSpectrumH - t41FreqData[i+1];
      if (tmp < 0) tmp = 0;
      if (tmp > freqSpectrumH) tmp = freqSpectrumH;
      fLines[i].Y2 = tmp;
    }

    // update waterfall
    //waterfall.RollWaterfall(t41FreqData, offset);
    waterfall.RollWaterfall(t41FreqData, offset);
  }

  public void UpdateAudioSpec(byte[] data) {
    int count = 0;
    // *** TODO: make these fields ***
    int lowIndex = (int)((double)Math.Abs(t41.FLoCut) * AudioGrat);
    int hiIndex = (int)((double)Math.Abs(t41.FHiCut) * AudioGrat);

    // interpolate data to audioSpectrumW
    // *** TODO: check for transforms to do this ***
    for(int i = 0; i < 270; i += 2) {
      t41AudioData[2 * (i + 1) - count - 2] = data[i];
      t41AudioData[2 * (i + 1) - count - 1] = (byte)((data[i] + data[i + 1]) / 2);
      t41AudioData[2 * (i + 1) - count] = data[i + 1];
      count++;
    }

    for(int i = 0; i < audioSpectrumW - 1; i++) {
      // skip filter lines
      // *** TODO: try out filter lines in a separate control ***
      if(!((i == lowIndex) || (i == hiIndex))) {
        if(t41AudioData[i] > 0) {
          // scale audio data for screen
          int tmp;
          tmp = audioSpectrumH - t41AudioData[i];
          if(tmp < 0) {
            tmp = 0;
          }
          aLines[i].Y1 = (byte)tmp;
        } else {
          aLines[i].Y1 = audioSpectrumH - 1;
        }
      }
    }
  }
}
