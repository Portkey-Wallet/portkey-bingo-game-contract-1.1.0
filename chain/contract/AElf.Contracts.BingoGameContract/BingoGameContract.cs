using Google.Protobuf.WellKnownTypes;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

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

        public override Int64Value Play(PlayInput input)
        {
            Assert(input.Amount >= State.MinimumBet.Value && input.Amount <= State.MaximumBet.Value, "Invalid bet amount.");
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

            playerInformation.Bouts.Add(new BoutInformation
            {
                PlayBlockHeight = Context.CurrentHeight,
                Amount = input.Amount,
                Type = input.Type,
                PlayId = Context.OriginTransactionId
            });

            State.PlayerInformation[Context.Sender] = playerInformation;

            return new Int64Value {Value = Context.CurrentHeight.Add(GetLagHeight())};
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

            Assert(!boutInformation.IsComplete, "Bout already finished.");
            var targetHeight = boutInformation.PlayBlockHeight.Add(GetLagHeight());
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
            
            Assert(randomHash != null && !randomHash.Value.IsNullOrEmpty(), "Still preparing your game result, please wait for a while :)");

            var usefulHash = HashHelper.ConcatAndCompute(randomHash, playerInformation.Seed);
            var bitArraySum = SumHash(usefulHash);
            var bitArraySumResult  = GetBitArraySumResult(bitArraySum);
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
            State.PlayerInformation[Context.Sender] = playerInformation;
            return new BoolValue {Value = isWin};
        }

        public override Int64Value GetAward(Hash input)
        {
            var boutInformation = GetPlayerInformation().Bouts.FirstOrDefault(i => i.PlayId == input);
            return boutInformation == null
                ? new Int64Value {Value = 0}
                : new Int64Value {Value = boutInformation.Award};
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
            Assert(input.MinAmount >= 0 && input.MaxAmount >= 0, "Invalid input");

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
    }
}