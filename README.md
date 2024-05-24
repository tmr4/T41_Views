# T41_Views

View and control code for a T41-EP Control App.

![Screenshot of T41 Control App](https://preview.redd.it/x0e6ib7k212d1.png?width=2021&format=png&auto=webp&s=74b873a107c8501048b4d5318156ffb1b385c7fa)

## Features

The app communicates with T41 over USB (SerialUSB1 on Teensy, must select `Dual` or `Triple` USB Type when compiling the [T41 software](https://github.com/tmr4/T41_SDR/tree/dev/v0.1). The control app has the following features:

  * Live view of frequency and audio spectrums, S-meter, waterfall, and filter bandwidth
  * Live updates can be paused or started with the button at the lower left of the waterfall
  * T41 clock set to PC time upon connection
  * Change frequency of active VFO by the active increment with the mouse wheel
  * Change the active increment with a mouse click (center or fine tune indicated by the green highlight in the info box)
  * Zero out the 1000s portion of the active VFO with a right-mouse click
  * Reset tuning of the active VFO with a mouse click on the Center Frequency
  * Switch to the inactive VFO with a mouse click on the inactive VFO
  * Set the noise floor with a mouse click on NF Set and a mouse wheel in the frequency spectrum (this occurs live unlike the base T41 software which stops operation while the noise floor is adjusted)
  * Change the following up or down with the mouse wheel (on the corresponding indicator):
    * Band
    * Operating mode
    * Demodulation mode
    * Transmit power
    * Volume
    * AGC
    * Center and fine tune increment

## Building the app

 I'm still determining the best way to make the app available.  You can build it using the community edition of {}
 I'm still determining the best way to make the app available.  In the meantime you can build it yourself using the community edition of [Visual Studio 2022 with C#](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here?tabs=vs-2022-17-10).  Start a new project with the C# project template and copy this repository's files into the Views folder.  You'll likely need to load some packages (System.IO.Ports and Microsoft.Graphics.Win2D for example).  I'll update this after I do a clean install to test this approach.  Ultimately I'd like to make the packaged app available, but I need to do some research and testing for that.