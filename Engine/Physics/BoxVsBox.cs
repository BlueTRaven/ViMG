//using BepuPhysics.Collidables;
//using BepuPhysics.CollisionDetection;
//using BepuPhysics.CollisionDetection.CollisionTasks;
//using BepuUtilities;
//using SharpDX.Direct3D9;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Numerics;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace Engine.Physics
//{
//    public static class BoxVsBox
//    {
//        public struct ManifoldCandidate
//        {
//            public float X;
//            public float Y;
//            public float Depth;
//            public int FeatureId;
//        }

//        static void TestEdgeEdge(
//           ref float halfWidthA, ref float halfHeightA, ref float halfLengthA,
//           ref float halfWidthB, ref float halfHeightB, ref float halfLengthB,
//           ref float offsetBX, ref float offsetBY, ref float offsetBZ,
//           ref Vector3 rBX, ref Vector3 rBY, ref Vector3 rBZ,
//           ref Vector3 edgeBDirection,
//           out float depth, out float localNormalAX, out float localNormalAY, out float localNormalAZ)
//        {
//            //Tests one axis of B against all three axes of A.
//            var x2 = edgeBDirection.X * edgeBDirection.X;
//            var y2 = edgeBDirection.Y * edgeBDirection.Y;
//            var z2 = edgeBDirection.Z * edgeBDirection.Z;
//            {
//                //A.X x edgeB
//                var length = float.Sqrt(y2 + z2);
//                var inverseLength = 1f / length;
//                localNormalAX = 0;
//                localNormalAY = edgeBDirection.Z * inverseLength;
//                localNormalAZ = -edgeBDirection.Y * inverseLength;
//                var extremeA = float.Abs(localNormalAY) * halfHeightA + float.Abs(localNormalAZ) * halfLengthA;
//                var nBX = localNormalAY * rBX.Y + localNormalAZ * rBX.Z;
//                var nBY = localNormalAY * rBY.Y + localNormalAZ * rBY.Z;
//                var nBZ = localNormalAY * rBZ.Y + localNormalAZ * rBZ.Z;
//                var extremeB = float.Abs(nBX) * halfWidthB + float.Abs(nBY) * halfHeightB + float.Abs(nBZ) * halfLengthB;
//                depth = extremeA + extremeB - float.Abs(offsetBY * localNormalAY + offsetBZ * localNormalAZ);
//                if (length < 1e-7f)
//                {
//                    depth = float.MaxValue;
//                }
//            }
//            {
//                //A.Y x edgeB
//                var length = float.Sqrt(x2 + z2);
//                var inverseLength = 1f / length;
//                var nX = edgeBDirection.Z * inverseLength;
//                var nZ = -edgeBDirection.X * inverseLength;
//                var extremeA = float.Abs(nX) * halfWidthA + float.Abs(nZ) * halfLengthA;
//                var nBX = nX * rBX.X + nZ * rBX.Z;
//                var nBY = nX * rBY.X + nZ * rBY.Z;
//                var nBZ = nX * rBZ.X + nZ * rBZ.Z;
//                var extremeB = float.Abs(nBX) * halfWidthB + float.Abs(nBY) * halfHeightB + float.Abs(nBZ) * halfLengthB;
//                var d = extremeA + extremeB - float.Abs(offsetBX * nX + offsetBZ * nZ);
//                if (length < 1e-7f)
//                {
//                    d = float.MaxValue;
//                }
//                if (d < depth)
//                {
//                    depth = d;
//                    localNormalAX = nX;
//                    localNormalAX = 0;
//                    localNormalAX = nZ;
//                }
//            }
//            {
//                //A.Z x edgeB
//                var length = float.Sqrt(x2 + y2);
//                var inverseLength = 1f / length;
//                var nX = edgeBDirection.Y * inverseLength;
//                var nY = -edgeBDirection.X * inverseLength;
//                var extremeA = float.Abs(nX) * halfWidthA + float.Abs(nY) * halfHeightA;
//                var nBX = nX * rBX.X + nY * rBX.Y;
//                var nBY = nX * rBY.X + nY * rBY.Y;
//                var nBZ = nX * rBZ.X + nY * rBZ.Y;
//                var extremeB = float.Abs(nBX) * halfWidthB + float.Abs(nBY) * halfHeightB + float.Abs(nBZ) * halfLengthB;
//                var d = extremeA + extremeB - float.Abs(offsetBX * nX + offsetBY * nY);
//                if (length < 1e-7f)
//                {
//                    d = float.MaxValue;
//                }
//                if (d < depth)
//                {
//                    depth = d;
//                    localNormalAX = nX;
//                    localNormalAY = nY;
//                    localNormalAX = 0;
//                }
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        static void TestFace(ref float halfLengthA,
//            ref float halfWidthB, ref float halfHeightB, ref float halfLengthB,
//            ref float offsetBZ,
//            ref Vector3 rBX, ref Vector3 rBY, ref Vector3 rBZ,
//            out float depth)
//        {
//            depth = halfLengthA + halfWidthB * float.Abs(rBX.Z) + halfHeightB * float.Abs(rBY.Z) + halfLengthB * float.Abs(rBZ.Z) - float.Abs(offsetBZ);
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        static void Select(
//            ref float depth, ref Vector3 normal,
//            ref float candidateDepth, ref float candidateNX, ref float candidateNY, ref float candidateNZ)
//        {
//            if (candidateDepth < depth)
//            {
//                depth = candidateDepth;
//                normal.X = candidateNX;
//                normal.Y = candidateNY;
//                normal.Z = candidateNZ;
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        static void AddBoxAVertex(in Vector3 vertex, in int featureId, in Vector3 faceNormalB, in Vector3 contactNormal, in float inverseContactNormalDotFaceNormalB,
//            in Vector3 faceCenterB, in Vector3 faceTangentBX, in Vector3 faceTangentBY, in float halfSpanBX, in float halfSpanBY,
//            ref ManifoldCandidate[] candidates, ref int candidateCount, int pairCount, in bool allowContacts)
//        {
//            //Cast a ray from the box A vertex up to the box B face along the contact normal.
//            var pointOnBToVertex = vertex - faceCenterB;
//            var planeDistance = Vector3.Dot(faceNormalB, pointOnBToVertex);
//            var offset = contactNormal * (planeDistance * inverseContactNormalDotFaceNormalB);
//            //Contact normal points from B to A by convention, so we have to subtract.
//            var vertexOnBFace = vertex - offset;

//            var vertexOffsetOnBFace = vertexOnBFace - faceCenterB;
//            Unsafe.SkipInit(out ManifoldCandidate candidate);
//            candidate.X = Vector3.Dot(vertexOffsetOnBFace, faceTangentBX);
//            candidate.Y = Vector3.Dot(vertexOffsetOnBFace, faceTangentBY);
//            candidate.FeatureId = featureId;

//            //Note that plane normals are assumed to point inward.
//            var contained = (float.Abs(candidate.X) <= halfSpanBX) & (float.Abs(candidate.Y) <= halfSpanBY);

//            //While we explicitly used an epsilon during edge contact generation, there is a risk of buffer overrun during the face vertex phase.
//            //Rather than assuming our numerical epsilon is guaranteed to always work, explicitly clamp the count. This should essentially never be needed,
//            //but it is very cheap and guarantees no memory stomping with a pretty reasonable fallback.
//            var belowBufferCapacity = candidateCount <= 8;
//            var contactExists = allowContacts & (contained & belowBufferCapacity);

//            if (contactExists)
//            {
//                candidates[candidateCount] = candidate;
//                candidateCount += 1;
//                candidateCount = int.Min(candidateCount, 8);
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void AddBoxAVertices(in Vector3 faceCenterB, in Vector3 faceTangentBX, in Vector3 faceTangentBY, in float halfSpanBX, in float halfSpanBY,
//            in Vector3 faceNormalB, in Vector3 contactNormal,
//            in Vector3 v00, in Vector3 v01, in Vector3 v10, in Vector3 v11,
//            in int f00, in int f01, in int f10, in int f11,
//            ref ManifoldCandidate[] candidates, ref int candidateCount, int pairCount, in bool allowContacts)
//        {
//            var normalDot = Vector3.Dot(faceNormalB, contactNormal);
//#if DEBUG
//            //Note that we don't handle the case where the normalDot is negative. The contact normal points from B to A and the representative face was chosen based on alignment
//            //with the contact normal, so there should never be a case where the contact normal and face normal are opposed.
//            Debug.Assert(normalDot >= 0);
//#endif
//            float inverseContactNormalDotFaceNormalB = 0;
//            if (float.Abs(normalDot) >= 1e-10f)
//            {
//                inverseContactNormalDotFaceNormalB = 1f / normalDot;
//            }
//            else inverseContactNormalDotFaceNormalB = float.MaxValue;

//            AddBoxAVertex(v00, f00, faceNormalB, contactNormal, inverseContactNormalDotFaceNormalB, faceCenterB, faceTangentBX, faceTangentBY, halfSpanBX, halfSpanBY,
//                ref candidates, ref candidateCount, pairCount, allowContacts);
//            AddBoxAVertex(v01, f01, faceNormalB, contactNormal, inverseContactNormalDotFaceNormalB, faceCenterB, faceTangentBX, faceTangentBY, halfSpanBX, halfSpanBY,
//                ref candidates, ref candidateCount, pairCount, allowContacts);
//            AddBoxAVertex(v10, f10, faceNormalB, contactNormal, inverseContactNormalDotFaceNormalB, faceCenterB, faceTangentBX, faceTangentBY, halfSpanBX, halfSpanBY,
//                ref candidates, ref candidateCount, pairCount, allowContacts);
//            AddBoxAVertex(v11, f11, faceNormalB, contactNormal, inverseContactNormalDotFaceNormalB, faceCenterB, faceTangentBX, faceTangentBY, halfSpanBX, halfSpanBY,
//                ref candidates, ref candidateCount, pairCount, allowContacts);
//        }

//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void ClipBoxBEdgeAgainstBoxAFace(in Vector3 edgeDirection,
//            in Vector3 edgeStartB0ToEdgeAnchorA00, in Vector3 edgeStartB0ToEdgeAnchorA11,
//            in Vector3 edgeStartB1ToEdgeAnchorA00, in Vector3 edgeStartB1ToEdgeAnchorA11,
//            in Vector3 boxEdgePlaneNormal,
//            out float min0, out float max0,
//            out float min1, out float max1)
//        {
//            var distance00 = Vector3.Dot(edgeStartB0ToEdgeAnchorA00, boxEdgePlaneNormal);
//            var distance01 = Vector3.Dot(edgeStartB0ToEdgeAnchorA11, boxEdgePlaneNormal);
//            var distance10 = Vector3.Dot(edgeStartB1ToEdgeAnchorA00, boxEdgePlaneNormal);
//            var distance11 = Vector3.Dot(edgeStartB1ToEdgeAnchorA11, boxEdgePlaneNormal);
//            var velocity = Vector3.Dot(boxEdgePlaneNormal, edgeDirection);
//            var inverseVelocity = 1f / velocity;

//            //If the distances to the planes have opposing signs, then the start must be between the two.
//            bool edgeStartIsInside0 = distance00 * distance01 <= 0;
//            bool edgeStartIsInside1 = distance10 * distance11 <= 0;
//            var dontUseFallback = float.Abs(velocity) > 1e-15f;
//            var t00 = distance00 * inverseVelocity;
//            var t01 = distance01 * inverseVelocity;
//            var t10 = distance10 * inverseVelocity;
//            var t11 = distance11 * inverseVelocity;
//            //If the edge direction and plane surface is parallel, then the interval is defined entirely by whether the edge starts inside or outside.
//            //If it's inside, then it's -inf to inf. If it's outside, then there is no overlap and we'll use inf to -inf.
//            var largeNegative = -float.MaxValue;
//            var largePositive = float.MaxValue;
//            min0 = dontUseFallback ? float.Min(t00, t01) : (edgeStartIsInside0 ? largeNegative : largePositive);
//            max0 = dontUseFallback ? float.Max(t00, t01) : (edgeStartIsInside0 ? largePositive : largeNegative);
//            min1 = dontUseFallback ? float.Min(t10, t11) : (edgeStartIsInside1 ? largeNegative : largePositive);
//            max1 = dontUseFallback ? float.Max(t10, t11) : (edgeStartIsInside1 ? largePositive : largeNegative);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void ClipBoxBEdgesAgainstBoxAFace(in Vector3 edgeStartB0, in Vector3 edgeStartB1, in Vector3 edgeDirectionB, in float halfSpanB,
//            in Vector3 vertexA00, in Vector3 vertexA11, in Vector3 edgePlaneNormalAX, in Vector3 edgePlaneNormalAY,
//            out float min0, out float max0, out float min1, out float max1)
//        {
//            var edgeStartB0ToVA00 = Vector3.Subtract(vertexA00, edgeStartB0);
//            var edgeStartB0ToVA11 = Vector3.Subtract(vertexA11, edgeStartB0);
//            var edgeStartB1ToVA00 = Vector3.Subtract(vertexA00, edgeStartB1);
//            var edgeStartB1ToVA11 = Vector3.Subtract(vertexA11, edgeStartB1);
//            ClipBoxBEdgeAgainstBoxAFace(edgeDirectionB, edgeStartB0ToVA00, edgeStartB0ToVA11, edgeStartB1ToVA00, edgeStartB1ToVA11, edgePlaneNormalAX,
//                out var minX0, out var maxX0, out var minX1, out var maxX1);
//            ClipBoxBEdgeAgainstBoxAFace(edgeDirectionB, edgeStartB0ToVA00, edgeStartB0ToVA11, edgeStartB1ToVA00, edgeStartB1ToVA11, edgePlaneNormalAY,
//                out var minY0, out var maxY0, out var minY1, out var maxY1);
//            var negativeHalfSpanB = -halfSpanB;
//            //Note that we are computing the intersection of the two intervals.
//            //If they overlap, then the minimum is the greater of the two minimums, and the maximum is the lesser of the two maximums.
//            //After applying those filters, if the min is greater than the max, then the interval has no actual overlap.
//            //Note that we explicitly do not clamp both sides of the interval. We want to preserve any interval where the max is below -halfSpanB, or the min is above halfSpanB.
//            //Such cases correspond to no contacts.
//            //(Note that we do clamp the maximum to the halfSpan. If an interval maximum reaches the end of the interval, it is used to create a contact representing the associated B vertex.
//            //The minimums are also clamped, because two of the edges flip their intervals during contact generation to ensure winding. The minimum becomes the maximum in that case.)
//            min0 = float.Max(negativeHalfSpanB, float.Max(minX0, minY0));
//            max0 = float.Min(halfSpanB, float.Min(maxX0, maxY0));
//            min1 = float.Max(negativeHalfSpanB, float.Max(minX1, minY1));
//            max1 = float.Min(halfSpanB, float.Min(maxX1, maxY1));
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void AddContactsForEdge(in float min, in ManifoldCandidate minCandidate, in float max, in ManifoldCandidate maxCandidate, in float halfSpanB,
//            in float epsilon, ref ManifoldCandidate[] candidates, ref int candidateCount, in bool allowContacts, int pairCount)
//        {
//            //If -halfSpan<min<halfSpan && (max-min)>epsilon for an edge, use the min intersection as a contact.
//            //If -halfSpan<=max<=halfSpan && max>=min, use the max intersection as a contact.
//            //Note the comparisons: if the max lies on a face vertex, it is used, but if the min lies on a face vertex, it is not. This avoids redundant entries.
//            bool minExists = (allowContacts & ((max - min > epsilon) & (float.Abs(min) < halfSpanB)));
//            if (minExists)
//            {
//                candidates[candidateCount] = minCandidate;
//                candidateCount += 1;
//                candidateCount = int.Min(candidateCount, 8);
//            }

//            var maxExists = (allowContacts & (max >= min) & (float.Abs(max) <= halfSpanB));
//            if (maxExists)
//            {
//                candidates[candidateCount] = maxCandidate;
//                candidateCount += 1;
//                candidateCount = int.Min(candidateCount, 8);
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void CreateEdgeContacts(
//            in Vector3 faceCenterB, in Vector3 faceTangentBX, in Vector3 faceTangentBY, in float halfSpanBX, in float halfSpanBY,
//            in Vector3 vertexA00, in Vector3 vertexA11, in Vector3 faceTangentAX, in Vector3 faceTangentAY, in Vector3 contactNormal,
//            in int featureIdX0, in int featureIdX1, in int featureIdY0, in int featureIdY1,
//            in float epsilonScale, ref ManifoldCandidate[] candidates, ref int candidateCount, int pairCount, in bool allowContacts)
//        {
//            //The critical observation here is that we are working in a contact plane defined by the contact normal- not the triangle face normal or the box face normal.
//            //So, when performing clipping, we actually want to clip on the contact normal plane.
//            //This is consistent with the box vertex test where we cast a ray from the box vertex to triangle along the contact normal.

//            //This is almost identical to testing against the unmodified box face, but we need to change the face edge plane normals.
//            //Rather than being the simple tangent axes, we must compute the plane normals such that the plane embeds the box edge while being perpendicular to the contact normal.
//            var edgePlaneNormalAX = Vector3.Cross(faceTangentAY, contactNormal);
//            var edgePlaneNormalAY = Vector3.Cross(faceTangentAX, contactNormal);

//            var edgeOffsetBX = faceTangentBY * halfSpanBY;
//            var edgeOffsetBY = faceTangentBX * halfSpanBX;
//            var edgeStartBX0 = faceCenterB - edgeOffsetBX;
//            var edgeStartBX1 = faceCenterB + edgeOffsetBX;
//            ClipBoxBEdgesAgainstBoxAFace(edgeStartBX0, edgeStartBX1, faceTangentBX, halfSpanBX, vertexA00, vertexA11, edgePlaneNormalAX, edgePlaneNormalAY,
//                out var minX0, out var maxX0, out var unflippedMinX1, out var unflippedMaxX1);
//            var edgeStartBY0 = Vector3.Subtract(faceCenterB, edgeOffsetBY);
//            var edgeStartBY1 = Vector3.Add(faceCenterB, edgeOffsetBY);
//            ClipBoxBEdgesAgainstBoxAFace(edgeStartBY0, edgeStartBY1, faceTangentBY, halfSpanBY, vertexA00, vertexA11, edgePlaneNormalAX, edgePlaneNormalAY,
//                out var unflippedMinY0, out var unflippedMaxY0, out var minY1, out var maxY1);

//            //The intervals were computed with the edge direction pointing in the same direction for both sides of the face. We want to have a consistent winding all the way around so that
//            //the end of one edge butts up against the start of the next. Given how we choose to create contacts based on the interval min/max, this ensures a good contact distribution.
//            //Flip the X1 and Y0 intervals.
//            var minX1 = -unflippedMaxX1;
//            var maxX1 = -unflippedMinX1;
//            var minY0 = -unflippedMaxY0;
//            var maxY0 = -unflippedMinY0;

//            //We now have intervals for all four box B edges.
//            var edgeFeatureIdOffset = 64;
//            var epsilon = epsilonScale * 1e-5f;
//            Unsafe.SkipInit(out ManifoldCandidate min);
//            Unsafe.SkipInit(out ManifoldCandidate max);
//            //X0
//            min.FeatureId = featureIdX0;
//            min.X = minX0;
//            min.Y = -halfSpanBY;
//            max.FeatureId = featureIdX0 + edgeFeatureIdOffset;
//            max.X = maxX0;
//            max.Y = min.Y;
//            AddContactsForEdge(minX0, min, maxX0, max, halfSpanBX, epsilon, ref candidates, ref candidateCount, allowContacts, pairCount);

//            //Y1
//            min.FeatureId = featureIdY1;
//            min.X = halfSpanBX;
//            min.Y = minY1;
//            max.FeatureId = featureIdY1 + edgeFeatureIdOffset;
//            max.X = halfSpanBX;
//            max.Y = maxY1;
//            AddContactsForEdge(minY1, min, maxY1, max, halfSpanBY, epsilon, ref candidates, ref candidateCount, allowContacts, pairCount);

//            //X1
//            min.FeatureId = featureIdX1;
//            min.X = unflippedMaxX1;
//            min.Y = halfSpanBY;
//            max.FeatureId = featureIdX1 + edgeFeatureIdOffset;
//            max.X = unflippedMinX1;
//            max.Y = halfSpanBY;
//            AddContactsForEdge(minX1, min, maxX1, max, halfSpanBX, epsilon, ref candidates, ref candidateCount, allowContacts, pairCount);

//            //Y0
//            min.FeatureId = featureIdY0;
//            min.X = -halfSpanBX;
//            min.Y = unflippedMaxY0;
//            max.FeatureId = featureIdY0 + edgeFeatureIdOffset;
//            max.X = min.X;
//            max.Y = unflippedMinY0;
//            AddContactsForEdge(minY0, min, maxY0, max, halfSpanBY, epsilon, ref candidates, ref candidateCount, allowContacts, pairCount);
//        }




//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static unsafe void Test(
//            ref BepuPhysics.Collidables.Box a, ref BepuPhysics.Collidables.Box b, ref float speculativeMargin,
//            ref Vector3 offsetB, ref Quaternion orientationA, ref Quaternion orientationB, int pairCount,
//            out Convex4ContactManifold manifold)
//        {
//            Unsafe.SkipInit(out manifold);
//            Matrix3x3.CreateFromQuaternion(orientationA, out var worldRA);
//            Matrix3x3.CreateFromQuaternion(orientationB, out var worldRB);
//            Matrix3x3.MultiplyTransposed(worldRB, worldRA, out var rB);
//            Matrix3x3.TransformTranspose(offsetB, worldRA, out var localOffsetB);

//            Vector3 localNormal;
//            //b.X
//            TestEdgeEdge(
//                ref a.HalfWidth, ref a.HalfHeight, ref a.HalfLength,
//                ref b.HalfWidth, ref b.HalfHeight, ref b.HalfLength,
//                ref localOffsetB.X, ref localOffsetB.Y, ref localOffsetB.Z,
//                ref rB.X, ref rB.Y, ref rB.Z, ref rB.X,
//                out var depth, out localNormal.X, out localNormal.Y, out localNormal.Z);
//            //b.Y
//            TestEdgeEdge(
//                ref a.HalfWidth, ref a.HalfHeight, ref a.HalfLength,
//                ref b.HalfWidth, ref b.HalfHeight, ref b.HalfLength,
//                ref localOffsetB.X, ref localOffsetB.Y, ref localOffsetB.Z,
//                ref rB.X, ref rB.Y, ref rB.Z, ref rB.Y,
//                out var edgeYDepth, out var edgeYNX, out var edgeYNY, out var edgeYNZ);
//            Select(ref depth, ref localNormal,
//                ref edgeYDepth, ref edgeYNX, ref edgeYNY, ref edgeYNZ);
//            //b.Z
//            TestEdgeEdge(
//                ref a.HalfWidth, ref a.HalfHeight, ref a.HalfLength,
//                ref b.HalfWidth, ref b.HalfHeight, ref b.HalfLength,
//                ref localOffsetB.X, ref localOffsetB.Y, ref localOffsetB.Z,
//                ref rB.X, ref rB.Y, ref rB.Z, ref rB.Z,
//                out var edgeZDepth, out var edgeZNX, out var edgeZNY, out var edgeZNZ);
//            Select(ref depth, ref localNormal,
//                ref edgeZDepth, ref edgeZNX, ref edgeZNY, ref edgeZNZ);

//            //Test face normals of A. Working in local space of A means potential axes are just (1,0,0) etc.
//            var absRBX = Vector3.Abs(rB.X);
//            var absRBY = Vector3.Abs(rB.Y);
//            var absRBZ = Vector3.Abs(rB.Z);
//            var faceAXDepth = a.HalfWidth + b.HalfWidth * absRBX.X + b.HalfHeight * absRBY.X + b.HalfLength * absRBZ.X - float.Abs(localOffsetB.X);
//            var one = 1f;
//            var zero = 0f;
//            Select(ref depth, ref localNormal, ref faceAXDepth, ref one, ref zero, ref zero);
//            var faceAYDepth = a.HalfHeight + b.HalfWidth * absRBX.Y + b.HalfHeight * absRBY.Y + b.HalfLength * absRBZ.Y - float.Abs(localOffsetB.Y);
//            Select(ref depth, ref localNormal, ref faceAYDepth, ref zero, ref one, ref zero);
//            var faceAZDepth = a.HalfLength + b.HalfWidth * absRBX.Z + b.HalfHeight * absRBY.Z + b.HalfLength * absRBZ.Z - float.Abs(localOffsetB.Z);
//            Select(ref depth, ref localNormal, ref faceAZDepth, ref zero, ref zero, ref one);

//            //Test face normals of B. Rows of A->B rotation.
//            Matrix3x3.TransformTranspose(localOffsetB, rB, out var bLocalOffsetB);
//            var faceBXDepth = b.HalfWidth + a.HalfWidth * absRBX.X + a.HalfHeight * absRBX.Y + a.HalfLength * absRBX.Z - float.Abs(bLocalOffsetB.X);
//            Select(ref depth, ref localNormal, ref faceBXDepth, ref rB.X.X, ref rB.X.Y, ref rB.X.Z);
//            var faceBYDepth = b.HalfHeight + a.HalfWidth * absRBY.X + a.HalfHeight * absRBY.Y + a.HalfLength * absRBY.Z - float.Abs(bLocalOffsetB.Y);
//            Select(ref depth, ref localNormal, ref faceBYDepth, ref rB.Y.X, ref rB.Y.Y, ref rB.Y.Z);
//            var faceBZDepth = b.HalfLength + a.HalfWidth * absRBZ.X + a.HalfHeight * absRBZ.Y + a.HalfLength * absRBZ.Z - float.Abs(bLocalOffsetB.Z);
//            Select(ref depth, ref localNormal, ref faceBZDepth, ref rB.Z.X, ref rB.Z.Y, ref rB.Z.Z);

//            var activeLanes = BundleIndexing.CreateMaskForCountInBundle(pairCount);
//            var minimumDepth = -speculativeMargin;
//            var allowContacts = Vector.BitwiseAnd(activeLanes, Vector.GreaterThanOrEqual(depth, minimumDepth));
//            if (Vector.EqualsAll(allowContacts, Vector<int>.Zero))
//            {
//                manifold.Contact0Exists = default;
//                manifold.Contact1Exists = default;
//                manifold.Contact2Exists = default;
//                manifold.Contact3Exists = default;
//                return;
//            }
//            //Calibrate the normal to point from B to A, matching convention.
//            var normalDotOffsetB = Vector3.Dot(localNormal, localOffsetB);
//            var shouldNegateNormal = normalDotOffsetB >= 0f;
//            localNormal.X = shouldNegateNormal ? -localNormal.X : localNormal.X;
//            localNormal.Y = shouldNegateNormal ? -localNormal.Y : localNormal.Y;
//            localNormal.Z = shouldNegateNormal ? -localNormal.Z : localNormal.Z;
//            Matrix3x3.Transform(localNormal, worldRA, out manifold.Normal);

//            //Contact generation always assumes face-face clipping. Other forms of contact generation are just special cases of face-face, and since we pay
//            //for all code paths, there's no point in handling them separately.
//            //We just have to guarantee that the face chosen on each box is guaranteed to include the deepest feature along the contact normal.
//            //To do this, choose the face on each box associated with the maximum axis dot with the collision normal.

//            //We represent each face as a center position, its two tangent axes, and the length along those axes.
//            //Technically, we could leave A's tangents implicit by swizzling components, but that complicates things a little bit for not much gain.
//            //Since we're not taking advantage of the dimension reduction of working in A's local space from here on out, just use the world axes to avoid a final retransform.
//             var axDot = Vector3.Dot(manifold.Normal, worldRA.X);
//             var ayDot = Vector3.Dot(manifold.Normal, worldRA.Y);
//            var azDot = Vector3.Dot(manifold.Normal, worldRA.Z);
//            var absAXDot = float.Abs(axDot);
//            var absAYDot = float.Abs(ayDot);
//            var absAZDot = float.Abs(azDot);
//            var maxADot = Vector.Max(absAXDot, Vector.Max(absAYDot, absAZDot));
//            var useAX = Vector.Equals(maxADot, absAXDot);
//            var useAY = Vector.AndNot(Vector.Equals(maxADot, absAYDot), useAX);
//            Vector3.ConditionalSelect(useAX, worldRA.X, worldRA.Z, out var normalA);
//            Vector3.ConditionalSelect(useAY, worldRA.Y, normalA, out normalA);
//            Vector3.ConditionalSelect(useAX, worldRA.Z, worldRA.Y, out var tangentAX);
//            Vector3.ConditionalSelect(useAY, worldRA.X, tangentAX, out tangentAX);
//            Vector3.ConditionalSelect(useAX, worldRA.Y, worldRA.X, out var tangentAY);
//            Vector3.ConditionalSelect(useAY, worldRA.Z, tangentAY, out tangentAY);
//            var halfSpanAX = Vector.ConditionalSelect(useAX, a.HalfLength, Vector.ConditionalSelect(useAY, a.HalfWidth, a.HalfHeight));
//            var halfSpanAY = Vector.ConditionalSelect(useAX, a.HalfHeight, Vector.ConditionalSelect(useAY, a.HalfLength, a.HalfWidth));
//            var halfSpanAZ = Vector.ConditionalSelect(useAX, a.HalfWidth, Vector.ConditionalSelect(useAY, a.HalfHeight, a.HalfLength));
//            //We'll construct vertex feature ids from axis ids. 
//            //Vertex ids will be constructed by setting or not setting the relevant bit for each axis.
//            var localXId = new Vector<int>(1);
//            var localYId = new Vector<int>(4);
//            var localZId = new Vector<int>(16);
//            var axisIdAX = Vector.ConditionalSelect(useAX, localZId, Vector.ConditionalSelect(useAY, localXId, localYId));
//            var axisIdAY = Vector.ConditionalSelect(useAX, localYId, Vector.ConditionalSelect(useAY, localZId, localXId));
//            var axisIdAZ = Vector.ConditionalSelect(useAX, localXId, Vector.ConditionalSelect(useAY, localYId, localZId));

//            Vector3.Dot(manifold.Normal, worldRB.X, out var bxDot);
//            Vector3.Dot(manifold.Normal, worldRB.Y, out var byDot);
//            Vector3.Dot(manifold.Normal, worldRB.Z, out var bzDot);
//            var absBXDot = float.Abs(bxDot);
//            var absBYDot = float.Abs(byDot);
//            var absBZDot = float.Abs(bzDot);
//            var maxBDot = Vector.Max(absBXDot, Vector.Max(absBYDot, absBZDot));
//            var useBX = Vector.Equals(maxBDot, absBXDot);
//            var useBY = Vector.AndNot(Vector.Equals(maxBDot, absBYDot), useBX);
//            Vector3.ConditionalSelect(useBX, worldRB.X, worldRB.Z, out var normalB);
//            Vector3.ConditionalSelect(useBY, worldRB.Y, normalB, out normalB);
//            Vector3.ConditionalSelect(useBX, worldRB.Z, worldRB.Y, out var tangentBX);
//            Vector3.ConditionalSelect(useBY, worldRB.X, tangentBX, out tangentBX);
//            Vector3.ConditionalSelect(useBX, worldRB.Y, worldRB.X, out var tangentBY);
//            Vector3.ConditionalSelect(useBY, worldRB.Z, tangentBY, out tangentBY);
//            var halfSpanBX = Vector.ConditionalSelect(useBX, b.HalfLength, Vector.ConditionalSelect(useBY, b.HalfWidth, b.HalfHeight));
//            var halfSpanBY = Vector.ConditionalSelect(useBX, b.HalfHeight, Vector.ConditionalSelect(useBY, b.HalfLength, b.HalfWidth));
//            var halfSpanBZ = Vector.ConditionalSelect(useBX, b.HalfWidth, Vector.ConditionalSelect(useBY, b.HalfHeight, b.HalfLength));
//            //We'll construct edge feature ids from axis ids. 
//            //Edge ids will be 6 bits total, representing 3 possible states (-1, 0, 1) for each of the 3 axes. Multiply the axis id by 1, 2, or 3 to get the edge id contribution for the axis.
//            var axisIdBX = Vector.ConditionalSelect(useBX, localZId, Vector.ConditionalSelect(useBY, localXId, localYId));
//            var axisIdBY = Vector.ConditionalSelect(useBX, localYId, Vector.ConditionalSelect(useBY, localZId, localXId));
//            var axisIdBZ = Vector.ConditionalSelect(useBX, localXId, Vector.ConditionalSelect(useBY, localYId, localZId));

//            //Calibrate normalB to face toward A, and normalA to face toward B.
//            Vector3.Dot(normalA, manifold.Normal, out var calibrationDotA);
//            var shouldNegateNormalA = Vector.GreaterThan(calibrationDotA, 0f);
//            normalA.X = Vector.ConditionalSelect(shouldNegateNormalA, -normalA.X, normalA.X);
//            normalA.Y = Vector.ConditionalSelect(shouldNegateNormalA, -normalA.Y, normalA.Y);
//            normalA.Z = Vector.ConditionalSelect(shouldNegateNormalA, -normalA.Z, normalA.Z);
//            Vector3.Dot(normalB, manifold.Normal, out var calibrationDotB);
//            var shouldNegateNormalB = Vector.LessThan(calibrationDotB, 0f);
//            normalB.X = Vector.ConditionalSelect(shouldNegateNormalB, -normalB.X, normalB.X);
//            normalB.Y = Vector.ConditionalSelect(shouldNegateNormalB, -normalB.Y, normalB.Y);
//            normalB.Z = Vector.ConditionalSelect(shouldNegateNormalB, -normalB.Z, normalB.Z);

//            //Note that we only allocate up to 8 candidates. It is not possible for this process to generate more than 8 (unless there are numerical problems, which we guard against).
//            int byteCount = Unsafe.SizeOf<ManifoldCandidate>() * 8;
//            var buffer = stackalloc byte[byteCount];
//            ref var candidates = ref Unsafe.As<byte, ManifoldCandidate>(ref *buffer);

//            //Face B edges against face A bound planes
//            Vector3.Scale(normalA, halfSpanAZ, out var faceCenterA);
//            Vector3.Scale(normalB, halfSpanBZ, out var faceCenterB);
//            Vector3.Add(faceCenterB, offsetB, out faceCenterB);
//            Vector3.Subtract(faceCenterA, faceCenterB, out var faceCenterBToFaceCenterA);
//            Vector3.Scale(tangentAY, halfSpanAY, out var edgeOffsetAX);
//            Vector3.Scale(tangentAX, halfSpanAX, out var edgeOffsetAY);

//            Vector3.Subtract(faceCenterA, edgeOffsetAX, out var vertexA0);
//            Vector3.Subtract(vertexA0, edgeOffsetAY, out var vertexA00);
//            Vector3.Add(faceCenterA, edgeOffsetAX, out var vertexA1);
//            Vector3.Add(vertexA1, edgeOffsetAY, out var vertexA11);

//            var epsilonScale = Vector.Min(Vector.Max(halfSpanAX, Vector.Max(halfSpanAY, halfSpanAZ)), Vector.Max(halfSpanBX, Vector.Max(halfSpanBY, halfSpanBZ)));
//            var twiceAxisIdBX = axisIdBX * new Vector<int>(2);
//            var three = new Vector<int>(3);
//            var axisZEdgeIdContribution = axisIdBZ * three;
//            var edgeIdBX0 = twiceAxisIdBX + axisIdBY + axisZEdgeIdContribution;
//            var edgeIdBX1 = twiceAxisIdBX + axisIdBY * three + axisZEdgeIdContribution;
//            var twiceAxisIdBY = axisIdBY * new Vector<int>(2);
//            var edgeIdBY0 = axisIdBX + twiceAxisIdBY + axisZEdgeIdContribution;
//            var edgeIdBY1 = axisIdBX * three + twiceAxisIdBY + axisZEdgeIdContribution;
//            var candidateCount = Vector<int>.Zero;
//            CreateEdgeContacts(faceCenterB, tangentBX, tangentBY, halfSpanBX, halfSpanBY, vertexA00, vertexA11, tangentAX, tangentAY, manifold.Normal,
//                edgeIdBX0, edgeIdBX1, edgeIdBY0, edgeIdBY1, epsilonScale, ref candidates, ref candidateCount, pairCount, allowContacts);

//            //Face A vertices
//            //Vertex ids only have two states per axis, so scale id by 0 or 1 before adding. Equivalent to conditional or.          
//            //Note that the feature id is negated. This disambiguates between edge-edge contacts and vertex contacts.
//            var vertexId00 = -axisIdAZ;
//            Vector3.Add(vertexA0, edgeOffsetAY, out var vertexA01);
//            var vertexId01 = -(axisIdAZ + axisIdAY);
//            Vector3.Subtract(vertexA1, edgeOffsetAY, out var vertexA10);
//            var vertexId10 = -(axisIdAZ + axisIdAX);
//            var vertexId11 = -(axisIdAZ + axisIdAX + axisIdAY);
//            AddBoxAVertices(faceCenterB, tangentBX, tangentBY, halfSpanBX, halfSpanBY, normalB, manifold.Normal,
//                vertexA00, vertexA01, vertexA10, vertexA11, vertexId00, vertexId01, vertexId10, vertexId11, ref candidates, ref candidateCount, pairCount, allowContacts);

//            ManifoldCandidateHelper.Reduce(ref candidates, candidateCount, 8, normalA, new float(-1f) / float.Abs(calibrationDotA), faceCenterBToFaceCenterA, tangentBX, tangentBY, epsilonScale, minimumDepth, pairCount,
//                out var contact0, out var contact1, out var contact2, out var contact3,
//                out manifold.Contact0Exists, out manifold.Contact1Exists, out manifold.Contact2Exists, out manifold.Contact3Exists);

//            //Transform the contacts into the manifold.
//            TransformContactToManifold(ref contact0, ref faceCenterB, ref tangentBX, ref tangentBY, ref manifold.OffsetA0, ref manifold.Depth0, ref manifold.FeatureId0);
//            TransformContactToManifold(ref contact1, ref faceCenterB, ref tangentBX, ref tangentBY, ref manifold.OffsetA1, ref manifold.Depth1, ref manifold.FeatureId1);
//            TransformContactToManifold(ref contact2, ref faceCenterB, ref tangentBX, ref tangentBY, ref manifold.OffsetA2, ref manifold.Depth2, ref manifold.FeatureId2);
//            TransformContactToManifold(ref contact3, ref faceCenterB, ref tangentBX, ref tangentBY, ref manifold.OffsetA3, ref manifold.Depth3, ref manifold.FeatureId3);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void TransformContactToManifold(
//            ref ManifoldCandidate rawContact, ref Vector3 faceCenterB, ref Vector3 tangentBX, ref Vector3 tangentBY,
//            ref Vector3 manifoldOffsetA, ref float manifoldDepth, ref int manifoldFeatureId)
//        {
//            manifoldOffsetA = tangentBX * rawContact.X;
//            var y = tangentBY * rawContact.Y;
//            manifoldOffsetA = manifoldOffsetA + y;
//            manifoldOffsetA = manifoldOffsetA + faceCenterB;
//            manifoldDepth = rawContact.Depth;
//            manifoldFeatureId = rawContact.FeatureId;
//        }

//        public static void Test(ref BoxWide a, ref BoxWide b, ref float speculativeMargin, ref Vector3 offsetB, ref QuaternionWide orientationB, int pairCount, out Convex4ContactManifoldWide manifold)
//        {
//            throw new NotImplementedException();
//        }

//        public static void Test(ref BoxWide a, ref BoxWide b, ref float speculativeMargin, ref Vector3 offsetB, int pairCount, out Convex4ContactManifoldWide manifold)
//        {
//            throw new NotImplementedException();
//        }

//        public struct Convex4ContactManifold
//        {
//            public Vector3 OffsetA0;
//            public Vector3 OffsetA1;
//            public Vector3 OffsetA2;
//            public Vector3 OffsetA3;
//            public Vector3 Normal;
//            public float Depth0;
//            public float Depth1;
//            public float Depth2;
//            public float Depth3;
//            public int FeatureId0;
//            public int FeatureId1;
//            public int FeatureId2;
//            public int FeatureId3;
//            public int Contact0Exists;
//            public int Contact1Exists;
//            public int Contact2Exists;
//            public int Contact3Exists;


//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public void ApplyFlipMask(ref Vector3 offsetB, in bool flipMask)
//            {
//                var flippedNormal = -Normal;
//                var flippedA0 = OffsetA0 - offsetB;
//                var flippedA1 = OffsetA1 - offsetB;
//                var flippedA2 = OffsetA2 - offsetB;
//                var flippedA3 = OffsetA3 - offsetB;
//                var flippedOffsetB = -offsetB;
//                if (flipMask)
//                {
//                    Normal = flippedNormal;
//                    OffsetA0 = flippedA0;
//                    OffsetA1 = flippedA1;
//                    OffsetA2 = flippedA2;
//                    OffsetA3 = flippedA3;
//                    offsetB = flippedOffsetB;
//                }
//            }

//            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//            //public void ReadFirst(in Vector3Wide offsetB, ref ConvexContactManifold target)
//            //{
//            //    target.Count = 0;
//            //    if (Contact0Exists[0] < 0)
//            //    {
//            //        ++target.Count;
//            //        target.Contact0.Offset.X = OffsetA0.X[0];
//            //        target.Contact0.Offset.Y = OffsetA0.Y[0];
//            //        target.Contact0.Offset.Z = OffsetA0.Z[0];
//            //        target.Contact0.Depth = Depth0[0];
//            //        target.Contact0.FeatureId = FeatureId0[0];
//            //    }
//            //    if (Contact1Exists[0] < 0)
//            //    {
//            //        ref var contact = ref Unsafe.Add(ref target.Contact0, target.Count++);
//            //        contact.Offset.X = OffsetA1.X[0];
//            //        contact.Offset.Y = OffsetA1.Y[0];
//            //        contact.Offset.Z = OffsetA1.Z[0];
//            //        contact.Depth = Depth1[0];
//            //        contact.FeatureId = FeatureId1[0];
//            //    }
//            //    if (Contact2Exists[0] < 0)
//            //    {
//            //        ref var contact = ref Unsafe.Add(ref target.Contact0, target.Count++);
//            //        contact.Offset.X = OffsetA2.X[0];
//            //        contact.Offset.Y = OffsetA2.Y[0];
//            //        contact.Offset.Z = OffsetA2.Z[0];
//            //        contact.Depth = Depth2[0];
//            //        contact.FeatureId = FeatureId2[0];
//            //    }
//            //    if (Contact3Exists[0] < 0)
//            //    {
//            //        ref var contact = ref Unsafe.Add(ref target.Contact0, target.Count++);
//            //        contact.Offset.X = OffsetA3.X[0];
//            //        contact.Offset.Y = OffsetA3.Y[0];
//            //        contact.Offset.Z = OffsetA3.Z[0];
//            //        contact.Depth = Depth3[0];
//            //        contact.FeatureId = FeatureId3[0];
//            //    }
//            //    if (target.Count > 0)
//            //    {
//            //        target.OffsetB.X = offsetB.X[0];
//            //        target.OffsetB.Y = offsetB.Y[0];
//            //        target.OffsetB.Z = offsetB.Z[0];
//            //        target.Normal.X = Normal.X[0];
//            //        target.Normal.Y = Normal.Y[0];
//            //        target.Normal.Z = Normal.Z[0];
//            //    }
//            //}
//        }
//    }
//}
