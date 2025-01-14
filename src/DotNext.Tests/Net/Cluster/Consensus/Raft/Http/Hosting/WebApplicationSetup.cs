﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNext.Net.Cluster.Consensus.Raft.Http.Hosting
{
    internal sealed class WebApplicationSetup : StartupBase
    {
        private readonly IConfiguration configuration;

        public WebApplicationSetup(IConfiguration configuration) => this.configuration = configuration;

        public override void Configure(IApplicationBuilder app)
        {

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.BecomeClusterMember(configuration);
        }
    }
}
