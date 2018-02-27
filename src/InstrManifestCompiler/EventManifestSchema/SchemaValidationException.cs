namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security;

    /// <summary>
    ///   Indicates an invalid event manifest schema.
    /// </summary>
    [Serializable]
    public sealed class SchemaValidationException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="SchemaValidationException"/> class
        ///   with a specified error message.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for this exception.
        /// </param>
        /// <param name="baseUri">The optional document URI where validation failed.</param>
        /// <param name="lineNumber">The line number where validation failed.</param>
        /// <param name="columnNumber">The column number where validation failed.</param>
        public SchemaValidationException(
            string message, string baseUri, int lineNumber, int columnNumber)
            : base(AppendLineInfo(message, baseUri, lineNumber, columnNumber))
        {
            OriginalMessage = message;
            BaseUri = baseUri;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SchemaValidationException"/> class
        ///   with a specified error message and the exception that is the cause
        ///   of this exception.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for this exception.
        /// </param>
        /// <param name="baseUri">The optional document URI where validation failed.</param>
        /// <param name="lineNumber">The line number where validation failed.</param>
        /// <param name="columnNumber">The column number where validation failed.</param>
        /// <param name="innerException">
        ///   The exception that is the cause of the current exception, or a
        ///   <see langword="null"/> reference (<c>Nothing</c> in Visual Basic)
        ///   if no inner exception is specified.
        /// </param>
        public SchemaValidationException(
            string message, string baseUri, int lineNumber, int columnNumber, Exception innerException)
            : base(AppendLineInfo(message, baseUri, lineNumber, columnNumber), innerException)
        {
            OriginalMessage = message;
            BaseUri = baseUri;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SchemaValidationException"/> class
        ///   with serialized data.
        /// </summary>
        /// <param name="info">
        ///   The serialization information object holding the serialized
        ///   object data in the name-value form.
        /// </param>
        /// <param name="context">
        ///   The contextual information about the source or destination of
        ///   the exception.
        /// </param>
        private SchemaValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            OriginalMessage = info.GetString("OriginalMessage");
            BaseUri = info.GetString("BaseUri");
            LineNumber = info.GetInt32("LineNumber");
            ColumnNumber = info.GetInt32("LinePosition");
        }

        public string OriginalMessage { get; private set; }
        public string BaseUri { get; private set; }
        public int LineNumber { get; private set; }
        public int ColumnNumber { get; private set; }

        /// <summary>
        ///   When overridden in a derived class, sets the
        ///   <see cref="SerializationInfo"/> with information about the
        ///   exception.
        /// </summary>
        /// <param name="info">
        ///   The <see cref="SerializationInfo"/> that holds the serialized
        ///   object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        ///   The <see cref="StreamingContext"/> that contains contextual
        ///   information about the source or destination.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="info"/> is <see langword="null"/>.
        /// </exception>
        [SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            base.GetObjectData(info, context);
            info.AddValue("OriginalMessage", OriginalMessage);
            info.AddValue("BaseUri", BaseUri);
            info.AddValue("LineNumber", LineNumber);
            info.AddValue("ColumnNumber", ColumnNumber);
        }

        private static string AppendLineInfo(
            string message, string baseUri, int lineNumber, int columnNumber)
        {
            if (baseUri == null && lineNumber == -1 && columnNumber == -1)
                return message;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} (at {1} {2},{3})",
                message,
                baseUri ?? "position ",
                lineNumber,
                columnNumber);
        }
    }
}
