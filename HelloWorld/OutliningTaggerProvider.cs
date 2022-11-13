using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.Text;

[Export(typeof(ITaggerProvider))]
[TagType(typeof(IOutliningRegionTag))]
[ContentType("text")]
internal sealed class OutliningTaggerProvider : ITaggerProvider
{
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        //create a single tagger for each buffer.
        Func<ITagger<T>> sc = delegate () { return new OutliningTagger(buffer) as ITagger<T>; };

        ITagger < T > tagger = buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
        return tagger;
    }
}
