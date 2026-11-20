using _Project.Scripts.Services.Config;
using _Project.Scripts.Services.Save;

namespace _Project.Scripts.Services
{
    public static class ServiceProvider
    {
        public static ISaveService Save { get; internal set; }
        public static IConfigService Config { get; internal set; }
    }
}