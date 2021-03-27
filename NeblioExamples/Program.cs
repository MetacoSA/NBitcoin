using NBitcoin;
using NBitcoin.Altcoins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static NBitcoin.Altcoins.Neblio;

namespace NeblioExamples
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello Crypto World!");
			Console.WriteLine("------------------------------------------------");
			Console.WriteLine("This Utility will create Neblio Address and Keys");
			Console.WriteLine("------------------------------------------------");

			var network = Neblio.Instance.Mainnet;

			// place your address here if you have it, then you should have next to exe folder "Account/youraddress/"
			// with privateKeyFromNetwork.txt with private key and txhexstring.txt with raw unsigned tx hex from neblio API

			var initAddress = "NVSzTaaQuRukkLvQp1ZoeoaN6agYdGVX73";
			var initFilesPath = "";

			if (!string.IsNullOrEmpty(initAddress))
			{
				var initLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				FileHelpers.CheckOrCreateTheFolder(Path.Join(initLoc, "Accounts"));
				initFilesPath = Path.Join(initLoc, "Accounts", initAddress.ToString());
				FileHelpers.CheckOrCreateTheFolder(initFilesPath);
			}
			/*
            
            Key privateKey = new Key(); // generate a random private key
            PubKey publicKey = privateKey.PubKey;

            // The private key, also known as the Bitcoin secret or the WIF (Wallet Interchange Format).
            // If you intend to use it, make sure you save the below somewhere safe!
            BitcoinSecret privateKeyFromNetwork = privateKey.GetBitcoinSecret(network);
            var address = publicKey.GetAddress(ScriptPubKeyType.Legacy, network);
            Console.WriteLine("publicKey = " + publicKey.ToString());
            Console.WriteLine("privateKeyFromNetwork = " + privateKeyFromNetwork.ToString());
            Console.WriteLine("address (from privateKey) = " + address.ToString());
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("-------------------End----------------");

            var filesPath = "";
            if (address.ToString() != initAddress)
            {
                var loc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                FileHelpers.CheckOrCreateTheFolder(Path.Join(loc, "Accounts"));
                filesPath = Path.Join(loc, "Accounts", address.ToString());
                FileHelpers.CheckOrCreateTheFolder(filesPath);
            }

            FileHelpers.WriteTextToFile(Path.Combine(filesPath, "address.txt"), address.ToString());
            FileHelpers.WriteTextToFile(Path.Combine(filesPath, "publicKey.txt"), publicKey.ToString());
            FileHelpers.WriteTextToFile(Path.Combine(filesPath, "ScriptPubKey.txt"), address.ScriptPubKey.ToString());
            FileHelpers.WriteTextToFile(Path.Combine(filesPath, "Wif.txt"), privateKey.GetWif(network).ToString());
            FileHelpers.WriteTextToFile(Path.Combine(filesPath, "privateKeyFromNetwork.txt"), privateKeyFromNetwork.ToString());
            
            */

			/*
            var command = Console.ReadLine();
            
            if (command.Contains("exit") || command.Contains("quit")) { return; }
            */

			// load the stored private key and create key and address objects
			var keyFromFilestr = FileHelpers.ReadTextFromFile(Path.Combine(initFilesPath, "privateKeyFromNetwork.txt"));
			BitcoinSecret keyfromFile = null;
			BitcoinAddress addressForTx = null;
			if (!string.IsNullOrEmpty(keyFromFilestr))
			{
				try
				{
					keyfromFile = network.CreateBitcoinSecret(keyFromFilestr);
					addressForTx = keyfromFile.GetAddress(ScriptPubKeyType.Legacy);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Exception during parsing private key from file: {ex}");
				}
			}

			try
			{
				// load raw unsigned hex as string
				var txhexstringfromfile = FileHelpers.ReadTextFromFile(Path.Combine(initFilesPath, "txhexstring.txt"));
				var txhexstring = "";
				if (!string.IsNullOrEmpty(txhexstringfromfile))
				{
					txhexstring = txhexstringfromfile;
				}
				else
				{
					Console.WriteLine("txhexstring.txt file is empty, please load the raw unsigned neblio tx");
					Console.WriteLine("Press enter to quit...");
					Console.ReadLine();
					return;
				}

				if (Transaction.TryParse(txhexstring, network, out var transaction))
				{

					// download input tx objects  
					var txrespToken = NeblioTransaction.GetNeblioTransaction(transaction.Inputs[0].PrevOut.Hash.ToString());
					var txrespNebl = NeblioTransaction.GetNeblioTransaction(transaction.Inputs[1].PrevOut.Hash.ToString());

					// there is still some issue in parsing json from api. now need to reparse the hex.
					Transaction.TryParse(txrespToken.Hex, network, out var tx1); // token
					Transaction.TryParse(txrespNebl.Hex, network, out var tx2); // nebl


					// load list of input coins for the source address
					// some of them must be spendable
					List<ICoin> list = new List<ICoin>();
					foreach (var to in tx1.Outputs)
					{
						if (to.ScriptPubKey == addressForTx.ScriptPubKey)
							list.Add(new Coin(tx1, (uint)(tx1.Outputs.IndexOf(to))));
					}

					foreach (var to in tx2.Outputs)
					{
						if (to.ScriptPubKey == addressForTx.ScriptPubKey)
							list.Add(new Coin(tx2, (uint)(tx2.Outputs.IndexOf(to))));
					}

					// recipient taddress - not need, just example how to get it
					//var address = BitcoinAddress.Create("NPWBL3i8ZQ8tmhDtrixXwYd93nofmunvhA", network);

					transaction.Inputs[0].ScriptSig = addressForTx.ScriptPubKey; //address.ScriptPubKey;
					transaction.Inputs[1].ScriptSig = addressForTx.ScriptPubKey; //address.ScriptPubKey;

					transaction.Sign(keyfromFile, list);

					var txhex = transaction.ToHex();

					Console.WriteLine("New Tx Hex: " + txhex);

					FileHelpers.WriteTextToFile(Path.Combine(initFilesPath, $"tx-{transaction.GetHash()}-txHex.txt"), txhex);

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception during loading inputs or signing tx: {ex}");
			}

			Console.WriteLine("Press enter to quit...");
			Console.ReadLine();
			return;
		}
	}
}
