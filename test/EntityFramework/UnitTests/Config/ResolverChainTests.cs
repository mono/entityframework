// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Config
{
    using System.Collections.Concurrent;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using Moq;
    using Xunit;

    public class ResolverChainTests : TestBase
    {
        public interface IPilkington
        {
        }

        public class Karl : IPilkington
        {
        }

        [Fact]
        public void Add_throws_if_given_a_null_resolver()
        {
            Assert.Equal(
                "resolver",
                Assert.Throws<ArgumentNullException>(() => new ResolverChain().Add(null)).ParamName);
        }

        [Fact]
        public void GetService_returns_null_for_empty_chain()
        {
            Assert.Null(new ResolverChain().GetService<IPilkington>());
        }

        [Fact]
        public void GetService_returns_null_if_no_resolver_in_the_chain_resolves_the_dependency()
        {
            var mockResolver1 = CreateMockResolver("Steve", new Mock<IPilkington>().Object);
            var mockResolver2 = CreateMockResolver("Ricky", new Mock<IPilkington>().Object);

            var chain = new ResolverChain();
            chain.Add(mockResolver1.Object);
            chain.Add(mockResolver2.Object);

            Assert.Null(chain.GetService<IPilkington>("Karl"));

            mockResolver1.Verify(m => m.GetService(typeof(IPilkington), "Karl"), Times.Once());
            mockResolver2.Verify(m => m.GetService(typeof(IPilkington), "Karl"), Times.Once());
        }

        [Fact]
        public void GetService_returns_the_service_returned_by_the_most_recently_added_resolver_that_resolves_the_dependency()
        {
            var karl = new Mock<IPilkington>().Object;

            var mockResolver1 = CreateMockResolver("Karl", new Mock<IPilkington>().Object);
            var mockResolver2 = CreateMockResolver("Karl", karl);
            var mockResolver3 = CreateMockResolver("Ricky", new Mock<IPilkington>().Object);

            var chain = new ResolverChain();
            chain.Add(mockResolver1.Object);
            chain.Add(mockResolver2.Object);
            chain.Add(mockResolver3.Object);

            Assert.Same(karl, chain.GetService<IPilkington>("Karl"));

            mockResolver1.Verify(m => m.GetService(typeof(IPilkington), "Karl"), Times.Never());
            mockResolver2.Verify(m => m.GetService(typeof(IPilkington), "Karl"), Times.Once());
            mockResolver3.Verify(m => m.GetService(typeof(IPilkington), "Karl"), Times.Once());
        }

        private static Mock<IDbDependencyResolver> CreateMockResolver<T>(string name, T service)
        {
            var mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver.Setup(m => m.GetService(typeof(T), name)).Returns(service);

            return mockResolver;
        }

        /// <summary>
        ///     This test makes calls from multiple threads such that we have at least some chance of finding threading
        ///     issues. As with any test of this type just because the test passes does not mean that the code is
        ///     correct. On the other hand if this test ever fails (EVEN ONCE) then we know there is a problem to
        ///     be investigated. DON'T just re-run and think things are okay if the test then passes.
        /// </summary>
        [Fact]
        public void GetService_and_Add_can_be_accessed_from_multiple_threads_concurrently()
        {
            for (var i = 0; i < 30; i++)
            {
                var bag = new ConcurrentBag<IPilkington>();
                var resolver = new ResolverChain();
                var karl = new Karl();

                ExecuteInParallel(
                    () =>
                        {
                            resolver.Add(new SingletonDependencyResolver<IPilkington>(karl, "Karl"));
                            bag.Add(resolver.GetService<IPilkington>("Karl"));
                        });

                Assert.Equal(20, bag.Count);
                Assert.True(bag.All(c => karl == c));
            }
        }

        [Fact]
        public void Resolvers_property_returns_resolvers_in_same_order_that_they_were_added()
        {
            var resolvers = new[]
                {
                    new Mock<IDbDependencyResolver>().Object,
                    new Mock<IDbDependencyResolver>().Object,
                    new Mock<IDbDependencyResolver>().Object,
                };

            var chain = new ResolverChain();
            resolvers.Each(chain.Add);

            Assert.Equal(resolvers, chain.Resolvers);
        }
    }
}
