﻿namespace SpeleoLogViewer.LogFileViewer.V2;

public interface ICommand;
public record Refresh(params string[] Text) : ICommand;
public record Filter(string Text) : ICommand;
public record Mask(string Text) : ICommand;
public record Next : ICommand;
public record Previous : ICommand;
public record SetErrorTag(string ErrorTag) : ICommand;
