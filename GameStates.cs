using System;
using System.Collections.Generic;
using System.Net;

namespace UDP_EASY
{
    class GameStates
    {
        private int lang = 045;

        private Dictionary<HackGame, GameStatus> games = new Dictionary<HackGame, GameStatus>();
  


        public GameStates()
        {
            games.Add(HackGame.Disk, new GameStatus());
            games.Add(HackGame.Monkey, new GameStatus());
            games.Add(HackGame.Puzzle, new GameStatus());
            games.Add(HackGame.RoundBall, new GameStatus());
            games.Add(HackGame.Globe, new GameStatus());
        }


        
        /// <returns>true - если язык изменился</returns>
        public bool SetLanguage(int newLang)
        {
            if (newLang == lang) return false;
            lang = newLang;
            return true;
        }
        
        /// <returns>true - если не было коллизии</returns>
        public bool SetState(HackGame game, HackGameState newState, IPAddress clientAddr)
        {
            bool allowedOperation = false;
            var gameData = games[game];

            if(gameData.State == HackGameState.NotFinished)
                gameData.Client = null;
           
            if (gameData.Client == null || gameData.Client.Equals(clientAddr))
            {
                gameData.State = newState;
                gameData.Client = clientAddr;

                allowedOperation = true;
            }

            // reset in case of collision
            if (!allowedOperation)
                gameData.State = HackGameState.NotFinished;           

            if (gameData.State == HackGameState.Finished || gameData.State == HackGameState.NotFinished)
                gameData.Client = null;

            return allowedOperation;
        }

        public void RemoveClient(IPAddress client)
        {
            foreach(var game in games)
            {
                if(game.Value.Client != null && game.Value.Client.Equals(client) )
                {
                    game.Value.State = HackGameState.NotFinished;
                    game.Value.Client = null;
                    Console.WriteLine("disconnecting " + client);
                }
            }
        }


        /// <summary>
        /// Возвращает массив данных с отчётом о состоянии
        /// порядок игр : disk monkey puzzle globe ball
        /// </summary>
        /// <returns>lang(4b), gameStates[0 - n](short 2b)</returns>
        public byte[] GetReport()
        {
            List<byte> reportContent = new List<byte>();            

            reportContent.AddRange(BitConverter.GetBytes(lang));
                        
            reportContent.AddRange(BitConverter.GetBytes(games[HackGame.Disk].State.GetID()));
            reportContent.AddRange(BitConverter.GetBytes(games[HackGame.Monkey].State.GetID()));
            reportContent.AddRange(BitConverter.GetBytes(games[HackGame.Puzzle].State.GetID()));
            reportContent.AddRange(BitConverter.GetBytes(games[HackGame.Globe].State.GetID()));
            reportContent.AddRange(BitConverter.GetBytes(games[HackGame.RoundBall].State.GetID()));

            return reportContent.ToArray();
        }

        public int getLang()
        {
            return lang;
        }

        public HackGameState getGameState(HackGame game)
        {
            return games[game].State;
        }
        
    }
}
