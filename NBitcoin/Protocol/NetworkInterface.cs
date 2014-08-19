using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using System.Threading;
using System.Collections.Concurrent;

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

            Thread Managementloopthread = new Thread(ManagementLoops);
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

        private void ManagementLoops()
        {
            //todo is this sleep really needed?
            Thread.Sleep(100);//wait for the Nodeset to be created
            int OldTickCount = System.Environment.TickCount;

            Thread NodeSetWatcher = new Thread(() => {
                while (true)
                {
                    if (connectedNodes.Count() < wantedConnectedNodeSetSize)
                    {
                        Info("Not connected to enough Nodes.(" + connectedNodes.Count() + "/" + wantedConnectedNodeSetSize + ")");
                        try
                        {
                            connectedNodes.AddNode(GetNewConnectedNode());
                            Info("Connected to new Node");
                        }
                        catch(Exception e)
                        {
                            Warning(e.Message);
                        }
                        
                    }
                    System.Threading.Thread.Sleep(100);
                }
            });
            NodeSetWatcher.IsBackground = true;
            NodeSetWatcher.Start();

            Thread IdleWatcher = new Thread(() => {
                while (true)
                {
                    if (IdleNodes.Count == 0 || System.Environment.TickCount - OldTickCount > 100)
                    {
                        OldTickCount = System.Environment.TickCount;
                        var NewIdlers = from node in connectedNodes.GetNodes() where node.idle select node;
                        foreach (var node in NewIdlers)
                        {
                            if(IdleNodes.Contains(node)==false)
                                IdleNodes.Add(node);
                        }
                    }
                    System.Threading.Thread.Sleep(1);
                }
            });
            IdleWatcher.IsBackground = true;
            IdleWatcher.Start();
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
                                chain.PushChange(new ChainChange() { BlockHeader = header, ChangeType = ChainChangeType.AddBlock }, header.GetHash());
                                //chain.GetOrAdd(header);
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
            Info("Calculating List of Blocks we need to download");
            var DoneSet = new HashSet<uint256>(from storedItem in MyIndexedBlockStore.Store.Enumerate(true) select storedItem.Item.GetHash());
            Info("Donethat, now get List of Blocks not done yet");
            var work = from head
                      in headerChain.EnumerateAfter(headerChain.Genesis)
                       where DoneSet.Contains(head.Header.GetHash()) == false //TODO add an contains() function to Index
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
            Node Provider = GetIdleNode();
            Provider.SendGetDataPayload(GDPayload);
            return Provider;
        }

        public BlockingCollection<Node> IdleNodes = new BlockingCollection<Node>();

        /// <summary>
        /// Blocking if empty!
        /// </summary>
        /// <returns></returns>
        internal Node GetIdleNode()
        {
            return IdleNodes.Take();
        }

    }
}
