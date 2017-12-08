using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/** Traceroute 
 * A traceroute application that performs traceroute for both IPv4 and IPv6 addresses for a given hostname
 * 
 * Author: Wm. Blake Sullivan
 */
namespace Traceroute
{
    /// The main program class
    class Program
    {
        static void Main(string[] args)
        {

            ///checking for proper arguments, exiting if incorrect
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Traceroute hostname");
                Environment.Exit(1);
            }

            ///get machine hostname
            string local = Dns.GetHostName();

            ///print out local addresses found
            Console.WriteLine(local + " addresses\n");
            IPAddress[] localAddressList = Dns.GetHostAddresses(local);
            IPAddress localv4 = null;
            IPAddress localv6 = null;

            ///print loop
            foreach (IPAddress ip in localAddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ///use for confirming that local and host have a valid IPv4 address
                    localv4 = ip;
                    Console.WriteLine("IPv4: " + localv4);
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ///use for confirming that local and host have a valid IPv6 address
                    localv6 = ip;
                    Console.WriteLine("IPv6: " + localv6);
                }
            }


            Console.WriteLine();

            ///get target hostname
            string hostname = args[0];

            ///print out host addresses found
            Console.WriteLine(hostname + " addresses\n");
            IPAddress[] addressList = Dns.GetHostAddresses(hostname);
            IPAddress hostv4 = null;
            IPAddress hostv6 = null;

            ///print loop
            foreach (IPAddress ip in addressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ///used for sending Pings to the target over IPv4
                    hostv4 = ip;
                    Console.WriteLine("IPv4: " + hostv4);
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ///used for sending Pings to the target over IPv6
                    hostv6 = ip;
                    Console.WriteLine("IPv6: " + hostv6);
                }
            }

            Console.WriteLine();

            ///perform IPv4 traceroute if able
            if (localv4 != null && hostv4 != null)
            {
                Console.WriteLine("Can perform IPv4 Traceroute");
                performTraceroute(hostv4);
            }

            Console.WriteLine();

            ///perform IPv6 traceroute if able
            if (localv6 != null && hostv6 != null)
            {
                Console.WriteLine("Can perform IPv6 Traceroute");
                performTraceroute(hostv6);
            }
        }

        /* <code>performTraceroute(IPAddress target)</code>
         * 
         * <param> <code>target</code>: The IPAddress of the host you want to perform the traceroute to
         */
        private static void performTraceroute(IPAddress target)
        {
            ///Ping object created for pinging the host
            Ping tr = new Ping();

            ///tracks the consecutive timeouts of traceroute
            ///after 3 consecutive timeouts, the current traceroute will cancel
            int timeouts = 0;
            for (int i = 1; i <= 30; i++)
            {
                ///sets the time-to-live, or hop limit, of the Ping
                PingOptions po = new PingOptions(i, true);

                Console.Write(i + "\t");

                PingReply pr = null;
                IPAddress pingAddress = null;

                ///tracks the number of missed packets in a single hop test of the traceroute
                ///if there are 3 consecutive timed-out packets sent, this counts as a timeout for the <code>timeouts</code> int
                int retries = 0;
                for (int j = 0; j < 3; j++)
                {

                    ///Used for getting the time in ms for each node since the PingReply only returns RoundTrip time if it successfully reaches its destination
                    Stopwatch ms = new Stopwatch();
                    ms.Start();

                    ///send the Ping, wait for PingReply
                    pr = tr.Send(target, 6000, new byte[52], po);

                    ///this gives us the roundtrip time to the particular node
                    ms.Stop();

                    ///this means the Ping did not time out
                    if (pr.Status == IPStatus.TtlExpired || pr.Status == IPStatus.Success)
                    {
                        timeouts = 0;
                        Console.Write(ms.ElapsedMilliseconds + "ms\t");
                        if (pingAddress == null)
                            pingAddress = pr.Address;
                        continue;
                    }
                    Console.Write("*\t");
                    retries++;
                }

                ///hop had a timeout
                if (retries == 3)
                {
                    Console.WriteLine("Request timeout.");
                    timeouts++;

                    ///traceroute had 3 consecutive tiemouts, stopping traceroute
                    if (timeouts == 3)
                    {
                        Console.WriteLine("3 consecutive timeouts, stopping traceroute.");
                        break;
                    }
                    continue;
                }

                ///try to print the hostname and IPAddress
                IPHostEntry hopHost = null;
                try
                {
                    hopHost = Dns.GetHostEntry(pingAddress);
                    Console.WriteLine(hopHost.HostName + " [" + pr.Address.ToString() + "]");
                }

                ///couldn't get a hostname, so just print the IPAddress
                catch (Exception e)
                {
                    Console.WriteLine(pr.Address.ToString());
                }

                ///we reached the target and teh traceroute is done
                if (pr.Status == IPStatus.Success)
                    break;
            }
        }
    }
}
