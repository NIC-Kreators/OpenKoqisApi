namespace OpenKoqis.Application.GenericRepository;

// TODO: is it an interface?
// That should be used as record with IOptions pattern anyway
public class IMongoSettings
{
    public required string DatabaseName { get; set; }
    public required string ConnectionString { get; set; }
}
