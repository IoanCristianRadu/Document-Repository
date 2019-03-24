using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DocumentRepositoryOnline.Startup))]

namespace DocumentRepositoryOnline
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}