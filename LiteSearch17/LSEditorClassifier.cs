using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace LiteSearch
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "LSEditorClassifier" classification type.
    /// </summary>
    internal class LSEditorClassifier : IClassifier
    {
        /// <summary>
        /// Classification type.
        /// </summary>
        private readonly IClassificationType classificationType;

        /// <summary>
        /// Initializes a new instance of the <see cref="LSEditorClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal LSEditorClassifier(IClassificationTypeRegistryService registry, ITextBuffer inBuffer)
        {
            this.classificationType = registry.GetClassificationType("LSEditorClassifier");

            this.buffer = inBuffer ?? throw new ArgumentNullException(nameof(inBuffer));
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
            return ReParse(span);
        }

        ITextBuffer buffer;
        List<ClassificationSpan> classificationSpans = new List<ClassificationSpan>();

        public List<ClassificationSpan> ReParse(SnapshotSpan span)
        {
            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            List<ClassificationSpan> newClassificationSpans = new List<ClassificationSpan>();

            object taggerProperty = null;

            if(buffer.Properties.TryGetProperty(typeof(ITagger<IOutliningRegionTag>), out taggerProperty) && taggerProperty != null)
            {
                var tagger = taggerProperty as LSOutliningTagger;

                string targetString = tagger.TargetText;

                if (targetString != "")
                {
                    string textToSearch = span.GetText();

                    int wordOffset = 0;

                    while ((wordOffset = textToSearch.IndexOf(targetString, wordOffset, LiteSearch.OptionsAccessor.Instance.CaseSensitive? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) != -1)
                    {
                        newClassificationSpans.Add(new ClassificationSpan(new SnapshotSpan(span.Start.Add(wordOffset), span.Start.Add(wordOffset + targetString.Length)), this.classificationType));

                        wordOffset += targetString.Length;
                    }
                }
            }

            return newClassificationSpans;
        }
    }

    #endregion

}
