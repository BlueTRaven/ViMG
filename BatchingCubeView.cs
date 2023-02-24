using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViMG
{
    //A cube view intended for anything that requires multithreading.
    //This should be the most commonly used view after initialization, as using other views in conjunction with this one can result in race conditions.
    public class BatchingCubeView
    {
        /*public delegate void RequestFinishedCallbackDel(object arg);

        private enum RequestState
        {
            Invalid,
            Requested,
            Finished,
            Consumed
        }
        private enum RequestType
        {
            Set,
            GetId,
            GetSides,
        }

        private struct Request
        {
            public double occurredAt;
            public CubePosition position;
            public ushort id;

            public RequestType type;
            public RequestFinishedCallbackDel callback;
            public object arg;

            public RequestState state;
            public readonly bool valid;


            public Request(CubePosition position, double currentTime, RequestType type, RequestFinishedCallbackDel callback, object arg)
            {
                this.position = position;
                id = 0;
                occurredAt = currentTime;

                this.type = type;
                this.callback = callback;
                this.arg = arg;

                state = RequestState.Requested;
                valid = true;
            }

            public Request(CubePosition position, ushort setTo, double currentTime, RequestFinishedCallbackDel callback, object arg)
            {
                this.position = position;
                this.id = setTo;
                this.occurredAt = currentTime;

                this.type = RequestType.Set;
                this.callback = callback;
                this.arg = arg;

                state = RequestState.Requested;
                valid = true;
            }
        }

        private double currentTime;
        private double processedTime;
        private readonly ChunkManager2 manager;

        private const int MAX_REQUESTS = ushort.MaxValue;
        private int startingGet;    //the beginning of the circular array; moved up by the number of processed elements each frame.
        private int currentGet;
        private int numGetRequests;
        private Request[] getRequests = new Request[MAX_REQUESTS];
        private int startingSet;
        private int currentSet;
        private int numSetRequests;
        private Request[] setRequests = new Request[MAX_REQUESTS];

        public BatchingCubeView(ChunkManager2 manager)
        {
            this.manager = manager;
        }

        //returns a handle that allows us to check whether or not the request has finished.
        //this handle is in 2 parts: the high short being a version and the low short being the actual handle.
        //TODO: implement versioning
        //Without versioning then we might end up with a stale get reference (i.e. the get request was overwritten before it could be read).
        //With versioning, we might not be able to prevent this, but we can at least detect it.
        public int RequestGetCubeId(CubePosition position, RequestFinishedCallbackDel callback = null, object arg = null)
        {
            currentGet = (currentGet + 1) % MAX_REQUESTS;
            lock (getRequests)
                getRequests[currentGet] = new Request(position, currentTime, RequestType.GetId, callback, arg);
            numGetRequests++;
            return currentGet;
        }

        public void RequestGetCubeIds(CubePosition[] positions, ref int[] handles, RequestFinishedCallbackDel callback = null, object arg = null)
        {
            if (positions.Length != handles.Length)
                throw new Exception("Length of positions must match length of handles. (Each position will return one handle!)");

            lock (getRequests)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    currentGet = (currentGet + 1) % MAX_REQUESTS;
                    getRequests[currentGet] = new Request(positions[i], currentTime, RequestType.GetId, callback, arg);
                    numGetRequests++;

                    handles[i] = currentGet;
                }
            }
        }

        public int RequestGetCubeSides(CubePosition position, RequestFinishedCallbackDel callback = null, object arg = null)
        {
            currentGet = (currentGet + 1) % MAX_REQUESTS;
            lock (getRequests)
                getRequests[currentGet] = new Request(position, currentTime, RequestType.GetSides, callback, arg);
            numGetRequests++;
            return currentGet;
        }

        public bool TryConsumeGetIdRequest(int handle, out ushort id)
        {
            lock (getRequests)
            {
                ref var req = ref getRequests[handle];
                if (req.state == RequestState.Finished)
                {
                    id = req.id;
                    req.state = RequestState.Consumed;
                    return true;
                }
            }

            id = 0;
            return false;
        }

        public bool TryConsumeGetIdRequests(int[] handles, ref ushort[] ids)
        {
            if (handles.Length != ids.Length)
                throw new Exception("Length of handles must match length of ids. (Each handle will return one id!)");

            lock (getRequests)
            {
                //Need to check to make sure they're all finished first before returning
                //We don't want to return partially finished get requests, as then some will be consumed and some won't be,
                //which will mess things up when we try to get the rest of the handles.
                bool allFinished = true;
                for (int i = 0; i < handles.Length; i++)
                {
                    if (getRequests[handles[i]].state != RequestState.Finished)
                        allFinished = false;
                }

                if (allFinished)
                {
                    for (int i = 0; i < handles.Length; i++)
                    {
                        ref var req = ref getRequests[handles[i]];

                        ids[i] = req.id;
                        req.state = RequestState.Consumed;
                    }
                }
                else return false;
            }

            return handles.Length > 0;
        }

        public int RequestSetCube(CubePosition position, ushort id, RequestFinishedCallbackDel callback = null, object arg = null)
        {
            currentSet = currentSet + 1 % MAX_REQUESTS;
            lock (setRequests)
                setRequests[currentSet] = new Request(position, id, currentTime, callback, arg);
            numSetRequests++;
            return currentSet;
        }

        public void RequestSetCubes(CubePosition[] positions, ushort[] toIds, int[] handles = null)
        {
            if (handles == null)
                RequestSetCubes(positions.AsSpan(), toIds.AsSpan(), Span<int>.Empty);
            else RequestSetCubes(positions.AsSpan(), toIds.AsSpan(), handles.AsSpan());
        }

        public void RequestSetCubes(Span<CubePosition> positions, Span<ushort> toIds, Span<int> handles, RequestFinishedCallbackDel callback = null, object arg = null)
        {
            if (handles.Length > 0 && positions.Length != handles.Length && positions.Length != toIds.Length)
                throw new Exception("Length of positions must match length of handles. (Each position will return one handle!)");

            lock (setRequests)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    currentSet = currentSet + 1 % MAX_REQUESTS;
                    setRequests[currentSet] = new Request(positions[i], toIds[i], currentTime, callback, arg);
                    numSetRequests++;

                    if (handles.Length > 0)
                        handles[i] = currentSet;
                }
            }
        }

        //If msTimeout is -1, blocks until the request is finished. Otherwise, if elapsed time > msTimeout, returns false.
        public bool WaitSetRequestFinished(int handle, int msCheckInterval = 100, int msTimeout = -1)
        {
            bool block = msTimeout == -1;

            Stopwatch watch = Stopwatch.StartNew();
            while (block || watch.ElapsedMilliseconds < msTimeout)
            {
                //check interval exists so that we're only locking this every so often.
                lock (setRequests)
                {
                    if (setRequests[handle].state == RequestState.Requested)
                    {
                        //Wait msCheckInterval milliseconds. If not blocking, then wait whichever is smaller, the amount of remaining time in the timeout
                        //(so we get one more check before returning) or the check interval.
                        int wait = 0;

                        if (!block)
                        {
                            int msRemaining = msTimeout = (int)watch.ElapsedMilliseconds;

                            wait = Math.Min(msCheckInterval, msRemaining - 1);
                        }
                        else wait = msCheckInterval;

                        Thread.Sleep(wait);
                    }
                    else
                        return true;
                }
            }

            watch.Stop();
            return false;
        }

        public bool WaitSetRequestsFinished(int[] handles, int msCheckInterval = 100, int msTimeout = -1)
        {
            int waitingHandle = 0;

            bool block = msTimeout == -1;

            Stopwatch watch = Stopwatch.StartNew();
            while (block || watch.ElapsedMilliseconds < msTimeout)
            {
                lock (setRequests)
                {
                    //we can just view handles as a stack
                    //Since all the handles need to be finished in order to return, even if a later handle finishes sooner, it won't make a difference.
                    //(The total amount of time spent waiting will be the time the longest waiting handle spends.)
                    if (setRequests[handles[waitingHandle]].state == RequestState.Requested)
                    {
                        int wait = 0;

                        if (!block)
                        {
                            int msRemaining = msTimeout = (int)watch.ElapsedMilliseconds;

                            wait = Math.Min(msCheckInterval, msRemaining - 1);
                        }
                        else wait = msCheckInterval;

                        Thread.Sleep(wait);
                    }
                    else
                    {
                        waitingHandle++;

                        if (waitingHandle >= handles.Length)
                            return true;
                    }
                }
            }

            watch.Stop();
            return false;
        }

        public void Update(double deltaTime)
        {
            currentTime += deltaTime;

            //Get and set requests need to be run concurrently.
            //All get and set for time X need to be processed before get and set for time Y can be processed.
            //We can assume that requests are in order, and that none have a time > currentTime.
            //Basically this is so we simulate the order things were called in. Get and Set might not all be run in one frame,
            //meaning we have gets or sets with occurredAt < currentTime.
            //If we were to run sets until we can't anymore (ran the max amount for this frame), and then the same for gets in that order,
            //we'd potentially run sets from a later frame than the gets, which could cause issues.
            //For instance, a get request might depend on an earlier set request that hasn't yet been resolved.
            //If we were to resolve this using the naive approach (without time sensitivity), then the get request would likely end up 
            //with a stale id, which could result in bugs.

            lock (manager)
            {
                lock (setRequests)
                {
                    //for now, assume we have to process all cubes this frame.
                    int setPosition = startingSet;
                    while (numSetRequests > 0)
                    {
                        ref Request req = ref setRequests[setPosition];

                        if (req.valid && req.state == RequestState.Requested)
                        {
                            manager.SetCube(req.position, req.id, true);
                            req.state = RequestState.Finished;
                            req.callback?.Invoke(req.arg);

                            numSetRequests--;
                        }

                        setPosition = (setPosition + 1) % MAX_REQUESTS;
                    }
                }

                lock (getRequests)
                {
                    int getPosition = startingGet;
                    while (numGetRequests > 0)
                    {
                        ref Request req = ref getRequests[getPosition];

                        if (req.valid && req.state == RequestState.Requested)
                        {
                            if (req.type == RequestType.GetId)
                                req.id = manager.GetCubeId(req.position);
                            else if (req.type == RequestType.GetSides)
                                req.id = (ushort)manager.GetCachedFaces(req.position);

                            req.state = RequestState.Finished;

                            numGetRequests--;
                        }
                        
                        getPosition = (getPosition + 1) % MAX_REQUESTS;
                    }
                }
            }
        }*/
    }
}
