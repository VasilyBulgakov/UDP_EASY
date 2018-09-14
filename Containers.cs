using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net;

namespace UDP_EASY
{
    /// <summary>
    /// Сетевой пакет и адрес откуда он пришёл
    /// </summary>
    class NetData
    {
        public readonly uNetPackage Packet;
        public readonly IPEndPoint Address;

        public NetData(uNetPackage packet, IPEndPoint from)
        {
            Packet = packet;
            Address = from;
        }
    }

    class ClientData
    {
        public short CurrentGame;
        public Stopwatch LastUpdate;

        public ClientData()
        {
            CurrentGame = 0;
            LastUpdate = new Stopwatch();
            LastUpdate.Start();
        }
    }

    /// <summary>
    /// Статус игры и клиенты играющие в неё в данный момент
    /// </summary>
    class GameStatus
    {
        public HackGameState State = HackGameState.NotFinished;

        public List<IPEndPoint> Clients = new List<IPEndPoint>(1);

        public GameStatus()
        {
            State = HackGameState.NotFinished;
        }

        public GameStatus(HackGameState state, List<IPEndPoint> clients)
        {
            State = state;
            Clients = clients;
        }
    }


}
