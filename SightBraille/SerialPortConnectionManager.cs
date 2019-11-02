using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SightBraille
{
    public class SerialPortConnectionManager
    {

        public ObservableCollection<SerialPortConnection> SerialPorts = new ObservableCollection<SerialPortConnection>();

        public void InitPortManager()
        {
            SerialPortService.PortsChanged += new EventHandler<PortsChangedArgs>(SerialPortChanged);
            SerialPortChanged(null, null);
        }

        public SerialPortConnectionManager()
        {

        }
        private void SerialPortChanged(object obj, PortsChangedArgs e)
        {
            List<string> newPorts = new List<string>(SerialPort.GetPortNames());

            foreach (string port in SerialPort.GetPortNames())
            {
                foreach (SerialPortConnection connection in SerialPorts)
                {
                    if (connection.PortName == port)
                    {
                        newPorts.Remove(port);
                    }
                }
            }

            foreach (string newPort in newPorts)
            {
                SightBrailleApp.Current.Dispatcher.Invoke(() =>
                {
                    this.SerialPorts.Add(new SerialPortConnection(newPort));
                });

            }

            List<SerialPortConnection> toRemove = new List<SerialPortConnection>();

            foreach (SerialPortConnection actualConnection in SerialPorts)
            {
                bool isStillConnected = false;
                foreach (string port in SerialPort.GetPortNames())
                {
                    if (actualConnection.PortName == port)
                    {
                        isStillConnected = true;
                        break;
                    }
                }
                if (!isStillConnected)
                {
                    toRemove.Add(actualConnection);
                }
            }

            foreach (SerialPortConnection remove in toRemove)
            {
                SightBrailleApp.Current.Dispatcher.Invoke(() => {
                    SerialPorts.Remove(remove);
                    remove.Disconnect();
                });
            }

            foreach (SerialPortConnection connection in SerialPorts)
            {
                connection.UpdateConnectionAsync();
            }
        }

    }
}
