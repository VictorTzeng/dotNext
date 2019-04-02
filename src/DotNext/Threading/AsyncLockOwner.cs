﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace DotNext.Threading
{
    using Generic;
    using Tasks;

    /// <summary>
    /// Represents asynchronous lock.
    /// </summary>
    internal sealed class AsyncLockOwner : Disposable
    {
        private sealed class LockNode : TaskCompletionSource<bool>
        {
            private LockNode previous;
            private LockNode next;

            internal LockNode() => previous = next = null;

            internal LockNode(LockNode previous)
            {
                previous.next = this;
                this.previous = previous;
            }

            internal void DetachNode()
            {
                if (!(previous is null))
                    previous.next = next;
                if (!(next is null))
                    next.previous = previous;
                next = previous = null;
            }

            internal LockNode CleanupAndGotoNext()
            {
                var next = this.next;
                this.next = this.previous = null;
                return next;
            }

            internal LockNode Previous => previous;

            internal LockNode Next => next;

            internal bool IsRoot => previous is null && next is null;

            internal void Complete() => SetResult(true);
        }

        private LockNode head, tail;

        private LockNode NewLockNode() => head is null ? head = tail = new LockNode() : tail = new LockNode(tail);

        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool RemoveNode(LockNode node)
        {
            var inList = ReferenceEquals(head, node) || !node.IsRoot;
            if (ReferenceEquals(head, node))
                head = node.Next;
            if (ReferenceEquals(tail, node))
                tail = node.Previous;
            node.DetachNode();
            return inList;
        }

        private async Task<bool> TryAcquire(LockNode node, CancellationToken token, TimeSpan timeout)
        {
            using (var tokenSource = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token, default) : new CancellationTokenSource())
            {
                if (ReferenceEquals(node.Task, await Task.WhenAny(node.Task, Task.Delay(timeout, tokenSource.Token)).ConfigureAwait(false)))
                {
                    tokenSource.Cancel();   //ensure that Delay task is cancelled
                    return true;
                }
            }
            if (RemoveNode(node))
            {
                token.ThrowIfCancellationRequested();
                return false;
            }
            else
                return await node.Task.ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal Task<bool> TryAcquire(CancellationToken token, TimeSpan timeout)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<bool>(token);
            else if (head is null)   //the lock was not obtained
            {
                head = tail = new LockNode();
                return CompletedTask<bool, BooleanConst.True>.Task;
            }
            else if (timeout == TimeSpan.Zero)   //if timeout is zero fail fast
                return CompletedTask<bool, BooleanConst.False>.Task;
            else
            {
                tail = new LockNode(tail);
                return timeout < TimeSpan.MaxValue || token.CanBeCanceled ? TryAcquire(tail, token, timeout) : tail.Task;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Release()
        {
            var waiterTask = head;
            if (!(waiterTask is null))
            {
                RemoveNode(waiterTask);
                waiterTask.Complete();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (var current = head; !(current is null); current = current.CleanupAndGotoNext())
                    current.TrySetCanceled();
            }
        }
    }
}
