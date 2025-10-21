using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using NBitcoin.Tests;

public abstract class NodeRunner
{
    public abstract int PortsNeeded { get; }

    public abstract void StopPreviouslyRunningProcesses(String dataDir);

    public abstract void WriteConfigFile(String dataDir, NetworkCredential rpcCreds, int[] rpcPorts, NodeConfigParameters extraParams);

    public abstract Task<Process> Run(String appPath, String dataDir, bool showNodeConsole);

    public abstract String TLSCertFilePath(String dataDir);

    public abstract void Kill();
}

public class DecredNodeRunner : NodeRunner
{
    private String walletSeed = "b280922d2cffda44648346412c5ec97f429938105003730414f10b01e1402eac";
    private String walletMiningAddr = "SspUvSyDGSzvPz2NfdZ5LW15uq6rmuGZyhL";
    private String walletPassphrase = "123";
    private Process dcrdProcess;

    public override int PortsNeeded => 3;

    public static NodeRunner CreateInstance() => new DecredNodeRunner();

    public override void StopPreviouslyRunningProcesses(String dataDir)
    {

    }

    private RPCClient makeRPCClient(String configFilePath)
    {
        var rpcAuth = extractRPCAuth(File.ReadAllText(configFilePath));
        var rpcPort = extractRPCPort(File.ReadAllText(configFilePath));
        var rpcCertPath = Path.Combine(Path.GetDirectoryName(configFilePath), "rpc.cert");
        var network = Network.GetNetwork("dcr-reg");
        return new RPCClient(rpcAuth, new Uri($"https://127.0.0.1:{rpcPort}"), rpcCertPath, network);
    }

    private int extractRPCPort(string config)
    {
        var p = Regex.Match(config, "rpclisten=:(.*)");
        return int.Parse(p.Groups[1].Value.Trim());
    }

    private String extractRPCAuth(string config)
    {
        var user = Regex.Match(config, "rpcuser=(.*)");
        var pass = Regex.Match(config, "rpcpass=(.*)");
        return user.Groups[1].Value.Trim() + ":" + pass.Groups[1].Value.Trim();
    }

    public override void WriteConfigFile(String dataDir, NetworkCredential rpcCreds, int[] rpcPorts, NodeConfigParameters extraParams)
    {
        // Check extraParams to see if it contains parameters that apply only to
        // dcrd. While extraParams provided by the caller would generally apply
        // to dcrwallet, some paramters in extraParams (such as "whitelist")
        // applies to dcrd only.
        var dcrdOnlyParams = new string[] { "whitelist" };
        NodeConfigParameters dcrdExtraParams = new NodeConfigParameters();
        foreach (var param in extraParams)
        {
            if (!dcrdOnlyParams.Contains(param.Key)) continue;
            dcrdExtraParams.Add(param.Key, param.Value);
            extraParams.Remove(param.Key);
        }

        // Write dcrd config file first. `CoreNode` uses ports[0] for syncing
        // and ports[1] for rpc requests. Syncing should connect to dcrd's sync
        // port but rpc requests should go to wallet rpc port so that rpc
        // requests that require a wallet can be handled while full node
        // requests are passed to the underlying dcrd node by the wallet.
        int dcrdSyncNodePort = rpcPorts[0], walletRPCPort = rpcPorts[1], dcrdRPCPort = rpcPorts[2];
        NodeConfigParameters config = new NodeConfigParameters
        {
            { "simnet", "1" },
            { "txindex", "1" },
            { "rpcuser", rpcCreds.UserName },
            { "rpcpass", rpcCreds.Password },
            { "rpclisten", $":{dcrdRPCPort}" },
            { "listen", $":{dcrdSyncNodePort}" },
            { "minrelaytxfee", "0.000001" },
        };
        config.Import(dcrdExtraParams, true); // override the above config values with any provided dcrd extra parameters
        var dcrdDataDir = Path.Combine(dataDir, "dcrd");
        Directory.CreateDirectory(dcrdDataDir);
        File.WriteAllText(Path.Combine(dcrdDataDir, "dcrd.conf"), config.ToString());

        // Write dcrwallet config file. Should connect to the above dcrd rpc
        // port using dcrd's rpc cert so that full node rpc requests that the
        // wallet receives can be passed to the dcrd node.
        var dcrdRPCUrl = $"127.0.0.1:{dcrdRPCPort}"; // must have 127.0.0.1 prefix else connection will fail
        var dcrdRPCCertPath = Path.GetFullPath(Path.Combine(dcrdDataDir, "rpc.cert"));
        config = new NodeConfigParameters
        {
            { "simnet", "1" },
            { "nogrpc", "1" },
            { "pass", walletPassphrase },
            { "username", rpcCreds.UserName },
            { "password", rpcCreds.Password },
            { "rpclisten", $":{walletRPCPort}" },
            { "rpcconnect", dcrdRPCUrl },
            { "cafile", dcrdRPCCertPath },
            { "tlscurve", "P-256" },
            // { "debuglevel", "debug" },
        };
        config.Import(extraParams, true); // override the above config values with any provided extra parameters
        var walletDataDir = Path.Combine(dataDir, "dcrwallet");
        Directory.CreateDirectory(walletDataDir);
        File.WriteAllText(Path.Combine(walletDataDir, "dcrwallet.conf"), config.ToString());
    }

    public override async Task<Process> Run(String appPath, String dataDir, bool showNodeConsole)
    {
        // Start dcrd, mine 2 blocks before starting dcrwallet.
        var dcrdExecPath = Path.Combine(Path.GetDirectoryName(appPath), "dcrd");
        if (appPath.EndsWith(".exe")) dcrdExecPath += ".exe";
        startDcrd(dcrdExecPath, dataDir);

        // Prepare the dcrwallet cli args.
        var walletDataDir = Path.Combine(dataDir, "dcrwallet");
        var walletConfPath = Path.Combine(walletDataDir, "dcrwallet.conf");
        var walletArgs = $"--configfile={walletConfPath} --appdata={walletDataDir}";

        // Create the wallet using the dcrwallet cli args.
        createWallet(appPath, walletArgs);

        // Start the wallet using the dcrwallet cli args and return the process.
        Process walletProcess;
        if (showNodeConsole)
        {
            ProcessStartInfo info = new ProcessStartInfo(appPath, walletArgs);
            info.UseShellExecute = true;
            walletProcess = Process.Start(info);
        }
        else
        {
            walletProcess = Process.Start(appPath, walletArgs);
        }

        // Delay a bit to allow the wallet process complete initialization
        // before returning the process.
        await Task.Delay(500);
        return walletProcess;
    }

    private void startDcrd(String dcrdExecPath, String dataDir)
    {
        // Run dcrd in a background process, using an address from the wallet as
        // the mining address.
        var dcrdDataDir = Path.Combine(dataDir, "dcrd");
        var dcrdConfPath = Path.Combine(dcrdDataDir, "dcrd.conf");
        var dcrdArgs = $"--appdata={dcrdDataDir} --configfile={dcrdConfPath} --miningaddr={walletMiningAddr}"; ;
        this.dcrdProcess = startProcessQuietly(dcrdExecPath, dcrdArgs);

        // dcrd process should be running now. Wait 3 seconds, then mine 2
        // blocks so that the wallet is ready for use when it is started later.
        // If the dcrd process ends during this 3 seconds of waiting, something
        // is wrong.
        var dcrdProcessEnded = dcrdProcess.WaitForExit(3_000);
        if (dcrdProcessEnded)
        {
            throwProcessStartException("dcrd", dcrdProcess);
        }

        // dcrd appears to be running fine, generate 2 blocks and leave dcrd
        // running.
        makeRPCClient(dcrdConfPath).Generate(2);
    }

    private void createWallet(String appPath, String walletArgs)
    {
        var walletProcess = startProcessQuietly(appPath, $"{walletArgs} --create");
        // Pass expected prompt responses to the process input stream. If the
        // walletProcess is killed while writing to the input stream, an
        // exception would be thrown; so wrap the writing in a try/catch block.
        try
        {
            var expectedResponses = $"yes\nno\nyes\n"; // use config pass=yes, additional encryption=no, existing seed=yes
            walletProcess.StandardInput.Write(expectedResponses);
            walletProcess.StandardInput.WriteLine(walletSeed);
            // Wait a bit for the seed input to be processed.
            Thread.Sleep(200);
            // Close the stdin stream so that subsequent wallet prompts will
            // receive an EOF error. Trying to send a response for subsequent
            // prompts here using walletProcess.StandardInput.Write doesn't
            // always work but the wallet is able to proceed if it gets EOF
            // error(s).
            walletProcess.StandardInput.Close();
        }
        catch { }

        // Wait 3 seconds for the wallet to be created, then the process should
        // exit. If it does not exit after 3 seconds, there was a problem. Kill
        // the process manually and throw an exception.
        var processEnded = walletProcess.WaitForExit(3_000);
        if (!processEnded)
        {
            walletProcess.Kill();
            walletProcess.WaitForExit();
            throwProcessStartException("dcrwallet", walletProcess);
        }
    }

    private Process startProcessQuietly(string path, string args)
    {
        ProcessStartInfo info = new ProcessStartInfo(path, args);
        info.RedirectStandardError = true;
        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        info.UseShellExecute = false;
        info.CreateNoWindow = true;

        Process process = new Process();
        process.StartInfo = info;
        process.Start();
        return process;
    }

    private void throwProcessStartException(String app, Process process)
    {
        String report = process.StandardError.ReadToEnd();
        if (report == null || report == "")
            report = process.StandardOutput.ReadToEnd();
        throw new Exception($"{app} failed to start:\n{report}");
    }

    public override String TLSCertFilePath(String dataDir)
    {
        return Path.Combine(dataDir, "dcrwallet", "rpc.cert");
    }

    public override void Kill()
    {
        if (dcrdProcess != null && !dcrdProcess.HasExited)
        {
            dcrdProcess.Kill();
            dcrdProcess.WaitForExit();
        }
    }
}
