using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static SightBraille.SightBrailleApp;

namespace SightBraille
{
    public class SerialPortConnection : INotifyPropertyChanged
    {

        public string PortName { get; set; }
        public SerialPort Port;

        public PortConnectionState State = PortConnectionState.CLOSED;

        private bool isBusy = false;

        public Brush DotColor
        {
            get
            {
                if (State == PortConnectionState.CONNECTED)
                {
                    return Brushes.Green;
                }
                else if (State == PortConnectionState.CHECKING)
                {
                    return Brushes.Coral;
                }
                return Brushes.Gray;
            }
            set { }
        }

        public SerialPortConnection(string portName)
        {
            this.PortName = portName;
            this.Port = new SerialPort(PortName, 9600, Parity.None, 8, StopBits.One);
            NotifyPropertyChanged("PortName");
        }

        public void UpdateConnectionAsync()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (object sender, DoWorkEventArgs e) => { UpdateConnection(); };
            worker.RunWorkerAsync(10000);
        }

        public void UpdateConnection()
        {
            if (!isBusy)
            {
                isBusy = true;
                if (State == PortConnectionState.CLOSED)
                {
                    if (!Port.IsOpen)
                    {
                        Console.WriteLine("Checking for port " + PortName);
                        setConnectionState(PortConnectionState.CHECKING);
                        try
                        {
                            Port.Open();
                            if (checkIsBraillePrinter())
                            {
                                setConnectionState(PortConnectionState.CONNECTED);
                            }
                            else
                            {
                                setConnectionState(PortConnectionState.CLOSED);
                                this.Disconnect();
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    else
                    {
                        Console.WriteLine(PortName + " is already opened");
                        setConnectionState(PortConnectionState.CLOSED);
                    }
                }
                else if (State == PortConnectionState.CONNECTED)
                {
                    if (!checkIsBraillePrinter())
                    {
                        setConnectionState(PortConnectionState.CLOSED);
                    }
                }
            }
            isBusy = false;
        }

        private void sendInstructions()
        {
            if (!isBusy)
            {
                this.isBusy = true;
                //Send instructions
            }
            this.isBusy = false;
        }

        private bool checkIsBraillePrinter()
        {
            try
            {
                Port.DiscardInBuffer();
                Port.DiscardOutBuffer();
                Port.WriteLine("IsBraillePrinterCheck");
                Port.ReadTimeout = 5000;
                string response = Port.ReadLine();
                Console.WriteLine("Response: " + response);
                if (response.Contains("IsBraillePrinterConfirmation"))
                {
                    setConnectionState(PortConnectionState.CONNECTED);
                    Console.WriteLine("Connected!");
                    return true;
                }
            }
            catch (Exception e)
            {
                try
                {
                    Port.Close();
                }
                catch (IOException)
                {

                }
                Console.WriteLine("An error occured while checking port");
                Console.WriteLine(e.Message);
            }

            return false;
        }

        public void Disconnect()
        {
            this.Port.Close();
        }

        private void setConnectionState(PortConnectionState state)
        {
            this.State = state;
            this.NotifyPropertyChanged("State");
            this.NotifyPropertyChanged("DotColor");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public enum PortConnectionState
    {
        CLOSED,
        CHECKING,
        CONNECTED
    }
}
