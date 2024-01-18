using System.Data;

namespace Microsoft.Protocols.Tds.Features;

public interface IQueryFeature
{
    string QueryString { get; set; }

    IDataReader Result { get; set; }
}
