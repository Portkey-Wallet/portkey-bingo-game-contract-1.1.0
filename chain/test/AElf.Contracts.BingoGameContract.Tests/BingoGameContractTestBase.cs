using AElf.Boilerplate.TestBase;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;
using AElf.Types;

namespace AElf.Contracts.BingoGameContract
{
    public class BingoGameContractTestBase : DAppContractTestBase<BingoGameContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);

        internal BingoGameContractContainer.BingoGameContractStub BingoGameContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }
        protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        protected Address DefaultAddress => Accounts[0].Address;

        public BingoGameContractTestBase()
        {
            BingoGameContractStub = GetBingoGameContractStub(DefaultKeyPair);
            TokenContractStub = GetTokenContractTester(DefaultKeyPair);
            AEDPoSContractStub = GetAEDPoSContractStub(DefaultKeyPair);
        }

        internal BingoGameContractContainer.BingoGameContractStub GetBingoGameContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<BingoGameContractContainer.BingoGameContractStub>(DAppContractAddress, senderKeyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSContractStub(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }
    }
}