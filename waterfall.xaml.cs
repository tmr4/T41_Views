
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Drawing;

namespace T41_UI.Views;

public sealed partial class Waterfall : UserControl {
  // The waterfall is updated by swapping back and forth between two CanvasRenderTargets,
  // repeatedly drawing the contents of one onto the other, moving the waterfall down one row
  // https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_CanvasRenderTarget.htm
  CanvasRenderTarget? currentWF, nextWF;

  private int waterfallW;
  private int waterfallH;

  // T41 waterfall color array, RGB565 values which is 65k colors
  // however, the RA8875 display used in the T41 is limited to 256 colors in
  // 2 layer mode
    // pg 73 of PA8875 datasheet confirms color bit structure
	  // to convert a 16bit color(565) into 8bit color(332)
  	// uint8_t _color16To8bpp(uint16_t color) => ((color & 0xe000) >> 8) | ((color & 0x700) >> 6) | ((color & 0x18) >> 3);
    // pg 75 of RA8875 datasheet shows 8-bit color mapping back to the display's 16-bit color
  int[] gradient = {                                                                // on T41 waterfall these are (# different colors):
    0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9,                               // black to blue (2)
    0x10, 0x1F, 0x11F, 0x19F, 0x23F, 0x2BF, 0x33F, 0x3BF, 0x43F, 0x4BF,             // lighter blue to cyan (6)
    0x53F, 0x5BF, 0x63F, 0x6BF, 0x73F, 0x7FE, 0x7FA, 0x7F5, 0x7F0, 0x7EB,           // cyan to cyan-green (3)
    0x7E6, 0x7E2, 0x17E0, 0x3FE0, 0x67E0, 0x8FE0, 0xB7E0, 0xD7E0, 0xFFE0, 0xFFC0,   // lime to yellow (6)
    0xFF80, 0xFF20, 0xFEE0, 0xFE80, 0xFE40, 0xFDE0, 0xFDA0, 0xFD40, 0xFD00, 0xFCA0, // yellow to yellow-brown (3)
    0xFC60, 0xFC00, 0xFBC0, 0xFB60, 0xFB20, 0xFAC0, 0xFA80, 0xFA20, 0xF9E0, 0xF980, // yellow-brown to red (4)
    0xF940, 0xF8E0, 0xF8A0, 0xF840, 0xF800, 0xF802, 0xF804, 0xF806, 0xF808, 0xF80A, // orange to red-magenta (3)
    0xF80C, 0xF80E, 0xF810, 0xF812, 0xF814, 0xF816, 0xF818, 0xF81A, 0xF81C, 0xF81E, // red-magenta to magenta (3)
    0xF81E, 0xF81E, 0xF81E, 0xF83E, 0xF83E, 0xF83E, 0xF83E, 0xF85E, 0xF85E, 0xF85E, // magenta (1)
    0xF85E, 0xF87E, 0xF87E, 0xF83E, 0xF83E, 0xF83E, 0xF83E, 0xF85E, 0xF85E, 0xF85E, // magenta (1), this row is messed up
    0xF85E, 0xF87E, 0xF87E, 0xF87E, 0xF87E, 0xF87E, 0xF87E, 0xF87E, 0xF87E, 0xF87E, // magenta (1)
    0xF87E, 0xF87E, 0xF87E, 0xF87E, 0xF88F, 0xF88F, 0xF88F                          // magenta to rose (2)
  };
  int[] gradientScaled = new int[174]; // gradient scaled up by 1.5 for app

  bool isPointerDown;
  int lastPointerX, lastPointerY;

  struct IntPoint {
    public int X;
    public int Y;

    public IntPoint(int x, int y) {
      X = x;
      Y = y;
    }
  }

  List<IntPoint> hitPoints = new List<IntPoint>();

  // https://microsoft.github.io/Win2D/WinUI3/html/N_Microsoft_Graphics_Canvas_Effects.htm
  // a transform matrix scales up the waterfall. Nearest neighbor filtering
  // avoids unwanted blurring of the waterfall.
  Transform2DEffect transformEffect = new Transform2DEffect();

  // custom DPI compensation effect to stop the system trying to
  // automatically convert DPI for us. The waterfall always works
  // in pixels (96 DPI) regardless of display DPI. Normally, the system would
  // handle this mismatch automatically and scale the image up as needed to fit
  // higher DPI displays. We don't want that behavior here, because it would use
  // a linear filter while we want nearest neighbor. So we insert a no-op DPI
  // converter of our own. This overrides the default adjustment by telling the
  // system the source image is already the same DPI as the destination canvas
  // (even though it really isn't). We'll handle any necessary scaling later
  // ourselves, using Transform2DEffect to control the interpolation mode.
  DpiCompensationEffect dpiCompensationEffect = new DpiCompensationEffect();

  public Waterfall() {
    int count = 0;

    this.InitializeComponent();

    // initialize waterfall size from XAML
    waterfallW = (int)canvas.Width;
    waterfallH = (int)canvas.Height;

    //canvas.Height = waterfallH; // *** TODO: need to update canvas height when changed to have is reflected in scrollviewer ***

    // interpolate T41 gradient to 174 = 116 * 1.5
    // *** TODO: check for transforms to do this ***
    for(int i = 0; i < 116; i += 2) {
      gradientScaled[2 * (i + 1) - count - 2] = gradient[i];
      gradientScaled[2 * (i + 1) - count - 1] = (gradient[i] + gradient[i + 1]) / 2;
      gradientScaled[2 * (i + 1) - count] = gradient[i + 1];
      count++;
    }
  }

  // https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_UI_Xaml_CanvasControl.htm
  void Canvas_CreateResources(CanvasControl sender, object args) {
    //const float defaultDpi = 96;
    const float defaultDpi = 200;

    currentWF = new CanvasRenderTarget(sender, waterfallW, waterfallH, defaultDpi);
    nextWF = new CanvasRenderTarget(sender, waterfallW, waterfallH, defaultDpi);
    //currentWF.SetPixelColors(Colors.Black);
    //nextWF.SetPixelColors(Colors.Black);

    dpiCompensationEffect.Source = currentWF;
    dpiCompensationEffect.SourceDpi = new Vector2(canvas.Dpi);
    transformEffect.Source = dpiCompensationEffect;
    transformEffect.InterpolationMode = CanvasImageInterpolation.Cubic; // *** TODO: try other options: NearestNeighbor, Linear ***

    //TestGradient();
    //TestGradient2();
  }

  // *** TODO: verify waterfall after fixing frequency spectrum noise floor ***
  void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args) {
    // Display the current surface
    transformEffect.TransformMatrix = GetDisplayTransform(sender);

    // CanvasDrawingSession has methods to draw to the CanvasControl
    // https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_CanvasDrawingSession.htm
    args.DrawingSession.DrawImage(transformEffect);
    //args.DrawingSession.DrawImage(currentWF); // unadjusted doesn't improve waterfall
  }
/*
  private void TestGradient() {
    for(int j = 0; j < 174 ; j++) {
      RollTest(j);
    }
  }
  public void TestGradient2() {
    for(int j = 0; j < 200 ; j++) {
      //RollTest(j);
      RollTest2();
    }
  }
  public void RollTest(int grad) {
    var colors = new Windows.UI.Color[waterfallW];
    var tmpWF = currentWF;

    // create data for waterfall
    var col = GetColorFromHex(gradientScaled[grad]);
    for (int i = 0; i < waterfallW; i++) {
      colors[i] = col;
    }

    // copy the current waterfall to the next one, moving it down one row
    nextWF.CopyPixelsFromBitmap(currentWF, 0, 1, 0, 0, waterfallW, waterfallH - 1);

    // copy new waterfall data into the freed up first row
    nextWF.SetPixelColors(colors, 0, 0, waterfallW, 1);

    // Swap the current and next waterfalls
    currentWF = nextWF;
    nextWF = tmpWF;

    // redraw waterfall
    canvas.Invalidate();
  }
  public int testOffset = 0;
  public void RollTest2() {
    var colors = new Windows.UI.Color[waterfallW];
    var tmpWF = currentWF;
    int grad;

    // create data for waterfall
    for (int i = 0; i < waterfallW; i++) {
      grad = (int)((double)i / 76.0) + testOffset * 10;
      //var col = GetColorFromHex(gradientScaled[grad]);
      var col = GetColorFromHex(gradient[grad]);
      colors[i] = col;
    }

    // copy the current waterfall to the next one, moving it down one row
    nextWF.CopyPixelsFromBitmap(currentWF, 0, 1, 0, 0, waterfallW, waterfallH - 1);

    // copy new waterfall data into the freed up first row
    nextWF.SetPixelColors(colors, 0, 0, waterfallW, 1);

    // Swap the current and next waterfalls
    currentWF = nextWF;
    nextWF = tmpWF;

    // redraw waterfall
    canvas.Invalidate();
  }
*/
  // Roll the waterfall 1 pixel down and add new data in the freed row
  // *** TODO: fix the freq data floor throughout T41 software to eliminate the arbitrary factors used to adjust level ***
  //public void RollWaterfall(int[] t41FreqData) {
  public void RollWaterfall(int[] t41FreqData, int offset) {
    var colors = new Windows.UI.Color[waterfallW];
    var tmpWF = currentWF;
    int tmp;

    // create data for waterfall
    for (int i = 0; i < waterfallW; i++) {
      //tmp = (int)((double)(-t41FreqData[i] + 220) / 1.5);
      //tmp = (int)((double)(-t41FreqData[i] + 220) * 1.5);
      //tmp = -t41FreqData[i] + 210;
      //tmp = -t41FreqData[i] + 220;
      tmp = t41FreqData[i] - 20;
      //tmp = -t41FreqData[i] + 225;
      //tmp = -t41FreqData[i] + 230;
      //tmp = (int)((225.0 - (double)t41FreqData[i]) / 1.5);
      //tmp = (int)((225.0 - (double)t41FreqData[i]) / 1.5);
      //tmp = -t41FreqData[i] + 270;
      //tmp = -t41FreqData[i] + 300;
      //tmp = (int)(i / 76) + offset - 4; // test color gradient
      if (tmp < 0) tmp = 0;
      if (tmp > 173) tmp = 173;
      colors[i] = GetColorFromHex(gradientScaled[tmp]);
      //if (tmp > 116) tmp = 116;
      //colors[i] = GetColorFromHex(gradient[tmp]);
    }

    // copy the current waterfall to the next one, moving it down one row
    nextWF?.CopyPixelsFromBitmap(currentWF, 0, 1, 0, 0, waterfallW, waterfallH - 1);

    // copy new waterfall data into the freed up first row
    nextWF?.SetPixelColors(colors, 0, 0, waterfallW, 1);

    // Swap the current and next waterfalls
    currentWF = nextWF;
    nextWF = tmpWF;

    // redraw waterfall
    canvas.Invalidate();
  }

  // Clears the waterfall state
  void ClearWaterfall(object sender, RoutedEventArgs e) {
    using (var ds = currentWF?.CreateDrawingSession()) {
      ds?.Clear(Colors.Black);
    }

    canvas.Invalidate();
  }

  void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e) {
    lock (hitPoints) {
      isPointerDown = true;
      lastPointerX = lastPointerY = int.MaxValue;

      ProcessPointerInput(e);
    }
  }

  void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e) {
    lock (hitPoints) {
      isPointerDown = false;
    }
  }

  void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e) {
    lock (hitPoints) {
      ProcessPointerInput(e);
    }
  }

  // Toggles the color of cells when they are clicked on.
  private void ProcessPointerInput(PointerRoutedEventArgs e) {
    if (!isPointerDown)
        return;

    // Invert the display transform, to convert pointer positions into simulation rendertarget space.
    Matrix3x2 transform;
    Matrix3x2.Invert(GetDisplayTransform(canvas), out transform);

    foreach (var point in e.GetIntermediatePoints(canvas)) {
      if (!point.IsInContact)
          continue;

      var pos = Vector2.Transform(point.Position.ToVector2(), transform);

      var x = canvas.ConvertDipsToPixels(pos.X, CanvasDpiRounding.Floor);
      var y = canvas.ConvertDipsToPixels(pos.Y, CanvasDpiRounding.Floor);

      // If the point is within the bounds of the rendertarget, and not the same as the last point...
      if (x >= 0 &&
          y >= 0 &&
          x < waterfallW &&
          y < waterfallH &&
          ((x != lastPointerX || y != lastPointerY)))
      {
        // We avoid manipulating GPU resources from inside an input event handler
        // (since we'd need to handle device lost and possible concurrent running with CreateResources).
        // Instead, we collect up a list of points and process them from the Draw handler.
        hitPoints.Add(new IntPoint(x, y));

        lastPointerX = x;
        lastPointerY = y;
      }
    }

    canvas.Invalidate();
  }

  void ApplyHitPoints() {
    //lock (hitPoints) {
    //  foreach (var point in hitPoints) {
    //    var x = point.X;
    //    var y = point.Y;
    //
    //    // Read the current color.
    //    var cellColor = currentWF.GetPixelColors(x, y, 1, 1);
    //
    //    // Toggle the value.
    //    cellColor[0] = cellColor[0].R > 0 ? Colors.Green : Colors.Yellow;
    //
    //    // Set the new color.
    //    currentWF.SetPixelColors(cellColor, x, y, 1, 1);
    //  }
    //
    //  hitPoints.Clear();
    //}
  }

  //static Matrix3x2 GetDisplayTransform(CanvasControl canvas) {
  Matrix3x2 GetDisplayTransform(CanvasControl canvas) {
    // Scale the display to fill the control
    var outputSize = canvas.Size.ToVector2();
    var sourceSize = new Vector2(canvas.ConvertPixelsToDips(waterfallW), canvas.ConvertPixelsToDips(waterfallH));
    var scale = outputSize / sourceSize;
    //var offset = Vector2.Zero;

    //scale.Y = 1;
    //return Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(offset);
    return Matrix3x2.CreateScale(scale);
    //return Matrix3x2.CreateTranslation(offset);
  }

  private void control_Unloaded(object sender, RoutedEventArgs e) {
    // Explicitly remove references to allow the Win2D controls to get garbage collected
    canvas.RemoveFromVisualTree();
    canvas = null;
  }

  // convert rgb565 to rgb888 to Windows.UI.Color
  public static Windows.UI.Color GetColorFromHex(int rgb565)
  {
    int red5, green6, blue5;
    byte red8, green8, blue8;

    // red upper 5-bits
    // green middle 6-bits
    // blue lower 5-bits
    // and convert these to 8-bit colors
    //red5 = rgb565 >> 11;
    //green6 = (rgb565 >> 5) & 0b111111;
    //blue5 = rgb565 & 0b11111;
    //red8 = (byte)((double)red5 / 31.0 * 255.0);
    //green8 = (byte)((double)green6 / 63.0 * 255.0);
    //blue8 = (byte)((double)blue5 / 31.0 * 255.0);

    // discard lesst significant bit
    // and convert these to 8-bit colors
    //red5 = (rgb565 >> 11) & 0b11110;
    //green6 = (rgb565 >> 5) & 0b111110;
    //blue5 = rgb565 & 0b11110;
    //red8 = (byte)((double)red5 / 30.0 * 255.0);
    //green8 = (byte)((double)green6 / 62.0 * 255.0);
    //blue8 = (byte)((double)blue5 / 30.0 * 255.0);

    // discard two lesst significant bits
    // and convert these to 8-bit colors
    //red5 = (rgb565 >> 11) & 0b11100;
    //green6 = (rgb565 >> 5) & 0b111100;
    //blue5 = rgb565 & 0b11100;
    //red8 = (byte)((double)red5 / 28.0 * 255.0);
    //green8 = (byte)((double)green6 / 60.0 * 255.0);
    //blue8 = (byte)((double)blue5 / 28.0 * 255.0);

    // discard two lesst significant bits
    // and convert these to 8-bit colors
    //red5 = (rgb565 >> 11) & 0b11000;
    //green6 = (rgb565 >> 6) & 0b10000;
    //blue5 = rgb565 & 0b111110;
    //red8 = (byte)((double)red5 / 24.0 * 255.0);
    //green8 = (byte)((double)green6 / 16.0 * 255.0);
    //blue8 = (byte)((double)blue5 / 62.0 * 255.0);

    // convert these to 256 colors
    //red8 = (byte)((double)red5 * 8.0 / 256.0);
    //green8 = (byte)((double)green6 * 4.0 / 256.0);
    //blue8 = (byte)((double)blue5 * 8.0 / 256.0);

    // above is too green compared to RA8875, try 6 bit blue
    //red5 = rgb565 >> 11;
    //green6 = (rgb565 >> 6) & 0b11111;
    //blue5 = rgb565 & 0b111111;
    //red8 = (byte)((double)red5 / 31.0 * 255.0);
    //green8 = (byte)((double)green6 / 31.0 * 255.0);
    //blue8 = (byte)((double)blue5 / 63.0 * 255.0);

    // still too green and red, try rgb466
    //red5 = rgb565 >> 12;
    //green6 = (rgb565 >> 5) & 0b111111;
    //blue5 = rgb565 & 0b111111;
    //red8 = (byte)((double)red5 / 15.0 * 255.0);
    //green8 = (byte)((double)green6 / 53.0 * 255.0);
    //blue8 = (byte)((double)blue5 / 63.0 * 255.0);


    // from RA8875.h
    // pg 73 of PA8875 datasheet confirms color bit structure
	  // to convert a 16bit color(565) into 8bit color(332)
  	// uint8_t _color16To8bpp(uint16_t color) => ((color & 0xe000) >> 8) | ((color & 0x700) >> 6) | ((color & 0x18) >> 3);
    red5 = (rgb565 & 0xe000) >> 13; // R2R1R0
    green6 = (rgb565 & 0x700) >> 8; // G2G1G0
    blue5 = (rgb565 & 0x18) >> 3; // B1B0

    // pg 75 of RA8875 datasheet shows 8-bit color mapping back to the display's 16-bit color table (R2R1R0R2R1G2G1G0G2G1G0B1B0B1B0B1)
    red8 = (byte)((double)((red5 << 2) | (red5 >> 1)) / 31.0 * 255.0); // R2R1R0R2R1
    green8 = (byte)((double)((green6 << 3) | green6) / 63.0 * 255.0); // G2G1G0G2G1G0
    blue8 = (byte)((double)((blue5 << 3) | (blue5 << 1) | (blue5 >> 1)) / 31.0 * 255.0); // B1B0B1B0B1

    //convert it to Windows.UI.Color
    //return Windows.UI.Color.FromArgb(255, red8, green8, blue8);
    return Windows.UI.Color.FromArgb(0, red8, green8, blue8);
  }
}
