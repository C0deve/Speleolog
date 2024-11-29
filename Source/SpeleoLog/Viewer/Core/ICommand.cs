﻿namespace SpeleoLog.Viewer.Core;

public interface ICommand;
public record Refresh(params string[] Text) : ICommand;
public record Filter(string Text) : ICommand;
public record Mask(string Text) : ICommand;
public record Next : ICommand;
public record Previous : ICommand;
public record GoToTop : ICommand;
public record SetErrorTag(string ErrorTag) : ICommand;
public record Highlight(string HighlightText) : ICommand;