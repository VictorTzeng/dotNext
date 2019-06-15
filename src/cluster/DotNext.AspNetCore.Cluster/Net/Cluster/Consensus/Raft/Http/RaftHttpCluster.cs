﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IServer = Microsoft.AspNetCore.Hosting.Server.IServer;

namespace DotNext.Net.Cluster.Consensus.Raft.Http
{
    using Generic;
    using Messaging;
    using Threading.Tasks;

    internal class RaftHttpCluster : RaftCluster<RaftClusterMember>, IHostedService, IHostingContext, IExpandableCluster
    {
        private static readonly Func<Task, bool> TrueTaskContinuation = task => true;
        private delegate ICollection<IPEndPoint> HostingAddressesProvider();

        private readonly IRaftClusterConfigurator configurator;
        private readonly IMessageHandler messageHandler;

        private readonly IDisposable configurationTracker;
        private volatile MemberMetadata metadata;
        private volatile ISet<IPNetwork> allowedNetworks;
        private readonly Uri consensusPath;

        [SuppressMessage("Usage", "CA2213", Justification = "This object is disposed via RaftCluster.members collection")]
        private RaftClusterMember localMember;
        private readonly HostingAddressesProvider hostingAddresses;

        [SuppressMessage("Reliability", "CA2000", Justification = "The member will be disposed in RaftCluster.Dispose method")]
        private RaftHttpCluster(RaftClusterMemberConfiguration config)
            : base(config, out var members)
        {
            consensusPath = config.ResourcePath;
            allowedNetworks = config.ParseAllowedNetworks();
            metadata = new MemberMetadata(config.Metadata);
            foreach (var memberUri in config.Members)
                members.Add(CreateMember(memberUri));
        }

        private RaftHttpCluster(IOptionsMonitor<RaftClusterMemberConfiguration> config, IServiceProvider dependencies)
            : this(config.CurrentValue)
        {
            configurator = dependencies.GetService<IRaftClusterConfigurator>();
            messageHandler = dependencies.GetService<IMessageHandler>();
            AuditTrail = dependencies.GetService<IPersistentState>();
            hostingAddresses = dependencies.GetRequiredService<IServer>().GetHostingAddresses;
            Logger = dependencies.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
            //track changes in configuration
            configurationTracker = config.OnChange(ConfigurationChanged);
        }

        public RaftHttpCluster(IServiceProvider dependencies)
            : this(dependencies.GetRequiredService<IOptionsMonitor<RaftClusterMemberConfiguration>>(), dependencies)
        {
        }

        private RaftClusterMember CreateMember(Uri address) => new RaftClusterMember(this, address, consensusPath);

        protected override ILogger Logger { get; }

        ILogger IHostingContext.Logger => Logger;

        private void ConfigurationChanged(RaftClusterMemberConfiguration configuration, string name)
        {
            metadata = new MemberMetadata(configuration.Metadata);
            allowedNetworks = configuration.ParseAllowedNetworks();
            ChangeMembers((in MemberCollection members) =>
            {
                var existingMembers = new HashSet<Uri>();
                //remove members
                foreach (var holder in members)
                    if (configuration.Members.Contains(holder.Member.BaseAddress))
                        existingMembers.Add(holder.Member.BaseAddress);
                    else
                    {
                        var member = holder.Remove();
                        MemberRemoved?.Invoke(this, member);
                        member.CancelPendingRequests();
                    }

                //add new members
                foreach (var memberUri in configuration.Members)
                    if (!existingMembers.Contains(memberUri))
                    {
                        var member = CreateMember(memberUri);
                        members.Add(member);
                        MemberAdded?.Invoke(this, member);
                    }
                existingMembers.Clear();
            });
        }

        IReadOnlyDictionary<string, string> IHostingContext.Metadata => metadata;

        bool IHostingContext.IsLeader(IRaftClusterMember member) => ReferenceEquals(Leader, member);

        Task<bool> IHostingContext.LocalCommitAsync(Replication.ILogEntry<LogEntryId> entry)
            => AuditTrail is null ? CompletedTask<bool, BooleanConst.False>.Task : AuditTrail.CommitAsync(entry);

        IPEndPoint IHostingContext.LocalEndpoint => localMember?.Endpoint;

        public event ClusterChangedEventHandler MemberAdded;
        public event ClusterChangedEventHandler MemberRemoved;
        public override event ClusterMemberStatusChanged MemberStatusChanged;

        void IHostingContext.MemberStatusChanged(IRaftClusterMember member, ClusterMemberStatus previousStatus, ClusterMemberStatus newStatus)
            => MemberStatusChanged?.Invoke(member, previousStatus, newStatus);

        public override Task StartAsync(CancellationToken token)
        {
            //detect local member
            localMember = Members.FirstOrDefault(hostingAddresses().Contains);
            if (localMember is null)
                throw new RaftProtocolException(ExceptionMessages.UnresolvedLocalMember);
            configurator?.Initialize(this, metadata);
            return base.StartAsync(token);
        }

        public override Task StopAsync(CancellationToken token)
        {
            configurator?.Shutdown(this);
            return base.StopAsync(token);
        }

        private async Task ReceiveVote(RequestVoteMessage request, HttpResponse response)
            => await RequestVoteMessage.CreateResponse(response,
                await ReceiveVote(request.Sender, request.ConsensusTerm, request.LastEntry, ClusterMember.Represents)
                    .ConfigureAwait(false)).ConfigureAwait(false);

        private Task ReceiveHeartbeat(HeartbeatMessage request, HttpResponse response)
        {
            HeartbeatMessage.CreateResponse(response);
            return ReceiveHeartbeat(request.Sender, request.ConsensusTerm, ClusterMember.Represents);
        }

        private async Task Resign(HttpResponse response) =>
            await ResignMessage.CreateResponse(response, await ReceiveResign().ConfigureAwait(false)).ConfigureAwait(false);

        private Task GetMetadata(HttpResponse response) => MetadataMessage.CreateResponse(response, metadata);

        private async Task ReceiveEntries(AppendEntriesMessage request, HttpResponse response)
            => await AppendEntriesMessage.CreateResponse(response,
                await ReceiveEntries(request.Sender, request.ConsensusTerm, ClusterMember.Represents, request.LogEntry,
                    request.PrecedingEntry).ConfigureAwait(false)).ConfigureAwait(false);

        private async Task ReceiveMessage(CustomMessage message, HttpResponse response)
        {
            if (messageHandler is null)
            {
                response.StatusCode = (int)HttpStatusCode.NotImplemented;
            }
            else if (message.IsOneWay)
            {
                messageHandler.ReceiveSignal(FindMember(message.Sender, ClusterMember.Represents), message.Message);
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                var reply = await messageHandler
                    .ReceiveMessage(FindMember(message.Sender, ClusterMember.Represents), message.Message)
                    .ConfigureAwait(false);
                await CustomMessage.CreateResponse(response, reply).ConfigureAwait(false);
            }
        }

        internal Task<bool> ProcessRequest(HttpContext context)
        {
            var networks = allowedNetworks;
            if (!string.Equals(consensusPath.GetComponents(UriComponents.Path, UriFormat.UriEscaped),
                context.Request.PathBase.Value, StringComparison.Ordinal))
                return CompletedTask<bool, BooleanConst.False>.Task;
            //checks whether the client's address is allowed
            if (networks.Count > 0 || networks.FirstOrDefault(context.Connection.RemoteIpAddress.IsIn) is null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return CompletedTask<bool, BooleanConst.True>.Task;
            }

            Task task;
            //process request
            switch (RaftHttpMessage.GetMessageType(context.Request))
            {
                case RequestVoteMessage.MessageType:
                    task = ReceiveVote(new RequestVoteMessage(context.Request), context.Response);
                    break;
                case HeartbeatMessage.MessageType:
                    task = ReceiveHeartbeat(new HeartbeatMessage(context.Request), context.Response);
                    break;
                case ResignMessage.MessageType:
                    task = Resign(context.Response);
                    break;
                case MetadataMessage.MessageType:
                    task = GetMetadata(context.Response);
                    break;
                case AppendEntriesMessage.MessageType:
                    task = ReceiveEntries(new AppendEntriesMessage(context.Request), context.Response);
                    break;
                case CustomMessage.MessageType:
                    task = ReceiveMessage(new CustomMessage(context.Request), context.Response);
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return CompletedTask<bool, BooleanConst.True>.Task;
            }

            return task.ContinueWith(TrueTaskContinuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously |
                                           TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                localMember = null;
                configurationTracker.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
