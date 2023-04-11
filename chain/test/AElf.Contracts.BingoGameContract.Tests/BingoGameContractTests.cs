using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.BingoGameContract
{
    public class BingoGameContractTests : BingoGameContractTestBase
    {
        [Fact]
        public async Task RegisterTests()
        {
            await BingoGameContractStub.Register.SendAsync(new Empty());
            var information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            information.Seed.ShouldNotBeNull();
            information.RegisterTime.ShouldNotBeNull();
        }

        [Fact]
        public async Task RegisterTests_Fail_AlreadyRegistered()
        {
            await RegisterTests();
            var result = await BingoGameContractStub.Register.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("already registered.");
        }

        [Fact]
        public async Task<Hash> PlayTests()
        {
            await RegisterTests();
            await InitializeAsync();

            var amount = 1_00000000;

            var height = await BingoGameContractStub.Play.SendAsync(new PlayInput
            {
                Amount = amount,
                Type = true
            });
            var information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            information.Bouts.First().Amount.ShouldBe(amount);
            information.Bouts.First().Type.ShouldBe(true);
            information.Bouts.First().PlayBlockHeight.ShouldBe(height.Output.Value - 1);

            return information.Bouts.First().PlayId;
        }

        [Fact]
        public async Task PlayTests_Fail_InvalidAmount()
        {
            await RegisterTests();
            await InitializeAsync();

            var amount = 0;

            var result = await BingoGameContractStub.Play.SendWithExceptionAsync(new PlayInput
            {
                Amount = amount,
                Type = true
            });
            result.TransactionResult.Error.ShouldContain("Invalid bet amount.");
        }

        [Fact]
        public async Task BingoTests()
        {
            var wins = 0;
            var loses = 0;
            var total = 10;
            for (var i = 0; i < total; i++)
            {
                var result = await BingoTest();
                if (result)
                {
                    wins++;
                }
                else
                {
                    loses++;
                }
            }

            var times = wins + loses;
            times.ShouldBe(total);
        }

        private async Task<bool> BingoTest()
        {
            var id = await PlayTests();
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = "ELF"
            });

            var information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            var bout = information.Bouts.First();

            var isWin = await BingoGameContractStub.Bingo.SendAsync(bout.PlayId);
            var balance2 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = "ELF"
            });

            information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            bout = information.Bouts.First();

            if (isWin.Output.Value)
            {
                bout.Award.ShouldBe(bout.Amount);
                balance2.Balance.ShouldBe(balance.Balance + bout.Award + bout.Amount);

                var num = await BingoGameContractStub.GetRandomNumber.CallAsync(id);
                num.Value.ShouldBeGreaterThan(127);
            }
            else
            {
                bout.Award.ShouldBe(-bout.Amount);
                balance2.Balance.ShouldBe(balance.Balance);

                var num = await BingoGameContractStub.GetRandomNumber.CallAsync(id);
                num.Value.ShouldBeLessThan(128);
            }

            var award = await BingoGameContractStub.GetAward.CallAsync(bout.PlayId);
            award.Value.ShouldNotBe(0);

            await BingoGameContractStub.Quit.SendAsync(new Empty());

            return isWin.Output.Value;
        }

        [Fact]
        public async Task BingoTests_Fail_InvalidInput()
        {
            var result = await BingoGameContractStub.Bingo.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("not registered before.");

            await RegisterTests();
            result = await BingoGameContractStub.Bingo.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("seems never join this game.");
        }

        [Fact]
        public async Task BingoTests_Fail_AfterRegister()
        {
            var hash = await PlayTests();
            var result = await BingoGameContractStub.Bingo.SendWithExceptionAsync(HashHelper.ComputeFrom("test"));
            result.TransactionResult.Error.ShouldContain("Bout not found.");
        }

        [Fact]
        public async Task SetLimitSettingsTests()
        {
            await RegisterTests();
            var settings = await BingoGameContractStub.GetLimitSettings.CallAsync(new Empty());
            settings.MaxAmount.ShouldBe(100_00000000);
            settings.MinAmount.ShouldBe(1_00000000);

            await BingoGameContractStub.SetLimitSettings.SendAsync(new LimitSettings
            {
                MinAmount = 5_00000000,
                MaxAmount = 15_00000000
            });

            settings = await BingoGameContractStub.GetLimitSettings.CallAsync(new Empty());
            settings.MaxAmount.ShouldBe(15_00000000);
            settings.MinAmount.ShouldBe(5_00000000);
        }

        [Fact]
        public async Task SetLimitSettingsTests_Fail_InvalidInput()
        {
            await RegisterTests();
            var result = await BingoGameContractStub.SetLimitSettings.SendWithExceptionAsync(new LimitSettings
            {
                MinAmount = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input");
        }

        private async Task InitializeAsync()
        {
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = DAppContractAddress,
                Symbol = "ELF",
                Amount = 1000_00000000
            });
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContractAddress,
                Symbol = "ELF",
                Amount = 1000_00000000
            });
        }

        // [Fact]
        // public async Task Test()
        // {
        //     // Get a stub for testing.
        //     var keyPair = SampleAccount.Accounts.First().KeyPair;
        //     var stub = GetBingoGameContractStub(keyPair);
        //     var tokenStub =
        //         GetTester<TokenContractContainer.TokenContractStub>(
        //             GetAddress(TokenSmartContractAddressNameProvider.StringName), keyPair);
        //
        //     // Prepare awards.
        //     await tokenStub.Transfer.SendAsync(new TransferInput
        //     {
        //         To = DAppContractAddress,
        //         Symbol = "ELF",
        //         Amount = 100_00000000
        //     });
        //
        //     await tokenStub.Create.SendAsync(new CreateInput
        //     {
        //         Symbol = "CARD",
        //         TokenName = "Bingo Card",
        //         Decimals = 0,
        //         Issuer = DAppContractAddress,
        //         IsBurnable = true,
        //         TotalSupply = long.MaxValue
        //     });
        //
        //     await stub.Register.SendAsync(new Empty());
        //
        //     await tokenStub.Approve.SendAsync(new ApproveInput
        //     {
        //         Spender = DAppContractAddress,
        //         Symbol = "CARD",
        //         Amount = long.MaxValue
        //     });
        //
        //     // Now I have player information.
        //     var address = Address.FromPublicKey(keyPair.PublicKey);
        //
        //     {
        //         var playerInformation = await stub.GetPlayerInformation.CallAsync(address);
        //         playerInformation.Seed.Value.ShouldNotBeEmpty();
        //         playerInformation.RegisterTime.ShouldNotBeNull();
        //     }
        //
        //     // Play.
        //     var txResult = (await tokenStub.Approve.SendAsync(new ApproveInput
        //     {
        //         Spender = DAppContractAddress,
        //         Symbol = "ELF",
        //         Amount = 10000
        //     })).TransactionResult;
        //     txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        //
        //     await stub.Play.SendAsync(new PlayInput
        //     {
        //         Amount = 200,
        //         Type = true
        //     });
        //
        //     Hash playId;
        //     {
        //         var playerInformation = await stub.GetPlayerInformation.CallAsync(address);
        //         playerInformation.Bouts.ShouldNotBeEmpty();
        //         playId = playerInformation.Bouts.First().PlayId;
        //     }
        //
        //     // Mine 7 more blocks.
        //     for (var i = 0; i < 7; i++)
        //     {
        //         await stub.Bingo.SendWithExceptionAsync(playId);
        //     }
        //
        //     await stub.Bingo.SendAsync(playId);
        //
        //     var award = await stub.GetAward.CallAsync(playId);
        //     award.Value.ShouldNotBe(0);
        // }
    }
}