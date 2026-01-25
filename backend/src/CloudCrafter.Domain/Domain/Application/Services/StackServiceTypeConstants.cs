namespace CloudCrafter.Domain.Domain.Application.Services;

public class StackServiceTypeConstants
{
    public static readonly Guid App = new("6257aa7c-09f0-42c0-8417-3d0ca0ead213");
    public static readonly Guid DatabasePostgres = new("81fc7fa9-a1ef-4ddc-b181-6f019ff0568b");
}

public enum StackServiceTypes
{
    App = 1,
    DatabasePostgres = 2,
}
