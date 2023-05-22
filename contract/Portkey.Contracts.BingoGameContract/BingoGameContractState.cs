using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Portkey.Contracts.BingoGameContract
{
    public partial class BingoGameContractState : ContractState 
    {
        public MappedState<Address, PlayerInformation> PlayerInformation { get; set; }

        public MappedState<Hash, BoutInformation> BoutInformations { get; set; }

        public SingletonState<long> LagHeight { get; set; }

        public BoolState Initialized { get; set; }
        public SingletonState<Address> Admin { get; set; }
        
        public SingletonState<long> MinimumBet { get; set; }
        public SingletonState<long> MaximumBet { get; set; }
    }
}