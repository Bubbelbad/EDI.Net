using indice.Edi.FormatSpec;

namespace indice.Edi.Serialization;

/// <summary>
/// Use <see cref="EdiValueAttribute"/> for any value inside a segment. 
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public sealed class EdiValueAttribute : EdiAttribute
{
    private IFormatSpec _formatSpec; 
    private PictureSpec _pictureSpec;
    private bool _Mandatory;
    private string _Description;
    private string _Format;
    private string _Path;
    
    /// <summary>
    /// Indicates that the current structure (Segment or Element) is mandatory or optional. By default this is false.
    /// </summary>
    public bool Mandatory {
        get { return _Mandatory; }
        set { _Mandatory = value; }
    }

    /// <summary>
    /// Helps by annotating the current member with a <see cref="Description"/>. Only for reference and documentation.
    /// </summary>
    public string Description {
        get { return _Description; }
        set { _Description = value; }
    }

    /// <summary>
    /// Extra dotnet style format string. Currently only used for date formatting. (ie yyyyMMdd)
    /// </summary>
    public string Format {
        get { return _Format; }
        set { _Format = value; }
    }

    /// <summary>
    /// The path for the component value. A string representation of an <see cref="EdiPath"/> pointing to a component value ie: "XYZ/0/0"
    /// </summary>
    public string Path {
        get { return _Path; }
        set { _Path = value; }
    }

    /// <summary>
    /// The value spec regarding value size and format.
    /// </summary>
    public IFormatSpec FormatSpec {
        get { return _formatSpec; }
    } 
    /// <summary>
    /// /// The value spec regarding value size and format.
    /// /// </summary>
    public PictureSpec PictureSpec {
        get { return _pictureSpec; }
    }

    /// <summary>
    /// Constructs the attribute with defaults
    /// </summary>
    public EdiValueAttribute() {
        _formatSpec = default(PictureSpec);
    }

    public EdiValueAttribute(string picture) { 
           _formatSpec = default(PictureSpec);
    }

    /// <summary>
    /// Constructs the attribute given the <see cref="IFormatSpec"/>
    /// </summary>
    /// <param name="formatSpec"></param>
    public EdiValueAttribute(string spec, FormatterType formatterType) {
        _formatSpec = FormatSpecFactory.Create(formatterType, spec);
    }
}
