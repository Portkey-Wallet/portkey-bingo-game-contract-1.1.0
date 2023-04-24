using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.BingoGameContract
{
    public partial class BingoGameContract
    {
        private PlayerInformation GetPlayerInformation()
        {
            var playerInformation = State.PlayerInformation[Context.Sender];
            
            Assert(playerInformation != null, $"User {Context.Sender} not registered before.");

            return playerInformation;
        }

        private BingoType GetBitArraySumResult(int bitArraySum)
        {
            Assert(bitArraySum is >= 0 and <= 255, $"random number: {bitArraySum} error");
            if (bitArraySum < 128)
            {
                return BingoType.Small;
            }

            return BingoType.Large;
        }

        private bool GetResult(BingoType bitArraySumResult, BingoType type)
        {
            return bitArraySumResult == type;
        }
    }
}