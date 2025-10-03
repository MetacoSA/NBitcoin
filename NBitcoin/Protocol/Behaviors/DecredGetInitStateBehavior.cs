
using System;

namespace NBitcoin.Protocol.Behaviors
{
    /// <summary>
    /// Behavior to respond to getinitstate message from a decred node.
    /// </summary>
    public class DecredGetInitStateBehavior : NodeBehavior
    {
        public override object Clone()
        {
            return new DecredGetInitStateBehavior();
        }

        protected override void AttachCore()
        {
            AttachedNode.MessageReceived += AttachedNode_MessageReceived;
        }


        void AttachedNode_MessageReceived(Node node, IncomingMessage message)
        {
            if (message.Message.Payload.Command == "getinitstate")
                node.SendMessageAsync(new DecredInitStatePayload());
        }

        protected override void DetachCore()
        {
            AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
        }
    }
}
