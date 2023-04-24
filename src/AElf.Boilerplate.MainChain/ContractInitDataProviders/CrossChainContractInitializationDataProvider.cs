using AElf.CrossChain;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.MainChain.ContractInitDataProviders
{
    public class CrossChainContractInitializationDataProvider : ICrossChainContractInitializationDataProvider,
        ITransientDependency
    {
        public CrossChainContractInitializationData GetContractInitializationData()
        {
            return new CrossChainContractInitializationData
            {
                IsPrivilegePreserved = true
            };
        }
    }
}