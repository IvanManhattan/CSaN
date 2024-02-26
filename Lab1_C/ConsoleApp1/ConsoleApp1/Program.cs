
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace ConsoleApp1 {
    internal class Program {
        
        // Send ARP
        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int destIP, int srcIP, byte[] macAddr, ref int macAddrLen);
        
        private static object _locker = new object();

        public static byte[] GetFirstIpAddress(byte[] value, byte[] mask) {
            byte[] result = new byte[4];

            for (int i = 0; i < 4; i++) {
                result[i] = (byte)(value[i] & mask[i]);
            }

            result[3]++;
            return result;
        }

        public static byte[] GetLastIpAddress(byte[] value, byte[] mask) {
            byte[] result = new byte[4];

            for (int i = 0; i < 4; i++) {
                result[i] = (byte)(value[i] | (~mask[i]));
            }

            result[3]--;
            return result;
        }

        public static void Send(object data) {
            byte[] startIpBuffer = new byte[4];
            startIpBuffer[0] = ((byte[])data)[0];
            startIpBuffer[1] = ((byte[])data)[1];
            startIpBuffer[2] = ((byte[])data)[2];
            startIpBuffer[3] = ((byte[])data)[3];


            byte[] macAddress = new byte[6];
            int lengthMacAddress = macAddress.Length;

            IPAddress destination =
                IPAddress.Parse(
                    $"{startIpBuffer[0]}.{startIpBuffer[1]}.{startIpBuffer[2]}.{startIpBuffer[3]}"
                );

            int retARP = SendARP(
                BitConverter.ToInt32(destination.GetAddressBytes(), 0), 0, macAddress, ref lengthMacAddress
            );
            lock (_locker) {
                if (retARP == 0) {
                    String macAddressStr = BitConverter.ToString(macAddress, 0, lengthMacAddress);
                    if (macAddress != null) {
                        Console.WriteLine(
                            $"{startIpBuffer[0]}.{startIpBuffer[1]}.{startIpBuffer[2]}.{startIpBuffer[3]}  | {macAddressStr}"
                        );
                    }
                    else {
                        Console.WriteLine(
                            $"{startIpBuffer[0]}.{startIpBuffer[1]}.{startIpBuffer[2]}.{startIpBuffer[3]}  | No creator found"    
                        );
                    }
                }
                else {
                    Console.WriteLine(
                        $"{startIpBuffer[0]}.{startIpBuffer[1]}.{startIpBuffer[2]}.{startIpBuffer[3]} | none"
                    );
                }

                Thread.Sleep(200);

            }
        }

        public static void CheckNetwork(byte[] startIpAddress, byte[] endIpAddress) {

            while ((startIpAddress[0] == endIpAddress[0] 
                   && startIpAddress[1] == endIpAddress[1] 
                   && startIpAddress[2] == endIpAddress[2]
                   && startIpAddress[3] - 1 == endIpAddress[3] - 1) == false) {
                int i = 0;
                Thread myThread = new Thread(Send);
                
                myThread.Start(startIpAddress);
                startIpAddress[3]++;
                if (startIpAddress[3] == 0) { 
                    startIpAddress[2]++; 
                    if (startIpAddress[2] == 0) {
                        startIpAddress[1]++;
                        if (startIpAddress[1] == 0) {
                            startIpAddress[0]++;
                        }
                    }
                }

                i++;
            }
        }


        static void Main(string[] args) {
            byte[][] ipAddresses = new byte[10][];
            byte[][] masks = new byte[10][];
            string[] ipAddressesStrings = new string[10];

            byte[][] ipRangeToCheck = new byte[2][];

            byte[] ipTest = new byte[4];
            int i = 0;

            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
                if (networkInterface.OperationalStatus == OperationalStatus.Up) {
                    IPInterfaceProperties adp = networkInterface.GetIPProperties();
                    UnicastIPAddressInformationCollection adpc = adp.UnicastAddresses;
                    foreach (UnicastIPAddressInformation uip in adpc) {
                        if (networkInterface.GetPhysicalAddress().ToString() != "") {
                            if (uip.Address.AddressFamily == AddressFamily.InterNetwork) {
                                Console.Write($"\n\n{networkInterface.Name} \n");
                                byte[] valIp = uip.Address.GetAddressBytes();
                                byte[] mask = uip.IPv4Mask.GetAddressBytes();
                                ipAddresses[i] = uip.Address.GetAddressBytes();
                                masks[i] = uip.IPv4Mask.GetAddressBytes();
                                ipAddressesStrings[i] = networkInterface.Name;
                                i++;
                            
                                Console.WriteLine("ip: " + uip.Address.ToString());
                                Console.WriteLine("Mask: " + uip.IPv4Mask.ToString());
                                Console.Write("First address: \n");
                            
                                ipRangeToCheck[0] = GetFirstIpAddress(valIp, mask);
                                Console.Write(
                                    $"{ipRangeToCheck[0][0]}.{ipRangeToCheck[0][1]}.{ipRangeToCheck[0][2]}.{ipRangeToCheck[0][3]}"
                                );
                                ipRangeToCheck[1] = GetLastIpAddress(ipRangeToCheck[0], mask);
                                Console.Write("\nLast address: \n");
                                Console.Write(
                                    $"{ipRangeToCheck[1][0]}.{ipRangeToCheck[1][1]}.{ipRangeToCheck[1][2]}.{ipRangeToCheck[1][3]}"
                                );
                            }
                        }
                    }
                }
            }

            for (int j = 0; j < i; j++) {
                Console.Write($"\n{j + 1}) ");
                Console.WriteLine($"Name - {ipAddressesStrings[j]}");
                Console.WriteLine($"{ipAddresses[j][0]}.{ipAddresses[j][1]}.{ipAddresses[j][2]}.{ipAddresses[j][3]}\n");
            }
            
            int index = 1;
            Console.WriteLine("Enter the number of network");
            do {
                index = Convert.ToInt32(Console.ReadLine());
            } while (index > i || index < 1);
                
            index--;
                
            byte [][] ipRangeCheck = new byte [2][];
            
            ipRangeCheck[0] = GetFirstIpAddress(ipAddresses[index], masks[index]);
            ipRangeCheck[1] = GetLastIpAddress(ipAddresses[index], masks[index]);
            
            Console.WriteLine($"{ipRangeCheck[0][0]}.{ipRangeCheck[0][1]}.{ipRangeCheck[0][2]}.{ipRangeCheck[0][3]}\n");
            Console.WriteLine($"{ipRangeCheck[1][0]}.{ipRangeCheck[1][1]}.{ipRangeCheck[1][2]}.{ipRangeCheck[1][3]}\n");
            
            CheckNetwork(GetFirstIpAddress(ipAddresses[index], masks[index]), 
                    GetLastIpAddress(ipAddresses[index], masks[index])
            );
            
            Console.ReadLine();
            
        }
    }
}

