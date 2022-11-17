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
            ReParse();

            return classificationSpans;
        }

        ITextBuffer buffer;
        List<ClassificationSpan> classificationSpans = new List<ClassificationSpan>();

        public void ReParse()
        {
            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            List<ClassificationSpan> newClassificationSpans = new List<ClassificationSpan>();



            object taggerProperty = null;

            if(buffer.Properties.TryGetProperty(typeof(ITagger<IOutliningRegionTag>), out taggerProperty) && taggerProperty != null)
            {
                var tagger = taggerProperty as LSOutliningTagger;

                string targetString = tagger.TargetText;


                int changeStart = int.MaxValue;
                int changeEnd = -1;

                if (targetString != "")
                {
                    foreach (var line in newSnapshot.Lines)
                    {
                        int wordOffset = 0;
                        string text = line.GetText();

                        while ((wordOffset = text.IndexOf(targetString, wordOffset, StringComparison.Ordinal)) != -1)
                        {
                            var startPoint = newSnapshot.GetLineFromLineNumber(line.LineNumber);

                            newClassificationSpans.Add(new ClassificationSpan(new SnapshotSpan(startPoint.Start + wordOffset, startPoint.Start + wordOffset + targetString.Length), this.classificationType));

                            wordOffset += targetString.Length;
                        }
                    }

                    //determine the changed span, and send a changed event with the new spans
                    List<Span> oldSpans =
                        new List<Span>(this.classificationSpans.Select(c => c.Span
                            .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
                            .Span));
                    List<Span> newSpans =
                            new List<Span>(newClassificationSpans.Select(c => c.Span.Span));

                    NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
                    NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

                    //the changed regions are regions that appear in one set or the other, but not both.
                    NormalizedSpanCollection removed =
                    NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

                    if (removed.Count > 0)
                    {
                        changeStart = removed[0].Start;
                        changeEnd = removed[removed.Count - 1].End;
                    }

                    if (newSpans.Count > 0)
                    {
                        changeStart = Math.Min(changeStart, newSpans[0].Start);
                        changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
                    }
                }
                else
                {
                    changeStart = newSnapshot.CreateTrackingPoint(0, PointTrackingMode.Positive).GetPosition(newSnapshot);
                    changeEnd = newSnapshot.CreateTrackingPoint(newSnapshot.Length, PointTrackingMode.Positive).GetPosition(newSnapshot);
                }

                this.classificationSpans = newClassificationSpans;

                if (changeStart <= changeEnd)
                {
                    //ITextSnapshot snap = this.snapshot;
                    if (this.ClassificationChanged != null)
                    {
                        this.ClassificationChanged(this, new ClassificationChangedEventArgs(
                            new SnapshotSpan(newSnapshot, Span.FromBounds(changeStart, changeEnd))));
                    }
                }
            }
        }
    }

    #endregion

}
