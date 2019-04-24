using System;

namespace NBitcoin.Miniscript
{
	internal class Cost
	{
		internal AstElem Ast { get; }
		internal UInt32 PkCost { get; }
		internal double SatCost { get; }
		public double DissatCost { get; internal set; }

		internal Cost (AstElem ast, UInt32 pkCost, double satCost, double dissatCost)
		{
			Ast = ast;
			PkCost = pkCost;
			SatCost = satCost;
			DissatCost = dissatCost;
		}


		internal static Cost Dummy()
			=> new Cost(
					new AstElem.Time(0),
					1024 * 1024,
					1024.0 * 1024.0,
					1024.0 * 1024.0
			);
		internal static Cost Wrap(Cost ecost)
		{
			// debug
			if (!ecost.Ast.IsE())
				throw new Exception("unreachable");
			return new Cost(
				new AstElem.Wrap(ecost.Ast),
				ecost.PkCost + 2,
				ecost.SatCost,
				ecost.DissatCost
				);
		}

		internal static Cost True(Cost vcost)
		{
			// debug
			if (!vcost.Ast.IsV())
				throw new Exception("unreachable");

			return new Cost(
				new AstElem.True(vcost.Ast),
				vcost.PkCost + 1,
				vcost.SatCost,
				0.0
			);
		}

		internal static Cost Likely(Cost fcost)
		{
			// debug
			if (!fcost.Ast.IsF())
				throw new Exception($"unreachable {fcost.Ast}");

			return new Cost(
				new AstElem.Likely(fcost.Ast),
				fcost.PkCost + 4,
				fcost.SatCost + 1.0,
				2.0
			);
		}

		internal static Cost Unlikely(Cost fcost)
		{
			// debug
			if (!fcost.Ast.IsF())
				throw new Exception("unreachable");

			return new Cost(
				ast: new AstElem.Unlikely(fcost.Ast),
				pkCost: fcost.PkCost + 4,
				satCost: fcost.SatCost + 2.0,
				dissatCost: 1.0
			);
		}

		internal static Cost FromTerminal(AstElem ast)
		{
			switch (ast.Tag)
			{
				case AstElem.Tags.Pk:
					return new Cost(
						ast: ast,
						pkCost: 35,
						satCost: 72.0,
						dissatCost: 1.0
					);
				case AstElem.Tags.PkV:
					return new Cost(
					
						ast: ast,
						pkCost: 35,
						satCost: 72.0,
						dissatCost: 1.0
					);
				case AstElem.Tags.PkQ:
					return new Cost(
						ast: ast,
						pkCost: 34,
						satCost: 0.0,
						dissatCost: 0.0
					);
				case AstElem.Tags.PkW:
					return new Cost(
					
						ast: ast,
						pkCost: 36,
						satCost: 72.0,
						dissatCost: 1.0
					);
				case AstElem.Tags.Multi:
					var m = ((AstElem.Multi)ast).Item1;
					var n = (uint)((AstElem.Multi)ast).Item2.Length;
					var numCost = (m > 16 && n > 16) ? 4u
						: !(m > 16) && !(n > 16) ? 2u
						: 3u;
					return new Cost(
					
						ast: ast,
						pkCost: numCost + 34u * n + 1u,
						satCost: 1.0 + 72.0 * m, 
						dissatCost: 1.0 + m
					);
				case AstElem.Tags.MultiV:
					var mv = ((AstElem.MultiV)ast).Item1;
					var nv = (uint)((AstElem.MultiV)ast).Item2.Length;
					var numCostv = (mv > 16 && nv > 16) ? 4u
						: !(mv > 16) && !(nv > 16) ? 2u
						: 3u;
					return new Cost(
						ast: ast,
						pkCost: numCostv + 34u * nv + 1u,
						satCost: 1.0 + 72.0 * mv, 
						dissatCost: 0.0
					);

				case AstElem.Tags.TimeT:
					return new Cost(
					
						ast: ast,
						pkCost: 1 + ScriptNumCost(((AstElem.TimeT)ast).Item1),
						satCost: 0.0,
						dissatCost: 0.0
					);
				case AstElem.Tags.TimeV:
					return new Cost(
						ast: ast,
						pkCost: 2 + ScriptNumCost(((AstElem.TimeV)ast).Item1),
						satCost: 0.0,
						dissatCost: 0.0
					);

				case AstElem.Tags.TimeF:
					return new Cost(
						ast: ast,
						pkCost: 2 + ScriptNumCost(((AstElem.TimeF)ast).Item1),
						satCost: 0.0,
						dissatCost: 0.0
					);
				case AstElem.Tags.Time:
					return new Cost(
					
						ast: ast,
						pkCost: 5 + ScriptNumCost(((AstElem.Time)ast).Item1),
						satCost: 2.0,
						dissatCost: 1.0
					);
				case AstElem.Tags.TimeW:
					return new Cost(
					
						ast: ast,
						pkCost: 6 + ScriptNumCost(((AstElem.TimeW)ast).Item1),
						satCost: 2.0,
						dissatCost: 1.0
					);
				case AstElem.Tags.HashT:
					return new Cost(
						ast: ast,
						pkCost: 39,
						satCost: 33.0,
						dissatCost: 0.0
					);
				case AstElem.Tags.HashV:
					return new Cost(
						ast: ast,
						pkCost: 39,
						satCost: 33.0,
						dissatCost: 0.0
					);
				case AstElem.Tags.HashW:
					return new Cost(
						ast: ast,
						pkCost: 39,
						satCost: 33.0,
						dissatCost: 1.0
					);
			}

			throw new Exception("Unreachable");
		}
		
		internal static Cost FromPair(Cost left, Cost right, int parentType, double lweight, double rweight)
		{
			switch (parentType)
			{
				case AstElem.Tags.AndCat:
					return new Cost(
						ast: new AstElem.AndCat(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost,
						satCost: left.SatCost + right.SatCost,
						dissatCost: 0.0
					);
				case AstElem.Tags.AndBool:
					return new Cost(
						ast: new AstElem.AndBool(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 1,
						satCost: left.SatCost + right.SatCost,
						dissatCost: left.DissatCost + right.DissatCost
					);
				case AstElem.Tags.AndCasc:
					return new Cost(
					
						ast: new AstElem.AndCasc(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 4,
						satCost: left.SatCost + right.SatCost,
						dissatCost: left.DissatCost
					);
				case AstElem.Tags.OrBool:
					return new Cost(
						ast: new AstElem.OrBool(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 4,
						satCost: (left.SatCost + right.DissatCost) * lweight + (right.SatCost + left.DissatCost) * rweight,  
						dissatCost: left.DissatCost + right.DissatCost
					);
				case AstElem.Tags.OrCasc:
					return new Cost(
						ast: new AstElem.OrCasc(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 3,
						satCost: left.SatCost * lweight + (right.SatCost + left.DissatCost) * rweight,
						dissatCost: left.DissatCost + right.DissatCost
					);
				case AstElem.Tags.OrCont:
					return new Cost(
						ast: new AstElem.OrCont(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 2,
						satCost: left.SatCost * lweight + (left.DissatCost + right.SatCost) * rweight,
						dissatCost: 0.0
					);
				case AstElem.Tags.OrKey:
					return new Cost(
						ast: new AstElem.OrKey(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 4,
						satCost: 72.0 + (left.SatCost + 2.0) * lweight + (right.SatCost + 1.0) * rweight,
						dissatCost: 0.0
					);
				case AstElem.Tags.OrKeyV:
					return new Cost(
					
						ast: new AstElem.OrKeyV(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 4,
						satCost: 72.0 + (left.SatCost + 2.0) * lweight + (right.SatCost + 1.0) * rweight,
						dissatCost: 0.0
					);
				case AstElem.Tags.OrIf:
					return new Cost(
						ast: new AstElem.OrIf(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 3,
						satCost: (left.SatCost + 2.0) * lweight + (right.SatCost + 1.0) * rweight,
						dissatCost: 1.0 + right.DissatCost
					);
				case AstElem.Tags.OrIfV:
					return new Cost(
					
						ast: new AstElem.OrIfV(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 4,
						satCost: (left.SatCost + 2.0) * lweight + (right.SatCost + 1.0) * rweight,
						dissatCost: 0.0
					);
				case AstElem.Tags.OrNotIf:
					return new Cost(
						ast: new AstElem.OrNotIf(left.Ast, right.Ast),
						pkCost: left.PkCost + right.PkCost + 3,
						satCost: (left.SatCost + 1.0) * lweight + (right.SatCost + 2.0) * rweight,
						dissatCost: left.DissatCost + 1.0
					);
			}

			throw new Exception("Unreachable");
		}
		internal static uint ScriptNumCost(UInt32 n)
		{
			if (n <= 16u)
				return 1u;
			else if (n < 0x80)
				return 2u;
			else if (n < 0x8000)
				return 3u;
			else if (n < 0x800000)
				return 4u;
			else
				return 5u;
		}
	}
}