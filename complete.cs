//
// complete.cs: Expression that are used for completion suggestions.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003-2009 Novell, Inc.
// Copyright 2011 Xamarin Inc
//
// Completion* classes derive from ExpressionStatement as this allows
// them to pass through the parser in many conditions that require
// statements even when the expression is incomplete (for example
// completing inside a lambda
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.CSharp {

	//
	// A common base class for Completing expressions, it
	// is just a very simple ExpressionStatement
	//
	public abstract class CompletingExpression : ExpressionStatement
	{
		public static void AppendResults (List<string> results, string prefix, IEnumerable<string> names)
		{
			foreach (string name in names) {
				if (name == null)
					continue;

				if (prefix != null && !name.StartsWith (prefix, StringComparison.OrdinalIgnoreCase))
					continue;

				if (results.Contains (name))
					continue;

				results.Add (name);
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return null;
		}

		public override void EmitStatement (EmitContext ec)
		{
			// Do nothing
		}

		public override void Emit (EmitContext ec)
		{
			// Do nothing
		}
	}
	
	public class CompletionSimpleName : CompletingExpression {
		public string Prefix;
		
		public CompletionSimpleName (string prefix, Location l)
		{
			this.loc = l;
			this.Prefix = prefix;
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			var results = new List<string> ();

			ec.CurrentMemberDefinition.GetCompletionStartingWith (Prefix, results);

			throw new CompletionResult (Prefix, results.Distinct ().ToArray ());
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			// Nothing
		}
	}
	
	public class CompletionMemberAccess : CompletingExpression {
		Expression expr;
		string partial_name;
		TypeArguments targs;
		
		public CompletionMemberAccess (Expression e, string partial_name, Location l)
		{
			this.expr = e;
			this.loc = l;
			this.partial_name = partial_name;
		}

		public CompletionMemberAccess (Expression e, string partial_name, TypeArguments targs, Location l)
		{
			this.expr = e;
			this.loc = l;
			this.partial_name = partial_name;
			this.targs = targs;
		}
		
		protected override Expression DoResolve (ResolveContext rc)
		{
			var sn = expr as SimpleName;
			const ResolveFlags flags = ResolveFlags.VariableOrValue | ResolveFlags.Type;

			if (sn != null) {
				var errors_printer = new SessionReportPrinter ();
				var old = rc.Report.SetPrinter (errors_printer);
				try {
					expr = sn.LookupNameExpression (rc, MemberLookupRestrictions.ReadAccess | MemberLookupRestrictions.ExactArity);
				} finally {
					rc.Report.SetPrinter (old);
				}

				if (errors_printer.ErrorsCount != 0)
					return null;

				//
				// Resolve expression which does have type set as we need expression type
				// with disable flow analysis as we don't know whether left side expression
				// is used as variable or type
				//
				if (expr is VariableReference || expr is ConstantExpr || expr is Linq.TransparentMemberAccess) {
					expr = expr.Resolve (rc);
				} else if (expr is TypeParameterExpr) {
					expr.Error_UnexpectedKind (rc, flags, sn.Location);
					expr = null;
				}
			} else {
				expr = expr.Resolve (rc, flags);
			}

			if (expr == null)
				return null;

			TypeSpec expr_type = expr.Type;
			if (expr_type.IsPointer || expr_type.Kind == MemberKind.Void || expr_type == InternalType.NullLiteral || expr_type == InternalType.AnonymousMethod) {
				expr.Error_OperatorCannotBeApplied (rc, loc, ".", expr_type);
				return null;
			}

			if (targs != null) {
				if (!targs.Resolve (rc, true))
					return null;
			}

			var results = new List<string> ();
			var nexpr = expr as NamespaceExpression;
			if (nexpr != null) {
				string namespaced_partial;

				if (partial_name == null)
					namespaced_partial = nexpr.Namespace.Name;
				else
					namespaced_partial = nexpr.Namespace.Name + "." + partial_name;

				rc.CurrentMemberDefinition.GetCompletionStartingWith (namespaced_partial, results);
                IEnumerable<string> startsWithPartialName = [];
                if (partial_name != null)
                {
                    startsWithPartialName = results
                        .Where(l => l.StartsWith (partial_name, StringComparison.OrdinalIgnoreCase))
                        .Where(l => !string.IsNullOrEmpty(l.Trim()));
                }
                results = results
                    .Where(l => l.StartsWith(namespaced_partial, StringComparison.OrdinalIgnoreCase))
                    .Select(l => l.Substring (namespaced_partial.LastIndexOf(".", StringComparison.Ordinal) + 1))
                    .Where(l => !string.IsNullOrEmpty(l.Trim()))
                    .Concat(startsWithPartialName)
                    .ToList();

                results.AddRange(nexpr.Namespace.CompletionGetTypesStartingWith(partial_name ?? String.Empty));
            } else {
				var r = MemberCache.GetCompletitionMembers (rc, expr_type, partial_name).Select (l => l.Name);
				AppendResults (results, partial_name, r);
                var extensionMethods = rc.Module.GlobalRootNamespace.LookupExtensionMethods(rc, partial_name, 0, true);
                var metaType = expr_type.GetMetaInfo();
                var typeInterfaces = metaType.GetInterfaces();
                var extensions = extensionMethods
                    .Select(x => new {extensionType = x.Parameters.Types[0].GetMetaInfo(), x.Name})
                    .Where(x => x.extensionType.IsInterface
                        ? typeInterfaces.Any(y => y.IsAssignableFrom(x.extensionType))
                        : metaType.IsAssignableFrom(x.extensionType))
                    .Select(l => l.Name);
                AppendResults (results, partial_name, extensions);
            }

			throw new CompletionResult (partial_name == null ? "" : partial_name, results.Distinct ().ToArray ());
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			CompletionMemberAccess target = (CompletionMemberAccess) t;

			if (targs != null)
				target.targs = targs.Clone ();

			target.expr = expr.Clone (clonectx);
		}
	}

	public class CompletionElementInitializer : CompletingExpression {
		string partial_name;
		
		public CompletionElementInitializer (string partial_name, Location l)
		{
			this.partial_name = partial_name;
			this.loc = l;
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			var members = MemberCache.GetCompletitionMembers (ec, ec.CurrentInitializerVariable.Type, partial_name);

// TODO: Does this mean exact match only ?
//			if (partial_name != null && results.Count > 0 && result [0] == "")
//				throw new CompletionResult ("", new string [] { "=" });

			var results = members.Where (l => (l.Kind & (MemberKind.Field | MemberKind.Property)) != 0).Select (l => l.Name).ToList ();
			if (partial_name != null) {
				var temp = new List<string> ();
				AppendResults (temp, partial_name, results);
				results = temp;
			}

			throw new CompletionResult (partial_name == null ? "" : partial_name, results.Distinct ().ToArray ());
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			// Nothing
		}
	}

	public class EmptyCompletion : CompletingExpression
	{
		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			throw new CompletionResult ("", new string [0]);
		}
	}
	
}
