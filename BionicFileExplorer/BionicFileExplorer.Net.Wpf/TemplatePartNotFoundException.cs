using System;

namespace BionicFileExplorer.Net.Wpf
{
  [Serializable]
  public class TemplatePartNotFoundException : Exception
  {
    public TemplatePartNotFoundException() { }
    public TemplatePartNotFoundException(string message) : base(message) { }
    public TemplatePartNotFoundException(string message, Exception inner) : base(message, inner) { }
    protected TemplatePartNotFoundException(
    System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
  }
}
