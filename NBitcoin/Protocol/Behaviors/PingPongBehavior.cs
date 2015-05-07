using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	[Flags]
	public enum PingPongMode
	{
		SendPing = 1,
		RespondPong = 2,
		Both = 3
	}
	public class PingPongBehavior : NodeBehavior
	{
		public PingPongBehavior()
		{
			Mode = PingPongMode.Both;
			TimeoutInterval = TimeSpan.FromMinutes(2.0);
			PingInterval = TimeSpan.FromMinutes(2.0);
		}
		PingPongMode _Mode;
		public PingPongMode Mode
		{
			get
			{
				return _Mode;
			}
			set
			{
				AssertNotAttached();
				_Mode = value;
			}
		}

		TimeSpan _TimeoutInterval;
		public TimeSpan TimeoutInterval
		{
			get
			{
				return _TimeoutInterval;
			}
			set
			{
				AssertNotAttached();
				_TimeoutInterval = value;
			}
		}

		TimeSpan _PingInterval;
		public TimeSpan PingInterval
		{
			get
			{
				return _PingInterval;
			}
			set
			{
				AssertNotAttached();
				_PingInterval = value;
			}
		}

		protected override void AttachCore()
		{
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			RegisterDisposable(new Timer(Ping, null, 0, (int)PingInterval.TotalMilliseconds));
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.HandShaked)
				Ping(null);
		}

		void Ping(object unused)
		{
			if(AttachedNode.State != NodeState.HandShaked)
				return;
			if(_CurrentPing != null)
				return;
			var node = AttachedNode;
			if(node == null)
				return;
			_CurrentPing = new PingPayload();
			_DateSent = DateTimeOffset.UtcNow;
			node.SendMessage(_CurrentPing);
			_PingTimeoutTimer = new Timer(PingTimeout, null, (int)TimeoutInterval.TotalMilliseconds, Timeout.Infinite);
		}

		void PingTimeout(object unused)
		{
			var node = AttachedNode;
			if(node != null)
				node.Disconnect("Pong timeout");
		}

		Timer _PingTimeoutTimer;
		volatile PingPayload _CurrentPing;
		DateTimeOffset _DateSent;

		public TimeSpan Latency
		{
			get;
			private set;
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			var ping = message.Message.Payload as PingPayload;
			if(ping != null && Mode.HasFlag(PingPongMode.RespondPong))
			{
				node.SendMessage(new PongPayload()
				{
					Nonce = ping.Nonce
				});
			}
			var pong = message.Message.Payload as PongPayload;
			if(pong != null &&
				Mode.HasFlag(PingPongMode.SendPing) &&
				_CurrentPing != null &&
				_CurrentPing.Nonce == pong.Nonce)
			{
				Latency = DateTimeOffset.UtcNow - _DateSent;
				ClearCurrentPing();
			}
		}

		private void ClearCurrentPing()
		{
			_CurrentPing = null;
			_DateSent = default(DateTimeOffset);
			var timeout = _PingTimeoutTimer;
			if(timeout != null)
			{
				timeout.Dispose();
				timeout = null;
			}
		}

		protected override void DetachCore()
		{
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
			ClearCurrentPing();
		}
	}
}
