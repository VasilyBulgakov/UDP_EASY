using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using System.IO;

using System.Security.Permissions;

namespace UDP_EASY
{
    class GameStateWriter
    {
        private FileStream outStream;

        private string file = "log.txt";


        private Stopwatch lastDTAccess = new Stopwatch();

        private FileSystemWatcher watcher = new FileSystemWatcher();

        private int lang = 045;
        private Dictionary<short, HackGameState> gameStates = new Dictionary<short, HackGameState>();


        private bool stateChanged = false;

        private bool fileChanged = true;


        public GameStateWriter()
        {
            gameStates.Add(21, HackGameState.NotFinished);
            gameStates.Add(41, HackGameState.NotFinished);
            gameStates.Add(42, HackGameState.NotFinished);
            gameStates.Add(43, HackGameState.NotFinished);
            gameStates.Add(44, HackGameState.NotFinished);

            runWatcher();
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
        /// Попытка записи состояния в файл
        /// Запись только если файл был открыт DT модулем от 200 до 300мс назад
        /// при наличии изменений в текущем статусе
        /// </summary>
        public void TryWrite()
        {
            long elapsed = lastDTAccess.ElapsedMilliseconds;
            if (fileChanged && stateChanged && elapsed > 200 && elapsed < 300)
            {
                writeStatus();
                fileChanged = false;
                Console.WriteLine("Write time: from " + elapsed.ToString() + " to " + lastDTAccess.ElapsedMilliseconds.ToString());
            }
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


        private void writeStatus()
        {
            string str = lang.ToString() + Environment.NewLine;
            foreach (var k in gameStates)
                str += (k.Value == HackGameState.Finished ? "1" : "0") + Environment.NewLine;

            watcher.EnableRaisingEvents = false;
            try
            {
                outStream = new FileStream(file, FileMode.Open,
                     FileAccess.Write, FileShare.ReadWrite);

                outStream.SetLength(0);
                outStream.Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
                outStream.Close();
                stateChanged = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("write error " + e.Message);
                outStream.Close();
                return;
            }
            watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Отслеживание изменений файла средствами системы
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void runWatcher()
        {
            watcher.Path = Directory.GetCurrentDirectory();

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Filter = file;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Запуск таймера последнего изменения дя синхроизации с DT модулем
        /// </summary>  
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (lastDTAccess.IsRunning && lastDTAccess.Elapsed.Milliseconds < 5) return;

            //Console.WriteLine(e.ChangeType + " time: " + lastDTAccess.Elapsed.Milliseconds);
            lastDTAccess.Restart();
            fileChanged = true;
        }
    }
}
