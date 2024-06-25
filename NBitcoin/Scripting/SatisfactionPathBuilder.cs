#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scripting
{
	public class SatisfactionChoice
	{
		public SatisfactionChoice(MiniscriptNode.Fragment fragment, int selectionRequired, MiniscriptNode[] parameters)
		{
			Fragment = fragment;
			SelectionRequired = selectionRequired;
			Parameters = parameters;
		}

		public MiniscriptNode.Fragment Fragment { get; }
		public int SelectionRequired { get; }

		public MiniscriptNode[] Parameters { get; }

		List<int> _Selected = new();
		public IReadOnlyList<int> Selected => _Selected;

		public void Select(SatisfactionPathSegment selection)
		{
			foreach (var pathIndex in selection.Choices)
			{
				Select(pathIndex);
			}
		}
		internal void Select(params int[] selection)
		{
			foreach (var pathIndex in selection)
			{
				Select(pathIndex);
			}
		}
		void Select(int pathIndex)
		{
			if (pathIndex < 0 || pathIndex >= Parameters.Length)
				throw new IndexOutOfRangeException();
			if (Selected.Count == SelectionRequired)
				throw new InvalidOperationException("All the selections have already been made");
			_Selected.Add(pathIndex);
			_Selected.Sort();
		}

		public IEnumerable<MiniscriptNode> GetSelectedParameters()
		{
			foreach (var i in _Selected)
				yield return this.Parameters[i];
		}
	}
	public class SatisfactionPath
	{
		public readonly static SatisfactionPath Empty = new SatisfactionPath(Array.Empty<SatisfactionPathSegment>());
		public SatisfactionPath(IReadOnlyList<SatisfactionPathSegment> segments)
		{
			ArgumentNullException.ThrowIfNull(segments);
			Segments = segments;
		}
		public IReadOnlyList<SatisfactionPathSegment> Segments { get; }
		public SatisfactionPathSegment this[int index]
		{
			get => Segments[index];
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			bool first = true;
			foreach (var segment in Segments)
			{
				if (!first)
					builder.Append('/');
				first = false;
				if (segment.Choices.Count == 1)
					builder.Append(segment.Choices[0]);
				else
				{
					builder.Append('(');
					builder.Append(string.Join(",", segment.Choices));
					builder.Append(')');
				}
				builder.Append("");
			}
			return builder.ToString();
		}
	}
	public class SatisfactionPathSegment
	{
		public SatisfactionPathSegment(IReadOnlyList<int> choices)
		{
			ArgumentNullException.ThrowIfNull(choices);
			Choices = choices;
		}
		public IReadOnlyList<int> Choices { get; }
	}

	public class SatisfactionPathBuilder
	{
		public SatisfactionPathBuilder(Miniscript miniscript)
		{
			Miniscript = miniscript;
			_Visiting.Push(new VisitStatus(miniscript.RootNode));
			this.GoToUnresolvedChoice();
		}

		class VisitStatus
		{
			public VisitStatus(MiniscriptNode node)
			{
				Node = node;
				Index = 0;
			}

			public MiniscriptNode Node { get; }
			public int Index { get; set; }
			public SatisfactionChoice? Choice { get; set; }
		}

		public Miniscript Miniscript { get; }
		Stack<VisitStatus> _Visiting = new Stack<VisitStatus>();

		List<SatisfactionPathSegment> _segments = new();
		public SatisfactionChoice? CurrentChoice { get; private set; }
		public SatisfactionPath GetPath() => new SatisfactionPath(_segments);

		/// <summary>
		/// Walk from the current node until the next choice to take.
		/// </summary>
		/// <returns>True if there is no more choice to take. If false, check the <see cref="CurrentChoice"/> property to make your choice.</returns>
		bool GoToUnresolvedChoice()
		{
			if (CurrentChoice is not null)
				return false;

			while (_Visiting.Count > 0)
			{
				var frame = _Visiting.Peek();
				if (frame.Node is not MiniscriptNode.Fragment f)
				{
					_Visiting.Pop();
					continue;
				}
				else if (frame.Choice is not null)
				{
					_Visiting.Pop();
					foreach (var param in frame.Choice.GetSelectedParameters().Reverse())
					{
						_Visiting.Push(new(param));
					}
				}
				else if (f.Descriptor.IsOr())
				{
					CurrentChoice = new SatisfactionChoice(f, 1, f.Parameters.ToArray());
					return false;
				}
				else if (f.Descriptor == FragmentDescriptor.andor)
				{
					CurrentChoice = new SatisfactionChoice(f, 1, f.Parameters.Skip(1).ToArray());
					return false;
				}
				else if (
					f.Descriptor == FragmentDescriptor.multi ||
					f.Descriptor == FragmentDescriptor.multi_a ||
					f.Descriptor == FragmentDescriptor.thresh)
				{
					var count = ((MiniscriptNode.Value.CountValue)f.Parameters.First()).Count;
					CurrentChoice = new SatisfactionChoice(f, count, f.Parameters.Skip(1).ToArray());
					return false;
				}
				else
				{
					_Visiting.Pop();
					foreach (var param in f.Parameters.Reverse())
						_Visiting.Push(new(param));
				}
			}
			CurrentChoice = null;
			return true;
		}

		bool GoToUnresolvedChoice([MaybeNullWhen(true)] out SatisfactionChoice choice)
		{
			GoToUnresolvedChoice();
			choice = CurrentChoice;
			return choice is null;
		}

		/// <summary>
		/// Select the satisfaction path for the current choice
		/// </summary>
		/// <param name="choice">The next unresolved choice</param>
		/// <param name="selection">The selection for the current choice</param>
		/// <returns>True if there is a new unresolved choice to make (<paramref name="choice"/> isn't null)</returns>
		public bool Select([MaybeNullWhen(true)] out SatisfactionChoice choice, params int[] selection)
		{
			if (GoToUnresolvedChoice(out choice))
				return true;
			choice.Select(selection);
			if (choice.Selected.Count >= choice.SelectionRequired)
			{
				var visiting = _Visiting.Peek();
				visiting.Choice = choice;
				_segments.Add(new SatisfactionPathSegment(choice.Selected));
				CurrentChoice = null;
			}
			return GoToUnresolvedChoice(out choice);
		}
		/// <summary>
		/// Select the satisfaction path for the current choice
		/// </summary>
		/// <param name="choice">The next unresolved choice</param>
		/// <param name="parameters">The selection for the current choice</param>
		/// <returns>True if there is a new unresolved choice to make (<paramref name="choice"/> isn't null)</returns>
		public bool Select([MaybeNullWhen(true)] out SatisfactionChoice choice, params MiniscriptNode[] parameters)
		{
			ArgumentNullException.ThrowIfNull(parameters);
			if (GoToUnresolvedChoice(out choice))
				return true;
			var localChoice = choice;
			var selection = parameters.Select(p => Array.IndexOf(localChoice.Parameters, p)).ToArray();
			return Select(out choice, selection);
		}

		/// <summary>
		/// Returns the next unresolved choice
		/// </summary>
		/// <param name="choice">The next unresolved choice</param>
		/// <returns>True if there is a new unresolved choice to make</returns>
		public bool Select([MaybeNullWhen(true)] out SatisfactionChoice choice) => Select(out choice, Array.Empty<int>());
	}
}
#endif
