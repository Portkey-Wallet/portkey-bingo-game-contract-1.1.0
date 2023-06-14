using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;
using AElf.Types;

namespace Portkey.Contracts.BingoGameContract
{
    public partial class BingoGameContractTests : TestBase
    {
        // private readonly ECKeyPair KeyPair;
        private readonly BingoGameContractContainer.BingoGameContractStub BingoGameContractStub;
        private readonly BingoGameContractContainer.BingoGameContractStub UserStub;
        private readonly TokenContractContainer.TokenContractStub TokenContractStub;
        private readonly AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub;
        protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        protected Address DefaultAddress => Accounts[0].Address;
        protected ECKeyPair UserKeyPair => Accounts[1].KeyPair;
        protected Address UserAddress => Accounts[1].Address;

        public BingoGameContractTests()
        {
            // KeyPair = SampleAccount.Accounts.First().KeyPair;
            BingoGameContractStub = GetContractStub<BingoGameContractContainer.BingoGameContractStub>(DefaultKeyPair);
            UserStub = GetContractStub<BingoGameContractContainer.BingoGameContractStub>(UserKeyPair);
            TokenContractStub = GetTester<TokenContractContainer.TokenContractStub>(
                TokenContractAddress, DefaultKeyPair);
            AEDPoSContractStub = GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ConsensusContractAddress, DefaultKeyPair);
        }
    }
    
}