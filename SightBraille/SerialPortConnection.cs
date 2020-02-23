using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static SightBraille.BrailleEditor;
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
                                TriggerAutoSelect();
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

        private void TriggerAutoSelect()
        {
            int i = 0;
            foreach (SerialPortConnection connection in ((SightBrailleApp.Current as SightBrailleApp).ConnectionManager.SerialPorts))
            {
                if (connection.State == PortConnectionState.CONNECTED)
                {
                    ((SightBrailleApp.Current as SightBrailleApp).MainWindow as EditorWindow).AutoSelectSerialPort(i);
                }
                i++;
            }
        }

        public void SendInstructions()
        {
            if (!isBusy)
            {
                this.isBusy = true;
                //Send instructions

                string instructions = (Application.Current as SightBrailleApp).Document.GetInstructions();
                Port.Write(instructions);
            }
            this.isBusy = false;
        }

        private List<BrailleSymbol> CalculateSymbols(Document document, int row)
        {
            List<BrailleCharacter> characters = document.Characters[row];
            List<BrailleSymbol> symbols = new List<BrailleSymbol>();
            int i = 0;
            int length = characters.Count;
            BrailleCharacter before = BlankCharacter;
            BrailleCharacter beforebefore = BlankCharacter;
            BrailleCharacter after;

            //int symbolCountBefore = Symbols[row].Count;
            foreach (BrailleCharacter character in characters)
            {
                if (i < length - 1)
                {
                    after = characters[i + 1];
                }
                else
                {
                    after = BlankCharacter;
                }

                foreach (BrailleSymbolSlot slot in character.GetSymbols(beforebefore, before, after))
                {
                    symbols.Add(slot.GetSymbol());

                }

                //Next
                beforebefore = before;
                before = character;
                i++;
            }

            return symbols;

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
