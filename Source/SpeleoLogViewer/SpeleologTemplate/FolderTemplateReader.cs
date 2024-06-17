﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace SpeleoLogViewer.SpeleologTemplate;

public class FolderTemplateReader
{
    public IReadOnlyList<TemplateInfos> Read(string path) =>
        Directory
            .EnumerateFiles(path, $"*{SpeleologTemplateReader.Extension}")
            .Select(filePath => new TemplateInfos(filePath))
            .ToImmutableArray();
}