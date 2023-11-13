using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleServer
{
    internal class Program
    {
        const int SERVER_PORT = 35072;
        static void ClientThread(object obj) 
        {
            Socket client = obj as Socket;
            byte[] buf = new byte[1024];
            int receivedMessageLength;
            try
            {
                while (client.Connected)
                {
                    receivedMessageLength = client.Receive(buf, 0, 1024, SocketFlags.None);
                    Console.WriteLine("Received message \"{0}\"", Encoding.UTF8.GetString(buf, 0, receivedMessageLength));
                }
            }
            catch (Exception ex)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            finally
            {
                Console.WriteLine("Work with client is over.");
            }
        }
        static void Main(string[] args)
        {
            // Устанавливаем для сокета локальную конечную точку
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = IPAddress.Parse("192.168.2.115");
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, SERVER_PORT);

            Console.WriteLine("IpAddres of server: " + ipAddr.ToString() +
                " port: " + SERVER_PORT.ToString());

            // Создаем сокет Tcp/Ip
            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Назначаем сокет локальной конечной точке и слушаем входящие сокеты
            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(10);

                // Начинаем слушать соединения
                while (true)
                {
                    Console.WriteLine("Ожидаем соединение через порт {0}", ipEndPoint);

                    // Программа приостанавливается, ожидая входящее соединение
                    Socket handler = sListener.Accept();
                    Thread clientThread = new Thread(ClientThread);
                    clientThread.Start(handler);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
