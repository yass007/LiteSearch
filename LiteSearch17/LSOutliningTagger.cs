using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

internal sealed class LSOutliningTagger : ITagger<IOutliningRegionTag>
{
    string ellipsis = "...";    //the characters that are displayed when the region is collapsed
    string hoverText = "hover text"; //the contents of the tooltip for the collapsed span
    ITextBuffer buffer;
    ITextSnapshot snapshot;
    List<Region> regions;
    string targetText = "";

    public string TargetText
    {
        get { return targetText; }
    }

    public LSOutliningTagger(ITextBuffer buffer)
    {
        this.buffer = buffer;
        this.snapshot = buffer.CurrentSnapshot;
        this.regions = new List<Region>();
        //this.ReParse();
        //this.buffer.Changed += BufferChanged;
    }

    public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        if (spans.Count == 0)
            yield break;
        List<Region> currentRegions = this.regions;
        ITextSnapshot currentSnapshot = this.snapshot;
        SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
        int startLineNumber = entire.Start.GetContainingLine().LineNumber;
        int endLineNumber = entire.End.GetContainingLine().LineNumber;
        foreach (var region in currentRegions)
        {
            if (region.StartLine <= endLineNumber &&
                region.EndLine >= startLineNumber)
            {
                var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

                //the region starts at the beginning of the "[", and goes until the *end* of the line that contains the "]".
                yield return new TagSpan<IOutliningRegionTag>(
                    new SnapshotSpan(startLine.Start + region.StartOffset,
                    endLine.End),
                    new OutliningRegionTag(true, false, ellipsis, hoverText));
            }
        }
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    //void BufferChanged(object sender, TextContentChangedEventArgs e)
    //{
    //    // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
    //    if (e.After != buffer.CurrentSnapshot)
    //        return;

    //}

    public bool IsValidRegion(ICollapsible collapsibleRegion)
    {
        ITextSnapshot currentSnapshot = this.snapshot;
        List<Region> currentRegions = this.regions;

        SnapshotSpan incomingSpan = collapsibleRegion.Extent.GetSpan(currentSnapshot);


        foreach (var region in currentRegions)
        {
            var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

            SnapshotSpan currentSpan = new SnapshotSpan(startLine.Start + region.StartOffset,
                    endLine.End);

            if (incomingSpan == currentSpan)
            {
                return true;
            }
        }

        return false;
    }
    public void GenerateTags(string targetString)
    {
        ReParse(targetString);
    }

    public bool AreTagsActive()
    {
        return regions.Count > 0;
    }

    public void ReParse(string textToLookFor)
    {
        ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
        List<Region> newRegions = new List<Region>();


        int AdditionalOffset = 2;

        int StartLine = 0;
        int EndLine = 0;

        if (textToLookFor != "")
        {
            foreach (var line in newSnapshot.Lines)
            {
                string text = line.GetText();

                if (text.IndexOf(textToLookFor, StringComparison.Ordinal) != -1)
                {
                    EndLine = line.LineNumber - (1 + AdditionalOffset);

                    if (EndLine > StartLine)
                    {
                        newRegions.Add(new Region()
                        {
                            Level = 1,
                            StartLine = StartLine,
                            StartOffset = 0,
                            EndLine = EndLine
                        });
                    }

                    StartLine = line.LineNumber + (1 + AdditionalOffset);
                }
            }

            EndLine = (newSnapshot.LineCount - 1);

            if (EndLine > StartLine )
            {
                newRegions.Add(new Region()
                {
                    Level = 1,
                    StartLine = StartLine,
                    StartOffset = 0,
                    EndLine = EndLine
                });
            }
        }

        //determine the changed span, and send a changed event with the new spans
        List<Span> oldSpans =
            new List<Span>(this.regions.Select(r => AsSnapshotSpan(r, this.snapshot)
                .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
                .Span));
        List<Span> newSpans =
                new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

        NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
        NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

        //the changed regions are regions that appear in one set or the other, but not both.
        NormalizedSpanCollection removed =
        NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

        int changeStart = int.MaxValue;
        int changeEnd = -1;

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

        this.snapshot = newSnapshot;
        this.regions = newRegions;

        if (changeStart <= changeEnd)
        {
            ITextSnapshot snap = this.snapshot;
            if (this.TagsChanged != null)
            {
                this.TagsChanged(this, new SnapshotSpanEventArgs(
                    new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
            }
        }

        targetText = textToLookFor;
    }

    static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot)
    {
        var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
        var endLine = (region.StartLine == region.EndLine) ? startLine
             : snapshot.GetLineFromLineNumber(region.EndLine);
        return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End);
    }

    class PartialRegion
    {
        public int StartLine { get; set; }
        public int StartOffset { get; set; }
        public int Level { get; set; }
        public PartialRegion PartialParent { get; set; }
    }

    class Region : PartialRegion
    {
        public int EndLine { get; set; }
    }
}

