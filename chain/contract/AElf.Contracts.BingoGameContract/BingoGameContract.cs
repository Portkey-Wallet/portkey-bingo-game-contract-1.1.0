using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.BingoGameContract
{
    public partial class BingoGameContract : BingoGameContractContainer.BingoGameContractBase
    {
        public override Empty Register(Empty input)
        {
            Initialize();
            Assert(State.PlayerInformation[Context.Sender] == null, $"User {Context.Sender} already registered.");
            var information = new PlayerInformation
            {
                // The value of seed will influence user's game result in some aspects.
                Seed = Context.TransactionId,
                RegisterTime = Context.CurrentBlockTime
            };
            State.PlayerInformation[Context.Sender] = information;
            // State.TokenContract.Issue.Send(new IssueInput
            // {
            //     Symbol = BingoGameContractConstants.CardSymbol,
            //     Amount = BingoGameContractConstants.InitialCards,
            //     To = Context.Sender,
            //     Memo = "Initial Bingo Cards for player."
            // });
            // State.TokenContract.Issue.Send(new IssueInput
            // {
            //     Symbol = BingoGameContractConstants.CardSymbol,
            //     Amount = BingoGameContractConstants.InitialCards,
            //     To = Context.Self
            // });
            // State.TokenContract.Transfer.Send(new TransferInput
            // {
            //     Symbol = Context.Variables.NativeSymbol,
            //     Amount = 100_00000000,
            //     To = Context.Sender,
            //     Memo = "Pay tx fee."
            // });
            return new Empty();
        }

        private void Initialize()
        {
            if (State.Initialized.Value)
            {
                return;
            }

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            State.Admin.Value = Context.Sender;
            State.MinimumBet.Value = BingoGameContractConstants.DefaultMinimumBet;
            State.MaximumBet.Value = BingoGameContractConstants.DefaultMaximumBet;

            State.Initialized.Value = true;
        }

        private void LimitBetAmount(PlayerInformation information)
        {
            while (information.Bouts.Count >= BingoGameContractConstants.MaximumBetTimes)
            {
                information.Bouts.RemoveAt(0);
            }
        }

        public override Int64Value Play(PlayInput input)
        {
            Assert(input.Amount >= State.MinimumBet.Value && input.Amount <= State.MaximumBet.Value,
                "Invalid bet amount.");
            var playerInformation = GetPlayerInformation();

            Context.LogDebug(() => $"Playing with amount {input.Amount}");

            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = input.Amount,
                Symbol = BingoGameContractConstants.CardSymbol,
                Memo = "Enjoy!"
            });

            LimitBetAmount(playerInformation);

            var roundNumber = State.ConsensusContract.GetCurrentRoundNumber.Call(new Empty());

            var boutInformation = new BoutInformation
            {
                PlayBlockHeight = Context.CurrentHeight,
                Amount = input.Amount,
                Type = input.Type,
                PlayId = Context.OriginTransactionId,
                RoundNumber = roundNumber.Value,
                PlayTime = Context.CurrentBlockTime
            };

            playerInformation.Bouts.Add(boutInformation);

            State.PlayerInformation[Context.Sender] = playerInformation;

            Context.Fire(new Played
            {
                PlayBlockHeight = boutInformation.PlayBlockHeight,
                PlayId = boutInformation.PlayId,
                Amount = boutInformation.Amount,
                Type = boutInformation.Type
            });

            return new Int64Value { Value = Context.CurrentHeight.Add(GetLagHeight()) };
        }

        public override BoolValue Bingo(Hash input)
        {
            Context.LogDebug(() => $"Getting game result of play id: {input.ToHex()}");

            var playerInformation = State.PlayerInformation[Context.Sender];

            Assert(playerInformation != null, $"User {Context.Sender} not registered before.");
            Assert(playerInformation!.Bouts.Count > 0, $"User {Context.Sender} seems never join this game.");

            var boutInformation = input == Hash.Empty
                ? playerInformation.Bouts.First(i => i.BingoBlockHeight == 0)
                : playerInformation.Bouts.FirstOrDefault(i => i.PlayId == input);

            Assert(boutInformation != null, "Bout not found.");

            Assert(!boutInformation!.IsComplete, "Bout already finished.");
            var targetHeight = boutInformation.PlayBlockHeight.Add(BingoGameContractConstants.BingoBlockHeight);
            Assert(targetHeight <= Context.CurrentHeight, "Invalid target height.");

            if (State.ConsensusContract.Value == null)
            {
                State.ConsensusContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }

            var randomHash = State.ConsensusContract.GetRandomHash.Call(new Int64Value
            {
                Value = targetHeight
            });

            Assert(randomHash != null && !randomHash.Value.IsNullOrEmpty(),
                "Still preparing your game result, please wait for a while :)");

            var outValue = GetCurrentOutValue(boutInformation.RoundNumber, boutInformation.PlayTime);

            randomHash = HashHelper.XorAndCompute(randomHash, outValue);

            var usefulHash = HashHelper.ConcatAndCompute(randomHash, playerInformation.Seed);
            // var bitArraySum = SumHash(usefulHash);
            var bitArraySum = (int)Context.ConvertHashToInt64(usefulHash, 0, 256);
            var bitArraySumResult = GetBitArraySumResult(bitArraySum);
            var isWin = GetResult(bitArraySumResult, boutInformation.Type);
            var award = isWin ? boutInformation.Amount : -boutInformation.Amount;
            var transferAmount = boutInformation.Amount.Add(award);
            if (transferAmount > 0)
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    Symbol = BingoGameContractConstants.CardSymbol,
                    Amount = transferAmount,
                    To = Context.Sender,
                    Memo = "Thx for playing my game."
                });
            }

            boutInformation.Award = award;
            boutInformation.IsComplete = true;
            boutInformation.RandomNumber = bitArraySum;

            State.PlayerInformation[Context.Sender] = playerInformation;

            Context.Fire(new Bingoed
            {
                PlayBlockHeight = boutInformation.PlayBlockHeight,
                PlayId = boutInformation.PlayId,
                Amount = boutInformation.Amount,
                Award = boutInformation.Award,
                BingoBlockHeight = boutInformation.BingoBlockHeight,
                IsComplete = boutInformation.IsComplete,
                RandomNumber = boutInformation.RandomNumber,
                Type = boutInformation.Type
            });

            return new BoolValue { Value = isWin };
        }

        private Hash GetOutValue(long roundNumber, Timestamp playTime)
        {
            var bingoBlockTime = BingoGameContractConstants.BingoBlockHeight.Div(2);

            var round = State.ConsensusContract.GetRoundInformation.Call(new Int64Value
            {
                Value = roundNumber
            });
            var time = round.RealTimeMinersInformation.Values.FirstOrDefault(m =>
                m.ActualMiningTimes.Contains(playTime.AddSeconds(bingoBlockTime)));

            return time?.OutValue;
        }

        private Hash GetLatestOutValue(long roundNumber, Timestamp playTime)
        {
            var bingoBlockTime = BingoGameContractConstants.BingoBlockHeight.Div(2);

            var round = State.ConsensusContract.GetRoundInformation.Call(new Int64Value
            {
                Value = roundNumber
            });
            var list = round.RealTimeMinersInformation.Values.OrderBy(m => m.Order).ToList();
            foreach (var miner in list)
            {
                var timestamp = miner.ActualMiningTimes.FirstOrDefault(t =>
                    t > playTime.AddSeconds(bingoBlockTime));
                if (timestamp != null)
                {
                    return miner.OutValue;
                }
            }

            return null;
        }

        private Hash GetCurrentOutValue(long roundNumber, Timestamp playTime)
        {
            var outValue = GetOutValue(roundNumber, playTime);
            
            if (outValue != null) return outValue;

            outValue = GetLatestOutValue(roundNumber, playTime);
            if (outValue != null) return outValue;

            var currentRoundNumber = State.ConsensusContract.GetCurrentRoundNumber.Call(new Empty());
            if (currentRoundNumber.Value > roundNumber)
            {
                var outValueLatest = GetOutValue(roundNumber + 1, playTime);
                if (outValueLatest == null)
                {
                    outValueLatest = GetLatestOutValue(roundNumber + 1, playTime);
                }

                outValue = outValueLatest;
            }

            return outValue;
        }

        public override Int64Value GetAward(Hash input)
        {
            var boutInformation = GetPlayerInformation().Bouts.FirstOrDefault(i => i.PlayId == input);
            return boutInformation == null
                ? new Int64Value { Value = 0 }
                : new Int64Value { Value = boutInformation.Award };
        }

        public override Empty Quit(Empty input)
        {
            State.PlayerInformation.Remove(Context.Sender);
            return new Empty();
        }

        public override PlayerInformation GetPlayerInformation(Address input)
        {
            return State.PlayerInformation[input];
        }

        public override Empty SetLimitSettings(LimitSettings input)
        {
            Assert(State.Admin.Value == Context.Sender, "No permission");
            Assert(input.MinAmount >= 0 && input.MaxAmount >= input.MinAmount, "Invalid input");

            State.MinimumBet.Value = input.MinAmount;
            State.MaximumBet.Value = input.MaxAmount;

            return new Empty();
        }

        public override LimitSettings GetLimitSettings(Empty input)
        {
            return new LimitSettings
            {
                MaxAmount = State.MaximumBet.Value,
                MinAmount = State.MinimumBet.Value
            };
        }

        public override Int32Value GetRandomNumber(Hash input)
        {
            var playerInformation = GetPlayerInformation();

            Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input");

            var boutInformation = playerInformation.Bouts.FirstOrDefault(i => i.PlayId == input);

            Assert(boutInformation != null, "Bout not found.");

            return new Int32Value { Value = boutInformation!.RandomNumber };
        }

        public override BoutInformation GetBoutInformation(GetBoutInformationInput input)
        {
            Assert(input != null, "Invalid input");
            Assert(input!.PlayId != null && !input.PlayId.Value.IsNullOrEmpty(), "Invalid playId");
            Assert(input.Address != null && !input.Address.Value.IsNullOrEmpty(), "Invalid address");

            var playerInformation = State.PlayerInformation[input.Address];
            Assert(playerInformation != null, "Player not registered before.");

            var boutInformation = playerInformation!.Bouts.FirstOrDefault(i => i.PlayId == input.PlayId);

            Assert(boutInformation != null, "Bout not found.");

            return boutInformation;
        }
    }
}