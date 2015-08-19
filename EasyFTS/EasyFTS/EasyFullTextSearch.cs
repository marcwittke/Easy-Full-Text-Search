//////////////////////////////////////////////////////////////////////////////
// Easy Full Text Search .NET Class Library
//
// This source code and all associated files and resources are copyrighted by
// the author(s). This source code and all associated files and resources may
// be used as long as they are used according to the terms and conditions set
// forth in The Code Project Open License (CPOL).
//
// Copyright (c) 2015 Jonathan Wood
// http://www.softcircuits.com
// http://www.blackbeltcoder.com
//

using System;
using System.Collections.Generic;

namespace EasyFTS
{
    /// <summary>
    /// Class to convert "user-friendly" search to SQL Server full-text search syntax.
    /// Supports a Google-like syntax as described in the remarks. No exceptions are thrown
    /// for badly formed input. The code simply constructs the best query it can.
    /// </summary>
    /// <remarks>
    /// The following list shows how various syntaxes are interpreted.
    /// 
    /// abc                     Find inflectional forms of abc
    /// ~abc                    Find thesaurus variations of abc
    /// +abc                    Find exact term abc
    /// "abc"                   Find exact term abc
    /// abc*                    Finds words that start with abc
    /// -abc                    Do not include results that contain inflectional forms of abc
    /// abc def                 Find inflectional forms of both abc and def
    /// abc or def              Find inflectional forms of either abc or def
    /// &lt;abc def&gt;         Find inflectional forms of abc near def
    /// abc and (def or ghi)    Find inflectional forms of both abc and either def or ghi
    /// </remarks>
    public class EasyFullTextSearch
    {
        /// <summary>
        /// Query term forms.
        /// </summary>
        protected enum TermForms
        {
            Inflectional,
            Thesaurus,
            Literal,
        }

        /// <summary>
        /// Term conjunction types.
        /// </summary>
        protected enum ConjunctionTypes
        {
            And,
            Or,
            Near,
        }

        /// <summary>
        /// Common interface for expression nodes
        /// </summary>
        protected interface INode
        {
            /// <summary>
            /// Indicates this term (or both child terms) should be excluded from
            /// the results
            /// </summary>
            bool Exclude { get; set; }

            /// <summary>
            /// Indicates this term is enclosed in parentheses
            /// </summary>
            bool Grouped { get; set; }
        }

        /// <summary>
        /// Terminal (leaf) expression node class.
        /// </summary>
        private class TerminalNode : INode
        {
            // Interface members
            public bool Exclude { get; set; }
            public bool Grouped { get; set; }

            // Class members
            public string Term { get; set; }
            public TermForms TermForm { get; set; }

            // Convert node to string
            public override string ToString()
            {
                string fmt = String.Empty;
                if (TermForm == TermForms.Inflectional)
                    fmt = "{0}FORMSOF(INFLECTIONAL, {1})";
                else if (TermForm == TermForms.Thesaurus)
                    fmt = "{0}FORMSOF(THESAURUS, {1})";
                else if (TermForm == TermForms.Literal)
                    fmt = "{0}\"{1}\"";
                return String.Format(fmt,
                    Exclude ? "NOT " : String.Empty,
                    Term);
            }
        }

        /// <summary>
        /// Internal (non-leaf) expression node class
        /// </summary>
        private class InternalNode : INode
        {
            // Interface members
            public bool Exclude { get; set; }
            public bool Grouped { get; set; }

            // Class members
            public INode Child1 { get; set; }
            public INode Child2 { get; set; }
            public ConjunctionTypes Conjunction { get; set; }

            // Convert node to string
            public override string ToString()
            {
                return String.Format(Grouped ? "({0} {1} {2})" : "{0} {1} {2}",
                    Child1.ToString(),
                    Conjunction.ToString().ToUpper(),
                    Child2.ToString());
            }
        }

        // Characters not allowed in unquoted search terms
        protected const string Punctuation = "~\"`!@#$%^&*()-+=[]{}\\|;:,.<>?/";

        /// <summary>
        /// Collection of stop words. These words will not
        /// be included in the resulting query unless quoted.
        /// </summary>
        public HashSet<string> StopWords { get; set; }

        // Class constructor
        public EasyFullTextSearch()
        {
            StopWords = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Converts an "easy" search term to a full-text search term.
        /// </summary>
        /// <param name="query">Search term to convert</param>
        /// <returns>A valid full-text search query</returns>
        public string ToFtsQuery(string query)
        {
            INode node = FixUpExpressionTree(ParseNode(query, ConjunctionTypes.And), true);
            return (node != null) ? node.ToString() : String.Empty;
        }

        /// <summary>
        /// Parses a query segment and converts it to an expression
        /// tree.
        /// </summary>
        /// <param name="query">Query segment to convert</param>
        /// <param name="defaultConjunction">Implicit conjunction type</param>
        /// <returns>Root node of expression tree</returns>
        private INode ParseNode(string query, ConjunctionTypes defaultConjunction)
        {
            TermForms termForm = TermForms.Inflectional;
            bool termExclude = false;
            ConjunctionTypes conjunction = defaultConjunction;
            bool resetState = true;
            INode root = null;
            INode node;
            string term;

            TextParser parser = new TextParser(query);
            while (!parser.EndOfText)
            {
                if (resetState)
                {
                    // Reset modifiers
                    termForm = TermForms.Inflectional;
                    termExclude = false;
                    conjunction = defaultConjunction;
                    resetState = false;
                }

                parser.MovePastWhitespace();
                if (!parser.EndOfText && Punctuation.IndexOf(parser.Peek()) < 0)
                {
                    // Extract query term
                    int start = parser.Position;
                    parser.MoveAhead();
                    while (!parser.EndOfText &&
                        Punctuation.IndexOf(parser.Peek()) < 0 &&
                        !Char.IsWhiteSpace(parser.Peek()))
                        parser.MoveAhead();

                    // Allow trailing wildcard
                    if (parser.Peek() == '*')
                    {
                        parser.MoveAhead();
                        termForm = TermForms.Literal;
                    }

                    // Interpret token
                    term = parser.Extract(start, parser.Position);
                    if (String.Compare(term, "AND", true) == 0)
                        conjunction = ConjunctionTypes.And;
                    else if (String.Compare(term, "OR", true) == 0)
                        conjunction = ConjunctionTypes.Or;
                    else if (String.Compare(term, "NEAR", true) == 0)
                        conjunction = ConjunctionTypes.Near;
                    else if (String.Compare(term, "NOT", true) == 0)
                        termExclude = true;
                    else
                    {
                        root = AddNode(root, term, termForm, termExclude, conjunction);
                        resetState = true;
                    }
                    continue;
                }
                else if (parser.Peek() == '"')
                {
                    // Match next term exactly
                    termForm = TermForms.Literal;
                    // Extract quoted term
                    term = ExtractQuote(parser);
                    root = AddNode(root, term.Trim(), termForm, termExclude, conjunction);
                    resetState = true;
                }
                else if (parser.Peek() == '(')
                {
                    // Parse parentheses block
                    term = ExtractBlock(parser, '(', ')');
                    node = ParseNode(term, defaultConjunction);
                    root = AddNode(root, node, conjunction, true);
                    resetState = true;
                }
                else if (parser.Peek() == '<')
                {
                    // Parse angle brackets block
                    term = ExtractBlock(parser, '<', '>');
                    node = ParseNode(term, ConjunctionTypes.Near);
                    root = AddNode(root, node, conjunction);
                    resetState = true;
                }
                else if (parser.Peek() == '-')
                {
                    // Match when next term is not present
                    termExclude = true;
                }
                else if (parser.Peek() == '+')
                {
                    // Match next term exactly
                    termForm = TermForms.Literal;
                }
                else if (parser.Peek() == '~')
                {
                    // Match synonyms of next term
                    termForm = TermForms.Thesaurus;
                }
                // Advance to next character
                parser.MoveAhead();
            }
            return root;
        }

        /// <summary>
        /// Fixes any portions of the expression tree that would produce an invalid SQL Server full-text
        /// query.
        /// </summary>
        /// <remarks>
        /// While our expression tree may be properly constructed, it may represent a query that
        /// is not supported by SQL Server. This method traverses the expression tree and corrects
        /// problem expressions as described below.
        /// 
        ///     NOT term1 AND term2         Subexpressions swapped.
        ///     NOT term1                   Expression discarded.
        ///     NOT term1 AND NOT term2     Expression discarded if node is grouped (parenthesized)
        ///                                 or is the root node; otherwise, the parent node may
        ///                                 contain another subexpression that will make this one
        ///                                 valid.
        ///     term1 OR NOT term2          Expression discarded.
        ///     term1 NEAR NOT term2        NEAR conjunction changed to AND.*
        ///
        /// * This method converts all NEAR conjunctions to AND when either subexpression is not
        /// an InternalNode with the form TermForms.Literal.
        /// </remarks>
        /// <param name="node">Node to fix up</param>
        /// <param name="isRoot">True if node is the tree's root node</param>
        protected INode FixUpExpressionTree(INode node, bool isRoot = false)
        {
            // Test for empty expression tree
            if (node == null) return null;

            // Special handling for internal nodes
            if (node is InternalNode)
            {
                // Fix up child nodes
                var internalNode = node as InternalNode;
                internalNode.Child1 = FixUpExpressionTree(internalNode.Child1);
                internalNode.Child2 = FixUpExpressionTree(internalNode.Child2);

                // Correct subexpressions incompatible with conjunction type
                if (internalNode.Conjunction == ConjunctionTypes.Near)
                {
                    // If either subexpression is incompatible with NEAR conjunction then change to AND
                    if (IsInvalidWithNear(internalNode.Child1) || IsInvalidWithNear(internalNode.Child2))
                        internalNode.Conjunction = ConjunctionTypes.And;
                }
                else if (internalNode.Conjunction == ConjunctionTypes.Or)
                {
                    // Eliminate subexpressions not valid with OR conjunction
                    if (IsInvalidWithOr(internalNode.Child1))
                        internalNode.Child1 = null;
                    if (IsInvalidWithOr(internalNode.Child2))
                        internalNode.Child1 = null;
                }

                // Handle eliminated child expressions
                if (internalNode.Child1 == null && internalNode.Child2 == null)
                {
                    // Eliminate parent node if both child nodes were eliminated
                    return null;
                }
                else if (internalNode.Child1 == null)
                {
                    // Child1 eliminated so return only Child2
                    node = internalNode.Child2;
                }
                else if (internalNode.Child2 == null)
                {
                    // Child2 eliminated so return only Child1
                    node = internalNode.Child1;
                }
                else
                {
                    // Determine if entire expression is an exclude expression
                    internalNode.Exclude = (internalNode.Child1.Exclude && internalNode.Child2.Exclude);
                    // If only first child expression is an exclude expression,
                    // then simply swap child expressions
                    if (!internalNode.Exclude && internalNode.Child1.Exclude)
                    {
                        var temp = internalNode.Child1;
                        internalNode.Child1 = internalNode.Child2;
                        internalNode.Child2 = temp;
                    }
                }
            }
            // Eliminate expression group if it contains only exclude expressions
            return ((node.Grouped || isRoot) && node.Exclude) ? null : node;
        }

        /// <summary>
        /// Determines if the specified node is invalid on either side of a NEAR conjuction.
        /// </summary>
        /// <param name="node">Node to test</param>
        protected bool IsInvalidWithNear(INode node)
        {
            // NEAR is only valid with TerminalNodes with form TermForms.Literal
            return !(node is TerminalNode) || ((TerminalNode)node).TermForm != TermForms.Literal;
        }

        /// <summary>
        /// Determines if the specified node is invalid on either side of an OR conjunction.
        /// </summary>
        /// <param name="node">Node to test</param>
        protected bool IsInvalidWithOr(INode node)
        {
            // OR is only valid with non-null, non-excluded (NOT) subexpressions
            return node == null || node.Exclude == true;
        }

        /// <summary>
        /// Creates an expression node and adds it to the
        /// give tree.
        /// </summary>
        /// <param name="root">Root node of expression tree</param>
        /// <param name="term">Term for this node</param>
        /// <param name="termForm">Indicates form of this term</param>
        /// <param name="termExclude">Indicates if this is an excluded term</param>
        /// <param name="conjunction">Conjunction used to join with other nodes</param>
        /// <returns>The new root node</returns>
        protected INode AddNode(INode root, string term, TermForms termForm, bool termExclude, ConjunctionTypes conjunction)
        {
            if (term.Length > 0 && !IsStopWord(term))
            {
                INode node = new TerminalNode
                {
                    Term = term,
                    TermForm = termForm,
                    Exclude = termExclude
                };
                root = AddNode(root, node, conjunction);
            }
            return root;
        }

        /// <summary>
        /// Adds an expression node to the given tree.
        /// </summary>
        /// <param name="root">Root node of expression tree</param>
        /// <param name="node">Node to add</param>
        /// <param name="conjunction">Conjunction used to join with other nodes</param>
        /// <returns>The new root node</returns>
        protected INode AddNode(INode root, INode node, ConjunctionTypes conjunction, bool group = false)
        {
            if (node != null)
            {
                node.Grouped = group;
                if (root != null)
                    root = new InternalNode
                    {
                        Child1 = root,
                        Child2 = node,
                        Conjunction = conjunction
                    };
                else
                    root = node;
            }
            return root;
        }

        /// <summary>
        /// Extracts a block of text delimited by the specified open and close
        /// characters. It is assumed the parser is positioned at an
        /// occurrence of the open character. The open and closing characters
        /// are not included in the returned string. On return, the parser is
        /// positioned at the closing character or at the end of the text if
        /// the closing character was not found.
        /// </summary>
        /// <param name="parser">TextParser object</param>
        /// <param name="openChar">Start-of-block delimiter</param>
        /// <param name="closeChar">End-of-block delimiter</param>
        /// <returns>The extracted text</returns>
        private string ExtractBlock(TextParser parser, char openChar, char closeChar)
        {
            // Track delimiter depth
            int depth = 1;

            // Extract characters between delimiters
            parser.MoveAhead();
            int start = parser.Position;
            while (!parser.EndOfText)
            {
                if (parser.Peek() == openChar)
                {
                    // Increase block depth
                    depth++;
                }
                else if (parser.Peek() == closeChar)
                {
                    // Decrease block depth
                    depth--;
                    // Test for end of block
                    if (depth == 0)
                        break;
                }
                else if (parser.Peek() == '"')
                {
                    // Don't count delimiters within quoted text
                    ExtractQuote(parser);
                }
                // Move to next character
                parser.MoveAhead();
            }
            return parser.Extract(start, parser.Position);
        }

        /// <summary>
        /// Extracts a block of text delimited by double quotes. It is
        /// assumed the parser is positioned at the first quote. The
        /// quotes are not included in the returned string. On return,
        /// the parser is positioned at the closing quote or at the end of
        /// the text if the closing quote was not found.
        /// </summary>
        /// <param name="parser">TextParser object</param>
        /// <returns>The extracted text</returns>
        private string ExtractQuote(TextParser parser)
        {
            // Extract contents of quote
            parser.MoveAhead();
            int start = parser.Position;
            while (!parser.EndOfText && parser.Peek() != '"')
                parser.MoveAhead();
            return parser.Extract(start, parser.Position);
        }

        /// <summary>
        /// Determines if the given word has been identified as
        /// a stop word.
        /// </summary>
        /// <param name="word">Word to check</param>
        protected bool IsStopWord(string word)
        {
            return StopWords.Contains(word);
        }
    }
}
