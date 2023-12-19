using System;
using System.Threading.Tasks;
using System.Windows.Forms;


using Windows.Devices.Bluetooth.Rfcomm; // ....\Windows Kits\10\UnionMetadata\10.0.19041.0\Windows.winmd
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime; // ....\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll
using System.IO;
using System.Diagnostics;

namespace RFCOMM_cli
{

    public partial class Form1 : Form
    {
        private static string password = "XXXXXXXXXXXXXXXXXXXXXXXXX";
        private static string filter_device_name = "remote_bt_name";

        private static string device_name;
        private static bool connected = false;
        private RfcommDeviceService _service;
        private StreamSocket bt_socket;
        IOutputStream output;
        IInputStream input;

        private static string current_file;
        private static bool isPlaying = false;
        private static bool exitLoop = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RFCOMM_Initialize();
            
        }

        private string sizeHuman(long val)
        {

            string ssize = "";

            if (val >= Math.Pow(1024, 2))
            {
                ssize = $"{((double)val / Math.Pow(1024, 2)).ToString("#.##")} MB"; // MB

            }
            else if (val >= 1024)
            {
                ssize = $"{((double)val / 1024).ToString("#.##")} KB"; // KB
            }
            else
            {
                ssize = $"{val} B"; // B
            }

            return ssize;
        }

        async private void sendAudio(StreamSocket bt_socket, string device_name, string soundfile)
        {

            bool error = false;
            
            FileInfo fi = new FileInfo(soundfile);
            FileStream stream = new FileStream(soundfile, FileMode.Open, FileAccess.Read);
            int blocksize = 1024 * 4;
            byte[] block = new byte[blocksize];
            int readMax = 64;
            long n = 0;
            IBuffer buffer;

            _AppendText(richTextBox1, $"[<] Sending REINIT to server", Color.DarkGray);
            buffer = System.Text.Encoding.ASCII.GetBytes("--REINIT--").AsBuffer();
            try
            {
                await output.WriteAsync(buffer);
                await output.FlushAsync();
            }
            catch (Exception e)
            {
                _AppendText(richTextBox1, $"[x] {e.ToString()} ", Color.DarkRed);
                error = true;
            }

            if (!error)
            {

                try
                {
                    byte[] readblock = new byte[(uint)32];
                    await input.ReadAsync(readblock.AsBuffer(), (uint)32, InputStreamOptions.Partial);
                    _AppendText(richTextBox1, $"[>] Receiving {System.Text.Encoding.ASCII.GetString(readblock).TrimEnd('\0')} state", Color.Green);

                    _AppendText(richTextBox1, $"[<] Sending file ({soundfile}) to ({device_name})", Color.DarkGray);
                    _AppendText(richTextBox1, $"[<] Sending datas ({sizeHuman(fi.Length)}) to ({device_name})", Color.DarkGray);
                   

                    while (stream.Read(block, 0, blocksize) > 0 && !exitLoop)
                    {

                        buffer = block.AsBuffer();
                        try
                        {
                            await output.WriteAsync(buffer);
                            await output.FlushAsync();
                            isPlaying = true;
                        }
                        catch (Exception e)
                        {
                            error = true;
                            _AppendText(richTextBox1, $"[x] Total sent {sizeHuman(n)}", Color.Red);
                            _AppendText(richTextBox1, $"[x] {e.ToString()} ", Color.DarkRed);                            
                            break;
                        }

                        n += block.LongLength;
                        
                    }                    

                    isPlaying = false;
                    if (exitLoop)
                    {
                        exitLoop = false;
                    }

                }
                catch (Exception e)
                {
                    _AppendText(richTextBox1, $"[x] {e.ToString()} ", Color.DarkRed);
                    error = true;
                }

            }

            if (!error)
            {
                _AppendText(richTextBox1, $"[+] Finished, total sent {sizeHuman(n)}", Color.LightGreen);
            }                        

        }

        private void _AppendText(RichTextBox box, string text, Color color/*, Font font*/)
        {

            box.SelectionColor = color;
            //box.SelectionFont = font;
            box.AppendText($"{text}{System.Environment.NewLine}");
            box.SelectionColor = box.ForeColor;

            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;


        }

        async void RFCOMM_Initialize()
        {
            _AppendText(richTextBox1, $"[<] Please wait while searching available RFCOMM (SerialPort) service...{System.Environment.NewLine}", Color.DarkGray);
            var services = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                    RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort)
            );

            if (services.Count > 0)
            {

                for (var i = 0; i < services.Count; i++){

                    // Initialize the target Bluetooth BR device

                    var service = await RfcommDeviceService.FromIdAsync(services[i].Id);
                    _AppendText(richTextBox1, $"\t{services[i].Name}", Color.White);
                    _AppendText(richTextBox1, $"\t{services[i].Id}{System.Environment.NewLine}", Color.White);


                    _AppendText(richTextBox1, $"\tId                     : {service.Device.DeviceInformation.Id}", Color.White);
                    //_AppendText(richTextBox1, $"\tName                   : {service.Device.DeviceInformation.Name}", Color.White);
                    _AppendText(richTextBox1, $"\tName                   : {service.Device.Name}", Color.White);
                    _AppendText(richTextBox1, $"\tBluetoothAddress       : {service.Device.BluetoothAddress}", Color.White);
                    _AppendText(richTextBox1, $"\tBluetoothDeviceId      : {service.Device.BluetoothDeviceId.Id}", Color.White);
                    _AppendText(richTextBox1, $"\tClassOfDevice          : {service.Device.ClassOfDevice.RawValue}", Color.White);
                    _AppendText(richTextBox1, $"\tConnectionStatus       : {service.Device.ConnectionStatus.ToString()}", Color.White);
                    _AppendText(richTextBox1, $"\tCurrentStatus          : {service.Device.DeviceAccessInformation.CurrentStatus}", Color.White);
                    //richTextBox1.AppendText($"\tConnectionStatus  : {service.Device.ConnectionStatus.ToString()}\n");
                    //richTextBox1.AppendText($"\tDeviceId          : {service.Device.DeviceId}\n");
                    _AppendText(richTextBox1, $"\tIsPaired               : {service.Device.DeviceInformation.Pairing.IsPaired}", Color.White);
                    _AppendText(richTextBox1, $"\tProtectionLevel        : {service.Device.DeviceInformation.Pairing.ProtectionLevel}", Color.White);
                    //_AppendText(richTextBox1, $"\tHost CanonicalName     : {service.Device.HostName.CanonicalName}", Color.White);
                    _AppendText(richTextBox1, $"\tHost DisplayName       : {service.Device.HostName.DisplayName}", Color.White);
                    //richTextBox1.AppendText($"\tHost NetworkAdapterId  : {service.Device.HostName.IPInformation.NetworkAdapter.NetworkAdapterId}\n");
                    //_AppendText(richTextBox1, $"\tHost RawName           : {service.Device.HostName.RawName}", Color.White);
                    _AppendText(richTextBox1, $"\tHost Type              : {service.Device.HostName.Type}{System.Environment.NewLine}", Color.White);


                    if (String.Equals(service.Device.Name, filter_device_name))
                    {
                        _service = service;

                        _AppendText(richTextBox1, $"[<] Connecting to ({service.Device.Name}) RFCOMM (SerialPort) ...", Color.DarkGray);
                        bt_socket = new StreamSocket();
                        try
                        {
                            await bt_socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName, SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                            _AppendText(richTextBox1, $"[+] Connected to ({service.Device.Name}){System.Environment.NewLine}", Color.LightGreen);
                            _AppendText(richTextBox1, $"\tLocalAddress  : {bt_socket.Information.LocalAddress}", Color.White);
                            _AppendText(richTextBox1, $"\tLocalPort     : {bt_socket.Information.LocalPort}", Color.White);
                            _AppendText(richTextBox1, $"\tRemoteAddress : {bt_socket.Information.RemoteAddress}", Color.White);
                            _AppendText(richTextBox1, $"\tRemotePort    : {bt_socket.Information.RemotePort}{System.Environment.NewLine}", Color.White);


                            connected = true;
                            device_name = service.Device.Name;
                            this.Text = $"{this.Text} ➔ {device_name} - {bt_socket.Information.RemoteAddress}";

                            input = bt_socket.InputStream;
                            
                            byte[] readblock = new byte[(uint)32];
                            await input.ReadAsync(readblock.AsBuffer(), (uint)32, InputStreamOptions.Partial);

                            Debug.WriteLine(BitConverter.ToString(readblock).Replace("-", " "));
                            

                            string datas = System.Text.Encoding.ASCII.GetString(readblock).TrimEnd('\0');
                            _AppendText(richTextBox1, $"[>] Receiving {datas} from {device_name}", Color.Green);

                            if(String.Equals("HELLO", datas))
                            {
                                IBuffer buffer_password = System.Text.Encoding.ASCII.GetBytes($"oauth:{password}").AsBuffer();
                                try
                                {

                                    output = bt_socket.OutputStream;

                                    await output.WriteAsync(buffer_password);
                                    await output.FlushAsync();

                                    readblock = new byte[(uint)32];
                                    await input.ReadAsync(readblock.AsBuffer(), (uint)32, InputStreamOptions.Partial);

                                    Debug.WriteLine(BitConverter.ToString(readblock).Replace("-", " "));

                                    datas = System.Text.Encoding.ASCII.GetString(readblock).TrimEnd('\0');
                                    if(String.Equals("200:OK", datas))
                                    {
                                        _AppendText(richTextBox1, $"[>] Receiving {datas} from {device_name}", Color.LightGreen);
                                    }else if (String.Equals("403:BADAUTH", datas))
                                    {
                                        this.Text = "RaspberryPi (BT RFCOMM)";
                                        _AppendText(richTextBox1, $"[x] Receiving {datas} from {device_name}", Color.Red);
                                        connected = false;
                                        input.Dispose();
                                        output.Dispose();
                                        bt_socket.Dispose();
                                    }

                                }
                                catch (Exception e)
                                {
                                    this.Text = "RaspberryPi (BT RFCOMM)";
                                    _AppendText(richTextBox1, $"[x] {e.ToString()} ", Color.Red);                                    
                                }

                                
                            }
                            else if(String.Equals("REFUSED", datas))
                            {
                                this.Text = "RaspberryPi (BT RFCOMM)";
                                _AppendText(richTextBox1, $"[x] Not allowed to communicate with {device_name}", Color.Red);
                                connected = false;
                                input.Dispose();
                                bt_socket.Dispose();
                            }
                                                       
                                                                                  

                        }
                        catch (Exception e)
                        {
                            connected = false;
                            this.Text = "RaspberryPi (BT RFCOMM)";
                            _AppendText(richTextBox1, $"[x] Unable to connect to ({service.Device.Name}) !", Color.Red);
                            _AppendText(richTextBox1, $"[x] {e.ToString()} ", Color.DarkRed);

                        }
                        
                        break;
                    }
                }

            }
            else
            {
                _AppendText(richTextBox1, $"[x] No device with RFCOMM (SerialPort) service found !", Color.Red);
                _AppendText(richTextBox1, $"[x] Check if Bluetooth is activated on your system.", Color.Red);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (connected)
            {
                input.Dispose();
                output.Dispose();
                bt_socket.Dispose();
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            // scroll it automatically
            richTextBox1.ScrollToCaret();
        }

        private void btn_file_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    if(filePath.Length > 56)
                    {
                        tb_file.Text = $"{filePath.Substring(0,53)}...";
                    }
                    else
                    {
                        tb_file.Text = filePath;
                    }                    

                    current_file = filePath;
                }
            }
        }

        private void btn_play_Click(object sender, EventArgs e)
        {
            if (connected)
            {
                try
                {
                    if (!String.IsNullOrEmpty(tb_file.Text))
                    {
                        if (isPlaying)
                        {
                            exitLoop = true;
                            _AppendText(richTextBox1, $"[<] Current audio is played, exit loop...", Color.Yellow);

                            timer1.Interval = 500;
                            timer1.Start();
                        }
                        else
                        {
                            sendAudio(bt_socket, device_name, current_file);
                        }
                                              
                    }
                    else
                    {
                        MessageBox.Show(this, "Please select a file.", this.Text) ;
                    }
                    
                }
                catch
                {
                    _AppendText(richTextBox1, $"[x] Unable to send datas to {device_name}", Color.Red);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!isPlaying)
            {
                sendAudio(bt_socket, device_name, current_file);
                timer1.Stop();
            }
        }
    }
}
