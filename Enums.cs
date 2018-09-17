using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDP_EASY
{
    
    public enum NetCommands
    {
        reg,
        unreg,
        report,
        regID,
        puzzleState,
        setLang,
        ping,
        denied
    }

    public enum HackGameState
    {
        NotFinished,
        Finished,
        InProgress
    }
    public enum HackGame
    {
        RoundBall,
        Puzzle,
        Monkey,
        Globe,
        Disk,
        None
    }

    public static class NetCommandsExtension
    {
        public static ushort CmdID(this NetCommands cmd)
        {
            switch (cmd)
            {
                case NetCommands.reg: return 10;
                case NetCommands.regID: return 11;
                case NetCommands.unreg: return 12;
                case NetCommands.puzzleState: return 1;
                case NetCommands.setLang: return 2;
                case NetCommands.report: return 3;
                case NetCommands.denied: return 6;

                case NetCommands.ping: return 7;     
                              

                default: return 0;
            }
        }

        public static short GetID(this HackGameState state)
        {
            switch (state)
            {
                case HackGameState.NotFinished: return 0;
                case HackGameState.Finished: return 1;
                case HackGameState.InProgress: return 2;

                default: return 0;
            }
        }

        public static HackGameState GetState(short state)
        {
            switch (state)
            {
                case 0: return HackGameState.NotFinished;
                case 1: return HackGameState.Finished;
                case 2: return HackGameState.InProgress;

                default: return 0;
            }
        }

        public static short GetID(this HackGame gameId)
        {
            switch (gameId)
            {
                case HackGame.Disk: return 21;
                case HackGame.Monkey: return 41;
                case HackGame.Puzzle: return 42;
                case HackGame.Globe: return 43;
                case HackGame.RoundBall: return 44;

                default: return 0;
            }
        }
        public static HackGame ToGame(short id)
        {
            switch (id)
            {
                case 21: return HackGame.Disk;
                case 41: return HackGame.Monkey;
                case 42: return HackGame.Puzzle;
                case 43: return HackGame.Globe;
                case 44: return HackGame.RoundBall;

                default: return HackGame.None;
            }
        }


    }
}
