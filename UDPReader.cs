﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;


using System.Net;
using System.Net.Sockets;


namespace UDP_EASY
{
    class UDPReader
    {
        private const ushort _classId = 176;

        //DTstring "01"
        private static byte[] DT_Request = new byte[] { 48, 0,  49, 0, 0,0 };


        public int LocalPort = 5000;
        public int RemotePort = 5001;        

        private Dictionary<IPEndPoint, ClientData> clients = new Dictionary<IPEndPoint, ClientData>();

        UdpClient udp;

        private Queue queue = new Queue();


        GameStates writer = new GameStates();


        private int suicideTime = 0;
        Stopwatch suicideTimer = new Stopwatch();

        public int MaxWaitBeforeDisconnect = 12000;


        public UDPReader()
        {
            udp = new UdpClient(LocalPort);
            Reset();

            queue = Queue.Synchronized(queue);

            startRecieve();
            Ping();

            suicideTimer.Start();
        }


        public void Update()
        {
            processQueue();

            checkSuicide();

            checkClients();
        }


        private void processQueue()
        {
            while (queue.Count > 0)
            {
                NetData netData = (NetData)queue.Dequeue();

                uNetPackage packet = netData.Packet;
                IPEndPoint addr = netData.Address;

                if (packet.ClassID == 0)
                {
                    processDTPacket(packet);
                }

                if (packet.ClassID != 160) continue;

                bool registered = clients.ContainsKey(addr);
                if(registered)
                    clients[addr].LastUpdate.Restart();


                if (packet.CmdID == NetCommands.reg.CmdID())
                {  
                    if(!registered){
                        clients.Add(addr, new ClientData());
                        Console.WriteLine("Registered: " + addr.ToString());
                    }
                    Report();                         
                }

                if (packet.CmdID == NetCommands.puzzleState.CmdID())
                {
                    var data = packet.GetData();
                    
                    HackGame game = NetCommandsExtension.ToGame(BitConverter.ToInt16(data, 0));
                    HackGameState newState = NetCommandsExtension.GetState( BitConverter.ToInt16(data, 2) ); 

                    if( writer.SetState(game, newState, addr.Address) )                    
                        Console.WriteLine("state " + newState.ToString() + " for " + game.ToString() + " set from: " + addr.ToString());  
                    else                    
                        Console.WriteLine("state collision, resetting" );                                            

                    Report();
                }
                if (packet.CmdID == NetCommands.setLang.CmdID())
                {
                    writer.SetLanguage( BitConverter.ToInt16(packet.GetData(), 0) );
                    Report();
                }
                if(packet.CmdID  == NetCommands.ping.CmdID())
                {
                    Ping();
                }
                if (packet.CmdID == NetCommands.report.CmdID())
                {
                    Report();
                }
                if(packet.CmdID == NetCommands.unreg.CmdID())
                {
                    removeClient(addr);                    
                }
            }
        }

        private void processDTPacket(uNetPackage packet)
        {
            if (packet.CmdID == NetCommands.report.CmdID())
            {
                ReportLocal();
            }
            else if (packet.CmdID == NetCommands.setLang.CmdID())
            {
                int res;
                if (int.TryParse(Encoding.ASCII.GetString(packet.GetData()), out res))
                {
                    writer.SetLanguage(res);
                    Console.WriteLine("Lang: " + res);
                    Report();
                }
            }
            suicideTimer.Restart();
        }




        private void checkSuicide()
        {
            if (suicideTime == 0) return;

            if(suicideTimer.Elapsed.Seconds > suicideTime)
            {
                udp.Close();
                Environment.Exit(1);
            }
        }


        private void checkClients()
        {
            foreach (IPEndPoint client in clients.Keys.ToArray())
            {
                if (clients[client].LastUpdate.ElapsedMilliseconds > MaxWaitBeforeDisconnect)
                {
                    removeClient(client);
                }
            }
        }



        private void startRecieve()
        {            
            udp.BeginReceive(new AsyncCallback(recieveCallback), this);
        }
        /// <summary>
        /// Чтение полученных данных, добавление в очередь обработки пакета
        /// </summary>
        /// <param name="_iar"></param>
        private void recieveCallback(IAsyncResult _iar)
        {            
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, LocalPort);
            try
            {
                byte[] data = udp.EndReceive(_iar, ref RemoteIpEndPoint);
                startRecieve();
                
                uNetPackage packet;                
               
                if(IPAddress.IsLoopback(RemoteIpEndPoint.Address))
                {
                    if (data.SequenceEqual(DT_Request)) {
                        packet = new uNetPackage(0, NetCommands.report.CmdID());
                    }
                    else {//read DT data      Console.WriteLine("dt: " + Convert(data));
                        packet = new uNetPackage(0, NetCommands.setLang.CmdID(), Encoding.ASCII.GetBytes(Convert(data)));
                    }                        
                }
                else
                {
                    packet = new uNetPackage();
                    packet.BuildFromArray(data);                    
                }
                queue.Enqueue(new NetData(packet, RemoteIpEndPoint));
            }
            catch (Exception e)
            {
                Console.WriteLine("callback error " + e.Message);
                startRecieve();
            }
        }



        /// <summary>
        /// Крик на все сети о своём существовании
        /// </summary>
        public void Ping()
        {
            var ips = IPTools.GetAdresses();

            foreach (IPAddress ip in ips)
            {
                IPAddress broadcast = ip.GetBroadcastAddress(IPAddress.Parse(IPTools.ReturnSubnetmask(ip.ToString())));
                IPEndPoint point = new IPEndPoint(broadcast, RemotePort);
                try
                {
                    byte[] data = new uNetPackage(_classId, NetCommands.ping.CmdID()).ToArray();
                    udp.Send(data, data.Length, point);

                    Console.WriteLine("Pinged: " + point.Address.ToString());
                }
                catch (Exception e) {
                    Console.WriteLine("ping error " + e.Message);
                }
            }
        }
        /// <summary>
        /// Отчет о состоянии игр и языка
        /// </summary>
        public void Report()
        {
            byte[] report = writer.GetReport();
            byte[] data = new uNetPackage(_classId, NetCommands.report.CmdID(), report).ToArray();
            foreach (IPEndPoint point in clients.Keys)
            {
                try
                {
                    udp.Send(data, data.Length, point);
                    Console.WriteLine("Reported " + report.Length + " bytes to: " + point.Address.ToString() + " In game: " + clients[point].CurrentGame.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Report error " + e.Message);
                }
            }
        }

        private void ReportLocal()
        {
            IPEndPoint point = new IPEndPoint(IPAddress.Loopback, 5001);

            byte[] data;            

            data = toDTArray( "21" + ((writer.getGameState(HackGame.Disk) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "41" + ((writer.getGameState(HackGame.Monkey) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "42" + ((writer.getGameState(HackGame.Puzzle) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "43" + ((writer.getGameState(HackGame.Globe) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "44" + ((writer.getGameState(HackGame.RoundBall) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);
        }       

        /// <summary>
        /// полный сброс UDP
        /// </summary>
        public void Reset()
        {
            if(udp != null)
            {
                udp.Close();
                udp = new UdpClient(LocalPort);
            }
        }

        /// <summary>
        /// Закрывает UDP 
        /// </summary>
        private void Close()
        {
            udp.Close();
        }

        /// <summary>
        /// Удаляет клиента, закрывает его игру и отправляет отчёт
        /// </summary>
        /// <param name="client"></param>
        private void removeClient(IPEndPoint client)
        {
            writer.RemoveClient(client.Address);

            ClientData data;
            if ( !clients.TryGetValue(client, out data) )  return;

            short game = data.CurrentGame; 

            Console.WriteLine(client.ToString() + " disconnected, game: " + game.ToString());
            clients.Remove(client);

            Report();
        }  
        
        /// <summary>
        /// Вывод статуса в консоль
        /// </summary>
        private void writeConsoleStatus()
        {
            int curPos = Console.CursorTop;
            Console.CursorVisible = false;

            Console.SetCursorPosition(0, 0);
            string fill = "                      ";

            Console.WriteLine("UDP Reader" + fill);
            Console.WriteLine("Language " + writer.getLang() + fill);
            Console.WriteLine("Clients connected: " + clients.Count + fill);
            foreach(var pair  in clients)
            {
                Console.WriteLine(" " + pair.Key.ToString() + " | game: " + pair.Value.CurrentGame + fill);
            }

            Console.WriteLine("\nGame statuses");
            Console.WriteLine(" Disk game: " + writer.getGameState(HackGame.Disk).ToString() + fill);
            Console.WriteLine(" Monkey game: " + writer.getGameState(HackGame.Monkey).ToString() + fill);
            Console.WriteLine(" Puzzle game: " + writer.getGameState(HackGame.Puzzle).ToString() + fill);
            Console.WriteLine(" Globe game: " + writer.getGameState(HackGame.Globe).ToString() + fill);
            Console.WriteLine(" RB game: " + writer.getGameState(HackGame.RoundBall).ToString() + fill);

            Console.SetCursorPosition(0, curPos);
        }

        /// <summary>
        /// Конверсия сообщения в формат DT пакета
        /// Добавляет нулевой байт после каждого символа и 2 нулевых в конец
        /// </summary>
        /// <param name="message">сообщение для обраотки</param>
        /// <returns></returns>
        private static byte[] toDTArray(string message)
        {
            byte[] res = new byte[message.Length * 2 + 2];

            var arr = message.ToCharArray();
            for (int i = 0, j = 0; i < arr.Length; i++, j += 2)
            {
                res[j] = (byte)arr[i];
                res[j + 1] = 0;
            }
            res[res.Length - 1] = 0;
            res[res.Length - 2] = 0;
            return res;
        }

        /// <summary>
        /// Конверсия пакета DT контроллера в обычную строку
        /// Удаляет нулевые байты используемые контроллером после кажого символа и в конце строки
        /// </summary>
        /// <param name="DT_Packet">данные присланные контроллером</param>
        /// <returns>строка</returns>
        public static string Convert(byte[] DT_Packet)
        {
            string data = "";            
            foreach (var b in DT_Packet)
            {
                if (b == 0) continue;
                data += (char)b;
            }
            return data;
        }

    }
}
