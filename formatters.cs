using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace T41_UI.Views;

public class FreqFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    NumberFormatInfo format = new() {
      NumberDecimalDigits = 0,
      NumberGroupSeparator = parameter != null ? (string)parameter : "."
    };

    return ((long)value).ToString("N", format);
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class BandFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    string band = "";
    switch((int)value) {
      case 0: // BAND_80M:
        band = "80m";
        break;
      case 1: // BAND_40M:
        band = "40m";
        break;
      case 2: // BAND_20M:
        band = "20m";
        break;
      case 3: // BAND_17M:
        band = "17m";
        break;
      case 4: // BAND_15M:
        band = "15m";
        break;
      case 5: // BAND_12M:
        band = "12m";
        break;
      case 6: // BAND_10M:
        band = "10m";
        break;
      default:
        band = "40m";
        break;
    }
    return band;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class ModeFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    string mode = "";
    switch((int)value) {
      case 0: // SSB_MODE:
        mode = "SSB";
        break;
      case 1: // CW_MODE:
        mode = "CW";
        break;
      case 2: // DATA_MODE:
        mode = "DATA";
        break;
      default:
        mode = "SSB";
        break;
    }
    return mode;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class DemodFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
      string demod = "";
      switch((int)value) {
        case 0: // DEMOD_USB:
          demod = "(USB)";
          break;
        case 1: // DEMOD_LSB:
          demod = "(LSB)";
          break;
        case 2: // DEMOD_AM:
          demod = "(AM)";
          break;
        case 3: // DEMOD_NFM:
          demod = "(NFM)";
          break;
        case 4: // DEMOD_PSK31_WAV:
          demod = "(PSK31.wav)";
          break;
        case 5: // DEMOD_PSK31:
          demod = "(PSK31)";
          break;
        case 6: // DEMOD_FT8_WAV:
          demod = "(FT8.wav)";
          break;
        case 7: // DEMOD_FT8:
          demod = "(FT8)";
          break;
        case 8: // DEMOD_SAM:
          demod = "(SAM)";
          break;
        default:
          demod = "(LSB)";
          break;
      }
    return demod;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class AgcFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    // const char *agcOpts[] = { "Off", "L", "S", "M", "F" };
    string mode = "";
    switch((int)value) {
      case 0:
        mode = "Off";
        break;
      case 1:
        mode = "L";
        break;
      case 2:
        mode = "S";
        break;
      case 3:
        mode = "M";
        break;
      case 4:
        mode = "F";
        break;
      default:
        mode = "Off";
        break;
    }
    return mode;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class PwrFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    return ((int)value).ToString("D") + " Watts";
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class NfFlagColorFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    //return (SolidColorBrush)new SolidColorBrush((int)value == 1 ? Colors.Lime : Colors.White);
    return new SolidColorBrush((int)value == 1 ? Colors.Lime : Colors.White);
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}
public class NfFlagTextFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    return (int)value == 1 ? "On" : "Off";
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}
/*
public class NcoFreqBWFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    return ; // 360 for 3k filter, 0 NCOFreq
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class BWFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    return 24;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}

public class NcoFreqFormatter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    return 384;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    throw new NotImplementedException();
  }
}
*/
//public class TimeFormatter : IValueConverter {
//  public object Convert(object value, Type targetType, object parameter, string language) {
//    return "PST: " + DateTime.Now.ToString("H:mm:ss");
//  }
//
//  public object ConvertBack(object value, Type targetType, object parameter, string language) {
//    throw new NotImplementedException();
//  }
//}

//public class Formatter : IValueConverter {
//  public object Convert(object value, Type targetType, object parameter, string language) {
//    return ;
//  }
//
//  public object ConvertBack(object value, Type targetType, object parameter, string language) {
//    throw new NotImplementedException();
//  }
//}
