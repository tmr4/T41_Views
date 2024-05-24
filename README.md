# T41_Views

View and control code for a T41-EP Control App.

![Screenshot of T41 Control App](https://preview.redd.it/x0e6ib7k212d1.png?width=2021&format=png&auto=webp&s=74b873a107c8501048b4d5318156ffb1b385c7fa)

## Features

The app communicates with the T41 over USB (SerialUSB1 on Teensy; you must select `Dual` or `Triple` USB Type when compiling the [T41 software](https://github.com/tmr4/T41_SDR/tree/dev/v0.1)).

The control app has the following features:

  * Live view of frequency and audio spectrums, S-meter, waterfall, and filter bandwidth
  * Live updates can be paused or started with the button at the lower left of the waterfall
  * T41 clock set to PC time upon connection
  * Change frequency of active VFO by the active increment with the mouse wheel
  * Change the active increment with a mouse click (center or fine tune indicated by the green highlight in the info box)
  * Zero out the 1000s portion of the active VFO with a right-mouse click
  * Reset tuning of the active VFO with a mouse click on the Center Frequency
  * Switch to the inactive VFO with a mouse click on the inactive VFO
  * Set the noise floor with a mouse click on NF Set and a mouse wheel (this occurs live unlike the base T41 software which stops operation while the noise floor is adjusted)
  * Change the following up or down with the mouse wheel (on the corresponding indicator):
    * Band
    * Operating mode
    * Demodulation mode
    * Transmit power
    * Volume
    * AGC
    * Center and fine tune increment

## Building the app

 I'm still determining the best way to make the app available.  In the meantime, you can build it yourself using the community edition of [Visual Studio 2022 with C#](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here?tabs=vs-2022-17-10).  Start a new project with the C# project template and copy this repository's files into the Views folder.  You'll likely need to load some packages (System.IO.Ports and Microsoft.Graphics.Win2D for example).  I'll update this after I do a clean install to test this approach.  Ultimately I'd like to make the packaged app available, but I need to do some research and testing for that.

## Limitations

This is a work in progress and many T41 features are not available through the control app.  Currently the control app receives data from the T41 and the T41 responds to commands from the control app once per loop or about 4 times per second.  This is fine for normal operations but at this rate the T41 response lags when sending many commands in succession, for example adjusting the frequency or volume.  The T41 will eventually process all of the commands, usually in a couple of seconds or so.  I'll eventually get around to testing how far I can push the communication rate.

My T41 software is required for the control app to function. The structure of the EEPROM is different from the original T41 software so a clean install is required.  Note that some T41 functionality may not work in my version probably because I made breaking changes and haven't tested things that don't interest me at the moment.  

Unfortunately, a simple copy/paste port to version V049.2 isn't possible.  While the main T41 USB serial code is in a single file, there are other changes to the T41 code, and I've made many changes to my version which I utilize here.  Still, if you're interested in the app and are familiar with the T41 software, porting this to version V049.2 shouldn't be too difficult.

## FAQs

### Why not C++ instead of C#?

I started out using C++ but had difficulties getting a few things to work.  Turns out C# is more fully supported in WinUI 3 than C++.

### Why not .Net Maui instead of WinUI 3?

I looked into .Net Maui at the start of this project because of it's multiple platform design.  However, it's controls were just enough different than the WinUI controls I was familiar with to require more work than I wanted to get this up and running.  Maui has more robust support in Visual Studio than WinUI, including hot reboot, but that wasn't enough to get me to switch.  Also it appeared at first glance all of the controls I needed/wanted weren't available in Maui.  Finally, I don't have a need for a multi-platform app.

### Why not use V049.2 or T41EEE instead of your own T41 software version?

Probably timing more than anything else.  It's still too early in development to have multiple people working on the same version of software without constantly having to deal with breaking changes.  I choose to spend my time on more productive efforts.  And off course there still isn't an official repository for V049.2.

### Why not use Kenwood computer control commands instead of you own?

You can probably see from the code that I started with a subset of Kenwood commands but after a while it seemed silly to continue with this when I didn't have an immediate need.  That said, it would take a much work to get many of the app's features working other radios using similar protocols.

### Does the app use compressed data streams?

No.  There isn't a large amount of data being transferred to the control app, mostly just the 512-byte frequency spectrum and ~270-byte audio spectrum every quarter second or so and much less going in the other direction.  I want to transfer audio for data modes as well, but even with that I don't think compression will be needed over the USB connection.  Also, the work required to compress the data might bog down the Teensy.  I'll see how it goes when I get there.
