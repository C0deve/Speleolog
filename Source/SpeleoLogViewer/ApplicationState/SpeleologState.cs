using System.Collections.Generic;

namespace SpeleoLogViewer.ApplicationState;

public record SpeleologState(List<string> LastOpenFiles, bool AppendFromBottom = true);