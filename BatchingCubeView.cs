using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    //A cube view intended for anything that requires multithreading.
    //This should be the most commonly used view after initialization, as using other views in conjunction with this one can result in race conditions.
    public class BatchingCubeView
    {
        private enum RequestState
        {
            Invalid,
            Requested,
            Finished,
            Consumed
        }
        private struct Request
        {
            public double occurredAt;
            public CubePosition position;
            public ushort id;

            public bool finished;
            public readonly bool valid;

            public Request(CubePosition position, double currentTime)
            {
                this.position = position;
                id = 0;
                occurredAt = currentTime;

                finished = false;
                valid = true;
            }

            public Request(CubePosition position, ushort setTo, double currentTime)
            {
                this.position = position;
                this.id = setTo;
                this.occurredAt = currentTime;

                finished = false;
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
        public int RequestGetCube(CubePosition position)
        {
            currentGet = (currentGet + 1) % MAX_REQUESTS;
            lock (getRequests)
                getRequests[currentGet] = new Request(position, currentTime);
            numGetRequests++;
            return currentGet;
        }

        public void RequestGetCubes(CubePosition[] positions, ref int[] handles)
        {
            if (positions.Length != handles.Length)
                throw new Exception("Length of positions must match length of handles. (Each position will return one handle!)");

            lock (getRequests)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    currentGet = (currentGet + 1) % MAX_REQUESTS;
                    getRequests[currentGet] = new Request(positions[i], currentTime);
                    numGetRequests++;

                    handles[i] = currentGet;
                }
            }
        }

        public bool TryGetGetRequest(int handle, out ushort id)
        {
            lock (getRequests)
            {
                if (getRequests[handle].finished)
                {
                    id = getRequests[handle].id;
                    return true;
                }
            }

            id = 0;
            return false;
        }

        public bool TryGetGetRequests(int[] handles, ref ushort[] ids)
        {
            if (handles.Length != ids.Length)
                throw new Exception("Length of handles must match length of ids. (Each handle will return one id!)");

            lock (getRequests)
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    if (getRequests[handles[i]].finished)
                    {
                        ids[i] = getRequests[handles[i]].id;
                    }
                    //if any request is not yet finished, return false.
                    else return false;
                }
            }

            return handles.Length > 0;
        }

        public int RequestSetCube(CubePosition position, ushort id)
        {
            currentSet = currentSet + 1 % MAX_REQUESTS;
            lock (setRequests)
                setRequests[currentSet] = new Request(position, id, currentTime);
            numSetRequests++;
            return currentSet;
        }

        //this should check version
        public bool IsSetRequestFinished()
        {
            return true;
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
            /*int setPosition = startingSet;
            while (numSetRequests > 0)
            {
                while (processedTime < currentTime && numSetRequests > 0)
                {
                    ref Request req = ref setRequests[setPosition];

                    //if the current request is before the processed time (which, if there are new requests, it should always be)
                    if (req.occurredAt < processedTime)
                    {
                        manager.SetCube(req.position, req.id);

                        setPosition = (setPosition + 1) % MAX_REQUESTS;
                        numSetRequests--;

                        req = ref setRequests[setPosition];
                    }
                    else processedTime += deltaTime;
                }
            }*/

            lock (manager)
            {
                //for now, assume we have to process all cubes this frame.
                int setPosition = startingSet;
                while (numSetRequests > 0)
                {
                    ref Request req = ref setRequests[setPosition];

                    manager.SetCube(req.position, req.id, true);
                    req.finished = true;

                    setPosition = (setPosition + 1) % MAX_REQUESTS;

                    numSetRequests--;
                }

                int getPosition = startingGet;
                while (numGetRequests > 0)
                {
                    ref Request req = ref getRequests[getPosition];

                    req.id = manager.GetCubeId(req.position);
                    req.finished = true;

                    getPosition = (getPosition + 1) % MAX_REQUESTS;

                    numGetRequests--;
                }
            }
        }
    }
}
