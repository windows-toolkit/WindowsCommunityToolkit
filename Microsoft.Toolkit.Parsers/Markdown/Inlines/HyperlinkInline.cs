// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Toolkit.Parsers.Markdown.Helpers;

namespace Microsoft.Toolkit.Parsers.Markdown.Inlines
{
    /// <summary>
    /// Represents a type of hyperlink where the text and the target URL cannot be controlled
    /// independently.
    /// </summary>
    public class HyperlinkInline : MarkdownInline, IInlineLeaf, ILinkElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HyperlinkInline"/> class.
        /// </summary>
        public HyperlinkInline()
            : base(MarkdownInlineType.RawHyperlink)
        {
        }

        /// <summary>
        /// Gets or sets the text to display.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the URL to link to.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets this type of hyperlink does not have a tooltip.
        /// </summary>
        string ILinkElement.Tooltip => null;

        /// <summary>
        /// Gets or sets the type of hyperlink.
        /// </summary>
        public HyperlinkType LinkType { get; set; }

        /// <summary>
        /// Attempts to parse a URL within angle brackets e.g. "http://www.reddit.com".
        /// </summary>
        public class AngleBracketLinkParser : Parser<HyperlinkInline>
        {
            /// <inheritdoc/>
            protected override InlineParseResult<HyperlinkInline> ParseInternal(LineBlock markdown, int tripLine, int tripPos, MarkdownDocument document, IEnumerable<Type> ignoredParsers)
            {
                int innerStart = tripPos + 1;

                var line = markdown.Lines[tripLine];

                // Check for a known scheme e.g. "https://".
                int pos = -1;
                foreach (var scheme in MarkdownDocument.KnownSchemes)
                {
                    if (maxEnd - innerStart >= scheme.Length && line.Slice(innerStart, scheme.Length).StartsWith(scheme.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        // URL scheme found.
                        pos = innerStart + scheme.Length;
                        break;
                    }
                }

                if (pos == -1)
                {
                    return null;
                }

                // Angle bracket links should not have any whitespace.
                int innerEnd = line.Slice(pos, maxEnd - pos).IndexOfAny(" \t\r\n>".AsSpan()) + pos;
                if (innerEnd < pos || line[innerEnd] != '>')
                {
                    return null;
                }

                // There should be at least one character after the http://.
                if (innerEnd == pos)
                {
                    return null;
                }

                var url = line.Slice(innerStart, innerEnd - innerStart).ToString();
                return InlineParseResult.Create(new HyperlinkInline { Url = url, Text = url, LinkType = HyperlinkType.BracketedUrl }, tripPos, innerEnd + 1);
            }

            /// <inheritdoc/>
            public override IEnumerable<char> TripChar => "<";
        }

        /// <summary>
        /// Attempts to parse a URL e.g. "http://www.reddit.com".
        /// </summary>
        public class UrlParser : Parser<HyperlinkInline>
        {
            /// <inheritdoc/>
            protected override InlineParseResult<HyperlinkInline> ParseInternal(LineBlock markdown, int tripLine, int tripPos, MarkdownDocument document, IEnumerable<Type> ignoredParsers)
            {
                int start = -1;
                var line = markdown.Lines[tripLine];

                // Check for a known scheme e.g. "https://".
                foreach (var scheme in MarkdownDocument.KnownSchemes)
                {
                    int schemeStart = tripPos - scheme.Length;
                    if (schemeStart >= 0 && schemeStart <= maxEnd - scheme.Length && line.Slice(schemeStart, scheme.Length).StartsWith(scheme.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        // URL scheme found.
                        start = schemeStart;
                        break;
                    }
                }

                if (start == -1)
                {
                    return null;
                }

                // The previous character must be non-alphanumeric i.e. "ahttp://t.co" is not a valid URL.
                if (start > 0 && char.IsLetter(line[start - 1]))
                {
                    return null;
                }

                // The URL must have at least one character after the http:// and at least one dot.
                int pos = tripPos + 3;
                if (pos > maxEnd)
                {
                    return null;
                }

                int dotIndex = line.Slice(pos, maxEnd - pos).IndexOf('.') + pos;
                if (dotIndex < pos || dotIndex == pos)
                {
                    return null;
                }

                // Find the end of the URL.
                int end = FindUrlEnd(line, dotIndex + 1, maxEnd);

                var url = line.Slice(start, end - start).ToString();
                return InlineParseResult.Create(new HyperlinkInline { Url = url, Text = url, LinkType = HyperlinkType.FullUrl }, start, end);
            }

            /// <inheritdoc/>
            public override IEnumerable<char> TripChar => ":";
        }

        /// <summary>
        /// Parses a redit link.
        /// </summary>
        public class ReditLinkParser : Parser<HyperlinkInline>
        {
            /// <inheritdoc/>
            protected override InlineParseResult<HyperlinkInline> ParseInternal(LineBlock markdown, int tripLine, int tripPos, MarkdownDocument document, IEnumerable<Type> ignoredParsers)
            {
                var line = markdown.Lines[tripLine];
                var result = ParseDoubleSlashLink(line, tripPos, maxEnd);
                if (result != null)
                {
                    return result;
                }

                return ParseSingleSlashLink(line, tripPos, maxEnd);
            }

            /// <inheritdoc/>
            public override IEnumerable<char> TripChar => "/";

            /// <summary>
            /// Parse a link of the form "/r/news" or "/u/quinbd".
            /// </summary>
            /// <param name="markdown"> The markdown text. </param>
            /// <param name="start"> The location to start parsing. </param>
            /// <param name="maxEnd"> The location to stop parsing. </param>
            /// <returns> A parsed subreddit or user link, or <c>null</c> if this is not a subreddit link. </returns>
            private static InlineParseResult<HyperlinkInline> ParseDoubleSlashLink(ReadOnlySpan<char> markdown, int start, int maxEnd)
            {
                // The minimum length is 4 characters ("/u/u").
                if (start > maxEnd - 4)
                {
                    return null;
                }

                // Determine the type of link (subreddit or user).
                HyperlinkType linkType;
                if (markdown[start + 1] == 'r')
                {
                    linkType = HyperlinkType.Subreddit;
                }
                else if (markdown[start + 1] == 'u')
                {
                    linkType = HyperlinkType.User;
                }
                else
                {
                    return null;
                }

                // Check that there is another slash.
                if (markdown[start + 2] != '/')
                {
                    return null;
                }

                // Find the end of the link.
                int end = FindEndOfRedditLink(markdown, start + 3, maxEnd);

                // Subreddit names must be at least two characters long, users at least one.
                if (end - start < (linkType == HyperlinkType.User ? 4 : 5))
                {
                    return null;
                }

                // We found something!
                var text = markdown.Slice(start, end - start).ToString();
                return InlineParseResult.Create(new HyperlinkInline { Text = text, Url = text, LinkType = linkType }, start, end);
            }

            /// <summary>
            /// Parse a link of the form "r/news" or "u/quinbd".
            /// </summary>
            /// <param name="markdown"> The markdown text. </param>
            /// <param name="start"> The location to start parsing. </param>
            /// <param name="maxEnd"> The location to stop parsing. </param>
            /// <returns> A parsed subreddit or user link, or <c>null</c> if this is not a subreddit link. </returns>
            private static InlineParseResult<HyperlinkInline> ParseSingleSlashLink(ReadOnlySpan<char> markdown, int start, int maxEnd)
            {
                // The minimum length is 3 characters ("u/u").
                start--;
                if (start < 0 || start > maxEnd - 3)
                {
                    return null;
                }

                // Determine the type of link (subreddit or user).
                HyperlinkType linkType;
                if (markdown[start] == 'r')
                {
                    linkType = HyperlinkType.Subreddit;
                }
                else if (markdown[start] == 'u')
                {
                    linkType = HyperlinkType.User;
                }
                else
                {
                    return null;
                }

                // If the link doesn't start with '/', then the previous character must be
                // non-alphanumeric i.e. "bear/trap" is not a valid subreddit link.
                if (start >= 1 && (char.IsLetterOrDigit(markdown[start - 1]) || markdown[start - 1] == '/'))
                {
                    return null;
                }

                // Find the end of the link.
                int end = FindEndOfRedditLink(markdown, start + 2, maxEnd);

                // Subreddit names must be at least two characters long, users at least one.
                if (end - start < (linkType == HyperlinkType.User ? 3 : 4))
                {
                    return null;
                }

                // We found something!
                var text = markdown.Slice(start, end - start).ToString();
                return InlineParseResult.Create(new HyperlinkInline { Text = text, Url = "/" + text, LinkType = linkType }, start, end);
            }

            /// <summary>
            /// Finds the next character that is not a letter, digit or underscore in a range.
            /// </summary>
            /// <param name="markdown"> The markdown text. </param>
            /// <param name="start"> The location to start searching. </param>
            /// <param name="end"> The location to stop searching. </param>
            /// <returns> The location of the next character that is not a letter, digit or underscore. </returns>
            private static int FindEndOfRedditLink(ReadOnlySpan<char> markdown, int start, int end)
            {
                int pos = start;
                while (pos < markdown.Length && pos < end)
                {
                    char c = markdown[pos];
                    if ((c < 'a' || c > 'z') &&
                        (c < 'A' || c > 'Z') &&
                        (c < '0' || c > '9') &&
                        c != '_' && c != '/')
                    {
                        return pos;
                    }

                    pos++;
                }

                return end;
            }
        }

        /// <summary>
        /// Attempts to parse a URL without a scheme e.g. "www.reddit.com".
        /// </summary>
        public class PartialLinkParser : Parser<HyperlinkInline>
        {
            /// <inheritdoc/>
            protected override InlineParseResult<HyperlinkInline> ParseInternal(LineBlock markdown, int tripLine, int tripPos, MarkdownDocument document, IEnumerable<Type> ignoredParsers)
            {
                var line = markdown.Lines[tripLine];
                int start = tripPos - 3;
                if (start < 0 || line[start] != 'w' || line[start + 1] != 'w' || line[start + 2] != 'w')
                {
                    return null;
                }

                // The character before the "www" must be non-alphanumeric i.e. "bwww.reddit.com" is not a valid URL.
                if (start >= 1 && (char.IsLetterOrDigit(line[start - 1]) || line[start - 1] == '<'))
                {
                    return null;
                }

                // The URL must have at least one character after the www.
                if (start >= maxEnd - 4)
                {
                    return null;
                }

                // Find the end of the URL.
                int end = FindUrlEnd(line, start + 4, maxEnd);

                var url = line.Slice(start, end - start).ToString();
                return InlineParseResult.Create(new HyperlinkInline { Url = "http://" + url, Text = url, LinkType = HyperlinkType.PartialUrl }, start, end);
            }

            /// <inheritdoc/>
            public override IEnumerable<char> TripChar => ".";
        }

        /// <summary>
        /// Attempts to parse an email address e.g. "test@reddit.com".
        /// </summary>
        public class EmailAddressParser : Parser<HyperlinkInline>
        {
            /// <inheritdoc/>
            protected override InlineParseResult<HyperlinkInline> ParseInternal(LineBlock markdown, int tripLine, int tripPos, MarkdownDocument document, IEnumerable<Type> ignoredParsers)
            {
                // Search backwards until we find a character which is not a letter, digit, or one of
                // these characters: '+', '-', '_', '.'.
                // Note: it is intended that this code match the reddit.com markdown parser; there are
                // many characters which are legal in email addresses but which aren't picked up by
                // reddit (for example: '$' and '!').

                // Special characters as per https://en.wikipedia.org/wiki/Email_address#Local-part allowed
                char[] allowedchars = new char[] { '!', '#', '$', '%', '&', '\'', '*', '+', '-', '/', '=', '?', '^', '_', '`', '{', '|', '}', '~' };

                var line = markdown.Lines[tripLine];

                int start = tripPos;
                while (start > 0)
                {
                    char c = line[start - 1];
                    if ((c < 'a' || c > 'z') &&
                        (c < 'A' || c > 'Z') &&
                        (c < '0' || c > '9') &&
                        !allowedchars.Contains(c))
                    {
                        break;
                    }

                    start--;
                }

                // There must be at least one character before the '@'.
                if (start == tripPos)
                {
                    return null;
                }

                // Search forwards until we find a character which is not a letter, digit, or one of
                // these characters: '-', '_'.
                // Note: it is intended that this code match the reddit.com markdown parser;
                // technically underscores ('_') aren't allowed in a host name.
                int dotIndex = tripPos + 1;
                while (dotIndex < maxEnd)
                {
                    char c = line[dotIndex];
                    if ((c < 'a' || c > 'z') &&
                        (c < 'A' || c > 'Z') &&
                        (c < '0' || c > '9') &&
                        c != '-' && c != '_')
                    {
                        break;
                    }

                    dotIndex++;
                }

                // We are expecting a dot.
                if (dotIndex == maxEnd || line[dotIndex] != '.')
                {
                    return null;
                }

                // Search forwards until we find a character which is not a letter, digit, or one of
                // these characters: '.', '-', '_'.
                // Note: it is intended that this code match the reddit.com markdown parser;
                // technically underscores ('_') aren't allowed in a host name.
                int end = dotIndex + 1;
                while (end < maxEnd)
                {
                    char c = line[end];
                    if ((c < 'a' || c > 'z') &&
                        (c < 'A' || c > 'Z') &&
                        (c < '0' || c > '9') &&
                        c != '.' && c != '-' && c != '_')
                    {
                        break;
                    }

                    end++;
                }

                // There must be at least one character after the dot.
                if (end == dotIndex + 1)
                {
                    return null;
                }

                // We found an email address!
                var emailAddress = line.Slice(start, end - start).ToString();
                return InlineParseResult.Create(new HyperlinkInline { Url = "mailto:" + emailAddress, Text = emailAddress, LinkType = HyperlinkType.Email }, start, end);
            }

            /// <inheritdoc/>
            public override IEnumerable<char> TripChar => "@";
        }

        /// <summary>
        /// Converts the object into it's textual representation.
        /// </summary>
        /// <returns> The textual representation of this object. </returns>
        public override string ToString()
        {
            if (Text == null)
            {
                return base.ToString();
            }

            return Text;
        }

        /// <summary>
        /// Finds the end of a URL.
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start searching. </param>
        /// <param name="maxEnd"> The location to stop searching. </param>
        /// <returns> The location of the end of the URL. </returns>
        private static int FindUrlEnd(ReadOnlySpan<char> markdown, int start, int maxEnd)
        {
            // For some reason a less than character ends a URL...
            int end = markdown.Slice(start, maxEnd - start).IndexOfAny(" \t\r\n<".AsSpan()) + start;
            if (end < start)
            {
                end = maxEnd;
            }

            // URLs can't end on a punctuation character.
            while (end - 1 > start)
            {
                if (Array.IndexOf(new char[] { ')', '}', ']', '!', ';', '.', '?', ',' }, markdown[end - 1]) < 0)
                {
                    break;
                }

                end--;
            }

            return end;
        }
    }
}