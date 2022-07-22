# RASPBERRYPI-ZERO , WAVESHARE 3.5" TFT SCREEN MODEL B v2 + USB COMPOSITE GADGET

```
git clone https://github.com/waveshare/LCD-show.git
cd LCD-show/
```

### IMPORTANT : CONNECT TO INTERNET BEFORE DOING THIS :
```
chmod +x LCD35B-show-V2
./LCD35B-show-V2
```

### UNPLUG THE DEVICE AND PLUG THE SD WITH USB ADAPTER TO YOUR COMPUTER TO ACCESS TO FILES:

sudo nano /boot/cmdline.txt

```
console=serial0,115200 console=tty1 root=/dev/mmcblk0p2 rootfstype=ext4 elevator=deadline fsck.repair=yes rootwait quiet splash modules-load=dwc2 fbcon=map:10 fbcon=font:ProFont6x11
```

sudo nano /boot/config.txt

```
framebuffer_width=480
framebuffer_height=320
hdmi_force_hotplug=1
dtparam=i2c_arm=on
dtparam=spi=on
enable_uart=1
dtoverlay=dwc2
dtparam=audio=on
dtoverlay=waveshare35b-v2
hdmi_force_hotplug=1
hdmi_group=2
hdmi_mode=1
hdmi_mode=87
hdmi_cvt 480 320 60 6 0 0 0
hdmi_drive=2
display_rotate=1
```

### PLUG AGAIN THE DEVICE WITH USB-OTG AND CONNECT TO SSH THEN :

sudo nano /usr/share/X11/xorg.conf.d/99-fbdev.conf
```
Section "Device"
   Identifier "waveshare35b-v2"
   Driver "fbdev"
   Option "fbdev" "/dev/fb1"
EndSection
```

Clear your .Xauthority file
```
mv .Xauthority .Xauthority.backup
```

### OPTIONAL : ROTATE THE SCREEN IF NECESSARY
### WARNING : maybe cmdline.txt and/or config.txt will be overwrite !
```
sudo ./LCD35B-show-V2 90
```

# Ref: 

https://tejaswid.github.io/wikitm/rpi/pi-tft-display

https://www.waveshare.com/wiki/3.5inch_RPi_LCD_(A)#Image

