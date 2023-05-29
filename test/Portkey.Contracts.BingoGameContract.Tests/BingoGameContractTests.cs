using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.BingoGameContract
{
    public class BingoGameContractTests : BingoGameContractTestBase
    {
        [Fact]
        public async Task InitializeTests()
        {
            await BingoGameContractStub.Initialize.SendAsync(new Empty());
            await BingoGameContractStub.Initialize.SendAsync(new Empty());
        }

        [Fact]
        public async Task RegisterTests()
        {
            await InitializeTests();
            await BingoGameContractStub.Register.SendAsync(new Empty());
            var information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            information.Seed.ShouldNotBeNull();
            information.RegisterTime.ShouldNotBeNull();
        }

        [Fact]
        public async Task RegisterTests_Fail_AlreadyRegistered()
        {
            await InitializeTests();
            await RegisterTests();
            var result = await BingoGameContractStub.Register.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("already registered.");
        }

        [Fact]
        public async Task<Hash> PlayTests()
        {
            await InitializeTests();
            await RegisterTests();
            await InitializeAsync();

            var amount = 1_00000000;

            var tx = await BingoGameContractStub.Play.SendAsync(new PlayInput
            {
                Amount = amount,
                Type = BingoType.Small
            });
            var information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);

            return tx.TransactionResult.TransactionId;
        }

        private async Task<Hash> PlayAsync()
        {
            await InitializeAsync();

            var amount = 1_00000000;

            var height = await BingoGameContractStub.Play.SendAsync(new PlayInput
            {
                Amount = amount,
                Type = BingoType.Small
            });

            return height.TransactionResult.TransactionId;
        }

        [Fact]
        public async Task PlayTests_Fail_InvalidInput()
        {
            await InitializeTests();
            await RegisterTests();
            await InitializeAsync();

            var result = await BingoGameContractStub.Play.SendWithExceptionAsync(new PlayInput
            {
                Amount = 0
            });
            result.TransactionResult.Error.ShouldContain("Invalid bet amount.");
        }

        [Fact]
        public async Task BingoTests()
        {
            await InitializeTests();
            await RegisterTests();

            var wins = 0;
            var loses = 0;
            var total = 51;
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

        [Fact]
        public async Task BingoTest_TransferAmountGreaterThanZero_TransfersToken()
        {
            // Arrange
            var boutInformation = new BoutInformation
            {
                Amount = 10, // Set an appropriate value for the 'Amount' property
                PlayerAddress = DefaultAddress // Set the player address to an appropriate value
            };
            var award = -10; // Set an appropriate value for the 'award' variable

            // Act
            var transferAmount = boutInformation.Amount + award;
            // Assert
            var balanceAfterTransfer = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DAppContractAddress,
                Symbol = BingoGameContractConstants.CardSymbol
            });
            balanceAfterTransfer.Balance.ShouldBe(transferAmount);
        }

        private async Task<bool> BingoTest()
        {
            var id = await PlayAsync();
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = "ELF"
            });

            var information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            var bout = await BingoGameContractStub.GetBoutInformation.CallAsync(new GetBoutInformationInput
            {
                PlayId = id
            });

            for (var i = 0; i < 15; i++)
            {
                await BingoGameContractStub.Bingo.SendWithExceptionAsync(bout.PlayId);
            }

            var isWin = await BingoGameContractStub.Bingo.SendAsync(bout.PlayId);
            var balance2 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = "ELF"
            });

            information = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            bout = await BingoGameContractStub.GetBoutInformation.CallAsync(new GetBoutInformationInput
            {
                PlayId = bout.PlayId
            });

            if (isWin.Output.Value)
            {
                bout.Award.ShouldBe(bout.Amount);
                balance2.Balance.ShouldBe(balance.Balance + bout.Award + bout.Amount);

                var num = await BingoGameContractStub.GetRandomNumber.CallAsync(id);
                num.Value.ShouldBeGreaterThan(10);
            }
            else
            {
                bout.Award.ShouldBe(-bout.Amount);
                balance2.Balance.ShouldBe(balance.Balance);

                var num = await BingoGameContractStub.GetRandomNumber.CallAsync(id);
                num.Value.ShouldBeLessThan(11);
            }

            var award = await BingoGameContractStub.GetAward.CallAsync(bout.PlayId);
            award.Value.ShouldNotBe(0);

            return isWin.Output.Value;
        }

        [Fact]
        public async Task BingoTests_Fail_InvalidInput()
        {
            await InitializeTests();

            var result = await BingoGameContractStub.Bingo.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("not registered before.");

            await RegisterTests();
            result = await BingoGameContractStub.Bingo.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("Bout not found.");
        }

        [Fact]
        public async Task BingoTests_Fail_AfterRegister()
        {
            await InitializeTests();

            await PlayTests();
            var result = await BingoGameContractStub.Bingo.SendWithExceptionAsync(HashHelper.ComputeFrom("test"));
            result.TransactionResult.Error.ShouldContain("Bout not found.");
        }

        [Fact]
        public async Task SetLimitSettingsTests()
        {
            await InitializeTests();

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
        public async Task SetLimitSettingsTests_Fail_NoPermission()
        {
            await InitializeTests();

            var result = await UserStub.SetLimitSettings.SendWithExceptionAsync(new LimitSettings
            {
                MinAmount = 1_00000000,
                MaxAmount = 2_00000000
            });

            result.TransactionResult.Error.ShouldContain("No permission");
        }

        [Fact]
        public async Task SetLimitSettingsTests_Fail_InvalidInput()
        {
            await InitializeTests();

            var result = await BingoGameContractStub.SetLimitSettings.SendWithExceptionAsync(new LimitSettings
            {
                MinAmount = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input");

            result = await BingoGameContractStub.SetLimitSettings.SendWithExceptionAsync(new LimitSettings
            {
                MinAmount = 5_00000000,
                MaxAmount = 4_00000000
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

        [Fact]
        public async Task GetBoutInformationTests()
        {
            await InitializeTests();
            var playId = await PlayTests();

            await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            var result = await BingoGameContractStub.GetBoutInformation.CallAsync(new GetBoutInformationInput
            {
                PlayId = playId
            });
            result.IsComplete.ShouldBeFalse();
            result.PlayId.ShouldBe(playId);
        }
        
        [Fact]
        public async Task GetBoutInformationTests_Fail_InvalidInput()
        {
            await InitializeTests();
            var result = await BingoGameContractStub.GetBoutInformation.SendWithExceptionAsync(new GetBoutInformationInput());
            result.TransactionResult.Error.ShouldContain("Invalid playId");
            
            result = await BingoGameContractStub.GetBoutInformation.SendWithExceptionAsync(new GetBoutInformationInput
            {
                PlayId = Hash.Empty
            });
            result.TransactionResult.Error.ShouldContain("Bout not found.");
            
            result = await BingoGameContractStub.GetBoutInformation.SendWithExceptionAsync(new GetBoutInformationInput
            {
                PlayId = Hash.Empty,
            });
            result.TransactionResult.Error.ShouldContain("Bout not found.");

            await RegisterTests();
            result = await BingoGameContractStub.GetBoutInformation.SendWithExceptionAsync(new GetBoutInformationInput
            {
                PlayId = Hash.Empty,
            });
            result.TransactionResult.Error.ShouldContain("Bout not found.");
        }

        [Fact]
        public async Task QuitTests()
        {
            await InitializeTests();
            await RegisterTests();
            
            var playInformation = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            playInformation.ShouldNotBeNull();

            await BingoGameContractStub.Quit.SendAsync(new Empty());
            playInformation = await BingoGameContractStub.GetPlayerInformation.CallAsync(DefaultAddress);
            playInformation.Seed.ShouldBeNull();
        }
        
        [Fact]
        public async Task GetRandomNumberTests_Fail()
        {
            await RegisterTests();
            await PlayAsync();
            var result = await BingoGameContractStub.GetRandomNumber.SendWithExceptionAsync(new Hash());
            result.TransactionResult.Error.ShouldContain("Invalid input");
        }
    }
}