using AElf.CSharp.Core;

namespace AElf.Contracts.BingoGameContract
{
    public class BingoGameContractConstants
    {
        public const string CardSymbol = "ELF";
        // public const long InitialCards = 10_0000;
        public const long DefaultMinimumBet = 1_00000000;
        public const long DefaultMaximumBet = 100_00000000;
        public const long MaximumBetTimes = 50;
        
        public const long BingoBlockHeight = 16;
    }
}