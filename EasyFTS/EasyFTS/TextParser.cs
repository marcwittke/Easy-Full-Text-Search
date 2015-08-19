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
        /// Constructs a TextParse instance.
        /// </summary>
        /// <param name="text">Text to be parsed.</param>
        public TextParser(string text = null)
        {
            Reset(text);
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
        /// Moves the current position to the next character that is not whitespace.
        /// </summary>
        public void MovePastWhitespace()
        {
            while (char.IsWhiteSpace(Peek()))
                MoveAhead();
        }
    }
}
