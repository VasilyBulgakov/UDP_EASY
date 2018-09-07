using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private System.Collections.Generic.Dictionary<IPEndPoint, ClientData> clients = new System.Collections.Generic.Dictionary<IPEndPoint, ClientData>();

        UdpClient udp;

        private Queue queue = new Queue();


        GameStateWriter writer = new GameStateWriter();


        private int suicideTime = 0;
        System.Diagnostics.Stopwatch suicideTimer = new System.Diagnostics.Stopwatch();

        public int MaxWaitBeforeDisconnect = 12000;


        public UDPReader()
        {
            udp = new UdpClient(LocalPort);
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
            //writer.TryWrite();

            //writeConsoleStatus();
        }


        private void processQueue()
        {
            while (queue.Count > 0)
            {
                uNetPackage packet = (uNetPackage)queue.Dequeue();
                if(packet.ClassID == 0)
                {
                    if(packet.CmdID == NetCommands.report.CmdID())
                    {
                        ReportLocal();
                        suicideTimer.Restart();
                    }
                }

                if (packet.ClassID != 160) continue;           

                if (packet.CmdID == NetCommands.reg.CmdID())
                {
                    try
                    {
                        string str = Encoding.ASCII.GetString(packet.GetData());
                        var clientPoint = new IPEndPoint(IPAddress.Parse(str), RemotePort);
                        
                        // Register client or restart timer if already registered
                        if ( !clients.ContainsKey(clientPoint) )
                        {
                            clients.Add(clientPoint, new ClientData());                            
                            Console.WriteLine("Registered: " + str);
                        }
                        else {
                            // Console.WriteLine("Already registered: " + str);
                            clients[clientPoint].LastUpdate.Restart();
                        }

                        Report();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }            
                }

                if (packet.CmdID == NetCommands.puzzleState.CmdID())
                {
                    var data = packet.GetData();
                    short gameID = BitConverter.ToInt16(data, 0);
                    short newState = BitConverter.ToInt16(data, 2);              
                    

                    var clientPoint = new IPEndPoint(IPAddress.Parse(Encoding.ASCII.GetString(data, 4, data.Length - 4)), RemotePort);
                    Console.WriteLine("state " + newState.ToString() + " for " + gameID.ToString() + " set from: " + clientPoint.ToString() );

                    if( !clients.ContainsKey(clientPoint)) {
                        clients.Add(clientPoint, new ClientData());
                    }

                    if (newState == HackGameState.InProgress.GetID())
                        clients[clientPoint].CurrentGame = gameID;
                    else
                        clients[clientPoint].CurrentGame = 0;

                    if( !writer.SetState(gameID, NetCommandsExtension.GetState(newState)) )
                    {
                        var selected = from client in clients where client.Value.CurrentGame == gameID select client;                           
                        if (selected.Count() > 2)
                        {
                            Console.WriteLine("game run collision");  
                        }                        
                    }
                    

                    Report();
                }
                if (packet.CmdID == NetCommands.setLang.CmdID())
                {
                    var lang = BitConverter.ToInt16(packet.GetData(), 0);
                    writer.SetLanguage(lang);
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
                    string str = Encoding.ASCII.GetString(packet.GetData());
                    removeClient(new IPEndPoint(IPAddress.Parse(str), RemotePort));                    
                }
            }
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
            //Console.WriteLine("begin recieve");
            udp.BeginReceive(new AsyncCallback(recieveCallback), this);
        }

        private void recieveCallback(IAsyncResult _iar)
        {            
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, LocalPort);
            try
            {
                byte[] data = udp.EndReceive(_iar, ref RemoteIpEndPoint);
                startRecieve();
                
                uNetPackage packet;
                //Console.WriteLine(String.Join(", ", DT_Request) + " | " + String.Join(", ", data) );
                Console.WriteLine( (IPAddress.IsLoopback(RemoteIpEndPoint.Address) ? "local" : "remote") );
                if (data.SequenceEqual(DT_Request))
                {
                    //Console.WriteLine("equal");
                    packet = new uNetPackage(0, NetCommands.report.CmdID());
                }
                else
                {
                    packet = new uNetPackage();
                    packet.BuildFromArray(data);
                    // add IP to packet data
                    if (packet.CmdID == NetCommands.reg.CmdID() || packet.CmdID == NetCommands.unreg.CmdID()){
                        packet.setData(Encoding.ASCII.GetBytes(RemoteIpEndPoint.Address.ToString()));
                    }
                    if(packet.CmdID == NetCommands.puzzleState.CmdID()){ 
                        // append IP
                        packet.setData( packet.GetData().Concat( Encoding.ASCII.GetBytes(RemoteIpEndPoint.Address.ToString()) ).ToArray() );
                    }
                }
                                                
                queue.Enqueue(packet);
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
            //Console.WriteLine("Local Report");
            IPEndPoint point = new IPEndPoint(IPAddress.Loopback, 5001);

            byte[] data = toDTArray(writer.getLang().ToString());

            //udp.Send(data, data.Length, point);

            data = toDTArray( "21" + ((writer.getGameState(21) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "41" + ((writer.getGameState(41) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "42" + ((writer.getGameState(42) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "43" + ((writer.getGameState(43) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);

            data = toDTArray( "44" + ((writer.getGameState(44) == HackGameState.Finished) ? "1" : "0") );
            udp.Send(data, data.Length, point);
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
            ClientData data;
            if ( !clients.TryGetValue(client, out data) )  return;

            short game = data.CurrentGame;
            if (game != 0)
            {
                writer.SetState(game, HackGameState.NotFinished);
            }

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
            Console.WriteLine(" Disk game: " + writer.getGameState(21).ToString() + fill);
            Console.WriteLine(" Monkey game: " + writer.getGameState(41).ToString() + fill);
            Console.WriteLine(" Puzzle game: " + writer.getGameState(42).ToString() + fill);
            Console.WriteLine(" Globe game: " + writer.getGameState(43).ToString() + fill);
            Console.WriteLine(" RB game: " + writer.getGameState(44).ToString() + fill);

            Console.SetCursorPosition(0, curPos);
        }
    }
}
