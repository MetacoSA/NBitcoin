using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using System.Threading;

namespace NBitcoin.Protocol
{

    /// <summary>
    /// Things to know
    /// -waits most of the time until connected to at least 5 Nodes!
    /// - Work in progress
    /// </summary>
    public class NetworkInterface
    {

        Network network;
        private NodeSet connectedNodes;

        public delegate void StringMessage(string Message);

        #region Events
        public event StringMessage OnWarning;
        private void Warning(string warning)
        {
            if (OnWarning != null)
                OnWarning(warning);
        }

        public event StringMessage OnInfo;
        private void Info(string info)
        {
            if (OnInfo != null)
                OnInfo(info);
        }

        #endregion


        private int wantedConnectedNodeSetSize = 10;

        public int WantedConnectedNodeSetSize
        {
            get
            {
                return wantedConnectedNodeSetSize;
            }
            set
            {
                if (value < 0) throw new ArgumentException("Cant use negative Values here");
                if(value < 5)
                    Warning("Networkinterface may not work correctly with less than 5 Nodes"); 
                if(value>100)
                {
                    Warning("Do you really want such a big ConnectedNodesSet: "+value+"?"); 
                }
                wantedConnectedNodeSetSize = value;
            }
        }

        #region Constructors
        public NetworkInterface()
        {
            if (network == null)
                network = Network.Main;

            Thread Managementloopthread = new Thread(ManagementLoop);
            Managementloopthread.IsBackground = true;
            Managementloopthread.Start();
        }

        public NetworkInterface(Network network, NodeSet connectedNodes)
            : this()
        {
            this.connectedNodes = connectedNodes;
            this.network = network;
        }

        public NetworkInterface(Network network)
            : this(network, new NodeSet())
        {
        }

        #endregion

        private void ManagementLoop()
        {
            //todo is this sleep really needed?
            Thread.Sleep(100);//wait for the Nodeset to be created
            while(true)
            {
                if(connectedNodes.Count()<wantedConnectedNodeSetSize)
                {
                    Info("Not connected to enough Nodes.("+connectedNodes.Count()+"/"+wantedConnectedNodeSetSize+")");
                    connectedNodes.AddNode(GetNewConnectedNode());
                    Info("Connected to new Node");
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        private Node GetNewConnectedNode()
        {
            return new NodeServer(Network.Main).CreateNodeSet(1).GetNodes()[0];
        }

        public void WaitUntillConnectedToHealthyCountOfNodes()
        {
            if (connectedNodes.Count() < 1)
                Warning("Not connected to enough Nodes of the Network");
            while (connectedNodes.Count() == 0)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        public Chain BuildChain(ObjectStream<ChainChange> changes = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (changes == null)
                changes = new StreamObjectStream<ChainChange>();
            var chain = new Chain(network, changes);
            return BuildChain(chain, cancellationToken);
        }

        public Chain BuildChain(Chain chain, CancellationToken cancellationToken = default(CancellationToken))
        {
            WaitUntillConnectedToHealthyCountOfNodes();
            //TODO maybe used NoteSet is older and so Node.FullVersion.Startheight not anymore correct!
            TraceCorrelation trace = new TraceCorrelation(NodeServerTrace.Trace, "Build chain");
            using (trace.Open())
            {
                var pool = connectedNodes;

                int height = pool.GetNodes().Max(o => o.FullVersion.StartHeight);
                var listener = new PollMessageListener<IncomingMessage>();

                pool.SendMessage(new GetHeadersPayload()
                {
                    BlockLocators = chain.Tip.GetLocator()
                });

                using (pool.MessageProducer.AddMessageListener(listener))
                {
                    while (chain.Height != height)
                    {
                        var before = chain.Tip;
                        var headers = listener.RecieveMessage(cancellationToken).Message.Payload as HeadersPayload;
                        if (headers != null)
                        {
                            foreach (var header in headers.Headers)
                            {
                                chain.GetOrAdd(header);
                            }
                            if (before.HashBlock != chain.Tip.HashBlock)
                            {
                                NodeServerTrace.Information("Chain progress : " + chain.Height + "/" + height);
                                Info("Chain progress : " + chain.Height + "/" + height);
                                pool.SendMessage(new GetHeadersPayload()
                                {
                                    BlockLocators = chain.Tip.GetLocator()
                                });
                            }
                        }
                    }
                }
            }
            return chain;
        }

        public void ScnchroniseIndexedBlockstoreWithChain(IndexedBlockStore MyIndexedBlockStore, Chain headerChain)
        {
            //better dont access the db concurrent
            object MyLockToIndex=new object();
            
            //list of all hashes from blocks we need to download
            Console.WriteLine("Calculating List of Blocks we need to download");
            var work = from head
                      in headerChain.EnumerateAfter(headerChain.Genesis)
                       where MyIndexedBlockStore.GetHeader(head.Header.GetHash()) == null //TODO add an contains() function to Index
                       select head.Header.GetHash();

            //WaitUntillConnectedToHealthyCountOfNodes();

            work.AsParallel().WithDegreeOfParallelism(10).ForAll(
                (uint256 hash) => {
                    Block block = null;
                    while (block == null)
                    {
                        if(connectedNodes.Count()==0)
                            Warning("Not connected to any Node of the Network");
                        while(connectedNodes.Count()==0)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                        block = DownloadBlockFromNetwork(hash);
                        if (block != null)
                        {
                            lock (MyLockToIndex)
                            {
                                Console.WriteLine("Downloaded Block ("+ block.Transactions.Count() +") with Timestamp:" + block.Header.BlockTime.ToString());
                                Console.WriteLine("Downloaded Block (" + block.HeaderOnly + ") with Timestamp:" + block.Header.BlockTime.ToString());
                                Console.WriteLine(block.Header.GetHash());
                                Console.WriteLine(block.CheckMerkleRoot());
                                MyIndexedBlockStore.Put(block);
                            }
                        }
                    }
                });
            



            
        }

        private Block DownloadBlockFromNetwork(uint256 hash)
        {
            Node RandomNode = GetDataScheduler(new GetDataPayload(new InventoryVector() { Hash = hash, Type = InventoryType.MSG_BLOCK })); // connectedNodes.GetRandomNode();

            /*try
            {
                RandomNode.SendGetDataPayload(new GetDataPayload(new InventoryVector() { Hash = hash, Type = InventoryType.MSG_BLOCK }));
            }
            catch
            {
                connectedNodes.RemoveNode(RandomNode); //TODO Errorlog and refill of Nodeset
                return DownloadBlockFromNetwork(hash);
            }
             */
            try
            {
                BlockPayload BPayload = RandomNode.RecieveMessage<BlockPayload>(new TimeSpan(0, 0, 60));
                if (BPayload != null)
                {
                    if (BPayload.Object.GetHash() == hash)
                    {
                        RandomNode.idle = true;
                        return BPayload.Object;
                    }
                    else // Got wrong Block? Maybe was sending new mined Block...
                        return null;
                }
                return null;
            }
            catch
            {
                //TODO make a log that Blockdownload didnt work out
                return null;
            }
        }

        public Node GetDataScheduler(GetDataPayload GDPayload)
        {
            Node Provider = connectedNodes.GetIdleNode();
            Provider.SendGetDataPayload(GDPayload);
            return Provider;
        }

    }
}
