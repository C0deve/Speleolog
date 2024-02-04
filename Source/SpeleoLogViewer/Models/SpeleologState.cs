using System.Collections.Generic;

namespace SpeleoLogViewer.Models;

public record SpeleologState(List<string> LastOpenFiles, bool AppendFromBottom = true);