using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using System.Threading;

namespace NBitcoin.Protocol
{
    public class NetworkInterface
    {
        Network network;
        NodeSet connectedNodes;

        public NetworkInterface(Network network, NodeSet connectedNodes)
        {
            this.network = network;
            this.connectedNodes = connectedNodes;
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
                                Console.WriteLine("Chain progress : " + chain.Height + "/" + height);
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
            var work = from head
                      in headerChain.EnumerateAfter(headerChain.Genesis)
                       where MyIndexedBlockStore.GetHeader(head.Header.GetHash()) == null //TODO add an contains() function to Index
                       select head.Header.GetHash();

            work.AsParallel().WithDegreeOfParallelism(10).ForAll(
                (uint256 hash) => {
                    Block block = null;
                    while (block == null)
                    {
                        block = DownloadBlockFromNetwork(hash);
                        if (block != null)
                        {
                            lock (MyLockToIndex)
                            {
                                Console.WriteLine("Downloaded Block with Timestamp:" + block.Header.BlockTime.ToString());
                                MyIndexedBlockStore.Put(block);
                            }
                        }
                    }
                });
            



            
        }

        private Block DownloadBlockFromNetwork(uint256 hash)
        {
            Node RandomNode = connectedNodes.GetRandomNode();

            

            RandomNode.SendMessage(new GetDataPayload(new InventoryVector() { Hash = hash, Type = InventoryType.MSG_BLOCK }));
            
            try
            {
                BlockPayload BPayload = RandomNode.RecieveMessage<BlockPayload>(new TimeSpan(0, 0, 60));
                if (BPayload != null)
                {
                    if (BPayload.Object.GetHash() == hash)
                        return BPayload.Object;
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


    }
}
