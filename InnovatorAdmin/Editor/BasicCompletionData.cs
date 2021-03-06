﻿using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;

namespace Aras.Tools.InnovatorAdmin.Editor
{
  public class BasicCompletionData : ICompletionData
  {
    private string _description = null;

    public BasicCompletionData(string text)
    {
      this.Text = text;
    }
    public BasicCompletionData(string text, string description)
    {
      this.Text = text;
      _description = description;
    }

    public System.Windows.Media.ImageSource Image
    {
      get { return null; }
    }

    public string Text { get; private set; }

    // Use this property if you want to show a fancy UIElement in the list.
    public object Content
    {
      get { return this.Text; }
    }

    public object Description
    {
      get { return _description; }
    }

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
      textArea.Document.Replace(completionSegment, this.Text);
    }

    public double Priority
    {
      get { return 0; }
    }
  }
}
