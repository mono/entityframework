﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Entity.Utilities;
    using System.Globalization;
    using System.Linq;
    using Moq;
    using Xunit;

    public class InternalDispatcherTests
    {
        public class AddRemove : TestBase
        {
            [Fact]
            public void Interceptors_for_only_the_matching_interface_type_can_be_added_and_removed()
            {
                var mockInterceptor1 = new Mock<FakeInterceptor1>();
                var mockInterceptor2 = new Mock<FakeInterceptor2>();

                var dispatcher = new InternalDispatcher<FakeInterceptor1>();

                dispatcher.Add(mockInterceptor1.Object);
                dispatcher.Add(mockInterceptor2.Object);

                dispatcher.Dispatch(i => i.CallMe());

                mockInterceptor1.Verify(m => m.CallMe(), Times.Once());
                mockInterceptor2.Verify(m => m.CallMe(), Times.Never());

                dispatcher.Remove(mockInterceptor1.Object);
                dispatcher.Remove(mockInterceptor2.Object);

                dispatcher.Dispatch(i => i.CallMe());

                mockInterceptor1.Verify(m => m.CallMe(), Times.Once());
                mockInterceptor2.Verify(m => m.CallMe(), Times.Never());
            }

            [Fact]
            public void Removing_an_interceptor_that_is_not_registered_is_a_no_op()
            {
                new InternalDispatcher<FakeInterceptor1>().Remove(new Mock<FakeInterceptor1>().Object);
            }

            [Fact]
            public void Interceptors_can_be_added_removed_and_dispatched_to_concurrently()
            {
                var interceptors = new ConcurrentStack<InterceptorForThreads>();
                var dispatcher = new InternalDispatcher<InterceptorForThreads>();

                const int interceptorCount = 20;
                const int dispatchCount = 10;

                // Add in parallel
                ExecuteInParallel(
                    () =>
                        {
                            var interceptor = new InterceptorForThreads();
                            interceptors.Push(interceptor);
                            dispatcher.Add(interceptor);
                        }, interceptorCount);

                Assert.Equal(interceptorCount, interceptors.Count);

                // Dispatch in parallel
                var calledInterceptors = new ConcurrentStack<InterceptorForThreads>();
                ExecuteInParallel(() => dispatcher.Dispatch(calledInterceptors.Push), dispatchCount);

                Assert.Equal(dispatchCount * interceptorCount, calledInterceptors.Count);
                interceptors.Each(i => Assert.Equal(dispatchCount, calledInterceptors.Count(c => c == i)));

                var toRemove = new ConcurrentStack<InterceptorForThreads>(interceptors);

                // Add, remove, and dispatch in parallel
                ExecuteInParallel(
                    () =>
                        {
                            dispatcher.Dispatch(i => { });
                            InterceptorForThreads interceptor;
                            toRemove.TryPop(out interceptor);
                            dispatcher.Remove(interceptor);
                            dispatcher.Add(interceptor);
                        }, interceptorCount);

                // Dispatch in parallel
                calledInterceptors = new ConcurrentStack<InterceptorForThreads>();
                ExecuteInParallel(() => dispatcher.Dispatch(calledInterceptors.Push), dispatchCount);

                Assert.Equal(dispatchCount * interceptorCount, calledInterceptors.Count);
                interceptors.Each(i => Assert.Equal(dispatchCount, calledInterceptors.Count(c => c == i)));
            }

            // Can't use Moq in multi-threaded test
            public class InterceptorForThreads : IDbInterceptor
            {
            }
        }

        public class Dispatch : TestBase
        {
            [Fact]
            public void Simple_Dispatch_dispatches_to_all_registered_interceptors()
            {
                var mockInterceptors = CreateMockInterceptors();
                var dispatcher = new InternalDispatcher<FakeInterceptor1>();
                mockInterceptors.Each(i => dispatcher.Add(i.Object));

                dispatcher.Dispatch(i => i.CallMe());

                mockInterceptors.Each(i => i.Verify(m => m.CallMe(), Times.Once()));
            }

            [Fact]
            public void Result_Dispatch_dispatches_to_all_registered_interceptors_and_aggregates_result()
            {
                var mockInterceptors = CreateMockInterceptors();
                var dispatcher = new InternalDispatcher<FakeInterceptor1>();
                mockInterceptors.Each(i => dispatcher.Add(i.Object));

                Assert.Equal("0123", dispatcher.Dispatch("0", (r, i) => r + i.CallMe()));

                mockInterceptors.Each(i => i.Verify(m => m.CallMe(), Times.Once()));
            }

            [Fact]
            public void Result_Dispatch_returns_result_if_no_dispatchers_registered()
            {
                Assert.Equal("0", new InternalDispatcher<FakeInterceptor1>().Dispatch("0", (r, i) => r + i.CallMe()));
            }

            [Fact]
            public void Operation_Dispatch_dispatches_to_all_registered_interceptors_and_aggregates_results_of_operations()
            {
                var mockInterceptors = CreateMockInterceptors();
                var dispatcher = new InternalDispatcher<FakeInterceptor1>();
                mockInterceptors.Each(i => dispatcher.Add(i.Object));

                Assert.Equal("0123", dispatcher.Dispatch(() => "0", i => i.CallMeFirst(), (r, i) => r + i.CallMe()));

                mockInterceptors.Each(i => i.Verify(m => m.CallMeFirst(), Times.Once()));
                mockInterceptors.Each(i => i.Verify(m => m.CallMe(), Times.Once()));
            }

            [Fact]
            public void Operation_Dispatch_executes_operation_and_returns_result_if_no_dispatchers_registered()
            {
                Assert.Equal(
                    "0", 
                    new InternalDispatcher<FakeInterceptor1>().Dispatch(() => "0", i => i.CallMeFirst(), (r, i) => r + i.CallMe()));
            }
        }

        private static IList<Mock<FakeInterceptor1>> CreateMockInterceptors(int count = 3)
        {
            var mockInterceptors = new List<Mock<FakeInterceptor1>>();
            for (int i = 0; i < count; i++)
            {
                var mock = new Mock<FakeInterceptor1>();
                mockInterceptors.Add(mock);
                mock.Setup(m => m.CallMe()).Returns((i + 1).ToString(CultureInfo.InvariantCulture));
            }
            return mockInterceptors;
        }

        public interface FakeInterceptor1 : IDbInterceptor
        {
            string CallMeFirst();
            string CallMe();
        }

        public interface FakeInterceptor2 : IDbInterceptor
        {
            string CallMe();
        }
    }
}
