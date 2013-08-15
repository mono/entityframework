// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.ConfigFile
{
    using Xunit;

    public class ProviderElementTests : TestBase
    {
        [Fact]
        public void Provider_invariant_can_be_accessed()
        {
            var providerElement = new ProviderElement
                                      {
                                          InvariantName = "Free.Fallin'"
                                      };

            Assert.Equal("Free.Fallin'", providerElement.InvariantName);
        }

        [Fact]
        public void Type_name_can_be_accessed()
        {
            var providerElement = new ProviderElement
                                      {
                                          ProviderTypeName = "All.Right.Now"
                                      };

            Assert.Equal("All.Right.Now", providerElement.ProviderTypeName);
        }
    }
}
