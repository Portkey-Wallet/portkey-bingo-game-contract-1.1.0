using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.BingoGameContract
{
    public partial class BingoGameContract : BingoGameContractContainer.BingoGameContractBase
    {
        public override Empty Register(Empty input)
        {
            Assert(State.PlayerInformation[Context.Sender] == null, $"User {Context.Sender} already registered.");
            var information = new PlayerInformation
            {
                // The value of seed will influence user's game result in some aspects.
                Seed = Context.TransactionId,
                RegisterTime = Context.CurrentBlockTime
            };
            State.PlayerInformation[Context.Sender] = information;
            Context.Fire(new Registered
            {
                RegisterTime = information.RegisterTime,
                PlayerAddress = Context.Sender,
                Seed = information.Seed
            });

            return new Empty();
        }

        public override Empty Initialize(Empty input)
        {
            if (State.Initialized.Value)
            {
                return new Empty();
            }

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            State.Admin.Value = Context.Sender;
            State.MinimumBet.Value = BingoGameContractConstants.DefaultMinimumBet;
            State.MaximumBet.Value = BingoGameContractConstants.DefaultMaximumBet;

            State.Initialized.Value = true;
            return new Empty();
        }

        public override Int64Value Play(PlayInput input)
        {
            Assert(input.Amount >= State.MinimumBet.Value && input.Amount <= State.MaximumBet.Value,
                "Invalid bet amount.");

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

            var roundNumber = State.ConsensusContract.GetCurrentRoundNumber.Call(new Empty());

            var boutInformation = new BoutInformation
            {
                PlayBlockHeight = Context.CurrentHeight,
                Amount = input.Amount,
                Type = input.Type,
                PlayId = Context.OriginTransactionId,
                RoundNumber = roundNumber.Value,
                PlayTime = Context.CurrentBlockTime,
                Dices = new DiceList(),
            };
            State.BoutInformations[Context.OriginTransactionId] = boutInformation;

            Context.Fire(new Played
            {
                PlayBlockHeight = boutInformation.PlayBlockHeight,
                PlayId = boutInformation.PlayId,
                Amount = boutInformation.Amount,
                Type = boutInformation.Type,
                PlayerAddress = Context.Sender,
                Symbol = BingoGameContractConstants.CardSymbol
            });

            return new Int64Value { Value = Context.CurrentHeight.Add(BingoGameContractConstants.BingoBlockHeight) };
        }    
        private List<int> GetDices(Hash hashValue)
        {
            var hexString = hashValue.ToHex();
            var dices = new List<int>();

            for (int i = 0; i < 3; i++)
            {
                var startIndex = i * 8;
                var subString = hexString.Substring(startIndex, 8);
                var intValue = int.Parse(subString, System.Globalization.NumberStyles.HexNumber);
                var dice = (intValue % 6 + 5) % 6 + 1;
                dices.Add(dice);
            }

            return dices;
        }

        
        
        public override BoolValue Bingo(Hash input)
        {
            Context.LogDebug(() => $"Getting game result of play id: {input.ToHex()}");

            Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input.");

            var playerInformation = State.PlayerInformation[Context.Sender];

            Assert(playerInformation != null, $"User {Context.Sender} not registered before.");

            var boutInformation = State.BoutInformations[input];

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

            randomHash = HashHelper.XorAndCompute(randomHash, outValue ?? Hash.Empty);

            var usefulHash = HashHelper.ConcatAndCompute(randomHash, playerInformation.Seed);
            // var bitArraySum = SumHash(usefulHash);
            List<int> dices = GetDices(usefulHash);
            var bitArraySum = dices.Sum();
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
            boutInformation.BingoBlockHeight = Context.CurrentHeight;
            boutInformation.Dices.Dices.Add(dices[0]);
            boutInformation.Dices.Dices.Add(dices[1]);
            boutInformation.Dices.Dices.Add(dices[2]);

            State.PlayerInformation[Context.Sender] = playerInformation;
            State.BoutInformations[input] = boutInformation;

            Context.Fire(new Bingoed
            {
                PlayBlockHeight = boutInformation.PlayBlockHeight,
                PlayId = boutInformation.PlayId,
                Amount = boutInformation.Amount,
                Award = boutInformation.Award,
                BingoBlockHeight = boutInformation.BingoBlockHeight,
                IsComplete = boutInformation.IsComplete,
                RandomNumber = boutInformation.RandomNumber,
                Dices = boutInformation.Dices,
                Type = boutInformation.Type,
                PlayerAddress = Context.Sender,
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

            var miners = round.RealTimeMinersInformation.Values.ToList();

            var miner = miners.FirstOrDefault(m =>
                m.ActualMiningTimes.Contains(playTime.AddSeconds(bingoBlockTime)));

            if (miner == null) return null;

            var value = miner.OutValue;

            if (value == null || value.Value.IsNullOrEmpty())
            {
                if (miners.Last().Equals(miner))
                {
                    miners.Remove(miner);

                    value = miners.LastOrDefault()?.OutValue;
                }
            }

            return value;
        }

        private Hash GetLatestOutValue(long roundNumber, Timestamp playTime)
        {
            var bingoBlockTime = BingoGameContractConstants.BingoBlockHeight.Div(2);

            var round = State.ConsensusContract.GetRoundInformation.Call(new Int64Value
            {
                Value = roundNumber
            });

            var miners = round.RealTimeMinersInformation.Values.OrderBy(m => m.Order).ToList();

            Hash value = null;
            foreach (var miner in miners)
            {
                var timestamp = miner.ActualMiningTimes.FirstOrDefault(t =>
                    t > playTime.AddSeconds(bingoBlockTime));
                if (timestamp != null)
                {
                    value = miner.OutValue;

                    if (value == null || value.Value.IsNullOrEmpty())
                    {
                        if (miners.Last().Equals(miner))
                        {
                            miners.Remove(miner);

                            value = miners.LastOrDefault()?.OutValue;
                        }
                    }
                }
            }

            return value;
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
            Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input.");
            var boutInformation = State.BoutInformations[input];
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
            Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input.");
            var playerInformation = GetPlayerInformation();

            var boutInformation = State.BoutInformations[input];

            Assert(boutInformation != null, "Bout not found.");

            return new Int32Value { Value = boutInformation!.RandomNumber };
        }

        public override BoutInformation GetBoutInformation(GetBoutInformationInput input)
        {
            Assert(input != null, "Invalid input");
            Assert(input!.PlayId != null && !input.PlayId.Value.IsNullOrEmpty(), "Invalid playId");

            var boutInformation = State.BoutInformations[input.PlayId];

            Assert(boutInformation != null, "Bout not found.");

            return boutInformation;
        }

        public override GetRandomHashOutput GetRandomHash(GetRandomHashInput input)
        {
            if (State.ConsensusContract.Value == null)
            {
                State.ConsensusContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }

            var result = new List<RandomResult>();

            var output = State.ConsensusContract.GetCurrentRoundNumber.Call(new Empty());
            var roundNumber = output.Value;
            var currentHeight = Context.CurrentHeight;

            while (roundNumber > 0 && result.Count < input.Times)
            {
                var round = State.ConsensusContract.GetRoundInformation.Call(new Int64Value
                {
                    Value = roundNumber
                });

                var miners = round.RealTimeMinersInformation.Values.Where(m => m.ActualMiningTimes.Count > 0)
                    .OrderBy(m => m.Order).ToList();

                var miner = miners.LastOrDefault();

                while (miner != null)
                {
                    var outValue = miner.OutValue;
                    if (outValue == null || outValue.Value.IsNullOrEmpty())
                    {
                        if (miners.Count < 2)
                        {
                            break;
                        }

                        outValue = miners[^2].OutValue;
                    }

                    for (int i = miner.ActualMiningTimes.Count - 1; i >= 0; i--)
                    {
                        var hash = State.ConsensusContract.GetRandomHash.Call(new Int64Value
                        {
                            Value = currentHeight
                        });
                        hash = HashHelper.XorAndCompute(hash, outValue);
                        hash = HashHelper.ConcatAndCompute(hash, input.Seed);

                        result.Add(new RandomResult
                        {
                            RandomHash = hash,
                            Height = currentHeight
                        });
                        if (result.Count >= input.Times)
                        {
                            break;
                        }

                        currentHeight--;
                    }

                    miners.Remove(miner);
                    miner = miners.FirstOrDefault();
                }

                roundNumber--;
            }

            result.Reverse();

            return new GetRandomHashOutput
            {
                RandomResults = { result }
            };
        }
    }
    
}