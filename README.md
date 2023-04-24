# Portkey Contracts Bingogame

BRANCH | AZURE PIPELINES                                                                                                                                                                                                                                         | TESTS                                                                                                                                                                                                        | CODE COVERAGE
-------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------
MASTER | [![Build Status](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_apis/build/status%2FPortkey-Wallet.portkey-contracts?branchName=master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=15&branchName=master) | [![Test Status](https://img.shields.io/azure-devops/tests/Portkey-Finance/Portkey-Finance/15/master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=15&branchName=master) | [![codecov](https://codecov.io/github/Portkey-Wallet/portkey-contracts-bingo-game/branch/master/graph/badge.svg?token=1H3NK4UIFJ)](https://app.codecov.io/github/Portkey-Wallet/portkey-contracts-bingo-game)
DEV    | [![Build Status](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_apis/build/status%2FPortkey-Wallet.portkey-contracts?branchName=dev)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=15&branchName=dev)       | [![Test Status](https://img.shields.io/azure-devops/tests/Portkey-Finance/Portkey-Finance/15/dev)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=15&branchName=dev)       | [![codecov](https://codecov.io/github/Portkey-Wallet/portkey-contracts-bingo-game/branch/master/graph/badge.svg?token=1H3NK4UIFJ)](https://app.codecov.io/github/Portkey-Wallet/portkey-contracts-bingo-game)


A minimalistic game demo with @portkey

## **Introduction**

Bingogame is a fast and straightforward game with quick gameplay and instant results. One of the players' objectives is to guess whether the random number in the next round falls within the range of big or small numbers. The random number is issued based on the aelf's AEDPoS consensus random number principle.

## **How to use**

Before cloning the code and deploying the Portkey Contracts Bingogame, command dependencies, and development tools are needed. You can follow:

- [Common dependencies](https://aelf-boilerplate-docs.readthedocs.io/en/latest/overview/dependencies.html)
- [Building sources and development tools](https://aelf-boilerplate-docs.readthedocs.io/en/latest/overview/tools.html)

The following command will clone Portkey Contracts Bingogame into a folder. Please open a terminal and enter the following command:

```Bash
git clone https://github.com/Portkey-Wallet/portkey-contracts-bingo-game
```

The next step is to build the contract to ensure everything is working correctly. Once everything is built, you can run as follows:

```Bash
# enter the Launcher folder and build 
cd src/AElf.Boilerplate.BingogameContract.Launcher

# build
dotnet build

# run the node 
dotnet run
```

It will run a local temporary aelf node and automatically deploy the Portkey Contracts Bingogame. You can access the node from `localhost:1235`.

This temporary aelf node runs on a framework called Boilerplate for deploying smart contracts easily. When running it, you might see errors showing incorrect password. To solve this, you need to back up your `aelf/keys`folder and start with an empty keys folder. Once you have cleaned the keys folder, stop and restart the node with `dotnet run`command shown above. It will automatically generate a new aelf account for you. This account will be used for running the aelf node and deploying the Portkey Contracts Bingogame.

## **Test**

You can easily run unit tests on Portkey Contracts Bingogame. Navigate to the Portkey.Contracts.BingogameContract.Tests and run:

```Bash
cd ../../test/Portkey.Contracts.BingogameContract.Tests
dotnet test
```

## **Contributing**

We welcome contributions to the Portkey Contracts Bingogame project. If you would like to contribute, please fork the repository and submit a pull request with your changes. Before submitting a pull request, please ensure that your code is well-tested and adheres to the aelf coding standards.

## **License**

Portkey Contracts Bingogame is licensed under [MIT](https://github.com/Portkey-Wallet/portkey-contracts-bingo-game/blob/master/LICENSE).


## **Contact**

If you have any questions or feedback, please feel free to contact us at the Portkey community channels. You can find us on Discord, Telegram, and other social media platforms.

Links:

- Website: https://portkey.finance/
- Twitter: https://twitter.com/Portkey_DID
- Discord: https://discord.com/invite/EUBq3rHQhr
- Telegram: https://t.me/Portkey_Official_Group