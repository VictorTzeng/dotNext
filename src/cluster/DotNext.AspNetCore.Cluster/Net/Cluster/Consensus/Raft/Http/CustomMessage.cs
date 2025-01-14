﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;

namespace DotNext.Net.Cluster.Consensus.Raft.Http
{
    using Messaging;
    using NullMessage = Threading.Tasks.CompletedTask<Messaging.IMessage, Generic.DefaultConst<Messaging.IMessage>>;

    internal class CustomMessage : HttpMessage, IHttpMessageWriter<IMessage>, IHttpMessageReader<IMessage>
    {
        //request - represents custom message name
        private const string MessageNameHeader = "X-Raft-Message-Name";
        private static readonly ValueParser<DeliveryMode> DeliveryModeParser = Enum.TryParse;

        internal enum DeliveryMode
        {
            OneWayNoAck,
            OneWay,
            RequestReply
        }

        private sealed class OutboundMessageContent : OutboundTransferObject
        {
            internal OutboundMessageContent(IMessage message)
                : base(message)
            {
                Headers.ContentType = MediaTypeHeaderValue.Parse(message.Type.ToString());
                Headers.Add(MessageNameHeader, message.Name);
            }
        }

        private sealed class InboundMessageContent : StreamMessage
        {
            internal InboundMessageContent(Stream content, bool leaveOpen, string name, ContentType type)
                : base(content, leaveOpen, name, type)
            {
            }

            internal InboundMessageContent(HttpRequest request)
                : this(request.Body, true, ParseHeader<StringValues>(MessageNameHeader, request.Headers.TryGetValue),
                    new ContentType(request.ContentType))
            {
            }
        }

        internal new const string MessageType = "CustomMessage";
        private const string DeliveryModeHeader = "X-Delivery-Type";

        private const string RespectLeadershipHeader = "X-Respect-Leadership";

        internal readonly DeliveryMode Mode;
        internal readonly IMessage Message;
        internal bool RespectLeadership;

        private protected CustomMessage(IPEndPoint sender, IMessage message, DeliveryMode mode)
            : base(MessageType, sender)
        {
            Message = message;
            Mode = mode;
        }

        internal CustomMessage(IPEndPoint sender, IMessage message, bool requiresConfirmation)
            : this(sender, message, requiresConfirmation ? DeliveryMode.OneWay : DeliveryMode.OneWayNoAck)
        {

        }

        private CustomMessage(HeadersReader<StringValues> headers)
            : base(headers)
        {
            Mode = ParseHeader(DeliveryModeHeader, headers, DeliveryModeParser);
            RespectLeadership = ParseHeader(RespectLeadershipHeader, headers, BooleanParser);
        }

        internal CustomMessage(HttpRequest request)
            : this(request.Headers.TryGetValue)
        {
            Message = new InboundMessageContent(request);
        }

        internal sealed override void PrepareRequest(HttpRequestMessage request)
        {
            request.Headers.Add(DeliveryModeHeader, Mode.ToString());
            request.Headers.Add(RespectLeadershipHeader, RespectLeadership.ToString(InvariantCulture));
            request.Content = new OutboundMessageContent(Message);
            base.PrepareRequest(request);
        }

        public Task SaveResponse(HttpResponse response, IMessage message, CancellationToken token)
        {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = message.Type.ToString();
            response.ContentLength = message.Length;
            response.Headers.Add(MessageNameHeader, message.Name);
            return message.CopyToAsync(response.Body, token);
        }

        //do not parse response because this is one-way message
        Task<IMessage> IHttpMessageReader<IMessage>.ParseResponse(HttpResponseMessage response, CancellationToken token) => NullMessage.Task;

        private protected static async Task<T> ParseResponse<T>(HttpResponseMessage response, MessageReader<T> reader, CancellationToken token)
        {
            var contentType = new ContentType(response.Content.Headers.ContentType.ToString());
            var name = ParseHeader<IEnumerable<string>>(MessageNameHeader, response.Headers.TryGetValues);
            using (var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var message = new InboundMessageContent(content, true, name, contentType))
                return await reader(message, token).ConfigureAwait(false);
        }
    }

    internal sealed class CustomMessage<T> : CustomMessage, IHttpMessageReader<T>
    {
        private readonly MessageReader<T> reader;

        internal CustomMessage(IPEndPoint sender, IMessage message, MessageReader<T> reader) : base(sender, message, DeliveryMode.RequestReply) => this.reader = reader;

        Task<T> IHttpMessageReader<T>.ParseResponse(HttpResponseMessage response, CancellationToken token)
            => ParseResponse<T>(response, reader, token);
    }
}
