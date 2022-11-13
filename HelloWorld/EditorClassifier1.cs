using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace HelloWorld
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "EditorClassifier1" classification type.
    /// </summary>
    internal class EditorClassifier1 : IClassifier
    {
        /// <summary>
        /// Classification type.
        /// </summary>
        private readonly IClassificationType classificationType;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorClassifier1"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal EditorClassifier1(IClassificationTypeRegistryService registry, ITextBuffer inBuffer)
        {
            this.classificationType = registry.GetClassificationType("EditorClassifier1");

            this.buffer = inBuffer ?? throw new ArgumentNullException(nameof(inBuffer));
            this.snapshot = inBuffer.CurrentSnapshot;
        }

        #region IClassifier

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
        /// </summary>
        /// <remarks>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </remarks>
        /// <param name="span">The span currently being classified.</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            ReParse();

            return classificationSpans;
        }

        ITextBuffer buffer;
        ITextSnapshot snapshot;
        List<ClassificationSpan> classificationSpans = new List<ClassificationSpan>();

        public void ReParse()
        {
            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            classificationSpans = new List<ClassificationSpan>();

            var tagger = buffer.Properties.GetProperty(typeof(ITagger<IOutliningRegionTag>)) as OutliningTagger;
            string targetString = tagger.TargetText;

            if(targetString != "")
            {
                foreach (var line in newSnapshot.Lines)
                {
                    int wordOffset = 0;
                    string text = line.GetText();

                    while ((wordOffset = text.IndexOf(targetString, wordOffset, StringComparison.Ordinal)) != -1)
                    {
                        var startPoint = snapshot.GetLineFromLineNumber(line.LineNumber);

                        classificationSpans.Add(new ClassificationSpan(new SnapshotSpan(startPoint.Start + wordOffset, startPoint.Start + wordOffset + targetString.Length), this.classificationType));

                        wordOffset += targetString.Length;
                    }
                }
            }
        }
    }

    #endregion

}
