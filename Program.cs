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
        static Dictionary<string, Socket> _clients = new Dictionary<string, Socket>();
        const int SERVER_PORT = 35072;
        enum EClientState 
        {
            WaitingForAutorization,
            Chatting
        }
        enum EChatCode 
        {
            Autorization = 192,
            Chatting = 191,
            EmptyNickName = 3,
            AutorizationFinishedGood = 0,
            MessageAdded = 0,
            StringAlreadyConsist = 1,
            WrongCommandCode = 4
        }
        static void ClientThread(object obj) 
        {
            Socket client = obj as Socket;
            EClientState state = EClientState.WaitingForAutorization;
            byte[] buf = new byte[1024];
            int receivedMessageLength;
            string nickName="";
            string message;
            try
            {
                while (client.Connected)
                {
                    receivedMessageLength = client.Receive(buf, 0, 1024, SocketFlags.None);
                    if (receivedMessageLength > 0)
                    {
                        switch (state)
                        {
                            case EClientState.WaitingForAutorization:
                                switch ((EChatCode)buf[0])
                                {
                                    case EChatCode.Autorization:
                                         nickName = Encoding.UTF8.GetString(buf, 1, receivedMessageLength - 1);
                                        if (string.IsNullOrWhiteSpace(nickName))
                                        {
                                            client.Send(new byte[1] { (byte)EChatCode.EmptyNickName });
                                        }
                                        else if (_clients.Keys.Contains(nickName))
                                        {
                                            client.Send(new byte[1] { (byte)EChatCode.StringAlreadyConsist });
                                        }
                                        else
                                        {
                                            client.Send(new byte[1] { (byte)EChatCode.AutorizationFinishedGood });
                                            _clients.Add(nickName, client);
                                            state = EClientState.Chatting;
                                        }
                                        break;
                                    case EChatCode.Chatting:
                                    default:
                                        client.Send(new byte[1] { (byte)EChatCode.WrongCommandCode });
                                        break;

                                }
                                break;
                            case EClientState.Chatting:
                                switch ((EChatCode)buf[0]) 
                                {
                                    case EChatCode.Chatting:
                                    
                                        message =
                                            Encoding.UTF8.GetString(buf, 1, receivedMessageLength - 1);
                                        Console.WriteLine("{0}:{1}", nickName, message);
                                        client.Send(new byte[1] { (byte)EChatCode.MessageAdded });
                                        break;
                                    default:
                                        client.Send(new byte[1] { (byte)EChatCode.WrongCommandCode });
                                        break;
                                }
                                break;
                            default:
                                client.Send(new byte[1] { (byte)EChatCode.WrongCommandCode });
                                break;
                        }

                    }
                
                }
            }
            catch (Exception ex)
            {
                if (_clients.Keys.Contains(nickName)) 
                {
                    _clients.Remove(nickName);
                }
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
