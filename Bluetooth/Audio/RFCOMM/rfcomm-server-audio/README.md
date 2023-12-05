# RASPBERRY

```
---------------
CONFIGURATION :
---------------
sudo nano /etc/systemd/system/dbus-org.bluez.service
* ExecStart=/usr/libexec/bluetooth/bluetoothd --compat
sudo chmod 777 /var/run/sdp
sdptool add --channel=22 SP

sudo apt-get install python3-bluez python3-pyaudio

---------------
    USAGE     :
---------------
python rfcomm-server-audio.py
```

