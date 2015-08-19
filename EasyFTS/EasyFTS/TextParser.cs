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
using System.Linq;
using System.Text;

namespace EasyFTS
{
    /// <summary>
    /// Text parser helper class.
    /// </summary>
    public class TextParser
    {
        /// <summary>
        /// Represents an invalid character
        /// </summary>
        public const char NullChar = (char)0;

        /// <summary>
        /// Returns the current text being parsed.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Returns the current text position.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Returns the number of characters not yet parsed.
        /// </summary>
        /// <remarks>
        /// Returns the length of the current text being parsed minus the current position.
        /// </remarks>
        public int Remaining => Text.Length - Position;

        /// <summary>
        /// Constructs a TextParse instance.
        /// </summary>
        /// <param name="text">Text to be parsed.</param>
        public TextParser(string text = null)
        {
            Reset(text);
        }

        /// <summary>
        /// Resets the current position to the start of the current text.
        /// </summary>
        public void Reset()
        {
            Position = 0;
        }

        /// <summary>
        /// Sets the current text and resets the current position to the start of it.
        /// </summary>
        /// <param name="text">Text to be parsed.</param>
        public void Reset(string text = null)
        {
            Text = text ?? String.Empty;
            Position = 0;
        }

        /// <summary>
        /// Indicates if the current position is at the end of the current document.
        /// </summary>
        public bool EndOfText => (Position >= Text.Length);

        /// <summary>
        /// Returns the character at the current position, or a null character if we're
        /// at the end of the document.
        /// </summary>
        /// <returns>The character at the current position.</returns>
        public char Peek() => Peek(0);

        /// <summary>
        /// Returns the character at the specified number of characters beyond the current
        /// position, or a null character if the specified position is at the end of the
        /// document
        /// </summary>
        /// <param name="ahead">The number of characters beyond the current position.</param>
        /// <returns>The character at the specified position.</returns>
        public char Peek(int ahead)
        {
            int pos = (Position + ahead);
            return (pos < Text.Length) ? Text[pos] : NullChar;
        }

        /// <summary>
        /// Extracts a substring from the specified position to the end of the text.
        /// </summary>
        /// <param name="start">0-based position of first character to extract.</param>
        /// <returns>Returns the extracted string.</returns>
        public string Extract(int start) => Extract(start, Text.Length);

        /// <summary>
        /// Extracts a substring from the specified range of the current text.
        /// </summary>
        /// <param name="start">0-based position of first character to extract.</param>
        /// <param name="end">0-based position of the character that follows the last
        /// character to extract.</param>
        /// <returns>Returns the extracted string</returns>
        public string Extract(int start, int end) => Text.Substring(start, end - start);

        /// <summary>
        /// Moves the current position ahead one character.
        /// </summary>
        public void MoveAhead() => MoveAhead(1);

        /// <summary>
        /// Moves the current position ahead the specified number of characters
        /// </summary>
        /// <param name="ahead">The number of characters to move ahead</param>
        public void MoveAhead(int ahead)
        {
            Position = Math.Min(Position + ahead, Text.Length);
        }

        /// <summary>
        /// Moves to the next occurrence of the specified string
        /// </summary>
        /// <param name="s">String to find</param>
        /// <param name="ignoreCase">Indicates if case-insensitive comparisons are used</param>
        public void MoveTo(string s, bool ignoreCase = false)
        {
            Position = Text.IndexOf(s, Position, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (Position < 0)
                Position = Text.Length;
        }

        /// <summary>
        /// Moves to the next occurrence of any one of the specified
        /// characters.
        /// </summary>
        /// <param name="chars">Array of characters to find</param>
        public void MoveTo(params char[] chars)
        {
            Position = Text.IndexOfAny(chars, Position);
            if (Position < 0)
                Position = Text.Length;
        }

        /// <summary>
        /// Moves to the next occurrence of any character that is not one
        /// of the specified characters.
        /// </summary>
        /// <param name="chars">Array of characters to move past</param>
        public void MovePast(params char[] chars)
        {
            while (!EndOfText && chars.Contains(Peek()))
                MoveAhead();
        }

        /// <summary>
        /// Moves the current position to the first character that is part of a newline.
        /// </summary>
        public void MoveToEndOfLine() => MoveTo('\r', '\n' );

        /// <summary>
        /// Moves the current position to the next character that is not whitespace.
        /// </summary>
        public void MovePastWhitespace()
        {
            while (char.IsWhiteSpace(Peek()))
                MoveAhead();
        }

        /// <summary>
        /// Moves to the end of quoted text and returns the text within the quotes. Discards the
        /// quote characters. The current character is assumed to be the quote character. Two
        /// consecutive quotes are treated as a single literal character.
        /// </summary>
        /// <param name="escapeCharacter"></param>
        public string ParseQuotedText(char escapeCharacter = NullChar)
        {
            // Get quote character
            char quote = Peek();

            if (escapeCharacter == NullChar)
                escapeCharacter = quote;

            // Jump to start of quoted text
            MoveAhead();
            // Parse quoted text
            StringBuilder builder = new StringBuilder();
            while (!EndOfText)
            {
                int start = Position;
                // Move to next quote
                MoveTo(quote);
                // Capture quoted text
                builder.Append(Extract(start, Position));
                // Skip over quote
                MoveAhead();
                // Two consecutive quotes treated as quote literal
                if (Peek() == quote)
                {
                    builder.Append(quote);
                    MoveAhead();
                }
                else break; // Done if single closing quote
            }
            return builder.ToString();
        }
    }
}
