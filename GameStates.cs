using System;
using System.Collections.Generic;


namespace UDP_EASY
{
    class GameStates
    {
        private int lang = 045;
        private Dictionary<short, HackGameState> gameStates = new Dictionary<short, HackGameState>();


        private bool stateChanged = false;


        private Dictionary<HackGame, GameStatus> games = new Dictionary<HackGame, GameStatus>();
  


        public GameStates()
        {
            games.Add(HackGame.Disk, new GameStatus());
            games.Add(HackGame.Monkey, new GameStatus());
            games.Add(HackGame.Puzzle, new GameStatus());
            games.Add(HackGame.RoundBall, new GameStatus());
            games.Add(HackGame.Globe, new GameStatus());
        }

        public void SetLanguage(int newLang)
        {
            if (newLang == lang) return;

            stateChanged = true;
            lang = newLang;
        }
        /// <summary>
        /// изменяет состояние игры на заданное
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="newState"></param>
        /// <returns>true - если состояние изменилось</returns>
        public bool SetState(short gameID, HackGameState newState)
        {
            if (newState == gameStates[gameID]) return false;

            stateChanged = true;
            gameStates[gameID] = newState;
            return true;
        }

       
        /// <summary>
        /// Возвращает массив данных с отчётом о состоянии
        /// </summary>
        /// <returns>lang(4b), gameStates[0 - n](bool 1b)</returns>
        public byte[] GetReport()
        {
            List<byte> reportContent = new List<byte>();

            reportContent.AddRange(BitConverter.GetBytes(lang));
            foreach (var s in gameStates)
            {
                reportContent.AddRange(BitConverter.GetBytes(s.Value.GetID()));
            }
            
            return reportContent.ToArray();
        }

        public int getLang()
        {
            return lang;
        }

        public HackGameState getGameState(short gameID)
        {
            return gameStates[gameID];
        }
    }
}
